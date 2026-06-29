namespace SmartLink.Api.Config;

public class JwtOptions
{
    public string Issuer { get; set; } = "smartlink";
    public string Audience { get; set; } = "smartlink";
    public string Key { get; set; } = string.Empty;
    public int ExpiryHours { get; set; } = 12;
}

public class DeezerOptions
{
    public string AppId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    /// <summary>OAuth redirect URI registered in the Deezer app settings.</summary>
    public string RedirectUri { get; set; } = string.Empty;
}

public class SpotifyOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    /// <summary>OAuth redirect URI registered in the Spotify app settings.</summary>
    public string RedirectUri { get; set; } = string.Empty;
}

public class AdminSeedOptions
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "changeme";
}

public class AppUrlsOptions
{
    /// <summary>Public base URL of the web frontend, used for OAuth bounce-backs.</summary>
    public string WebBaseUrl { get; set; } = "http://localhost:5173";
}
