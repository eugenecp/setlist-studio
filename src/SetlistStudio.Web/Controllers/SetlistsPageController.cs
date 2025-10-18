using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// MVC controller for setlist management pages.
/// These routes are used primarily for security testing and backward compatibility.
/// </summary>
[Authorize]
[Route("Setlists")]
public class SetlistsPageController : Controller
{
    /// <summary>
    /// GET: /Setlists/Create
    /// Returns the create setlist page (requires authentication)
    /// </summary>
    [HttpGet("Create")]
    public IActionResult Create()
    {
        // This route requires authentication
        return Ok("Create setlist page (authenticated)");
    }

    /// <summary>
    /// GET: /Setlists
    /// Returns the setlists list page (requires authentication)
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        return Ok("Setlists index page (authenticated)");
    }
}