# TASKS

Opus planlar ve görevleri buraya yazar; ucuz ajanlar uygular; her görevden sonra build+test çalışır ve sonuç buraya işlenir.
Durum: `bekliyor` / `sürüyor` / `bitti`. Görev bitip build+test geçince sormadan commit + push. Kilometre taşında `/cost`.

**Uygulama sırası (yeni):** 14 → 15 → 16 → 17 → 18. Referans: "Hız Göstergesi" uygulaması (Sayaç/Dijital/Harita sekmeleri, 4'lü istatistik, kilometre sayacı, koyu harita).

**Eski sıra:** 3 → 2 → 11 → 12 → 13 → 1 (cihaz, en sona ertelendi). 5–10 bitti.

| # | Görev | Durum | Ajan | Doğrulama |
|---|---|---|---|---|
| 0 | Canlı konum akışı (ILiveLocationFeed, dashboard canlı alanlar, irtifa/yön persist) | bitti | kod-yazici + test-yazici | 93 test geçti, commit 789339e push edildi |
| 1 | Cihazda doğrulama: ±m/GPS badge, kayıt başlat/durdur, sayfa değişince kayıt sürmesi, irtifa/yön/duruş süresi, kırmızı ışıkta mesafe artmaması (5 sonrası) | ertelendi (en son, kullanıcı) | kullanıcı (cihaz) | - |
| 2 | Android: ekran kapalıyken kayıt (location foreground service + kalıcı bildirim, `IRecordingKeepAlive` arkasında) | bitti | kod-yazici + test-yazici | build 0 hata (Android dahil), 125 test geçti; cihazda doğrulanmadı (#1) |
| 3 | Küçük bilinen sorunlar (liste aşağıda; cihaz doğrulamasında çıkanlar da buraya) | bitti | basit-duzeltici | build 0 hata, 119 test geçti (BAŞLAT zaten düzgündü) |
| 4 | Faz 5 → 11, 12, 13'e bölündü. Telemetri ve 3. parti crash servisi kapsam dışı (hesap/anahtar ve gizlilik kararı kullanıcının) | bölündü | - | - |
| 5 | Durdurma hatasına dayanıklılık (TripRecorder.StopAsync idempotent + VM hata yakalama) | bitti | kod-yazici + test-yazici | 96 test geçti, build 0 hata (kod commit 01e1d0a'da zaten vardı; doğrulandı) |
| 6 | GPS örnek filtresi: mesafe jitter + max hız sıçraması | bitti | kod-yazici + test-yazici | 103 test geçti, build 0 hata |
| 7 | Thread-safe özet: UI, rota listesini kilitsiz okumasın | bitti | kod-yazici + test-yazici | 105 test geçti, build 0 hata |
| 8 | iOS / MacCatalyst konum izni metni (Info.plist) | bitti | basit-duzeltici | 105 test geçti, build 0 hata |
| 9 | Ara kayıt + çökme kurtarma (sürüş sırasında periyodik yazma, açılışta yarım kalanı kapatma) | bitti | kod-yazici + test-yazici | 114 test geçti, build 0 hata |
| 10 | Liste performansı: Trips tablosunda özet sütunlar, listeler örnek yüklemesin, SaveAsync hazır komut | bitti | kod-yazici + test-yazici | build ok, 118 test geçti |
| 11 | CI: GitHub Actions (birim testleri + Android build) | bitti | kod-yazici | build 0 hata, 125 test geçti; yerel OK, Actions sonucu kontrol edilecek |
| 12 | Yerel çökme kaydı + Ayarlar'da paylaş/temizle | bitti | kod-yazici + test-yazici | build 0 hata (Android dahil), 129 test geçti; cihazda doğrulanmadı (#1) |
| 13 | Android release imzalama altyapısı (sır repoda yok) + docs/Release.md | bitti | basit-duzeltici | build 0 hata (Android dahil, MYG_KEYSTORE tanımsız), 129 test geçti; imzalı publish denenmedi (keystore yok) |
| 14 | Dashboard sadeleştirme: kesilen gösterge, boş yan kartlar, sahte yakıt/vites, 2x2 sürüş istatistiği | bitti | kod-yazici | build 0 hata (Android dahil), 142 test geçti; görsel kontrol cihazda (#1) |
| 15 | Üst durum çubuğu: GPS kalite + uydu + doğruluk tek satır, logo; hata mesajları toast'a | bekliyor | kod-yazici | - |
| 16 | Koyu harita + canlı konum işaretçisi + rota çizgisi + koordinat çipi | bekliyor | kod-yazici | - |
| 17 | Toplam kilometre sayacı (tüm sürüşler) | bekliyor | kod-yazici + test-yazici | - |
| 18 | Araç modu (yaya/bisiklet/motor/araba) → gösterge ölçeği | bekliyor | kod-yazici + test-yazici | - |

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

### 2 — Android: ekran kapalıyken kayıt
Karar: kayıt sürerken `location` tipinde bir **foreground service** çalışır; süreç ve konum dinleme ekran kapalı/uygulama arka plandayken canlı kalır. Servis kendisi konum okumaz; mevcut `MauiLocationService` dinlemeye devam eder. Servis uygulama öndeyken (BAŞLAT'a basınca) başlatıldığı için "uygulama kullanılırken" izni yeterlidir → **`ACCESS_BACKGROUND_LOCATION` istenmez** (Play politikası yükü yok). Sadece kayıt sürerken çalışır; dashboard canlı akışı için çalışmaz. iOS/Windows/MacCatalyst: no-op (iOS arka plan konumu ayrı iş, kapsam dışı).
- Yeni `src/MYG.RideR.Application/Abstractions/IRecordingKeepAlive.cs`:
  `public interface IRecordingKeepAlive { Task StartAsync(CancellationToken ct = default); Task StopAsync(CancellationToken ct = default); }`
- `TripRecorder` (ctor'a `IRecordingKeepAlive keepAlive` ekle):
  - `StartAsync`: `CreateAsync` başarılı olduktan sonra `await _keepAlive.StartAsync()`. Hata atarsa **kayıt devam eder**: yut + `Debug.WriteLine`. `private bool _keepAliveOn` tut.
  - `StopAsync`: feed release'ten hemen sonra, `_keepAliveOn` ise `StopAsync` (hata yut + logla), `_keepAliveOn = false`. Kaydetme hatası olsa bile keep-alive kapanmış olur (sürüş zaten bitti).
  - `StartAsync` içindeki her hata dalında (acquire/create hatası) keep-alive başlatılmamış olduğundan ek iş yok.
- Yeni `src/MYG.RideR.Mobile/Services/NoOpRecordingKeepAlive.cs`: iki metot `Task.CompletedTask`.
- Yeni `src/MYG.RideR.Mobile/Platforms/Android/Services/RecordingForegroundService.cs`:
  - `[Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeLocation)]`, `Android.App.Service`'ten türer, `OnBind` → null.
  - `OnStartCommand`: bildirim kanalı oluştur (id `"ride_recording"`, ad `"Sürüş kaydı"`, `NotificationImportance.Low`). Bildirim: başlık `"MYG RideR"`, metin `"Sürüş kaydediliyor"`, `SetOngoing(true)`, küçük ikon `Resource.Mipmap.appicon` (derlenmezse `Android.Resource.Drawable.IcMenuMyLocation`), dokununca `MainActivity`'yi açan `PendingIntent` (`PendingIntentFlags.Immutable | UpdateCurrent`, intent flag `SingleTop`). `AndroidX.Core.App.ServiceCompat.StartForeground(this, 1001, notification, (int)Android.Content.PM.ForegroundService.TypeLocation)`. Dönüş `StartCommandResult.NotSticky`.
  - `OnDestroy`: `StopForeground(StopForegroundFlags.Remove)`.
- Yeni `src/MYG.RideR.Mobile/Platforms/Android/Services/AndroidRecordingKeepAlive.cs` (`IRecordingKeepAlive`):
  - `StartAsync`: API 33+ ise `Permissions.RequestAsync<Permissions.PostNotifications>()` (red edilse de devam et; servis yine çalışır, sadece bildirim görünmez). Sonra `MainThread.InvokeOnMainThreadAsync` içinde `AndroidX.Core.Content.ContextCompat.StartForegroundService(Platform.AppContext, new Intent(Platform.AppContext, typeof(RecordingForegroundService)))`.
  - `StopAsync`: `Platform.AppContext.StopService(new Intent(..., typeof(RecordingForegroundService)))`.
- `MauiProgram.cs`: `#if ANDROID` → `AddSingleton<IRecordingKeepAlive, AndroidRecordingKeepAlive>()`, `#else` → `NoOpRecordingKeepAlive`.
- `Platforms/Android/AndroidManifest.xml` izinler: `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_LOCATION`, `POST_NOTIFICATIONS`. (`ACCESS_BACKGROUND_LOCATION` **ekleme**.)
- `docs/Roadmap.md` "Phase 2 Notes": arka plan için ACCESS_BACKGROUND_LOCATION gerekmediği, foreground service + while-in-use kararı ve iOS'un kapsam dışı olduğu yazılır.
- Testler (`TripRecorderTests`, sahte keep-alive sayaçlı): Start sonrası keep-alive Start 1 kez; Stop sonrası Stop 1 kez; keep-alive Start hata atınca kayıt yine başlar; acquire veya create hatasında keep-alive Start hiç çağrılmaz; kaydetme hatalı Stop'ta keep-alive yine kapanır ve ikinci Stop'ta tekrar Stop çağrılmaz.
- Risk (cihaz testinde bakılacak, bu görevde kod yok): MAUI `StartListeningForegroundAsync` arka planda güncelleme kesiyorsa ileride servis içinde FusedLocation'a geçilir.

### 11 — CI: GitHub Actions
- Yeni `.github/workflows/ci.yml`, tetikleyici `push` (main) ve `pull_request`:
  - Job `test` (`ubuntu-latest`): `actions/checkout@v4`; `actions/setup-dotnet@v4` with `global-json-file: global.json`; `dotnet test tests/MYG.RideR.UnitTests --nologo -v q`.
  - Job `android` (`windows-latest`, `needs: test`): checkout; setup-dotnet (global.json); `dotnet workload restore src/MYG.RideR.Mobile/MYG.RideR.Mobile.csproj`; `dotnet build src/MYG.RideR.Mobile/MYG.RideR.Mobile.csproj -f net10.0-android -c Release --nologo -v q`. İmzalama yok.
- `README.md` başına CI rozeti: `![CI](https://github.com/nicewo/MyGRideR/actions/workflows/ci.yml/badge.svg)`.
- Doğrulama: yerel build+test (workflow'u yerelde koşturmak gerekmez). Push sonrası Actions sonucu kullanıcı tarafından kontrol edilir; TASKS.md doğrulama sütununa "yerel OK, Actions sonucu kontrol edilecek" yaz.

### 12 — Yerel çökme kaydı
Karar: 3. parti servis yok; çökmeler cihazda dosyaya yazılır, kullanıcı Ayarlar'dan paylaşır.
- Yeni `src/MYG.RideR.Mobile/Interfaces/ICrashLog.cs`: `void Install(); Task<string?> ReadAsync(); Task ClearAsync(); string FilePath { get; }`.
- Yeni `src/MYG.RideR.Mobile/Services/CrashLogService.cs`:
  - Dosya `Path.Combine(FileSystem.AppDataDirectory, "crash.log")`.
  - `Install` (bir kez, ikinci çağrı no-op): `AppDomain.CurrentDomain.UnhandledException` ve `TaskScheduler.UnobservedTaskException` (bunda `SetObserved()` çağırma) yakala. `#if ANDROID` ek olarak `Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser`.
  - Kayıt formatı: `=== {UTC ISO zaman} | {AppInfo.VersionString} | {DeviceInfo.Platform} {DeviceInfo.VersionString} ===` + `exception.ToString()` + boş satır. Senkron `File.AppendAllText` (çökme anında async güvenilmez), her şey try/catch içinde (log yazımı asla ikinci istisna atmasın).
  - Dosya 256 KB'ı geçerse yazmadan önce son 128 KB'ı tutup üstüne yaz.
  - `ReadAsync`: dosya yoksa null. `ClearAsync`: dosyayı sil.
- `MauiProgram`: `AddSingleton<ICrashLog, CrashLogService>()`; `builder.Build()` sonrası `app.Services.GetRequiredService<ICrashLog>().Install()` çağırıp `app`'i döndür.
- `SettingsViewModel` (`partial`, ctor `ICrashLog`): `[ObservableProperty] string CrashLogStatus` ("Hata kaydı yok" / "Hata kaydı var (N KB)"); `LoadAsync`; `ShareCrashLogCommand` → dosya varsa `Share.Default.RequestAsync(new ShareFileRequest("MYG RideR hata kaydı", new ShareFile(FilePath)))`; `ClearCrashLogCommand` → temizle + status yenile.
- `SettingsPage.xaml(.cs)`: BindingContext DI'dan gelen `SettingsViewModel`; `OnAppearing` → `LoadAsync`. Mevcut karta durum label'ı + "Paylaş" ve "Temizle" butonları (mevcut tema stilleri, yeni renk yok). Başlıklar Türkçe: sayfa "Ayarlar".
- Testler: `CrashLogService` platform API'sine bağlı olduğundan birim testi yok; `SettingsViewModel` durum metni mantığını ayrı statik yardımcıya (`CrashLogStatusText.From(long? bytes)`) çıkar ve onu test et (null → "Hata kaydı yok", 2048 → "Hata kaydı var (2 KB)"). Yardımcı `src/MYG.RideR.Application/Diagnostics/CrashLogStatusText.cs` içinde (test projesi Mobile'a referans vermiyor).

### 13 — Android release imzalama altyapısı
Karar: anahtar ve şifreler **asla** repoya girmez; ortam değişkenlerinden okunur. Değişken yoksa Release build imzasız devam eder (CI kırılmaz).
- `MYG.RideR.Mobile.csproj` yeni PropertyGroup:
  ```xml
  <PropertyGroup Condition="'$(Configuration)' == 'Release' and $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android' and '$(MYG_KEYSTORE)' != ''">
    <AndroidKeyStore>true</AndroidKeyStore>
    <AndroidSigningKeyStore>$(MYG_KEYSTORE)</AndroidSigningKeyStore>
    <AndroidSigningKeyAlias>$(MYG_KEY_ALIAS)</AndroidSigningKeyAlias>
    <AndroidSigningStorePass>env:MYG_STORE_PASS</AndroidSigningStorePass>
    <AndroidSigningKeyPass>env:MYG_KEY_PASS</AndroidSigningKeyPass>
    <AndroidPackageFormat>aab</AndroidPackageFormat>
  </PropertyGroup>
  ```
- `.gitignore`: `*.keystore`, `*.jks`, `*.aab`, `*.apk` ekle.
- Yeni `docs/Release.md` (Türkçe, kısa): (1) `keytool -genkeypair -v -keystore myg-rider.keystore -alias myg-rider -keyalg RSA -keysize 2048 -validity 10000` — keystore'u repo dışında sakla, yedekle (kaybı = mağazada güncelleme yapamamak); (2) PowerShell'de `$env:MYG_KEYSTORE`, `$env:MYG_KEY_ALIAS`, `$env:MYG_STORE_PASS`, `$env:MYG_KEY_PASS` ayarla; (3) `dotnet publish src/MYG.RideR.Mobile/MYG.RideR.Mobile.csproj -f net10.0-android -c Release`; (4) çıktı `bin/Release/net10.0-android/publish/*.aab`; (5) sürüm artırma: `ApplicationDisplayVersion` ve `ApplicationVersion`.
- Doğrulama: değişkenler tanımsızken normal build+test geçmeli.

### 14 — Dashboard sadeleştirme
Sorun (cihaz ekranı): hız göstergesinin üstü (100–140) üst çubuğun altında kesiliyor; sol/sağdaki 6 `StatCard` (Mesafe, Süre, Rakım, Ort, Maks, Duruş) boş hap olarak görünüyor, değerleri okunmuyor; `FuelRatio` (E/F) ve `Gear` (VİTES) gerçek veri değil.
- `src/MYG.RideR.Mobile/Views/DashboardPage.xaml`: yan `StatCard` sütunlarını ve yakıt çubuğunu (E/F, `FuelRatio`) kaldır. `SpeedGauge`'a `Gear` bağlamasını ve VİTES kutusunu kaldır (`Controls/SpeedGauge.xaml(.cs)` içinde de VİTES bölümünü sil).
- Göstergeyi tam genişlik ortala, `HeightRequest` ile tamamı görünecek şekilde yerleştir (üst çubuğun altında kesilmesin; gerekirse `Margin` üst 8).
- Göstergenin altına 2x2 `Grid`: Süre (`TripTime`), Mesafe (`DistanceKm` "0.00 km"), Ort. hız (`AverageSpeedKmh` km/h), Azami hız (`MaximumSpeedKmh` km/h). Büyük rakam (FontSize 28, Bold, `TextPrimary`), küçük başlık (`CaptionLabel`). Duruş süresi (`StopTime`) Süre'nin altında küçük satır.
- Alttaki 3 kartı (HIZ GRAFİĞİ / TOPLAM MESAFE / RAKIM) tek satır küçük bilgiye indir: sadece Rakım (`AltitudeM` m) ve Yön (`HeadingText`). Hız ve mesafe yukarıda zaten var.
- `DashboardViewModel`: `Gear`, `FuelRatio` özelliklerini sil (başka yerde kullanılmıyorsa). `RecordButtonText` sığmıyor ("BAŞLA" görünüyor): `BottomToolbar.xaml` kayıt butonunda `FontSize` 14 ve `LineBreakMode="NoWrap"`.
- Doğrulama: build+test. Görsel kontrol cihazda (#1).

### 15 — Üst durum çubuğu + toast
- `Controls/TopStatusBar.xaml(.cs)`: kırmızı motor ikonunu `mygrider_logo.png` (32x32) ile değiştir. Orta hap: `GPS iyi · 8/14 · ±6m` biçimi — `DashboardViewModel` yeni `GpsSummaryText` özelliği: `GpsStatus` kalite metni + `SatelliteText` + `AccuracyText`, araya " · ". Hap rengi kaliteye göre (Good=`Success`, Fair/Poor=`Warning`, NoFix=`Danger`) → `GpsQualityColor` özelliği.
- Hata/bilgi mesajları GPS hapına yazılmasın: `ShowRecordingError` → `CommunityToolkit.Maui.Alerts.Toast.Make(message, ToastDuration.Long).Show()` (MainThread'de). `_recordingError`/`_recordingErrorUntil` mantığını kaldır; `ApplyQuality` sadece kaliteyi yazar.
- Doğrulama: build+test.

### 16 — Koyu harita + canlı konum
- `Controls/MapPanel.xaml.cs`: OSM tile yerine koyu katman: `new TileLayer(new HttpTileSource(new GlobalSphericalMercator(), "https://basemaps.cartocdn.com/dark_all/{z}/{x}/{y}.png", name: "CARTO Dark", attribution: new Attribution("© OpenStreetMap contributors © CARTO"), userAgent: "MYG-RideR/1.0"))` (Mapsui 5 API adlarına uy; derlenmezse BruTile `HttpTileSource` imzasına göre düzelt). Atıf görünür kalmalı.
- `MapPanel`'e `UpdatePosition(double lat, double lon, double? courseDeg)` ve `SetRoute(IReadOnlyList<(double Lat,double Lon)>)` ekle: `MemoryLayer` ile konum noktası (NeonBlue daire, 10px) ve rota çizgisi (`LineString`, turuncu #FF8A00, 4px). İlk konumda `CenterOnAndZoomTo(resolution 4)`, sonra sadece "takip" açıkken ortala; kullanıcı haritayı kaydırınca takip kapanır, konum butonu açar.
- Konum butonu `Geolocation.GetLocationAsync` yerine son canlı örneği kullanır.
- `DashboardPage.xaml.cs`: VM'den `PositionChanged` olayı (lat, lon, course) ve kayıt sürerken rota noktaları (her 5 sn'de `GetCurrentRoutePoints()` → `ITripRecorder` lock altında kopya) → MapPanel.
- Sol alta koordinat çipi (`39°25'34.1"N, 29°59'42.5"E` biçimi, `Application/Dashboard/LiveReadout.CoordinateText(lat, lon)` + birim testi).
- Doğrulama: build+test.

### 17 — Toplam kilometre sayacı
- `Application/Trips/TripStatistics` zaten toplam mesafe veriyor. `DashboardViewModel.InitializeAsync` ve her `StopAsync` sonrası `ITripRepository.ListHeadersAsync` → `TotalDistanceKm` → `OdometerText` ("000026 km", 6 hane, sıfır dolgulu, tam km).
- Dashboard'da 2x2 bloğun altında küçük "Kilometre sayacı" satırı.
- Test: `LiveReadout.OdometerText(26.4)` → "000026".

### 18 — Araç modu
- `Application/Dashboard/VehicleMode.cs`: `enum VehicleMode { Walk, Bicycle, Motorcycle, Car }` + `MaxGaugeKmh(mode)`: 20 / 60 / 240 / 240.
- Ayarlar'da 4 butonluk seçim; `Preferences` "vehicle_mode" (varsayılan Motorcycle). `SpeedGauge`'a `MaxValue` bindable property; ölçek ve etiketler bu değere göre çizilir (`GaugeDrawable`).
- Test: `MaxGaugeKmh` değerleri.

### 3 — Küçük bilinen sorunlar
- `DashboardViewModel`: `RecordButtonText` başlangıç değeri `"BAŞLA"` → `"BAŞLAT"`.
- `DashboardViewModel.Refresh`: trip yokken `DistanceKm`, `AverageSpeedKmh`, `MaximumSpeedKmh`, `TripTime`, `StopTime` son sürüşün değerinde kalıyor — bilinçli karar: **kalsın** (son sürüş özeti olarak görünür), değişiklik yok.
- `src/MYG.RideR.Application/MYG.RideR.Application.csproj`: kullanılmayan `CommunityToolkit.Mvvm` referansını kaldır.
- `README.md`: "dashboard currently uses mock data…" durum notunu güncelle (gerçek GPS, SQLite, Mapsui var; arka plan kayıt yok).
- `Platforms/Android/AndroidManifest.xml`: `android:allowBackup="false"` (konum geçmişi bulut yedeğine gitmesin).
- `SqliteTripRepository` sample insert (satır ~153): `AppendSamplesAsync` için `INSERT OR IGNORE INTO Samples` kullan. Neden: append DB'de commit olup istisna sonradan gelirse (iptal vb.) aynı `Seq` tekrar denenir, PK ihlali yüzünden sürüş bir daha kaydedilemez. `SaveAsync` önce siler, onu değiştirme. Test: aynı `firstSeq` ile iki kez append → hata yok, örnek sayısı değişmez.
- `MYG.RideR.Mobile.csproj`: `MauiIcon`/`MauiSplashScreen` `Color="#512BD4"` → tema arka plan rengi (`Themes/Colors.xaml` içindeki ana koyu renk).

## Notlar
- Kapsam dışı (referanstaki Nav. sekmesi): adres arama + rota tarifi. Ayrı faz; ücretsiz servis (OSRM/Nominatim) kullanım politikası incelenmeli.
- #1 cihaz testi kullanıcı isteğiyle en sona ertelendi; 2/11/12/13 cihazda doğrulanmadan bitti sayılır, cihaz testinde çıkanlar #3'e eklenir.
- Kapsam dışı (canlı akış işinden): grafikler, harita rota+marker, uydu sayısı, vites, yakıt, canlı pil.
- Sonraki aday (henüz görev değil): "hareket halinde ortalama hız" (duruşlar hariç); `MapPanel` konumu `ILiveLocationFeed`'den alsın ve rota çizgisi göstersin.
- Ajan görevleri kendi başına anlaşılır yazılır: dosya yolları, istenen sonuç, kapsam sınırı.
