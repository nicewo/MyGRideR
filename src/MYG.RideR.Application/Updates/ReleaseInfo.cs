using System.Text.Json;

namespace MYG.RideR.Application.Updates;

/// <summary>Latest published build: CI tags releases as "build-{versionCode}" with one .apk asset.</summary>
public sealed record ReleaseInfo(int VersionCode, string ApkUrl)
{
    public const string TagPrefix = "build-";

    public static ReleaseInfo? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagEl) || tagEl.GetString() is not { } tag
                || !tag.StartsWith(TagPrefix, StringComparison.Ordinal)
                || !int.TryParse(tag.AsSpan(TagPrefix.Length), out var versionCode)
                || versionCode <= 0)
                return null;

            if (root.TryGetProperty("draft", out var draft) && draft.ValueKind == JsonValueKind.True)
                return null;
            if (root.TryGetProperty("prerelease", out var pre) && pre.ValueKind == JsonValueKind.True)
                return null;

            if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
                var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                if (name is not null && url is not null
                    && name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)
                    && url.StartsWith("https://", StringComparison.Ordinal))
                    return new ReleaseInfo(versionCode, url);
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Local developer builds keep versionCode 1 and are signed with a different key; never update those.</summary>
    public bool IsNewerThan(int installedVersionCode) =>
        installedVersionCode > 1 && VersionCode > installedVersionCode;
}
