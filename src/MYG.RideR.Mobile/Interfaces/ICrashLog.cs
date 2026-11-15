namespace MYG.RideR.Mobile.Interfaces;

public interface ICrashLog
{
    string FilePath { get; }

    void Install();

    Task<string?> ReadAsync();

    Task ClearAsync();
}
