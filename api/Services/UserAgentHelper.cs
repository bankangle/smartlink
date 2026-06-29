namespace SmartLink.Api.Services;

public static class UserAgentHelper
{
    /// <summary>Very coarse device classification for analytics: mobile | tablet | desktop.</summary>
    public static string Classify(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "unknown";
        var ua = userAgent.ToLowerInvariant();

        if (ua.Contains("ipad") || (ua.Contains("tablet") && !ua.Contains("mobile")))
            return "tablet";
        if (ua.Contains("mobi") || ua.Contains("iphone") || ua.Contains("android"))
            return "mobile";
        return "desktop";
    }
}
