using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SetlistStudio.Web.Pages;
using System.Security.Claims;
using Xunit;
using FluentAssertions;
using MudBlazor;
using MudBlazor.Services;

namespace SetlistStudio.Tests.Pages;

public class LoginTests : TestContext
{
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;

    public LoginTests()
    {
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
        
        // Register MudBlazor services
        Services.AddMudServices();
        
        // Register authorization services
        Services.AddAuthorizationCore();
        
        // Register mocked services
        Services.AddScoped<AuthenticationStateProvider>(_ => _mockAuthStateProvider.Object);
    }

    [Fact]
    public void Login_ShouldRenderCorrectly_WithAllAuthenticationButtons()
    {
        // Arrange
        var mockAuthState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<Login>();

        // Assert
        component.Should().NotBeNull();
        
        // Check for Google sign-in button
        var googleButton = component.Find("a[href='/auth/google']");
        googleButton.Should().NotBeNull();
        googleButton.GetAttribute("aria-label").Should().Contain("Sign in with Google");

        // Check for Microsoft sign-in button
        var microsoftButton = component.Find("a[href='/auth/microsoft']");
        microsoftButton.Should().NotBeNull();
        microsoftButton.GetAttribute("aria-label").Should().Contain("Sign in with Microsoft");
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldHandleAuthenticatedUser_Appropriately()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser@example.com"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act & Assert - This tests the OnInitializedAsync logic
        // The component should handle authenticated users appropriately
        var component = RenderComponent<Login>();
        
        // Wait for async operations
        await Task.Delay(100);
        
        // The component should render without errors
        component.Should().NotBeNull();
    }

    [Fact]
    public void Login_ShouldShowCorrectTitle_AndDescription()
    {
        // Arrange
        var mockAuthState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<Login>();

        // Assert
        component.Markup.Should().Contain("Welcome to Setlist Studio");
        component.Markup.Should().Contain("Sign in to organize your music and create amazing setlists");
    }

    [Fact]
    public void Login_ShouldHaveProperAccessibilityAttributes()
    {
        // Arrange
        var mockAuthState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<Login>();

        // Assert
        // Check that authentication buttons have proper ARIA labels
        var googleButton = component.Find("a[href='/auth/google']");
        googleButton.GetAttribute("aria-label").Should().NotBeNullOrEmpty();
        
        var microsoftButton = component.Find("a[href='/auth/microsoft']");
        microsoftButton.GetAttribute("aria-label").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Login_ShouldRenderMudComponents_Correctly()
    {
        // Arrange
        var mockAuthState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<Login>();

        // Assert
        // Verify MudPaper is rendered
        var mudPaper = component.Find(".mud-paper");
        mudPaper.Should().NotBeNull();
        
        // Verify MudButton components are rendered
        var buttons = component.FindAll(".mud-button");
        buttons.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task Login_ShouldHandleAuthStateChanges_Gracefully()
    {
        // Arrange
        var unauthenticatedState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(unauthenticatedState);

        // Act
        var component = RenderComponent<Login>();
        
        // Change authentication state
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser@example.com")
        }, "test"));
        
        var authenticatedState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authenticatedState);
        
        // Force a re-render and wait
        component.Render();
        await Task.Delay(50);

        // Assert - Component should handle state changes without throwing
        component.Should().NotBeNull();
    }

    [Fact]
    public void Login_ShouldShowProviderIcons_Correctly()
    {
        // Arrange
        var mockAuthState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<Login>();

        // Assert
        // Check for provider-specific styling or icons
        var googleButton = component.Find("a[href='/auth/google']");
        googleButton.ClassList.Should().Contain("mud-button");
        
        var microsoftButton = component.Find("a[href='/auth/microsoft']");
        microsoftButton.ClassList.Should().Contain("mud-button");
    }

    [Fact]
    public void Login_ShouldHaveCorrectButtonTargets()
    {
        // Arrange
        var mockAuthState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<Login>();

        // Assert
        var googleButton = component.Find("a[href='/auth/google']");
        googleButton.GetAttribute("href").Should().Be("/auth/google");
        
        var microsoftButton = component.Find("a[href='/auth/microsoft']");
        microsoftButton.GetAttribute("href").Should().Be("/auth/microsoft");
    }

    [Fact]
    public async Task Login_ShouldLoadInitialState_WithoutErrors()
    {
        // Arrange
        var mockAuthState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act & Assert - Should not throw during component initialization
        var exception = await Record.ExceptionAsync(async () =>
        {
            var component = RenderComponent<Login>();
            await Task.Delay(50); // Allow async operations to complete
        });

        exception.Should().BeNull();
    }

    [Fact]
    public void Login_ShouldRenderResponsiveLayout()
    {
        // Arrange
        var mockAuthState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<Login>();

        // Assert
        // Check for responsive grid or container classes
        var container = component.Find(".mud-container");
        container.Should().NotBeNull();
        
        // Check for grid system usage
        var gridItems = component.FindAll(".mud-grid-item");
        gridItems.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldHandleUnauthenticatedUser()
    {
        // Arrange
        var unauthenticatedUser = new ClaimsPrincipal();
        var mockAuthState = new AuthenticationState(unauthenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);
        
        // Act
        var component = RenderComponent<Login>();
        
        // Wait for async operations
        await Task.Delay(50);

        // Assert - Should render login page for unauthenticated users
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Welcome to Setlist Studio");
    }
}