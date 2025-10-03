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
}