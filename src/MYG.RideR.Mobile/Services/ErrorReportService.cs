using System.Text;
using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.Services;

public sealed class ErrorReportService : IErrorReporter
{
    private const string EmailKey = "error_report_email";
    private const string ReportedLengthKey = "error_report_length";
    private const int BodyTailChars = 6000;

    private readonly ICrashLog _crashLog;

    public ErrorReportService(ICrashLog crashLog)
    {
        _crashLog = crashLog;
    }

    public string ReportEmail
    {
        get => Preferences.Default.Get(EmailKey, string.Empty);
        set => Preferences.Default.Set(EmailKey, (value ?? string.Empty).Trim());
    }

    public Task<bool> HasUnreportedAsync()
    {
        var length = CurrentLength();
        return Task.FromResult(length > 0 && length != Preferences.Default.Get(ReportedLengthKey, 0L));
    }

    public async Task<bool> SendAsync()
    {
        var log = await _crashLog.ReadAsync();
        if (string.IsNullOrEmpty(log) || !Email.Default.IsComposeSupported)
            return false;

        // Only what was logged since the last report, so old (already fixed) errors are not sent again.
        var reported = Preferences.Default.Get(ReportedLengthKey, 0L);
        var fresh = reported > 0 && reported < log.Length ? log[(int)reported..] : log;
        var tail = fresh.Length > BodyTailChars ? fresh[^BodyTailChars..] : fresh;
        var body = new StringBuilder()
            .AppendLine($"Sürüm: {AppInfo.VersionString} (build {AppInfo.BuildString})")
            .AppendLine($"Cihaz: {DeviceInfo.Manufacturer} {DeviceInfo.Model}, {DeviceInfo.Platform} {DeviceInfo.VersionString}")
            .AppendLine()
            .AppendLine("--- crash.log (son kısım) ---")
            .Append(tail)
            .ToString();

        var message = new EmailMessage
        {
            Subject = $"MYG RideR hata raporu build {AppInfo.BuildString}",
            Body = body,
            BodyFormat = EmailBodyFormat.PlainText,
        };

        if (!string.IsNullOrWhiteSpace(ReportEmail))
            message.To = [ReportEmail];

        message.Attachments ??= [];
        message.Attachments.Add(new EmailAttachment(_crashLog.FilePath));

        await Email.Default.ComposeAsync(message);
        Preferences.Default.Set(ReportedLengthKey, CurrentLength());
        return true;
    }

    private long CurrentLength()
    {
        try
        {
            var info = new FileInfo(_crashLog.FilePath);
            return info.Exists ? info.Length : 0;
        }
        catch (IOException)
        {
            return 0;
        }
    }
}
