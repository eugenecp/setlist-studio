using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SetlistStudio.Core.Security;

namespace SetlistStudio.Web.Security
{
    /// <summary>
    /// Provides secure methods for extracting and sanitizing user data from HTTP context.
    /// This utility ensures all user data is immediately sanitized to prevent security vulnerabilities.
    /// </summary>
    public static class SecureUserContext
    {
        /// <summary>
        /// Safely extracts and sanitizes the user ID from the current user context.
        /// This method prevents user-controlled data from being used in logging without sanitization.
        /// </summary>
        /// <param name="user">The current user claims principal</param>
        /// <returns>A sanitized user ID safe for logging and storage</returns>
        public static string GetSanitizedUserId(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return "anonymous";
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? user.Identity?.Name 
                ?? "anonymous";
            
            if (user.Identity is not null)
            {
                userId = userId ?? user.Identity.Name ?? "anonymous";
            }

            // Immediately sanitize the user ID to break taint chains
            return SecureLoggingHelper.SanitizeUserId(userId) ?? "anonymous";
        }

        /// <summary>
        /// Safely extracts and sanitizes the user name from the current user context.
        /// </summary>
        /// <param name="user">The current user claims principal</param>
        /// <returns>A sanitized user name safe for logging</returns>
        public static string GetSanitizedUserName(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return "Anonymous User";
            }

            var userName = "Unknown User";
            if (user.Identity is not null)
            {
                userName = user.Identity.Name ?? "Unknown User";
            }
            return SecureLoggingHelper.SanitizeMessage(userName) ?? "Unknown User";
        }

        /// <summary>
        /// Safely extracts and sanitizes user email from claims.
        /// </summary>
        /// <param name="user">The current user claims principal</param>
        /// <returns>A sanitized email address safe for logging</returns>
        public static string GetSanitizedUserEmail(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return "anonymous@localhost";
            }

            var email = user.FindFirst(ClaimTypes.Email)?.Value 
                ?? user.FindFirst("email")?.Value 
                ?? "unknown@localhost";

            return SecureLoggingHelper.SanitizeMessage(email) ?? "unknown@localhost";
        }

        /// <summary>
        /// Safely extracts and sanitizes client IP address from HTTP context.
        /// Masks last octet for IPv4 and last segments for IPv6 to protect user privacy.
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>A sanitized IP address safe for logging</returns>
        public static string GetSanitizedClientIp(HttpContext? context)
        {
            if (context is null)
            {
                return "unknown";
            }

            // Try to get real IP from forwarded headers first (for reverse proxy scenarios)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var firstIp = ips[0].Trim(); // First IP is the original client
                return SecureLoggingHelper.SanitizeIpAddress(firstIp);
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return SecureLoggingHelper.SanitizeIpAddress(realIp);
            }

            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            return SecureLoggingHelper.SanitizeIpAddress(remoteIp);
        }

        /// <summary>
        /// Safely extracts and sanitizes user agent from HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>A sanitized user agent string safe for logging</returns>
        public static string GetSanitizedUserAgent(HttpContext? context)
        {
            if (context is null)
            {
                return "Unknown";
            }

            var userAgent = context.Request.Headers.UserAgent.ToString();
            
            // Handle empty or whitespace user agent
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return "Unknown";
            }
            
            return SecureLoggingHelper.SanitizeMessage(userAgent) ?? "Unknown";
        }

        /// <summary>
        /// Safely extracts and sanitizes request path from HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>A sanitized request path safe for logging</returns>
        public static string GetSanitizedRequestPath(HttpContext? context)
        {
            if (context is null)
            {
                return "/unknown";
            }

            var path = context.Request.Path.ToString();
            return SecureLoggingHelper.SanitizeMessage(path) ?? "/unknown";
        }

        /// <summary>
        /// Creates a secure logging context with all user and request data sanitized.
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>A dictionary of sanitized context data safe for logging</returns>
        public static Dictionary<string, string> CreateSecureLoggingContext(HttpContext? context)
        {
            return new Dictionary<string, string>
            {
                ["UserId"] = GetSanitizedUserId(context?.User),
                ["UserName"] = GetSanitizedUserName(context?.User),
                ["ClientIp"] = GetSanitizedClientIp(context),
                ["UserAgent"] = GetSanitizedUserAgent(context),
                ["RequestPath"] = GetSanitizedRequestPath(context),
                ["RequestMethod"] = SecureLoggingHelper.SanitizeMessage(context?.Request.Method ?? "UNKNOWN") ?? "UNKNOWN"
            };
        }
    }
}