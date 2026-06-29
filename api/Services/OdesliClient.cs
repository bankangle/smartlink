using System.Text.Json;
using SmartLink.Api.Dtos;

namespace SmartLink.Api.Services;

/// <summary>
/// Resolves a single DSP URL into links across all major platforms using the
/// free Odesli / song.link API (https://api.song.link/v1-alpha.1/links).
/// </summary>
public class OdesliClient
{
    private readonly IHttpClientFactory _http;
    private readonly ColorExtractor _color;
    private readonly ILogger<OdesliClient> _log;

    public OdesliClient(IHttpClientFactory http, ColorExtractor color, ILogger<OdesliClient> log)
    {
        _http = http;
        _color = color;
        _log = log;
    }

    public async Task<ResolveResponse?> ResolveAsync(string url, CancellationToken ct = default)
    {
        var client = _http.CreateClient("odesli");
        var requestUri = $"links?url={Uri.EscapeDataString(url)}&songIfSingle=true";

        using var resp = await client.GetAsync(requestUri, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("Odesli returned {Status} for {Url}", resp.StatusCode, url);
            return null;
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        // ---- platform links ----
        var links = new List<ResolvedLinkDto>();
        if (root.TryGetProperty("linksByPlatform", out var byPlatform))
        {
            foreach (var prop in byPlatform.EnumerateObject())
            {
                var platform = prop.Name;
                if (!PlatformCatalog.IsKnown(platform)) continue;
                if (!prop.Value.TryGetProperty("url", out var urlEl)) continue;

                // entityUniqueId looks like "DEEZER_SONG::3135556" — the suffix is the
                // platform's own track id, which the pre-save flows add to the library.
                string? trackId = prop.Value.TryGetProperty("entityUniqueId", out var euid)
                    ? ParseTrackId(euid.GetString())
                    : null;

                string? deezerTrackId = platform.Equals("deezer", StringComparison.OrdinalIgnoreCase) ? trackId : null;
                string? spotifyTrackId = platform.Equals("spotify", StringComparison.OrdinalIgnoreCase) ? trackId : null;

                links.Add(new ResolvedLinkDto(platform, urlEl.GetString() ?? "", deezerTrackId, spotifyTrackId));
            }
        }

        links = links
            .OrderBy(l => PlatformCatalog.OrderOf(l.Platform))
            .ToList();

        // ---- title / artist / artwork from the source entity ----
        string? title = null, artist = null, artwork = null;
        if (root.TryGetProperty("entitiesByUniqueId", out var entities))
        {
            string? sourceId = root.TryGetProperty("entityUniqueId", out var sId) ? sId.GetString() : null;
            JsonElement entity = default;
            bool found = sourceId != null && entities.TryGetProperty(sourceId, out entity);
            if (!found)
            {
                // fall back to the first entity
                foreach (var e in entities.EnumerateObject()) { entity = e.Value; found = true; break; }
            }

            if (found)
            {
                title = GetStr(entity, "title");
                artist = GetStr(entity, "artistName");
                artwork = GetStr(entity, "thumbnailUrl");
            }
        }

        string? accent = null;
        if (!string.IsNullOrWhiteSpace(artwork))
            accent = await _color.FromImageUrlAsync(artwork!, ct);

        return new ResolveResponse(artist, title, artwork, accent, links);
    }

    private static string? GetStr(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    /// <summary>entityUniqueId looks like "DEEZER_SONG::3135556" -> "3135556".</summary>
    private static string? ParseTrackId(string? entityUniqueId)
    {
        if (string.IsNullOrEmpty(entityUniqueId)) return null;
        var idx = entityUniqueId.LastIndexOf("::", StringComparison.Ordinal);
        return idx >= 0 ? entityUniqueId[(idx + 2)..] : null;
    }
}
