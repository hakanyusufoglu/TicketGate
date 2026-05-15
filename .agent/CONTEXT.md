# CONTEXT.md â€” Aktif Session Durumu
# Her session baÅŸÄ±nda oku. Session sonunda gÃ¼ncelle.

## Aktif GÃ¶rev
P10 — Notification SSE

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

## SÄ±radaki Prompt
P10 — Notification: SSE + Redis Pub/Sub fan-out

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


