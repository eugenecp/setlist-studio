using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// MVC controller for song management pages.
/// These routes are used primarily for security testing and backward compatibility.
/// </summary>
[Authorize]
[Route("Songs")]
public class SongsPageController : Controller
{
    /// <summary>
    /// GET: /Songs/Create
    /// Returns the create song page (requires authentication)
    /// </summary>
    [HttpGet("Create")]
    public IActionResult Create()
    {
        // Check if user is authenticated
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Challenge(); // This will trigger a redirect to login
        }
        
        // This route requires authentication
        // In real implementation, this would return a view or redirect to Blazor page
        return Ok("Create song page (authenticated)");
    }

    /// <summary>
    /// GET: /Songs
    /// Returns the songs list page (requires authentication)
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        return Ok("Songs index page (authenticated)");
    }
}