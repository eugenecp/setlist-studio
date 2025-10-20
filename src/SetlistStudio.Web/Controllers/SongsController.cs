using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SetlistStudio.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("ApiPolicy")]
public class SongsController : ControllerBase
{
    private readonly ISongService _songService;
    private readonly ILogger<SongsController> _logger;

    public SongsController(ISongService songService, ILogger<SongsController> logger)
    {
        _songService = songService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSongs()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "anonymous";
            var (songs, totalCount) = await _songService.GetSongsAsync(userId);
            return Ok(new { songs, totalCount });
        }
        catch (Exception ex)
        {
            // Log the actual exception for debugging but don't expose details to client
            _logger.LogError(ex, "Error retrieving songs for user");
            return StatusCode(500, new { error = "An error occurred while retrieving songs" });
        }
    }

    [HttpGet("search")]
    // Removed AllowAnonymous - require authentication for search endpoint
    public async Task<IActionResult> SearchSongs([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Query parameter is required" });
        }

        // Basic XSS protection - sanitize input
        if (ContainsMaliciousContent(query))
        {
            return BadRequest(new { error = "Invalid search query" });
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "anonymous";
            var (songs, totalCount) = await _songService.GetSongsAsync(userId, searchTerm: query);
            return Ok(new { songs, totalCount });
        }
        catch (Exception ex)
        {
            // Log the actual exception for debugging but don't expose details to client
            _logger.LogError(ex, "Error searching songs for user");
            return StatusCode(500, new { error = "An error occurred while searching songs" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateSong([FromBody] CreateSongRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "anonymous";
            var song = new Song
            {
                Title = request.Title,
                Artist = request.Artist,
                Bpm = request.Bpm,
                MusicalKey = request.Key,
                Genre = request.Genre,
                DurationSeconds = request.Duration?.TotalSeconds > 0 ? (int)Math.Round(request.Duration.Value.TotalSeconds) : null,
                Notes = request.Notes,
                UserId = userId
            };

            var createdSong = await _songService.CreateSongAsync(song);
            return CreatedAtAction(nameof(GetSongs), new { id = createdSong.Id }, createdSong);
        }
        catch (Exception ex)
        {
            // Log the actual exception for debugging but don't expose details to client
            _logger.LogError(ex, "Error creating song for user");
            return StatusCode(500, new { error = "An error occurred while creating the song" });
        }
    }

    private static bool ContainsMaliciousContent(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var maliciousPatterns = new[]
        {
            "<script", "</script>", "javascript:", "vbscript:",
            "onload=", "onerror=", "onclick=", "onmouseover=",
            "UNION SELECT", "DROP TABLE", "DELETE FROM", "INSERT INTO",
            "'; DROP", "--", "/*", "*/"
        };

        return maliciousPatterns.Any(pattern => 
            input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

public class CreateSongRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Artist { get; set; } = string.Empty;

    [Range(40, 250, ErrorMessage = "BPM must be between 40 and 250")]
    public int? Bpm { get; set; }

    [StringLength(10)]
    public string? Key { get; set; }

    [StringLength(50)]
    public string? Genre { get; set; }

    public TimeSpan? Duration { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}