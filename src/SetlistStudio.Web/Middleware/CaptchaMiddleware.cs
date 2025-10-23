using SetlistStudio.Web.Services;
using Microsoft.Extensions.Caching.Memory;

namespace SetlistStudio.Web.Middleware;

/// <summary>
/// Middleware that enforces CAPTCHA challenges for suspicious requests
/// to prevent automated attacks and rate limiting bypass attempts.
/// </summary>
public class CaptchaMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnhancedRateLimitingService _rateLimitingService;
    private readonly ILogger<CaptchaMiddleware> _logger;
    private readonly IMemoryCache _cache;

    public CaptchaMiddleware(
        RequestDelegate next,
        IEnhancedRateLimitingService rateLimitingService,
        ILogger<CaptchaMiddleware> logger,
        IMemoryCache cache)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _rateLimitingService = rateLimitingService ?? throw new ArgumentNullException(nameof(rateLimitingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip CAPTCHA for certain endpoints (static files, health checks, etc.)
        if (ShouldSkipCaptcha(context))
        {
            await _next(context);
            return;
        }

        var clientIp = GetClientIpAddress(context);
        var captchaBypassKey = $"captcha_bypass:{clientIp}";

        // Check if client has recently passed CAPTCHA
        if (_cache.TryGetValue(captchaBypassKey, out _))
        {
            await _next(context);
            return;
        }

        try
        {
            // Check if CAPTCHA is required
            var requiresCaptcha = await _rateLimitingService.ShouldRequireCaptchaAsync(context);
            
            if (requiresCaptcha)
            {
                // Check if CAPTCHA response is provided
                var captchaResponse = GetCaptchaResponse(context);
                
                if (string.IsNullOrEmpty(captchaResponse))
                {
                    // Return CAPTCHA challenge
                    await ReturnCaptchaChallenge(context, clientIp);
                    return;
                }

                try
                {
                    // Validate CAPTCHA response
                    var isValidCaptcha = await _rateLimitingService.ValidateCaptchaAsync(captchaResponse, clientIp);
                    
                    if (!isValidCaptcha)
                    {
                        _logger.LogWarning("Invalid CAPTCHA response from IP {ClientIp}", clientIp);
                        await ReturnCaptchaChallenge(context, clientIp, "Invalid CAPTCHA. Please try again.");
                        return;
                    }

                    // CAPTCHA passed - grant bypass for 30 minutes
                    _cache.Set(captchaBypassKey, true, TimeSpan.FromMinutes(30));
                    
                    _logger.LogInformation("CAPTCHA challenge passed for IP {ClientIp}", clientIp);
                }
                catch (HttpRequestException ex)
                {
                    // CAPTCHA service is unavailable - return too many requests
                    _logger.LogError(ex, "CAPTCHA service unavailable for IP {ClientIp}", clientIp);
                    context.Response.StatusCode = 429;
                    await context.Response.WriteAsync("CAPTCHA service temporarily unavailable. Please try again later.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            // Rate limiting service failed - continue without CAPTCHA check
            _logger.LogError(ex, "Rate limiting service failed for IP {ClientIp}. Continuing without CAPTCHA.", clientIp);
        }

        await _next(context);
    }

    private bool ShouldSkipCaptcha(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        if (string.IsNullOrEmpty(path))
            return false;

        // Skip CAPTCHA for static resources and system endpoints
        var skipPatterns = new[]
        {
            "/css/", "/js/", "/images/", "/fonts/", "/favicon.ico",
            "/health", "/ready", "/live", "/metrics",
            "/_framework/", "/_content/",
            "/captcha/", "/recaptcha/"
        };

        return skipPatterns.Any(pattern => path.Contains(pattern));
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Try to get real IP from forwarded headers (for reverse proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return ips[0].Trim(); // First IP is the original client
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string? GetCaptchaResponse(HttpContext context)
    {
        // Check form data for CAPTCHA response
        if (context.Request.HasFormContentType && context.Request.Form.ContainsKey("g-recaptcha-response"))
        {
            return context.Request.Form["g-recaptcha-response"];
        }

        // Check headers for CAPTCHA response (for API requests)
        if (context.Request.Headers.ContainsKey("X-Captcha-Response"))
        {
            return context.Request.Headers["X-Captcha-Response"];
        }

        // Check query string
        if (context.Request.Query.ContainsKey("captcha"))
        {
            return context.Request.Query["captcha"];
        }

        return null;
    }

    private async Task ReturnCaptchaChallenge(HttpContext context, string clientIp, string? errorMessage = null)
    {
        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "text/html";

        var isApiRequest = IsApiRequest(context);
        
        if (isApiRequest)
        {
            // Return JSON response for API requests
            context.Response.ContentType = "application/json";
            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "captcha_required",
                message = errorMessage ?? "CAPTCHA verification required to continue",
                captcha_site_key = GetCaptchaSiteKey(),
                retry_after = 60
            });
            
            await context.Response.WriteAsync(jsonResponse);
        }
        else
        {
            // Return HTML CAPTCHA challenge page
            var html = GenerateCaptchaChallengePage(context, errorMessage);
            await context.Response.WriteAsync(html);
        }

        _logger.LogInformation("CAPTCHA challenge issued to IP {ClientIp} for path {Path}", 
            clientIp, context.Request.Path);
    }

    private bool IsApiRequest(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        var acceptHeader = context.Request.Headers.Accept.ToString();
        
        return path?.StartsWith("/api/") == true || 
               acceptHeader.Contains("application/json") ||
               context.Request.Headers.ContainsKey("X-Requested-With");
    }

    private string GetCaptchaSiteKey()
    {
        // This should be configured in appsettings.json
        return "your-recaptcha-site-key"; // Replace with actual site key
    }

    private string GenerateCaptchaChallengePage(HttpContext context, string? errorMessage)
    {
        var siteKey = GetCaptchaSiteKey();
        var currentUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
        
        var errorHtml = !string.IsNullOrEmpty(errorMessage) 
            ? $"<div class='alert alert-danger'>{System.Web.HttpUtility.HtmlEncode(errorMessage)}</div>"
            : "";

        return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Security Verification - Setlist Studio</title>
    <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css' rel='stylesheet'>
    <script src='https://www.google.com/recaptcha/api.js' async defer></script>
    <style>
        body {{ background-color: #f8f9fa; }}
        .captcha-container {{ 
            max-width: 500px; 
            margin: 100px auto; 
            padding: 30px;
            background: white;
            border-radius: 10px;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
        }}
        .captcha-icon {{ 
            font-size: 48px; 
            color: #dc3545; 
            text-align: center; 
            margin-bottom: 20px; 
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='captcha-container'>
            <div class='captcha-icon'>🛡️</div>
            <h2 class='text-center mb-4'>Security Verification Required</h2>
            
            {errorHtml}
            
            <p class='text-center text-muted mb-4'>
                Our security system has detected unusual activity. Please complete the verification below to continue.
            </p>
            
            <form method='POST' action='{currentUrl}'>
                <div class='text-center mb-4'>
                    <div class='g-recaptcha' data-sitekey='{siteKey}'></div>
                </div>
                
                <div class='d-grid'>
                    <button type='submit' class='btn btn-primary btn-lg'>
                        Verify and Continue
                    </button>
                </div>
            </form>
            
            <div class='text-center mt-4'>
                <small class='text-muted'>
                    This security measure helps protect our service from automated attacks.
                    <br>
                    <a href='https://www.google.com/recaptcha/intro/v3.html' target='_blank'>
                        Learn more about reCAPTCHA
                    </a>
                </small>
            </div>
        </div>
    </div>
</body>
</html>";
    }
}

/// <summary>
/// Extension method to register the CAPTCHA middleware
/// </summary>
public static class CaptchaMiddlewareExtensions
{
    public static IApplicationBuilder UseCaptchaMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CaptchaMiddleware>();
    }
}