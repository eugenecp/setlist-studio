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

    /// <summary>
    /// TDD Step 7 (GREEN): Added pagination validation
    /// Filters songs by genre with input and pagination validation
    /// </summary>
    [HttpGet("genre/{genre}")]
    public async Task<IActionResult> GetSongsByGenre(
        [FromRoute] string genre,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // TDD Cycle 2: Add validation for null/whitespace genre
        if (string.IsNullOrWhiteSpace(genre))
        {
            return BadRequest(new { error = "Genre parameter is required" });
        }

        // TDD Cycle 3: Add pagination validation
        if (page < 1)
        {
            return BadRequest(new { error = "Page number must be greater than 0" });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { error = "Page size must be between 1 and 100" });
        }

        // TDD Cycle 4: Add malicious content validation
        if (ContainsMaliciousContent(genre))
        {
            return BadRequest(new { error = "Invalid genre parameter" });
        }

        try
        {
            var userId = SecureUserContext.GetSanitizedUserId(User);
            var (songs, totalCount) = await _songService.GetSongsAsync(
                userId,
                genre: genre.Trim(),
                pageNumber: page,
                pageSize: pageSize);

            return Ok(new { songs, totalCount });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized song creation attempt");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Song service unavailable");
            return StatusCode(503, new { error = "Song service temporarily unavailable" });
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            // Log the actual exception for debugging but don't expose details to client
            _logger.LogError(ex, "Unexpected error creating song for user");
            return StatusCode(500, new { error = "An error occurred while creating the song" });
        }
    }

    private static bool ContainsMaliciousContent(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var maliciousPatterns = new[]
        {
            // XSS patterns
            "<script", "</script>", "javascript:", "vbscript:",
            "onload=", "onerror=", "onclick=", "onmouseover=",
            "<iframe", "<object", "<embed",
            // SQL injection patterns
            "UNION SELECT", "DROP TABLE", "DROP ", "DELETE FROM", "DELETE ", 
            "INSERT INTO", "UPDATE ", "'; DROP", "--", "/*", "*/",
            "' OR '", "\" OR \"", "OR 1=1", "OR '1'='1",
            // SQL Server command injection patterns
            "xp_", "sp_executesql", "sp_", ";--", "; DROP", "; DELETE", 
            "; UPDATE", "; INSERT",
            // OS Command injection patterns
            "&&", "||", "; ", "| ", "$(", "${", 
            "powershell", "bash", "sh ", "cmd ", "/bin/", "rm -rf",
            // Additional special characters used in injection
            "alert(", "fromCharCode"
        };

        // Also check for backticks separately since they can be tricky in strings
        if (input.Contains('`'))
            return true;

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