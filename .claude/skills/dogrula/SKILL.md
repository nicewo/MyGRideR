---
name: dogrula
description: MYG RideR solution'ını derler ve birim testlerini çalıştırır, sadece hata ve özet satırlarını döndürür. Her kod değişikliğinden sonra, commit öncesi ve "build al", "test et", "doğrula" denince kullan.
---

# Doğrula (build + test, kısa çıktı)

Amaç: build/test çıktısı bağlamı şişirmesin. Tam çıktıyı asla sohbete dökme.

## Adımlar

1. Build (repo kökünden):
   ```bash
   dotnet build MYG.RideR.sln --nologo -v q -clp:ErrorsOnly 2>&1 | tail -n 30
   ```
2. Build hatasızsa testler:
   ```bash
   dotnet test tests/MYG.RideR.UnitTests --nologo -v q 2>&1 | tail -n 25
   ```
3. Tek bir test başarısızsa ayrıntı için sadece onu çalıştır:
   ```bash
   dotnet test tests/MYG.RideR.UnitTests --nologo -v q --filter "FullyQualifiedName~<TestAdı>" 2>&1 | tail -n 40
   ```

## Sonucu raporlama

Tek satır: `build OK, X/Y test geçti` ya da `build HATA: <dosya:satır> <mesaj>` / `test HATA: <test adı> – <kısa neden>`.

## Kurallar

- Hata varsa kodu düzelt, tekrar doğrula. 3 denemede geçmezse dur ve nedenini yaz.
- Mobile projesi derlenemiyorsa (workload/SDK eksik) bunu ortam sorunu olarak raporla; kodu değiştirerek "geçirmeye" çalışma.
- Testi geçirmek için test beklentisini değiştirme; yalnızca TASKS.md'de "beklenti değişir" deniyorsa değiştir.
