using Microsoft.AspNetCore.Http;

namespace SetlistStudio.Web.Utilities;

/// <summary>
/// Utility class for extracting client IP addresses from HTTP contexts
/// </summary>
public static class IpAddressUtility
{
    /// <summary>
    /// Extracts the client IP address from the HTTP context, considering proxy scenarios
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The client IP address or "Unknown" if not available</returns>
    public static string GetClientIpAddress(HttpContext context)
    {
        // Try forwarded headers first (for load balancers/proxies)
        var forwardedIp = GetForwardedIpAddress(context);
        if (!string.IsNullOrEmpty(forwardedIp))
            return forwardedIp;

        // Try real IP header (common with nginx)
        var realIp = GetRealIpAddress(context);
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fall back to connection remote IP
        return GetRemoteConnectionIp(context);
    }

    /// <summary>
    /// Extracts IP from X-Forwarded-For header
    /// </summary>
    private static string? GetForwardedIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(forwardedFor))
            return null;

        // Find the first non-empty IP in the chain (the original client)
        return forwardedFor.Split(',')
            .Select(ip => ip.Trim())
            .FirstOrDefault(trimmedIp => !string.IsNullOrWhiteSpace(trimmedIp));
    }

    /// <summary>
    /// Extracts IP from X-Real-IP header
    /// </summary>
    private static string? GetRealIpAddress(HttpContext context)
    {
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(realIp) ? null : realIp;
    }

    /// <summary>
    /// Gets IP from direct connection
    /// </summary>
    private static string GetRemoteConnectionIp(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}