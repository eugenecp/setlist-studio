using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Web.Security;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// Test controller to verify filtering & pagination pattern follows documented approach.
/// PATTERN: Server-side genre filtering with offset pagination
/// FIVE PRINCIPLES: Works, Secure, Scales, Maintainable, User Delight
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("ApiPolicy")]
[InputSanitization]
public class GenresControllerTest : ControllerBase
{
    private readonly ISongService _songService;
    private readonly ILogger<GenresControllerTest> _logger;

    public GenresControllerTest(ISongService songService, ILogger<GenresControllerTest> logger)
    {
        _songService = songService;
        _logger = logger;
    }

    /// <summary>
    /// Get songs filtered by genre with pagination.
    /// SECURITY: Requires authentication and rate limiting
    /// WORKS: Returns filtered, paginated results with stable ordering
    /// SCALES: Pagination clamped to max 100 items per page; uses AsNoTracking() for performance
    /// </summary>
    [HttpGet("songs")]
    public async Task<IActionResult> GetGenreSongs(
        [FromQuery] string? genre = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // SECURITY: Extract and validate user identity
            var userId = SecureUserContext.GetSanitizedUserId(User);

            // WORKS: Call service with filter and pagination parameters
            var (songs, totalCount) = await _songService.GetSongsAsync(
                userId,
                genre: genre,
                pageNumber: pageNumber,
                pageSize: pageSize);

            // WORKS: Include pagination metadata in response
            Response.Headers["X-Total-Count"] = totalCount.ToString();

            return Ok(new
            {
                songs,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                hasNext = pageNumber < Math.Ceiling(totalCount / (double)pageSize),
                hasPrevious = pageNumber > 1
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var sanitizedUserId = SecureUserContext.GetSanitizedUserId(User);
            _logger.LogWarning(ex, "Unauthorized access to genre songs for user {UserId}", sanitizedUserId);
            return Forbid();
        }
        catch (Exception ex)
        {
            var sanitizedUserId = SecureUserContext.GetSanitizedUserId(User);
            _logger.LogError(ex, "Unexpected error retrieving genre songs for user {UserId}", sanitizedUserId);
            return StatusCode(500, new { error = "An error occurred while retrieving genre songs" });
        }
    }
}
