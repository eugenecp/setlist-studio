using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SetlistStudio.Core.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// Controller for handling Content Security Policy (CSP) violation reports.
/// Provides secure endpoint for browsers to report CSP violations for security monitoring.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // CSP reports come from browsers, not authenticated users
[EnableRateLimiting("ApiPolicy")] // Prevent abuse of reporting endpoint
public class CspReportController : ControllerBase
{
    private readonly ILogger<CspReportController> _logger;
    private readonly IConfiguration _configuration;

    public CspReportController(ILogger<CspReportController> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Receives and processes Content Security Policy violation reports from browsers.
    /// </summary>
    /// <param name="report">The CSP violation report from the browser</param>
    /// <returns>HTTP 204 No Content on successful processing</returns>
    [HttpPost("report")]
    [Consumes("application/csp-report", "application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Report([FromBody] CspViolationReport report)
    {
        try
        {
            // Validate the report
            if (report?.CspReport == null)
            {
                _logger.LogWarning("Received invalid CSP report: Report or CspReport is null");
                return BadRequest("Invalid CSP report format");
            }

            // Check if CSP reporting is enabled
            var isEnabled = _configuration.GetValue<bool>("Security:CspReporting:Enabled", true);
            if (!isEnabled)
            {
                _logger.LogDebug("CSP reporting is disabled, ignoring report");
                return NoContent();
            }

            // Get client information for security analysis
            var clientIp = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();
            var referer = Request.Headers.Referer.ToString();

            // Log the CSP violation with security context
            LogCspViolation(report.CspReport, clientIp, userAgent, referer);

            // Check for suspicious patterns
            await AnalyzeCspViolationAsync(report.CspReport, clientIp);

            return NoContent();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse CSP report JSON");
            return BadRequest("Invalid JSON format");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid CSP report data received");
            return BadRequest("Invalid report data");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "CSP reporting service unavailable");
            return StatusCode(503, "CSP reporting service temporarily unavailable");
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing CSP violation report");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Health check endpoint for CSP reporting service.
    /// </summary>
    /// <returns>Service status information</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        var isEnabled = _configuration.GetValue<bool>("Security:CspReporting:Enabled", true);
        var response = new
        {
            Status = "Healthy",
            Service = "CSP Reporting",
            Enabled = isEnabled,
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    private void LogCspViolation(CspReport cspReport, string clientIp, string userAgent, string referer)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "CSP_VIOLATION",
            ["ClientIP"] = clientIp,
            ["UserAgent"] = userAgent,
            ["Referer"] = referer,
            ["Timestamp"] = DateTime.UtcNow
        });

        var messageBuilder = new StringBuilder();
        messageBuilder.Append("CSP Violation Detected: {DirectiveViolated} blocked {ViolatedDirective} from {SourceFile} at line {LineNumber}. ");
        messageBuilder.Append("Blocked URI: {BlockedUri}, Document URI: {DocumentUri}");
        _logger.LogWarning(messageBuilder.ToString(),
            cspReport.ViolatedDirective,
            cspReport.EffectiveDirective,
            cspReport.SourceFile,
            cspReport.LineNumber,
            cspReport.BlockedUri,
            cspReport.DocumentUri);
    }

    private async Task AnalyzeCspViolationAsync(CspReport cspReport, string clientIp)
    {
        // Check for suspicious patterns that might indicate attacks
        var suspiciousPatterns = new[]
        {
            "javascript:",
            "data:",
            "vbscript:",
            "eval(",
            "innerHTML",
            "document.write",
            "onerror",
            "onload"
        };

        var blockedUri = cspReport.BlockedUri?.ToLowerInvariant() ?? string.Empty;
        var sourceFile = cspReport.SourceFile?.ToLowerInvariant() ?? string.Empty;

        var hasSuspiciousPattern = suspiciousPatterns.Any(pattern => 
            blockedUri.Contains(pattern) || sourceFile.Contains(pattern));

        if (hasSuspiciousPattern)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["EventType"] = "SUSPICIOUS_CSP_VIOLATION",
                ["ClientIP"] = clientIp,
                ["SeverityLevel"] = "HIGH",
                ["ThreatType"] = "POTENTIAL_XSS_ATTEMPT"
            });

            var alertMessageBuilder = new StringBuilder();
            alertMessageBuilder.Append("SECURITY ALERT: Suspicious CSP violation detected from IP {ClientIP}. ");
            alertMessageBuilder.Append("Blocked URI: {BlockedUri}, Source: {SourceFile}. ");
            alertMessageBuilder.Append("This may indicate an XSS or code injection attempt.");
            // Use TaintBarrier for complete taint isolation
            var safeLogMessage = TaintBarrier.CreateSafeLogMessage(
                "SECURITY ALERT: Suspicious CSP violation detected from IP {0}. Blocked URI: {1}, Source: {2}. This may indicate an XSS or code injection attempt.",
                clientIp, cspReport.BlockedUri ?? "unknown", cspReport.SourceFile ?? "unknown");
            _logger.LogError(safeLogMessage);

            // In a production environment, you might want to:
            // - Send alerts to security team
            // - Temporarily rate limit or block the IP
            // - Store violation for analysis
            await Task.Delay(1); // Placeholder for async security actions
        }
    }

    private string GetClientIpAddress()
    {
        // Use SecureLoggingHelper to get sanitized IP address for privacy protection
        var rawIp = GetRawClientIpAddress();
        return SecureLoggingHelper.SanitizeIpAddress(rawIp);
    }

    private string GetRawClientIpAddress()
    {
        // Check for forwarded IP from proxy/load balancer
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

/// <summary>
/// Represents the structure of a CSP violation report sent by browsers.
/// </summary>
public class CspViolationReport
{
    [Required]
    [JsonPropertyName("csp-report")]
    public CspReport CspReport { get; set; } = null!;
}

/// <summary>
/// Represents the details of a Content Security Policy violation.
/// </summary>
public class CspReport
{
    /// <summary>
    /// The URI of the document where the violation occurred.
    /// </summary>
    [JsonPropertyName("document-uri")]
    public string? DocumentUri { get; set; }

    /// <summary>
    /// The URI that was blocked by the CSP policy.
    /// </summary>
    [JsonPropertyName("blocked-uri")]
    public string? BlockedUri { get; set; }

    /// <summary>
    /// The directive that was violated.
    /// </summary>
    [JsonPropertyName("violated-directive")]
    public string? ViolatedDirective { get; set; }

    /// <summary>
    /// The effective directive that was violated (may differ from violated-directive).
    /// </summary>
    [JsonPropertyName("effective-directive")]
    public string? EffectiveDirective { get; set; }

    /// <summary>
    /// The original CSP policy that was violated.
    /// </summary>
    [JsonPropertyName("original-policy")]
    public string? OriginalPolicy { get; set; }

    /// <summary>
    /// The URI of the resource that caused the violation.
    /// </summary>
    [JsonPropertyName("source-file")]
    public string? SourceFile { get; set; }

    /// <summary>
    /// The line number where the violation occurred.
    /// </summary>
    [JsonPropertyName("line-number")]
    public int? LineNumber { get; set; }

    /// <summary>
    /// The column number where the violation occurred.
    /// </summary>
    [JsonPropertyName("column-number")]
    public int? ColumnNumber { get; set; }

    /// <summary>
    /// The status code of the HTTP response that included the CSP policy.
    /// </summary>
    [JsonPropertyName("status-code")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// Sample of the blocked content (may be truncated).
    /// </summary>
    [JsonPropertyName("script-sample")]
    public string? ScriptSample { get; set; }
}