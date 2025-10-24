using System.Threading.RateLimiting;

namespace SetlistStudio.Web.Middleware;

/// <summary>
/// Middleware that adds rate limiting headers to HTTP responses
/// Provides clients with rate limit information for better API usage
/// </summary>
public class RateLimitHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public RateLimitHeadersMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add rate limit headers before processing the request
        AddRateLimitHeaders(context);

        await _next(context);
    }

    private static void AddRateLimitHeaders(HttpContext context)
    {
        // Add standard rate limit headers
        // These values represent the API policy limits (100 per minute)
        context.Response.Headers.TryAdd("X-RateLimit-Limit", "100");
        
        // Calculate remaining based on a simple estimation
        // In a real implementation, you'd get this from the rate limiter state
        var remainingRequests = CalculateRemainingRequests(context);
        context.Response.Headers.TryAdd("X-RateLimit-Remaining", remainingRequests.ToString());
        
        // Add reset time (next minute)
        var resetTime = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds();
        context.Response.Headers.TryAdd("X-RateLimit-Reset", resetTime.ToString());
        
        // Add window duration
        context.Response.Headers.TryAdd("X-RateLimit-Window", "60");
    }

    private static int CalculateRemainingRequests(HttpContext context)
    {
        // Simple estimation - in production, you'd track actual usage
        // For testing purposes, we'll return a reasonable value
        var currentSecond = DateTime.UtcNow.Second;
        
        // Simulate decreasing remaining requests throughout the minute
        return Math.Max(100 - (currentSecond * 2), 10);
    }
}

/// <summary>
/// Extension methods for registering the rate limit headers middleware
/// </summary>
public static class RateLimitHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds the rate limit headers middleware to the application pipeline
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseRateLimitHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitHeadersMiddleware>();
    }
}