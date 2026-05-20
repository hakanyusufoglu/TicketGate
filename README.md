# TicketGate 🎫

Bilet satışı başladı. 1000 kullanıcı aynı anda aynı koltuğa hücum etti.
Sisteminiz bunu kaldırır mı?

TicketGate bu soruyu Redis atomik kilitleri, PostgreSQL xmin concurrency,
Outbox pattern ve Virtual Waiting Room ile yanıtlar.


[![.NET](https://img.shields.io/badge/.NET-10.0_LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-MIT-22c55e)](LICENSE)
[![Tests](https://img.shields.io/badge/Tests-68_passing-22c55e)](tests/)

---

## Problem

Popüler bir konser için bilet satışı başladığında binlerce kullanıcı aynı anda
aynı koltuğu almaya çalışır. Yanlış tasarlanmış bir sistemde:

- Aynı koltuk birden fazla kişiye satılır **(double booking)**
- Stripe başarılı ama veritabanı güncellenemez **(ödeme tutarsızlığı)**
- Kim daha hızlı istek atarsa o kazanır **(adil olmayan sıra)**
- Koltuk durumu anlık güncellenemiyor **(kötü kullanıcı deneyimi)**

Her sorunun kendine özgü bir çözümü vardır. TicketGate bu çözümleri
bir sistemde bir araya getirir.

---

## Çözüm

| Problem | Mekanizma | Teknoloji |
|---------|-----------|-----------|
| Double booking | Atomik kilit | Redis `SETNX NX EX` |
| Race condition | Optimistic concurrency | PostgreSQL `xmin` |
| Ödeme tutarsızlığı | Atomik yazım | Outbox Pattern |
| Adil olmayan sıra | Virtual Waiting Room | Redis Sorted Set |
| Anlık bildirim eksikliği | Server-Sent Events | Redis Pub/Sub |
| Audit trail | Zero-code log | CDC + Debezium |

---

## Mimari
  <img src="https://github.com/user-attachments/assets/57d78ab0-2ac8-460a-98f5-a5cf98f46931" alt="ticketgate-architecture" width="800" />



### Proje Yapısı

```
src/
├── TicketGate.API              ← Minimal API host
├── TicketGate.Core             ← Result<T>, IModule, SeatMap, DomainEvent
└── Modules/
    ├── TicketGate.Identity     ← JWT auth, BCrypt, refresh token rotation
    ├── TicketGate.Event        ← Etkinlik, venue, section/row/seat SeatMap (jsonb)
    ├── TicketGate.Booking      ← Redis Lock, Waiting Room, TTL Worker, QueueDispatcher
    ├── TicketGate.Payment      ← Outbox pattern, IdempotencyKey, Mock Stripe gateway
    └── TicketGate.Notification ← SSE endpoints, Redis Pub/Sub fan-out
```

> Modüller birbirini import etmez. İletişim `Mediator INotification` (domain event) üzerinden.

---

## Kritik Mekanizmalar

### Atomik Kilit — Redis SETNX

```
1000 kullanıcı aynı koltuğa aynı anda istek attı.

SET ticket:{id}:lock {userId} NX EX 600
                               ↑       ↑
                          sadece yoksa  10 dakika TTL

→ 1 kullanıcı true alır, 999 kullanıcı 409 Conflict alır.
→ TTL dolunca TicketLockExpiredWorker bileti tekrar Available'a çeker.
→ Crash recovery: başlangıçta süresi geçmiş lock'lar taranır.
```

### Ödeme Güvencesi — Outbox Pattern

```
Naif yaklaşım:
  db.SaveChanges()      ✓
  stripe.Charge()       ← crash → para çekildi, kayıt yok ❌

Outbox Pattern:
  TEK TRANSACTION:
    payments.INSERT(payment)
    outbox.INSERT(message)   ← ya ikisi ya hiçbiri

  OutboxWorker (5sn polling):
    MockGateway.Charge()     ← ayrı süreçte
    PaymentCompleted event   → ticket otomatik Confirmed
```

`IdempotencyKey` ile network retry'da çifte ödeme engellenir.

### Adil Sıra — Virtual Waiting Room

```
ZADD waitingroom:{eventId} {unix_ts} {userId} NX
                            ↑ giriş zamanı     ↑ iki kez girme engeli

ZRANK   → kullanıcı pozisyonu    O(log N)
ZPOPMIN → sıradaki kullanıcılar  QueueDispatcher, 5sn'de bir

Kapasite boşsa  → direkt rezervasyon
Kapasite doluysa → kuyruğa gir, SSE ile bildirim al
Your turn geldi ama işlem yapılmadıysa → checkout session expire, sıra ilerler

active_checkout sayacı her girişte INCR, her çıkışta DECR yapılır.
Çıkış senaryoları: reserve başarısız · lock TTL expire · ödeme tamamlandı ·
ödeme başarısız · iade · checkout timeout. Sayaç hiçbir zaman sızmaz.
```

### Ticket State Machine

```
          Redis SETNX NX EX 600s
AVAILABLE ──────────────────────► RESERVED
    ▲                                 │
    │  TTL expire (Worker)            │ OutboxWorker → PaymentCompleted
    └─────────────────────────────────┘
                                      ▼
                                  CONFIRMED
                                 /          \
                    Refund      /            \  Cancel
                               ▼              ▼
                           AVAILABLE       CANCELLED
```

---

## Uygulanan Pattern'lar

| Pattern | Nerede | Amaç |
|---------|--------|------|
| **Vertical Slice Architecture** | Tüm modüller | Her feature kendi klasöründe; Command, Handler, Validator, Endpoint bir arada |
| **CQRS** | Tüm modüller | Command (yazar) ve Query (okur) kesin ayrımı; Mediator pipeline |
| **Outbox Pattern** | Payment modülü | Stripe + DB atomikliği; at-least-once garantisi |
| **Result Pattern** | Core | Exception yok; `Result<T>` ile öngörülebilir hata akışı |
| **Domain Event Choreography** | Modüller arası | Booking → Payment → Notification; loose coupling |
| **Optimistic Concurrency** | Booking modülü | PostgreSQL `xmin` ile eş zamanlı güncelleme koruması |
| **Idempotency Pattern** | Payment modülü | Network retry'da çifte ödeme engeli; unique key |
| **Cache-aside Pattern** | Event modülü | Redis → DB fallback; TTL bazlı invalidation |
| **State Machine** | Ticket entity | Durum geçişleri domain içinde kapsüllenmiş; dışarıdan alan değiştirilemiyor |
| **CDC** | Altyapı | Uygulama kodu log yazmaz; WAL → Debezium → Kafka → Elasticsearch |

### Bilinçli Olarak Kullanılmayanlar

| Pattern | Neden yok |
|---|---|
| **Repository Pattern** | EF Core zaten Repository/Unit of Work benzeri bir soyutlama sağlıyor. Üstüne ekstra Repository katmanı eklemek bu proje için gereksiz karmaşıklık yaratır. Handler’lar DbContext’i doğrudan kullanır; davranışlar entegrasyon testlerinde Testcontainers ile doğrulanır. |
| **AutoMapper** | Mapping davranışının görünür olmasını tercih ettim. `Select()` projection ile dönüşümler açık şekilde yazılır ve derleme zamanında daha kolay kontrol edilir. Bu da gizli mapping hatası riskini azaltır. |
| **Saga / Orchestration** | Modüller arası iletişim domain event choreography ile ilerliyor. Bu senaryoda merkezi bir orkestratör eklemek, çözdüğü problemden daha fazla karmaşıklık getirebilir. |
| **API Gateway / Ocelot** | Uygulama şu an tek process içinde çalışan Modular Monolith olarak tasarlandı. Bu aşamada API Gateway eklemek gereksiz operasyonel karmaşıklık oluşturur. Servisler ayrıldığında tekrar değerlendirilebilir. Şimdilik ASP.NET Core built-in RateLimiter yeterli. |

---

## Teknoloji Stack

**Backend**
`.NET 10 LTS` · `C# 14` · `Minimal API` · `EF Core 10` · `Npgsql`

**CQRS & Validation**
`Mediator` (MIT) · `FluentValidation 11`

**Veri**
`PostgreSQL 16` (WAL, xmin, jsonb) · `Redis 7` (SETNX, Sorted Set, Pub/Sub)

**Mesajlaşma & CDC**
`Apache Kafka 7.6` · `Debezium 2.6` · `Elasticsearch 8.13` · `Kibana`

**Observability**
`Prometheus` · `Grafana` · `Serilog` (Elasticsearch sink) · `Correlation ID`

**Test**
`xUnit` · `FluentAssertions` · `Testcontainers` (gerçek PG + Redis) · `Respawn`

**Deployment**
`Docker Compose` · `GitHub Actions CI`

---

## Kurulum

**Gereksinimler:** [.NET 10 SDK](https://dotnet.microsoft.com/download) · [Docker Desktop](https://www.docker.com/products/docker-desktop)

```bash
# 1. Klonla
git clone https://github.com/kullanici/TicketGate.git
cd TicketGate

# 2. Servisleri başlat
docker compose -f infrastructure/docker/docker-compose.yml up -d

# 3. Migration
bash infrastructure/scripts/migrate.sh

# 4. API
dotnet run --project src/TicketGate.API
# → http://localhost:5123
# → http://localhost:5123/swagger

# 5. CDC connector (opsiyonel)
curl -X POST http://localhost:8083/connectors \
  -H "Content-Type: application/json" \
  -d @infrastructure/debezium/connector-config.json
```

> Uygulama başlarken seed data otomatik yüklenir:
> Volkswagen Arena (50 koltuk · VIP / Normal / Ekonomi) + Tarkan Konseri 2026

---

## Test

```bash
# Tüm testler (unit + integration)
dotnet test TicketGate.sln -v minimal -m:1
```

> Integration testler Testcontainers ile gerçek PostgreSQL ve Redis kullanır.
> `xmin` concurrency ve Redis race condition InMemory DB ile test edilemez.

```
src/TicketGate.API/Http/
  e2e.http   → 5 senaryo: rezervasyon, iade, race condition, waiting room, hata
```

**SSE testi:**
```cmd
curl -N -H "Authorization: Bearer TOKEN" ^
  http://localhost:5123/api/v1/sse/ticket/TICKET_ID
```

---

## Monitoring

| Servis | URL |
|--------|-----|
| Swagger | `http://localhost:5123/swagger` |
| Grafana | `http://localhost:3000` · admin / ticketgate |
| Kibana | `http://localhost:5601` |
| Prometheus | `http://localhost:9090` |
| Kafka UI | `http://localhost:8080` |

---

## Lisans

[MIT](LICENSE)
