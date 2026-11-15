# TASKS

Opus planlar ve görevleri buraya yazar; ucuz ajanlar uygular; her görevden sonra build+test çalışır ve sonuç buraya işlenir.
Durum: `bekliyor` / `sürüyor` / `bitti`. Görev bitip build+test geçince sormadan commit + push. Kilometre taşında `/cost`.

**Uygulama sırası:** 5 → 6 → 7 → 8 → 9 → 10 → 1 (cihaz) → 2 → 3 → 4

| # | Görev | Durum | Ajan | Doğrulama |
|---|---|---|---|---|
| 0 | Canlı konum akışı (ILiveLocationFeed, dashboard canlı alanlar, irtifa/yön persist) | bitti | kod-yazici + test-yazici | 93 test geçti, commit 789339e push edildi |
| 1 | Cihazda doğrulama: ±m/GPS badge, kayıt başlat/durdur, sayfa değişince kayıt sürmesi, irtifa/yön/duruş süresi, kırmızı ışıkta mesafe artmaması (5 sonrası) | bekliyor | kullanıcı (cihaz) | - |
| 2 | Android foreground service: ekran kapalıyken kayıt (type `location`, `ACCESS_BACKGROUND_LOCATION`, `FOREGROUND_SERVICE_LOCATION`, kalıcı bildirim; `ILocationService` arkasında) | bekliyor (Opus planı gerekli, 9 sonrası) | Opus plan -> kod-yazici -> test-yazici | - |
| 3 | Küçük bilinen sorunlar (liste aşağıda; cihaz doğrulamasında çıkanlar da buraya) | bekliyor | basit-duzeltici | - |
| 4 | Faz 5: telemetri, diagnostik, crash reporting, release signing, CI | bekliyor | Opus plan -> kod-yazici | - |
| 5 | Durdurma hatasına dayanıklılık (TripRecorder.StopAsync idempotent + VM hata yakalama) | bitti | kod-yazici + test-yazici | 96 test geçti, build 0 hata (kod commit 01e1d0a'da zaten vardı; doğrulandı) |
| 6 | GPS örnek filtresi: mesafe jitter + max hız sıçraması | bitti | kod-yazici + test-yazici | 103 test geçti, build 0 hata |
| 7 | Thread-safe özet: UI, rota listesini kilitsiz okumasın | bekliyor | kod-yazici + test-yazici | - |
| 8 | iOS / MacCatalyst konum izni metni (Info.plist) | bekliyor | basit-duzeltici | - |
| 9 | Ara kayıt + çökme kurtarma (sürüş sırasında periyodik yazma, açılışta yarım kalanı kapatma) | bekliyor | kod-yazici + test-yazici | - |
| 10 | Liste performansı: Trips tablosunda özet sütunlar, listeler örnek yüklemesin, SaveAsync hazır komut | bekliyor | kod-yazici + test-yazici | - |

## Görev detayları

Kararlar verildi; ajan tasarım değiştirmez. Her görev sonunda: `dotnet build MYG.RideR.sln --nologo -v q` ve `dotnet test tests/MYG.RideR.UnitTests --nologo -v q`.

### 5 — Durdurma hatasına dayanıklılık
Sorun: `StopAsync` önce `trip.Complete()` sonra `SaveAsync` yapıyor. Save hata atarsa `CurrentTrip` kalır, ikinci DURDUR `Complete` içinde "already completed" atar → sürüş hiç kaydedilemez. Ayrıca ikinci çağrı `ReleaseAsync`'i tekrar çağırıp dashboard'un feed tutucusunu düşürür. VM sadece `InvalidOperationException` yakalıyor; `SqliteException` async komuttan kaçıp uygulamayı çökertebilir.
- `src/MYG.RideR.Application/Trips/TripRecorder.cs`:
  - `private bool _feedHeld;` ekle. `StartAsync` başarılı acquire sonrası `true`. `StopAsync` içinde yalnızca `_feedHeld` ise abonelikten çık + `ReleaseAsync`, sonra `false`.
  - `Complete` yalnızca `trip.Status == TripStatus.Recording` ise çağrılır (lock içinde).
  - `SaveAsync` başarılı olursa `CurrentTrip = null`. Hata atarsa `CurrentTrip` tamamlanmış haliyle kalır ve istisna yeniden fırlatılır → sonraki `StopAsync` sadece kaydı tekrar dener.
  - `StartAsync`: `CurrentTrip` tamamlanmış ama kaydedilmemişse `InvalidOperationException("Önceki sürüş henüz kaydedilmedi.")`.
- `src/MYG.RideR.Mobile/ViewModels/DashboardViewModel.cs`, `ToggleRecordingAsync`: `catch (InvalidOperationException)` yanına `catch (Exception ex)` ekle; mesaj `"Kayıt kaydedilemedi, tekrar DURDUR'a bas"`, `Debug.WriteLine(ex)`. Mevcut 5 sn hata gösterimi mekanizması kullanılır. `Refresh` içinde `trip.Status == Completed` ise buton metni `"KAYDET"`.
- Testler (`tests/.../Trips/TripRecorderTests.cs`): save ilk çağrıda hata atan sahte repo → ilk Stop istisna, `CurrentTrip` dolu ve Completed; ikinci Stop başarılı, `CurrentTrip` null, `ReleaseAsync` toplam 1 kez çağrıldı; tamamlanmış-kaydedilmemiş durumda Start istisna.

### 6 — GPS örnek filtresi
Sorun: `Route.Add` her örneği mesafeye ekliyor; durunca ±5–10 m jitter km biriktiriyor, ortalama hız bozuluyor. `MaximumSpeedKmh` ham hızdan; tek kötü fix max'ı bozar.
Karar: ham örneklerin hepsi saklanmaya devam eder (GPX için). Sadece mesafe ve max hız hesabı filtrelenir. `Trip.Restore` aynı yolu kullandığı için eski kayıtlar da yeni kuralla yeniden hesaplanır (beklenen davranış).
- Yeni dosya `src/MYG.RideR.Core/Domain/TrackingThresholds.cs` (`public static class`, sadece `const`):
  - `MaxAccuracyMeters = 25`
  - `MinSegmentMeters = 5`
  - `MaxPlausibleSpeedMps = 83.3` (300 km/h)
  - `JumpReanchorCount = 3`
- `src/MYG.RideR.Core/Domain/Route.cs` — mesafe için "çapa" (anchor) mantığı:
  - `private LocationSample? _anchor; private int _rejectedJumps;`
  - Her `Add`: sıra kontrolü aynı kalır, örnek `_samples`'a her zaman eklenir.
  - `AccuracyMeters` null değil ve `> MaxAccuracyMeters` → mesafe için yok sayılır (return).
  - `_anchor` null → `_anchor = sample`, return.
  - `d = _anchor.DistanceMetersTo(sample)`, `dt = (sample.Timestamp - _anchor.Timestamp).TotalSeconds`.
  - `d < Math.Max(MinSegmentMeters, Math.Max(_anchor.AccuracyMeters ?? 0, sample.AccuracyMeters ?? 0))` → sayma, çapa yerinde kalır (durma jitter'ı birikmez).
  - `dt > 0 && d / dt > MaxPlausibleSpeedMps` → `_rejectedJumps++`; `_rejectedJumps >= JumpReanchorCount` ise `_anchor = sample; _rejectedJumps = 0;` (mesafe eklemeden yeniden çapala). Return.
  - Aksi halde `DistanceMeters += d; _anchor = sample; _rejectedJumps = 0;`
- `src/MYG.RideR.Core/Domain/Trip.cs`, `AddSample` içinde max hız:
  - Yalnızca `AccuracyMeters` null veya `<= MaxAccuracyMeters` ve `speed <= MaxPlausibleSpeedMps` olan örneklerin hızı aday olur.
  - Son 3 aday hızı `Queue<double>` içinde tut. 3 dolunca medyanı al → `MaximumSpeedKmh = Math.Max(MaximumSpeedKmh, median * 3.6)`.
- Testler (`tests/.../Domain/`): aynı noktada ±8 m jitter 60 örnek → mesafe 0; düz çizgide 10 m/s 60 sn → ≈600 m (±%2); accuracy 50 m örnek mesafeye katılmaz; tek 2 km zıplama → mesafe değişmez; 3 ardışık zıplama → yeniden çapalanır, mesafe eklenmez; tek 60 m/s sıçrama max'ı değiştirmez, 3 ardışık 20 m/s → max 72 km/h. Mevcut testlerden bu kurala uymayanlar yeni davranışa göre güncellenir (beklenti değiştirilir, kural değil).

### 7 — Thread-safe özet
Sorun: örnekler `TripRecorder._gate` altında ekleniyor, `DashboardViewModel.Refresh` ise `TripSummary.From(trip)` ile `Route.Samples`'ı UI thread'den kilitsiz geziyor.
- `src/MYG.RideR.Application/Abstractions/ITripRecorder.cs`: `TripSummary? GetCurrentSummary(DateTimeOffset now);` ekle (`CurrentTrip` yoksa null).
- `TripRecorder`: `lock (_gate) { return CurrentTrip is { } t ? TripSummary.From(t, now) : null; }`
- `DashboardViewModel.Refresh`: `TripSummary.From(trip, …)` yerine `_tripRecorder.GetCurrentSummary(now)` kullan.
- Test: summary değerleri `TripSummary.From` ile aynı; trip yokken null.

### 8 — iOS / MacCatalyst konum izni
- `src/MYG.RideR.Mobile/Platforms/iOS/Info.plist` ve `Platforms/MacCatalyst/Info.plist` içine `<dict>` altında:
  `<key>NSLocationWhenInUseUsageDescription</key><string>Hız, mesafe ve sürüş rotasını kaydetmek için konumunuz kullanılır.</string>`
- Başka değişiklik yok. Doğrulama: build.

### 9 — Ara kayıt + çökme kurtarma
Sorun: sürüş yalnızca DURDUR'da yazılıyor; çökme, OS'un uygulamayı kapatması veya pil bitmesi tüm sürüşü siler.
Karar: sürüş başında başlık satırı yazılır, örnekler parti parti eklenir, bitişte kapatılır. Açılışta yarım kalan sürüş **devam ettirilmez, otomatik kapatılır** (EndedAt = son örnek zamanı, örnek yoksa StartedAt).
- `src/MYG.RideR.Application/Abstractions/ITripRepository.cs` ekle (mevcut `SaveAsync` kalır):
  - `Task CreateAsync(Guid id, DateTimeOffset startedAt, CancellationToken ct = default);`
  - `Task AppendSamplesAsync(Guid tripId, int firstSeq, IReadOnlyList<LocationSample> samples, CancellationToken ct = default);`
  - `Task CompleteAsync(Guid tripId, DateTimeOffset endedAt, CancellationToken ct = default);`
  - `Task<IReadOnlyList<Trip>> ListUnfinishedAsync(CancellationToken ct = default);` (EndedAt IS NULL)
- `src/MYG.RideR.Infrastructure/Persistence/SqliteTripRepository.cs`: yukarıdakileri uygula. Append tek transaction + tek hazırlanmış komut (bkz. 10). Bağlantı açılışına `PRAGMA journal_mode = WAL;` ekle.
- `TripRecorder`:
  - `StartAsync`: feed acquire başarılıysa `CreateAsync`. Hata atarsa feed release + `CurrentTrip = null` + yeniden fırlat.
  - `_persistedCount` (yazılan örnek sayısı) ve `_lastFlushAt` tut.
  - `OnLocationChanged` içinde lock sonrası: `trip.Route.Samples.Count - _persistedCount >= 15` veya `now - _lastFlushAt >= 15 sn` → `_ = FlushAsync()`.
  - `FlushAsync`: `SemaphoreSlim _flushGate` ile seri; lock altında bekleyen örneklerin kopyasını al, `AppendSamplesAsync`, başarılıysa `_persistedCount` ilerlet. Hatayı yut + logla (sonraki flush tekrar dener; örnekler bellekte durur).
  - `StopAsync`: önce kalan örnekleri flush (hata fırlatır), sonra `CompleteAsync`. 5'teki idempotent kurallar aynen geçerli (`SaveAsync` yerine flush + `CompleteAsync`).
  - Yeni `Task<int> RecoverUnfinishedAsync(CancellationToken ct = default)` (`ITripRecorder`'a da ekle): `ListUnfinishedAsync` → her biri için `CompleteAsync(son örnek zamanı ?? StartedAt)`; kapatılan sayıyı döndür.
- `src/MYG.RideR.Mobile/ViewModels/DashboardViewModel.cs` `InitializeAsync`: ilk çağrıda bir kez `RecoverUnfinishedAsync`; >0 ise 5 sn mesaj `"Yarım kalan sürüş kaydedildi"`.
- Testler: 15 örnek sonrası append çağrıldı; 15 sn sonra (FakeTimeProvider) append çağrıldı; append hatası sonrası örnek kaybolmadan sonraki flush'ta gidiyor; Stop kalanları yazıp Complete ediyor; recover EndedAt'ı son örnek zamanına koyuyor; Sqlite: create+append+complete sonrası `GetAsync` örnekleri sırasıyla döndürüyor, `ListUnfinishedAsync` sadece açık olanları döndürüyor.

### 10 — Liste performansı
Sorun: `ListAsync` her sürüşün tüm örneklerini yüklüyor (1 saat ≈ 3600 satır); Trips/Statistics sayfaları büyüdükçe yavaşlar. `SaveAsync` her örnek için yeni komut oluşturuyor.
- `Trips` tablosuna nullable sütunlar (mevcut `MigrateSamplesAsync` deseniyle `ALTER TABLE`): `DistanceM REAL`, `MaxSpeedKmh REAL`, `StoppedSeconds REAL`.
- `SaveAsync` ve `CompleteAsync` bu sütunları doldurur. `CompleteAsync` için örnekleri okuyup `Trip.Restore` + `TripSummary.From(trip, endedAt)` ile hesaplar.
- Yeni `src/MYG.RideR.Application/Trips/TripHeader.cs`: `public sealed record TripHeader(Guid Id, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, double DistanceKm, double MaximumSpeedKmh, TimeSpan StoppedDuration);`
- `ITripRepository.ListHeadersAsync(CancellationToken ct = default)` → `Task<IReadOnlyList<TripHeader>>`, sadece `Trips` tablosundan, `StartedAt DESC`. Özet sütunu NULL olan bitmiş satırlar için bir kez örnekleri yükleyip hesapla ve sütunları güncelle (backfill).
- `TripStatistics.Aggregate(IEnumerable<TripHeader>, DateTimeOffset now)` overload'u. Süre: `(EndedAt ?? now) - StartedAt`.
- `TripsViewModel` ve `StatisticsViewModel` `ListHeadersAsync` kullanır. `ListAsync` GPX/detay için kalır.
- Örnek insert'leri (`SaveAsync` ve `AppendSamplesAsync`): döngü dışında tek komut + parametreler, `Prepare()`, döngüde sadece `Value` ata.
- Testler: header'lar doğru özetleri döndürüyor; özet sütunu NULL eski satır backfill ediliyor; Aggregate(header) eski Aggregate(trip) ile aynı sonucu veriyor.

### 3 — Küçük bilinen sorunlar
- `DashboardViewModel`: `RecordButtonText` başlangıç değeri `"BAŞLA"` → `"BAŞLAT"`.
- `DashboardViewModel.Refresh`: trip yokken `DistanceKm`, `AverageSpeedKmh`, `MaximumSpeedKmh`, `TripTime`, `StopTime` son sürüşün değerinde kalıyor — bilinçli karar: **kalsın** (son sürüş özeti olarak görünür), değişiklik yok.
- `src/MYG.RideR.Application/MYG.RideR.Application.csproj`: kullanılmayan `CommunityToolkit.Mvvm` referansını kaldır.
- `README.md`: "dashboard currently uses mock data…" durum notunu güncelle (gerçek GPS, SQLite, Mapsui var; arka plan kayıt yok).
- `Platforms/Android/AndroidManifest.xml`: `android:allowBackup="false"` (konum geçmişi bulut yedeğine gitmesin).
- `MYG.RideR.Mobile.csproj`: `MauiIcon`/`MauiSplashScreen` `Color="#512BD4"` → tema arka plan rengi (`Themes/Colors.xaml` içindeki ana koyu renk).

## Notlar
- Kapsam dışı (canlı akış işinden): grafikler, harita rota+marker, uydu sayısı, vites, yakıt, canlı pil.
- Sonraki aday (henüz görev değil): "hareket halinde ortalama hız" (duruşlar hariç); `MapPanel` konumu `ILiveLocationFeed`'den alsın ve rota çizgisi göstersin.
- Ajan görevleri kendi başına anlaşılır yazılır: dosya yolları, istenen sonuç, kapsam sınırı.
