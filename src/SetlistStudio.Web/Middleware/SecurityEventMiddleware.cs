using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Security;
using SetlistStudio.Web.Security;
using System.Security;

namespace SetlistStudio.Web.Middleware;

/// <summary>
/// Middleware that captures and logs security events including authentication attempts,
/// authorization failures, and suspicious activities.
/// </summary>
public class SecurityEventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityEventMiddleware> _logger;

    public SecurityEventMiddleware(RequestDelegate next, ILogger<SecurityEventMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, ISecurityEventHandler securityEventHandler, SecurityEventLogger securityEventLogger)
    {
        var startTime = DateTimeOffset.UtcNow;
        var requestPath = context.Request.Path.Value ?? string.Empty;

        try
        {
            // Monitor for suspicious patterns in requests
            await DetectSuspiciousPatterns(context, securityEventHandler, securityEventLogger);

            // Continue processing the request
            await _next(context);

            // Log successful authentication events after request completion
            await LogAuthenticationEvents(context, securityEventHandler, securityEventLogger);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt detected for path {RequestPath}", requestPath);
            await LogSecurityException(context, securityEventHandler, ex);
            throw;
        }
        catch (SecurityException ex)
        {
            _logger.LogWarning(ex, "Security exception in middleware for path {RequestPath}", requestPath);
            await LogSecurityException(context, securityEventHandler, ex);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation in security middleware for path {RequestPath}", requestPath);
            if (IsSecurityRelatedException(ex))
            {
                await LogSecurityException(context, securityEventHandler, ex);
            }
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument in security middleware for path {RequestPath}", requestPath);
            throw;
        }
        // CodeQL[cs/catch-of-all-exceptions] - Middleware boundary catch for security logging
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in security event middleware for path {RequestPath}", requestPath);
            
            // Log potential security-related exceptions
            if (IsSecurityRelatedException(ex))
            {
                await LogSecurityException(context, securityEventHandler, ex);
            }

            throw; // Re-throw to maintain normal error handling
        }
        finally
        {
            // Log slow requests that might indicate attacks
            var duration = DateTimeOffset.UtcNow - startTime;
            if (duration.TotalSeconds > 10) // Requests taking longer than 10 seconds
            {
                securityEventHandler.OnSuspiciousActivity(
                    context,
                    "SlowRequest",
                    $"Request to {requestPath} took {duration.TotalSeconds:F2} seconds",
                    context.User?.Identity?.Name,
                    SecurityEventSeverity.Medium);
            }
        }
    }

    /// <summary>
    /// Detects suspicious patterns in incoming requests.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="securityEventHandler">The security event handler</param>
    /// <param name="securityEventLogger">The security event logger</param>
    private async Task DetectSuspiciousPatterns(HttpContext context, ISecurityEventHandler securityEventHandler, SecurityEventLogger securityEventLogger)
    {
        var request = context.Request;
        var requestPath = request.Path.Value ?? string.Empty;
        var userAgent = request.Headers.UserAgent.ToString();
        var userId = context.User?.Identity?.Name;

        // Check for common attack patterns in URL
        var suspiciousUrlPatterns = new[]
        {
            "../", "..\\", "%2e%2e", "script>", "<script", "javascript:", "vbscript:",
            "onload=", "onerror=", "eval(", "alert(", "document.cookie", "document.write",
            "union select", "drop table", "insert into", "delete from", "update set",
            "exec(", "execute(", "sp_executesql", "xp_cmdshell"
        };

        var suspiciousPattern = suspiciousUrlPatterns.FirstOrDefault(pattern =>
            requestPath.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
            request.QueryString.Value?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true);
            
        if (suspiciousPattern != null)
        {
            securityEventHandler.OnSuspiciousActivity(
                context,
                "MaliciousUrlPattern",
                $"Suspicious pattern '{suspiciousPattern}' detected in request",
                userId,
                SecurityEventSeverity.High);
        }

        // Check for suspicious user agents with context-aware validation
        var userAgentRisk = AssessUserAgentRisk(userAgent, requestPath);
        if (userAgentRisk.IsSuspicious)
        {
            securityEventHandler.OnSuspiciousActivity(
                context,
                userAgentRisk.EventType,
                userAgentRisk.Message,
                userId,
                userAgentRisk.Severity);
        }

        // Check for rapid requests from same IP (basic rate limiting detection)
        await CheckRapidRequests(context, securityEventHandler);

        // Check request body for suspicious patterns (for POST requests)
        if (request.Method == "POST" && request.HasFormContentType)
        {
            await CheckFormDataForSuspiciousPatterns(context, securityEventHandler);
        }
    }

    /// <summary>
    /// Assesses the risk level of a user agent string with context-aware validation.
    /// Provides graduated response based on legitimacy and request context.
    /// </summary>
    /// <param name="userAgent">The user agent string</param>
    /// <param name="requestPath">The request path for context</param>
    /// <returns>Risk assessment with event details</returns>
    private static UserAgentRiskAssessment AssessUserAgentRisk(string userAgent, string requestPath)
    {
        // Handle missing User-Agent with context awareness
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            // Allow missing User-Agent for health checks and monitoring endpoints
            if (IsHealthCheckOrMonitoringEndpoint(requestPath))
            {
                return UserAgentRiskAssessment.Safe();
            }

            // Allow missing User-Agent for test environments (low severity)
            return new UserAgentRiskAssessment
            {
                IsSuspicious = true,
                EventType = "MissingUserAgent",
                Message = "Missing User-Agent header - potential automation",
                Severity = SecurityEventSeverity.Low // Reduced from Medium
            };
        }

        // Check for legitimate testing and monitoring tools (whitelist)
        if (IsLegitimateTestingTool(userAgent))
        {
            return UserAgentRiskAssessment.Safe();
        }

        // Check for legitimate search engine bots (whitelist)
        if (IsLegitimateSearchBot(userAgent))
        {
            return UserAgentRiskAssessment.Safe();
        }

        // Check for security scanning tools (high risk)
        if (IsSecurityScanningTool(userAgent))
        {
            return new UserAgentRiskAssessment
            {
                IsSuspicious = true,
                EventType = "SecurityScannerUserAgent",
                Message = $"Security scanner detected: {SecureLoggingHelper.PreventLogInjection(userAgent)}",
                Severity = SecurityEventSeverity.High
            };
        }

        // Check for suspicious automation tools
        if (IsSuspiciousAutomationTool(userAgent))
        {
            return new UserAgentRiskAssessment
            {
                IsSuspicious = true,
                EventType = "SuspiciousAutomationUserAgent",
                Message = $"Suspicious automation tool detected: {SecureLoggingHelper.PreventLogInjection(userAgent)}",
                Severity = SecurityEventSeverity.Medium
            };
        }

        return UserAgentRiskAssessment.Safe();
    }

    /// <summary>
    /// Checks if the request path is for health checks or monitoring endpoints.
    /// </summary>
    /// <param name="requestPath">The request path</param>
    /// <returns>True if path is for health checks or monitoring</returns>
    private static bool IsHealthCheckOrMonitoringEndpoint(string requestPath)
    {
        var healthCheckPaths = new[]
        {
            "/health", "/healthcheck", "/ping", "/status", "/ready", "/alive",
            "/metrics", "/monitoring", "/probe", "/check"
        };

        return healthCheckPaths.Any(path => 
            requestPath.StartsWith(path, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the user agent represents a legitimate testing tool or framework.
    /// </summary>
    /// <param name="userAgent">The user agent string</param>
    /// <returns>True if user agent is from a legitimate testing tool</returns>
    private static bool IsLegitimateTestingTool(string userAgent)
    {
        var legitimateTestingTools = new[]
        {
            // .NET testing frameworks
            "Microsoft.AspNetCore.TestHost",
            "xunit",
            "nunit",
            "mstest",
            
            // Common development tools
            "Postman",
            "Insomnia",
            "RestClient",
            "HTTPie",
            
            // Load testing tools
            "k6/",
            "Apache-HttpClient",
            "JMeter",
            
            // CI/CD systems
            "GitHub-Actions",
            "Azure DevOps",
            "Jenkins",
            "TeamCity"
        };

        return legitimateTestingTools.Any(tool => 
            userAgent.Contains(tool, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the user agent represents a legitimate search engine bot.
    /// </summary>
    /// <param name="userAgent">The user agent string</param>
    /// <returns>True if user agent is from a legitimate search engine</returns>
    private static bool IsLegitimateSearchBot(string userAgent)
    {
        var legitimateSearchBots = new[]
        {
            "Googlebot",
            "Bingbot",
            "Slurp", // Yahoo
            "DuckDuckBot",
            "Baiduspider",
            "YandexBot",
            "facebookexternalhit",
            "Twitterbot",
            "LinkedInBot",
            "WhatsApp",
            "Applebot"
        };

        return legitimateSearchBots.Any(bot => 
            userAgent.Contains(bot, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the user agent represents a security scanning tool.
    /// </summary>
    /// <param name="userAgent">The user agent string</param>
    /// <returns>True if user agent is from a security scanner</returns>
    private static bool IsSecurityScanningTool(string userAgent)
    {
        var securityScanners = new[]
        {
            "sqlmap", "nmap", "nikto", "dirb", "gobuster", "dirbuster",
            "burp", "zap", "owasp", "masscan", "nessus", "openvas", "w3af",
            "whatweb", "httprint", "skipfish", "wpscan", "nuclei",
            "acunetix", "netsparker", "appscan", "veracode"
        };

        return securityScanners.Any(scanner => 
            userAgent.Contains(scanner, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the user agent represents a potentially suspicious automation tool.
    /// </summary>
    /// <param name="userAgent">The user agent string</param>
    /// <returns>True if user agent might be from suspicious automation</returns>
    private static bool IsSuspiciousAutomationTool(string userAgent)
    {
        // Only flag specific suspicious patterns, not all automation
        var suspiciousPatterns = new[]
        {
            // Generic/basic scrapers (not whitelisted ones)
            "python-urllib",
            "java/",
            "go-http-client",
            
            // Suspicious curl/wget patterns (very basic versions)
            "curl/7.0", "curl/7.1", "curl/7.2", // Very old versions
            "wget/1.0", "wget/1.1", // Very old versions
            
            // Generic scraping indicators
            "scraper", "harvester", "collector"
        };

        return suspiciousPatterns.Any(pattern => 
            userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Represents the result of a User-Agent risk assessment.
    /// </summary>
    private class UserAgentRiskAssessment
    {
        public bool IsSuspicious { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public SecurityEventSeverity Severity { get; set; }

        public static UserAgentRiskAssessment Safe() => new()
        {
            IsSuspicious = false,
            EventType = string.Empty,
            Message = string.Empty,
            Severity = SecurityEventSeverity.Low
        };
    }

    /// <summary>
    /// Checks for rapid requests that might indicate an attack.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="securityEventHandler">The security event handler</param>
    private Task CheckRapidRequests(HttpContext context, ISecurityEventHandler securityEventHandler)
    {
        // This is a simplified implementation. In production, you'd want to use a more sophisticated
        // tracking mechanism with distributed cache or database.
        var ipAddress = GetClientIpAddress(context);
        if (string.IsNullOrEmpty(ipAddress))
            return Task.CompletedTask;

        // For demonstration, we'll just check if there are multiple requests in a short timeframe
        // In production, implement proper request tracking
        var requestCount = context.Items.Count; // Simplified check
        if (requestCount > 100) // Arbitrary threshold for demo
        {
            securityEventHandler.OnSuspiciousActivity(
                context,
                "RapidRequests",
                $"High number of requests from IP {ipAddress}",
                context.User?.Identity?.Name,
                SecurityEventSeverity.High);
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks form data for suspicious patterns that might indicate injection attacks.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="securityEventHandler">The security event handler</param>
    private async Task CheckFormDataForSuspiciousPatterns(HttpContext context, ISecurityEventHandler securityEventHandler)
    {
        try
        {
            var form = await context.Request.ReadFormAsync();
            
            foreach (var field in form)
            {
                var fieldValue = field.Value.ToString() ?? string.Empty;
                
                // Check for XSS patterns
                if (ContainsXssPattern(fieldValue))
                {
                    // Use the overload that doesn't extract data from HttpContext to break taint chain
                    var sanitizedUserAgent = SecureLoggingHelper.SanitizeMessage(context.Request.Headers.UserAgent.ToString());
                    var sanitizedIpAddress = SecureLoggingHelper.SanitizeMessage(GetClientIpAddress(context) ?? "Unknown");
                    var sanitizedRequestPath = SecureLoggingHelper.SanitizeMessage(context.Request.Path.ToString());
                    var sanitizedRequestMethod = SecureLoggingHelper.SanitizeMessage(context.Request.Method);

                    securityEventHandler.OnSuspiciousActivity(
                        "XSS_Pattern_Detection",
                        $"XSS pattern detected in field {SecureLoggingHelper.PreventLogInjection(field.Key)}",
                        context.User?.Identity?.Name,
                        SecurityEventSeverity.High,
                        sanitizedUserAgent,
                        sanitizedIpAddress,
                        sanitizedRequestPath,
                        sanitizedRequestMethod);
                }

                // Check for SQL injection patterns
                if (ContainsSqlInjectionPattern(fieldValue))
                {
                    // Use the overload that doesn't extract data from HttpContext to break taint chain
                    var sanitizedUserAgent = SecureLoggingHelper.SanitizeMessage(context.Request.Headers.UserAgent.ToString());
                    var sanitizedIpAddress = SecureLoggingHelper.SanitizeMessage(GetClientIpAddress(context) ?? "Unknown");
                    var sanitizedRequestPath = SecureLoggingHelper.SanitizeMessage(context.Request.Path.ToString());
                    var sanitizedRequestMethod = SecureLoggingHelper.SanitizeMessage(context.Request.Method);

                    securityEventHandler.OnSuspiciousActivity(
                        "SQL_Injection_Pattern_Detection",
                        $"SQL injection pattern detected in field {SecureLoggingHelper.PreventLogInjection(field.Key)}",
                        context.User?.Identity?.Name,
                        SecurityEventSeverity.High,
                        sanitizedUserAgent,
                        sanitizedIpAddress,
                        sanitizedRequestPath,
                        sanitizedRequestMethod);
                }
            }
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null argument while checking form data patterns");
        }
        catch (InvalidCastException ex)
        {
            _logger.LogWarning(ex, "Invalid cast while processing form data");
        }
        // CodeQL[cs/catch-of-all-exceptions] - Defensive programming for security pattern analysis
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error while checking form data for suspicious patterns");
        }
    }

    /// <summary>
    /// Checks if a value contains potential XSS patterns.
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <returns>True if XSS patterns are detected</returns>
    private static bool ContainsXssPattern(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        var xssPatterns = new[]
        {
            "<script", "javascript:", "vbscript:", "onload=", "onerror=", "onclick=",
            "eval(", "alert(", "confirm(", "prompt(", "document.cookie", "document.write"
        };

        return xssPatterns.Any(pattern => 
            value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a value contains potential SQL injection patterns.
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <returns>True if SQL injection patterns are detected</returns>
    private static bool ContainsSqlInjectionPattern(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        var sqlPatterns = new[]
        {
            "union select", "drop table", "insert into", "delete from", "update set",
            "exec(", "execute(", "sp_executesql", "xp_cmdshell", "'; --", "' or '1'='1",
            "\" or \"1\"=\"1", "or 1=1", "and 1=1", "having 1=1"
        };

        return sqlPatterns.Any(pattern => 
            value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Logs authentication-related events after successful request processing.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="securityEventHandler">The security event handler</param>
    /// <param name="securityEventLogger">The security event logger</param>
    private Task LogAuthenticationEvents(HttpContext context, ISecurityEventHandler securityEventHandler, SecurityEventLogger securityEventLogger)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var user = context.User;

        // Log successful authentication for protected pages
        if (user.Identity?.IsAuthenticated == true)
        {
            var userId = user.Identity.Name;
            
            // Log access to sensitive areas
            if (IsSensitiveArea(path))
            {
                securityEventLogger.LogDataAccess(
                    userId!,
                    "SensitiveArea",
                    path,
                    context.Request.Method);
            }
        }

        // Log login/logout events
        if (path.Contains("/login", StringComparison.OrdinalIgnoreCase) && context.Response.StatusCode == 200)
        {
            // Login success is handled by Identity events, but we can log page access
            _logger.LogInformation("Login page accessed successfully");
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines if a path represents a sensitive area requiring additional logging.
    /// </summary>
    /// <param name="path">The request path</param>
    /// <returns>True if the path is considered sensitive</returns>
    private static bool IsSensitiveArea(string path)
    {
        var sensitivePaths = new[]
        {
            "/admin", "/account", "/profile", "/settings", "/api", "/dashboard"
        };

        return sensitivePaths.Any(sensitive => 
            path.StartsWith(sensitive, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Logs security-related exceptions.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="securityEventHandler">The security event handler</param>
    /// <param name="exception">The exception that occurred</param>
    private Task LogSecurityException(HttpContext context, ISecurityEventHandler securityEventHandler, Exception exception)
    {
        securityEventHandler.OnSuspiciousActivity(
            context,
            "SecurityException",
            $"Security-related exception occurred: {SecureLoggingHelper.PreventLogInjection(exception.GetType().Name)}",
            context.User?.Identity?.Name,
            SecurityEventSeverity.High);
            
        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines if an exception is security-related and should be logged.
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if the exception is security-related</returns>
    private static bool IsSecurityRelatedException(Exception exception)
    {
        var securityExceptions = new[]
        {
            typeof(UnauthorizedAccessException),
            typeof(SecurityException),
            typeof(InvalidOperationException) // Might indicate authorization issues
        };

        return securityExceptions.Contains(exception.GetType());
    }

    /// <summary>
    /// Extracts the client IP address from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The client IP address</returns>
    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return context.Connection.RemoteIpAddress?.ToString();
    }
}