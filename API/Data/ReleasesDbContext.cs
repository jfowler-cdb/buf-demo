using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class ReleasesDbContext : DbContext
{
    public ReleasesDbContext(DbContextOptions<ReleasesDbContext> options) : base(options) { }

    public DbSet<ReleaseEntity> Releases => Set<ReleaseEntity>();
    public DbSet<TrackEntity> Tracks => Set<TrackEntity>();
    public DbSet<ReleaseTrackEntity> ReleaseTracks => Set<ReleaseTrackEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReleaseEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Artist).IsRequired();
        });

        modelBuilder.Entity<TrackEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Artist).IsRequired();
        });

        modelBuilder.Entity<ReleaseTrackEntity>(entity =>
        {
            entity.HasKey(e => new { e.ReleaseId, e.TrackId });
            entity.HasOne(e => e.Release).WithMany(r => r.ReleaseTracks).HasForeignKey(e => e.ReleaseId);
            entity.HasOne(e => e.Track).WithMany(t => t.ReleaseTracks).HasForeignKey(e => e.TrackId);
        });
    }
}
