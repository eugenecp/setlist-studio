using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using SetlistStudio.Web.Attributes;

namespace SetlistStudio.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
[EnableRateLimiting(RateLimitPolicies.AuthenticatedApi)] // Enhanced rate limiting for authenticated API calls
[SecurityRateLimitConfig(EnableSecurityLogging = true, BlockSuspiciousUserAgents = true)]
public class ArtistsController : ControllerBase
{
    [HttpGet("search")]
    [Authorize] // Require authentication for consistency and security
    public IActionResult SearchArtists([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { error = "Name parameter is required" });
        }

        // Basic XSS protection - sanitize input
        if (ContainsMaliciousContent(name))
        {
            return BadRequest(new { error = "Invalid search query" });
        }

        try
        {
            // Simulate artist search (in real implementation, this would query a database)
            var artists = new[]
            {
                new { Id = 1, Name = "Queen", Albums = 15, Songs = 180 },
                new { Id = 2, Name = "The Beatles", Albums = 12, Songs = 213 },
                new { Id = 3, Name = "Led Zeppelin", Albums = 8, Songs = 88 },
                new { Id = 4, Name = "The Rolling Stones", Albums = 20, Songs = 300 }
            }.Where(a => a.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

            return Ok(new { artists, query = name, totalCount = artists.Count() });
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception)
        {
            return StatusCode(500, new { error = "An error occurred while searching artists" });
        }
    }

    [HttpGet]
    [Authorize] // Require authentication for full artist listing
    public IActionResult GetArtists()
    {
        try
        {
            // Simulate getting all artists
            var artists = new[]
            {
                new { Id = 1, Name = "Queen", Albums = 15, Songs = 180 },
                new { Id = 2, Name = "The Beatles", Albums = 12, Songs = 213 },
                new { Id = 3, Name = "Led Zeppelin", Albums = 8, Songs = 88 },
                new { Id = 4, Name = "Pink Floyd", Albums = 10, Songs = 120 },
                new { Id = 5, Name = "The Rolling Stones", Albums = 20, Songs = 300 }
            };

            return Ok(new { artists, totalCount = artists.Length });
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving artists" });
        }
    }

    private static bool ContainsMaliciousContent(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var maliciousPatterns = new[]
        {
            "<script", "</script>", "javascript:", "vbscript:",
            "onload=", "onerror=", "onclick=", "onmouseover=", "onfocus=",
            "UNION SELECT", "DROP TABLE", "DELETE FROM", "INSERT INTO",
            "'; DROP", "--", "/*", "*/"
        };

        return maliciousPatterns.Any(pattern => 
            input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}