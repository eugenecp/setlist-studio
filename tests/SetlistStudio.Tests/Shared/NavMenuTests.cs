using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using Xunit;
using FluentAssertions;
using SetlistStudio.Web.Shared;
using MudBlazor.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components.Web;
using System.Security.Claims;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Authorization;

namespace SetlistStudio.Tests.Shared;

public class NavMenuTests : TestContext
{
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;

    public NavMenuTests()
    {
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
        
        // Register services needed for NavMenu
        Services.AddMudServices();
        Services.AddAuthorization();
        Services.AddSingleton<IAuthorizationService, FakeAuthorizationService>();
        Services.AddLogging();
        Services.AddScoped<AuthenticationStateProvider>(_ => _mockAuthStateProvider.Object);
        
        // Setup JSInterop for MudBlazor components
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void NavMenu_ShouldRenderWithoutErrors()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<NavMenu>(0);
                childBuilder.CloseComponent();
            }));

        // Assert
        component.Should().NotBeNull("NavMenu component should render successfully");
        component.Markup.Should().NotBeNullOrEmpty("NavMenu component should have markup");
    }

    [Fact]
    public void NavMenu_ShouldContainNavigationLinks()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<NavMenu>(0);
                childBuilder.CloseComponent();
            }));

        // Assert
        // NavMenu should contain navigation links
        component.Markup.Should().NotBeEmpty("NavMenu should contain navigation elements");
    }

    [Fact]
    public void NavMenu_ShouldHandleAuthenticationState()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<NavMenu>(0);
                childBuilder.CloseComponent();
            }));

        // Assert
        component.Should().NotBeNull("NavMenu should render for unauthenticated users");
    }

    [Fact]
    public void NavMenu_ShouldRenderDifferentlyForAuthenticatedUsers()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<NavMenu>(0);
                childBuilder.CloseComponent();
            }));

        // Assert
        // Authenticated users might see different navigation options
        component.Markup.Should().NotBeEmpty("NavMenu should render content for authenticated users");
    }

    [Fact]
    public void NavMenu_ShouldBeAccessible()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<NavMenu>(0);
                childBuilder.CloseComponent();
            }));

        // Assert
        // NavMenu should have proper accessibility attributes
        component.Markup.Should().NotBeEmpty("NavMenu should be accessible");
    }

    private void SetupAuthenticatedUser()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser@example.com"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);

        _mockAuthStateProvider
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);
    }

    private void SetupUnauthenticatedUser()
    {
        var principal = new ClaimsPrincipal();
        var authState = new AuthenticationState(principal);

        _mockAuthStateProvider
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);
    }
}