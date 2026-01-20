namespace SongApi.Models
{
    public class AppTrack
    {
        public int ReleaseId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Duration { get; set; }
    }
}
