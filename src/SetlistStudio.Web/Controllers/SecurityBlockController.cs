using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// Controller to explicitly handle and block access to sensitive endpoints.
/// Returns 404 Not Found for security-sensitive paths that should not exist.
/// </summary>
[Route("")]
public class SecurityBlockController : Controller
{
    /// <summary>
    /// Block access to sensitive admin endpoints
    /// </summary>
    [HttpGet("admin")]
    [HttpGet("api/admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return NotFound();
    }

    /// <summary>
    /// Block access to configuration endpoints
    /// </summary>
    [HttpGet("config")]
    [HttpGet(".env")]
    public IActionResult Config()
    {
        return NotFound();
    }

    /// <summary>
    /// Block access to debug endpoints
    /// </summary>
    [HttpGet("debug")]
    [HttpGet("trace.axd")]
    [HttpGet("elmah.axd")]
    public IActionResult Debug()
    {
        return NotFound();
    }
}