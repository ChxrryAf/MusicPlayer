using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SongApi.Data;
using SongApi.Models;
using System.Net.Http.Json;
using System.Data;

namespace SongApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SongApiController : ControllerBase
    {
        private readonly DiscogsContext _discogs;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SongApiController> _logger;

        public SongApiController(DiscogsContext discogs, IHttpClientFactory httpClientFactory, ILogger<SongApiController> logger)
        {
            _discogs = discogs;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // GET /SongApi/debug/columns (dev helper)
        [HttpGet("debug/columns"), ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> GetColumns([FromQuery] string? table = null, CancellationToken ct = default)
        {
            try
            {
                var tableName = string.IsNullOrWhiteSpace(table) ? "app_releases" : table.Trim();
                var conn = _discogs.Database.GetDbConnection();
                await conn.OpenAsync(ct);
                var schema = conn.GetSchema("Columns", new string?[] { null, conn.Database, tableName, null });
                var cols = schema.Rows.Cast<DataRow>()
                    .Select(r => r["COLUMN_NAME"]?.ToString())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToArray();
                await conn.CloseAsync();
                return Ok(new { table = tableName, columns = cols });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read app_releases columns");
                return StatusCode(StatusCodes.Status500InternalServerError, "Schema read failed");
            }
        }

        // GET /SongApi/songs?query=term
        [HttpGet("songs")]
        public async Task<IActionResult> GetSongs([FromQuery] string? query = null, [FromQuery] int limit = 50)
        {
            var normalizedLimit = Math.Clamp(limit, 1, 200);

            var baseQuery =
                from r in _discogs.AppReleases.AsNoTracking()
                join a in _discogs.AppArtists.AsNoTracking() on r.ArtistId equals a.Id into artistJoin
                from a in artistJoin.DefaultIfEmpty()
                select new { Release = r, ArtistName = a != null ? a.Name : string.Empty };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToLower();
                baseQuery = baseQuery.Where(s =>
                    EF.Functions.Like((s.Release.Title ?? string.Empty).ToLower(), $"%{term}%") ||
                    EF.Functions.Like((s.ArtistName ?? string.Empty).ToLower(), $"%{term}%"));
            }

            var songs = await baseQuery
                .OrderBy(s => s.Release.Title)
                .Take(normalizedLimit)
                .ToListAsync();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var dto = songs.Select(s => new Song
            {
                Id = s.Release.Id,
                Title = s.Release.Title ?? string.Empty,
                Artist = s.ArtistName ?? string.Empty,
                Duration = 0,
                AlbumArt = EnsureAbsolute(baseUrl, s.Release.CoverUrl),
                StreamUrl = string.Empty,
                UpdatedAt = DateTime.UtcNow
            });

            return Ok(dto);
        }

        // GET /SongApi/discogs/search?query=term
        [HttpGet("discogs/search")]
        public async Task<IActionResult> SearchDiscogs([FromQuery] string query, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Parameter 'query' ist erforderlich.");
            }

            var normalizedLimit = Math.Clamp(limit, 1, 50);
            var client = _httpClientFactory.CreateClient("discogs");
            var requestUri = $"database/search?q={Uri.EscapeDataString(query.Trim())}&type=release&per_page={normalizedLimit}";

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(requestUri, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Discogs search failed for {Query}", query);
                return StatusCode(StatusCodes.Status502BadGateway, "Discogs-Suche nicht erreichbar.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Discogs search returned {Status} for {Query}", response.StatusCode, query);
                return StatusCode((int)response.StatusCode, "Discogs-Suche fehlgeschlagen.");
            }

            DiscogsSearchResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<DiscogsSearchResponse>(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Discogs JSON parse failed for {Query}", query);
                return StatusCode(StatusCodes.Status502BadGateway, "Discogs-Antwort konnte nicht gelesen werden.");
            }

            if (payload?.Results == null || payload.Results.Count == 0)
            {
                return Ok(Array.Empty<DiscogsSearchItem>());
            }

            var results = payload.Results
                .Where(r => string.Equals(r.Type, "release", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(r.Type))
                .Take(normalizedLimit)
                .Select(MapToSearchItem)
                .ToList();

            return Ok(results);
        }

        // GET /SongApi/songs/{id}
        [HttpGet("songs/{id:int}")]
        public async Task<IActionResult> GetSong(int id)
        {
            var song = await (
                from r in _discogs.AppReleases.AsNoTracking()
                where r.Id == id
                join a in _discogs.AppArtists.AsNoTracking() on r.ArtistId equals a.Id into artistJoin
                from a in artistJoin.DefaultIfEmpty()
                select new { Release = r, ArtistName = a != null ? a.Name : string.Empty }
            ).FirstOrDefaultAsync();

            if (song is null) return NotFound();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return Ok(new Song
            {
                Id = song.Release.Id,
                Title = song.Release.Title ?? string.Empty,
                Artist = song.ArtistName ?? string.Empty,
                Duration = 0,
                AlbumArt = EnsureAbsolute(baseUrl, song.Release.CoverUrl),
                StreamUrl = string.Empty,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // POST /SongApi/songs
        [HttpPost("songs")]
        public async Task<IActionResult> AddSong([FromBody] Song song)
        {
            return BadRequest("Schreibzugriff auf discogs-App-Daten ist deaktiviert.");
        }

        private static string EnsureAbsolute(string baseUrl, string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            if (url.StartsWith("http://") || url.StartsWith("https://")) return url;
            return $"{baseUrl}{(url.StartsWith("/") ? "" : "/")}{url}";
        }

        private static DiscogsSearchItem MapToSearchItem(DiscogsSearchResult result)
        {
            var (artist, title) = SplitTitle(result.Title ?? string.Empty);

            return new DiscogsSearchItem
            {
                DiscogsId = result.Id.ToString(),
                Title = title,
                Artist = artist,
                Year = result.Year,
                Country = result.Country,
                CoverUrl = string.IsNullOrWhiteSpace(result.CoverImage) ? string.Empty : result.CoverImage,
                ResourceUrl = result.ResourceUrl
            };
        }

        private static (string Artist, string Title) SplitTitle(string rawTitle)
        {
            if (string.IsNullOrWhiteSpace(rawTitle)) return (string.Empty, string.Empty);
            var parts = rawTitle.Split(" - ", 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2
                ? (parts[0].Trim(), parts[1].Trim())
                : (string.Empty, rawTitle.Trim());
        }
    }
}
