using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace SetlistStudio.Tests.Pages;

/// <summary>
/// Tests for Pages__Host razor page
/// Target: Achieve 90%+ line and branch coverage
/// Note: Testing the _Host page through integration tests since it's a Razor page
/// </summary>
public class Pages__HostTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Pages__HostTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Host_ShouldReturnSuccessStatusCode_WhenAccessedDirectly()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/_Host");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Host_ShouldContainHtmlContent_WhenRendered()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/_Host");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeEmpty();
        content.Should().Contain("html", "should contain HTML structure");
    }

    [Fact]
    public async Task Host_ShouldHandleRedirectionScenarios_WhenNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/_Host");

        // Assert
        // Should either return OK (if anonymous access allowed) or redirect to login
        var validStatusCodes = new[] { HttpStatusCode.OK, HttpStatusCode.Found, HttpStatusCode.Redirect };
        validStatusCodes.Should().Contain(response.StatusCode);
    }

    [Fact]
    public async Task Host_ShouldBeAccessibleViaRootUrl_WhenApplicationConfiguredCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert  
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Host_ShouldHandleDifferentHttpMethods_WhenRequested()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act & Assert - GET should work
        var getResponse = await client.GetAsync("/_Host");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // POST should be handled (might return MethodNotAllowed, OK, Found, or BadRequest depending on configuration)
        var postResponse = await client.PostAsync("/_Host", null);
        var validPostStatusCodes = new[] { HttpStatusCode.OK, HttpStatusCode.MethodNotAllowed, HttpStatusCode.Found, HttpStatusCode.BadRequest };
        validPostStatusCodes.Should().Contain(postResponse.StatusCode);
    }

    [Fact]
    public async Task Host_ShouldHandleCustomHeaders_WhenProvided()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Custom-Header", "TestValue");
        client.DefaultRequestHeaders.Add("User-Agent", "TestAgent/1.0");

        // Act
        var response = await client.GetAsync("/_Host");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Host_ShouldHandleQueryParameters_WhenProvided()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/_Host?test=value&param=123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/_Host")]
    [InlineData("/_host")]  // Test case sensitivity
    [InlineData("/_HOST")]  // Test case sensitivity
    public async Task Host_ShouldHandleCaseVariations_WhenRequested(string path)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(path);

        // Assert
        // Should either work or give a consistent response (not crash)
        var validStatusCodes = new[] { HttpStatusCode.OK, HttpStatusCode.NotFound };
        validStatusCodes.Should().Contain(response.StatusCode);
    }

    [Fact]
    public async Task Host_ShouldContainBlazorServerElements_WhenRendered()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/_Host");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeEmpty();
        // The _Host page typically contains Blazor server elements
        content.Should().Contain("Setlist Studio", "should contain app title");
    }

    [Fact]
    public async Task Host_ShouldHandleConcurrentRequests_WhenMultipleClientsMakeRequests()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Make multiple concurrent requests
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(client.GetAsync("/_Host"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(5);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task Host_ShouldHaveCorrectContentType_WhenRendered()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/_Host");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task Host_ShouldHandleSlowRequests_WithTimeout()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30); // Set a reasonable timeout

        // Act
        var response = await client.GetAsync("/_Host");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}