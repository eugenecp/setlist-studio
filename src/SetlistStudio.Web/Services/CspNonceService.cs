using System.Security.Cryptography;
using System.Text;

namespace SetlistStudio.Web.Services;

/// <summary>
/// Service for generating Content Security Policy (CSP) nonces
/// Provides secure random nonces for inline scripts and styles
/// </summary>
public interface ICspNonceService
{
    /// <summary>
    /// Generates a new cryptographically secure nonce for CSP
    /// </summary>
    /// <returns>Base64-encoded nonce value</returns>
    string GenerateNonce();
    
    /// <summary>
    /// Gets or sets the current request's script nonce
    /// </summary>
    string? ScriptNonce { get; set; }
    
    /// <summary>
    /// Gets or sets the current request's style nonce
    /// </summary>
    string? StyleNonce { get; set; }
}

/// <summary>
/// Implementation of CSP nonce service using cryptographically secure random generation
/// </summary>
public class CspNonceService : ICspNonceService
{
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
    
    /// <summary>
    /// Current request's script nonce
    /// </summary>
    public string? ScriptNonce { get; set; }
    
    /// <summary>
    /// Current request's style nonce  
    /// </summary>
    public string? StyleNonce { get; set; }

    /// <summary>
    /// Generates a cryptographically secure nonce for CSP
    /// Uses 32 bytes of random data encoded as Base64
    /// </summary>
    /// <returns>Base64-encoded nonce value</returns>
    public string GenerateNonce()
    {
        var nonceBytes = new byte[32]; // 256 bits of entropy
        _rng.GetBytes(nonceBytes);
        return Convert.ToBase64String(nonceBytes);
    }
}

/// <summary>
/// Middleware to generate and set CSP nonces for each request
/// </summary>
public class CspNonceMiddleware
{
    private readonly RequestDelegate _next;

    public CspNonceMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context, ICspNonceService cspNonceService)
    {
        // Generate nonces for this request
        cspNonceService.ScriptNonce = cspNonceService.GenerateNonce();
        cspNonceService.StyleNonce = cspNonceService.GenerateNonce();
        
        // Store nonces in HttpContext for use in views
        context.Items["ScriptNonce"] = cspNonceService.ScriptNonce;
        context.Items["StyleNonce"] = cspNonceService.StyleNonce;
        
        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering CSP nonce services and middleware
/// </summary>
public static class CspNonceExtensions
{
    /// <summary>
    /// Adds CSP nonce services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCspNonce(this IServiceCollection services)
    {
        services.AddScoped<ICspNonceService, CspNonceService>();
        return services;
    }
    
    /// <summary>
    /// Adds CSP nonce middleware to the request pipeline
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseCspNonce(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CspNonceMiddleware>();
    }
}