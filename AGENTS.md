# TicketGate — AGENTS.md
# Tüm AI araçları için tek kaynak of truth.
# Her yeni feature yazmadan önce bu dosyayı oku.

## Proje özeti
Bilet satış ve rezervasyon platformu. Modüler Monolith, tek deployment, ileride servise ayrılabilir sınırlar.
Linux ortamında Docker Compose ile çalışır. Ocelot API Gateway üzerinden dışarıya açılır.

## Stack
- .NET 10 (LTS) · Minimal API · C# 14
- PostgreSQL 16 + EF Core 10 (Npgsql) · snake_case naming convention
- MediatR 12 · FluentValidation 11
- StackExchange.Redis (Lock + SortedSet + Pub/Sub)
- Ocelot API Gateway (rate limit, auth, routing, load balance config)
- Debezium + Kafka (CDC → Elasticsearch log pipeline)
- Elasticsearch 8 + Kibana (log analizi)
- Prometheus + Grafana (metrik ve alert)
- Serilog (structured logging)
- Docker Compose (Linux deployment)

## Solution yapısı
```
TicketGate.sln
├── src/
│   ├── TicketGate.Gateway          ← Ocelot; dışarıya açık tek giriş noktası (port 5000)
│   ├── TicketGate.API              ← Minimal API host; iç network, dışarıya kapalı (port 5001)
│   ├── TicketGate.Core             ← Shared kernel; ProjectReference, NuGet değil
│   └── Modules/
│       ├── TicketGate.Identity
│       ├── TicketGate.Event        ← TAMAMLANDI
│       ├── TicketGate.Booking      ← Redis Lock + TTL + WaitingRoom
│       ├── TicketGate.Payment      ← Outbox pattern zorunlu
│       └── TicketGate.Notification ← SSE + Redis Pub/Sub
├── tests/
│   ├── TicketGate.Identity.Tests
│   ├── TicketGate.Event.Tests      ← TAMAMLANDI
│   ├── TicketGate.Booking.Tests
│   └── TicketGate.Payment.Tests
└── infrastructure/
    ├── docker/          → docker-compose.yml
    ├── postgres/        → init.sql
    ├── debezium/        → connector-config.json
    ├── elasticsearch/   → index template
    ├── prometheus/      → prometheus.yml
    └── grafana/         → dashboard json'ları
```

## Ağ mimarisi
```
Internet
    ↓
TicketGate.Gateway (Ocelot) — port 5000, dışarıya açık
    ↓
TicketGate.API — port 5001, sadece iç network (dışarıya kapalı)
    ↓
PostgreSQL · Redis · Kafka · Elasticsearch
```
TicketGate.API docker-compose'da dışarıya port expose etmez.
Tüm dış trafik Gateway üzerinden geçer.

## Bilinen ortam notları
- Docker PostgreSQL host portu: 55432 (lokal Windows PostgreSQL 5432 ile çakışmaması için)
- EF CLI: 10.0.5, runtime: 10.0.8 — migration çalışıyor, tooling ilerleyen fazda hizalanacak
- Şu an sadece postgres ve redis çalışıyor; kafka/debezium/elasticsearch sonraki fazlarda

---

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
- Gereksiz yorum YASAK — yalnızca "neden" gerekiyorsa

### Modüller arası iletişim
- Direkt proje referansı YASAK — modüller birbirini import etmez
- İletişim: `MediatR INotification` (domain event) üzerinden
- Gelecekte Kafka'ya taşınabilir olacak şekilde interface arkasında tutulur

### Her modülün kendi
- `DbContext`'i vardır (kendi schema'sında)
- `IModule` implementasyonu vardır
- Migration'ları vardır (`--project` flag ile ayrı üretilir)

### Gateway kuralları (Ocelot)
- Rate limiting: IP bazlı, endpoint bazlı konfigüre edilir
- JWT validation Gateway'de yapılır — API tekrar validate etmez
- Load balancing: RoundRobin config hazır, replicas: 1 ile başlar
- Her downstream route ocelot.json'da tanımlıdır
- Circuit breaker: Polly ile entegre

### Logging kuralları (Serilog)
- Structured logging zorunlu — string interpolation ile log YASAK
- Her request'te CorrelationId bulunur (X-Correlation-Id header)
- Hassas veri (şifre, token, kart no) loglarda YASAK

### MediatR pipeline kaydı
- `AddOpenBehavior(typeof(ValidationBehavior<,>))` yalnızca bir kez merkezi olarak kaydedilir
- Her modülde tekrar kaydetme — duplicate validation pipeline oluşur

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
| Redis key | `{entity}:{id}:{detail}` | `ticket:uuid:lock` |
| Kafka topic | `db.{module}.{table}` | `db.booking.tickets` |
| Correlation ID header | `X-Correlation-Id` | — |

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

### Lock
```
Key   : ticket:{ticketId}:lock
Value : {userId}
NX EX : 600 (saniye)
```
- SETNX başarısız → 409 Conflict
- TTL expire → keyspace notification → TicketLockExpiredWorker

### Waiting Room (Sorted Set)
```
Key    : waitingroom:{eventId}
Score  : Unix timestamp ms
Member : {userId}
```
- ZADD NX — ilk giriş zamanı korunur
- ZRANK → pozisyon
- ZPOPMIN → dispatcher batch alır

### SSE Pub/Sub
```
seat:{ticketId}:status   → koltuk durum değişikliği
queue:{userId}:turn      → sıra geldi bildirimi
```

---

## Outbox pattern (Payment modülünde zorunlu)

1. Handler: tek transaction → Payment + OutboxMessage
2. OutboxWorker (IHostedService): 5sn polling, batch 10
3. Worker harici gateway'i (Stripe/PayPal) çağırır
4. Başarılı → ProcessedAt doldurulur, BookingConfirmed event publish edilir
5. Başarısız → RetryCount++ (max 3) → Dead Letter

---

## CDC pipeline

- wal_level = logical PostgreSQL'de aktif
- Debezium: booking + payment schema izler (outbox izlenmez)
- Kafka topics: db.booking.tickets, db.payment.payments
- Elasticsearch Sink → index: ticketgate-{topic}-{yyyy.MM}

---

## SSE endpoint kuralları

- Content-Type: text/event-stream
- Last-Event-ID header desteklenmeli (reconnect için)
- Heartbeat: 15 saniyede bir
- Redis Pub/Sub fan-out — çoklu instance hazır

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
| Rate limit aşımı | — | 429 |
| Gateway auth hatası | — | 401 |

---

## Yeni feature eklerken kontrol listesi

1. Features/{Entity}/Commands/{Action}/ veya Queries/Get{Entity}/ klasörü aç
2. Command/Query record yaz — IRequest<Result<T>>
3. Handler yaz — internal sealed, CancellationToken ct zorunlu
4. Validator ekle (yalnızca Command için)
5. {Entity}Endpoints.cs'e endpoint ekle — ToHttpResult() kullan
6. IModule.MapEndpoints() içinde register et
7. ocelot.json'a route ekle (Gateway tamamlandıktan sonra)
8. Migration: --project ile modül bazlı
9. Handler unit test yaz
10. Projection-first kontrol — gereksiz Include var mı?
11. CancellationToken tüm async çağrılara iletildi mi?

---

## Session yönetimi (tüm araçlar için)

### Her session BAŞINDA
1. AGENTS.md oku — mimari kurallar
2. .agent/MEMORY.md oku — ne bitti, kararlar
3. .agent/CONTEXT.md oku — aktif görev
4. Okuduğunu özetle, göreve geç

### Her session SONUNDA
1. .agent/MEMORY.md güncelle
2. .agent/CONTEXT.md güncelle
3. .agent/HANDOFF.md güncelle

### Context %60-70'e geldiğinde
Session'ı kapat, dosyaları güncelle, yeni session aç.

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
❌ Magic string (const veya enum kullan)
❌ CancellationToken atlamak
❌ Production'da db.Database.MigrateAsync()
❌ Core'a domain veya feature kodu eklemek
❌ Redis Lock olmadan ticket reserve etmek
❌ Outbox olmadan Payment → dış gateway çağırmak
❌ Serilog string interpolation ile log yazmak
❌ Hassas veri loglara yazmak
❌ TicketGate.API'yi dışarıya port expose etmek
❌ Her modülde AddOpenBehavior tekrar kaydetmek
```
