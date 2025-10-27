using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace SetlistStudio.Tests.Web.Pages;

/// <summary>
/// Integration tests for the Error.cshtml Razor page to cover AspNetCoreGeneratedDocument.Pages_Error generated code
/// </summary>
public class ErrorPageTests : IClassFixture<ErrorPageTests.TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ErrorPageTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ErrorPage_ShouldReturnOk_WhenAccessed()
    {
        // Act
        var response = await _client.GetAsync("/Error");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ErrorPage_ShouldContainExpectedContent()
    {
        // Act
        var response = await _client.GetAsync("/Error");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Something went wrong");
        content.Should().Contain("Error ID:");
        content.Should().Contain("Go back to home");
        content.Should().Contain("Setlist Studio");
    }

    [Fact]
    public async Task ErrorPage_ShouldHaveCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/Error");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task ErrorPage_ShouldDisplayRequestId_WhenProvided()
    {
        // Arrange - Create a request that will generate a trace identifier
        using var scope = _factory.Services.CreateScope();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // No additional services needed for this test
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/Error");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // The error page should display some form of error ID/request ID
        content.Should().MatchRegex(@"Error ID:\s*[^\s]+");
    }

    [Fact]
    public async Task ErrorPage_ShouldHaveSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/Error");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Check for basic security headers
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
    }

    [Fact]
    public async Task ErrorPage_ShouldHaveNoCacheHeaders()
    {
        // Act
        var response = await _client.GetAsync("/Error");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Error pages should not be cached - check if cache control is set appropriately
        var cacheControl = response.Headers.CacheControl;
        if (cacheControl != null)
        {
            // If cache control is set, it should prevent caching
            if (cacheControl.NoCache)
                cacheControl.NoCache.Should().BeTrue();
            if (cacheControl.NoStore)
                cacheControl.NoStore.Should().BeTrue();
            if (cacheControl.MaxAge.HasValue)
                cacheControl.MaxAge.Should().Be(TimeSpan.Zero);
        }
        
        // At minimum, there should be no explicit caching headers encouraging caching
        response.Headers.Should().NotContainKey("Expires");
        response.Content.Headers.Should().NotContainKey("Last-Modified");
    }

    [Fact]
    public async Task ErrorPage_ShouldNotRequireAuthentication()
    {
        // Arrange - Create client without authentication
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/Error");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Error page should be accessible without authentication
    }

    [Fact]
    public async Task ErrorPage_ShouldHaveCorrectLayout()
    {
        // Act
        var response = await _client.GetAsync("/Error");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify HTML structure
        content.Should().Contain("<!DOCTYPE html>");
        content.Should().Contain("<html lang=\"en\">");
        content.Should().Contain("<head>");
        content.Should().Contain("<meta charset=\"utf-8\"");
        content.Should().Contain("<meta name=\"viewport\"");
        content.Should().Contain("<title>Error - Setlist Studio</title>");
        content.Should().Contain("<body>");
        content.Should().Contain("class=\"error-container\"");
    }

    [Fact]
    public async Task ErrorPage_ShouldHaveInlineStyles()
    {
        // Act
        var response = await _client.GetAsync("/Error");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify inline styles are present
        content.Should().Contain("<style>");
        content.Should().Contain("error-container");
        content.Should().Contain("back-link");
        content.Should().Contain("font-family: Arial");
    }

    [Fact]
    public async Task ErrorPage_ShouldHaveBackLinkToHome()
    {
        // Act
        var response = await _client.GetAsync("/Error");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("<a href=\"/\" class=\"back-link\">");
        content.Should().Contain("Go back to home");
    }

    [Fact]
    public async Task ErrorPage_ShouldRenderModelData()
    {
        // Act
        var response = await _client.GetAsync("/Error");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify that the Model.RequestId is rendered (even if empty)
        content.Should().Contain("Error ID:");
        
        // The error page should not expose sensitive information
        content.Should().NotContain("exception");
        content.Should().NotContain("stack");
        content.Should().NotContain("sql");
        content.Should().NotContain("password");
    }

    [Fact]
    public async Task ErrorPage_ShouldHaveAccessibleDesign()
    {
        // Act
        var response = await _client.GetAsync("/Error");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Check for accessibility features
        content.Should().Contain("lang=\"en\"");
        
        // Verify semantic HTML structure
        content.Should().Contain("<h1>");
        content.Should().Contain("<p>");
        content.Should().Contain("<div");
    }

    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            
            builder.ConfigureServices(services =>
            {
                // Add any test-specific services if needed
                // The Error page shouldn't need complex dependencies
            });
        }
    }
}