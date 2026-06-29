namespace SmartLink.Api.Services;

/// <summary>
/// Canonical set of supported platforms, their display order and default CTA labels.
/// </summary>
public static class PlatformCatalog
{
    public record PlatformMeta(string Key, string DisplayName, string ActionLabel, int Order);

    private static readonly PlatformMeta[] All =
    {
        new("spotify",      "Spotify",       "Play",     10),
        new("appleMusic",   "Apple Music",   "Play",     20),
        new("youtubeMusic", "YouTube Music", "Play",     30),
        new("youtube",      "YouTube",       "Watch",    35),
        new("amazonMusic",  "Amazon Music",  "Play",     40),
        new("deezer",       "Deezer",        "Play",     50),
        new("tidal",        "Tidal",         "Play",     60),
        new("soundcloud",   "SoundCloud",    "Play",     70),
        new("pandora",      "Pandora",       "Play",     80),
        new("itunes",       "iTunes Store",  "Download", 90),
        new("amazonStore",  "Amazon",        "Buy",      100),
    };

    private static readonly Dictionary<string, PlatformMeta> ByKey =
        All.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static bool IsKnown(string key) => ByKey.ContainsKey(key);

    public static PlatformMeta? Get(string key) =>
        ByKey.TryGetValue(key, out var m) ? m : null;

    public static int OrderOf(string key) => Get(key)?.Order ?? 999;

    public static string ActionLabelOf(string key) => Get(key)?.ActionLabel ?? "Play";
}
