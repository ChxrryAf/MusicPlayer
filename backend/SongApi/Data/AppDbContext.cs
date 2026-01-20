using Microsoft.EntityFrameworkCore;
using SongApi.Models;

namespace SongApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Song> Songs => Set<Song>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Song>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Artist).IsRequired().HasMaxLength(200);
                entity.Property(e => e.AlbumArt).HasMaxLength(500);
                entity.Property(e => e.StreamUrl).HasMaxLength(500);
                entity.Property(e => e.DiscogsId).HasMaxLength(100);
                entity.Property(e => e.DiscogsCacheJson).HasColumnType("longtext");
                entity.Property(e => e.Duration).HasDefaultValue(0);
                entity.Property(e => e.UpdatedAt)
                      .HasColumnType("timestamp")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Volltextindex für schnelle Suche (MySQL 5.6+ auf InnoDB)
                entity.HasIndex(e => new { e.Title, e.Artist }).HasDatabaseName("IX_Songs_Fulltext");
            });
        }
    }
}
