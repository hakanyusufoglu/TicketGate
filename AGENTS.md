# TicketGate — AGENTS.md
# Codex ve tüm AI araçları için tek kaynak of truth.
# Her yeni feature yazmadan önce bu dosyayı oku.

## Proje özeti
Bilet satış ve rezervasyon platformu. Modüler Monolith, tek deployment, ileride servise ayrılabilir sınırlar.

## Stack
- .NET 10 (LTS) · Minimal API · C# 14
- PostgreSQL 16 + EF Core 9 (Npgsql) · snake_case naming convention
- MediatR 12 · FluentValidation 11
- StackExchange.Redis (Lock + SortedSet + Pub/Sub)
- Debezium + Kafka (CDC → Elasticsearch log pipeline)
- Elasticsearch 8 + Kibana
- Docker Compose (local dev)

## Solution yapısı
```
TicketGate.sln
├── src/
│   ├── TicketGate.API              ← tek host; tüm modülleri register eder
│   ├── TicketGate.Core             ← shared kernel; NuGet değil ProjectReference
│   └── Modules/
│       ├── TicketGate.Identity
│       ├── TicketGate.Event
│       ├── TicketGate.Booking      ← kritik; Redis Lock + TTL + WaitingRoom
│       ├── TicketGate.Payment      ← Outbox pattern zorunlu
│       └── TicketGate.Notification ← SSE endpoint + Redis Pub/Sub consumer
└── tests/
    ├── TicketGate.Identity.Tests
    ├── TicketGate.Event.Tests
    ├── TicketGate.Booking.Tests
    └── TicketGate.Payment.Tests
```

## Mimari kurallar (DEĞİŞTİRİLEMEZ)

### Genel
- Vertical Slice Architecture — her feature kendi klasöründe yaşar
- CQRS via MediatR — Command (yazar) / Query (okur) kesin ayrımı
- Repository pattern YASAK — DbContext handler içinde direkt kullanılır
- AutoMapper YASAK — `.Select()` projection kullan
- Exception fırlatma YASAK — `Result<T>` döndür
- `CancellationToken ct` — tüm async metodlarda zorunlu
- Endpoint sadece HTTP dönüşümü yapar, iş mantığı içermez
- Handler başka handler çağırmaz
- Domain içinde DbContext kullanılmaz
- Query handler'lara validator eklenmez
- Gereksiz yorum YASAK — "ne yaptığını" anlatan yorum yazılmaz; yalnızca "neden" gerekiyorsa

### Modüller arası iletişim
- Direkt proje referansı YASAK — modüller birbirini import etmez
- İletişim: `MediatR INotification` (domain event) üzerinden
- Örnek: `BookingConfirmed` eventi → Payment + Notification handler'ları aynı anda tetiklenir
- Gelecekte Kafka'ya taşınabilir olacak şekilde interface arkasında tutulur

### Her modülün kendi
- `DbContext`'i vardır (kendi schema'sında)
- `IModule` implementasyonu vardır
- Migration'ları vardır (`--project` flag ile ayrı üretilir)

---

## Naming kuralları

| Tür | Format | Örnek |
|-----|--------|-------|
| Command | `{Action}{Entity}Command` | `ReserveTicketCommand` |
| Handler | `{Action}{Entity}Handler` | `ReserveTicketHandler` |
| Validator | `{Action}{Entity}Validator` | `ReserveTicketValidator` |
| Query | `Get{Entity}{Qualifier}Query` | `GetTicketByIdQuery` |
| DTO | `{Entity}{Qualifier}Dto` | `TicketDetailDto` |
| Domain Event | `{Entity}{Action}` | `TicketReserved` |
| Endpoint dosyası | `{Entity}Endpoints.cs` | `TicketEndpoints.cs` |
| Redis key | `{entity}:{id}:{detail}` | `ticket:42:lock` |
| Kafka topic | `db.{module}.{table}` | `db.booking.tickets` |

---

## Ticket state machine (DEĞİŞTİRİLEMEZ)

```
available → reserved (Redis Lock alındı, TTL=600s)
reserved  → confirmed (ödeme başarılı)
reserved  → available (TTL expire veya kullanıcı vazgeçti)
confirmed → cancelled (iade talebi)
```

State geçişleri yalnızca `Booking` modülünde yapılır.
`TicketStatus` enum: `Available | Reserved | Confirmed | Cancelled`

---

## Redis kullanım kuralları

### Lock (Redlock)
```
Key   : ticket:{ticketId}:lock
Value : {userId}
NX EX : 600 (saniye)
```
- `SETNX` başarısız → `409 Conflict` döndür
- TTL expire → keyspace notification → `TicketLockExpiredWorker` tetiklenir

### Waiting Room (Sorted Set)
```
Key   : waitingroom:{eventId}
Score : Unix timestamp (giriş sırası)
Member: {userId}
```
- `ZADD waitingroom:{eventId} {ts} {userId}` — kuyruğa ekle
- `ZRANK waitingroom:{eventId} {userId}` — pozisyon öğren
- `ZPOPMIN waitingroom:{eventId} {batchSize}` — dispatcher kullanır

### SSE Pub/Sub
```
Channels:
  seat:{ticketId}:status   → koltuk durum değişikliği
  queue:{userId}:turn      → sıra geldi bildirimi
```

---

## Outbox pattern (Payment modülünde zorunlu)

1. `InitiatePayment` handler: tek transaction içinde hem `Payment` kaydı hem `OutboxMessage` yazar
2. `OutboxWorker` (IHostedService): `outbox.messages` tablosunu 5sn polling ile okur
3. Worker harici gateway'i (Stripe/PayPal) çağırır, sonucu yazar
4. Başarılı → `OutboxMessage.ProcessedAt` doldurulur, `BookingConfirmed` eventi publish edilir
5. Başarısız → retry count artırılır, max 3 retry sonrası `Dead Letter` olarak işaretlenir

---

## CDC pipeline

- `wal_level = logical` PostgreSQL'de aktif olmalı
- Debezium connector: `booking`, `payment` schema'larını izler (`outbox` schema izlenmez)
- Kafka topics: `db.booking.tickets`, `db.booking.reservations`, `db.payment.payments`
- Kafka Connect Elasticsearch Sink → index: `ticketgate-{topic}-{yyyy.MM}`

---

## SSE endpoint kuralları

- `GET /api/v1/sse/queue/{eventId}` → waiting room pozisyon stream
- `GET /api/v1/sse/ticket/{ticketId}` → koltuk durum stream
- `Content-Type: text/event-stream`
- `Last-Event-ID` header desteklenmeli (reconnect için)
- Her SSE bağlantısı Redis Pub/Sub kanalına subscribe olur
- Sticky session veya Redis üzerinden fan-out zorunlu

---

## HTTP durum kodları

| İşlem | Başarı | Hata |
|-------|--------|------|
| GET tek | 200 | 404 |
| GET liste | 200 | — |
| POST | 201 | 422 / 409 |
| PUT | 204 | 404 / 409 / 422 |
| DELETE | 204 | 404 |
| Ticket lock çakışması | — | 409 |
| TTL dolmadan ikinci reserve | — | 409 |

---

## Yeni feature eklerken kontrol listesi

1. `Features/{Entity}/Commands/{Action}{Entity}/` veya `Queries/Get{Entity}.../` klasörü aç
2. Command/Query record yaz — `IRequest<Result<T>>`
3. Handler yaz — `internal sealed class`, `CancellationToken ct` zorunlu
4. Validator ekle (yalnızca Command için)
5. `{Entity}Endpoints.cs`'e endpoint ekle — `ToHttpResult()` kullan
6. `IModule.MapEndpoints()` içinde register et
7. Migration: `dotnet ef migrations add {Action}_{Entity}_{Detail} --project src/Modules/TicketGate.{Module}`
8. Handler unit test yaz
9. Projection-first kontrol et — gereksiz `Include` var mı?
10. `CancellationToken` tüm async çağrılara iletildi mi?

---

## Session yönetimi (tüm araçlar için)

### Her session BAŞINDA
1. Bu dosyayı (AGENTS.md) oku — mimari kurallar
2. `.agent/MEMORY.md` oku — ne bitti, hangi kararlar alındı
3. `.agent/CONTEXT.md` oku — aktif görev, sıradaki adım
4. Okuduğunu kısaca özetle, sonra göreve geç

### Her session SONUNDA
1. `.agent/MEMORY.md` güncelle — yeni tamamlananlar, yeni kararlar
2. `.agent/CONTEXT.md` güncelle — aktif görevi ve sıradaki adımı yaz
3. `.agent/HANDOFF.md` güncelle — bu session'ın kısa özeti

### Context %60-70'e geldiğinde
Beklemeden session'ı kapat, üç dosyayı güncelle, yeni session aç.
Yeni session'da ilk mesaj: "AGENTS.md, .agent/MEMORY.md ve .agent/CONTEXT.md oku, özetle, devam et."

---

## Yasak pratikler

```
❌ Repository pattern
❌ AutoMapper
❌ Lazy loading
❌ Exception fırlatarak hata yönetimi
❌ Handler içinde başka handler çağırma
❌ Modüller arası direkt proje referansı
❌ Domain içinde DbContext
❌ Endpoint içinde iş mantığı
❌ Query handler'a validator ekleme
❌ Gereksiz Include (projection navigation'ı çözüyor olabilir)
❌ Magic string (const veya enum kullan)
❌ CancellationToken atlamak
❌ Production'da db.Database.MigrateAsync()
❌ IgnoreQueryFilters() normal handler'larda
❌ Core'a domain veya feature kodu eklemek
❌ Pessimistic lock içinde dış servis çağrısı (Stripe vs)
❌ Redis Lock olmadan ticket reserve etmek
❌ Outbox olmadan Payment → dış gateway çağırmak
```
