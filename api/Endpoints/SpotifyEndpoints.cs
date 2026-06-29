using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartLink.Api.Config;
using SmartLink.Api.Data;
using SmartLink.Api.Models;
using SmartLink.Api.Services;

namespace SmartLink.Api.Endpoints;

public static class SpotifyEndpoints
{
    private record SaveState(Guid PageId, string TrackId);

    public static void MapSpotifyEndpoints(this IEndpointRouteBuilder app)
    {
        // Step 1: bounce the visitor to Spotify's consent screen.
        app.MapGet("/api/spotify/login", async (
            Guid pageId, string trackId, SpotifyClient spotify,
            AppDbContext db, IOptions<AppUrlsOptions> urls, CancellationToken ct) =>
        {
            // Demo fallback: when no Spotify app is configured, record the intent and
            // bounce back with a clearly labeled demo result so the flow is still fully
            // clickable. The real OAuth path below activates once credentials are present.
            if (!spotify.IsConfigured)
            {
                var webBase = urls.Value.WebBaseUrl.TrimEnd('/');
                var slug = await ResolveSlug(db, pageId, ct);
                db.SpotifySaves.Add(new SpotifySave { PageId = pageId, TrackId = trackId, Status = "demo" });
                await db.SaveChangesAsync(ct);
                return Results.Redirect($"{webBase}/{slug}?presave=ok-demo");
            }

            var json = JsonSerializer.Serialize(new SaveState(pageId, trackId));
            var state = Base64Url(json);
            return Results.Redirect(spotify.BuildAuthUrl(state));
        });

        // Step 2: Spotify redirects back here with ?code & ?state.
        app.MapGet("/api/spotify/callback", async (
            string? code, string? state, string? error,
            SpotifyClient spotify, AppDbContext db,
            IOptions<AppUrlsOptions> urls, CancellationToken ct) =>
        {
            var webBase = urls.Value.WebBaseUrl.TrimEnd('/');

            SaveState? parsed = TryParseState(state);
            string returnSlug = await ResolveSlug(db, parsed?.PageId, ct);
            string backTo = $"{webBase}/{returnSlug}";

            // User denied, or no code returned.
            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || parsed is null)
                return Results.Redirect($"{backTo}?presave=cancelled");

            var token = await spotify.ExchangeCodeAsync(code, ct);
            if (token is null)
                return await Fail(db, parsed, backTo, ct);

            var userId = await spotify.GetUserIdAsync(token, ct);
            var ok = await spotify.AddTrackAsync(token, parsed.TrackId, ct);

            db.SpotifySaves.Add(new SpotifySave
            {
                PageId = parsed.PageId,
                TrackId = parsed.TrackId,
                SpotifyUserId = userId,
                Status = ok ? "ok" : "failed",
            });
            await db.SaveChangesAsync(ct);

            return Results.Redirect($"{backTo}?presave={(ok ? "ok" : "failed")}");
        });
    }

    private static async Task<IResult> Fail(AppDbContext db, SaveState s, string backTo, CancellationToken ct)
    {
        db.SpotifySaves.Add(new SpotifySave { PageId = s.PageId, TrackId = s.TrackId, Status = "failed" });
        await db.SaveChangesAsync(ct);
        return Results.Redirect($"{backTo}?presave=failed");
    }

    private static async Task<string> ResolveSlug(AppDbContext db, Guid? pageId, CancellationToken ct)
    {
        if (pageId is null) return "";
        var slug = await db.Pages.AsNoTracking()
            .Where(p => p.Id == pageId)
            .Select(p => p.Slug)
            .FirstOrDefaultAsync(ct);
        return slug ?? "";
    }

    private static SaveState? TryParseState(string? state)
    {
        if (string.IsNullOrEmpty(state)) return null;
        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(state));
            return JsonSerializer.Deserialize<SaveState>(json);
        }
        catch { return null; }
    }

    private static string Base64Url(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
