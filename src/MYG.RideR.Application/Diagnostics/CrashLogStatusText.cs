namespace MYG.RideR.Application.Diagnostics;

public static class CrashLogStatusText
{
    public static string From(long? bytes)
    {
        if (bytes is null or <= 0)
        {
            return "Hata kaydı yok";
        }

        var kb = (bytes.Value + 1023) / 1024;
        return $"Hata kaydı var ({kb} KB)";
    }
}
