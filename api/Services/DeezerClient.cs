using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartLink.Api.Config;

namespace SmartLink.Api.Services;

/// <summary>
/// Minimal Deezer OAuth client for the pre-save flow: build the consent URL,
/// exchange the code for a token, and add a track to the user's library.
/// </summary>
public class DeezerClient
{
    private const string ConnectBase = "https://connect.deezer.com/oauth";
    private const string ApiBase = "https://api.deezer.com";

    private readonly IHttpClientFactory _http;
    private readonly DeezerOptions _opts;
    private readonly ILogger<DeezerClient> _log;

    public DeezerClient(IHttpClientFactory http, IOptions<DeezerOptions> opts, ILogger<DeezerClient> log)
    {
        _http = http;
        _opts = opts.Value;
        _log = log;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_opts.AppId) &&
        !string.IsNullOrWhiteSpace(_opts.SecretKey) &&
        !string.IsNullOrWhiteSpace(_opts.RedirectUri);

    public string BuildAuthUrl(string state)
    {
        var q = new Dictionary<string, string?>
        {
            ["app_id"] = _opts.AppId,
            ["redirect_uri"] = _opts.RedirectUri,
            ["perms"] = "manage_library",
            ["state"] = state,
        };
        var query = string.Join("&", q.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value ?? "")}"));
        return $"{ConnectBase}/auth.php?{query}";
    }

    /// <summary>Exchanges an auth code for an access token. Returns null on failure.</summary>
    public async Task<string?> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var client = _http.CreateClient("deezer");
        var url = $"{ConnectBase}/access_token.php?app_id={Uri.EscapeDataString(_opts.AppId)}" +
                  $"&secret={Uri.EscapeDataString(_opts.SecretKey)}" +
                  $"&code={Uri.EscapeDataString(code)}&output=json";

        using var resp = await client.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("Deezer token exchange failed: {Status} {Body}", resp.StatusCode, body);
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("access_token", out var tok))
                return tok.GetString();
        }
        catch (JsonException)
        {
            _log.LogWarning("Deezer token response was not JSON: {Body}", body);
        }
        return null;
    }

    /// <summary>Returns the Deezer user id for a token, or null.</summary>
    public async Task<string?> GetUserIdAsync(string accessToken, CancellationToken ct = default)
    {
        var client = _http.CreateClient("deezer");
        using var resp = await client.GetAsync($"{ApiBase}/user/me?access_token={Uri.EscapeDataString(accessToken)}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("id", out var id))
                return id.ValueKind == JsonValueKind.Number ? id.GetInt64().ToString() : id.GetString();
        }
        catch (JsonException) { }
        return null;
    }

    /// <summary>Adds a track to the authenticated user's library. Returns true on success.</summary>
    public async Task<bool> AddTrackAsync(string accessToken, string trackId, CancellationToken ct = default)
    {
        var client = _http.CreateClient("deezer");
        var url = $"{ApiBase}/user/me/tracks?track_id={Uri.EscapeDataString(trackId)}&access_token={Uri.EscapeDataString(accessToken)}";

        using var resp = await client.PostAsync(url, content: null, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("Deezer add track failed: {Status} {Body}", resp.StatusCode, body);
            return false;
        }

        // Deezer returns `true` on success, or an object with an "error" field.
        if (body.Contains("\"error\"", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("Deezer add track error body: {Body}", body);
            return false;
        }
        return body.Contains("true", StringComparison.OrdinalIgnoreCase);
    }
}
