using Microsoft.AspNetCore.Mvc;

namespace SetlistStudio.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var status = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "Setlist Studio",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };
        
        return Ok(status);
    }
    
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { Status = "Pong", Timestamp = DateTime.UtcNow });
    }
}