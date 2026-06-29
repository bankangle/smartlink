namespace SmartLink.Api.Models;

/// <summary>
/// An admin operator who can create and edit pages and view analytics.
/// </summary>
public class AdminUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>The single moderator account. Sees every page and approves artists.</summary>
    public bool IsAdmin { get; set; }

    /// <summary>Self-registered artists start unapproved; their pages stay hidden until a moderator approves them.</summary>
    public bool IsApproved { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
