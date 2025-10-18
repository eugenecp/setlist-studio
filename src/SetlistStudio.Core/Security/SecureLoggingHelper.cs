using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace SetlistStudio.Core.Security;

/// <summary>
/// Provides utilities for secure logging with automatic sanitization of sensitive data.
/// Prevents logging of passwords, tokens, personal information, and other sensitive data.
/// </summary>
public static class SecureLoggingHelper
{
    // Sensitive data patterns that should never be logged
    public static readonly Regex[] SensitivePatterns = 
    {
        new(@"password\s*[:=]\s*[""']?(.{1,})([""'\s;,}]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"token\s*[:=]\s*[""']?(.{1,})([""'\s;,}]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"secret\s*[:=]\s*[""']?(.{1,})([""'\s;,}]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(?<!musical\s)(?<!song\s)(?<!and\s)key\s*[:=]\s*[""']?(.{1,})([""'\s;,}]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled), // Exclude musical keys
        new(@"api[_-]?key\s*[:=]\s*[""']?(.{1,})([""'\s;,}]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"client[_-]?secret\s*[:=]\s*[""']?(.{1,})([""'\s;,}]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"bearer\s+([a-zA-Z0-9._-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"authorization\s*[:=]\s*[""']?(.{1,})([""'\s;,}]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\b[A-Za-z0-9._%+-]+@([A-Za-z0-9.-]+\.[A-Z|a-z]{2,})\b", RegexOptions.Compiled), // Email addresses
        new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled), // SSN pattern
        new(@"\b\d{4}[- ]?\d{4}[- ]?\d{4}[- ]?\d{4}\b", RegexOptions.Compiled), // Credit card pattern
    };

    // Fields that should be completely redacted
    private static readonly string[] SensitiveFields =
    {
        "password", "token", "secret", "key", "apikey", "api_key", "clientsecret", "client_secret",
        "authorization", "bearer", "jwt", "sessionid", "session_id", "cookie", "csrf", "antiforgery"
    };

    /// <summary>
    /// Sanitizes a message by removing or masking sensitive data patterns.
    /// </summary>
    /// <param name="message">The original message that may contain sensitive data</param>
    /// <returns>A sanitized version of the message with sensitive data masked</returns>
    public static string SanitizeMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var sanitized = message;

        // Replace sensitive patterns with masked versions
        for (int i = 0; i < SensitivePatterns.Length; i++)
        {
            var pattern = SensitivePatterns[i];
            sanitized = pattern.Replace(sanitized, match =>
            {
                // For patterns with capture groups (first 8 patterns)
                if (i < 8 && match.Groups.Count > 1)
                {
                    var prefix = match.Value.Substring(0, match.Value.Length - match.Groups[1].Length - (match.Groups.Count > 2 ? match.Groups[2].Length : 0));
                    var suffix = match.Groups.Count > 2 ? match.Groups[2].Value : "";
                    return $"{prefix}[REDACTED]{suffix}";
                }
                // For email pattern (index 8)
                else if (i == 8 && match.Groups.Count > 1)
                {
                    return $"[REDACTED]@{match.Groups[1].Value}";
                }
                // For simple patterns (SSN, credit cards)
                return "[REDACTED]";
            });
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes an object by creating a safe representation for logging.
    /// Replaces sensitive properties with [REDACTED] values.
    /// </summary>
    /// <param name="obj">The object to sanitize</param>
    /// <returns>A dictionary representation with sensitive data masked</returns>
    public static Dictionary<string, object?> SanitizeObject(object obj)
    {
        if (obj == null)
            return new Dictionary<string, object?>();

        var result = new Dictionary<string, object?>();
        var properties = obj.GetType().GetProperties();

        foreach (var property in properties)
        {
            try
            {
                var propertyName = property.Name;
                var value = property.GetValue(obj);

                // Check if this is a sensitive field
                if (IsSensitiveField(propertyName))
                {
                    result[propertyName] = "[REDACTED]";
                    continue;
                }

                // Handle different value types
                if (value == null)
                {
                    result[propertyName] = null;
                }
                else if (value is string stringValue)
                {
                    result[propertyName] = SanitizeMessage(stringValue);
                }
                else if (value.GetType().IsPrimitive || value is DateTime || value is DateTimeOffset || value is TimeSpan)
                {
                    result[propertyName] = value;
                }
                else
                {
                    // For complex objects, just include the type name to avoid recursive serialization
                    result[propertyName] = $"[{value.GetType().Name}]";
                }
            }
            catch
            {
                // If we can't read the property, skip it
                continue;
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a field name indicates sensitive data.
    /// </summary>
    /// <param name="fieldName">The field name to check</param>
    /// <returns>True if the field is considered sensitive</returns>
    public static bool IsSensitiveField(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
            return false;

        var lowerFieldName = fieldName.ToLowerInvariant();
        return SensitiveFields.Any(sensitiveField => lowerFieldName.Contains(sensitiveField));
    }

    /// <summary>
    /// Creates a secure log entry structure with sanitized data and security context.
    /// </summary>
    /// <param name="action">The action being performed</param>
    /// <param name="userId">The user performing the action (nullable)</param>
    /// <param name="resourceType">The type of resource being accessed</param>
    /// <param name="resourceId">The ID of the resource (nullable)</param>
    /// <param name="additionalData">Additional data to include (will be sanitized)</param>
    /// <returns>A secure log entry structure</returns>
    public static Dictionary<string, object?> CreateSecureLogEntry(
        string action,
        string? userId,
        string resourceType,
        string? resourceId,
        object? additionalData = null)
    {
        var logEntry = new Dictionary<string, object?>
        {
            ["Action"] = SanitizeMessage(action),
            ["UserId"] = SanitizeUserId(userId),
            ["ResourceType"] = SanitizeMessage(resourceType),
            ["ResourceId"] = resourceId,
            ["Timestamp"] = DateTimeOffset.UtcNow,
            ["CorrelationId"] = Guid.NewGuid().ToString()
        };

        if (additionalData != null)
        {
            var sanitizedData = SanitizeObject(additionalData);
            logEntry["AdditionalData"] = sanitizedData;
        }

        return logEntry;
    }

    /// <summary>
    /// Sanitizes a user ID for logging (keeps user identifiable but removes sensitive patterns).
    /// </summary>
    /// <param name="userId">The user ID to sanitize</param>
    /// <returns>A sanitized user ID safe for logging</returns>
    public static string? SanitizeUserId(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return userId;

        // If it looks like an email, mask the domain part
        if (userId.Contains('@'))
        {
            var parts = userId.Split('@');
            if (parts.Length == 2)
            {
                return $"{parts[0]}@[DOMAIN]";
            }
        }

        // For GUIDs or other identifiers, return as-is (they're not sensitive)
        return userId;
    }
}