using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartLink.Api.Config;
using SmartLink.Api.Models;
using SmartLink.Api.Services;

namespace SmartLink.Api.Data;

public static class DbSeeder
{
    /// <summary>Applies migrations, ensures the admin account exists, and (optionally) seeds a demo page.</summary>
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var seed = sp.GetRequiredService<IOptions<AdminSeedOptions>>().Value;
        var config = sp.GetRequiredService<IConfiguration>();
        var log = sp.GetRequiredService<ILogger<AppDbContext>>();

        // Build the schema from the current model on a fresh database. Simple and
        // deploy-safe for this MVP (no migration history to drift); schema changes
        // are applied by recreating the database. Swapping in EF migrations is a
        // documented next step.
        await db.Database.EnsureCreatedAsync();

        if (!await db.AdminUsers.AnyAsync(u => u.Username == seed.Username))
        {
            db.AdminUsers.Add(new AdminUser
            {
                Username = seed.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(seed.Password),
                IsAdmin = true,
                IsApproved = true,
            });
            await db.SaveChangesAsync();
            log.LogInformation("Seeded admin user '{User}'.", seed.Username);
        }

        // Seed the Coasthill IV roster from the embedded JSON so a fresh deploy
        // ships with real content. Idempotent: only creates pages whose slug is
        // missing, so it's safe to run on every boot.
        await SeedRosterAsync(db, log);

        // Best-effort demo page so a fresh deployment isn't blank. Skips silently
        // if disabled, if any pages already exist, or if Odesli is unreachable.
        var seedDemo = config.GetValue("DemoSeed:Enabled", true);
        if (seedDemo && !await db.Pages.AnyAsync())
        {
            await TrySeedDemoPageAsync(sp, db, config, log);
        }
    }

    private record RosterPage(
        string Slug, string ArtistName, string Title, string? Subtitle, string? ArtworkUrl,
        string? AccentColor, string? SourceUrl, bool IsPublished, List<RosterLink> Links);
    private record RosterLink(
        string Platform, string Url, string ActionLabel, int SortOrder, bool IsEnabled,
        string? DeezerTrackId, string? SpotifyTrackId);

    /// <summary>Creates any roster pages (from the embedded seed file) whose slug isn't present yet.</summary>
    private static async Task SeedRosterAsync(AppDbContext db, ILogger log)
    {
        try
        {
            var asm = typeof(DbSeeder).Assembly;
            var resourceName = Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("roster-seed.json", StringComparison.Ordinal));
            if (resourceName is null) { log.LogInformation("Roster seed resource not found; skipping."); return; }

            await using var stream = asm.GetManifestResourceStream(resourceName)!;
            var roster = await JsonSerializer.DeserializeAsync<List<RosterPage>>(
                stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (roster is null || roster.Count == 0) return;

            var existing = (await db.Pages.Select(p => p.Slug).ToListAsync()).ToHashSet();
            var added = 0;
            foreach (var r in roster.Where(r => !existing.Contains(r.Slug)))
            {
                db.Pages.Add(new Page
                {
                    Slug = r.Slug,
                    ArtistName = r.ArtistName,
                    Title = r.Title,
                    Subtitle = r.Subtitle,
                    ArtworkUrl = r.ArtworkUrl,
                    AccentColor = r.AccentColor,
                    SourceUrl = r.SourceUrl,
                    IsPublished = r.IsPublished,
                    OwnerId = null, // moderator/seed-owned → always public
                    Links = (r.Links ?? new()).Select(l => new PlatformLink
                    {
                        Platform = l.Platform,
                        Url = l.Url,
                        ActionLabel = l.ActionLabel,
                        SortOrder = l.SortOrder,
                        IsEnabled = l.IsEnabled,
                        DeezerTrackId = l.DeezerTrackId,
                        SpotifyTrackId = l.SpotifyTrackId
                    }).ToList()
                });
                added++;
            }

            if (added > 0)
            {
                await db.SaveChangesAsync();
                log.LogInformation("Seeded {Count} roster page(s).", added);
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Roster seed skipped due to an error.");
        }
    }

    private static async Task TrySeedDemoPageAsync(
        IServiceProvider sp, AppDbContext db, IConfiguration config, ILogger log)
    {
        try
        {
            var sourceUrl = config["DemoSeed:SourceUrl"]
                ?? "https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp"; // The Killers — Mr. Brightside

            var odesli = sp.GetRequiredService<OdesliClient>();
            var resolved = await odesli.ResolveAsync(sourceUrl);
            if (resolved is null || resolved.Links.Count == 0)
            {
                log.LogInformation("Demo seed skipped: Odesli returned nothing.");
                return;
            }

            // The public URL is the artist's name (e.g. /the-killers). An explicit
            // DemoSeed:Slug override still wins if configured.
            var slug = config["DemoSeed:Slug"];
            if (string.IsNullOrWhiteSpace(slug))
                slug = Endpoints.AdminEndpoints.Slugify(resolved.ArtistName);
            if (string.IsNullOrWhiteSpace(slug))
                slug = "demo";

            var page = new Page
            {
                Slug = slug,
                ArtistName = resolved.ArtistName ?? "Demo Artist",
                Title = resolved.Title ?? "Demo Track",
                Subtitle = "Out now",
                ArtworkUrl = resolved.ArtworkUrl,
                AccentColor = resolved.AccentColor,
                SourceUrl = sourceUrl,
                IsPublished = true,
                Links = resolved.Links.Select((l, i) => new PlatformLink
                {
                    Platform = l.Platform,
                    Url = l.Url,
                    ActionLabel = PlatformCatalog.ActionLabelOf(l.Platform),
                    SortOrder = i * 10,
                    IsEnabled = true,
                    DeezerTrackId = l.DeezerTrackId,
                    SpotifyTrackId = l.SpotifyTrackId
                }).ToList()
            };

            db.Pages.Add(page);
            await db.SaveChangesAsync();
            log.LogInformation("Seeded demo page '/{Slug}' ({Count} links).", slug, page.Links.Count);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Demo seed skipped due to an error.");
        }
    }
}
