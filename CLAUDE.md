# MYG RideR – Çalışma Kuralları

Mimari kurallar: `docs/AI_RULES.md`, `docs/Architecture.md`. Yol haritası: `docs/Roadmap.md`.

## Maliyet odaklı çalışma düzeni
Hedef: en az token ile doğru sonuç. Her iş en ucuz yeterli katmana verilir.

| Katman | Model | Ne için |
|---|---|---|
| Mimari / plan | Opus (plan modu, `opusplan`) | Tasarım kararları, yeni faz planı, belirsiz veya riskli işler |
| Kod yazımı | Sonnet (`kod-yazici` ajanı veya ana oturum) | Net tarifli özellik, ViewModel/servis/XAML yazımı, refactor |
| Mekanik işler | Haiku (`basit-duzeltici`) | Yeniden adlandırma, renk token'a çevirme, dosya silme, doküman/metin düzeltmesi |
| Arama | Haiku (`kod-tarayici`) | "Nerede kullanılıyor?", geniş tarama |
| Test | Haiku (`test-yazici`) | Mevcut kod için birim testi |

## Kurallar
- Plan Opus'ta yapılır, plan onaylandıktan sonra uygulama Sonnet/Haiku'ya verilir. Opus ile kod yazma.
- Ajana verilen görev kendi başına anlaşılır olmalı: dosya yolları, istenen sonuç, kapsam sınırı yazılır. Tasarım kararı ajana bırakılmaz.
- Küçük (1–2 dosyalık) iş için ajan açma. Ajan başlatmak soğuk başlangıç maliyetidir, doğrudan yap.
- Dosyaları baştan sona okuma; `Grep` ile bul, gereken satır aralığını oku.
- Aynı bilgiyi tekrar okuma/türetme. Uzun sohbetlerde gereksiz özet ve tekrar yok.
- Çıktı kısa: ne değişti, doğrulama sonucu. Dosya içeriğini sohbete dökme.
- Build/test çıktısını süz (`| tail`, `--nologo -v q`).
- Commit yalnızca kullanıcı isteyince.
- Gerçek harcama yalnızca `/cost` ile görülür. Her kilometre taşında (faz bitişi, büyük refactor, commit öncesi) kullanıcıya `/cost` çalıştırmasını hatırlat.

## Doğrulama
- `dotnet build MYG.RideR.sln --nologo -v q`
- `dotnet test tests/MYG.RideR.UnitTests --nologo -v q`
