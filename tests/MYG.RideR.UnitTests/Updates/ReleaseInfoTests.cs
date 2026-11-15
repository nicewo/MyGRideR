using MYG.RideR.Application.Updates;

namespace MYG.RideR.UnitTests.Updates;

public sealed class ReleaseInfoTests
{
    private const string Valid = """
        {
          "tag_name": "build-42",
          "draft": false,
          "prerelease": false,
          "assets": [
            { "name": "notes.txt", "browser_download_url": "https://github.com/x/notes.txt" },
            { "name": "myg-rider.apk", "browser_download_url": "https://github.com/nicewo/MyGRideR/releases/download/build-42/myg-rider.apk" }
          ]
        }
        """;

    [Fact]
    public void Parse_ReadsVersionAndApkUrl()
    {
        var info = ReleaseInfo.Parse(Valid);

        Assert.NotNull(info);
        Assert.Equal(42, info!.VersionCode);
        Assert.EndsWith("/build-42/myg-rider.apk", info.ApkUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{ "tag_name": "v1.0", "assets": [] }""")]
    [InlineData("""{ "tag_name": "build-abc", "assets": [] }""")]
    [InlineData("""{ "tag_name": "build-5", "assets": [ { "name": "a.zip", "browser_download_url": "https://x/a.zip" } ] }""")]
    [InlineData("""{ "tag_name": "build-5", "draft": true, "assets": [ { "name": "a.apk", "browser_download_url": "https://x/a.apk" } ] }""")]
    [InlineData("""{ "tag_name": "build-5", "assets": [ { "name": "a.apk", "browser_download_url": "http://x/a.apk" } ] }""")]
    public void Parse_RejectsInvalid(string json) => Assert.Null(ReleaseInfo.Parse(json));

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(10, 10, false)]
    [InlineData(10, 11, false)]
    [InlineData(10, 1, false)]
    [InlineData(10, 0, false)]
    public void IsNewerThan(int release, int installed, bool expected) =>
        Assert.Equal(expected, new ReleaseInfo(release, "https://x/a.apk").IsNewerThan(installed));
}
