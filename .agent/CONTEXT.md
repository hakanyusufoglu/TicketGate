# CONTEXT.md — Aktif Session Durumu
# Her session başında oku. Session sonunda güncelle.

## Aktif Görev
Yeni P3 — TicketGate.Gateway (Ocelot) implementasyonu

## Mevcut Durum
Pre-production check tamamlandı.
Build ve test temiz:
- `dotnet build TicketGate.sln --no-restore -v minimal` → 0 hata, 0 warning
- `dotnet test TicketGate.sln --no-build` → 27 test geçti

AGENTS.md kritik ihlalleri düzeltildi:
- `AddOpenBehavior(typeof(ValidationBehavior<,>))` tek merkezi kayda taşındı: `TicketGate.Core/Extensions/ModuleExtensions.cs`
- Booking, Payment, Notification modüllerine `IModule` implementasyonu eklendi
- Identity/Event runtime schema literal'leri schema const üzerinden kullanılıyor

Check sonucu production'a geçiş için temiz. Gateway yeni P3 olarak devam edecek.

## Neden Gateway Önce?
Foundation katmanı (Gateway, Serilog, Health Checks, Testcontainers) modül kodlarından önce gelmeli.
Bu katman olmadan yazılan kod production'da eksik kalır — logging yok, rate limit yok, integration test yok.

## Sıradaki Adımlar
1. TicketGate.Gateway projesi oluştur (ayrı .csproj)
2. Ocelot + Polly NuGet paketleri
3. ocelot.json — tüm mevcut route'lar (identity, event)
4. Rate limiting — endpoint bazlı kurallar
5. JWT validation Gateway'de
6. Load balancing config (RoundRobin, replicas:1, scale için hazır)
7. Circuit breaker (Polly)
8. docker-compose güncelle:
   - Gateway: port 5000 dışarıya açık
   - API: port 5001 sadece iç network, expose etme
9. event.http ve identity.http URL'lerini Gateway üzerinden güncelle

## Tamamlanmış (bu session öncesi)
- P1 ✅ P2 ✅ P2.5 ✅ P3(Event) ✅
- Event modülü commit edilmedi — Gateway ile birlikte commit atılacak

## Dikkat Edilecekler
- AddOpenBehavior merkezi kayıtta — modüllerde tekrar etme
- Docker PostgreSQL host portu: 55432
- API hiçbir zaman dışarıya port expose etmez
- Şimdilik tek instance, scale config ocelot.json'da hazır ama kapalı

## Aktif Dosyalar (bu promptta değişecekler)
```
src/TicketGate.Gateway/              ← yeni proje
  Program.cs
  ocelot.json
  TicketGate.Gateway.csproj
infrastructure/docker/docker-compose.yml  ← gateway eklenir, API port kaldırılır
src/TicketGate.API/Http/identity.http     ← URL güncellenir
src/TicketGate.API/Http/event.http        ← URL güncellenir
TicketGate.sln                            ← yeni proje eklenir
```
