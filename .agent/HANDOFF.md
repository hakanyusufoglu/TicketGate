## SON HANDOFF � 2026-05-14 Virtual Waiting Room

### Proje
TicketGate � bilet satis platformu
.NET 10 � Moduler Monolith � Vertical Slice Architecture

### Bu Session'da Yapilanlar
- Virtual Waiting Room slicelari eklendi: JoinQueue, GetQueuePosition, LeaveQueue.
- JoinQueue kapasite bosken active_checkout:{eventId} sayacini Lua script ile atomik artirip Position=0 direct grant donuyor.
- Kapasite doluyken waitingroom:{eventId} Sorted Set'ine ZADD NX ile ekliyor; ayni kullanicinin pozisyonu korunuyor.
- QueueDispatcher eklendi ve BookingModule icinde hosted service olarak kaydedildi.
- Dispatcher waitingroom:* key'lerini tarayip Lua script ile kapasiteye gore ZPOPMIN + active_checkout INCR islemini atomik yapiyor.
- Redis Pub/Sub queue:{userId}:turn kanalina your_turn payload'i yayinlaniyor; SSE P10'da bu kanali dinleyecek.
- QueueTurnGranted ve UserJoinedQueue domain eventleri eklendi; source event id alani DomainEvent.EventId ile cakismamasi icin SourceEventId kullanildi.
- src/TicketGate.API/Http/waitingroom.http eklendi.
- WaitingRoom integration testleri eklendi; real Redis Sorted Set, NX, Pub/Sub ve concurrency kapasite davranisi dogrulandi.

### Dogrulama
- dotnet build TicketGate.sln --no-restore -v minimal: basarili, mevcut NuGet guvenlik uyarilari devam ediyor.
- dotnet test tests/TicketGate.Booking.Tests/TicketGate.Booking.Tests.csproj --no-build -v normal: 21/21 basarili.
- Bir onceki tum test kosusunda Docker/Testcontainers initialize timeout flake'i goruldu; ayni test izole ve sonraki tam kosuda gecti.

### Dikkat
- Commit atilmadi; kullanici son talimatta commit istemedi.
- active_checkout:{eventId} sayacini odeme/checkout tamamlaninca azaltacak akis P8/P9 tarafinda netlestirilmeli. Aksi halde kapasite kalici dolu kalir.
- Redis server.Keys waitingroom:* taramasi mevcut faz icin kabul edilebilir; production yuksek trafik icin aktif event set'i veya SCAN temelli explicit registry daha saglam olur.

### Siradaki Gorev
P8 � Payment: InitiatePayment + Outbox + Idempotency

---## SON HANDOFF � 2026-05-14 TicketLockExpiredWorker

### Proje
TicketGate � bilet satis platformu
.NET 10 � Moduler Monolith � Vertical Slice Architecture

### Bu Session'da Yapilanlar
- TicketLockExpiredWorker eklendi.
- Worker Redis __keyevent@0__:expired kanalini dinliyor ve yalnizca ticket:{id}:lock formatindaki expired key'leri isliyor.
- TTL expire olunca ilgili Reserved ticket Release() ile Available durumuna aliniyor.
- Startup crash recovery taramasi LockTtlSeconds suresini asmis Reserved ticket'lari temizliyor.
- BookingModule icinde AddHostedService<TicketLockExpiredWorker>() kaydi yapildi.
- TicketLockExpiredWorker integration testleri eklendi: Redis keyspace notification ve startup recovery dogrulandi.
- booking.http icine TTL sonrasi status kontrol senaryosu eklendi.

### Dikkat
- Redis notify-keyspace-events = KEx docker-compose ve Testcontainers Redis command'inde aktif.
- TicketReleased event contract'i mevcut haliyle UserId tasiyor; worker release oncesi LockedByUserId degerini yakalayip publish ediyor.
- Seat bilgisinin SSE icin event'e eklenmesi istenirse TicketReleased contract'i ayri ve bilincli bir degisiklikle genisletilmeli.

### Siradaki Gorev
P7 � Booking Virtual Waiting Room

---
## SON HANDOFF � 2026-05-14 Configuration Refactor

### Proje
TicketGate � bilet satis platformu
.NET 10 � Moduler Monolith � Vertical Slice Architecture

### Bu Session'da Yapilanlar
- AGENTS.md'deki yeni yapilandirma kurali okundu: runtime magic number degerleri appsettings + strongly-typed options uzerinden yonetilecek.
- BookingSettings eklendi; ReserveTicketHandler Redis lock TTL'i BookingSettings:LockTtlSeconds degerinden okuyor.
- JwtSettings eklendi; access token suresi, refresh token suresi ve clock skew config'e tasindi.
- OutboxSettings ve SseSettings options siniflari eklendi; Payment ve Notification modulleri ilgili options kayitlarini yapiyor.
- appsettings.json ve appsettings.Development.json BookingSettings, OutboxSettings, SseSettings ve Jwt sure alanlariyla guncellendi.
- ReserveTicketHandler XML summary sabit TTL ifadesi yerine BookingSettings kaynakli TTL'i anlatacak sekilde duzeltildi.

### Dikkat
- appsettings degerleri TimeSpan string formatinda degil; numeric seconds/minutes/days olarak kaldi.
- Kod tarafinda TimeSpan.FromSeconds/FromMinutes appsettings options degerleriyle kullaniliyor; sabit 10/15/2 verilmedi.
- Test verisi, seed koltuk numaralari, package versionlari ve migration metadata magic number kapsaminda degil.

### Siradaki Gorev
P6 � Booking Virtual Waiting Room

---
## SON HANDOFF � 2026-05-14 SeatMap + GenerateTickets

### Proje
TicketGate � bilet satis platformu
.NET 10 � Moduler Monolith � Vertical Slice Architecture

### Bu Session'da Yapilanlar
- SeatMap Core ortak value object olarak eklendi: Section/Row/Seat hiyerarsisi, TotalCapacity ve GetPrice.
- Venue entity string SeatMap yerine typed SeatMap kullanacak sekilde guncellendi; jsonb conversion korundu.
- Event modulu IEventSeatMapReader implementasyonu ekledi; Booking endpoint'i Event modulu DB tiplerine direkt referans almiyor.
- Booking GenerateTickets command/response/validator/handler slice'i eklendi.
- POST /api/v1/events/{eventId}/tickets/generate endpoint'i eklendi.
- GetAvailableSeats SeatDto artik SeatCode, Section, Row, SeatNumber ve Price donuyor.
- Seed venue 50 koltuklu VIP/NORMAL/EKONOMI SeatMap formatina guncellendi.
- Update_Venue_SeatMap migration olusturuldu ve database update calistirildi.
- GenerateTickets handler integration testleri eklendi.

### Biten Gorev
Seat map refactor + GenerateTickets slice.

### Dikkat
- Migration schema olarak bos; kolon zaten jsonb oldugu icin degisen taraf CLR model/converter.
- GenerateTickets tekrar cagrida mevcut ticket kontroluyle 409 donuyor. Yuksek concurrency icin ileride EventId+Seat unique constraint ve DbUpdateException mapping eklemek daha saglam olur.
- Core'a SeatMap eklemek AGENTS'taki "Core'a domain kodu ekleme" kuralina normalde ters; bu session'da kullanici kesin kural olarak SeatMap'in Core'a tasinmasini istedi.

### Siradaki Gorev
P6 � Booking Virtual Waiting Room

---
# HANDOFF.md — Session Geçiş Şablonu

---

## SON HANDOFF — 2026-05-14 Seed

### Proje
TicketGate — bilet satış platformu
.NET 10 · Modüler Monolith · Vertical Slice Architecture

### Bu Session'da Yapılanlar
- Development ortamı için Event modülü seed data eklendi
- SeedGuids ile sabit Venue, Performer ve Event Guid'leri tanımlandı
- SeedDataService idempotent şekilde Venue, Performer ve published Event oluşturacak şekilde eklendi
- Program.cs Development ortamında seed çağıracak şekilde güncellendi
- http-client.env.json sabit seed değişkenleriyle güncellendi
- event.http response chaining kullanmayacak şekilde sabit Guid değişkenlerine geçirildi

### Biten Görev
Development seed data — Venue + Performer + Event

### Dikkat
- Ticket seed eklenmedi; ticket'lar manuel oluşturulacak.
- Event entity factory sabit Guid overload'u sunmadığı için seed servisinde EF Core change tracker üzerinden Id sabitleniyor.
- API run doğrulamasında seed kayıtları zaten bulunduğunda oluşturuldu logları görünmez; bu idempotent davranıştır.

### Sıradaki Görev
P6 — Booking Virtual Waiting Room

---

## SON HANDOFF — 2026-05-14 P5

### Proje
TicketGate — bilet satış platformu
.NET 10 · Modüler Monolith · Vertical Slice Architecture

### Bu Session'da Yapılanlar
- TicketGate.Booking P5 implement edildi: Ticket entity, TicketStatus enum, Booking domain eventleri
- BookingDbContext, TicketConfiguration, booking schema ve Init_Tickets migration eklendi
- ReserveTicket, ConfirmTicket, CancelTicket command sliceları eklendi
- GetTicketById ve GetAvailableSeats query sliceları projection-first olarak eklendi
- TicketEndpoints ve BookingModule servis/endpoint kayıtları tamamlandı
- BookingIntegrationTestBase gerçek PostgreSQL/Redis, MediatR, validator ve migration çalıştıracak şekilde dolduruldu
- ReserveTicket integration testleri eklendi; Redis SETNX race condition ve lock cleanup doğrulandı
- booking.http eklendi

### Biten Görev
P5 — Booking: Ticket + ReserveTicket + Redis Lock

### Dikkat
- Npgsql EF Core 10'da eski UseXminAsConcurrencyToken extension'ı yok; aynı isimli local extension shadow xmin row-version mapping yapıyor.
- Testcontainers testleri assembly seviyesinde seri çalışıyor; paralel çalıştırmak Docker readiness timeout üretebiliyor.
- NuGet restore sırasında Testcontainers transitive paketlerinden mevcut güvenlik uyarıları gelmeye devam ediyor.

### Sıradaki Görev
P6 — Booking Virtual Waiting Room

---

## SON HANDOFF — 2026-05-14

### Proje
TicketGate — bilet satış platformu
.NET 10 · Modüler Monolith · Vertical Slice Architecture
Repo: github.com/[kullanici]/TicketGate

### Bu Session'da Yapılanlar
- P4 Testcontainers altyapısı tamamlandı
- tests/TicketGate.TestInfrastructure projesi eklendi ve solution'a bağlandı
- IntegrationTestBase eklendi: PostgreSQL 16, Redis 7, Respawn reset, schema hazırlığı
- Booking.Tests ve Payment.Tests Testcontainers paketleri + ProjectReference ile ortak altyapıya bağlandı
- BookingIntegrationTestBase ve PaymentIntegrationTestBase eklendi
- Booking integration smoke testleri eklendi: PostgreSQL schema erişimi, Redis SET NX ve FLUSHDB reset davranışı
- http-client.env.json baseUrl http://localhost:5001 yapıldı

### Biten Görev
P4 — Testcontainers altyapısı

### Yarım Kalan / Dikkat
- Event modülü commit edilmedi
- AddOpenBehavior her modülde tekrar kaydediliyor — Gateway promptunda merkezi yapılacak
- Docker PG host portu: 55432
- Integration testleri 55432'yi kullanmaz; Testcontainers izole PostgreSQL/Redis container başlatır
- Testcontainers 3.x transitive paketlerinde NuGet güvenlik uyarıları var

### Sıradaki Görev
P5 — Booking modülü: Ticket + ReserveTicket + Redis Lock

### Yeni Session Başlangıç Komutu
```
Aşağıdaki dosyaları sırayla oku, 3-4 cümleyle özetle, sonra göreve geç:
1. AGENTS.md
2. .agent/MEMORY.md
3. .agent/CONTEXT.md
```

---

## HANDOFF KULLANIM REHBERİ

### Ne zaman üretilir?
- Token limiti %60-70'e geldiğinde
- Araç değiştirirken
- Günlük çalışma bitişinde
- Bir prompt tamamlandığında

### Session sonu komutu (Codex'e söyle)
```
Bu session'ı bitiriyoruz.
1. .agent/MEMORY.md → tamamlananlar ve yeni kararları ekle
2. .agent/CONTEXT.md → aktif görevi ve sıradaki adımı güncelle
3. .agent/HANDOFF.md → bu session özetini yaz
```

### Yeni session başlangıcı (tüm araçlar)
```
Aşağıdaki dosyaları sırayla oku, özetle, devam et:
1. AGENTS.md
2. .agent/MEMORY.md
3. .agent/CONTEXT.md
```

### Araç notları
- Codex CLI: AGENTS.md otomatik okunur, .agent/ dosyalarını ilk mesajda ver
- Claude Code: CLAUDE.md → .agent/ dosyalarına referans ver
- Cursor: .cursorrules → aynı yönlendirme
- Web arayüzleri: HANDOFF.md içeriğini ilk mesaj olarak yapıştır



