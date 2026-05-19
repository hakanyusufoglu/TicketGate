# CONTEXT.md â€” Aktif Session Durumu
# Her session baÅŸÄ±nda oku. Session sonunda gÃ¼ncelle.

## Aktif Görev
MediatR -> Mediator MIT migration tamamlandi. Aktif görev: README hazırlığı.

## Neden P7 Sonra?
P5 Booking Ticket + ReserveTicket + Redis Lock tamamlandÄ±. Booking modÃ¼lÃ¼nde
Ticket entity, xmin concurrency, Redis SETNX lock, Reserve/Confirm/Cancel
slicelarÄ±, query endpointleri, Init_Tickets migration ve integration testleri hazÄ±r.
Development seed data eklendi: Event modÃ¼lÃ¼ iÃ§in sabit Guid'li Venue, Performer
ve published Event kaydÄ± idempotent olarak oluÅŸturuluyor; ticket seed yok.

## P4 Son Durum
- [x] tests/TicketGate.TestInfrastructure projesi eklendi
- [x] IntegrationTestBase eklendi
- [x] Booking.Tests ve Payment.Tests Testcontainers altyapÄ±sÄ±na baÄŸlandÄ±
- [x] Booking integration smoke testleri gerÃ§ek PostgreSQL/Redis ile geÃ§ti
- [x] http-client.env.json baseUrl http://localhost:5001 yapÄ±ldÄ±

## Sıradaki Prompt
README hazırlığı

## Ã‡Ä±karÄ±lan Promptlar (ve neden)
- Ocelot Gateway â†’ monolith'te gereksiz; microservice'e geÃ§ince
- Serilog â†’ CDC ES zaten var; CDC kurulunca eklenecek
- Health Checks â†’ P14 production Docker'da eklenecek

## Dikkat Edilecekler
- Docker PG host portu: 55432
- Integration testleri docker-compose PostgreSQL 55432'yi kullanmaz; Testcontainers dinamik portlu izole PostgreSQL baÅŸlatÄ±r
- API dÄ±ÅŸarÄ±ya port expose etmez
- Her yeni sÄ±nÄ±fa TÃ¼rkÃ§e XML summary zorunlu
- Built-in RateLimiter (Ocelot yerine) â€” P17'de eklenecek
- Testcontainers 3.x restore/build aÅŸamasÄ±nda transitive NuGet gÃ¼venlik uyarÄ±larÄ± Ã¼retiyor; ileride paket versiyonu veya major upgrade deÄŸerlendirilmeli

## Son Tamamlanan Ara Gorev
MediatR bagimliligi Mediator MIT stack'ine tasindi. Library projeleri `Mediator.Abstractions`, API ve integration test host'lari `Mediator.SourceGenerator` kullaniyor. Endpointlerde `IMediator.Send`, handler/worker event akislarinda `IMediator.Publish` kullaniliyor. Tum handler'lar public sealed ve Mediator `ValueTask` imzasina uyumlu. `dotnet restore`, `dotnet build --no-restore` ve `dotnet test --no-build -m:1` basarili; commit atilmadi.

## Son Tamamlanan Ara Gorev
Seat map JSON yapisi typed Core SeatMap value object'ine tasindi. Event Venue jsonb persist'i converter ile guncellendi, Booking GenerateTickets slice'i eklendi ve POST /api/v1/events/{eventId}/tickets/generate endpoint'i Event seat map reader soyutlamasi uzerinden seat map okuyacak sekilde baglandi. Rezervasyon mekanizmasina dokunulmadi.

## Son Tamamlanan Ara Gorev
Magic number konfigurasyon refactor'u baslatildi. Booking Redis lock TTL, Jwt token sureleri, Outbox polling/batch/retry ve SSE heartbeat degerleri strongly-typed options ve appsettings uzerinden yonetilecek sekilde guncellendi. appsettings degerleri numeric tutuldu; kod tarafinda TimeSpan donusumu options degerlerinden yapiliyor.

## Son Tamamlanan Ara Gorev
TicketLockExpiredWorker tamamlandi. Redis keyspace expired event'i ticket lock anahtarlarini dinliyor, TTL dolunca Reserved ticket'i Available'a cekiyor ve startup crash recovery taramasi suresi gecmis Reserved ticket'lari temizliyor. Lock dongusu ReserveTicket ile baslayip Redis TTL expire sonrasi Postgres state cleanup ile tamamlandi.


## Son Tamamlanan Ara Gorev
Virtual Waiting Room tamamlandi. JoinQueue kapasite bosken active_checkout sayacini atomik Lua script ile artirip direkt gecis veriyor; kapasite doluyken Redis Sorted Set'e ZADD NX ile ekliyor ve ZRANK ile pozisyon donuyor. GetQueuePosition ve LeaveQueue slicelari eklendi. QueueDispatcher aktif waitingroom:* key'lerini tarayip kapasiteye gore ZPOPMIN + active_checkout artisini tek Lua script'inde yapiyor ve queue:{userId}:turn kanalina your_turn Pub/Sub mesaji yayinliyor.

## Son Tamamlanan Ara Gorev
Payment P8 tamamlandi. InitiatePayment handler idempotency key ile once mevcut payment response'unu donuyor, sonra ITicketReservationReader ile ticket Reserved ve UserId eslesmesini dogruluyor, Payment + OutboxMessage kaydini tek transaction'da yaziyor. Stripe/PayPal handler'da cagrilmiyor; MockPaymentGateway yalnizca P9 worker icin DI'a eklendi. Init_Payments migration uretildi; local database update PostgreSQL acildiktan sonra basariyla uygulandi; Testcontainers migration/test akisi de basarili.

## Son Tamamlanan Ara Gorev
Payment P9 tamamlandi. InitiatePayment artik UserId ve Amount'u request body'den almiyor; UserId JWT claim'den, tutar ticket price contract'inden okunuyor. OutboxWorker PaymentInitiated ve PaymentRefundRequested mesajlarini isliyor; basarili charge PaymentCompleted event'i, retry limiti asilan charge PaymentFailed event'i yayinliyor. Booking tarafinda PaymentCompleted/PaymentFailed handler'lari ticket state ve Redis lock cleanup akislarini domain event uzerinden tamamliyor.

## Son Tamamlanan Ara Gorev
Endpoint security refactor tamamlandi. Booking reserve/confirm/cancel, WaitingRoom join/position/leave ve Payment refund endpointleri UserId'yi body/query yerine JWT claim'den HttpContext.GetUserId() ile okuyor. ClaimExtensions Core'a eklendi; endpoint Swagger metadata eksikleri WithSummary, WithDescription ve Produces ile tamamlandi. Command ve handler sozlesmelerine dokunulmadi.

## Son Tamamlanan Ara Gorev
Iade akisi tamamlandi. Payment RefundPayment handler Completed payment icin refund outbox mesaji yaziyor, OutboxWorker refund gateway sonucunda Payment'i Refunded yapip PaymentRefunded event'i yayinliyor. Booking PaymentRefundedHandler bu event'i dinleyerek Confirmed ticket'i ReleaseAfterRefund() ile Available durumuna cekiyor ve TicketReleased event'i yayinliyor. CancelTicket organizator/admin iptali olarak Confirmed -> Cancelled kalir; Refund kullanici iade talebi olarak Confirmed -> Available yapar ve bilet tekrar satisa acilir.

## Son Tamamlanan Ara Gorev
OutboxWorker son kontrolu tamamlandi. MockPaymentGateway ayri dosyaya tasindi ve Stripe benzeri `mock_ch_{guid}` ExternalPaymentId uretiyor. Payment outbox payload record'lari worker payload klasoru altina alindi. PaymentCompleted ve PaymentFailed Booking handler integration testleri eklendi; Confirm/Release state gecisleri ve Redis lock cleanup dogrulandi. Siradaki aktif gorev P10 Notification: SSE + Redis Pub/Sub fan-out.

## Son Tamamlanan Ara Gorev
Notification P10 tamamlandi. SseEventTypes ve SseChannels contract'lari eklendi; SsePublisher TicketReserved, TicketReleased, TicketConfirmed, QueueTurnGranted, UserJoinedQueue ve PaymentCompleted event'lerini Redis Pub/Sub kanallarina yayinliyor. Ticket SSE stream'i seat_status_changed eventlerini, user SSE stream'i your_turn, queue_position ve payment_confirmed eventlerini dinliyor. Heartbeat SseSettings uzerinden okunuyor; Last-Event-ID sadece event id sayacini devam ettirir, Redis Pub/Sub gecmis mesaj replay etmez. Booking event contract'lari Core.Events altina tasindi ve QueueDispatcher dogrudan Redis publish yerine QueueTurnGranted event'i yayinlayacak sekilde guncellendi.

## Son Tamamlanan Ara Gorev
CDC P11 tamamlandi. Docker Compose tum servisleri calistiriyor; Debezium Connect imaji Elasticsearch sink plugin'i ile build ediliyor. Postgres `wal_level=logical`, `ticketgate_pub` publication'i booking/payment schema tablolarini kapsiyor ve outbox CDC disinda kaldi. Debezium source connector `db.booking.tickets` ve `db.payment.payments` topic'lerini uretiyor; sink connector TimestampRouter ile `ticketgate-db.*-yyyy.MM` indexlerine yaziyor. Serilog + Elasticsearch sink ve CorrelationId middleware TicketGate.API'ye eklendi.

## Son Tamamlanan Ara Gorev
Prometheus + Grafana tamamlandi. TicketGate.API `UseHttpMetrics()` ve `MapMetrics()` ile `/metrics` endpoint'i sunuyor. `TicketGateMetrics` shared kernel altinda tutuldu; Booking rezervasyon/lock/waiting room, Payment outbox/payment/dead-letter ve Notification SSE baglanti metriklerini Prometheus'a yaziyor. Prometheus scrape config, alert rules, Grafana datasource/dashboard provisioning ve 9 panelli TicketGate dashboard eklendi. Docker Compose Prometheus ve Grafana servisleriyle guncellendi.

## Son Tamamlanan Ara Gorev
Docker Compose Production tamamlandi. `infrastructure/docker/docker-compose.yml` base servis tanimlarina ayrildi, `docker-compose.prod.yml` restart/resource/healthcheck katmani olarak eklendi ve `docker-compose.override.yml` development portlari icin olusturulup git disinda tutuldu. TicketGate.API `/health/live`, `/health/ready` ve `/health` endpointlerini sunuyor; readiness PostgreSQL, Redis, Kafka ve Elasticsearch kontrollerini kapsiyor. API Dockerfile curl destekli runtime image'a guncellendi, `.env.example` ve `infrastructure/scripts/migrate.sh` eklendi. Commit atilmadi.

## Son Tamamlanan Ara Gorev
CI/CD GitHub Actions kurulumu son duruma hizalandi. `.github/workflows/ci.yml` master/main/develop push ve PR akislari icin restore, build, test ve migration check adimlarini calistiriyor; migration check `--context` ve `--configuration Release` kullaniyor. Repo ana branch'i master olarak kabul edildi; main branch bu proje icin onemli degil. `.github/workflows/cd.yml` dosyasi silinmedi, roadmap olarak tutuldu; `workflow_dispatch` ve `if: false` ile otomatik deploy devre disi, ileride SERVER_HOST/SERVER_USER/SERVER_SSH_KEY ve production server hazirlaninca doldurulacak. Dependabot PR kalabaligi ve kirmizi run'lar nedeniyle `.github/dependabot.yml` silindi, acik Dependabot PR'lari kapatildi. Son CI run'lari master icin yesil calisiyor; eski kirmizi CD/Dependabot run'lari sadece gecmis kaydi.

## Son Tamamlanan Ara Gorev
Security hardening ve built-in RateLimiter tamamlandi. TicketGate.API host seviyesinde CORS, RateLimiter, global validation options ve SecurityHeadersMiddleware eklendi; middleware sirasi `UseRouting -> SecurityHeaders -> CorrelationId -> CORS -> Authentication -> Authorization -> RateLimiter -> endpoints` olarak merkezi hale getirildi. Rate limit policy'leri IP bazli fixed-window olarak appsettings `RateLimiting` ayarlarindan okunuyor ve endpoint metadata'si Core `RateLimitPolicies` sabitleriyle baglaniyor. JWT validation clock skew sifira indirildi; API security testleri rate limit 429, security headers ve development CORS davranisini dogruluyor.

## Son Tamamlanan Ara Gorev
Performance optimizasyonu tamamlandi. Tum handler'lar EF Core tracking ve Include kullanimi acisindan tarandi; query handler'lar AsNoTracking/projection-first durumda, command handler'daki InitiatePayment AsNoTracking kullanimi kaldirildi. Event modulu icin Redis cache-aside servisi eklendi; GetEventById cache hit/miss akisi, UpdateEvent/PublishEvent invalidation ve PublishEvent output cache tag evict davranisi test edildi. TicketGate.API response compression ve output cache middleware'leriyle guncellendi; event listesi 60sn output cache policy'sine baglandi ve cache/pool sureleri appsettings uzerinden okunuyor.

## Son Tamamlanan Ara Gorev
Smoke Test + E2E `.http` senaryolari tamamlandi. `src/TicketGate.API/Http/e2e.http` Register, Login, seed Event, ticket generate, seat listesi, reserve, payment, OutboxWorker sonrasi Confirmed, refund sonrasi Available, tekrar reserve, race condition, waiting room, auth/rate-limit ve duplicate idempotency key akislarini sirali response chaining ile belgeliyor. Her adimda Turkce yorum ve beklenen HTTP sonucu var. Opsiyonel xUnit E2E projesi ve CI adimi eklenmedi; bu prompt icin VS/Rider `.http` destegi yeterli kabul edildi.


