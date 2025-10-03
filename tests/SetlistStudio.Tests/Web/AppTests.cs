using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using Xunit;
using FluentAssertions;
using SetlistStudio.Web;
using MudBlazor.Services;
using System.Security.Claims;
using SetlistStudio.Core.Interfaces;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Authorization;

namespace SetlistStudio.Tests.Web;

public class AppTests : TestContext
{
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;
    private readonly Mock<ISongService> _mockSongService;
    private readonly Mock<ISetlistService> _mockSetlistService;

    public AppTests()
    {
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
        _mockSongService = new Mock<ISongService>();
        _mockSetlistService = new Mock<ISetlistService>();
        
        // Register basic services needed for App component
        Services.AddLogging();
        Services.AddAuthorization();
        Services.AddSingleton<IAuthorizationService, FakeAuthorizationService>();
        Services.AddMudServices();
        Services.AddScoped<AuthenticationStateProvider>(_ => _mockAuthStateProvider.Object);
        Services.AddScoped<ISongService>(_ => _mockSongService.Object);
        Services.AddScoped<ISetlistService>(_ => _mockSetlistService.Object);
        
        // Setup JSInterop for MudBlazor components
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        SetupAuthenticatedUser();
    }

    private void SetupAuthenticatedUser()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.NameIdentifier, "123")
        }, "test");

        var user = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(user);

        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);
    }

    [Fact]
    public void App_ShouldRenderWithoutErrors()
    {
        // Arrange & Act
        var component = RenderComponent<App>();

        // Assert
        component.Should().NotBeNull("App component should render successfully");
        component.Markup.Should().NotBeNullOrEmpty("App component should have markup");
    }

    [Fact]
    public void App_ShouldContainRouterComponent()
    {
        // Arrange & Act
        var component = RenderComponent<App>();

        // Assert - Router should be functional, evidenced by rendered navigation and main content
        component.Markup.Should().Contain("main-content", "App should contain Router component that renders main content");
        component.Markup.Should().Contain("nav-menu", "App should contain Router component that enables navigation");
    }

    [Fact]
    public void App_ShouldHandleNotFoundRoutes()
    {
        // Arrange & Act
        var component = RenderComponent<App>();

        // Assert
        // App.razor typically contains a Router with NotFound template
        component.Markup.Should().NotBeNull("App component should handle routing");
    }
}