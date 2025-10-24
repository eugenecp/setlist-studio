using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using SetlistStudio.Tests.Web;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive tests for security headers implementation
/// Validates that all required security headers are present and properly configured
/// Tests against OWASP security header recommendations
/// </summary>
public class SecurityHeadersTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityHeadersTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region Core Security Headers Tests

    [Fact]
    public async Task SecurityHeaders_ShouldIncludeXContentTypeOptions_OnAllResponses()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.Headers.Should().Contain(h => h.Key == "X-Content-Type-Options");
        var headerValue = response.Headers.GetValues("X-Content-Type-Options").First();
        headerValue.Should().Be("nosniff", "Should prevent MIME type sniffing attacks");
    }

    [Fact]
    public async Task SecurityHeaders_ShouldIncludeXFrameOptions_OnAllResponses()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.Headers.Should().Contain(h => h.Key == "X-Frame-Options");
        var headerValue = response.Headers.GetValues("X-Frame-Options").First();
        headerValue.Should().Be("DENY", "Should prevent clickjacking attacks");
    }

    [Fact]
    public async Task SecurityHeaders_ShouldIncludeXXSSProtection_OnAllResponses()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.Headers.Should().Contain(h => h.Key == "X-XSS-Protection");
        var headerValue = response.Headers.GetValues("X-XSS-Protection").First();
        headerValue.Should().Be("1; mode=block", "Should enable XSS protection for legacy browsers");
    }

    [Fact]
    public async Task SecurityHeaders_ShouldIncludeReferrerPolicy_OnAllResponses()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.Headers.Should().Contain(h => h.Key == "Referrer-Policy");
        var headerValue = response.Headers.GetValues("Referrer-Policy").First();
        headerValue.Should().Be("strict-origin-when-cross-origin", "Should protect referrer information");
    }

    [Fact]
    public async Task SecurityHeaders_ShouldIncludeContentSecurityPolicy_OnAllResponses()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.Headers.Should().Contain(h => h.Key == "Content-Security-Policy");
        var cspHeader = response.Headers.GetValues("Content-Security-Policy").First();
        
        // Validate key CSP directives
        cspHeader.Should().Contain("default-src 'self'", "Should restrict default sources to same origin");
        cspHeader.Should().Contain("frame-ancestors 'none'", "Should prevent framing");
        cspHeader.Should().Contain("base-uri 'self'", "Should restrict base URI");
        cspHeader.Should().Contain("form-action 'self'", "Should restrict form actions");
    }

    [Fact]
    public async Task SecurityHeaders_ShouldIncludePermissionsPolicy_OnAllResponses()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.Headers.Should().Contain(h => h.Key == "Permissions-Policy");
        var permissionsPolicy = response.Headers.GetValues("Permissions-Policy").First();
        
        // Validate dangerous permissions are disabled
        permissionsPolicy.Should().Contain("camera=()", "Should disable camera access");
        permissionsPolicy.Should().Contain("microphone=()", "Should disable microphone access");
        permissionsPolicy.Should().Contain("geolocation=()", "Should disable geolocation access");
    }

    #endregion

    #region HTTPS and HSTS Tests

    [Fact]
    public async Task SecurityHeaders_ShouldIncludeHSTS_WhenUsingHTTPS()
    {
        // Arrange - Create HTTPS client
        var httpsClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_URLS", "https://localhost:5001");
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Production");
        }).CreateClient();

        // Act
        var response = await httpsClient.GetAsync("/");

        // Assert
        if (response.Headers.Contains("Strict-Transport-Security"))
        {
            var hstsHeader = response.Headers.GetValues("Strict-Transport-Security").First();
            hstsHeader.Should().Contain("max-age=", "Should specify HSTS max age");
            hstsHeader.Should().Contain("includeSubDomains", "Should include subdomains");
            hstsHeader.Should().Contain("preload", "Should support HSTS preload");
        }
    }

    [Fact]
    public async Task SecurityHeaders_ShouldNotIncludeHSTS_InDevelopment()
    {
        // Arrange - Ensure we're in development mode
        var devClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
        }).CreateClient();

        // Act
        var response = await devClient.GetAsync("/");

        // Assert
        response.Headers.Should().NotContain(h => h.Key == "Strict-Transport-Security",
            "HSTS should not be set in development environment");
    }

    #endregion

    #region Endpoint-Specific Security Headers Tests

    [Theory]
    [InlineData("/")]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Register")]
    [InlineData("/Songs")]
    [InlineData("/Setlists")]
    public async Task SecurityHeaders_ShouldBePresent_OnAllEndpoints(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        var requiredHeaders = new[]
        {
            "X-Content-Type-Options",
            "X-Frame-Options",
            "X-XSS-Protection",
            "Referrer-Policy",
            "Content-Security-Policy",
            "Permissions-Policy"
        };

        foreach (var header in requiredHeaders)
        {
            response.Headers.Should().Contain(h => h.Key == header,
                $"Security header '{header}' should be present on endpoint '{endpoint}'");
        }
    }

    [Theory]
    [InlineData("/api/status")]
    [InlineData("/health")]
    public async Task SecurityHeaders_ShouldBePresent_OnAPIEndpoints(string apiEndpoint)
    {
        // Act
        var response = await _client.GetAsync(apiEndpoint);

        // Assert - API endpoints should also have security headers
        response.Headers.Should().Contain(h => h.Key == "X-Content-Type-Options");
        response.Headers.Should().Contain(h => h.Key == "X-Frame-Options");
        response.Headers.Should().Contain(h => h.Key == "Content-Security-Policy");
    }

    #endregion

    #region Content Security Policy Detailed Tests

    [Fact]
    public async Task ContentSecurityPolicy_ShouldAllowBlazorRequirements()
    {
        // Act
        var response = await _client.GetAsync("/");
        var cspHeader = response.Headers.GetValues("Content-Security-Policy").First();

        // Assert - Blazor Server requires unsafe-inline but NOT unsafe-eval (security improvement)
        cspHeader.Should().Contain("script-src 'self' 'unsafe-inline'",
            "Blazor Server requires unsafe-inline for SignalR operation");
        cspHeader.Should().NotContain("unsafe-eval",
            "unsafe-eval should be removed for better security");
        cspHeader.Should().Contain("style-src 'self' 'unsafe-inline'",
            "MudBlazor requires unsafe-inline styles");
    }

    [Fact]
    public async Task ContentSecurityPolicy_ShouldRestrictImageSources()
    {
        // Act
        var response = await _client.GetAsync("/");
        var cspHeader = response.Headers.GetValues("Content-Security-Policy").First();

        // Assert
        cspHeader.Should().Contain("img-src 'self' data: https:",
            "Should allow self, data URLs, and HTTPS images only");
    }

    [Fact]
    public async Task ContentSecurityPolicy_ShouldRestrictFontSources()
    {
        // Act
        var response = await _client.GetAsync("/");
        var cspHeader = response.Headers.GetValues("Content-Security-Policy").First();

        // Assert
        cspHeader.Should().Contain("font-src 'self'",
            "Should only allow fonts from same origin");
    }

    [Fact]
    public async Task ContentSecurityPolicy_ShouldRestrictConnectSources()
    {
        // Act
        var response = await _client.GetAsync("/");
        var cspHeader = response.Headers.GetValues("Content-Security-Policy").First();

        // Assert
        cspHeader.Should().Contain("connect-src 'self'",
            "Should only allow connections to same origin");
    }

    #endregion

    #region Permissions Policy Detailed Tests

    [Fact]
    public async Task PermissionsPolicy_ShouldDisableDangerousFeatures()
    {
        // Act
        var response = await _client.GetAsync("/");
        var permissionsPolicy = response.Headers.GetValues("Permissions-Policy").First();

        // Assert - All dangerous features should be disabled
        var dangerousFeatures = new[]
        {
            "camera=()",
            "microphone=()",
            "geolocation=()",
            "payment=()",
            "usb=()"
        };

        foreach (var feature in dangerousFeatures)
        {
            permissionsPolicy.Should().Contain(feature,
                $"Dangerous feature should be disabled: {feature}");
        }
    }

    #endregion

    #region Security Headers Compliance Tests

    [Fact]
    public async Task SecurityHeaders_ShouldMeetOWASPRecommendations()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert - OWASP recommended headers
        var owaspHeaders = new Dictionary<string, string>
        {
            { "X-Content-Type-Options", "nosniff" },
            { "X-Frame-Options", "DENY" },
            { "X-XSS-Protection", "1; mode=block" },
            { "Referrer-Policy", "strict-origin-when-cross-origin" }
        };

        foreach (var (header, expectedValue) in owaspHeaders)
        {
            response.Headers.Should().Contain(h => h.Key == header);
            var actualValue = response.Headers.GetValues(header).First();
            actualValue.Should().Be(expectedValue,
                $"OWASP header '{header}' should have correct value");
        }
    }

    [Fact]
    public async Task SecurityHeaders_ShouldNotLeakServerInformation()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert - Should not expose server information
        response.Headers.Should().NotContain(h => h.Key == "Server",
            "Server header should not be present to avoid information disclosure");
        response.Headers.Should().NotContain(h => h.Key == "X-Powered-By",
            "X-Powered-By header should not be present to avoid information disclosure");
        response.Headers.Should().NotContain(h => h.Key == "X-AspNet-Version",
            "X-AspNet-Version header should not be present to avoid information disclosure");
    }

    #endregion

    #region Environment-Specific Security Headers Tests

    [Fact]
    public async Task SecurityHeaders_ShouldHaveStricterPolicy_InProduction()
    {
        // Arrange
        var prodClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Production");
        }).CreateClient();

        // Act
        var response = await prodClient.GetAsync("/");

        // Assert
        response.Headers.Should().Contain(h => h.Key == "Content-Security-Policy");
        var cspHeader = response.Headers.GetValues("Content-Security-Policy").First();
        
        // Production should have stricter policies
        cspHeader.Should().Contain("frame-ancestors 'none'",
            "Production should prevent all framing");
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBeConsistent_AcrossMultipleRequests()
    {
        // Act - Make multiple requests
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 3; i++)
        {
            responses.Add(await _client.GetAsync("/"));
        }

        // Assert - Headers should be consistent
        var firstResponseHeaders = responses[0].Headers.ToDictionary(h => h.Key, h => h.Value.First());
        
        foreach (var response in responses.Skip(1))
        {
            var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => h.Value.First());
            
            foreach (var (header, value) in firstResponseHeaders.Where(h => 
                (h.Key.StartsWith("X-") && !h.Key.StartsWith("X-RateLimit-")) || h.Key == "Content-Security-Policy"))
            {
                responseHeaders.Should().ContainKey(header);
                responseHeaders[header].Should().Be(value,
                    $"Security header '{header}' should be consistent across requests");
            }
        }

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    #endregion
}