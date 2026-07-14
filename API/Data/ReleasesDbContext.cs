using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class ReleasesDbContext : DbContext
{
    public ReleasesDbContext(DbContextOptions<ReleasesDbContext> options) : base(options) { }

    public DbSet<ReleaseEntity> Releases => Set<ReleaseEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReleaseEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Artist).IsRequired();
        });
    }
}
