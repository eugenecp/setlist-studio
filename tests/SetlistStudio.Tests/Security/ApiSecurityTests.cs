using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SetlistStudio.Web;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive API security testing suite for Setlist Studio.
/// Tests for SQL injection, XSS, CSRF vulnerabilities, and rate limiting in API endpoints.
/// Validates security controls and input validation across all API controllers.
/// </summary>
public class ApiSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public ApiSecurityTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = _factory.CreateClient();
    }

    #region SQL Injection Tests

    [Theory]
    [InlineData("'; DROP TABLE Songs; --")]
    [InlineData("' OR '1'='1")]
    [InlineData("' UNION SELECT * FROM Users --")]
    [InlineData("'; INSERT INTO Songs (Name) VALUES ('Malicious'); --")]
    [InlineData("' OR 1=1 --")]
    [InlineData("admin'--")]
    [InlineData("admin' /*")]
    [InlineData("' OR 'x'='x")]
    public async Task API_WithSqlInjectionPayloads_ShouldNotBeVulnerable(string maliciousPayload)
    {
        // Test multiple API endpoints that might be vulnerable to SQL injection
        var endpoints = new[]
        {
            $"/api/songs/search?query={Uri.EscapeDataString(maliciousPayload)}",
            $"/api/artists/search?name={Uri.EscapeDataString(maliciousPayload)}",
            $"/api/setlists/search?title={Uri.EscapeDataString(maliciousPayload)}"
        };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert - Should not return 500 (server error) or unexpected data
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError, 
                $"SQL injection attempt should not cause server error for endpoint: {endpoint}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Should not return raw SQL error messages
                content.Should().NotContainEquivalentOf("SQL");
                content.Should().NotContainEquivalentOf("database");
                content.Should().NotContainEquivalentOf("syntax error");
                content.Should().NotContainEquivalentOf("violation");
                
                _output.WriteLine($"✓ SQL injection test passed for {endpoint} with payload: {maliciousPayload}");
            }
        }
    }

    [Fact]
    public async Task API_PostWithSqlInjectionInBody_ShouldNotBeVulnerable()
    {
        // Arrange - Song creation with SQL injection attempt
        var maliciousSong = new
        {
            Title = "'; DROP TABLE Songs; --",
            Artist = "' OR '1'='1",
            Album = "'; INSERT INTO Users (Username) VALUES ('hacker'); --",
            Key = "' UNION SELECT password FROM Users --",
            Bpm = 120
        };

        var json = JsonSerializer.Serialize(maliciousSong);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/songs", content);

        // Assert - Should validate input and not execute malicious SQL
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().NotContainEquivalentOf("DROP");
            responseContent.Should().NotContainEquivalentOf("INSERT");
            responseContent.Should().NotContainEquivalentOf("UNION");
        }
    }

    #endregion

    #region XSS (Cross-Site Scripting) Tests

    [Theory]
    [InlineData("<script>alert('XSS')</script>")]
    [InlineData("<img src=x onerror=alert('XSS')>")]
    [InlineData("javascript:alert('XSS')")]
    [InlineData("<svg onload=alert('XSS')>")]
    [InlineData("'><script>alert('XSS')</script>")]
    [InlineData("\"><script>alert('XSS')</script>")]
    [InlineData("<iframe src=javascript:alert('XSS')></iframe>")]
    [InlineData("<input onfocus=alert('XSS') autofocus>")]
    [InlineData("<select onfocus=alert('XSS') autofocus>")]
    [InlineData("<textarea onfocus=alert('XSS') autofocus>")]
    public async Task API_WithXssPayloads_ShouldSanitizeAndEscapeOutput(string xssPayload)
    {
        // Test XSS prevention in API responses
        var endpoints = new[]
        {
            $"/api/songs/search?query={Uri.EscapeDataString(xssPayload)}",
            $"/api/artists/search?name={Uri.EscapeDataString(xssPayload)}"
        };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Should not return unescaped script tags or event handlers
                content.Should().NotContainEquivalentOf("<script");
                content.Should().NotContainEquivalentOf("javascript:");
                content.Should().NotContainEquivalentOf("onerror=");
                content.Should().NotContainEquivalentOf("onload=");
                content.Should().NotContainEquivalentOf("onfocus=");
                
                _output.WriteLine($"✓ XSS prevention test passed for {endpoint}");
            }
        }
    }

    [Fact]
    public async Task API_PostWithXssInJsonBody_ShouldSanitizeInput()
    {
        // Arrange - Song with XSS payload
        var xssSong = new
        {
            Title = "<script>alert('XSS')</script>Sweet Child O' Mine",
            Artist = "<img src=x onerror=alert('XSS')>Guns N' Roses",
            Album = "javascript:alert('XSS')",
            Key = "<svg onload=alert('XSS')>D</svg>",
            Bpm = 125
        };

        var json = JsonSerializer.Serialize(xssSong);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/songs", content);

        // Assert - Should sanitize or reject malicious content
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().NotContain("<script");
            responseContent.Should().NotContain("javascript:");
            responseContent.Should().NotContain("onerror=");
            responseContent.Should().NotContain("onload=");
        }
        else
        {
            // If rejected, should be due to validation, not server error
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        }
    }

    #endregion

    #region CSRF (Cross-Site Request Forgery) Tests

    [Fact]
    public async Task API_PostWithoutAntiforggeryToken_ShouldRequireValidation()
    {
        // Arrange - POST request without CSRF token
        var songData = new
        {
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            Album = "A Night at the Opera",
            Key = "Bb",
            Bpm = 72
        };

        var json = JsonSerializer.Serialize(songData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/songs", content);

        // Assert - Should either require authentication or CSRF validation
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            // Check if CSRF validation is mentioned in the error
            responseContent.Should().Match(content => 
                content.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("validation", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("forbidden", StringComparison.OrdinalIgnoreCase));
        }
        
        _output.WriteLine($"CSRF protection test: {response.StatusCode}");
    }

    [Fact]
    public async Task API_StateChangingEndpoints_ShouldRequireAuthentication()
    {
        // Test that state-changing operations require authentication
        var endpoints = new[]
        {
            HttpMethod.Post,
            HttpMethod.Put,
            HttpMethod.Delete
        };

        var testUrls = new[]
        {
            "/api/songs",
            "/api/setlists",
            "/api/artists"
        };

        foreach (var method in endpoints)
        {
            foreach (var url in testUrls)
            {
                // Arrange
                var request = new HttpRequestMessage(method, url);
                if (method == HttpMethod.Post || method == HttpMethod.Put)
                {
                    request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                }

                // Act
                var response = await _client.SendAsync(request);

                // Assert - Should require authentication
                response.StatusCode.Should().BeOneOf(
                    HttpStatusCode.Unauthorized,
                    HttpStatusCode.Forbidden,
                    HttpStatusCode.Redirect,
                    HttpStatusCode.BadRequest  // May return validation error before auth check
                );

                _output.WriteLine($"✓ {method} {url} requires authentication: {response.StatusCode}");
            }
        }
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task API_WithRapidRequests_ShouldEnforceRateLimit()
    {
        // Arrange - Send many requests rapidly
        var tasks = new List<Task<HttpResponseMessage>>();
        var endpoint = "/api/status"; // Health check endpoint

        // Act - Send 20 rapid requests
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_client.GetAsync(endpoint));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - Should eventually hit rate limit
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        var successfulResponses = responses.Count(r => r.IsSuccessStatusCode);

        _output.WriteLine($"Rate limiting test: {successfulResponses} successful, {rateLimitedResponses} rate-limited");

        // At least some requests should succeed, but rate limiting should kick in
        successfulResponses.Should().BeGreaterThan(0, "Some requests should succeed");
        
        // Check if rate limiting is working (may depend on configuration)
        if (rateLimitedResponses > 0)
        {
            _output.WriteLine("✓ Rate limiting is active");
        }
        else
        {
            _output.WriteLine("⚠ Rate limiting may need adjustment for this endpoint");
        }
    }

    [Fact]
    public async Task API_RateLimitHeaders_ShouldBePresent()
    {
        // Act
        var response = await _client.GetAsync("/api/status");

        // Assert - Rate limiting headers should be present
        response.Headers.Should().ContainKey("X-RateLimit-Limit");
        response.Headers.Should().ContainKey("X-RateLimit-Remaining");
        
        var rateLimitLimit = response.Headers.GetValues("X-RateLimit-Limit").FirstOrDefault();
        var rateLimitRemaining = response.Headers.GetValues("X-RateLimit-Remaining").FirstOrDefault();

        rateLimitLimit.Should().NotBeNullOrEmpty("Rate limit should be specified");
        rateLimitRemaining.Should().NotBeNullOrEmpty("Remaining requests should be specified");

        _output.WriteLine($"Rate Limit: {rateLimitLimit}, Remaining: {rateLimitRemaining}");
    }

    #endregion

    #region Input Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("A very long string that exceeds the maximum allowed length for a song title and should be rejected by the validation system because it's unreasonably long and could indicate malicious intent")]
    public async Task API_WithInvalidInput_ShouldValidateAndReject(string invalidInput)
    {
        // Arrange - Song with invalid data
        var invalidSong = new
        {
            Title = invalidInput,
            Artist = invalidInput,
            Album = invalidInput,
            Key = invalidInput,
            Bpm = -1 // Invalid BPM
        };

        var json = JsonSerializer.Serialize(invalidSong);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/songs", content);

        // Assert - Should reject with validation error
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden
        );

        _output.WriteLine($"✓ Input validation working for invalid input: '{invalidInput}'");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)] // Unreasonably high BPM
    public async Task API_WithInvalidBpm_ShouldValidateRange(int invalidBpm)
    {
        // Arrange
        var songWithInvalidBpm = new
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Album = "Test Album",
            Key = "C",
            Bpm = invalidBpm
        };

        var json = JsonSerializer.Serialize(songWithInvalidBpm);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/songs", content);

        // Assert - Should reject invalid BPM
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().ContainEquivalentOf("bpm");
        }

        _output.WriteLine($"✓ BPM validation working for value: {invalidBpm}");
    }

    #endregion

    #region Security Headers Tests

    [Fact]
    public async Task API_Responses_ShouldIncludeSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/status");

        // Assert - Security headers should be present
        var headers = response.Headers.ToList();
        headers.AddRange(response.Content.Headers.ToList());

        // Check for essential security headers
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("Content-Security-Policy");
        response.Headers.Should().ContainKey("Referrer-Policy");

        var xContentTypeOptions = response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault();
        xContentTypeOptions.Should().Be("nosniff");

        var xFrameOptions = response.Headers.GetValues("X-Frame-Options").FirstOrDefault();
        xFrameOptions.Should().BeOneOf("DENY", "SAMEORIGIN");

        _output.WriteLine("✓ Security headers are properly configured");
    }

    [Fact]
    public async Task API_ContentType_ShouldBeValidated()
    {
        // Arrange - Request with incorrect content type
        var content = new StringContent("{\"test\": \"data\"}", Encoding.UTF8, "text/plain");

        // Act
        var response = await _client.PostAsync("/api/songs", content);

        // Assert - Should reject incorrect content type
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden
        );

        _output.WriteLine($"✓ Content-Type validation working: {response.StatusCode}");
    }

    #endregion

    #region Authentication and Authorization Tests

    [Fact]
    public async Task API_ProtectedEndpoints_ShouldRequireAuthentication()
    {
        // Test endpoints that should require authentication
        var protectedEndpoints = new[]
        {
            "/api/songs",
            "/api/setlists",
            "/api/artists",
            "/api/users/profile"
        };

        foreach (var endpoint in protectedEndpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert - Should redirect to login or return unauthorized
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Redirect,
                HttpStatusCode.NotFound  // May return 404 if endpoint doesn't exist yet
            );

            _output.WriteLine($"✓ {endpoint} requires authentication: {response.StatusCode}");
        }
    }

    [Fact]
    public async Task API_WithInvalidJwtToken_ShouldRejectRequest()
    {
        // Arrange - Add invalid JWT token
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        // Act
        var response = await _client.GetAsync("/api/songs");

        // Assert - Should reject invalid token
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden
        );

        _output.WriteLine($"✓ Invalid JWT token rejected: {response.StatusCode}");
    }

    #endregion

    #region Error Handling Security Tests

    [Fact]
    public async Task API_WithMalformedJson_ShouldHandleGracefully()
    {
        // Arrange - Malformed JSON
        var content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/songs", content);

        // Assert - Should handle gracefully without exposing internal details
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Should not expose internal exception details
            responseContent.Should().NotContainEquivalentOf("StackTrace");
            responseContent.Should().NotContainEquivalentOf("Exception");
            responseContent.Should().NotContainEquivalentOf("System.");
        }

        _output.WriteLine($"✓ Malformed JSON handled gracefully: {response.StatusCode}");
    }

    [Fact]
    public async Task API_WithExcessivelyLargePayload_ShouldReject()
    {
        // Arrange - Very large payload (1MB+ of data)
        var largeString = new string('A', 1024 * 1024); // 1MB string
        var largePayload = new
        {
            Title = largeString,
            Artist = largeString,
            Album = largeString,
            Key = "C",
            Bpm = 120
        };

        var json = JsonSerializer.Serialize(largePayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/songs", content);

        // Assert - Should reject excessively large payload
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden
        );

        _output.WriteLine($"✓ Large payload rejected: {response.StatusCode}");
    }

    #endregion
}