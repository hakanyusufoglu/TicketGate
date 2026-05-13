# MEMORY.md — TicketGate Proje Hafızası
# Her session sonunda bu dosyayı güncelle.
# Yeni session başında ilk oku.

## Tamamlanan Modüller

- [x] Solution iskelet — TicketGate.sln, 11 proje
- [x] TicketGate.Core — Result<T>, AppError, IModule, DomainEvent, ValidationBehavior, PagedResult, ModuleExtensions
- [x] TicketGate.Identity — Register, Login, RefreshToken (BCrypt, JWT, rotation)
- [x] docker-compose.yml — postgres, redis, kafka, debezium, elasticsearch, kibana, kafka-ui
- [x] infrastructure/ klasör yapısı — postgres/init.sql, debezium/connector-config.json
- [x] AGENTS.md — mimari kurallar ve yasaklar
- [x] Mimari döküman — TicketGate-Mimari-Dokuman.docx
- [ ] Prompt 2.5 — Identity uçtan uca test (.http, swagger, migration) — DEVAM EDİYOR
- [ ] TicketGate.Event
- [ ] TicketGate.Booking
- [ ] TicketGate.Payment
- [ ] TicketGate.Notification

## Alınan Mimari Kararlar

| Tarih | Karar | Gerekçe |
|-------|-------|---------|
| 2026-05-13 | Modüler Monolith seçildi | Tek geliştirici + AI, microservice karmaşıklığı erken |
| 2026-05-13 | .NET 10 LTS | Kasım 2028'e kadar destek |
| 2026-05-13 | Vertical Slice Architecture | Feature bazlı organizasyon, Codex ile tutarlı üretim |
| 2026-05-13 | `infrastructure/` adı | `infra/` belirsiz, tam isim tercih edildi |
| 2026-05-13 | Program.cs sade | AddModules + MapModules — her modül kendi IModule'ünü yönetir |
| 2026-05-13 | ModuleExtensions.cs Core'da | Assembly scan ile otomatik modül keşfi |
| 2026-05-13 | Repository pattern YASAK | DbContext direkt handler'da — gereksiz soyutlama yok |
| 2026-05-13 | Result<T> pattern | Exception fırlatma yok, öngörülebilir hata akışı |
| 2026-05-13 | SSE seçildi (WebSocket değil) | Tek yönlü iletişim yeterli, HTTP/2 uyumlu |
| 2026-05-13 | Outbox Pattern (Payment) | Stripe/PayPal çağrısı ve DB yazısı atomik olamaz |
| 2026-05-13 | CDC → Debezium → Kafka → Elastic | Uygulama kodu log yazmaz, WAL otomatik üretir |
| 2026-05-13 | Redis 3 ayrı rol | Lock (SETNX) / SortedSet (Queue) / Pub-Sub (SSE) karıştırılmaz |
| 2026-05-13 | .agent/ klasörü | Tool-agnostic session yönetimi (.claude/ değil) |

## Naming Kuralları (özet)

- Command: `{Action}{Entity}Command`
- Handler: `{Action}{Entity}Handler`
- Validator: `{Action}{Entity}Validator` (sadece Command)
- Query: `Get{Entity}{Qualifier}Query`
- Redis key: `{entity}:{id}:{detail}` → örn: `ticket:uuid:lock`
- Kafka topic: `db.{module}.{table}` → örn: `db.booking.tickets`
- Schema: `identity` | `events` | `booking` | `payment` | `outbox`

## Bilinen Sorunlar / Dikkat Edilecekler

- Identity migration henüz çalıştırılmadı (Prompt 2.5 bekliyor)
- appsettings.Development.json'da Jwt__SecretKey placeholder — production'da değiştirilmeli
- Redis `notify-keyspace-events KEx` docker-compose'da ayarlı, production'da da aktif olmalı
- Debezium connector henüz register edilmedi (CDC pipeline aktif değil)
- `outbox` schema kasıtlı olarak CDC scope dışında — Debezium bu schema'yı izlemez

## Prompt Serisi Durumu

| # | Konu | Durum |
|---|------|-------|
| 1 | Solution iskelet + Core + docker-compose | ✅ Tamamlandı |
| 2 | Identity modülü | ✅ Tamamlandı |
| 2.5 | Identity uçtan uca (.http, swagger, migration) | 🔄 Codex'te |
| 3 | Event modülü | ⏳ Bekliyor |
| 4 | Booking — Ticket entity + ReserveTicket + Redis Lock | ⏳ Bekliyor |
| 5 | Booking — WaitingRoom | ⏳ Bekliyor |
| 6 | Booking — TicketLockExpiredWorker | ⏳ Bekliyor |
| 7 | Payment — InitiatePayment + Outbox | ⏳ Bekliyor |
| 8 | Payment — OutboxWorker | ⏳ Bekliyor |
| 9 | Notification — SSE endpoints | ⏳ Bekliyor |
| 10 | CDC — Debezium + Elasticsearch | ⏳ Bekliyor |

## Stack (hızlı referans)

- .NET 10 LTS · C# 14 · Minimal API
- EF Core 10 · Npgsql · snake_case naming
- MediatR 12 · FluentValidation 11
- PostgreSQL 16 · Redis 7 · Kafka 7.6
- Debezium 2.6 · Elasticsearch 8.13 · Kibana
- BCrypt.Net · JWT Bearer
