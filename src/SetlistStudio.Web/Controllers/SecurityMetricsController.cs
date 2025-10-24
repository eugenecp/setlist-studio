using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SetlistStudio.Core.Security;
using SetlistStudio.Web.Services;
using System.Diagnostics;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// Security metrics and monitoring dashboard controller.
/// Provides endpoints for retrieving security metrics, monitoring authentication failures,
/// rate limit violations, and suspicious activities for operational visibility.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")] // Only administrators can access security metrics
[EnableRateLimiting("AdminPolicy")] // Stricter rate limiting for admin endpoints
public class SecurityMetricsController : ControllerBase
{
    private readonly ISecurityMetricsService _securityMetricsService;
    private readonly ILogger<SecurityMetricsController> _logger;
    private readonly IConfiguration _configuration;

    public SecurityMetricsController(
        ISecurityMetricsService securityMetricsService,
        ILogger<SecurityMetricsController> logger,
        IConfiguration configuration)
    {
        _securityMetricsService = securityMetricsService ?? throw new ArgumentNullException(nameof(securityMetricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Gets a real-time snapshot of current security metrics.
    /// </summary>
    /// <returns>Current security metrics including recent events and threat indicators</returns>
    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(SecurityMetricsSnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<SecurityMetricsSnapshot> GetSnapshot()
    {
        try
        {
            _logger.LogInformation("Security metrics snapshot requested by user {UserId}", 
                User.Identity?.Name ?? "Unknown");

            var snapshot = _securityMetricsService.GetMetricsSnapshot();
            return Ok(snapshot);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to security metrics by user {UserId}", 
                User.Identity?.Name ?? "Unknown");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Security metrics service unavailable");
            return StatusCode(503, "Security metrics service temporarily unavailable");
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving security metrics snapshot");
            return StatusCode(500, "Error retrieving security metrics");
        }
    }

    /// <summary>
    /// Gets detailed security metrics for a specified time range.
    /// </summary>
    /// <param name="startTime">Start time for metrics (UTC). Defaults to 24 hours ago.</param>
    /// <param name="endTime">End time for metrics (UTC). Defaults to now.</param>
    /// <returns>Detailed security metrics with analysis and trends</returns>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(DetailedSecurityMetrics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<DetailedSecurityMetrics> GetDetailedMetrics(
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        try
        {
            // Validate time range
            if (startTime.HasValue && endTime.HasValue && startTime > endTime)
            {
                return BadRequest("Start time cannot be after end time");
            }

            // Limit historical data to prevent performance issues
            var maxHistoryDays = _configuration.GetValue<int>("Security:MaxHistoryDays", 30);
            var earliestAllowed = DateTime.UtcNow.AddDays(-maxHistoryDays);
            
            if (startTime.HasValue && startTime < earliestAllowed)
            {
                startTime = earliestAllowed;
            }

            _logger.LogInformation("Detailed security metrics requested by user {UserId} for period {StartTime} to {EndTime}", 
                User.Identity?.Name ?? "Unknown", startTime, endTime);

            var metrics = _securityMetricsService.GetDetailedMetrics(startTime, endTime);
            return Ok(metrics);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid date range for security metrics");
            return BadRequest("Invalid date range specified");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Security metrics service unavailable");
            return StatusCode(503, "Security metrics service temporarily unavailable");
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving detailed security metrics");
            return StatusCode(500, "Error retrieving detailed security metrics");
        }
    }

    /// <summary>
    /// Gets security metrics for a specific time period (last hour, day, week, etc.).
    /// </summary>
    /// <param name="period">Time period in hours (1, 24, 168 for hour, day, week)</param>
    /// <returns>Security metrics and analysis for the specified period</returns>
    [HttpGet("period/{period:int}")]
    [ProducesResponseType(typeof(SecurityMetricsPeriod), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<SecurityMetricsPeriod> GetMetricsForPeriod(int period)
    {
        try
        {
            // Validate period
            if (period <= 0 || period > 8760) // Max 1 year
            {
                return BadRequest("Period must be between 1 and 8760 hours (1 year)");
            }

            var timeSpan = TimeSpan.FromHours(period);
            
            _logger.LogInformation("Security metrics for period {Period} hours requested by user {UserId}", 
                period, User.Identity?.Name ?? "Unknown");

            var metrics = _securityMetricsService.GetMetricsForPeriod(timeSpan);
            return Ok(metrics);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid period value {Period} for security metrics", period);
            return BadRequest("Invalid period specified");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Security metrics service unavailable for period {Period}", period);
            return StatusCode(503, "Security metrics service temporarily unavailable");
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving security metrics for period {Period}", period);
            return StatusCode(500, "Error retrieving security metrics for period");
        }
    }

    /// <summary>
    /// Gets security dashboard data optimized for real-time monitoring.
    /// </summary>
    /// <returns>Dashboard data including key metrics, alerts, and status indicators</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(SecurityDashboard), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<SecurityDashboard> GetSecurityDashboard()
    {
        try
        {
            _logger.LogInformation("Security dashboard requested by user {UserId}", 
                User.Identity?.Name ?? "Unknown");

            var snapshot = _securityMetricsService.GetMetricsSnapshot();
            var last24Hours = _securityMetricsService.GetMetricsForPeriod(TimeSpan.FromHours(24));
            var lastHour = _securityMetricsService.GetMetricsForPeriod(TimeSpan.FromHours(1));

            var dashboard = new SecurityDashboard
            {
                Timestamp = DateTime.UtcNow,
                ThreatLevel = last24Hours.ThreatLevel,
                SecurityScore = Math.Round(last24Hours.SecurityScore, 1),
                ActiveThreats = snapshot.RecentEvents.Count(e => 
                    e.Severity == "CRITICAL" || e.Severity == "HIGH"),
                
                // Key metrics
                AuthFailuresLast24h = snapshot.RecentEvents.Count(e => 
                    e.EventType == "AUTHENTICATION_FAILURE" && 
                    e.Timestamp >= DateTime.UtcNow.AddHours(-24)),
                    
                RateLimitViolationsLast24h = snapshot.RecentEvents.Count(e => 
                    e.EventType == "RATE_LIMIT_VIOLATION" && 
                    e.Timestamp >= DateTime.UtcNow.AddHours(-24)),
                    
                SuspiciousActivitiesLast24h = snapshot.RecentEvents.Count(e => 
                    e.EventType == "SUSPICIOUS_ACTIVITY" && 
                    e.Timestamp >= DateTime.UtcNow.AddHours(-24)),

                // Trends
                AuthFailureTrend = CalculateTrend(snapshot.RecentEvents, "AUTHENTICATION_FAILURE"),
                RateLimitTrend = CalculateTrend(snapshot.RecentEvents, "RATE_LIMIT_VIOLATION"),
                ThreatTrend = CalculateOverallThrend(lastHour, last24Hours),

                // Top threats
                TopAttackingIPs = snapshot.TopFailingIps.Take(5).ToArray(),
                TopViolatedEndpoints = snapshot.TopViolatedEndpoints.Take(5).ToArray(),
                
                // Alerts and recommendations
                ActiveAlerts = GenerateActiveAlerts(snapshot, last24Hours),
                Recommendations = last24Hours.Recommendations,
                
                // System status
                SystemStatus = DetermineSystemStatus(last24Hours.ThreatLevel, last24Hours.SecurityScore),
                LastIncidentTime = snapshot.RecentEvents
                    .Where(e => e.Severity == "CRITICAL" || e.Severity == "HIGH")
                    .OrderByDescending(e => e.Timestamp)
                    .FirstOrDefault()?.Timestamp,
                
                // Operational metrics
                MonitoringStatus = "ACTIVE",
                AlertsEnabled = _configuration.GetValue<bool>("Security:AlertsEnabled", true),
                MetricsRetentionDays = _configuration.GetValue<int>("Security:MetricsRetentionDays", 7)
            };

            return Ok(dashboard);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to security dashboard by user {UserId}", User.Identity?.Name ?? "Unknown");
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in security dashboard request");
            return BadRequest("Invalid request parameters");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Security metrics service unavailable");
            return StatusCode(503, "Security metrics service temporarily unavailable");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout retrieving security dashboard data");
            return StatusCode(504, "Request timeout - please try again");
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving security dashboard");
            return StatusCode(500, "Error retrieving security dashboard");
        }
    }

    /// <summary>
    /// Health check endpoint for security monitoring service.
    /// </summary>
    /// <returns>Health status of the security monitoring system</returns>
    [HttpGet("health")]
    [AllowAnonymous] // Health checks should be accessible for monitoring
    [ProducesResponseType(typeof(SecurityMonitoringHealth), StatusCodes.Status200OK)]
    public ActionResult<SecurityMonitoringHealth> GetHealth()
    {
        try
        {
            var snapshot = _securityMetricsService.GetMetricsSnapshot();
            var isHealthy = DateTime.UtcNow - snapshot.LastEventTime < TimeSpan.FromMinutes(30);

            var health = new SecurityMonitoringHealth
            {
                Status = isHealthy ? "Healthy" : "Warning",
                Timestamp = DateTime.UtcNow,
                MetricsCollectionActive = true,
                LastEventTime = snapshot.LastEventTime,
                TotalEventsProcessed = snapshot.TotalEvents,
                MonitoringUptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                AlertingEnabled = _configuration.GetValue<bool>("Security:AlertsEnabled", true),
                Details = isHealthy ? "Security monitoring is functioning normally" : 
                         "No recent security events detected - monitoring may need attention"
            };

            return Ok(health);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Security metrics service temporarily unavailable");
            return StatusCode(503, new SecurityMonitoringHealth
            {
                Status = "Degraded",
                Timestamp = DateTime.UtcNow,
                Details = "Security metrics service temporarily unavailable"
            });
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout checking security monitoring health");
            return StatusCode(504, new SecurityMonitoringHealth
            {
                Status = "Timeout",
                Timestamp = DateTime.UtcNow,
                Details = "Health check timeout - monitoring service may be overloaded"
            });
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking security monitoring health");
            return StatusCode(500, new SecurityMonitoringHealth
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Details = "Error checking security monitoring health"
            });
        }
    }

    /// <summary>
    /// Manually records a security event (for testing or external integration).
    /// </summary>
    /// <param name="request">Security event to record</param>
    /// <returns>Confirmation of event recording</returns>
    [HttpPost("events")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult RecordSecurityEvent([FromBody] RecordSecurityEventRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.EventType))
            {
                return BadRequest("Event type is required");
            }

            var sanitizedEventType = SecureLoggingHelper.PreventLogInjection(request.EventType);
            var sanitizedSeverity = SecureLoggingHelper.PreventLogInjection(request.Severity ?? "MEDIUM");
            var sanitizedDetails = SecureLoggingHelper.PreventLogInjection(request.Details ?? $"Manual event recorded by {User.Identity?.Name}");
            
            _securityMetricsService.RecordSecurityEvent(
                sanitizedEventType, 
                sanitizedSeverity, 
                sanitizedDetails);

            // Use TaintBarrier for complete taint isolation
            var safeLogMessage = TaintBarrier.CreateSafeLogMessage(
                "Manual security event recorded by user {0}: {1}",
                User.Identity?.Name ?? "Unknown", sanitizedEventType);
            _logger.LogInformation(safeLogMessage);

            return Created("", new { Message = "Security event recorded successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid security event data provided");
            return BadRequest("Invalid event data provided");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to record security event by user {UserId}", User.Identity?.Name ?? "Unknown");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Security event recording service unavailable");
            return StatusCode(503, "Security event recording temporarily unavailable");
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error recording manual security event");
            return StatusCode(500, "Error recording security event");
        }
    }

    private string CalculateTrend(SecurityEvent[] events, string eventType)
    {
        var last1Hour = events.Count(e => e.EventType == eventType && e.Timestamp >= DateTime.UtcNow.AddHours(-1));
        var previous1Hour = events.Count(e => e.EventType == eventType && 
                                           e.Timestamp >= DateTime.UtcNow.AddHours(-2) && 
                                           e.Timestamp < DateTime.UtcNow.AddHours(-1));

        if (previous1Hour == 0) return last1Hour > 0 ? "INCREASING" : "STABLE";
        
        var change = ((double)(last1Hour - previous1Hour) / previous1Hour) * 100;
        
        return change > 20 ? "INCREASING" : change < -20 ? "DECREASING" : "STABLE";
    }

    private string CalculateOverallThrend(SecurityMetricsPeriod lastHour, SecurityMetricsPeriod last24Hours)
    {
        var hourlyRate = lastHour.EventCount;
        var dailyAverageRate = last24Hours.EventCount / 24.0;
        
        return hourlyRate > dailyAverageRate * 1.5 ? "INCREASING" : 
               hourlyRate < dailyAverageRate * 0.5 ? "DECREASING" : "STABLE";
    }

    private string[] GenerateActiveAlerts(SecurityMetricsSnapshot snapshot, SecurityMetricsPeriod last24Hours)
    {
        var alerts = new List<string>();

        if (last24Hours.ThreatLevel == "CRITICAL")
        {
            alerts.Add("🔴 CRITICAL: High-severity security events detected in the last 24 hours");
        }

        if (last24Hours.AuthFailureRate > 10) // More than 10 per hour
        {
            alerts.Add("⚠️ HIGH: Elevated authentication failure rate detected");
        }

        if (last24Hours.RateLimitRate > 50) // More than 50 per hour
        {
            alerts.Add("⚠️ MEDIUM: High rate limit violation activity");
        }

        if (last24Hours.SecurityScore < 70)
        {
            alerts.Add("⚠️ LOW: Security score has dropped below acceptable threshold");
        }

        var recentCriticalEvents = snapshot.RecentEvents.Count(e => 
            e.Severity == "CRITICAL" && e.Timestamp >= DateTime.UtcNow.AddMinutes(-30));
        
        if (recentCriticalEvents > 0)
        {
            alerts.Add($"🔴 URGENT: {recentCriticalEvents} critical security event(s) in the last 30 minutes");
        }

        return alerts.ToArray();
    }

    private string DetermineSystemStatus(string threatLevel, double securityScore)
    {
        return threatLevel switch
        {
            "CRITICAL" => "UNDER_ATTACK",
            "HIGH" => "ELEVATED_RISK",
            "MEDIUM" when securityScore < 80 => "MONITORING_REQUIRED",
            _ => "SECURE"
        };
    }

    private string GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

#region Request/Response Models

public class RecordSecurityEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public string? Details { get; set; }
}

public class SecurityDashboard
{
    public DateTime Timestamp { get; set; }
    public string ThreatLevel { get; set; } = string.Empty;
    public double SecurityScore { get; set; }
    public int ActiveThreats { get; set; }
    
    // Key metrics
    public int AuthFailuresLast24h { get; set; }
    public int RateLimitViolationsLast24h { get; set; }
    public int SuspiciousActivitiesLast24h { get; set; }
    
    // Trends
    public string AuthFailureTrend { get; set; } = string.Empty;
    public string RateLimitTrend { get; set; } = string.Empty;
    public string ThreatTrend { get; set; } = string.Empty;
    
    // Top threats
    public string[] TopAttackingIPs { get; set; } = Array.Empty<string>();
    public string[] TopViolatedEndpoints { get; set; } = Array.Empty<string>();
    
    // Alerts and recommendations
    public string[] ActiveAlerts { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
    
    // System status
    public string SystemStatus { get; set; } = string.Empty;
    public DateTime? LastIncidentTime { get; set; }
    
    // Operational metrics
    public string MonitoringStatus { get; set; } = string.Empty;
    public bool AlertsEnabled { get; set; }
    public int MetricsRetentionDays { get; set; }
}

public class SecurityMonitoringHealth
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool MetricsCollectionActive { get; set; }
    public DateTime? LastEventTime { get; set; }
    public int TotalEventsProcessed { get; set; }
    public TimeSpan MonitoringUptime { get; set; }
    public bool AlertingEnabled { get; set; }
    public string Details { get; set; } = string.Empty;
}

#endregion