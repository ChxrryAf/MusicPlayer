using Microsoft.AspNetCore.Mvc;

namespace SongApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SongApiController : ControllerBase
    {
        // Beispiel-Daten – idealerweise später aus einem Service ziehen
        private static readonly List<Song> Songs = new()
        {
            new(1, "Midnight Dreams",  "Luna & Co",       234, "/images/catja.jpg", "/audio/track1.mp3"),
            new(2, "Lavender Fields",  "Ethereal Sounds", 189, "/images/cat2.jpg",  "/audio/track2.mp3"),
            new(3, "Purple Haze",      "Violet Sky",      205, "/images/AVO.jpg",   "/audio/track3.mp3"),
            new(4, "Second Flower", "Purple whiskers", 177, "/images/cat5.jpg", "/audio/track4.mp3")
        };

        [HttpGet("songs")]
        public IActionResult GetSongs()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var dto = Songs.Select(s => s with
            {
                AlbumArt = $"{baseUrl}{s.AlbumArt}",
                StreamUrl = $"{baseUrl}{s.StreamUrl}"
            });
            return Ok(dto);
        }

        [HttpGet("songs/{id:int}")]
        public IActionResult GetSong(int id)
        {
            var s = Songs.FirstOrDefault(x => x.Id == id);
            if (s is null) return NotFound();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return Ok(s with
            {
                AlbumArt = $"{baseUrl}{s.AlbumArt}",
                StreamUrl = $"{baseUrl}{s.StreamUrl}"
            });
        }

        [HttpPost("songs")]
        public IActionResult AddSong([FromBody] Song song)
        {
            var nextId = Songs.Any() ? Songs.Max(x => x.Id) + 1 : 1;
            Songs.Add(song with { Id = nextId });
            return CreatedAtAction(nameof(GetSong), new { id = nextId }, song with { Id = nextId });
        }
    }

    public record Song(int Id, string Title, string Artist, int Duration, string AlbumArt, string StreamUrl);
}

