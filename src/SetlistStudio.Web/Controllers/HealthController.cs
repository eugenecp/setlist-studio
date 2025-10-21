using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SetlistStudio.Infrastructure.Data;

namespace SetlistStudio.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[EnableRateLimiting("ApiPolicy")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly SetlistStudioDbContext? _context;

    public HealthController(ILogger<HealthController> logger, SetlistStudioDbContext? context = null)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var healthStatus = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "Setlist Studio",
            Version = "1.0.0",
            Database = await CheckDatabaseHealth()
        };

        _logger.LogInformation("Health check requested - Status: {Status}, Database: {Database}", 
            healthStatus.Status, healthStatus.Database);
        
        return Ok(healthStatus);
    }

    [HttpGet("simple")]
    public IActionResult GetSimple()
    {
        // Simple health check without database dependency
        var healthStatus = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "Setlist Studio",
            Version = "1.0.0"
        };

        _logger.LogInformation("Simple health check requested - Status: Healthy");
        
        return Ok(healthStatus);
    }

    private async Task<string> CheckDatabaseHealth()
    {
        try
        {
            if (_context is null)
            {
                return "Database context not available";
            }
            
            await _context.Database.CanConnectAsync();
            return "Connected";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Database configuration invalid during health check");
            return "Database configuration error";
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Database connection timeout during health check");
            return "Database connection timeout";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error during database health check");
            return "Database connection failed";
        }
    }
}