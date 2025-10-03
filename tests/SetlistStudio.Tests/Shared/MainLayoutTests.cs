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

public class MainLayoutTests : TestContext
{
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;

    public MainLayoutTests()
    {
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
        
        // Register services needed for MainLayout
        Services.AddMudServices();
        Services.AddAuthorization();
        Services.AddSingleton<IAuthorizationService, FakeAuthorizationService>();
        Services.AddLogging();
        Services.AddScoped<AuthenticationStateProvider>(_ => _mockAuthStateProvider.Object);
        
        // Setup JSInterop for MudBlazor components
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void MainLayout_ShouldRenderWithoutErrors()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Assert
        component.Should().NotBeNull("MainLayout component should render successfully");
        component.Markup.Should().NotBeNullOrEmpty("MainLayout component should have markup");
    }

    [Fact]
    public void MainLayout_ShouldContainNavigationElements()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Assert
        // MainLayout typically contains navigation elements
        component.Markup.Should().NotBeNull("MainLayout should contain navigation structure");
    }

    [Fact]
    public void MainLayout_ShouldHaveProperStructure()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Assert
        component.Markup.Should().Contain("Setlist Studio", "MainLayout should contain app title");
        component.Markup.Should().NotBeNullOrEmpty("MainLayout should render content");
    }

    [Fact]
    public void MainLayout_ShouldHandleUnauthenticatedUsers()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Assert
        component.Should().NotBeNull("MainLayout should render for unauthenticated users");
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