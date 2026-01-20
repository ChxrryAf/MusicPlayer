namespace SongApi.Models
{
    public class Song
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public int Duration { get; set; } // seconds
        public string AlbumArt { get; set; } = string.Empty;
        public string StreamUrl { get; set; } = string.Empty;

        // Optional: persist Discogs lookup results to avoid repeat calls
        public string? DiscogsId { get; set; }
        public string? DiscogsCacheJson { get; set; }
        public DateTime? DiscogsCachedAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
