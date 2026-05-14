# MEMORY.md — TicketGate Proje Hafızası
# Her session sonunda güncelle. Yeni session başında ilk oku.

## Tamamlanan Modüller

- [x] Solution iskelet — TicketGate.sln, 11 proje
- [x] TicketGate.Core — Result<T>, AppError, IModule, DomainEvent, ValidationBehavior, PagedResult, ModuleExtensions
- [x] TicketGate.Core — Result.ToHttpResult non-generic overload eklendi, validation 422'ye hizalandı
- [x] TicketGate.Identity — Register, Login, RefreshToken (BCrypt, JWT, rotation)
- [x] Prompt 2.5 — Identity uçtan uca: migration, Swagger, .http, docker postgres/redis
- [x] docker-compose.yml — postgres(55432), redis, kafka, debezium, elasticsearch, kibana, kafka-ui
- [x] infrastructure/ klasör yapısı
- [x] AGENTS.md — production-ready (Ocelot, Serilog, Prometheus dahil)
- [x] .agent/ session yönetim sistemi
- [x] TicketGate.Event — Event, Venue, Performer entity'leri
  - CreateEvent, UpdateEvent, PublishEvent, GetEventById, GetEventList
  - CreateVenue, GetVenueById, minimal CreatePerformer
  - EventDbContext, schema: events, Init_Events migration
  - event.http, unit testler geçiyor
- [x] Pre-production check — build/test ve AGENTS.md kural denetimi tamamlandı
  - Build: 0 hata, 0 warning
  - Test: 27 test geçti
  - AddOpenBehavior merkezi ModuleExtensions kaydına taşındı
  - Booking, Payment, Notification IModule implementasyonları eklendi
  - Identity/Event runtime schema adları sabite bağlandı
- [ ] TicketGate.Gateway (Ocelot) — P3 (sıradaki)
- [ ] Foundation: Serilog + Correlation ID + Global Exception — P4
- [ ] Foundation: Health Checks — P5
- [ ] Foundation: Testcontainers — P6
- [ ] TicketGate.Booking — P7-P9 (eskiden P4-P6)
- [ ] TicketGate.Payment — P10-P11
- [ ] TicketGate.Notification — P12
- [ ] CDC Pipeline — P13
- [ ] Production promptları — P14-P21

## Git Durumu

- İlk commit: `ad645ff` — `feat(solution): initial modular monolith skeleton with .NET 10`
- Push: `origin/main`
- Event modülü henüz commit edilmedi

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
| 2026-05-13 | Ocelot Gateway | .NET native, ayrı proje (TicketGate.Gateway) |
| 2026-05-13 | Nginx YOK | Ocelot yeterli, SSL şimdilik yok |
| 2026-05-13 | Tek instance | Donanım kısıtı, scale config hazır |
| 2026-05-13 | Prometheus + Grafana | Kibana ile birlikte monitoring |
| 2026-05-13 | Linux + Docker Compose | Deployment hedefi |
| 2026-05-13 | Kubernetes YOK | Şimdilik gereksiz |
| 2026-05-13 | Docker PostgreSQL port 55432 | Lokal Windows PG 5432 ile çakışma |
| 2026-05-13 | Swagger IdentityModule içinde | Program.cs minimal kuralı |
| 2026-05-13 | Validation HTTP 422 | AGENTS.md kuralına hizalandı |
| 2026-05-13 | CreatePerformer endpoint eklendi | Event .http happy path için gerekli |
| 2026-05-13 | AddOpenBehavior merkezi kayıt | Her modülde tekrar → duplicate pipeline |

## Bilinen Sorunlar / Dikkat

- Docker PG host portu 55432 (lokal PG ile çakışma)
- EF CLI 10.0.5 / runtime 10.0.8 — migration çalışıyor, tooling sonra hizalanacak
- Kafka/Debezium/ES henüz çalıştırılmadı — sonraki fazlarda
- AddOpenBehavior merkezi kayıtta; modüllerde tekrar eklenmemeli
- Magic string taraması migration, route ve table adlarını yakalıyor; schema runtime kullanımı const'a taşındı, EF migration çıktısına manuel müdahale edilmedi
- Event modülü commit edilmedi

## Prompt Serisi Durumu (22 prompt)

| # | Prompt | Durum |
|---|--------|-------|
| P1 | Solution iskelet + Core + docker-compose | ✅ |
| P2 | Identity modülü | ✅ |
| P2.5 | Identity uçtan uca test | ✅ |
| P3 | Event modülü | ✅ |
| P4 (yeni P3) | Gateway — Ocelot | ⏳ Sıradaki |
| P5 (yeni P4) | Serilog + Correlation ID + Global Exception | ⏳ |
| P6 (yeni P5) | Health Checks | ⏳ |
| P7 (yeni P6) | Testcontainers altyapısı | ⏳ |
| P8 (yeni P7) | Booking — Ticket + ReserveTicket + Redis Lock | ⏳ |
| P9 (yeni P8) | Booking — Virtual Waiting Room | ⏳ |
| P10 (yeni P9) | Booking — TicketLockExpiredWorker | ⏳ |
| P11 (yeni P10) | Payment — InitiatePayment + Outbox + Idempotency | ⏳ |
| P12 (yeni P11) | Payment — OutboxWorker + dead letter | ⏳ |
| P13 (yeni P12) | Notification — SSE + Redis fan-out | ⏳ |
| P14 (yeni P13) | CDC — Debezium + Kafka + Elasticsearch | ⏳ |
| P15 | Seed data + Migration stratejisi | ⏳ |
| P16 | Prometheus + Grafana | ⏳ |
| P17 | Docker Compose production konfigürasyonu | ⏳ |
| P18 | CI/CD — GitHub Actions | ⏳ |
| P19 | Environment yönetimi + Secrets | ⏳ |
| P20 | Security hardening | ⏳ |
| P21 | Performance optimizasyonu | ⏳ |
| P22 | Smoke test + E2E test | ⏳ |

## Stack (hızlı referans)

- .NET 10 LTS · C# 14 · Minimal API
- EF Core 10 · Npgsql · snake_case
- MediatR 12 · FluentValidation 11
- PostgreSQL 16 · Redis 7 · Kafka 7.6
- Ocelot (Gateway) · Serilog · Testcontainers
- Debezium 2.6 · Elasticsearch 8.13 · Kibana
- Prometheus · Grafana
- BCrypt.Net · JWT Bearer
- Docker Compose · Linux
