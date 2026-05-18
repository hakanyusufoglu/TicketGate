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
- [x] TicketGate.Booking — P6 Virtual Waiting Room tamamlandi
- [x] TicketGate.Booking — P7 TicketLockExpiredWorker tamamlandi
- [x] TicketGate.Payment — P8 InitiatePayment + Outbox + Idempotency
- [x] TicketGate.Payment — P9 OutboxWorker
- [x] Iade akisi tamamlandi
- [x] PaymentRefundedHandler Booking'e eklendi
- [x] Ticket Confirmed -> Available donusumu calisiyor
- [x] CancelTicket vs Refund ayrimi netlesti
- [x] TicketGate.Notification — P10
- [x] CDC Pipeline — P11
- [x] CDC pipeline aktif
- [x] Debezium connector çalışıyor
- [x] Kafka topics oluştu
- [x] Elasticsearch index template
- [x] Serilog + Elasticsearch entegrasyonu
- [x] Correlation ID middleware
- [x] Prometheus + Grafana tamamlandi
- [x] Ozel metrikler eklendi
- [x] Alert rules tanimlandi
- [x] Docker Compose production konfigürasyonu
- [x] Health checks eklendi
- [x] Dockerfile oluşturuldu
- [x] .env.example oluşturuldu
- [x] Migration script eklendi
- [x] CI/CD pipeline tamamlandi
- [x] GitHub Actions workflows eklendi
- [x] Dependabot eklendi, sonra temiz GitHub Actions ekrani icin devre disi birakildi
- [x] PR template eklendi
- [x] CD workflow roadmap olarak tutuldu, otomatik deploy devre disi
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
| P6 | Booking — Virtual Waiting Room | ✅ |
| P7 | Booking — TicketLockExpiredWorker | ✅ |
| P8 | Payment — InitiatePayment + Outbox + Idempotency | ✅ |
| P9 | Payment — OutboxWorker + dead letter | ✅ |
| P10 | Notification — SSE + Redis fan-out | ✅ |
| P11 | CDC — Debezium + Kafka + Elasticsearch | ✅ |
| P12 | Seed data + Migration stratejisi | 🔄 |
| P13 | Prometheus + Grafana | ✅ |
| P14 | Docker Compose production (Health Checks dahil) | ✅ |
| P15 | CI/CD — GitHub Actions | ✅ |
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

## 2026-05-14 Virtual Waiting Room Notu

- [x] Virtual Waiting Room tamamlandi
- [x] QueueDispatcher eklendi
- [x] JoinQueue, LeaveQueue ve GetQueuePosition slicelari eklendi
- [x] Redis Sorted Set sirasi ZADD NX ve ZRANK ile korunuyor
- [x] Direct join ve dispatcher kapasite grant islemleri Lua script ile atomik hale getirildi
- [x] QueueDispatcher Redis Pub/Sub queue:{userId}:turn kanalina your_turn mesaji yayinliyor
- [x] waitingroom.http ve WaitingRoom integration testleri eklendi

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-14 | Queue event source id alani SourceEventId | DomainEvent zaten EventId metadata property'sine sahip; source event id icin EventId kullanmak record parametresini bosa dusuruyor ve consumer hatasina yol acar |
| 2026-05-14 | Waiting room kapasite grant Lua ile atomik | StringGet + INCR ayri komutlar olursa es zamanli join/dispatcher istekleri MaxCheckoutCapacity asabilir |

## 2026-05-15 Payment P8 Notu

- [x] TicketGate.Payment P8 tamamlandi
- [x] Payment entity ve PaymentStatus enum eklendi
- [x] OutboxMessage entity, outbox mesaj tipleri ve payment schema migration eklendi
- [x] InitiatePayment command/validator/handler endpoint ve integration testleri eklendi
- [x] IdempotencyKey unique index ve mevcut response donme davranisi eklendi
- [x] MockPaymentGateway ve IPaymentGateway eklendi; handler gateway cagirmiyor
- [x] RefundPayment ve GetPaymentById temel slicelari eklendi
- [x] Booking reserved ticket kontrolu Core ITicketReservationReader contract'i ile yapiliyor; Payment Booking DbContext'e referans vermiyor
- [x] Init_Payments migration local PostgreSQL payment schema'sina uygulandi

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-15 | ITicketReservationReader Core contract | Payment modulu Ticket Reserved/UserId kontrolu yapmak zorunda ama Booking projesine direkt referans yasak; contract siniri moduler monolith kuralini koruyor |
| 2026-05-15 | Refund handler status'u hemen Refunded yapmiyor | Harici gateway sonucu P9 OutboxWorker tarafindan dogrulanmadan Completed -> Refunded yapmak production icin yanlis durum bildirimi olur |

## 2026-05-15 Payment P9 Notu

- [x] InitiatePayment refactor tamamlandi; UserId body'den kaldirildi ve JWT NameIdentifier claim'inden okunuyor.
- [x] InitiatePayment amount refactor tamamlandi; Amount body'den kaldirildi ve ticket price Core ITicketReservationReader contract'i uzerinden okunuyor.
- [x] ITicketReservationReader contract'i Price bilgisi tasiyacak sekilde genisletildi; Payment modulu Booking projesine direkt referans vermiyor.
- [x] OutboxWorker tamamlandi; PaymentInitiated ve PaymentRefundRequested mesajlarini batch ile isliyor.
- [x] Gateway basarili charge sonucunda Payment Completed oluyor, outbox processed isaretleniyor ve PaymentCompleted event'i yayinlaniyor.
- [x] Gateway basarisiz charge sonucunda RetryCount artiyor; MaxRetryCount sonrasi Payment Failed oluyor ve PaymentFailed event'i yayinlaniyor.
- [x] Booking PaymentCompleted/PaymentFailed handler'lari eklendi; ticket Confirmed/Available akisinda Redis lock temizleniyor.
- [x] Payment endpointleri authorization ve OpenAPI metadata ile guncellendi.
- [x] Testcontainers altyapisinda PostgreSQL ve Redis start sirali hale getirildi; Docker named pipe timeout flake'i azaltildi.

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-15 | Payment event contract'lari Core'a tasindi | Booking modulu Payment projesine referans veremez; integration event contract'i shared kernel sinirinda kalmali |
| 2026-05-15 | Payment handler BookingDbContext kullanmiyor | Prompt'taki onerinin aksine bu moduller arasi direkt referans kuralini ihlal ederdi; mevcut Core contract genisletildi |

## 2026-05-15 Endpoint Security Refactor Notu

- [x] Security refactor — userId JWT'den okunuyor
- [x] ClaimExtensions Core'a eklendi
- [x] Tum endpoint'lere WithSummary + WithDescription + Produces eklendi
- [x] Booking reserve/confirm/cancel endpointleri userId body yerine HttpContext.GetUserId() kullaniyor
- [x] WaitingRoom join/position/leave endpointleri userId body/query yerine JWT claim kullaniyor
- [x] Payment refund endpoint'i userId body yerine JWT claim kullaniyor

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-15 | UserId endpoint katmaninda JWT claim'den okunuyor | Client body/query ile baska kullanici adina islem yapamamali; command ve handler sozlesmeleri korunarak guvenlik siniri endpoint'te kapatildi |
| 2026-05-15 | ClaimExtensions Core'da tutuluyor | Moduller arasinda tekrar eden claim okuma kodu yerine shared kernel extension'i kullanildi |

## 2026-05-15 Refund Flow Notu

- [x] Iade akisi tamamlandi
- [x] Ticket.ReleaseAfterRefund() eklendi; Confirmed -> Available gecisini kapsuller
- [x] PaymentRefundedHandler Booking'e eklendi; PaymentRefunded event'i gelince ticket tekrar satisa aciliyor
- [x] Ticket Confirmed -> Available donusumu integration test ile dogrulandi
- [x] RefundPayment wrong user senaryosu 401 Unauthorized'a hizalandi
- [x] OutboxWorker refund mesaji icin Payment Refunded ve PaymentRefunded event akisina test eklendi
- [x] CancelTicket vs Refund ayrimi netlesti: CancelTicket Confirmed -> Cancelled ve bilet satisa donmez; Refund Confirmed -> Available ve bilet tekrar satisa acilir

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-15 | Refund icin ayri ReleaseAfterRefund metodu | Release() TTL/payment failure icin Reserved -> Available akisini temsil ediyor; refund Confirmed -> Available oldugu icin state gecisini ayirmak daha okunabilir ve test edilebilir |
| 2026-05-15 | Wrong user refund 401 | Baska kullanicinin odemesini iade etmeye calismak is kuralindan cok authorization ihlalidir; 409 bu durum icin zayif semantik olur |

## 2026-05-15 OutboxWorker Son Kontrol Notu

- [x] OutboxWorker tamamlandi
- [x] MockPaymentGateway Stripe simulasyonu ayri dosyaya tasindi ve `mock_ch_{guid}` formatinda ExternalPaymentId uretiyor
- [x] PaymentInitiated/Refund payload record'lari worker payload klasoru altina tasindi
- [x] PaymentCompleted/Failed handler Booking'e testleriyle eklendi; Redis lock temizligi dogrulandi
- [x] Tam odeme dongusu calisiyor: Reserve -> Initiate -> Worker -> Complete -> Confirm
- [x] Tam iade dongusu calisiyor: Refund -> Worker -> Refunded -> Available

## 2026-05-15 Notification P10 Notu

- [x] Notification SSE tamamlandi
- [x] 4 event tipi implement edildi: seat_status_changed, your_turn, payment_confirmed, queue_position
- [x] SsePublisher entegrasyon event'lerini dinleyip Redis Pub/Sub kanallarina yayinliyor
- [x] QueuePositionPublisher eklendi
- [x] Ticket ve user SSE endpoint'leri eklendi; heartbeat SseSettings uzerinden okunuyor
- [x] Booking event contract'lari moduller arasi direkt referans olmamasi icin Core.Events altina tasindi
- [x] QueueDispatcher dogrudan Redis publish yerine QueueTurnGranted event'i yayinlayacak sekilde guncellendi

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-15 | Booking event contract'lari Core'a tasindi | Notification modulu Booking projesine direkt referans veremez; event contract'i shared kernel sinirinda tutuldu |
| 2026-05-15 | TicketConfirmed payment_confirmed yayinlamiyor | PaymentCompleted zaten payment_confirmed kaynagi; iki event'ten ayni bildirim uretmek client'a duplicate mesaj gonderirdi |

## 2026-05-17 CDC P11 Notu

- [x] CDC pipeline aktif hale getirildi.
- [x] Debezium Postgres connector `booking.tickets` ve `payment.payments` tablolarini `db.booking.tickets` ve `db.payment.payments` topic'lerine yaziyor.
- [x] Outbox schema CDC scope disinda birakildi; `ticketgate_pub` yalnizca booking ve payment schema tablolarini kapsiyor.
- [x] Debezium connector `decimal.handling.mode=double` ile numeric alanlari Elasticsearch float mapping'iyle uyumlu uretiyor.
- [x] Elasticsearch sink connector ayni Debezium Connect imajina Confluent Elasticsearch plugin'i eklenerek calistirildi.
- [x] `ticketgate-*` index template'i kaydedildi; `ticketgate-db.booking.tickets-2026.05` ve `ticketgate-db.payment.payments-2026.05` indexleri olustu.
- [x] TicketGate.API Serilog + Elasticsearch sink + Correlation ID middleware ile guncellendi.

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-17 | Debezium Connect imajina Elasticsearch sink plugin'i eklendi | `debezium/connect:2.6` Postgres CDC plugin'lerini tasiyor ama Confluent Elasticsearch sink sinifini icermiyor |
| 2026-05-17 | Sink index adi TimestampRouter ile ay bazli yapiliyor | Confluent Elasticsearch sink index adini topic'ten aliyor; resmi SMT yaklasimi date suffix icin `TimestampRouter` kullaniyor |
| 2026-05-17 | Debezium decimal handling double yapildi | Schemaless JSON'da decimal bytes base64 string'e donusuyordu ve Elasticsearch float mapping'i bulk insert'i reddediyordu |

## 2026-05-18 Prometheus + Grafana Notu

- [x] `prometheus-net` ve `prometheus-net.AspNetCore` paketleri eklendi.
- [x] TicketGate.API `/metrics` endpoint'i ve HTTP request metrics middleware'i eklendi.
- [x] `TicketGateMetrics` shared kernel altina alindi; moduller API projesine referans vermeden rezervasyon, Redis lock, waiting room, outbox, payment ve SSE metriklerini yaziyor.
- [x] Prometheus scrape config ve alert rules eklendi.
- [x] Grafana datasource, dashboard provisioning ve 9 panelli TicketGate dashboard eklendi.
- [x] docker-compose Prometheus ve Grafana servisleri ile guncellendi.

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-18 | TicketGateMetrics Core altinda tutuldu | Prompt API altina koysa da Booking/Payment/Notification modulleri API'ye referans veremez; shared kernel moduler monolith sinirini bozmadan ortak metrik sozlesmesi saglar |

## 2026-05-18 Docker Compose Production Notu

- [x] `infrastructure/docker/docker-compose.yml` base servis tanimlarina ayrildi; API container portu 5001 ic networkte expose ediliyor.
- [x] `infrastructure/docker/docker-compose.override.yml` development port publish ve local secret degerleri icin olusturuldu; `.gitignore` ile git disinda tutuluyor.
- [x] `infrastructure/docker/docker-compose.prod.yml` restart policy, resource limits ve healthcheck katmani olarak eklendi.
- [x] TicketGate.API icin `/health/live`, `/health/ready` ve `/health` endpointleri eklendi; readiness PostgreSQL, Redis, Kafka ve Elasticsearch kontrollerini kapsiyor.
- [x] API Dockerfile multi-stage publish + runtime curl kurulumu ile production healthcheck'e hazirlandi.
- [x] `.env.example` ve `infrastructure/scripts/migrate.sh` eklendi; production migration stratejisi CI/CD script'i uzerinden tutuluyor.

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-18 | Development override git disinda tutuldu | Lokal portlar ve development secret degerleri repo'ya production konfigu gibi commit edilmemeli; base/prod dosyalari deploy sozlesmesini tasir |
| 2026-05-18 | Health checks API projesinde extension olarak tutuldu | Endpointler host seviyesinde davranis oldugu icin module boundary bozmaz; Docker Compose ve izleme araclari ayni endpointleri kullanabilir |

## 2026-05-18 CI/CD GitHub Actions Notu

- [x] `.github/workflows/ci.yml` eklendi; main/master/develop push ve PR akislari icin restore, build, test ve migration check adimlarini calistiriyor.
- [x] CI migration check `--context` ve `--configuration Release` ile duzeltildi; Release build sonrasi EF CLI artik Debug output aramiyor.
- [x] `.github/workflows/cd.yml` roadmap olarak tutuldu; `workflow_dispatch` + `if: false` ile otomatik deploy devre disi.
- [x] `infrastructure/docker/docker-compose.yml` API image degeri `TICKETGATE_API_IMAGE` ile override edilebilir hale getirildi; CD server tarafinda SHA tag'li GHCR image'i kullanabiliyor.
- [x] `.github/branch-protection.md` master branch icin guncellendi; main branch notu kaldirildi.
- [x] `.github/pull_request_template.md` eklendi.
- [x] `.github/dependabot.yml` once eklendi; olusan Dependabot PR/kirmizi run kalabaligi nedeniyle silindi ve acik Dependabot PR'lari kapatildi.
- [x] CI GitHub'da master branch icin yesil calisti; CD eski aktif denemeden kalan kirmizi run disinda yeni deploy tetiklemeyecek.

| Tarih | Karar | Gerekce |
|-------|-------|---------|
| 2026-05-18 | Ana branch master kabul edildi | GitHub remote HEAD `master`; main branch bu repo icin kullanilmiyor |
| 2026-05-18 | CD workflow disabled roadmap olarak tutuldu | Production server/secrets hazir degil; repo'ya giren gelistirici CD yol haritasini gorsun ama deploy tetiklenmesin |
| 2026-05-18 | Dependabot devre disi birakildi | Ilk kurulumda cok sayida major upgrade PR'i ve kirmizi run olusturdu; temiz CI gorunurlugu tercih edildi |
| 2026-05-18 | Compose API image environment override ile yonetiliyor | Base compose local `ticketgate/api:latest` davranisini korurken production deploy GHCR SHA tag'li imaji cekebiliyor |
