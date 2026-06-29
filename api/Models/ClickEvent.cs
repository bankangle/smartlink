namespace SmartLink.Api.Models;

/// <summary>
/// One recorded outbound click on a platform link (logged at the redirect endpoint).
/// </summary>
public class ClickEvent
{
    public long Id { get; set; }

    public Guid PageId { get; set; }
    public Page? Page { get; set; }

    public string Platform { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? UserAgent { get; set; }
    public string? Referrer { get; set; }

    /// <summary>Coarse device class derived from the user agent: mobile, tablet, desktop.</summary>
    public string? DeviceType { get; set; }
}
