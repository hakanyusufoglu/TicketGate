# CONTEXT.md — Aktif Session Durumu
# Her session başında oku. Session sonunda güncelle.

## Aktif Görev
P5 — Booking modülü

## Neden P5 Şimdi?
P4 Testcontainers altyapısı tamamlandı. Booking modülündeki xmin concurrency
ve Redis SETNX race condition testleri artık gerçek PostgreSQL ve Redis
container'ları üzerinde yazılabilir.

## P4 Son Durum
- [x] tests/TicketGate.TestInfrastructure projesi eklendi
- [x] IntegrationTestBase eklendi
- [x] Booking.Tests ve Payment.Tests Testcontainers altyapısına bağlandı
- [x] Booking integration smoke testleri gerçek PostgreSQL/Redis ile geçti
- [x] http-client.env.json baseUrl http://localhost:5001 yapıldı

## Sıradaki Prompt
P5 — Booking: Ticket + ReserveTicket + Redis Lock

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
