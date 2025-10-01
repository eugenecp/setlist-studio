using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;
using System.Net;

namespace SetlistStudio.Tests.Web;

/// <summary>
/// Simplified component tests for Razor pages using integration testing approach
/// </summary>
public class RazorPagesComponentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RazorPagesComponentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task IndexPage_ShouldRenderWelcomeMessage_WhenRendered()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Welcome to Setlist Studio");
        content.Should().Contain("Organize your music library and create professional setlists");
    }

    [Fact]
    public async Task IndexPage_ShouldContainFeatureSections()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Organize Your Music");
        content.Should().Contain("LibraryMusic");
    }

    [Fact]
    public async Task IndexPage_ShouldHaveCorrectPageTitle()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("<title>Setlist Studio - Music Performance Management</title>");
    }

    [Fact]
    public async Task LoginPage_ShouldRenderWelcomeMessage()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Welcome to Setlist Studio");
        content.Should().Contain("Sign in to organize your music and create amazing setlists");
    }

    [Fact]
    public async Task LoginPage_ShouldContainAuthenticationButtons()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Sign in with Google");
        content.Should().Contain("Sign in with Microsoft");
        content.Should().Contain("Sign in with Facebook");
        content.Should().Contain("/auth/google");
        content.Should().Contain("/auth/microsoft");
        content.Should().Contain("/auth/facebook");
    }

    [Fact]
    public async Task LoginPage_ShouldHaveAccessibilityAttributes()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("aria-label=\"Sign in with Google\"");
        content.Should().Contain("aria-label=\"Sign in with Microsoft\"");
        content.Should().Contain("aria-label=\"Sign in with Facebook\"");
    }

    [Fact]
    public async Task LoginPage_ShouldHaveCorrectPageTitle()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("<title>Setlist Studio - Music Performance Management</title>");
    }

    [Fact]
    public async Task LoginPage_ShouldContainMusicIcon()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("LibraryMusic");
    }

    [Fact]
    public async Task LoginPage_ShouldHaveProperStyling()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("login-container");
        content.Should().Contain("min-vh-100");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/login")]
    public async Task Pages_ShouldBeAccessible_WithoutAuthentication(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().NotBeNull();
    }

    [Fact]
    public async Task Pages_ShouldRenderWithMudBlazorComponents()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var indexResponse = await client.GetAsync("/");
        var loginResponse = await client.GetAsync("/login");
        var indexContent = await indexResponse.Content.ReadAsStringAsync();
        var loginContent = await loginResponse.Content.ReadAsStringAsync();

        // Assert
        indexResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify MudBlazor components are present (they render as CSS classes)
        indexContent.Should().Contain("mud-container");
        indexContent.Should().Contain("mud-typography");
        indexContent.Should().Contain("mud-button");
        
        loginContent.Should().Contain("mud-container");
        loginContent.Should().Contain("mud-paper");
        loginContent.Should().Contain("mud-button");
    }
}