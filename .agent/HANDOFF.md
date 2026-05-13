# HANDOFF.md — Session Geçiş Şablonu
# Bu dosyayı her session sonunda Codex/Claude'a şunu söyleyerek ürettir:
# "Bu session'ı bitiriyoruz. HANDOFF.md'yi güncelle, MEMORY.md ve CONTEXT.md'yi yaz."
# Sonra yeni tool'da ilk mesaj olarak bu dosyanın içeriğini yapıştır.

---

## SON HANDOFF — [TARİH GİRİLECEK]

### Proje
TicketGate — bilet satış platformu
.NET 10 · Modüler Monolith · Vertical Slice Architecture
Repo: github.com/[kullanici]/TicketGate

### Bu Session'da Yapılanlar
<!-- Codex buraya dolduracak -->
- 

### Biten Görev
<!-- Hangi prompt tamamlandı -->

### Yarım Kalan / Dikkat
<!-- Tamamlanmayan, bilinmesi gereken -->

### Sıradaki Görev
<!-- Hangi prompt, ne yapılacak -->

### Yeni Session Başlangıç Komutu
```
Aşağıdaki dosyaları sırayla oku, sonra göreve başla:
1. AGENTS.md          → mimari kurallar ve yasaklar
2. .agent/MEMORY.md   → tamamlanan modüller ve kararlar  
3. .agent/CONTEXT.md  → aktif görev ve sıradaki adım

Okuduğunu kısaca özetle, sonra şunu yap: [GÖREV]
```

---

## HANDOFF KULLANIM REHBERİ

### Ne zaman üretilir?
- Token limiti %60-70'e geldiğinde
- Araç değiştirirken (Codex → Claude Code vb.)
- Günlük çalışma bitişinde
- Bir prompt tamamlandığında

### Codex'e söylenecek komut (session sonu)
```
Bu session'ı bitiriyoruz.
1. .agent/MEMORY.md → tamamlanan modüller, yeni kararlar ekle
2. .agent/CONTEXT.md → aktif görevi ve sıradaki adımı güncelle  
3. .agent/HANDOFF.md → bu session'ın özetini yaz
```

### Yeni session başlangıç komutu (evrensel — tüm araçlar)
```
TicketGate projesine devam ediyoruz.
Önce şu dosyaları oku:
- AGENTS.md
- .agent/MEMORY.md
- .agent/CONTEXT.md

Okuduğunu özetle ve [GÖREV] yap.
```

### Araç bazlı notlar

**Codex CLI:**
- `AGENTS.md` otomatik okunur
- Session başında `.agent/` dosyalarını ilk mesajda ver

**Claude Code:**
- `CLAUDE.md` oluştur, içine: "Read .agent/MEMORY.md and .agent/CONTEXT.md at session start"
- `--continue` ile son session'a dön

**Cursor:**
- `.cursorrules` içine aynı yönlendirmeyi ekle

**Web (claude.ai, chatgpt vb.):**
- HANDOFF.md içeriğini ilk mesaj olarak yapıştır
