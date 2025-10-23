using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace SetlistStudio.Web.Attributes;

/// <summary>
/// Pre-configured rate limiting attributes for different security levels
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Global policy for general endpoints
    /// </summary>
    public const string Global = "GlobalPolicy";

    /// <summary>
    /// API policy for standard API endpoints
    /// </summary>
    public const string Api = "ApiPolicy";

    /// <summary>
    /// Enhanced API policy for authenticated users
    /// </summary>
    public const string AuthenticatedApi = "AuthenticatedApiPolicy";

    /// <summary>
    /// Authentication policy for login/register endpoints
    /// </summary>
    public const string Auth = "AuthPolicy";

    /// <summary>
    /// Enhanced policy for authenticated users
    /// </summary>
    public const string Authenticated = "AuthenticatedPolicy";

    /// <summary>
    /// Strict policy for high-risk endpoints
    /// </summary>
    public const string Strict = "StrictPolicy";

    /// <summary>
    /// Sensitive operations policy
    /// </summary>
    public const string Sensitive = "SensitivePolicy";
}

/// <summary>
/// Attribute to mark endpoints that require CAPTCHA verification
/// under suspicious circumstances.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireCaptchaOnSuspiciousActivityAttribute : Attribute
{
    public RequireCaptchaOnSuspiciousActivityAttribute()
    {
    }

    /// <summary>
    /// Threshold for triggering CAPTCHA (number of violations)
    /// </summary>
    public int ViolationThreshold { get; set; } = 3;

    /// <summary>
    /// Time window for counting violations (in minutes)
    /// </summary>
    public int TimeWindowMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to require CAPTCHA for all unauthenticated requests
    /// </summary>
    public bool RequireForUnauthenticated { get; set; } = false;
}

/// <summary>
/// Attribute to configure multi-factor rate limiting parameters
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class MultiFactorRateLimitAttribute : Attribute
{
    public MultiFactorRateLimitAttribute()
    {
    }

    /// <summary>
    /// Enable IP-based rate limiting
    /// </summary>
    public bool EnableIpLimiting { get; set; } = true;

    /// <summary>
    /// Enable user-based rate limiting
    /// </summary>
    public bool EnableUserLimiting { get; set; } = true;

    /// <summary>
    /// Enable session-based rate limiting
    /// </summary>
    public bool EnableSessionLimiting { get; set; } = true;

    /// <summary>
    /// Enable user agent fingerprinting
    /// </summary>
    public bool EnableUserAgentFingerprinting { get; set; } = true;

    /// <summary>
    /// Enable network segment limiting (subnet-based)
    /// </summary>
    public bool EnableNetworkSegmentLimiting { get; set; } = false;

    /// <summary>
    /// Custom rate limit for this endpoint (requests per minute)
    /// </summary>
    public int? CustomRateLimit { get; set; }

    /// <summary>
    /// Custom time window for rate limiting (in minutes)
    /// </summary>
    public int? CustomTimeWindowMinutes { get; set; }
}

/// <summary>
/// Security-focused configuration for high-risk operations
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class SecurityRateLimitConfigAttribute : Attribute
{
    /// <summary>
    /// Require CAPTCHA after rate limit violations
    /// </summary>
    public bool RequireCaptchaOnViolation { get; set; } = true;

    /// <summary>
    /// Enable additional security logging
    /// </summary>
    public bool EnableSecurityLogging { get; set; } = true;

    /// <summary>
    /// Block suspicious user agents
    /// </summary>
    public bool BlockSuspiciousUserAgents { get; set; } = true;

    /// <summary>
    /// The rate limit policy to use
    /// </summary>
    public string PolicyName { get; set; } = RateLimitPolicies.Strict;
}

/// <summary>
/// Extension methods for applying rate limiting attributes
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Applies authentication rate limiting to the controller/action
    /// </summary>
    public static void ApplyAuthRateLimit(this Controller controller)
    {
        // This would be used by reflection or AOP frameworks
        // For now, it serves as documentation of the intended usage
    }

    /// <summary>
    /// Applies API rate limiting to the controller/action
    /// </summary>
    public static void ApplyApiRateLimit(this Controller controller)
    {
        // This would be used by reflection or AOP frameworks
        // For now, it serves as documentation of the intended usage
    }
}