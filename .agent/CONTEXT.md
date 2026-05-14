# CONTEXT.md — Aktif Session Durumu
# Her session başında oku. Session sonunda güncelle.

## Aktif Görev
P7 � Booking Virtual Waiting Room

## Neden P7 Sonra?
P5 Booking Ticket + ReserveTicket + Redis Lock tamamlandı. Booking modülünde
Ticket entity, xmin concurrency, Redis SETNX lock, Reserve/Confirm/Cancel
sliceları, query endpointleri, Init_Tickets migration ve integration testleri hazır.
Development seed data eklendi: Event modülü için sabit Guid'li Venue, Performer
ve published Event kaydı idempotent olarak oluşturuluyor; ticket seed yok.

## P4 Son Durum
- [x] tests/TicketGate.TestInfrastructure projesi eklendi
- [x] IntegrationTestBase eklendi
- [x] Booking.Tests ve Payment.Tests Testcontainers altyapısına bağlandı
- [x] Booking integration smoke testleri gerçek PostgreSQL/Redis ile geçti
- [x] http-client.env.json baseUrl http://localhost:5001 yapıldı

## Sıradaki Prompt
P7 � Booking: Virtual Waiting Room

## Çıkarılan Promptlar (ve neden)
- Ocelot Gateway → monolith'te gereksiz; microservice'e geçince
- Serilog → CDC ES zaten var; CDC kurulunca eklenecek
- Health Checks → P14 production Docker'da eklenecek

## Dikkat Edilecekler
- Docker PG host portu: 55432
- Integration testleri docker-compose PostgreSQL 55432'yi kullanmaz; Testcontainers dinamik portlu izole PostgreSQL başlatır
- API dışarıya port expose etmez
- Her yeni sınıfa Türkçe XML summary zorunlu
- Built-in RateLimiter (Ocelot yerine) — P17'de eklenecek
- Testcontainers 3.x restore/build aşamasında transitive NuGet güvenlik uyarıları üretiyor; ileride paket versiyonu veya major upgrade değerlendirilmeli

## Son Tamamlanan Ara Gorev
Seat map JSON yapisi typed Core SeatMap value object'ine tasindi. Event Venue jsonb persist'i converter ile guncellendi, Booking GenerateTickets slice'i eklendi ve POST /api/v1/events/{eventId}/tickets/generate endpoint'i Event seat map reader soyutlamasi uzerinden seat map okuyacak sekilde baglandi. Rezervasyon mekanizmasina dokunulmadi.

## Son Tamamlanan Ara Gorev
Magic number konfigurasyon refactor'u baslatildi. Booking Redis lock TTL, Jwt token sureleri, Outbox polling/batch/retry ve SSE heartbeat degerleri strongly-typed options ve appsettings uzerinden yonetilecek sekilde guncellendi. appsettings degerleri numeric tutuldu; kod tarafinda TimeSpan donusumu options degerlerinden yapiliyor.

## Son Tamamlanan Ara Gorev
TicketLockExpiredWorker tamamlandi. Redis keyspace expired event'i ticket lock anahtarlarini dinliyor, TTL dolunca Reserved ticket'i Available'a cekiyor ve startup crash recovery taramasi suresi gecmis Reserved ticket'lari temizliyor. Lock dongusu ReserveTicket ile baslayip Redis TTL expire sonrasi Postgres state cleanup ile tamamlandi.

