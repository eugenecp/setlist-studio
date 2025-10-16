using Microsoft.AspNetCore.Identity;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Security;

namespace SetlistStudio.Web.Security;

/// <summary>
/// Enhanced account lockout service with progressive lockout and comprehensive security logging
/// </summary>
public class EnhancedAccountLockoutService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SecurityEventLogger _securityEventLogger;
    private readonly ILogger<EnhancedAccountLockoutService> _logger;

    public EnhancedAccountLockoutService(
        UserManager<ApplicationUser> userManager,
        SecurityEventLogger securityEventLogger,
        ILogger<EnhancedAccountLockoutService> logger)
    {
        _userManager = userManager;
        _securityEventLogger = securityEventLogger;
        _logger = logger;
    }

    /// <summary>
    /// Handle failed login attempt with progressive lockout and security logging
    /// </summary>
    /// <param name="user">User who failed login attempt</param>
    /// <param name="ipAddress">IP address of the failed attempt</param>
    /// <param name="userAgent">User agent of the failed attempt</param>
    /// <returns>Lockout information</returns>
    public async Task<LockoutResult> HandleFailedLoginAttemptAsync(ApplicationUser user, string ipAddress, string userAgent)
    {
        if (user == null)
        {
            // Log suspicious activity for non-existent user attempts
            _securityEventLogger.LogSuspiciousActivity(
                "NonExistentUserLogin",
                "Login attempt for non-existent user from IP: " + ipAddress,
                null,
                SecurityEventSeverity.High,
                new { IpAddress = ipAddress, UserAgent = userAgent }
            );
            
            return new LockoutResult
            {
                IsLockedOut = false,
                Message = "Invalid login attempt."
            };
        }

        // Record the failed attempt
        var result = await _userManager.AccessFailedAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to record access failure for user {UserId}", user.Id);
            return new LockoutResult
            {
                IsLockedOut = false,
                Message = "An error occurred processing the login attempt."
            };
        }

        var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);
        var isLockedOut = await _userManager.IsLockedOutAsync(user);
        
        if (isLockedOut)
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var progressiveLockoutTime = CalculateProgressiveLockoutTime(failedAttempts);
            
            // Apply progressive lockout if this is a repeat offender
            if (failedAttempts > 5)
            {
                var newLockoutEnd = DateTimeOffset.UtcNow.Add(progressiveLockoutTime);
                await _userManager.SetLockoutEndDateAsync(user, newLockoutEnd);
                lockoutEnd = newLockoutEnd;
            }
            
            // Log account lockout event
            _securityEventLogger.LogAccountLockout(
                user.Id,
                progressiveLockoutTime,
                failedAttempts,
                ipAddress
            );

            return new LockoutResult
            {
                IsLockedOut = true,
                LockoutEnd = lockoutEnd,
                FailedAttempts = failedAttempts,
                Message = $"Account locked until {lockoutEnd:yyyy-MM-dd HH:mm:ss UTC} due to {failedAttempts} failed login attempts."
            };
        }
        else
        {
            // Log failed authentication attempt
            _securityEventLogger.LogAuthenticationFailure(
                user.Id,
                "Password",
                $"Failed login attempt {failedAttempts}/5",
                userAgent,
                ipAddress
            );

            return new LockoutResult
            {
                IsLockedOut = false,
                FailedAttempts = failedAttempts,
                RemainingAttempts = 5 - failedAttempts,
                Message = $"Invalid login attempt. {5 - failedAttempts} attempts remaining before account lockout."
            };
        }
    }

    /// <summary>
    /// Handle successful login - reset failed attempts and log success
    /// </summary>
    /// <param name="user">Successfully authenticated user</param>
    /// <param name="ipAddress">IP address of successful login</param>
    /// <param name="userAgent">User agent of successful login</param>
    public async Task HandleSuccessfulLoginAsync(ApplicationUser user, string ipAddress, string userAgent)
    {
        if (user == null) return;

        var previousFailedAttempts = await _userManager.GetAccessFailedCountAsync(user);
        
        // Reset failed access count on successful login
        if (previousFailedAttempts > 0)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        // Log successful authentication
        _securityEventLogger.LogAuthenticationSuccess(
            user.Id,
            "Password",
            userAgent,
            ipAddress
        );
    }

    /// <summary>
    /// Calculate progressive lockout time based on failed attempts
    /// </summary>
    /// <param name="failedAttempts">Number of failed attempts</param>
    /// <returns>Progressive lockout duration</returns>
    private static TimeSpan CalculateProgressiveLockoutTime(int failedAttempts)
    {
        return failedAttempts switch
        {
            <= 5 => TimeSpan.FromMinutes(5),      // First lockout: 5 minutes
            <= 10 => TimeSpan.FromMinutes(15),    // Second lockout: 15 minutes  
            <= 15 => TimeSpan.FromHours(1),       // Third lockout: 1 hour
            <= 20 => TimeSpan.FromHours(4),       // Fourth lockout: 4 hours
            <= 25 => TimeSpan.FromHours(12),      // Fifth lockout: 12 hours
            _ => TimeSpan.FromHours(24)           // Subsequent lockouts: 24 hours
        };
    }

    /// <summary>
    /// Checks if an IP address should be blocked due to suspicious activity
    /// </summary>
    /// <param name="ipAddress">IP address to check</param>
    /// <returns>True if IP should be blocked</returns>
    public Task<bool> IsIpAddressBlockedAsync(string ipAddress)
    {
        // This could be implemented with Redis or in-memory cache for production
        // For now, we'll use basic in-memory tracking
        // In production, consider implementing distributed caching for load balancing scenarios
        
        return Task.FromResult(false); // Placeholder - implement IP-based rate limiting if needed
    }
}

/// <summary>
/// Result of a lockout operation
/// </summary>
public class LockoutResult
{
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int FailedAttempts { get; set; }
    public int RemainingAttempts { get; set; }
    public string Message { get; set; } = string.Empty;
}