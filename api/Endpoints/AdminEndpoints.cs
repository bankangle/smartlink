using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartLink.Api.Data;
using SmartLink.Api.Dtos;
using SmartLink.Api.Models;
using SmartLink.Api.Services;

namespace SmartLink.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin");

        // ---- Login (anonymous) ----
        admin.MapPost("/login", async (LoginRequest req, AppDbContext db, TokenService tokens) =>
        {
            var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            return Results.Ok(new LoginResponse(tokens.Create(user), user.Username, user.IsAdmin, user.IsApproved));
        });

        // ---- Register a new artist account (anonymous) ----
        admin.MapPost("/register", async (RegisterRequest req, AppDbContext db, TokenService tokens) =>
        {
            var username = req.Username?.Trim() ?? "";
            if (username.Length < 3)
                return Results.BadRequest(new { error = "Username must be at least 3 characters." });
            if ((req.Password?.Length ?? 0) < 6)
                return Results.BadRequest(new { error = "Password must be at least 6 characters." });
            if (await db.AdminUsers.AnyAsync(u => u.Username == username))
                return Results.Conflict(new { error = "That username is already taken." });

            var user = new AdminUser
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                IsAdmin = false,
                IsApproved = false, // pending until the moderator approves
            };
            db.AdminUsers.Add(user);
            await db.SaveChangesAsync();

            // Auto sign-in: hand back a token just like login does.
            return Results.Ok(new LoginResponse(tokens.Create(user), user.Username, user.IsAdmin, user.IsApproved));
        });

        // Everything below requires a valid JWT.
        var secure = admin.MapGroup("").RequireAuthorization();

        // ---- Pending-artist moderation (moderator only) ----
        secure.MapGet("/artists", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var (_, isAdmin) = Caller(principal);
            if (!isAdmin) return Results.Forbid();

            var artists = await db.AdminUsers
                .Where(u => !u.IsAdmin)
                .OrderBy(u => u.IsApproved).ThenByDescending(u => u.CreatedAt)
                .Select(u => new ArtistModerationDto(
                    u.Id, u.Username, u.IsApproved,
                    db.Pages.Count(p => p.OwnerId == u.Id), u.CreatedAt))
                .ToListAsync();
            return Results.Ok(artists);
        });

        secure.MapPost("/artists/{id:guid}/approve", async (Guid id, ClaimsPrincipal principal, AppDbContext db) =>
        {
            var (_, isAdmin) = Caller(principal);
            if (!isAdmin) return Results.Forbid();

            var u = await db.AdminUsers.FirstOrDefaultAsync(x => x.Id == id && !x.IsAdmin);
            if (u is null) return Results.NotFound();
            u.IsApproved = true;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        secure.MapPost("/artists/{id:guid}/revoke", async (Guid id, ClaimsPrincipal principal, AppDbContext db) =>
        {
            var (_, isAdmin) = Caller(principal);
            if (!isAdmin) return Results.Forbid();

            var u = await db.AdminUsers.FirstOrDefaultAsync(x => x.Id == id && !x.IsAdmin);
            if (u is null) return Results.NotFound();
            u.IsApproved = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---- Resolve a DSP URL into all platform links (Odesli preview) ----
        secure.MapPost("/resolve", async (ResolveRequest req, OdesliClient odesli, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Url))
                return Results.BadRequest(new { error = "URL is required." });

            var result = await odesli.ResolveAsync(req.Url.Trim(), ct);
            return result is null
                ? Results.BadRequest(new { error = "Couldn't resolve that link. Check the URL and try again." })
                : Results.Ok(result);
        });

        // ---- List pages (own pages, or all if moderator) ----
        secure.MapGet("/pages", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var (uid, isAdmin) = Caller(principal);
            var q = db.Pages.AsNoTracking().AsQueryable();
            if (!isAdmin) q = q.Where(p => p.OwnerId == uid);

            var pages = await q
                .OrderByDescending(p => p.UpdatedAt)
                .Select(p => new PageListItemDto(
                    p.Id, p.Slug, p.ArtistName, p.Title, p.ArtworkUrl, p.IsPublished,
                    p.Clicks.Count, p.UpdatedAt))
                .ToListAsync();
            return Results.Ok(pages);
        });

        // ---- Get one page ----
        secure.MapGet("/pages/{id:guid}", async (Guid id, ClaimsPrincipal principal, AppDbContext db) =>
        {
            var (uid, isAdmin) = Caller(principal);
            var page = await db.Pages.AsNoTracking()
                .Include(p => p.Links)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (page is null || (!isAdmin && page.OwnerId != uid)) return Results.NotFound();
            return Results.Ok(ToAdminDto(page));
        });

        // ---- Create ----
        secure.MapPost("/pages", async (PageInput input, ClaimsPrincipal principal, AppDbContext db) =>
        {
            var (uid, _) = Caller(principal);
            var slug = Slugify(input.Slug);
            if (string.IsNullOrWhiteSpace(slug))
                return Results.BadRequest(new { error = "Slug is required." });
            if (await db.Pages.AnyAsync(p => p.Slug == slug))
                return Results.Conflict(new { error = $"Slug '{slug}' is already taken." });

            var page = new Page { Slug = slug, OwnerId = uid };
            ApplyScalars(page, input);
            page.Links = BuildLinks(input);
            db.Pages.Add(page);
            await db.SaveChangesAsync();

            return Results.Created($"/api/admin/pages/{page.Id}", ToAdminDto(page));
        });

        // ---- Update ----
        secure.MapPut("/pages/{id:guid}", async (Guid id, PageInput input, ClaimsPrincipal principal, AppDbContext db) =>
        {
            var (uid, isAdmin) = Caller(principal);
            var page = await db.Pages.Include(p => p.Links).FirstOrDefaultAsync(p => p.Id == id);
            if (page is null || (!isAdmin && page.OwnerId != uid)) return Results.NotFound();

            var slug = Slugify(input.Slug);
            if (string.IsNullOrWhiteSpace(slug))
                return Results.BadRequest(new { error = "Slug is required." });
            if (await db.Pages.AnyAsync(p => p.Slug == slug && p.Id != id))
                return Results.Conflict(new { error = $"Slug '{slug}' is already taken." });

            page.Slug = slug;
            ApplyScalars(page, input);
            page.UpdatedAt = DateTimeOffset.UtcNow;

            // Replace the link set in two phases. Reassigning the tracked navigation
            // collection in a single SaveChanges makes EF mistake the brand-new rows
            // for edits of the deleted ones (UPDATE … WHERE id = <new guid> → 0 rows).
            // So: delete the loaded links + persist the scalar edits first …
            db.PlatformLinks.RemoveRange(page.Links);
            await db.SaveChangesAsync();

            // … then insert the fresh set with the FK set explicitly.
            var newLinks = BuildLinks(input);
            foreach (var link in newLinks) link.PageId = page.Id;
            db.PlatformLinks.AddRange(newLinks);
            await db.SaveChangesAsync();

            page.Links = newLinks;
            return Results.Ok(ToAdminDto(page));
        });

        // ---- Delete ----
        secure.MapDelete("/pages/{id:guid}", async (Guid id, ClaimsPrincipal principal, AppDbContext db) =>
        {
            var (uid, isAdmin) = Caller(principal);
            var page = await db.Pages.FirstOrDefaultAsync(p => p.Id == id);
            if (page is null || (!isAdmin && page.OwnerId != uid)) return Results.NotFound();
            db.Pages.Remove(page);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---- Stats ----
        secure.MapGet("/pages/{id:guid}/stats", async (Guid id, ClaimsPrincipal principal, AppDbContext db) =>
        {
            var (uid, isAdmin) = Caller(principal);
            var page = await db.Pages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (page is null || (!isAdmin && page.OwnerId != uid)) return Results.NotFound();

            var byPlatform = await db.ClickEvents
                .Where(c => c.PageId == id)
                .GroupBy(c => c.Platform)
                .Select(g => new PlatformCountDto(g.Key, g.Count()))
                .ToListAsync();

            var total = byPlatform.Sum(p => p.Clicks);

            var deezerSaves = await db.DeezerSaves.CountAsync(d =>
                d.PageId == id && (d.Status == "ok" || d.Status == "demo"));
            var spotifySaves = await db.SpotifySaves.CountAsync(d =>
                d.PageId == id && (d.Status == "ok" || d.Status == "demo"));
            var saves = deezerSaves + spotifySaves;

            // Group by day in memory — date-truncation on DateTimeOffset doesn't
            // translate cleanly to SQL, and click volume per page is small.
            var times = await db.ClickEvents
                .Where(c => c.PageId == id)
                .Select(c => c.CreatedAt)
                .ToListAsync();

            var byDay = times
                .GroupBy(t => DateOnly.FromDateTime(t.UtcDateTime))
                .OrderBy(g => g.Key)
                .Select(g => new DailyCountDto(g.Key, g.Count()))
                .ToList();

            return Results.Ok(new PageStatsDto(
                page.Id, page.Slug, total, saves,
                byPlatform.OrderByDescending(p => p.Clicks).ToList(), byDay));
        });
    }

    // ----- helpers -----

    /// <summary>Pulls the caller's user id and moderator flag out of the JWT claims.</summary>
    private static (Guid UserId, bool IsAdmin) Caller(ClaimsPrincipal user)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        Guid.TryParse(idStr, out var id);
        return (id, user.IsInRole("admin"));
    }

    /// <summary>Copies the scalar fields from the input onto the page. Links are
    /// handled separately by the caller so update can delete the old rows first.</summary>
    private static void ApplyScalars(Page page, PageInput input)
    {
        page.ArtistName = input.ArtistName.Trim();
        page.Title = input.Title.Trim();
        page.Subtitle = Trim(input.Subtitle);
        page.ArtworkUrl = Trim(input.ArtworkUrl);
        page.AccentColor = Trim(input.AccentColor);
        page.SourceUrl = Trim(input.SourceUrl);
        page.IsPublished = input.IsPublished;
    }

    private static List<PlatformLink> BuildLinks(PageInput input)
    {
        var links = new List<PlatformLink>();
        if (input.Links is null) return links;

        var order = 0;
        foreach (var l in input.Links)
        {
            if (string.IsNullOrWhiteSpace(l.Url) || string.IsNullOrWhiteSpace(l.Platform)) continue;
            links.Add(new PlatformLink
            {
                Platform = l.Platform.Trim(),
                Url = l.Url.Trim(),
                ActionLabel = string.IsNullOrWhiteSpace(l.ActionLabel)
                    ? PlatformCatalog.ActionLabelOf(l.Platform)
                    : l.ActionLabel.Trim(),
                SortOrder = l.SortOrder != 0 ? l.SortOrder : order,
                IsEnabled = l.IsEnabled,
                DeezerTrackId = Trim(l.DeezerTrackId),
                SpotifyTrackId = Trim(l.SpotifyTrackId),
            });
            order += 10;
        }
        return links;
    }

    private static AdminPageDto ToAdminDto(Page p) => new(
        p.Id, p.Slug, p.ArtistName, p.Title, p.Subtitle, p.ArtworkUrl, p.AccentColor,
        p.SourceUrl, p.IsPublished, p.CreatedAt, p.UpdatedAt,
        p.Links.OrderBy(l => l.SortOrder)
            .Select(l => new LinkInput(l.Platform, l.Url, l.ActionLabel, l.SortOrder, l.IsEnabled, l.DeezerTrackId, l.SpotifyTrackId))
            .ToList());

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Normalizes a slug: lowercase, spaces/underscores to hyphens, strip unsafe chars.</summary>
    public static string Slugify(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var s = input.Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder(s.Length);
        char prevDash = '\0';
        foreach (var c in s)
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(c);
                prevDash = '\0';
            }
            else if (c is ' ' or '-' or '_' or '.')
            {
                if (prevDash != '-' && sb.Length > 0) { sb.Append('-'); prevDash = '-'; }
            }
        }
        return sb.ToString().Trim('-');
    }
}
