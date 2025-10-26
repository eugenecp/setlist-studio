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
            if (!IsUserAuthenticated(user))
            {
                return GetAnonymousUserId();
            }

            var rawUserId = ExtractRawUserId(user!);
            return SanitizeExtractedUserId(rawUserId);
        }

        /// <summary>
        /// Checks if the user is authenticated and valid for ID extraction
        /// </summary>
        /// <param name="user">The user claims principal to validate</param>
        /// <returns>True if the user is authenticated, false otherwise</returns>
        private static bool IsUserAuthenticated(ClaimsPrincipal? user)
        {
            return user?.Identity?.IsAuthenticated == true;
        }

        /// <summary>
        /// Returns the standard anonymous user identifier
        /// </summary>
        /// <returns>The anonymous user ID constant</returns>
        private static string GetAnonymousUserId()
        {
            return "anonymous";
        }

        /// <summary>
        /// Extracts the raw user ID from an authenticated user's claims or identity
        /// </summary>
        /// <param name="user">The authenticated user claims principal</param>
        /// <returns>The raw (unsanitized) user ID</returns>
        private static string ExtractRawUserId(ClaimsPrincipal user)
        {
            // Try NameIdentifier claim first (most reliable)
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                return userIdClaim;
            }

            // Fallback to Identity.Name
            var identityName = user.Identity?.Name;
            if (!string.IsNullOrEmpty(identityName))
            {
                return identityName;
            }

            // Final fallback
            return GetAnonymousUserId();
        }

        /// <summary>
        /// Sanitizes the extracted user ID to break taint chains and ensure safe logging
        /// </summary>
        /// <param name="rawUserId">The raw user ID to sanitize</param>
        /// <returns>A sanitized user ID safe for logging</returns>
        private static string SanitizeExtractedUserId(string rawUserId)
        {
            // Immediately sanitize the user ID to break taint chains
            return SecureLoggingHelper.SanitizeUserId(rawUserId) ?? GetAnonymousUserId();
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