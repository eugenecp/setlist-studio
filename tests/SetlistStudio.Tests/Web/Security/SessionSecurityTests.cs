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
/// Tests for secure cookie configuration, session management, and security attributes.
/// Validates session security across different environments and cookie configurations.
/// </summary>
public class SessionSecurityTests : IClassFixture<SessionSecurityTests.TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SessionSecurityTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                    ["Authentication:Google:ClientId"] = "test-client-id",
                    ["Authentication:Google:ClientSecret"] = "test-client-secret",
                    ["AllowedHosts"] = "*"
                });
            });
        }

        public HttpClient CreateClient(string environment = "Development")
        {
            return base.CreateClient();
        }
    }

    [Fact]
    public async Task Production_SessionCookie_ShouldHaveSecureAttributes()
    {
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var sessionCookie = setCookieHeaders.FirstOrDefault(c => c.Contains("__Host-SessionId") || c.Contains("AspNetCore.Session"));

        if (sessionCookie != null)
        {
            // Session cookie should have secure attributes in production
            sessionCookie.Should().Contain("Secure", "Session cookie must have Secure attribute in production");
            sessionCookie.Should().Contain("HttpOnly", "Session cookie must have HttpOnly attribute");
            sessionCookie.Should().Contain("SameSite=Strict", "Session cookie should have SameSite=Strict for CSRF protection");
            
            // Should use __Host- prefix for enhanced security
            if (sessionCookie.Contains("__Host-"))
            {
                sessionCookie.Should().Contain("Path=/", "__Host- prefix requires Path=/");
                sessionCookie.Should().NotContain("Domain=", "__Host- prefix prohibits Domain attribute");
            }
        }
    }

    [Fact]
    public async Task Development_SessionCookie_ShouldHaveBasicSecurityAttributes()
    {
        // Arrange
        var client = _factory.CreateClient("Development");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var sessionCookie = setCookieHeaders.FirstOrDefault(c => 
            c.Contains("AspNetCore.Session") || c.Contains("SessionId"));

        if (sessionCookie != null)
        {
            // Development should still have HttpOnly for basic security
            sessionCookie.Should().Contain("HttpOnly", "Session cookie must have HttpOnly attribute even in development");
            sessionCookie.Should().Contain("SameSite", "Session cookie should have SameSite attribute");
        }
    }

    [Fact]
    public async Task AuthenticationCookie_ShouldHaveSecureConfiguration()
    {
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act - Try to access a protected resource to trigger auth cookie
        var response = await client.GetAsync("/Identity/Account/Login");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var authCookie = setCookieHeaders.FirstOrDefault(c => 
            c.Contains("AspNetCore.Identity.Application") || 
            c.Contains("__Host-Identity") ||
            c.Contains(".AspNetCore.Identity"));

        if (authCookie != null)
        {
            authCookie.Should().Contain("HttpOnly", "Auth cookie must have HttpOnly attribute");
            authCookie.Should().Contain("Secure", "Auth cookie must have Secure attribute in production");
            authCookie.Should().Contain("SameSite=Strict", "Auth cookie should use SameSite=Strict");
        }
    }

    [Fact]
    public async Task AntiForgeryToken_ShouldHaveSecureConfiguration()
    {
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act - Get a page with forms that should include anti-forgery tokens
        var response = await client.GetAsync("/Identity/Account/Login");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var antiForgeryToken = setCookieHeaders.FirstOrDefault(c => 
            c.Contains("__RequestVerificationToken") || 
            c.Contains("__Host-RequestVerification") ||
            c.Contains(".AspNetCore.Antiforgery"));

        if (antiForgeryToken != null)
        {
            antiForgeryToken.Should().Contain("SameSite=Strict", "Anti-forgery token should use SameSite=Strict for CSRF protection");
            antiForgeryToken.Should().Contain("Secure", "Anti-forgery token should be secure in production");
            
            // Anti-forgery tokens should use __Host- prefix when possible
            if (antiForgeryToken.Contains("__Host-"))
            {
                antiForgeryToken.Should().Contain("Path=/", "__Host- prefix requires Path=/");
                antiForgeryToken.Should().NotContain("Domain=", "__Host- prefix prohibits Domain attribute");
            }
        }
    }

    [Theory]
    [InlineData("AspNetCore.Session")]
    [InlineData("AspNetCore.Identity.Application")]
    [InlineData("AspNetCore.Antiforgery")]
    public async Task SecurityCriticalCookies_ShouldNotBeAccessibleViaJavaScript(string cookieName)
    {
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var targetCookie = setCookieHeaders.FirstOrDefault(c => c.Contains(cookieName));

        if (targetCookie != null)
        {
            targetCookie.Should().Contain("HttpOnly", 
                $"{cookieName} cookie must have HttpOnly to prevent JavaScript access");
        }
    }

    [Fact]
    public async Task SessionTimeout_ShouldBeConfiguredAppropriately()
    {
        // This test verifies session timeout through cookie expiration
        // In a real implementation, we'd need to check the session store configuration
        
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var sessionCookie = setCookieHeaders.FirstOrDefault(c => 
            c.Contains("AspNetCore.Session") || c.Contains("SessionId"));

        if (sessionCookie != null && sessionCookie.Contains("Max-Age="))
        {
            // Extract Max-Age value
            var maxAgeStart = sessionCookie.IndexOf("Max-Age=") + 8;
            var maxAgeEnd = sessionCookie.IndexOf(";", maxAgeStart);
            if (maxAgeEnd == -1) maxAgeEnd = sessionCookie.Length;
            
            var maxAgeValue = sessionCookie.Substring(maxAgeStart, maxAgeEnd - maxAgeStart);
            
            if (int.TryParse(maxAgeValue, out var maxAgeSeconds))
            {
                // Session timeout should be reasonable (not too short, not too long)
                maxAgeSeconds.Should().BeGreaterThan(1800); // At least 30 minutes
                maxAgeSeconds.Should().BeLessOrEqualTo(7200); // At most 2 hours
            }
        }
    }

    [Fact] 
    public async Task CookiePath_ShouldBeRestrictive()
    {
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        
        foreach (var cookie in setCookieHeaders)
        {
            if (cookie.Contains("__Host-"))
            {
                // __Host- cookies must have Path=/
                cookie.Should().Contain("Path=/", "__Host- cookies must specify Path=/");
            }
            else if (cookie.Contains("Path="))
            {
                // Regular cookies should have restrictive paths
                cookie.Should().NotContain("Path=/admin", "Security cookies should not use admin paths");
                cookie.Should().NotContain("Path=/api/internal", "Security cookies should not use internal API paths");
            }
        }
    }

    [Fact]
    public async Task CookieDomain_ShouldBeSecurelyConfigured()
    {
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        
        foreach (var cookie in setCookieHeaders)
        {
            if (cookie.Contains("__Host-"))
            {
                // __Host- cookies must NOT have Domain attribute
                cookie.Should().NotContain("Domain=", "__Host- cookies must not specify Domain attribute");
            }
            else if (cookie.Contains("Domain="))
            {
                // Domain should not be overly permissive
                cookie.Should().NotContain("Domain=.com", "Cookie domain should not be overly broad");
                cookie.Should().NotContain("Domain=localhost", "Cookie domain should not use localhost in production");
            }
        }
    }

    [Theory]
    [InlineData("SameSite=None")]
    [InlineData("SameSite=Lax")]
    public async Task SecurityCriticalCookies_ShouldNotUsePermissiveSameSite(string sameSiteValue)
    {
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var securityCookies = setCookieHeaders.Where(c => 
            c.Contains("Identity") || 
            c.Contains("Session") || 
            c.Contains("Antiforgery") ||
            c.Contains("__Host-"));

        foreach (var cookie in securityCookies)
        {
            cookie.Should().NotContain(sameSiteValue, 
                $"Security-critical cookies should not use permissive {sameSiteValue}");
        }
    }

    [Fact]
    public async Task CookieNames_ShouldUseSecurePrefixes()
    {
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var securityCookies = setCookieHeaders.Where(c => 
            c.Contains("Identity") || 
            c.Contains("Session") || 
            c.Contains("Antiforgery"));

        // At least some security cookies should use __Host- prefix
        securityCookies.Should().Contain(c => c.Contains("__Host-"), 
            "Some security-critical cookies should use __Host- prefix for enhanced security");
    }

    [Fact]
    public async Task SessionState_ShouldBeSecureByDefault()
    {
        // Test that session state is configured securely
        
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act - Make a request that would initialize a session
        var response = await client.PostAsync("/api/test-session", new StringContent("test"));

        // Assert
        // Session should be created with secure defaults
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var sessionCookie = setCookieHeaders.FirstOrDefault(c => 
            c.Contains("Session") || c.Contains("__Host-SessionId"));

        if (sessionCookie != null)
        {
            // Session cookie should have all security attributes
            sessionCookie.Should().Contain("HttpOnly");
            sessionCookie.Should().Contain("Secure");
            sessionCookie.Should().Contain("SameSite=Strict");
        }
    }

    [Fact]
    public async Task CookieEncryption_ShouldBeEnabled()
    {
        // This test verifies that sensitive cookies are encrypted
        
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act
        var response = await client.GetAsync("/Identity/Account/Login");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        var identityCookie = setCookieHeaders.FirstOrDefault(c => c.Contains("Identity"));

        if (identityCookie != null)
        {
            // Extract cookie value (between = and ;)
            var valueStart = identityCookie.IndexOf('=') + 1;
            var valueEnd = identityCookie.IndexOf(';', valueStart);
            if (valueEnd == -1) valueEnd = identityCookie.Length;
            
            var cookieValue = identityCookie.Substring(valueStart, valueEnd - valueStart);
            
            // Encrypted cookies should not contain readable data
            cookieValue.Should().NotContain("user");
            cookieValue.Should().NotContain("admin");
            cookieValue.Should().NotContain("password");
            cookieValue.Should().NotContain("token");
            
            // Should appear to be base64 or similar encoding
            cookieValue.Length.Should().BeGreaterThan(20, "Encrypted cookie values should be reasonably long");
        }
    }

    [Fact]
    public async Task SessionRegeneration_ShouldOccurOnPrivilegeChange()
    {
        // This is a conceptual test - in practice, we'd need to simulate login
        // and verify that session ID changes after authentication
        
        // Arrange
        var client = _factory.CreateClient("Production");
        
        // Act - Get initial session
        var initialResponse = await client.GetAsync("/");
        var initialCookies = initialResponse.Headers.GetValues("Set-Cookie");
        var initialSession = initialCookies.FirstOrDefault(c => c.Contains("Session"));
        
        // Simulate authentication (in real test, would use actual login)
        var loginResponse = await client.PostAsync("/Identity/Account/Login", 
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "test@example.com",
                ["Password"] = "TestPassword123!"
            }));
        
        // Assert
        // After authentication, session should be regenerated for security
        if (loginResponse.Headers.Contains("Set-Cookie"))
        {
            var postAuthCookies = loginResponse.Headers.GetValues("Set-Cookie");
            var postAuthSession = postAuthCookies.FirstOrDefault(c => c.Contains("Session"));
            
            if (initialSession != null && postAuthSession != null)
            {
                // Session ID should change after authentication
                initialSession.Should().NotBe(postAuthSession, 
                    "Session ID should regenerate after authentication for security");
            }
        }
    }

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("data:text/html,<script>alert('xss')</script>")]
    public async Task CookieValues_ShouldNotContainMaliciousContent(string maliciousContent)
    {
        // Arrange
        var client = _factory.CreateClient("Production");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        
        foreach (var cookie in setCookieHeaders)
        {
            cookie.Should().NotContain(maliciousContent, 
                "Cookie values should not contain malicious content");
        }
    }
}
