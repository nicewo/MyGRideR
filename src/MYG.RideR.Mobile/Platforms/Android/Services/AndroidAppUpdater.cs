using Android.Content;
using Android.OS;
using AndroidX.Core.Content;
using MYG.RideR.Application.Abstractions;
using MYG.RideR.Application.Updates;
using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.Platforms.Android.Services;

/// <summary>
/// Self-update from GitHub Releases (CI publishes "build-{versionCode}" with a signed APK).
/// Android always shows its own install confirmation; that single tap cannot be skipped for sideloaded apps.
/// </summary>
public sealed class AndroidAppUpdater : IAppUpdater
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/nicewo/MyGRideR/releases/latest";
    private const string ApkMimeType = "application/vnd.android.package-archive";

    private readonly ITripRecorder _tripRecorder;
    private readonly HttpClient _http;
    private int _running;

    public AndroidAppUpdater(ITripRecorder tripRecorder)
    {
        _tripRecorder = tripRecorder;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MYG-RideR-Updater/1.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public async Task CheckAndInstallAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _running, 1) == 1)
            return;

        try
        {
            // Installing kills the process; never interrupt a ride.
            if (_tripRecorder.CurrentTrip is not null)
                return;

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                return;

            var installed = int.TryParse(AppInfo.BuildString, out var code) ? code : 0;
            var json = await _http.GetStringAsync(LatestReleaseUrl, cancellationToken);
            var release = ReleaseInfo.Parse(json);
            if (release is null || !release.IsNewerThan(installed))
                return;

            var context = Platform.AppContext;
            var dir = Path.Combine(context.CacheDir!.AbsolutePath, "updates");
            Directory.CreateDirectory(dir);
            var apkPath = Path.Combine(dir, $"myg-rider-{release.VersionCode}.apk");

            foreach (var old in Directory.GetFiles(dir))
            {
                if (!string.Equals(old, apkPath, StringComparison.Ordinal))
                    TryDelete(old);
            }

            if (!File.Exists(apkPath))
            {
                var partPath = apkPath + ".part";
                await using (var source = await _http.GetStreamAsync(release.ApkUrl, cancellationToken))
                await using (var target = File.Create(partPath))
                {
                    await source.CopyToAsync(target, cancellationToken);
                }

                File.Move(partPath, apkPath, overwrite: true);
            }

            if (_tripRecorder.CurrentTrip is not null)
                return;

            await MainThread.InvokeOnMainThreadAsync(() => LaunchInstaller(context, apkPath));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex}");
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    private static void LaunchInstaller(Context context, string apkPath)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O && context.PackageManager?.CanRequestPackageInstalls() != true)
        {
            // One-time: user allows "install unknown apps" for MYG RideR; the next launch installs.
            var settings = new Intent(
                global::Android.Provider.Settings.ActionManageUnknownAppSources,
                global::Android.Net.Uri.Parse("package:" + context.PackageName));
            settings.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(settings);
            return;
        }

        var uri = FileProvider.GetUriForFile(context, context.PackageName + ".fileprovider", new Java.IO.File(apkPath));
        var install = new Intent(Intent.ActionView);
        install.SetDataAndType(uri, ApkMimeType);
        install.AddFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);
        context.StartActivity(install);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
