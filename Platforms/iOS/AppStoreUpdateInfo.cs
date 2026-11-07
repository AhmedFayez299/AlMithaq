#if IOS
using System.Text.Json;

namespace AlMithaq.Platforms.iOS;

internal sealed record AppStoreUpdateInfo(string Version, Uri StoreUrl)
{
    internal static AppStoreUpdateInfo? FromLookup(string json, string bundleId, string installedVersion, string deviceOS)
    {
        var installed = ParseVersion(installedVersion);
        var os = ParseVersion(deviceOS);
        if (installed is null || os is null) return null;
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array) return null;
        foreach (var app in results.EnumerateArray())
        {
            if (Text(app, "bundleId") != bundleId || Text(app, "kind") != "software") continue;
            var text = Text(app, "version");
            var version = ParseVersion(text);
            if (version is null || version <= installed) continue;
            if (Text(app, "minimumOsVersion") is { } minimum)
            {
                var required = ParseVersion(minimum);
                if (required is null || required > os) continue;
            }
            if (!Uri.TryCreate(Text(app, "trackViewUrl"), UriKind.Absolute, out var url) ||
                url.Scheme != Uri.UriSchemeHttps || url.Host is not ("apps.apple.com" or "itunes.apple.com")) continue;
            return new(text!, url);
        }
        return null;
    }

    private static string? Text(JsonElement element, string name) => element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static Version? ParseVersion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (!System.Version.TryParse(text.Contains('.') ? text : text + ".0", out var version)) return null;
        return new(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
    }
}
#endif
