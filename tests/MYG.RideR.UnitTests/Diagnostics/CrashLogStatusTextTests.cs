using MYG.RideR.Application.Diagnostics;

namespace MYG.RideR.UnitTests.Diagnostics;

public class CrashLogStatusTextTests
{
    [Fact]
    public void Null_means_no_log() => Assert.Equal("Hata kaydı yok", CrashLogStatusText.From(null));

    [Fact]
    public void Zero_means_no_log() => Assert.Equal("Hata kaydı yok", CrashLogStatusText.From(0));

    [Fact]
    public void Exact_kb() => Assert.Equal("Hata kaydı var (2 KB)", CrashLogStatusText.From(2048));

    [Fact]
    public void Partial_kb_rounds_up() => Assert.Equal("Hata kaydı var (1 KB)", CrashLogStatusText.From(100));
}
