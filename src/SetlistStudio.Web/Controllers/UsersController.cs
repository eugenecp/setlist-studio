using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// API controller for user profile operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
[EnableRateLimiting("ApiPolicy")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;

    public UsersController(ILogger<UsersController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get the authenticated user's profile
    /// </summary>
    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        try
        {
            var userId = User.Identity?.Name ?? "anonymous";
            _logger.LogInformation("Retrieving profile for user {UserId}", userId);

            return Ok(new 
            { 
                userId = userId,
                name = User.Identity?.Name,
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, "Internal server error");
        }
    }
}