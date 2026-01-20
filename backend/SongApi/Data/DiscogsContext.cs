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
                entity.Property(a => a.Id).HasColumnName("id");
                entity.Property(a => a.Name).HasColumnName("name").HasMaxLength(255);
                entity.Property(a => a.Profile).HasColumnName("profile");
            });

            modelBuilder.Entity<AppRelease>(entity =>
            {
                entity.ToTable("app_releases");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.ArtistId).HasColumnName("artist_id");
                entity.Property(r => r.Title).HasColumnName("title").HasMaxLength(500);
                entity.Property(r => r.Country).HasColumnName("country").HasMaxLength(100);
                entity.Property(r => r.CoverUrl).HasMaxLength(1000).HasColumnName("cover_url");
                entity.Property(r => r.Year).HasColumnName("year");
            });

            modelBuilder.Entity<AppTrack>(entity =>
            {
                entity.ToTable("app_tracks");
                entity.HasKey(t => new { t.ReleaseId, t.Position });
                entity.Property(t => t.ReleaseId).HasColumnName("release_id");
                entity.Property(t => t.Position).HasColumnName("position").HasMaxLength(50);
                entity.Property(t => t.Title).HasColumnName("title").HasMaxLength(500);
                entity.Property(t => t.Duration).HasColumnName("duration").HasMaxLength(50);
                // optionale Surrogate-ID in Tabelle ignorieren
                entity.Ignore("id");
            });
        }
    }
}
