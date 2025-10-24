using Microsoft.Extensions.Logging;
using System.Text;

namespace SetlistStudio.Core.Security;

/// <summary>
/// Centralized authorization helper providing consistent security patterns
/// Ensures standardized user ownership validation, security logging, and audit trails
/// </summary>
public static class ResourceAuthorizationHelper
{
    /// <summary>
    /// Resource action types for authorization checks
    /// </summary>
    public enum ResourceAction
    {
        Read,
        Create,
        Update,
        Delete,
        List
    }

    /// <summary>
    /// Resource types supported by the authorization system
    /// </summary>
    public enum ResourceType
    {
        Song,
        Setlist,
        SetlistSong
    }

    /// <summary>
    /// Validates that a user ID is not null or empty
    /// </summary>
    /// <param name="userId">The user ID to validate</param>
    /// <param name="resourceType">The type of resource being accessed</param>
    /// <param name="resourceId">The ID of the resource being accessed</param>
    /// <param name="action">The action being attempted</param>
    /// <param name="logger">Logger instance for security event logging</param>
    /// <returns>Authorization result with success or failure details</returns>
    public static AuthorizationResult ValidateUserId(
        string? userId, 
        ResourceType resourceType, 
        string resourceId, 
        ResourceAction action,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Security violation: Invalid user ID attempt for {ResourceType} {ResourceId} ({Action})",
                resourceType, resourceId, action);
            
            return AuthorizationResult.InvalidUser(
                userId ?? "null", 
                resourceType.ToString(), 
                resourceId, 
                action.ToString());
        }

        return AuthorizationResult.Success(userId, resourceType.ToString(), resourceId, action.ToString());
    }

    /// <summary>
    /// Creates a comprehensive security context for logging and auditing
    /// </summary>
    /// <param name="userId">The requesting user's ID</param>
    /// <param name="resourceType">The type of resource being accessed</param>
    /// <param name="resourceId">The ID of the resource being accessed</param>
    /// <param name="action">The action being attempted</param>
    /// <param name="additionalContext">Additional context information</param>
    /// <returns>Dictionary containing security context information</returns>
    public static Dictionary<string, object> CreateSecurityContext(
        string userId,
        ResourceType resourceType,
        string resourceId,
        ResourceAction action,
        Dictionary<string, object>? additionalContext = null)
    {
        var context = new Dictionary<string, object>
        {
            { "UserId", userId },
            { "ResourceType", resourceType.ToString() },
            { "ResourceId", resourceId },
            { "Action", action.ToString() },
            { "Timestamp", DateTime.UtcNow },
            { "SecurityCheck", "ResourceOwnership" }
        };

        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        return context;
    }

    /// <summary>
    /// Logs successful authorization events for security auditing
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="result">Authorization result to log</param>
    public static void LogAuthorizationSuccess(ILogger logger, AuthorizationResult result)
    {
        logger.LogInformation("Authorization successful: User {UserId} {Action} access to {ResourceType} {ResourceId}",
            result.UserId, result.Action, result.ResourceType, result.ResourceId);
    }

    /// <summary>
    /// Logs failed authorization events for security monitoring and incident response
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="result">Authorization result to log</param>
    /// <param name="requestContext">Additional request context for security analysis</param>
    public static void LogAuthorizationFailure(ILogger logger, AuthorizationResult result, Dictionary<string, object>? requestContext = null)
    {
        var contextInfoBuilder = new StringBuilder();
        if (requestContext != null)
        {
            bool first = true;
            foreach (var kvp in requestContext)
            {
                if (!first) contextInfoBuilder.Append(", ");
                contextInfoBuilder.Append(kvp.Key).Append("=").Append(kvp.Value);
                first = false;
            }
        }
        var contextInfo = requestContext != null ? contextInfoBuilder.ToString() : "none";
        
        logger.LogWarning("SECURITY ALERT: Authorization failed - User {UserId} attempted {Action} on {ResourceType} {ResourceId}. Reason: {Reason}. Context: {Context}",
            result.UserId, result.Action, result.ResourceType, result.ResourceId, result.Reason, contextInfo);
    }

    /// <summary>
    /// Logs suspicious authorization patterns that may indicate security threats
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="userId">User attempting the action</param>
    /// <param name="pattern">Description of the suspicious pattern</param>
    /// <param name="details">Additional details about the suspicious activity</param>
    public static void LogSuspiciousActivity(ILogger logger, string userId, string pattern, Dictionary<string, object> details)
    {
        var detailsInfoBuilder = new StringBuilder();
        bool first = true;
        foreach (var kvp in details)
        {
            if (!first) detailsInfoBuilder.Append(", ");
            detailsInfoBuilder.Append(kvp.Key).Append("=").Append(kvp.Value);
            first = false;
        }
        var detailsInfo = detailsInfoBuilder.ToString();
        
        logger.LogError("SECURITY THREAT: Suspicious activity detected - User {UserId}, Pattern: {Pattern}, Details: {Details}",
            userId, pattern, detailsInfo);
    }

    /// <summary>
    /// Validates resource ownership with enhanced security context
    /// </summary>
    /// <param name="resourceUserId">The user ID associated with the resource</param>
    /// <param name="requestingUserId">The user ID making the request</param>
    /// <param name="resourceType">The type of resource being accessed</param>
    /// <param name="resourceId">The ID of the resource</param>
    /// <param name="action">The action being attempted</param>
    /// <param name="logger">Logger for security events</param>
    /// <returns>Authorization result with detailed security information</returns>
    public static AuthorizationResult ValidateResourceOwnership(
        string? resourceUserId,
        string requestingUserId,
        ResourceType resourceType,
        string resourceId,
        ResourceAction action,
        ILogger logger)
    {
        // First validate the requesting user ID
        var userValidation = ValidateUserId(requestingUserId, resourceType, resourceId, action, logger);
        if (!userValidation.IsAuthorized)
        {
            return userValidation;
        }

        // Check if resource exists (resourceUserId is not null)
        if (string.IsNullOrEmpty(resourceUserId))
        {
            LogAuthorizationFailure(logger, AuthorizationResult.NotFound(requestingUserId, resourceType.ToString(), resourceId, action.ToString()));
            return AuthorizationResult.NotFound(requestingUserId, resourceType.ToString(), resourceId, action.ToString());
        }

        // Check if user owns the resource
        if (resourceUserId != requestingUserId)
        {
            var forbiddenResult = AuthorizationResult.Forbidden(requestingUserId, resourceType.ToString(), resourceId, action.ToString(), resourceUserId);
            LogAuthorizationFailure(logger, forbiddenResult, CreateSecurityContext(requestingUserId, resourceType, resourceId, action));
            return forbiddenResult;
        }

        // Authorization successful
        var successResult = AuthorizationResult.Success(requestingUserId, resourceType.ToString(), resourceId, action.ToString());
        LogAuthorizationSuccess(logger, successResult);
        return successResult;
    }

    /// <summary>
    /// Validates bulk resource access for operations affecting multiple resources
    /// </summary>
    /// <param name="resourceUserIds">User IDs associated with each resource</param>
    /// <param name="requestingUserId">The user ID making the request</param>
    /// <param name="resourceType">The type of resources being accessed</param>
    /// <param name="resourceIds">The IDs of the resources</param>
    /// <param name="action">The action being attempted</param>
    /// <param name="logger">Logger for security events</param>
    /// <returns>Dictionary of resource IDs to authorization results</returns>
    public static Dictionary<string, AuthorizationResult> ValidateBulkResourceOwnership(
        Dictionary<string, string?> resourceUserIds,
        string requestingUserId,
        ResourceType resourceType,
        ResourceAction action,
        ILogger logger)
    {
        var results = new Dictionary<string, AuthorizationResult>();

        foreach (var kvp in resourceUserIds)
        {
            var resourceId = kvp.Key;
            var resourceUserId = kvp.Value;

            results[resourceId] = ValidateResourceOwnership(
                resourceUserId, 
                requestingUserId, 
                resourceType, 
                resourceId, 
                action, 
                logger);
        }

        // Log bulk operation summary
        var successCount = results.Values.Count(r => r.IsAuthorized);
        var failureCount = results.Values.Count(r => !r.IsAuthorized);

        if (failureCount > 0)
        {
            logger.LogWarning("Bulk authorization completed with {SuccessCount} successes and {FailureCount} failures for user {UserId} on {ResourceType}",
                successCount, failureCount, requestingUserId, resourceType);
        }
        else
        {
            logger.LogInformation("Bulk authorization successful: {Count} {ResourceType} resources authorized for user {UserId}",
                successCount, resourceType, requestingUserId);
        }

        return results;
    }
}