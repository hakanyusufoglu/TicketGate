# CONTEXT.md — Aktif Session Durumu
# Her session başında oku. Session sonunda güncelle.
# Geçmiş kararlar için MEMORY.md'ye bak.

## Aktif Görev

**Prompt 2.5 — Identity Uçtan Uca Test**
Codex'te işleniyor. Çıktı bekleniyor.

## Prompt 2.5 Checklist

- [ ] `docker compose up -d postgres redis` çalışıyor
- [ ] Identity migration oluşturuldu (`Init_Identity`)
- [ ] `dotnet ef database update` çalıştırıldı
- [ ] `identity.users` ve `identity.refresh_tokens` tabloları oluştu
- [ ] Swagger JWT desteğiyle açılıyor (`/swagger`)
- [ ] `src/TicketGate.API/Http/identity.http` oluşturuldu
- [ ] `src/TicketGate.API/Http/http-client.env.json` oluşturuldu
- [ ] Register → 201 ✅
- [ ] Login → 200 + token ✅
- [ ] Refresh → 200 + yeni token ✅
- [ ] Duplicate email → 409 ✅
- [ ] Yanlış şifre → 401 ✅
- [ ] README güncellendi

## Sıradaki Prompt (2.5 tamamlandıktan sonra)

**Prompt 3 — Event Modülü**

Entities: Event, Venue, Performer
Features:
- CreateEvent (Command)
- UpdateEvent (Command)
- PublishEvent (Command)
- GetEventById (Query)
- GetEventList (Query — pagination + search)
- CreateVenue (Command)
- GetVenueById (Query)

## Aktif Dosyalar (bu session'da değiştirilen)

```
src/TicketGate.API/Program.cs
src/TicketGate.API/appsettings.Development.json
src/TicketGate.API/Http/identity.http          ← yeni
src/TicketGate.API/Http/http-client.env.json   ← yeni
src/Modules/TicketGate.Identity/**             ← tamamlandı
infrastructure/docker/docker-compose.yml
infrastructure/postgres/init.sql
README.md
```

## Bağlam Notları

- Program.cs sadece `AddModules(config)` + `MapModules()` içermeli
- Her modül `IModule` implement ederek kendini register eder
- Swagger sadece Development ortamında aktif
- `.http` dosyalarında hem happy path hem hata senaryoları olmalı
- Migration komutu: `--output-dir Infrastructure/Persistence/Migrations`
