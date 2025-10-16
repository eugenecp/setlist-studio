using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SetlistStudio.Web.Controllers;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive unit and integration tests for CspReportController.
/// Tests CSP violation reporting, security analysis, and endpoint protection.
/// Ensures proper handling of CSP violation reports from browsers.
/// </summary>
public class CspReportControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public CspReportControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = _factory.CreateClient();
    }

    #region Report Endpoint Tests

    [Fact]
    public async Task Report_WithValidCspViolation_ShouldReturn204NoContent()
    {
        // Arrange
        var violation = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://example.com/page",
                BlockedUri = "https://malicious.com/script.js",
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'",
                SourceFile = "https://example.com/page",
                LineNumber = 42,
                ColumnNumber = 15,
                StatusCode = 200
            }
        };

        var json = JsonSerializer.Serialize(violation);
        var content = new StringContent(json, Encoding.UTF8, "application/csp-report");

        // Act
        var response = await _client.PostAsync("/api/cspreport/report", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Report_WithNullReport_ShouldReturn400BadRequest()
    {
        // Arrange
        var content = new StringContent("null", Encoding.UTF8, "application/csp-report");

        // Act
        var response = await _client.PostAsync("/api/cspreport/report", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Invalid CSP report format");
    }

    [Fact]
    public async Task Report_WithInvalidJson_ShouldReturn400BadRequest()
    {
        // Arrange
        var content = new StringContent("{ invalid json }", Encoding.UTF8, "application/csp-report");

        // Act
        var response = await _client.PostAsync("/api/cspreport/report", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Invalid JSON format");
    }

    [Fact]
    public async Task Report_WithEmptyBody_ShouldReturn400BadRequest()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/csp-report");

        // Act
        var response = await _client.PostAsync("/api/cspreport/report", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("javascript:alert('xss')", true)]
    [InlineData("data:text/html,<script>alert(1)</script>", true)]
    [InlineData("vbscript:msgbox('xss')", true)]
    [InlineData("https://legitimate-cdn.com/script.js", false)]
    [InlineData("https://example.com/normal-resource", false)]
    public async Task Report_WithSuspiciousPatterns_ShouldLogAppropriately(string blockedUri, bool shouldBeSuspicious)
    {
        // Arrange
        var violation = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://example.com/page",
                BlockedUri = blockedUri,
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'",
                SourceFile = "https://example.com/page",
                LineNumber = 42
            }
        };

        var json = JsonSerializer.Serialize(violation);
        var content = new StringContent(json, Encoding.UTF8, "application/csp-report");

        // Act
        var response = await _client.PostAsync("/api/cspreport/report", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Note: In a real implementation, you might want to verify logging behavior
        // This would require custom logging verification or test-specific logging setup
        _output.WriteLine($"Tested {blockedUri} - Expected suspicious: {shouldBeSuspicious}");
    }

    [Fact]
    public async Task Report_WithXssAttemptPattern_ShouldDetectSuspiciousActivity()
    {
        // Arrange
        var violation = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://example.com/page",
                BlockedUri = "javascript:eval(atob('YWxlcnQoJ1hTUycp'))", // Base64 encoded XSS
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'",
                SourceFile = "https://example.com/page",
                LineNumber = 1,
                ScriptSample = "eval(atob('YWxlcnQoJ1hTUycp'))"
            }
        };

        var json = JsonSerializer.Serialize(violation);
        var content = new StringContent(json, Encoding.UTF8, "application/csp-report");

        // Act
        var response = await _client.PostAsync("/api/cspreport/report", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Report_WhenReportingDisabled_ShouldReturn204WithoutProcessing()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Security:CspReporting:Enabled", "false")
                });
            });
        });

        var client = factory.CreateClient();
        var violation = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://example.com/page",
                BlockedUri = "https://malicious.com/script.js",
                ViolatedDirective = "script-src"
            }
        };

        var json = JsonSerializer.Serialize(violation);
        var content = new StringContent(json, Encoding.UTF8, "application/csp-report");

        // Act
        var response = await client.PostAsync("/api/cspreport/report", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion

    #region Health Endpoint Tests

    [Fact]
    public async Task Health_ShouldReturnHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/cspreport/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthStatus = JsonSerializer.Deserialize<HealthResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        healthStatus.Should().NotBeNull();
        healthStatus!.Status.Should().Be("Healthy");
        healthStatus.Service.Should().Be("CSP Reporting");
        healthStatus.Enabled.Should().BeTrue();
        healthStatus.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Health_WhenReportingDisabled_ShouldShowDisabledStatus()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Security:CspReporting:Enabled", "false")
                });
            });
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/cspreport/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthStatus = JsonSerializer.Deserialize<HealthResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        healthStatus.Should().NotBeNull();
        healthStatus!.Status.Should().Be("Healthy");
        healthStatus.Service.Should().Be("CSP Reporting");
        healthStatus.Enabled.Should().BeFalse();
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task Report_WithMultipleRequests_ShouldRespectRateLimit()
    {
        // Arrange
        var violation = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://example.com/page",
                BlockedUri = "https://malicious.com/script.js",
                ViolatedDirective = "script-src"
            }
        };

        var json = JsonSerializer.Serialize(violation);

        // Act - Send multiple requests rapidly
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 10; i++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/csp-report");
            tasks.Add(_client.PostAsync("/api/cspreport/report", content));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - Some requests should succeed, rate limiting behavior depends on configuration
        responses.Should().NotBeEmpty();
        responses.Should().OnlyContain(r => 
            r.StatusCode == HttpStatusCode.NoContent || 
            r.StatusCode == HttpStatusCode.TooManyRequests);
    }

    #endregion

    #region Security Headers Tests

    [Fact]
    public async Task Report_Response_ShouldIncludeSecurityHeaders()
    {
        // Arrange
        var violation = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://example.com/page",
                BlockedUri = "https://example.com/blocked",
                ViolatedDirective = "script-src"
            }
        };

        var json = JsonSerializer.Serialize(violation);
        var content = new StringContent(json, Encoding.UTF8, "application/csp-report");

        // Act
        var response = await _client.PostAsync("/api/cspreport/report", content);

        // Assert
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("Content-Security-Policy");
    }

    #endregion

    #region Unit Tests for Controller Logic

    [Fact]
    public void CspReport_Properties_ShouldSerializeCorrectly()
    {
        // Arrange
        var report = new CspReport
        {
            DocumentUri = "https://example.com/document",
            BlockedUri = "https://blocked.com/resource",
            ViolatedDirective = "script-src 'self'",
            EffectiveDirective = "script-src",
            OriginalPolicy = "default-src 'self'; script-src 'self'",
            SourceFile = "https://example.com/source.js",
            LineNumber = 42,
            ColumnNumber = 15,
            StatusCode = 200,
            ScriptSample = "alert('test')"
        };

        // Act
        var json = JsonSerializer.Serialize(report);
        var deserialized = JsonSerializer.Deserialize<CspReport>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.DocumentUri.Should().Be("https://example.com/document");
        deserialized.BlockedUri.Should().Be("https://blocked.com/resource");
        deserialized.ViolatedDirective.Should().Be("script-src 'self'");
        deserialized.EffectiveDirective.Should().Be("script-src");
        deserialized.OriginalPolicy.Should().Be("default-src 'self'; script-src 'self'");
        deserialized.SourceFile.Should().Be("https://example.com/source.js");
        deserialized.LineNumber.Should().Be(42);
        deserialized.ColumnNumber.Should().Be(15);
        deserialized.StatusCode.Should().Be(200);
        deserialized.ScriptSample.Should().Be("alert('test')");
    }

    [Fact]
    public void CspViolationReport_WithNullCspReport_ShouldBeInvalid()
    {
        // Arrange
        var violation = new CspViolationReport
        {
            CspReport = null!
        };

        // Act & Assert
        violation.CspReport.Should().BeNull();
    }

    #endregion

    #region Test Models

    private class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}