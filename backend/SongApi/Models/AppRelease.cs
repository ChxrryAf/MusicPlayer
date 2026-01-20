namespace SongApi.Models
{
    public class AppRelease
    {
        public int Id { get; set; }
        public int? ArtistId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? Year { get; set; }
        public string? Country { get; set; }
        public string? CoverUrl { get; set; }
    }
}
