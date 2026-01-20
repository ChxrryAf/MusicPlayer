using System.Text.Json.Serialization;

namespace SongApi.Models
{
    public class DiscogsSearchResponse
    {
        [JsonPropertyName("results")]
        public List<DiscogsSearchResult> Results { get; set; } = new();
    }

    public class DiscogsSearchResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("cover_image")]
        public string? CoverImage { get; set; }

        [JsonPropertyName("resource_url")]
        public string? ResourceUrl { get; set; }

        [JsonPropertyName("format")]
        public List<string>? Formats { get; set; }

        [JsonPropertyName("style")]
        public List<string>? Styles { get; set; }

        [JsonPropertyName("genre")]
        public List<string>? Genres { get; set; }
    }

    public class DiscogsSearchItem
    {
        public string DiscogsId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public int? Year { get; set; }
        public string? Country { get; set; }
        public string CoverUrl { get; set; } = string.Empty;
        public string? ResourceUrl { get; set; }
    }
}
