namespace SmartLink.Api.Models;

/// <summary>
/// A record of a visitor adding a track to their Deezer library via the pre-save flow.
/// </summary>
public class DeezerSave
{
    public long Id { get; set; }

    public Guid PageId { get; set; }
    public Page? Page { get; set; }

    /// <summary>Deezer track id that was added.</summary>
    public string TrackId { get; set; } = string.Empty;

    /// <summary>Deezer user id of the visitor, when the API returned it.</summary>
    public string? DeezerUserId { get; set; }

    /// <summary>ok | failed.</summary>
    public string Status { get; set; } = "ok";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
