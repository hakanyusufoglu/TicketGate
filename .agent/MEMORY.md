# MEMORY.md — TicketGate Proje Hafızası
# Her session sonunda güncelle. Yeni session başında ilk oku.

## Tamamlanan Modüller

- [x] Solution iskelet — TicketGate.sln, 11 proje
- [x] TicketGate.Core — Result<T>, AppError, IModule, DomainEvent, ValidationBehavior, PagedResult, ModuleExtensions
- [x] TicketGate.Core — Result.ToHttpResult non-generic overload, validation 422'ye hizalandı
- [x] TicketGate.Identity — Register, Login, RefreshToken (BCrypt, JWT, rotation)
- [x] Prompt 2.5 — Identity uçtan uca: migration, Swagger, .http, docker postgres/redis
- [x] docker-compose.yml — postgres(55432), redis, kafka, debezium, elasticsearch, kibana, kafka-ui
- [x] infrastructure/ klasör yapısı
- [x] AGENTS.md — production-ready + XML summary kuralları
- [x] .agent/ session yönetim sistemi
- [x] TicketGate.Event — Event, Venue, Performer, tüm slicelar, migration, testler
- [x] Testcontainers altyapısı — P4 tamamlandı
- [x] TicketGate.TestInfrastructure projesi eklendi
- [x] TicketGate.Booking — P5 tamamlandı (Ticket + ReserveTicket + Redis Lock)
- [x] BookingIntegrationTestBase ConfigureServices dolduruldu
- [x] Init_Tickets migration oluşturuldu
- [x] Seed data tamamlandı (Venue + Performer + Event)
- [x] SeedGuids oluşturuldu
- [x] http-client.env.json güncellendi
- [x] event.http sabit Guid'lere geçirildi
- [ ] TicketGate.Booking — P6-P7 (sıradaki)
- [ ] TicketGate.Payment — P8-P9
- [ ] TicketGate.Notification — P10
- [ ] CDC Pipeline — P11
- [ ] Production promptları — P12-P19

## Çıkarılan / Ertelenen Kararlar

| Karar | Gerekçe |
|-------|---------|
| Ocelot Gateway atlandı | Modüler monolith'te gereksiz karmaşıklık; microservice'e geçince eklenecek |
| Serilog atlandı | CDC → Elasticsearch zaten var; console logging şimdilik yeterli; CDC kurulunca eklenecek |
| Health Checks atlandı | P14 Docker Compose production'da eklenecek; şimdilik gereksiz |
| ASP.NET Core built-in RateLimiter | Ocelot yerine; tek servis için yeterli |

## Alınan Mimari Kararlar

| Tarih | Karar | Gerekçe |
|-------|-------|---------|
| 2026-05-13 | Modüler Monolith | Tek geliştirici + AI |
| 2026-05-13 | .NET 10 LTS | 2028'e kadar destek |
| 2026-05-13 | Vertical Slice | Feature bazlı, Codex uyumlu |
| 2026-05-13 | infrastructure/ adı | infra/ yerine tam isim |
| 2026-05-13 | Program.cs sade | AddModules + MapModules |
| 2026-05-13 | Repository YASAK | DbContext direkt handler'da |
| 2026-05-13 | Result<T> pattern | Exception yok |
| 2026-05-13 | SSE (WebSocket değil) | Tek yönlü yeterli |
| 2026-05-13 | Outbox (Payment) | Atomiklik garantisi |
| 2026-05-13 | CDC → Debezium | Uygulama log yazmaz |
| 2026-05-13 | Redis 3 ayrı rol | Lock / SortedSet / Pub-Sub |
| 2026-05-13 | .agent/ klasörü | Tool-agnostic session yönetimi |
| 2026-05-13 | Kafka iki rol | CDC pipeline + ileride queue bildirimi |
| 2026-05-13 | Docker PG port 55432 | Lokal PG ile çakışma |
| 2026-05-13 | Validation HTTP 422 | AGENTS.md kuralına hizalandı |
| 2026-05-13 | AddOpenBehavior merkezi | Her modülde tekrar → duplicate |
| 2026-05-13 | XML summary Türkçe | GitHub'dan okuyan anlasın |
| 2026-05-13 | Ocelot YOK (şimdilik) | Monolith'te gereksiz katman |
| 2026-05-13 | Serilog YOK (şimdilik) | CDC ES var; sonra eklenecek |
| 2026-05-13 | Health Checks YOK (şimdilik) | Production Docker'da eklenecek |
| 2026-05-13 | Built-in RateLimiter | Ocelot yerine; tek servis yeterli |

## Git Durumu

- İlk commit: ad645ff — feat(solution): initial modular monolith skeleton
- Event modülü henüz commit edilmedi

## Bilinen Sorunlar / Dikkat

- Docker PG host portu: 55432
- EF CLI 10.0.5 / runtime 10.0.8 — çalışıyor
- Kafka/Debezium/ES henüz çalıştırılmadı
- AddOpenBehavior her modülde tekrar kaydediliyor — Check promptunda düzeltilecek
- Event modülü commit edilmedi
- XML summary P1-P3 kodunda eksik — Check promptunda eklenecek

## Prompt Serisi Durumu (19 prompt)

| # | Prompt | Durum |
|---|--------|-------|
| P1 | Solution iskelet + Core + docker-compose | ✅ |
| P2 | Identity modülü | ✅ |
| P2.5 | Identity uçtan uca test | ✅ |
| P3 | Event modülü | ✅ |
| Check | Pre-production kontrol + summary audit + commit | 🔄 |
| P4 | Testcontainers altyapısı | ✅ |
| P5 | Booking — Ticket + ReserveTicket + Redis Lock | ✅ |
| P6 | Booking — Virtual Waiting Room | ⏳ |
| P7 | Booking — TicketLockExpiredWorker | ⏳ |
| P8 | Payment — InitiatePayment + Outbox + Idempotency | ⏳ |
| P9 | Payment — OutboxWorker + dead letter | ⏳ |
| P10 | Notification — SSE + Redis fan-out | ⏳ |
| P11 | CDC — Debezium + Kafka + Elasticsearch | ⏳ |
| P12 | Seed data + Migration stratejisi | 🔄 |
| P13 | Prometheus + Grafana | ⏳ |
| P14 | Docker Compose production (Health Checks dahil) | ⏳ |
| P15 | CI/CD — GitHub Actions | ⏳ |
| P16 | Environment yönetimi + Secrets | ⏳ |
| P17 | Security hardening + Built-in RateLimiter | ⏳ |
| P18 | Performance optimizasyonu | ⏳ |
| P19 | Smoke test + E2E test | ⏳ |

## Stack (hızlı referans)

- .NET 10 LTS · C# 14 · Minimal API
- EF Core 10 · Npgsql · snake_case
- MediatR 12 · FluentValidation 11
- PostgreSQL 16 · Redis 7 · Kafka 7.6
- Debezium 2.6 · Elasticsearch 8.13 · Kibana
- Prometheus · Grafana
- BCrypt.Net · JWT Bearer
- Testcontainers · xUnit · FluentAssertions
- Docker Compose · Linux

## 2026-05-14 Ara Gorev Notu

- [x] SeatMap value object Core'a tasindi
- [x] Venue entity typed SeatMap formatina guncellendi
- [x] GenerateTickets slice eklendi
- [x] SeatDto section/row/seat bilgisiyle guncellendi
- [x] Update_Venue_SeatMap migration olusturuldu ve database update calistirildi

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-14 | SeatMap Core contract | Event ve Booking tarafinda ortak seat map modeli gerekiyor; moduller arasi direkt referans yerine Core contract kullanildi |
| 2026-05-14 | IEventSeatMapReader | Booking endpoint'i EventDbContext'e direkt baglanmasin diye Event modulu seat map okuma soyutlamasi sagliyor |

## 2026-05-14 Configuration Refactor Notu

- [x] BookingSettings strongly-typed options eklendi; Redis lock TTL appsettings BookingSettings:LockTtlSeconds uzerinden okunuyor.
- [x] JwtSettings eklendi; access token, refresh token ve clock skew sureleri config'e tasindi.
- [x] OutboxSettings ve SseSettings options siniflari eklendi; ileride worker/heartbeat magic number kullanmadan baglanacak.
- [x] appsettings.json ve appsettings.Development.json BookingSettings, OutboxSettings ve SseSettings ile guncellendi.

## 2026-05-14 TicketLockExpiredWorker Notu

- [x] TicketLockExpiredWorker tamamlandi
- [x] Lock dongusu tamamlandi
- [x] Redis keyspace expired event'i ticket:{id}:lock formatinda dinleniyor
- [x] Startup crash recovery ile suresi gecmis Reserved ticket'lar Available'a cekiliyor
