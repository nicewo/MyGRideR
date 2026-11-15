using System.Text;
using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.Services;

public sealed class CrashLogService : ICrashLog
{
    private const int MaxBytes = 256 * 1024;
    private const int KeepBytes = 128 * 1024;

    private readonly object _lock = new();
    private bool _installed;

    public string FilePath { get; } = Path.Combine(FileSystem.AppDataDirectory, "crash.log");

    public void Install()
    {
        lock (_lock)
        {
            if (_installed)
            {
                return;
            }

            _installed = true;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) => Write(e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => Write(e.Exception);
#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) => Write(e.Exception);
#endif
    }

    public Task<string?> ReadAsync()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return Task.FromResult<string?>(null);
            }

            return File.ReadAllTextAsync(FilePath, Encoding.UTF8).ContinueWith(t => (string?)t.Result);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public Task ClearAsync()
    {
        try
        {
            lock (_lock)
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
        }
        catch
        {
            // ignore
        }

        return Task.CompletedTask;
    }

    private void Write(object? error)
    {
        try
        {
            var text = new StringBuilder()
                .Append("=== ")
                .Append(DateTime.UtcNow.ToString("o"))
                .Append(" | ")
                .Append(AppInfo.VersionString)
                .Append(" | ")
                .Append(DeviceInfo.Platform)
                .Append(' ')
                .Append(DeviceInfo.VersionString)
                .AppendLine(" ===")
                .AppendLine(error?.ToString() ?? "(unknown exception)")
                .AppendLine()
                .ToString();

            lock (_lock)
            {
                Trim();
                File.AppendAllText(FilePath, text, Encoding.UTF8);
            }
        }
        catch
        {
            // log yazımı asla ikinci istisna atmamalı
        }
    }

    private void Trim()
    {
        var info = new FileInfo(FilePath);
        if (!info.Exists || info.Length <= MaxBytes)
        {
            return;
        }

        byte[] tail = new byte[KeepBytes];
        using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
        {
            fs.Seek(-KeepBytes, SeekOrigin.End);
            fs.ReadExactly(tail);
        }

        File.WriteAllBytes(FilePath, tail);
    }
}
