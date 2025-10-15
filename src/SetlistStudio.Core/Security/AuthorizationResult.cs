using System.Collections.Generic;

namespace SetlistStudio.Core.Security;

/// <summary>
/// Comprehensive authorization result containing detailed security information
/// Provides status, user context, and detailed reason information for security decisions
/// </summary>
public class AuthorizationResult
{
    /// <summary>
    /// Whether the authorization check passed
    /// </summary>
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// The user ID that was checked for authorization
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The resource type being accessed
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// The resource ID being accessed
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// The action being attempted (Read, Create, Update, Delete)
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable reason for the authorization decision
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Additional context information for security auditing
    /// </summary>
    public Dictionary<string, object> SecurityContext { get; set; } = new();

    /// <summary>
    /// Timestamp when the authorization check was performed
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful authorization result
    /// </summary>
    public static AuthorizationResult Success(string userId, string resourceType, string resourceId, string action)
    {
        return new AuthorizationResult
        {
            IsAuthorized = true,
            UserId = userId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Action = action,
            Reason = "User owns the requested resource"
        };
    }

    /// <summary>
    /// Creates a failed authorization result due to resource not found
    /// </summary>
    public static AuthorizationResult NotFound(string userId, string resourceType, string resourceId, string action)
    {
        return new AuthorizationResult
        {
            IsAuthorized = false,
            UserId = userId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Action = action,
            Reason = "Requested resource does not exist or user does not have access"
        };
    }

    /// <summary>
    /// Creates a failed authorization result due to ownership violation
    /// </summary>
    public static AuthorizationResult Forbidden(string userId, string resourceType, string resourceId, string action, string actualOwnerId = "")
    {
        var reason = string.IsNullOrEmpty(actualOwnerId) 
            ? "User does not own the requested resource"
            : $"Resource belongs to user {actualOwnerId}, not {userId}";

        return new AuthorizationResult
        {
            IsAuthorized = false,
            UserId = userId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Action = action,
            Reason = reason,
            SecurityContext = string.IsNullOrEmpty(actualOwnerId) 
                ? new Dictionary<string, object>()
                : new Dictionary<string, object> { { "ActualOwnerId", actualOwnerId } }
        };
    }

    /// <summary>
    /// Creates a failed authorization result due to invalid user
    /// </summary>
    public static AuthorizationResult InvalidUser(string userId, string resourceType, string resourceId, string action)
    {
        return new AuthorizationResult
        {
            IsAuthorized = false,
            UserId = userId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Action = action,
            Reason = "Invalid or missing user ID"
        };
    }
}