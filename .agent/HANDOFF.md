# HANDOFF.md — Session Geçiş Şablonu

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
