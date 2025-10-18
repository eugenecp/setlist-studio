using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Security;

namespace SetlistStudio.Core.Security;

/// <summary>
/// Defines different types of security events that can occur in the application.
/// Used for categorizing and filtering security logs.
/// </summary>
public enum SecurityEventType
{
    Authentication,
    Authorization,
    AccountLockout,
    SuspiciousActivity,
    DataAccess,
    ValidationFailure,
    ConfigurationChange,
    TokenManagement
}

/// <summary>
/// Represents the severity level of a security event.
/// </summary>
public enum SecurityEventSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Provides centralized security event logging with automatic sanitization and standardized formatting.
/// All security-related events should be logged through this service to ensure consistency and compliance.
/// </summary>
public class SecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;

    public SecurityEventLogger(ILogger<SecurityEventLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Logs a security event with standardized formatting and data sanitization.
    /// </summary>
    /// <param name="eventType">The type of security event</param>
    /// <param name="severity">The severity level of the event</param>
    /// <param name="message">A description of the security event</param>
    /// <param name="userId">The user associated with the event (nullable)</param>
    /// <param name="resourceType">The type of resource involved (nullable)</param>
    /// <param name="resourceId">The ID of the resource involved (nullable)</param>
    /// <param name="additionalData">Additional context data (will be sanitized)</param>
    public virtual void LogSecurityEvent(
        SecurityEventType eventType,
        SecurityEventSeverity severity,
        string message,
        string? userId = null,
        string? resourceType = null,
        string? resourceId = null,
        object? additionalData = null)
    {
        var logLevel = MapSeverityToLogLevel(severity);
        var sanitizedMessage = SecureLoggingHelper.SanitizeMessage(message);
        
        var logEntry = SecureLoggingHelper.CreateSecureLogEntry(
            $"SecurityEvent_{eventType}",
            userId,
            resourceType ?? "Unknown",
            resourceId,
            additionalData);

        logEntry["EventType"] = eventType.ToString();
        logEntry["Severity"] = severity.ToString();
        logEntry["Message"] = sanitizedMessage;

        _logger.Log(logLevel, "Security Event: {EventType} | {Severity} | {Message} | Data: {@LogEntry}",
            eventType, severity, sanitizedMessage, logEntry);
    }

    /// <summary>
    /// Logs successful authentication events.
    /// </summary>
    /// <param name="userId">The authenticated user ID</param>
    /// <param name="authenticationMethod">The method used (e.g., "Password", "OAuth", "Google")</param>
    /// <param name="userAgent">The user agent string (optional)</param>
    /// <param name="ipAddress">The IP address (optional)</param>
    public virtual void LogAuthenticationSuccess(string userId, string authenticationMethod, string? userAgent = null, string? ipAddress = null)
    {
        var additionalData = new
        {
            AuthenticationMethod = authenticationMethod,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            LoginTime = DateTimeOffset.UtcNow
        };

        LogSecurityEvent(
            SecurityEventType.Authentication,
            SecurityEventSeverity.Low,
            $"User successfully authenticated using {authenticationMethod}",
            userId,
            "Authentication",
            null,
            additionalData);
    }

    /// <summary>
    /// Logs failed authentication attempts.
    /// </summary>
    /// <param name="attemptedUserId">The user ID that was attempted (may be invalid)</param>
    /// <param name="authenticationMethod">The method attempted</param>
    /// <param name="failureReason">The reason for failure</param>
    /// <param name="userAgent">The user agent string (optional)</param>
    /// <param name="ipAddress">The IP address (optional)</param>
    public virtual void LogAuthenticationFailure(string? attemptedUserId, string authenticationMethod, string failureReason, string? userAgent = null, string? ipAddress = null)
    {
        var additionalData = new
        {
            AuthenticationMethod = authenticationMethod,
            FailureReason = failureReason,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            AttemptTime = DateTimeOffset.UtcNow
        };

        LogSecurityEvent(
            SecurityEventType.Authentication,
            SecurityEventSeverity.Medium,
            $"Authentication failed for user {attemptedUserId ?? "[unknown]"}: {failureReason}",
            attemptedUserId,
            "Authentication",
            null,
            additionalData);
    }

    /// <summary>
    /// Logs authorization failures when users attempt to access resources they don't own.
    /// </summary>
    /// <param name="userId">The user who attempted access</param>
    /// <param name="resourceType">The type of resource (e.g., "Song", "Setlist")</param>
    /// <param name="resourceId">The ID of the resource</param>
    /// <param name="action">The attempted action</param>
    public virtual void LogAuthorizationFailure(string userId, string resourceType, string resourceId, string action)
    {
        var additionalData = new
        {
            Action = action,
            AttemptTime = DateTimeOffset.UtcNow
        };

        LogSecurityEvent(
            SecurityEventType.Authorization,
            SecurityEventSeverity.High,
            $"User attempted unauthorized {action} on {resourceType}",
            userId,
            resourceType,
            resourceId,
            additionalData);
    }

    /// <summary>
    /// Logs successful authorization events for audit purposes.
    /// </summary>
    /// <param name="userId">The user who was authorized</param>
    /// <param name="resourceType">The type of resource</param>
    /// <param name="resourceId">The ID of the resource</param>
    /// <param name="action">The authorized action</param>
    public virtual void LogAuthorizationSuccess(string userId, string resourceType, string resourceId, string action)
    {
        var additionalData = new
        {
            Action = action,
            AuthorizedTime = DateTimeOffset.UtcNow
        };

        LogSecurityEvent(
            SecurityEventType.Authorization,
            SecurityEventSeverity.Low,
            $"User successfully authorized for {action} on {resourceType}",
            userId,
            resourceType,
            resourceId,
            additionalData);
    }

    /// <summary>
    /// Logs account lockout events.
    /// </summary>
    /// <param name="userId">The locked user ID</param>
    /// <param name="lockoutDuration">The duration of the lockout</param>
    /// <param name="failedAttemptCount">The number of failed attempts that triggered the lockout</param>
    /// <param name="ipAddress">The IP address of the failed attempts (optional)</param>
    public virtual void LogAccountLockout(string userId, TimeSpan lockoutDuration, int failedAttemptCount, string? ipAddress = null)
    {
        var additionalData = new
        {
            LockoutDuration = lockoutDuration.ToString(),
            FailedAttemptCount = failedAttemptCount,
            IpAddress = ipAddress,
            LockoutTime = DateTimeOffset.UtcNow
        };

        LogSecurityEvent(
            SecurityEventType.AccountLockout,
            SecurityEventSeverity.High,
            $"Account locked due to {failedAttemptCount} failed login attempts",
            userId,
            "UserAccount",
            userId,
            additionalData);
    }

    /// <summary>
    /// Logs suspicious activity such as unusual patterns, potential attacks, or security violations.
    /// </summary>
    /// <param name="activityType">The type of suspicious activity detected</param>
    /// <param name="description">Detailed description of the activity</param>
    /// <param name="userId">The user associated with the activity (nullable)</param>
    /// <param name="severity">The severity level of the suspicious activity</param>
    /// <param name="additionalContext">Additional context data</param>
    public virtual void LogSuspiciousActivity(string activityType, string description, string? userId = null, SecurityEventSeverity severity = SecurityEventSeverity.High, object? additionalContext = null)
    {
        var additionalData = new
        {
            ActivityType = activityType,
            DetectionTime = DateTimeOffset.UtcNow,
            Context = additionalContext
        };

        LogSecurityEvent(
            SecurityEventType.SuspiciousActivity,
            severity,
            $"Suspicious activity detected: {description}",
            userId,
            "Security",
            null,
            additionalData);
    }

    /// <summary>
    /// Logs data access events for sensitive resources.
    /// </summary>
    /// <param name="userId">The user accessing the data</param>
    /// <param name="resourceType">The type of resource accessed</param>
    /// <param name="resourceId">The ID of the resource</param>
    /// <param name="action">The action performed (e.g., "Read", "Create", "Update", "Delete")</param>
    /// <param name="recordCount">The number of records accessed (optional)</param>
    public virtual void LogDataAccess(string userId, string resourceType, string? resourceId, string action, int? recordCount = null)
    {
        var additionalData = new
        {
            Action = action,
            RecordCount = recordCount,
            AccessTime = DateTimeOffset.UtcNow
        };

        LogSecurityEvent(
            SecurityEventType.DataAccess,
            SecurityEventSeverity.Low,
            $"User performed {action} on {resourceType}" + (recordCount.HasValue ? $" ({recordCount} records)" : ""),
            userId,
            resourceType,
            resourceId,
            additionalData);
    }

    /// <summary>
    /// Logs input validation failures that could indicate malicious input or security probes.
    /// </summary>
    /// <param name="validationType">The type of validation that failed</param>
    /// <param name="fieldName">The field that failed validation</param>
    /// <param name="userId">The user who submitted the invalid input (nullable)</param>
    /// <param name="severity">The severity of the validation failure</param>
    /// <param name="additionalContext">Additional context about the validation failure</param>
    public virtual void LogValidationFailure(string validationType, string fieldName, string? userId = null, SecurityEventSeverity severity = SecurityEventSeverity.Medium, object? additionalContext = null)
    {
        var additionalData = new
        {
            ValidationType = validationType,
            FieldName = fieldName,
            ValidationTime = DateTimeOffset.UtcNow,
            Context = additionalContext
        };

        LogSecurityEvent(
            SecurityEventType.ValidationFailure,
            severity,
            $"Input validation failed: {validationType} on field {fieldName}",
            userId,
            "InputValidation",
            fieldName,
            additionalData);
    }

    /// <summary>
    /// Maps security event severity to appropriate log level.
    /// </summary>
    /// <param name="severity">The security event severity</param>
    /// <returns>The corresponding log level</returns>
    private static LogLevel MapSeverityToLogLevel(SecurityEventSeverity severity)
    {
        return severity switch
        {
            SecurityEventSeverity.Low => LogLevel.Information,
            SecurityEventSeverity.Medium => LogLevel.Warning,
            SecurityEventSeverity.High => LogLevel.Error,
            SecurityEventSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Information
        };
    }
}