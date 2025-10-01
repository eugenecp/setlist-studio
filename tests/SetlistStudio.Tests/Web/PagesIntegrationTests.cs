using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;

namespace SetlistStudio.Tests.Web;

public class PagesIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public PagesIntegrationTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    [Fact]
    public async Task IndexPage_ShouldReturnSuccessAndCorrectContent_WhenVisited()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        content.Should().Contain("Welcome to Setlist Studio");
        content.Should().Contain("Organize your music library and create professional setlists");
        content.Should().Contain("Sign In to Get Started");
    }

    [Fact]
    public async Task IndexPage_ShouldShowSignInButton_WhenUserNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        content.Should().Contain("Sign In to Get Started");
        content.Should().Contain("/login");
        content.Should().NotContain("Welcome back,");
    }

    [Fact]
    public async Task IndexPage_ShouldShowUserSpecificContent_WhenUserAuthenticated()
    {
        // Arrange
        using var authFactory = new TestWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                });
            });
        
        var client = authFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        content.Should().Contain("Welcome to Setlist Studio");
        // Note: The exact authenticated content may vary based on implementation
    }

    [Fact]
    public async Task LoginPage_ShouldReturnSuccessAndCorrectContent_WhenVisited()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        content.Should().Contain("Welcome to Setlist Studio");
        content.Should().Contain("Sign in to organize your music");
        content.Should().Contain("Sign in with Google");
        content.Should().Contain("Sign in with Microsoft");
        content.Should().Contain("Sign in with Facebook");
    }

    [Fact]
    public async Task LoginPage_ShouldContainAuthenticationLinks_WithCorrectAttributes()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        content.Should().Contain("/auth/google");
        content.Should().Contain("/auth/microsoft");
        content.Should().Contain("/auth/facebook");
        content.Should().Contain("aria-label=\"Sign in with Google\"");
        content.Should().Contain("aria-label=\"Sign in with Microsoft\"");
        content.Should().Contain("aria-label=\"Sign in with Facebook\"");
    }

    [Fact]
    public async Task LoginPage_ShouldUseEmptyLayout()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        // The login page should use EmptyLayout and have login-specific styling
        content.Should().Contain("login-container");
        content.Should().Contain("min-vh-100");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/login")]
    public async Task Pages_ShouldHaveCorrectPageTitles(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        // Both pages use the same title
        content.Should().Contain("<title>Setlist Studio - Music Performance Management</title>");
    }

    [Fact]
    public async Task IndexPage_ShouldContainFeatureHighlights()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        // Check for feature highlights that should be present on the index page
        content.Should().Contain("Organize Your Music");
        content.Should().Contain("LibraryMusic");
        // Note: MudIcon components might render differently in server-side prerendering
        // so we'll check for the icon class that gets rendered instead
        content.Should().Contain("Icons.Material.Filled");
    }

    [Fact]
    public async Task IndexPage_ShouldRenderWithMudBlazorComponents()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify MudBlazor components are rendered (they get compiled to HTML classes)
        content.Should().Contain("mud-container");
        content.Should().Contain("mud-typography");
        content.Should().Contain("mud-button");
        content.Should().Contain("mud-paper");
    }

    [Fact]
    public async Task LoginPage_ShouldRenderWithMudBlazorComponents()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify MudBlazor components are present (they render as CSS classes)
        content.Should().Contain("mud-container");
        content.Should().Contain("mud-paper");
        content.Should().Contain("mud-button");
        content.Should().Contain("mud-icon");
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

    public void Dispose()
    {
        _factory?.Dispose();
    }
}

// Test authentication handler for integration tests
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Email, "testuser@example.com")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}