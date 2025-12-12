using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Security;
using SetlistStudio.Web.Security;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SetlistStudio.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("ApiPolicy")]
[InputSanitization]
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
            var userId = SecureUserContext.GetSanitizedUserId(User);
            var (songs, totalCount) = await _songService.GetSongsAsync(userId);
            return Ok(new { songs, totalCount });
        }
        catch (UnauthorizedAccessException ex)
        {
            var sanitizedUserId = SecureUserContext.GetSanitizedUserId(User);
            _logger.LogWarning(ex, "Unauthorized access to songs for user {UserId}", sanitizedUserId);
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            var sanitizedUserId = SecureUserContext.GetSanitizedUserId(User);
            _logger.LogWarning(ex, "Invalid argument while retrieving songs for user {UserId}", sanitizedUserId);
            return BadRequest(new { error = "Invalid request parameters" });
        }
        catch (InvalidOperationException ex)
        {
            var sanitizedUserId = SecureUserContext.GetSanitizedUserId(User);
            _logger.LogError(ex, "Invalid operation while retrieving songs for user {UserId}", sanitizedUserId);
            return StatusCode(500, new { error = "Service temporarily unavailable" });
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            var sanitizedUserId = SecureUserContext.GetSanitizedUserId(User);
            _logger.LogError(ex, "Unexpected error retrieving songs for user {UserId}", sanitizedUserId);
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
            var userId = SecureUserContext.GetSanitizedUserId(User);
            var (songs, totalCount) = await _songService.GetSongsAsync(userId, searchTerm: query);
            return Ok(new { songs, totalCount });
        }
        catch (ArgumentException ex)
        {
            var sanitizedUserId = SecureUserContext.GetSanitizedUserId(User);
            _logger.LogWarning(ex, "Invalid search query for user {UserId}", sanitizedUserId);
            return BadRequest(new { error = "Invalid search parameters" });
        }
        catch (InvalidOperationException ex)
        {
            var sanitizedUserId = SecureUserContext.GetSanitizedUserId(User);
            _logger.LogError(ex, "Search service unavailable for user {UserId}", sanitizedUserId);
            return StatusCode(503, new { error = "Search service temporarily unavailable" });
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            var sanitizedUserId = SecureUserContext.GetSanitizedUserId(User);
            _logger.LogError(ex, "Unexpected error searching songs for user {UserId}", sanitizedUserId);
            return StatusCode(500, new { error = "An error occurred while searching songs" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSong([FromBody] CreateSongRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = SecureUserContext.GetSanitizedUserId(User);
            var song = new Song
            {
                Title = request.Title,
                Artist = request.Artist,
                Bpm = request.Bpm,
                MusicalKey = request.Key,
                Genre = request.Genre,
                DurationSeconds = request.Duration.HasValue && request.Duration.Value.TotalSeconds > 0 ? (int)Math.Round(request.Duration.Value.TotalSeconds) : null,
                Notes = request.Notes,
                UserId = userId
            };

            var createdSong = await _songService.CreateSongAsync(song);
            return CreatedAtAction(nameof(GetSongs), new { id = createdSong.Id }, createdSong);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid song data provided");
            return BadRequest(new { error = "Invalid song data provided" });
        }
        catch (InvalidOperationException ex)
        {
            // Check if this is a duplicate song error
            if (ex.Message.Contains("already exists in your library"))
            {
                _logger.LogWarning(ex, "Duplicate song creation attempt");
                return Conflict(new { error = ex.Message });
            }
            _logger.LogError(ex, "Song service unavailable");
            return StatusCode(503, new { error = "Song service temporarily unavailable" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized song creation attempt");
            return Forbid();
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            // Log the actual exception for debugging but don't expose details to client
            _logger.LogError(ex, "Unexpected error creating song for user");
            return StatusCode(500, new { error = "An error occurred while creating the song" });
        }
    }

    [HttpPut("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSong(int id, [FromBody] CreateSongRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = SecureUserContext.GetSanitizedUserId(User);
            var song = new Song
            {
                Id = id,
                Title = request.Title,
                Artist = request.Artist,
                Bpm = request.Bpm,
                MusicalKey = request.Key,
                Genre = request.Genre,
                DurationSeconds = request.Duration.HasValue && request.Duration.Value.TotalSeconds > 0 ? (int)Math.Round(request.Duration.Value.TotalSeconds) : null,
                Notes = request.Notes,
                UserId = userId
            };

            var updatedSong = await _songService.UpdateSongAsync(song, userId);
            if (updatedSong == null)
            {
                return NotFound(new { error = "Song not found" });
            }
            return Ok(updatedSong);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid song data provided");
            return BadRequest(new { error = "Invalid song data provided" });
        }
        catch (InvalidOperationException ex)
        {
            // Check if this is a duplicate song error
            if (ex.Message.Contains("already exists in your library"))
            {
                _logger.LogWarning(ex, "Duplicate song update attempt");
                return Conflict(new { error = ex.Message });
            }
            _logger.LogError(ex, "Song service unavailable");
            return StatusCode(503, new { error = "Song service temporarily unavailable" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized song update attempt");
            return Forbid();
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            // Log the actual exception for debugging but don't expose details to client
            _logger.LogError(ex, "Unexpected error updating song for user");
            return StatusCode(500, new { error = "An error occurred while updating the song" });
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
    [SafeString(MaxLength = 200, AllowEmpty = false)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [SafeString(MaxLength = 100, AllowEmpty = false)]
    public string Artist { get; set; } = string.Empty;

    [SafeBpm(MinBpm = 40, MaxBpm = 250)]
    public int? Bpm { get; set; }

    [StringLength(10)]
    [MusicalKey]
    public string? Key { get; set; }

    [StringLength(50)]
    [SafeString(MaxLength = 50, AllowEmpty = true)]
    public string? Genre { get; set; }

    public TimeSpan? Duration { get; set; }

    [StringLength(1000)]
    [SafeString(MaxLength = 1000, AllowEmpty = true)]
    public string? Notes { get; set; }
}