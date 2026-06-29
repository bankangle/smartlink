using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartLink.Api.Config;

namespace SmartLink.Api.Services;

/// <summary>
/// Minimal Spotify OAuth client for the pre-save flow: build the consent URL,
/// exchange the code for a token, and add a track to the user's library.
/// Note: Spotify apps start in "development mode" — only users you add to the
/// app's allowlist (up to 25) can authorize until the app is quota-extended.
/// </summary>
public class SpotifyClient
{
    private const string AuthorizeUrl = "https://accounts.spotify.com/authorize";
    private const string TokenUrl = "https://accounts.spotify.com/api/token";
    private const string ApiBase = "https://api.spotify.com/v1";
    private const string Scope = "user-library-modify";

    private readonly IHttpClientFactory _http;
    private readonly SpotifyOptions _opts;
    private readonly ILogger<SpotifyClient> _log;

    public SpotifyClient(IHttpClientFactory http, IOptions<SpotifyOptions> opts, ILogger<SpotifyClient> log)
    {
        _http = http;
        _opts = opts.Value;
        _log = log;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_opts.ClientId) &&
        !string.IsNullOrWhiteSpace(_opts.ClientSecret) &&
        !string.IsNullOrWhiteSpace(_opts.RedirectUri);

    public string BuildAuthUrl(string state)
    {
        var q = new Dictionary<string, string?>
        {
            ["client_id"] = _opts.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _opts.RedirectUri,
            ["scope"] = Scope,
            ["state"] = state,
        };
        var query = string.Join("&", q.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value ?? "")}"));
        return $"{AuthorizeUrl}?{query}";
    }

    /// <summary>Exchanges an auth code for an access token. Returns null on failure.</summary>
    public async Task<string?> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var client = _http.CreateClient("spotify");

        using var req = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opts.ClientId}:{_opts.ClientSecret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _opts.RedirectUri,
        });

        using var resp = await client.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("Spotify token exchange failed: {Status} {Body}", resp.StatusCode, body);
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
            _log.LogWarning("Spotify token response was not JSON: {Body}", body);
        }
        return null;
    }

    /// <summary>Returns the Spotify user id for a token, or null.</summary>
    public async Task<string?> GetUserIdAsync(string accessToken, CancellationToken ct = default)
    {
        var client = _http.CreateClient("spotify");
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var resp = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("id", out var id))
                return id.GetString();
        }
        catch (JsonException) { }
        return null;
    }

    /// <summary>Adds a track to the authenticated user's library. Returns true on success.</summary>
    public async Task<bool> AddTrackAsync(string accessToken, string trackId, CancellationToken ct = default)
    {
        var client = _http.CreateClient("spotify");
        using var req = new HttpRequestMessage(HttpMethod.Put, $"{ApiBase}/me/tracks?ids={Uri.EscapeDataString(trackId)}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var resp = await client.SendAsync(req, ct);
        if (resp.IsSuccessStatusCode) return true;

        var body = await resp.Content.ReadAsStringAsync(ct);
        _log.LogWarning("Spotify add track failed: {Status} {Body}", resp.StatusCode, body);
        return false;
    }
}
