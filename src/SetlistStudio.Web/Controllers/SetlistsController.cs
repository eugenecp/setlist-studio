using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Security;
using SetlistStudio.Web.Models;
using SetlistStudio.Web.Security;
using System.ComponentModel.DataAnnotations;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// API controller for setlist management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
[EnableRateLimiting("ApiPolicy")]
[InputSanitization]
public class SetlistsController : ControllerBase
{
    private readonly ISetlistService _setlistService;
    private readonly ILogger<SetlistsController> _logger;

    public SetlistsController(ISetlistService setlistService, ILogger<SetlistsController> logger)
    {
        _setlistService = setlistService;
        _logger = logger;
    }

    /// <summary>
    /// Get setlists for the authenticated user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SetlistResponse>>> GetSetlists(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        try
        {
            var userId = User.Identity?.Name ?? "anonymous";
            _logger.LogInformation("Retrieving setlists for user {UserId} (page {Page})", userId, page);

            var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(userId, pageNumber: page, pageSize: limit);
            var response = setlists.Select(s => new SetlistResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CreatedDate = s.CreatedAt,
                SongCount = s.SetlistSongs?.Count ?? 0
            });

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to setlists");
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for setlist retrieval");
            return BadRequest("Invalid request parameters");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Service unavailable for setlist retrieval");
            return StatusCode(503, "Service temporarily unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving setlists");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Search setlists by name
    /// </summary>
    [HttpGet("search")]
    [Authorize] // Require authentication for setlist search to protect user data
    public async Task<ActionResult<IEnumerable<SetlistResponse>>> SearchSetlists(
        [FromQuery, Required] string query,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        try
        {
            // Check for malicious content
            if (ContainsMaliciousContent(query))
            {
                var sanitizedQuery = SecureLoggingHelper.SanitizeMessage(query);
                _logger.LogWarning("Malicious content detected in setlist search query: {Query}", sanitizedQuery);
                return BadRequest("Invalid search query");
            }

            var userId = User.Identity?.Name ?? "anonymous";
            var sanitizedUserId = SecureUserContext.GetSanitizedUserId(User);
            var sanitizedSearchQuery = SecureLoggingHelper.SanitizeMessage(query);
            _logger.LogInformation("Searching setlists for user {UserId} with query '{Query}' (page {Page})", sanitizedUserId, sanitizedSearchQuery, page);

            var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(userId, searchTerm: query, pageNumber: page, pageSize: limit);
            var response = setlists.Select(s => new SetlistResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CreatedDate = s.CreatedAt,
                SongCount = s.SetlistSongs?.Count ?? 0
            });

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid search query for setlists");
            return BadRequest("Invalid search parameters");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Search service unavailable for setlists");
            return StatusCode(503, "Search service temporarily unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error searching setlists");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new setlist
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<SetlistResponse>> CreateSetlist([FromBody] CreateSetlistRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check for malicious content
            if (ContainsMaliciousContent(request.Name) || ContainsMaliciousContent(request.Description))
            {
                _logger.LogWarning("Malicious content detected in setlist creation request");
                return BadRequest("Invalid setlist data");
            }

            var userId = User.Identity?.Name ?? "anonymous";
            var sanitizedUserId = SecureUserContext.GetSanitizedUserId(User);
            var sanitizedName = SecureLoggingHelper.SanitizeMessage(request.Name);
            _logger.LogInformation("Creating setlist '{Name}' for user {UserId}", sanitizedName, sanitizedUserId);

            var setlist = new Setlist
            {
                Name = request.Name,
                Description = request.Description,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            var createdSetlist = await _setlistService.CreateSetlistAsync(setlist);
            var response = new SetlistResponse
            {
                Id = createdSetlist.Id,
                Name = createdSetlist.Name,
                Description = createdSetlist.Description,
                CreatedDate = createdSetlist.CreatedAt,
                SongCount = 0
            };

            return CreatedAtAction(nameof(GetSetlist), new { id = createdSetlist.Id }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid setlist data provided");
            return BadRequest("Invalid setlist data");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized setlist creation attempt");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Setlist service unavailable");
            return StatusCode(503, "Service temporarily unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating setlist");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific setlist by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SetlistResponse>> GetSetlist(int id)
    {
        try
        {
            var userId = User.Identity?.Name ?? "anonymous";
            var setlist = await _setlistService.GetSetlistByIdAsync(id, userId);
            
            if (setlist == null)
            {
                return NotFound();
            }

            var response = new SetlistResponse
            {
                Id = setlist.Id,
                Name = setlist.Name,
                Description = setlist.Description,
                CreatedDate = setlist.CreatedAt,
                SongCount = setlist.SetlistSongs?.Count ?? 0
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid setlist ID {SetlistId}", id);
            return BadRequest($"Invalid setlist ID: {id}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to setlist {SetlistId}", id);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Service unavailable for setlist {SetlistId}", id);
            return StatusCode(503, "Service temporarily unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving setlist {SetlistId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Check if content contains malicious patterns
    /// </summary>
    private static bool ContainsMaliciousContent(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        var maliciousPatterns = new[]
        {
            "<script", "javascript:", "vbscript:", "onload=", "onerror=",
            "alert(", "document.cookie", "window.location", "eval(",
            "base64,", "data:text/html", "<iframe", "<object", "<embed"
        };

        return maliciousPatterns.Any(pattern => 
            content.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}