namespace SmartLink.Api.Models;

/// <summary>
/// One streaming/store destination shown on a page (Spotify, Apple Music, ...).
/// </summary>
public class PlatformLink
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PageId { get; set; }
    public Page? Page { get; set; }

    /// <summary>Canonical platform key: spotify, appleMusic, youtubeMusic, amazonMusic, deezer, tidal, ...</summary>
    public string Platform { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    /// <summary>Call-to-action label: "Play", "Save", "Download".</summary>
    public string ActionLabel { get; set; } = "Play";

    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>Deezer track id, when known, used by the "Add to Library" pre-save flow.</summary>
    public string? DeezerTrackId { get; set; }

    /// <summary>Spotify track id, when known, used by the "Add to Library" pre-save flow.</summary>
    public string? SpotifyTrackId { get; set; }
}
