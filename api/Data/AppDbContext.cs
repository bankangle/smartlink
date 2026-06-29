using Microsoft.EntityFrameworkCore;
using SmartLink.Api.Models;

namespace SmartLink.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PlatformLink> PlatformLinks => Set<PlatformLink>();
    public DbSet<ClickEvent> ClickEvents => Set<ClickEvent>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<DeezerSave> DeezerSaves => Set<DeezerSave>();
    public DbSet<SpotifySave> SpotifySaves => Set<SpotifySave>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Page>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Slug).HasMaxLength(120).IsRequired();
            e.Property(p => p.ArtistName).HasMaxLength(200).IsRequired();
            e.Property(p => p.Title).HasMaxLength(200).IsRequired();
            e.HasMany(p => p.Links)
                .WithOne(l => l.Page!)
                .HasForeignKey(l => l.PageId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Clicks)
                .WithOne(c => c.Page!)
                .HasForeignKey(c => c.PageId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => p.OwnerId);
        });

        b.Entity<PlatformLink>(e =>
        {
            e.Property(l => l.Platform).HasMaxLength(40).IsRequired();
            e.Property(l => l.Url).HasMaxLength(2048).IsRequired();
            e.Property(l => l.ActionLabel).HasMaxLength(40);
            e.HasIndex(l => new { l.PageId, l.Platform });
        });

        b.Entity<ClickEvent>(e =>
        {
            e.Property(c => c.Platform).HasMaxLength(40).IsRequired();
            e.HasIndex(c => new { c.PageId, c.CreatedAt });
        });

        b.Entity<AdminUser>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(80).IsRequired();
        });

        b.Entity<DeezerSave>(e =>
        {
            e.Property(d => d.TrackId).HasMaxLength(40).IsRequired();
            e.HasIndex(d => new { d.PageId, d.CreatedAt });
        });

        b.Entity<SpotifySave>(e =>
        {
            e.Property(d => d.TrackId).HasMaxLength(64).IsRequired();
            e.HasIndex(d => new { d.PageId, d.CreatedAt });
        });
    }
}
