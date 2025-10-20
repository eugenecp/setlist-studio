using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// Test controller for integration tests.
/// Used by SessionSecurityTests and other test suites.
/// Only available in Development and Test environments.
/// </summary>
[Route("api")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public TestController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Test endpoint that creates session state to trigger cookie creation.
    /// Used by SessionSecurityTests to validate cookie security attributes.
    /// </summary>
    [HttpPost("test-session")]
    public async Task<IActionResult> CreateTestSession([FromBody] string? content)
    {
        // Only allow in development and test environments (including test factories using Production/Staging)
        if (!_environment.IsDevelopment() && 
            _environment.EnvironmentName != "Test" &&
            _environment.EnvironmentName != "Testing" &&     // Allow for CI Testing environment
            _environment.EnvironmentName != "Production" &&  // Allow for test factories
            _environment.EnvironmentName != "Staging")       // Allow for test factories
        {
            return NotFound();
        }

        // Create session state to trigger cookie creation
        HttpContext.Session.SetString("TestKey", content ?? "test-value");
        await HttpContext.Session.CommitAsync();

        return Ok(new { message = "Session created", sessionId = HttpContext.Session.Id });
    }

    /// <summary>
    /// Test endpoint that requires authentication to test auth cookies.
    /// Used by SessionSecurityTests to validate authentication cookie security.
    /// </summary>
    [HttpGet("test-auth")]
    [Authorize]
    public IActionResult TestAuthEndpoint()
    {
        // Only allow in development and test environments (including test factories using Production/Staging)
        if (!_environment.IsDevelopment() && 
            _environment.EnvironmentName != "Test" &&
            _environment.EnvironmentName != "Testing" &&     // Allow for CI Testing environment
            _environment.EnvironmentName != "Production" &&  // Allow for test factories
            _environment.EnvironmentName != "Staging")       // Allow for test factories
        {
            return NotFound();
        }

        return Ok(new { message = "Authenticated", user = User.Identity?.Name });
    }

    /// <summary>
    /// Test endpoint that creates antiforgery token to test antiforgery cookies.
    /// Used by SessionSecurityTests to validate antiforgery cookie security.
    /// </summary>
    [HttpGet("test-antiforgery")]
    public IActionResult TestAntiforgery()
    {
        // Only allow in development and test environments (including test factories using Production/Staging)
        if (!_environment.IsDevelopment() && 
            _environment.EnvironmentName != "Test" &&
            _environment.EnvironmentName != "Testing" &&     // Allow for CI Testing environment
            _environment.EnvironmentName != "Production" &&  // Allow for test factories
            _environment.EnvironmentName != "Staging")       // Allow for test factories
        {
            return NotFound();
        }

        // This will trigger antiforgery token creation
        var token = HttpContext.RequestServices
            .GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>()
            .GetAndStoreTokens(HttpContext);

        return Ok(new { token = token.RequestToken });
    }
}