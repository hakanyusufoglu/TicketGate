# MEMORY.md - TicketGate Proje Hafizasi

Her session sonunda bu dosya guncellenir. Yeni session basinda AGENTS.md ve CONTEXT.md ile birlikte okunur.

## Tamamlanan Moduller

- [x] Solution iskeleti - TicketGate.sln, 11 proje
- [x] TicketGate.Core - Result<T>, AppError, IModule, DomainEvent, ValidationBehavior, PagedResult, ModuleExtensions
- [x] TicketGate.Identity - Register, Login, RefreshToken (BCrypt, JWT, refresh token rotation)
- [x] Prompt 2.5 - Identity uctan uca hazirlik: migration, Swagger, .http dosyalari, docker postgres/redis
- [x] docker-compose.yml - postgres, redis, kafka, debezium, elasticsearch, kibana, kafka-ui
- [x] infrastructure/ klasor yapisi - postgres/init.sql, debezium/connector-config.json, docker/docker-compose.yml
- [x] AGENTS.md - mimari kurallar ve yasaklar
- [x] .agent/ session yonetimi - MEMORY.md, CONTEXT.md, HANDOFF.md
- [ ] TicketGate.Event
- [ ] TicketGate.Booking
- [ ] TicketGate.Payment
- [ ] TicketGate.Notification

## Git Durumu

- Ilk commit atildi.
- Ilk commit hash: `ad645ff8139e3f4234adc5d7cf2f5426d8947fc9`
- Ilk commit mesajı: `feat(solution): initial modular monolith skeleton with .NET 10`
- Ilk commit `origin/main` branch'ine push edildi.

## Alinan Mimari Kararlar

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-13 | Modular Monolith secildi | Tek deployment, module boundary korunur, microservice karmasasi ertelenir |
| 2026-05-13 | Program.cs sade tutuldu | Host sadece `AddModules` ve `MapModules` cagirir |
| 2026-05-13 | Module discovery Core icinde | Yeni modul `IModule` implemente ederek sisteme dahil olur |
| 2026-05-13 | Repository pattern yasaklandi | EF Core DbContext handler icinde dogrudan kullanilir |
| 2026-05-13 | AutoMapper yasaklandi | Projection ve explicit mapping tercih edilir |
| 2026-05-13 | Result<T> hata modeli secildi | Beklenen hatalar exception ile degil typed result ile doner |
| 2026-05-13 | `infra/` klasoru `infrastructure/` olarak degistirildi | Daha acik ve uzun vadede okunabilir isim |
| 2026-05-13 | Docker PostgreSQL host portu `55432` yapildi | Lokal Windows PostgreSQL `5432` ile port cakismasi engellendi |
| 2026-05-13 | Swagger IdentityModule icinde kaydedildi | Program.cs minimal kuralini bozmadan Development ortaminda Swagger acildi |
| 2026-05-13 | Identity migration `Infrastructure/Persistence/Migrations` altina alindi | Modul bazli migration sahipligi korunur |

## Bilinen Sorunlar / Dikkat Edilecekler

- Host uzerinde lokal PostgreSQL `5432` dinliyor olabilir; Docker PostgreSQL host portu `55432` olarak kullanilmali.
- EF CLI surumu `10.0.5`, runtime `10.0.8`; su an migration calisiyor, fakat tooling surumu sonra hizalanmali.
- `appsettings.Development.json` icindeki JWT secret sadece local development icindir.
- Debezium connector config mevcut, fakat connector henuz Kafka Connect'e register edilmedi.
- Kafka, Debezium, Elasticsearch, Kibana ve Kafka UI compose dosyasinda var; mevcut local calismada sadece postgres ve redis baslatildi.

## Prompt Serisi Durumu

| # | Konu | Durum |
|---|------|-------|
| 1 | Solution iskelet + Core + docker-compose | Tamamlandi |
| 2 | Identity modulu | Tamamlandi |
| 2.5 | Identity uctan uca (.http, Swagger, migration) | Tamamlandi |
| 3 | Event modulu | Bekliyor |
| 4 | Booking - Ticket entity + ReserveTicket + Redis Lock | Bekliyor |
| 5 | Booking - WaitingRoom | Bekliyor |
| 6 | Booking - TicketLockExpiredWorker | Bekliyor |
| 7 | Payment - InitiatePayment + Outbox | Bekliyor |
| 8 | Payment - OutboxWorker | Bekliyor |
| 9 | Notification - SSE endpoints | Bekliyor |
| 10 | CDC - Debezium + Elasticsearch | Bekliyor |
