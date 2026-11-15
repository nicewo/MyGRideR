namespace MYG.RideR.Mobile.Interfaces;

public interface ICrashLog
{
    string FilePath { get; }

    void Install();

    /// <summary>Logs a handled exception (app keeps running) so it can be shared from Settings.</summary>
    void Record(Exception exception, string context);

    Task<string?> ReadAsync();

    Task ClearAsync();
}
