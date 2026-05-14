# HANDOFF.md — Session Geçiş Şablonu

---

## SON HANDOFF — 2026-05-14

### Proje
TicketGate — bilet satış platformu
.NET 10 · Modüler Monolith · Vertical Slice Architecture
Repo: github.com/[kullanici]/TicketGate

### Bu Session'da Yapılanlar
- Pre-production check çalıştırıldı
- İlk build/test temizdi: 0 hata, 0 warning; 27 test geçti
- AGENTS.md kural taramaları yapıldı
- Duplicate `AddOpenBehavior` ihlali düzeltildi
- MediatR validation pipeline merkezi `ModuleExtensions` kaydına taşındı
- Booking, Payment, Notification için `IModule` implementasyonları eklendi
- Identity/Event runtime schema adları const üzerinden kullanılacak şekilde düzeltildi
- Son build/test tekrar temiz geçti
- .agent/ dosyaları güncellendi

### Biten Görev
Pre-production check ve kritik AGENTS.md ihlal düzeltmeleri

### Yarım Kalan / Dikkat
- Event modülü commit edilmedi
- AddOpenBehavior merkezi kayıtta; modüllerde tekrar edilmemeli
- Magic string grep'i EF migration, route ve table adlarını da yakalıyor; runtime schema kullanımı const'a taşındı
- Docker PG host portu: 55432

### Sıradaki Görev
Yeni P3 (eski P4) — TicketGate.Gateway (Ocelot)

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
