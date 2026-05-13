# CONTEXT.md - Aktif Session Durumu

Her session basinda AGENTS.md ve MEMORY.md ile birlikte okunur.

## Aktif Gorev

Prompt 2.5 tamamlandi. Ilk commit atildi ve `origin/main` branch'ine push edildi.

## Son Yapilanlar

- Docker PostgreSQL host portu `55432` olarak degistirildi.
- `appsettings.Development.json` connection string portlari `55432` olarak guncellendi.
- Identity migration olusturuldu ve uygulandi.
- `identity.users` ve `identity.refresh_tokens` tablolari dogrulandi.
- Email ve refresh token unique indexleri dogrulandi.
- Swagger Development ortaminda JWT Bearer destegiyle aciliyor.
- `src/TicketGate.API/Http/identity.http` ve `http-client.env.json` olusturuldu.
- README baslangic adimlari guncellendi.

## Prompt 2.5 Kontrol Sonuclari

- [x] `docker compose -f infrastructure/docker/docker-compose.yml up -d postgres redis` calisiyor
- [x] Identity migration olusturuldu (`Init_Identity`)
- [x] `dotnet ef database update` host uzerinden calisiyor
- [x] `identity.users` ve `identity.refresh_tokens` tablolari olustu
- [x] `ix_users_email` unique index olustu
- [x] `ix_refresh_tokens_token` unique index olustu
- [x] Swagger JWT destegiyle aciliyor (`/swagger`)
- [x] `src/TicketGate.API/Http/identity.http` olusturuldu
- [x] `src/TicketGate.API/Http/http-client.env.json` olusturuldu
- [x] README guncellendi
- [x] `dotnet build TicketGate.sln` basarili
- [x] `dotnet test TicketGate.sln --no-build` basarili

## Siradaki Adim

Prompt 2.5 ciktisini kontrol et. Ardindan Prompt 3 - TicketGate.Event modulu implementasyonuna gec.

## Siradaki Prompt

Prompt 3 - Event Modulu

Beklenen kapsam:
- Event, Venue, Performer entity'leri
- CreateEvent, UpdateEvent, PublishEvent command'lari
- GetEventById, GetEventList query'leri
- CreateVenue, GetVenueById
- Event module DbContext, migration, endpoints ve testler

## Dikkat

- Program.cs sadece `AddModules(builder.Configuration)`, `Build`, `MapModules`, `Run` icermeli.
- Yeni module kayitlari ilgili `IModule` implementasyonu icinde yapilmali.
- Query handler'lara validator eklenmemeli.
- Exception ile beklenen hata yonetimi yapilmamali; `Result<T>` donmeli.
- Docker PostgreSQL icin host portu `55432`, container portu `5432`.
