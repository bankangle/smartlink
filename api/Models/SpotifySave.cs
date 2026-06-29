namespace SmartLink.Api.Models;

/// <summary>
/// A record of a visitor adding a track to their Spotify library via the pre-save flow.
/// </summary>
public class SpotifySave
{
    public long Id { get; set; }

    public Guid PageId { get; set; }
    public Page? Page { get; set; }

    /// <summary>Spotify track id that was added.</summary>
    public string TrackId { get; set; } = string.Empty;

    /// <summary>Spotify user id of the visitor, when the API returned it.</summary>
    public string? SpotifyUserId { get; set; }

    /// <summary>ok | failed | demo.</summary>
    public string Status { get; set; } = "ok";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
