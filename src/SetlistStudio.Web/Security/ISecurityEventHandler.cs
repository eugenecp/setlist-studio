using Microsoft.AspNetCore.Identity;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Security;

namespace SetlistStudio.Web.Security;

/// <summary>
/// Interface for handling security event logging for authentication and authorization events.
/// Provides abstraction for testing and dependency injection of security event handling.
/// </summary>
public interface ISecurityEventHandler
{
    /// <summary>
    /// Handles successful login events from various authentication providers.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="user">The authenticated user</param>
    void OnLoginSuccess(HttpContext context, ApplicationUser user);

    /// <summary>
    /// Handles failed login attempts.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="attemptedUserId">The user ID that was attempted (may be invalid)</param>
    /// <param name="failureReason">The reason for the failure</param>
    void OnLoginFailure(HttpContext context, string? attemptedUserId, string failureReason);

    /// <summary>
    /// Handles account lockout events.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="user">The locked user</param>
    /// <param name="lockoutEnd">When the lockout ends</param>
    /// <param name="failedAttemptCount">The number of failed attempts</param>
    void OnAccountLockout(HttpContext context, ApplicationUser user, DateTimeOffset lockoutEnd, int failedAttemptCount);

    /// <summary>
    /// Handles logout events.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="userId">The user who logged out</param>
    void OnLogout(HttpContext context, string userId);

    /// <summary>
    /// Handles suspicious activity detection.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="activityType">The type of suspicious activity</param>
    /// <param name="description">Description of the activity</param>
    /// <param name="userId">The user associated with the activity (optional)</param>
    /// <param name="severity">The severity level</param>
    void OnSuspiciousActivity(HttpContext context, string activityType, string description, string? userId = null, SecurityEventSeverity severity = SecurityEventSeverity.High);

    /// <summary>
    /// Handles suspicious activity with pre-sanitized context data to prevent log injection.
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
    void OnSuspiciousActivity(
        string activityType, 
        string description, 
        string? userId = null, 
        SecurityEventSeverity severity = SecurityEventSeverity.High,
        string? sanitizedUserAgent = null,
        string? sanitizedIpAddress = null,
        string? sanitizedRequestPath = null,
        string? sanitizedRequestMethod = null);
}