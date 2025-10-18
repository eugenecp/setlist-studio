using Microsoft.AspNetCore.Cors.Infrastructure;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FluentAssertions;
using Xunit;
using SetlistStudio.Web;

namespace SetlistStudio.Tests.Web.Security;

/// <summary>
/// Integration tests for CORS policies across different environments.
/// Tests origin validation, security headers, and environment-specific configurations.
/// </summary>
public class CorsSecurityTests : IClassFixture<CorsSecurityTests.TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CorsSecurityTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient("Development");
    }

    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly Dictionary<string, WebApplicationFactory<Program>> _factories = new();

        public HttpClient CreateClient(string environment = "Development")
        {
            if (!_factories.TryGetValue(environment, out var factory))
            {
                factory = CreateFactory(environment);
                _factories[environment] = factory;
            }

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "SetlistStudio-Test-Client/1.0");
            return client;
        }

        private WebApplicationFactory<Program> CreateFactory(string environment)
        {
            return new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // CRITICAL: Set environment FIRST, before service configuration
                    builder.UseEnvironment(environment);
                    
                    // Override configuration to provide proper test values
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        // Clear all existing configuration to ensure our test config takes precedence
                        config.Sources.Clear();
                        
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                            ["Authentication:Google:ClientId"] = "test-client-id-123456",
                            ["Authentication:Google:ClientSecret"] = "test-client-secret-abcdef",
                            ["Authentication:Microsoft:ClientId"] = "test-ms-client-id-789012",
                            ["Authentication:Microsoft:ClientSecret"] = "test-ms-client-secret-ghijkl",
                            ["Authentication:Facebook:AppId"] = "test-fb-app-id-345678",
                            ["Authentication:Facebook:AppSecret"] = "test-fb-app-secret-mnopqr",
                            ["AllowedHosts"] = "*", // Allow all hosts for testing
                            ["Logging:LogLevel:Default"] = "Warning",
                            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                            ["Security:CspReporting:Enabled"] = "false"
                        });
                    })
                    .ConfigureServices((context, services) =>
                    {
                        // Ensure the environment is correctly set for service configuration
                        // This is critical for CORS policy configuration which depends on environment
                    });
                });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var factory in _factories.Values)
                {
                    factory.Dispose();
                }
                _factories.Clear();
            }
            base.Dispose(disposing);
        }
    }

    [Fact]
    public async Task Development_CorsPreflight_ShouldAllowAllOrigins()
    {
        // Arrange
        var client = _factory.CreateClient("Development");
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/songs");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("*");
        response.Headers.GetValues("Access-Control-Allow-Methods").Should().Contain(m => 
            m.Contains("GET") && m.Contains("POST") && m.Contains("PUT") && m.Contains("DELETE"));
        response.Headers.GetValues("Access-Control-Allow-Headers").Should().Contain(h => 
            h.Contains("authorization", StringComparison.OrdinalIgnoreCase) && 
            h.Contains("content-type", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("https://setliststudio.com")]
    [InlineData("https://www.setliststudio.com")]
    [InlineData("https://api.setliststudio.com")]
    public async Task Production_CorsPreflight_ShouldAllowTrustedOrigins(string origin)
    {
        // Arrange
        var client = _factory.CreateClient("Production");
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain(origin);
    }

    [Theory]
    [InlineData("https://malicious-site.com")]
    [InlineData("http://setliststudio.com")] // HTTP not HTTPS
    [InlineData("https://setliststudio.com.evil.com")]
    [InlineData("https://fake-setliststudio.com")]
    public async Task Production_CorsPreflight_ShouldRejectUntrustedOrigins(string origin)
    {
        // Arrange
        var client = _factory.CreateClient("Production");
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        // CORS should reject the request by not including Access-Control-Allow-Origin header
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Theory]
    [InlineData("https://staging.setliststudio.com")]
    [InlineData("https://test.setliststudio.com")]
    [InlineData("https://preview.setliststudio.com")]
    public async Task Staging_CorsPreflight_ShouldAllowStagingOrigins(string origin)
    {
        // Arrange
        var client = _factory.CreateClient("Staging");
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain(origin);
    }

    [Fact]
    public async Task CorsPreflight_ShouldNotAllowCredentialsWithWildcardOrigin()
    {
        // Arrange
        var client = _factory.CreateClient("Development");
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        // When allowing all origins (*), credentials should not be allowed for security
        if (response.Headers.GetValues("Access-Control-Allow-Origin").Contains("*"))
        {
            response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
        }
    }

    [Fact]
    public async Task Production_CorsPreflight_ShouldAllowCredentialsWithSpecificOrigin()
    {
        // Arrange
        var client = _factory.CreateClient("Production");
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", "https://setliststudio.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("https://setliststudio.com");
        response.Headers.GetValues("Access-Control-Allow-Credentials").Should().Contain("true");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task CorsPreflight_ShouldAllowStandardHttpMethods(string method)
    {
        // Arrange
        var client = _factory.CreateClient("Development");
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", method);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var allowedMethods = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods"));
        allowedMethods.Should().Contain(method);
    }

    [Theory]
    [InlineData("authorization")]
    [InlineData("content-type")]
    [InlineData("x-requested-with")]
    [InlineData("accept")]
    [InlineData("origin")]
    public async Task CorsPreflight_ShouldAllowCommonHeaders(string header)
    {
        // Arrange
        var client = _factory.CreateClient("Development");
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", header);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var allowedHeaders = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers"));
        allowedHeaders.ToLower().Should().Contain(header.ToLower());
    }

    [Theory]
    [InlineData("x-custom-malicious-header")]
    [InlineData("x-admin-token")]
    [InlineData("x-internal-api-key")]
    public async Task CorsPreflight_ShouldRejectSuspiciousHeaders(string header)
    {
        // Arrange
        var client = _factory.CreateClient("Production");
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", "https://setliststudio.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", header);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        // Should either reject the request or not allow the suspicious header
        if (response.Headers.Contains("Access-Control-Allow-Headers"))
        {
            var allowedHeaders = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers"));
            allowedHeaders.Should().NotContain(header);
        }
    }

    [Fact]
    public async Task ActualRequest_WithValidOrigin_ShouldIncludeAccessControlHeaders()
    {
        // Arrange
        var client = _factory.CreateClient("Development");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", "http://localhost:3000");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task CorsPolicy_ShouldHaveReasonableMaxAge()
    {
        // Arrange
        var client = _factory.CreateClient("Production");
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", "https://setliststudio.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        if (response.Headers.Contains("Access-Control-Max-Age"))
        {
            var maxAge = response.Headers.GetValues("Access-Control-Max-Age").First();
            var maxAgeSeconds = int.Parse(maxAge);
            
            // Should be reasonable (not too short, not too long)
            maxAgeSeconds.Should().BeGreaterThan(300); // At least 5 minutes
            maxAgeSeconds.Should().BeLessOrEqualTo(86400); // At most 24 hours
        }
    }

    [Fact]
    public async Task CorsPolicy_WithoutOriginHeader_ShouldNotIncludeCorsHeaders()
    {
        // Arrange
        var client = _factory.CreateClient("Production");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        // No Origin header

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Headers.Should().NotContainKey("Access-Control-Allow-Origin");
    }

    [Theory]
    [InlineData("null")]
    [InlineData("")]
    [InlineData("javascript:void(0)")]
    public async Task CorsPolicy_WithInvalidOriginValues_ShouldRejectRequest(string origin)
    {
        // Arrange
        var client = _factory.CreateClient("Production");
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        // Should reject invalid origins
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task CorsPolicy_ShouldNotExposeInternalHeaders()
    {
        // Arrange
        var client = _factory.CreateClient("Production");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", "https://setliststudio.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        // Should not expose internal/sensitive headers
        if (response.Headers.Contains("Access-Control-Expose-Headers"))
        {
            var exposedHeaders = string.Join(",", response.Headers.GetValues("Access-Control-Expose-Headers"));
            exposedHeaders.Should().NotContain("server");
            exposedHeaders.Should().NotContain("x-powered-by");
            exposedHeaders.Should().NotContain("x-aspnet-version");
        }
    }

    [Fact]
    public async Task CorsPolicy_WithMultipleOrigins_ShouldHandleCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient("Production");
        
        var origins = new[]
        {
            "https://setliststudio.com",
            "https://www.setliststudio.com",
            "https://api.setliststudio.com"
        };

        foreach (var origin in origins)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain(origin);
        }
    }

    [Fact]
    public async Task CorsPolicy_SecurityHeaders_ShouldBePresent()
    {
        // Arrange
        var client = _factory.CreateClient("Production");
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Origin", "https://setliststudio.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        // Verify that security headers are present alongside CORS headers
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.Should().ContainKey("Referrer-Policy");
    }

    [Theory]
    [InlineData("Development", "*")]
    [InlineData("Staging", "https://staging.setliststudio.com,https://test.setliststudio.com")]
    [InlineData("Production", "https://setliststudio.com,https://www.setliststudio.com")]
    public async Task CorsPolicy_EnvironmentSpecific_ShouldHaveCorrectConfiguration(
        string environment, string expectedAllowedOrigins)
    {
        // Arrange
        var client = _factory.CreateClient(environment);
        
        // Test with first allowed origin
        var testOrigin = environment == "Development" ? 
            "http://localhost:3000" : 
            expectedAllowedOrigins.Split(',')[0];

        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", testOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        if (environment == "Development")
        {
            response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("*");
        }
        else
        {
            response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain(testOrigin);
        }
    }
}
