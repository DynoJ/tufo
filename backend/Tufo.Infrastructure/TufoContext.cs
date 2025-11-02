using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tufo.Infrastructure.Identity;
using Tufo.Core.Entities; 

namespace Tufo.Infrastructure;

public class TufoContext : IdentityDbContext<AppUser>
{
    public TufoContext(DbContextOptions<TufoContext> options) : base(options) { }

    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Climb> Climbs => Set<Climb>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<RouteNote> RouteNotes => Set<RouteNote>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Area>(e => e.Property(x => x.Name).IsRequired().HasMaxLength(160));

        b.Entity<Climb>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(160);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.HeroUrl).HasMaxLength(512);
            e.Property(x => x.HeroAttribution).HasMaxLength(256);
            e.HasOne(x => x.Area)
             .WithMany(a => a.Climbs)
             .HasForeignKey(x => x.AreaId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.AreaId);
        });

        b.Entity<Media>(e =>
        {
            e.Property(x => x.Url).IsRequired().HasMaxLength(512);
            e.Property(x => x.ThumbnailUrl).HasMaxLength(512);
            e.Property(x => x.Caption).HasMaxLength(512);
            e.HasOne(x => x.Climb)
             .WithMany(c => c.Media)
             .HasForeignKey(x => x.ClimbId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ClimbId);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CreatedAt);
            e.HasOne<AppUser>()
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<RouteNote>(e =>
        {
            e.Property(x => x.Body).IsRequired().HasMaxLength(2000);
            e.HasOne(x => x.Climb)
             .WithMany(c => c.Notes)
             .HasForeignKey(x => x.ClimbId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ClimbId);
            e.HasIndex(x => x.UserId);
            e.HasOne<AppUser>()
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}