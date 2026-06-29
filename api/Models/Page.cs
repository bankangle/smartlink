namespace SmartLink.Api.Models;

/// <summary>
/// A smart-link landing page for a single track or release.
/// </summary>
public class Page
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>URL-safe identifier used in the public path, e.g. /buryy-closer.</summary>
    public string Slug { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional one-liner shown under the title (e.g. "Out now").</summary>
    public string? Subtitle { get; set; }

    /// <summary>Cover artwork URL (resolved from the source link or set manually).</summary>
    public string? ArtworkUrl { get; set; }

    /// <summary>Hex accent color derived from the artwork; drives the page gradient.</summary>
    public string? AccentColor { get; set; }

    /// <summary>The original DSP URL the admin pasted to resolve all platforms via Odesli.</summary>
    public string? SourceUrl { get; set; }

    public bool IsPublished { get; set; } = true;

    /// <summary>The artist account that owns this page. Null = created by the moderator/seed (always public).</summary>
    public Guid? OwnerId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<PlatformLink> Links { get; set; } = new();
    public List<ClickEvent> Clicks { get; set; } = new();
}
