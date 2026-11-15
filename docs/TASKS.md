# TASKS

Opus planlar ve görevleri buraya yazar; ucuz ajanlar uygular; her görevden sonra build+test çalışır ve sonuç buraya işlenir.
Durum: `bekliyor` / `sürüyor` / `bitti`. Commit yalnızca kullanıcı isteyince. Kilometre taşında `/cost`.

| # | Görev | Durum | Ajan | Doğrulama |
|---|---|---|---|---|
| 0 | Canlı konum akışı (ILiveLocationFeed, dashboard canlı alanlar, irtifa/yön persist) | bitti | kod-yazici + test-yazici | 93 test geçti, commit 789339e push edildi |
| 1 | Cihazda doğrulama: ±m/GPS badge, kayıt başlat/durdur, sayfa değişince kayıt sürmesi, irtifa/yön/duruş süresi | bekliyor | kullanıcı (cihaz) | - |
| 2 | Android foreground service: ekran kapalıyken kayıt (type `location`, `ACCESS_BACKGROUND_LOCATION`, `FOREGROUND_SERVICE_LOCATION`, kalıcı bildirim; `ILocationService` arkasında) | bekliyor (Opus planı gerekli) | Opus plan -> kod-yazici -> test-yazici | - |
| 3 | Küçük bilinen sorunlar (cihaz doğrulamasında çıkanlar buraya eklenir) | bekliyor | basit-duzeltici / kod-yazici | - |
| 4 | Faz 5: telemetri, diagnostik, crash reporting, release signing, CI | bekliyor | Opus plan -> kod-yazici | - |

## Notlar
- Kapsam dışı (canlı akış işinden): grafikler, harita rota+marker, uydu sayısı, vites, yakıt, canlı pil.
- Ajan görevleri kendi başına anlaşılır yazılır: dosya yolları, istenen sonuç, kapsam sınırı.
