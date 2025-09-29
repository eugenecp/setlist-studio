using Microsoft.AspNetCore.Mvc;

namespace SetlistStudio.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var healthStatus = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "Setlist Studio",
            Version = "1.0.0"
        };

        _logger.LogInformation("Health check requested - Status: Healthy");
        
        return Ok(healthStatus);
    }
}