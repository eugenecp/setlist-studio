using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using SetlistStudio.Core.Security;

namespace SetlistStudio.Web.Services;

/// <summary>
/// Enhanced rate limiting service that implements multi-factor rate limiting
/// to prevent bypass through IP rotation, distributed attacks, and other evasion techniques.
/// </summary>
public interface IEnhancedRateLimitingService
{
    /// <summary>
    /// Gets a composite partition key based on multiple factors (IP, User, Session, Device)
    /// </summary>
    Task<string> GetCompositePartitionKeyAsync(HttpContext httpContext);
    
    /// <summary>
    /// Checks if the request should be challenged with CAPTCHA
    /// </summary>
    Task<bool> ShouldRequireCaptchaAsync(HttpContext httpContext);
    
    /// <summary>
    /// Records a rate limit violation for enhanced monitoring
    /// </summary>
    Task RecordRateLimitViolationAsync(HttpContext httpContext, string partitionKey);
    
    /// <summary>
    /// Gets the appropriate rate limit policy for the request
    /// </summary>
    string GetRateLimitPolicy(HttpContext httpContext);
    
    /// <summary>
    /// Validates CAPTCHA response
    /// </summary>
    Task<bool> ValidateCaptchaAsync(string captchaResponse, string clientIp);
}

public class EnhancedRateLimitingService : IEnhancedRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<EnhancedRateLimitingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, RateLimitViolationRecord> _violations;
    private readonly string _captchaSecretKey;

    public EnhancedRateLimitingService(
        IMemoryCache cache,
        ILogger<EnhancedRateLimitingService> logger,
        IConfiguration configuration)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _violations = new ConcurrentDictionary<string, RateLimitViolationRecord>();
        _captchaSecretKey = _configuration["Captcha:SecretKey"] ?? string.Empty;
    }

    public Task<string> GetCompositePartitionKeyAsync(HttpContext httpContext)
    {
        if (httpContext?.Request == null)
            return Task.FromResult("anonymous");

        var factors = new List<string>();

        // Factor 1: User Identity (highest priority for authenticated users)
        var userIdentity = httpContext.User?.Identity;
        if (userIdentity?.IsAuthenticated == true && !string.IsNullOrEmpty(userIdentity.Name))
        {
            factors.Add($"user:{userIdentity.Name}");
        }

        // Factor 2: IP Address (always include for geographical/network tracking)
        var clientIp = GetClientIpAddress(httpContext);
        if (!string.IsNullOrEmpty(clientIp))
        {
            factors.Add($"ip:{clientIp}");
        }

        // Factor 3: Session ID (for session-based tracking)
        try
        {
            var sessionId = httpContext.Session?.Id;
            if (!string.IsNullOrEmpty(sessionId))
            {
                factors.Add($"session:{sessionId}");
            }
        }
        catch (InvalidOperationException)
        {
            // Session not configured - skip session-based tracking
            // This is acceptable for enhanced security as we have other factors
        }

        // Factor 4: User Agent fingerprint (to detect automation/bots)
        var userAgentHash = GetUserAgentFingerprint(httpContext);
        if (!string.IsNullOrEmpty(userAgentHash))
        {
            factors.Add($"ua:{userAgentHash}");
        }

        // Factor 5: Network segment (subnet-based limiting)
        var networkSegment = GetNetworkSegment(clientIp);
        if (!string.IsNullOrEmpty(networkSegment))
        {
            factors.Add($"net:{networkSegment}");
        }

        // Create composite key
        var compositeKey = string.Join("|", factors);
        
        // Hash the composite key for consistent length and privacy
        using var sha256 = SHA256.Create();
        var hashedKey = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(compositeKey)));
        
        return Task.FromResult(hashedKey[..16]); // Use first 16 characters for readability in logs
    }

    public async Task<bool> ShouldRequireCaptchaAsync(HttpContext httpContext)
    {
        if (httpContext?.Request == null)
            return false;

        var clientIp = GetClientIpAddress(httpContext);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        
        // Check individual CAPTCHA trigger conditions
        var rateLimitViolations = await CheckRateLimitViolationHistoryAsync(clientIp);
        var suspiciousUserAgent = await CheckSuspiciousUserAgentAsync(userAgent);
        var distributedAttack = await CheckDistributedAttackPatternAsync(httpContext);
        var authFailures = await CheckAuthenticationFailurePatternAsync(httpContext);
        var highVelocity = await CheckHighVelocityRequestsAsync(clientIp);

        // Individual high-severity triggers that should require CAPTCHA alone
        if (suspiciousUserAgent)
        {
            var sanitizedClientIp = SecureLoggingHelper.SanitizeIpAddress(clientIp);
            var sanitizedUserAgent = SecureLoggingHelper.SanitizeMessage(userAgent);
            _logger.LogWarning("CAPTCHA required for IP {ClientIp} due to suspicious user agent: {UserAgent}", 
                sanitizedClientIp, sanitizedUserAgent);
            return true;
        }

        if (distributedAttack)
        {
            var sanitizedClientIp = SecureLoggingHelper.SanitizeIpAddress(clientIp);
            _logger.LogWarning("CAPTCHA required for IP {ClientIp} due to detected distributed attack pattern", 
                sanitizedClientIp);
            return true;
        }

        // Multiple other triggers should also require CAPTCHA
        var otherTriggers = new[] { rateLimitViolations, authFailures, highVelocity };
        var otherTriggerCount = otherTriggers.Count(t => t);
        
        if (otherTriggerCount >= 2)
        {
            var sanitizedClientIp = SecureLoggingHelper.SanitizeIpAddress(clientIp);
            _logger.LogWarning("CAPTCHA required for IP {ClientIp} due to {TriggerCount} security triggers", 
                sanitizedClientIp, otherTriggerCount);
            return true;
        }

        // Check endpoint-specific CAPTCHA requirements
        var endpoint = httpContext.Request.Path.Value?.ToLowerInvariant();
        if (IsHighRiskEndpoint(endpoint))
        {
            return await CheckHighRiskEndpointCriteria(httpContext);
        }

        return false;
    }

    public async Task RecordRateLimitViolationAsync(HttpContext httpContext, string partitionKey)
    {
        if (httpContext?.Request == null || string.IsNullOrEmpty(partitionKey))
            return;

        var clientIp = GetClientIpAddress(httpContext);
        var endpoint = httpContext.Request.Path.Value ?? "unknown";
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        var timestamp = DateTimeOffset.UtcNow;

        var violation = new RateLimitViolationRecord
        {
            PartitionKey = partitionKey,
            ClientIp = clientIp,
            Endpoint = endpoint,
            UserAgent = userAgent,
            Timestamp = timestamp,
            UserId = httpContext.User?.Identity?.Name
        };

        // Store violation record
        var violationKey = $"violation:{clientIp}:{timestamp:yyyyMMddHH}";
        _violations.TryAdd(violationKey, violation);

        // Enhanced logging with security context
        _logger.LogWarning("Rate limit violation recorded: {ViolationRecord}", 
            new { 
                violation.PartitionKey, 
                violation.ClientIp, 
                violation.Endpoint, 
                violation.UserId,
                UserAgentHash = GetUserAgentFingerprint(httpContext),
                Timestamp = violation.Timestamp 
            });

        // Update violation counters in cache
        await UpdateViolationCountersAsync(clientIp, endpoint);

        // Trigger security alerts if threshold exceeded
        await CheckSecurityThresholdsAsync(clientIp);
    }

    public string GetRateLimitPolicy(HttpContext httpContext)
    {
        if (httpContext?.Request == null)
            return "GlobalPolicy";

        var endpoint = httpContext.Request.Path.Value?.ToLowerInvariant();
        var method = httpContext.Request.Method;
        var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated == true;

        // Authentication endpoints - strictest limits
        if (IsAuthenticationEndpoint(endpoint))
        {
            return "AuthPolicy";
        }

        // Sensitive operations - enhanced limits (check before general API endpoints)
        if (IsSensitiveOperation(endpoint, method))
        {
            return "SensitivePolicy";
        }

        // High-risk endpoints - strict limits
        if (IsHighRiskEndpoint(endpoint))
        {
            return "StrictPolicy";
        }

        // API endpoints - moderate limits (general check after specific operations)
        if (IsApiEndpoint(endpoint))
        {
            return isAuthenticated ? "AuthenticatedApiPolicy" : "ApiPolicy";
        }

        // Default policy
        return isAuthenticated ? "AuthenticatedPolicy" : "GlobalPolicy";
    }

    public async Task<bool> ValidateCaptchaAsync(string captchaResponse, string clientIp)
    {
        if (string.IsNullOrEmpty(captchaResponse) || string.IsNullOrEmpty(_captchaSecretKey))
        {
            return false;
        }

        try
        {
            using var httpClient = new HttpClient();
            var parameters = new Dictionary<string, string>
            {
                ["secret"] = _captchaSecretKey,
                ["response"] = captchaResponse,
                ["remoteip"] = clientIp
            };

            using (var content = new FormUrlEncodedContent(parameters))
            {
                var response = await httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    // Parse JSON response to check success field
                    return jsonResponse.Contains("\"success\": true");
                }

                return false;
            }

            return false;
        }
        catch (Exception ex)
        {
            var sanitizedClientIp = SecureLoggingHelper.SanitizeIpAddress(clientIp);
            _logger.LogError(ex, "CAPTCHA validation failed for IP {ClientIp}", sanitizedClientIp);
            return false;
        }
    }

    #region Private Helper Methods

    private string GetClientIpAddress(HttpContext httpContext)
    {
        // Try to get real IP from forwarded headers (for reverse proxy scenarios)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return ips[0].Trim(); // First IP is the original client
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return httpContext.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetUserAgentFingerprint(HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrEmpty(userAgent))
            return string.Empty;

        // Create a hash of the user agent for fingerprinting
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(userAgent));
        return Convert.ToBase64String(hash)[..8]; // First 8 characters for brevity
    }

    private string GetNetworkSegment(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return string.Empty;

        // Extract /24 subnet for IPv4 (e.g., 192.168.1.0/24)
        var parts = ipAddress.Split('.');
        if (parts.Length == 4)
        {
            return $"{parts[0]}.{parts[1]}.{parts[2]}.0/24";
        }

        // For IPv6, extract /64 subnet
        if (ipAddress.Contains(':'))
        {
            var segments = ipAddress.Split(':');
            if (segments.Length >= 4)
            {
                return $"{segments[0]}:{segments[1]}:{segments[2]}:{segments[3]}::/64";
            }
        }

        return string.Empty;
    }

    private async Task<bool> CheckRateLimitViolationHistoryAsync(string? clientIp)
    {
        if (string.IsNullOrEmpty(clientIp))
            return false;

        var violationKey = $"violations:{clientIp}";
        var violationCount = await GetCachedCounterAsync(violationKey);
        
        // Trigger CAPTCHA if more than 3 violations in the last hour
        return violationCount > 3;
    }

    private Task<bool> CheckSuspiciousUserAgentAsync(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return Task.FromResult(true); // No user agent is suspicious

        var suspiciousPatterns = new[]
        {
            "bot", "crawler", "spider", "scraper", "curl", "wget",
            "python", "requests", "httpclient", "postman"
        };

        var result = suspiciousPatterns.Any(pattern => 
            userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(result);
    }

    private async Task<bool> CheckDistributedAttackPatternAsync(HttpContext httpContext)
    {
        // Check if multiple IPs from the same network segment are making requests
        var clientIp = GetClientIpAddress(httpContext);
        var networkSegment = GetNetworkSegment(clientIp);
        
        if (string.IsNullOrEmpty(networkSegment))
            return false;

        var segmentKey = $"segment_requests:{networkSegment}";
        var requestCount = await GetCachedCounterAsync(segmentKey);
        
        // If more than 50 requests from the same network segment in 5 minutes
        return requestCount > 50;
    }

    private async Task<bool> CheckAuthenticationFailurePatternAsync(HttpContext httpContext)
    {
        var clientIp = GetClientIpAddress(httpContext);
        var authFailureKey = $"auth_failures:{clientIp}";
        var failureCount = await GetCachedCounterAsync(authFailureKey);
        
        // Trigger CAPTCHA after 3 authentication failures
        return failureCount > 3;
    }

    private async Task<bool> CheckHighVelocityRequestsAsync(string? clientIp)
    {
        if (string.IsNullOrEmpty(clientIp))
            return false;

        var velocityKey = $"velocity:{clientIp}";
        var requestCount = await GetCachedCounterAsync(velocityKey);
        
        // High velocity = more than 100 requests per minute
        return requestCount > 100;
    }

    private async Task<bool> CheckHighRiskEndpointCriteria(HttpContext httpContext)
    {
        var clientIp = GetClientIpAddress(httpContext);
        var endpoint = httpContext.Request.Path.Value?.ToLowerInvariant();
        var endpointKey = $"endpoint_requests:{clientIp}:{endpoint}";
        var requestCount = await GetCachedCounterAsync(endpointKey);
        
        // Require CAPTCHA for high-risk endpoints after 5 requests per hour
        return requestCount > 5;
    }

    private Task<int> GetCachedCounterAsync(string key)
    {
        if (_cache == null)
            return Task.FromResult(0);

        var result = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return 0;
        });
        return Task.FromResult(result);
    }

    private Task UpdateViolationCountersAsync(string clientIp, string endpoint)
    {
        var now = DateTimeOffset.UtcNow;
        var hour = now.ToString("yyyyMMddHH");
        
        // Update various counters
        var counters = new[]
        {
            $"violations:{clientIp}",
            $"violations_hourly:{clientIp}:{hour}",
            $"endpoint_violations:{endpoint}:{hour}",
            $"global_violations:{hour}"
        };

        foreach (var counter in counters)
        {
            var currentCount = _cache.GetOrCreate(counter, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return 0;
            });
            
            _cache.Set(counter, currentCount + 1, TimeSpan.FromHours(1));
        }
        
        return Task.CompletedTask;
    }

    private async Task CheckSecurityThresholdsAsync(string clientIp)
    {
        var violationsKey = $"violations:{clientIp}";
        var violationCount = await GetCachedCounterAsync(violationsKey);
        
        // Alert thresholds
        if (violationCount >= 10)
        {
            var sanitizedClientIp = SecureLoggingHelper.SanitizeIpAddress(clientIp);
            _logger.LogCritical("SECURITY ALERT: IP {ClientIp} has exceeded violation threshold with {ViolationCount} violations", 
                sanitizedClientIp, violationCount);
        }
        else if (violationCount >= 5)
        {
            var sanitizedClientIp = SecureLoggingHelper.SanitizeIpAddress(clientIp);
            _logger.LogWarning("SECURITY WARNING: IP {ClientIp} approaching violation threshold with {ViolationCount} violations", 
                sanitizedClientIp, violationCount);
        }
    }

    private bool IsAuthenticationEndpoint(string? endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            return false;

        var authEndpoints = new[] 
        { 
            "/account/login", "/account/register", "/account/logout",
            "/api/auth", "/oauth", "/token"
        };

        return authEndpoints.Any(auth => endpoint.Contains(auth));
    }

    private bool IsApiEndpoint(string? endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            return false;

        return endpoint.StartsWith("/api/");
    }

    private bool IsHighRiskEndpoint(string? endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            return false;

        var highRiskEndpoints = new[]
        {
            "/account/", "/admin/", "/api/admin/", "/management/",
            "/security/", "/diagnostics/", "/health/"
        };

        return highRiskEndpoints.Any(risk => endpoint.Contains(risk));
    }

    private bool IsSensitiveOperation(string? endpoint, string method)
    {
        if (string.IsNullOrEmpty(endpoint))
            return false;

        // POST/PUT/DELETE operations on sensitive endpoints
        var sensitiveMethods = new[] { "POST", "PUT", "DELETE", "PATCH" };
        var sensitiveEndpoints = new[] { "/api/users", "/api/settings", "/api/export" };

        return sensitiveMethods.Contains(method) && 
               sensitiveEndpoints.Any(sensitive => endpoint.StartsWith(sensitive, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}

/// <summary>
/// Record of a rate limit violation for security monitoring
/// </summary>
public class RateLimitViolationRecord
{
    public string PartitionKey { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}