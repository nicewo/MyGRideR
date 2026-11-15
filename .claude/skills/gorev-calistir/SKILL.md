---
name: gorev-calistir
description: docs/TASKS.md'deki sıradaki (ya da numarası verilen) bekleyen görevi uygular, doğrular, TASKS.md'yi günceller, commit atar ve push eder. "sıradaki görev", "görev 7'yi yap", "görevlere devam" denince kullan.
---

# Görev çalıştır

## 1. Görevi seç
- Numara verildiyse o görev. Verilmediyse TASKS.md'deki **Uygulama sırası** satırına göre durumu `bekliyor` olan ilk görev.
- Durumu `kullanıcı (cihaz)` olan veya "Opus planı gerekli" yazan görevi atla ve bunu söyle; bunlar bu skill'le yapılmaz.
- Sadece TASKS.md'nin ilgili `### <no> —` bölümünü oku (Grep ile başlığı bul, o aralığı oku). Tüm dosyayı okuma.

## 2. Uygula
- Bölümde yazan dosya yollarına, kararlara ve kapsama birebir uy. Tasarım kararı değiştirme, kapsam dışına çıkma.
- Dosyaları baştan sona okuma; Grep ile ilgili yeri bul, gereken satır aralığını oku.
- Bölümdeki test listesini yaz.
- TASKS.md'de durumu `sürüyor` yap.

## 3. Doğrula
- `dogrula` skill'ini çalıştır.
- Geçmezse düzelt; 3 denemede geçmezse: durum `sürüyor` kalır, nedeni `## Notlar` altına `- #<no>: <kısa neden>` olarak yaz, commit atma, dur.

## 4. Kaydet
Geçtiyse:
1. TASKS.md tablo satırında durum `bitti`, Doğrulama sütunu `N test geçti, commit <kısa-hash>` (hash'i commit sonrası yaz, aynı commit'e `--amend` ile ekle).
2. Commit mesajı: `fix:`/`feat:` + görev başlığı, gövdede `Task #<no>`.
3. Sormadan: `git add -A && git commit` ve `git push`.
4. Push reddedilirse: `git pull --rebase` sonra tekrar push. Çakışma çıkarsa dur ve raporla.

## 5. Rapor
Tek satır: `#<no> bitti – <commit hash> – X test geçti` ya da `#<no> durdu – <neden>`.
Ardından tek görev yapıldıysa dur; kullanıcı "devam" ya da "hepsini" dediyse sıradakine geç ama her görev arasında `/clear` önerisini hatırlat (bağlam şişmesin).
