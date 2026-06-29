namespace SmartLink.Api.Dtos;

// ----- Public -----

public record PublicLinkDto(string Platform, string Url, string ActionLabel, string? DeezerTrackId, string? SpotifyTrackId);

public record PublicPageSummaryDto(
    string Slug,
    string ArtistName,
    string Title,
    string? Subtitle,
    string? ArtworkUrl,
    string? AccentColor,
    int LinkCount);

public record PublicPageDto(
    Guid Id,
    string Slug,
    string ArtistName,
    string Title,
    string? Subtitle,
    string? ArtworkUrl,
    string? AccentColor,
    IReadOnlyList<PublicLinkDto> Links);

// ----- Admin auth -----

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, bool IsAdmin, bool IsApproved);

public record ArtistModerationDto(
    Guid Id,
    string Username,
    bool IsApproved,
    int PageCount,
    DateTimeOffset CreatedAt);

// ----- Admin resolve (Odesli preview) -----

public record ResolveRequest(string Url);
public record ResolvedLinkDto(string Platform, string Url, string? DeezerTrackId, string? SpotifyTrackId);
public record ResolveResponse(
    string? ArtistName,
    string? Title,
    string? ArtworkUrl,
    string? AccentColor,
    IReadOnlyList<ResolvedLinkDto> Links);

// ----- Admin page CRUD -----

public record LinkInput(string Platform, string Url, string ActionLabel, int SortOrder, bool IsEnabled, string? DeezerTrackId, string? SpotifyTrackId);

public record PageInput(
    string Slug,
    string ArtistName,
    string Title,
    string? Subtitle,
    string? ArtworkUrl,
    string? AccentColor,
    string? SourceUrl,
    bool IsPublished,
    List<LinkInput> Links);

public record PageListItemDto(
    Guid Id,
    string Slug,
    string ArtistName,
    string Title,
    string? ArtworkUrl,
    bool IsPublished,
    int TotalClicks,
    DateTimeOffset UpdatedAt);

public record AdminPageDto(
    Guid Id,
    string Slug,
    string ArtistName,
    string Title,
    string? Subtitle,
    string? ArtworkUrl,
    string? AccentColor,
    string? SourceUrl,
    bool IsPublished,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<LinkInput> Links);

// ----- Stats -----

public record PlatformCountDto(string Platform, int Clicks);
public record DailyCountDto(DateOnly Date, int Clicks);
public record PageStatsDto(
    Guid PageId,
    string Slug,
    int TotalClicks,
    int PreSaves,
    IReadOnlyList<PlatformCountDto> ByPlatform,
    IReadOnlyList<DailyCountDto> ByDay);
