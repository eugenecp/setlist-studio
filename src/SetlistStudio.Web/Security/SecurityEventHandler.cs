using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Security;
using SetlistStudio.Web.Utilities;
using System.Security.Claims;

namespace SetlistStudio.Web.Security;

/// <summary>
/// Handles security event logging for authentication and authorization events.
/// Integrates with ASP.NET Core Identity to capture security-relevant events.
/// </summary>
public class SecurityEventHandler : ISecurityEventHandler
{
    private readonly SecurityEventLogger _securityEventLogger;
    private readonly ILogger<SecurityEventHandler> _logger;

    public SecurityEventHandler(SecurityEventLogger securityEventLogger, ILogger<SecurityEventHandler> logger)
    {
        _securityEventLogger = securityEventLogger ?? throw new ArgumentNullException(nameof(securityEventLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles successful login events from various authentication providers.
    /// </summary>
    /// <param name="context">The authentication context</param>
    /// <param name="user">The authenticated user</param>
    public void OnLoginSuccess(HttpContext context, ApplicationUser user)
    {
        try
        {
            var authenticationMethod = GetAuthenticationMethod(context);
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var ipAddress = SecureUserContext.GetSanitizedClientIp(context);

            _securityEventLogger.LogAuthenticationSuccess(
                user.Id,
                authenticationMethod,
                userAgent,
                ipAddress);

            _logger.LogInformation("Authentication success logged for user {UserId}", 
                SecureLoggingHelper.SanitizeUserId(user.Id));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument provided to authentication success logging");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Security event logging service temporarily unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error logging authentication success event");
        }
    }

    /// <summary>
    /// Handles failed login attempts.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="attemptedUserId">The user ID that was attempted (may be invalid)</param>
    /// <param name="failureReason">The reason for the failure</param>
    public void OnLoginFailure(HttpContext context, string? attemptedUserId, string failureReason)
    {
        try
        {
            var authenticationMethod = GetAuthenticationMethod(context);
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var ipAddress = SecureUserContext.GetSanitizedClientIp(context);

            _securityEventLogger.LogAuthenticationFailure(
                attemptedUserId,
                authenticationMethod,
                failureReason,
                userAgent,
                ipAddress);

            _logger.LogWarning("Authentication failure logged for attempted user {AttemptedUserId}: {FailureReason}", 
                SecureLoggingHelper.SanitizeUserId(attemptedUserId), 
                SecureLoggingHelper.SanitizeMessage(failureReason));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log authentication failure event");
        }
    }

    /// <summary>
    /// Handles account lockout events.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="user">The locked user</param>
    /// <param name="lockoutEnd">When the lockout ends</param>
    /// <param name="failedAttemptCount">The number of failed attempts</param>
    public void OnAccountLockout(HttpContext context, ApplicationUser user, DateTimeOffset lockoutEnd, int failedAttemptCount)
    {
        try
        {
            var lockoutDuration = lockoutEnd - DateTimeOffset.UtcNow;
            var ipAddress = SecureUserContext.GetSanitizedClientIp(context);

            _securityEventLogger.LogAccountLockout(
                user.Id,
                lockoutDuration,
                failedAttemptCount,
                ipAddress);

            _logger.LogError("Account lockout logged for user {UserId} with {FailedAttemptCount} failed attempts", 
                SecureLoggingHelper.SanitizeUserId(user.Id), 
                failedAttemptCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log account lockout event");
        }
    }

    /// <summary>
    /// Handles logout events.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="userId">The user who logged out</param>
    public void OnLogout(HttpContext context, string userId)
    {
        try
        {
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var ipAddress = SecureUserContext.GetSanitizedClientIp(context);

            var additionalData = new
            {
                UserAgent = userAgent,
                IpAddress = ipAddress,
                LogoutTime = DateTimeOffset.UtcNow
            };

            _securityEventLogger.LogSecurityEvent(
                SecurityEventType.Authentication,
                SecurityEventSeverity.Low,
                "User logged out successfully",
                userId,
                "Authentication",
                null,
                additionalData);

            _logger.LogInformation("Logout logged for user {UserId}", 
                SecureLoggingHelper.SanitizeUserId(userId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log logout event");
        }
    }

    /// <summary>
    /// Handles suspicious activity detection.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="activityType">The type of suspicious activity</param>
    /// <param name="description">Description of the activity</param>
    /// <param name="userId">The user associated with the activity (optional)</param>
    /// <param name="severity">The severity level</param>
    public void OnSuspiciousActivity(HttpContext context, string activityType, string description, string? userId = null, SecurityEventSeverity severity = SecurityEventSeverity.High)
    {
        try
        {
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var ipAddress = SecureUserContext.GetSanitizedClientIp(context);
            var requestPath = context.Request.Path;

            var additionalContext = new
            {
                UserAgent = SecureLoggingHelper.SanitizeMessage(userAgent),
                IpAddress = ipAddress, // Already sanitized by SecureUserContext
                RequestPath = SecureLoggingHelper.SanitizeMessage(requestPath.ToString()),
                RequestMethod = SecureLoggingHelper.SanitizeMessage(context.Request.Method),
                DetectionTime = DateTimeOffset.UtcNow
            };

            _securityEventLogger.LogSuspiciousActivity(
                activityType,
                description,
                userId,
                severity,
                additionalContext);

            _logger.LogError("Suspicious activity logged: {ActivityType} - {Description} for user {UserId}", 
                activityType, 
                SecureLoggingHelper.SanitizeMessage(description), 
                SecureLoggingHelper.SanitizeUserId(userId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log suspicious activity event");
        }
    }

    /// <summary>
    /// Logs suspicious activity with pre-sanitized context data to prevent log injection.
    /// This overload should be used when the context data has already been sanitized.
    /// </summary>
    /// <param name="activityType">The type of suspicious activity</param>
    /// <param name="description">Description of the activity</param>
    /// <param name="userId">The user associated with the activity (optional)</param>
    /// <param name="severity">The severity level</param>
    /// <param name="sanitizedUserAgent">Pre-sanitized user agent string</param>
    /// <param name="sanitizedIpAddress">Pre-sanitized IP address</param>
    /// <param name="sanitizedRequestPath">Pre-sanitized request path</param>
    /// <param name="sanitizedRequestMethod">Pre-sanitized request method</param>
    public void OnSuspiciousActivity(
        string activityType, 
        string description, 
        string? userId = null, 
        SecurityEventSeverity severity = SecurityEventSeverity.High,
        string? sanitizedUserAgent = null,
        string? sanitizedIpAddress = null,
        string? sanitizedRequestPath = null,
        string? sanitizedRequestMethod = null)
    {
        try
        {
            var additionalContext = new
            {
                UserAgent = sanitizedUserAgent ?? "Unknown",
                IpAddress = sanitizedIpAddress ?? "Unknown",
                RequestPath = sanitizedRequestPath ?? "Unknown",
                RequestMethod = sanitizedRequestMethod ?? "Unknown",
                DetectionTime = DateTimeOffset.UtcNow
            };

            _securityEventLogger.LogSuspiciousActivity(
                activityType,
                description,
                userId,
                severity,
                additionalContext);

            _logger.LogError("Suspicious activity logged: {ActivityType} - {Description} for user {UserId}", 
                activityType, 
                SecureLoggingHelper.SanitizeMessage(description), 
                SecureLoggingHelper.SanitizeUserId(userId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log suspicious activity event");
        }
    }

    /// <summary>
    /// Determines the authentication method used based on the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The authentication method name</returns>
    private static string GetAuthenticationMethod(HttpContext context)
    {
        var authenticationType = context.User?.Identity?.AuthenticationType;
        
        return authenticationType switch
        {
            "Identity.Application" => "Password",
            "Google" => "OAuth-Google",
            "Microsoft" => "OAuth-Microsoft", 
            "Facebook" => "OAuth-Facebook",
            _ => authenticationType ?? "Unknown"
        };
    }

    /// <summary>
    /// Extracts the client IP address from the HTTP context for security logging
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The client IP address</returns>
    private static string? GetClientIpAddress(HttpContext context)
    {
        return IpAddressUtility.GetClientIpAddress(context);
    }
}