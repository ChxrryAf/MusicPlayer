using Microsoft.EntityFrameworkCore;
using SongApi.Models;

namespace SongApi.Data
{
    public class DiscogsContext : DbContext
    {
        public DiscogsContext(DbContextOptions<DiscogsContext> options) : base(options)
        {
        }

        public DbSet<AppArtist> AppArtists => Set<AppArtist>();
        public DbSet<AppRelease> AppReleases => Set<AppRelease>();
        public DbSet<AppTrack> AppTracks => Set<AppTrack>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppArtist>(entity =>
            {
                entity.ToTable("app_artists");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Name).HasMaxLength(255);
            });

            modelBuilder.Entity<AppRelease>(entity =>
            {
                entity.ToTable("app_releases");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Title).HasMaxLength(500);
                entity.Property(r => r.Country).HasMaxLength(100);
                entity.Property(r => r.CoverUrl).HasMaxLength(1000);
            });

            modelBuilder.Entity<AppTrack>(entity =>
            {
                entity.ToTable("app_tracks");
                entity.HasKey(t => new { t.ReleaseId, t.Position });
                entity.Property(t => t.Position).HasMaxLength(50);
                entity.Property(t => t.Title).HasMaxLength(500);
                entity.Property(t => t.Duration).HasMaxLength(50);
            });
        }
    }
}
