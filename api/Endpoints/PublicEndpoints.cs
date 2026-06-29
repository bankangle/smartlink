using Microsoft.EntityFrameworkCore;
using SmartLink.Api.Data;
using SmartLink.Api.Dtos;
using SmartLink.Api.Models;
using SmartLink.Api.Services;

namespace SmartLink.Api.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        // Public gallery: every published page (consumed by the SSR landing page).
        app.MapGet("/api/pages", async (AppDbContext db) =>
        {
            var pages = await db.Pages
                .AsNoTracking()
                .Where(p => p.IsPublished &&
                    (p.OwnerId == null || db.AdminUsers.Any(u => u.Id == p.OwnerId && u.IsApproved)))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PublicPageSummaryDto(
                    p.Slug, p.ArtistName, p.Title, p.Subtitle, p.ArtworkUrl, p.AccentColor,
                    p.Links.Count(l => l.IsEnabled)))
                .ToListAsync();
            return Results.Ok(pages);
        });

        // Public page data by slug (consumed by the SvelteKit SSR loader).
        app.MapGet("/api/pages/{slug}", async (string slug, AppDbContext db) =>
        {
            var page = await db.Pages
                .AsNoTracking()
                .Include(p => p.Links)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished &&
                    (p.OwnerId == null || db.AdminUsers.Any(u => u.Id == p.OwnerId && u.IsApproved)));

            if (page is null) return Results.NotFound();

            var links = page.Links
                .Where(l => l.IsEnabled)
                .OrderBy(l => l.SortOrder)
                .ThenBy(l => PlatformCatalog.OrderOf(l.Platform))
                .Select(l => new PublicLinkDto(l.Platform, l.Url, l.ActionLabel, l.DeezerTrackId, l.SpotifyTrackId))
                .ToList();

            return Results.Ok(new PublicPageDto(
                page.Id, page.Slug, page.ArtistName, page.Title, page.Subtitle,
                page.ArtworkUrl, page.AccentColor, links));
        });

        // Click-tracking redirect: log the choice, then 302 to the real DSP URL.
        app.MapGet("/api/r/{pageId:guid}/{platform}", async (
            Guid pageId, string platform, HttpContext ctx, AppDbContext db) =>
        {
            var link = await db.PlatformLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.PageId == pageId && l.Platform == platform && l.IsEnabled);

            if (link is null) return Results.NotFound();

            var ua = ctx.Request.Headers.UserAgent.ToString();
            db.ClickEvents.Add(new ClickEvent
            {
                PageId = pageId,
                Platform = platform,
                UserAgent = string.IsNullOrWhiteSpace(ua) ? null : ua,
                Referrer = ctx.Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : null,
                DeviceType = UserAgentHelper.Classify(ua),
            });
            await db.SaveChangesAsync();

            return Results.Redirect(link.Url, permanent: false);
        });
    }
}
