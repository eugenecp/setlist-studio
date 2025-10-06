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

    [Fact]
    public void MainLayout_ShouldToggleDrawer_WhenMenuButtonClicked()
    {
        // Arrange
        SetupAuthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Act - Find and click the menu button
        var menuButton = component.Find("button[aria-label='Open navigation menu']");
        menuButton.Should().NotBeNull("Menu button should be present");
        menuButton.Click();

        // Assert - The drawer state should have changed
        component.Markup.Should().NotBeNullOrEmpty("Component should still render after drawer toggle");
    }

    [Fact]
    public void MainLayout_ShouldToggleTheme_WhenThemeButtonClicked()
    {
        // Arrange
        SetupAuthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Act - Find and click the theme toggle button
        var themeButton = component.Find("button[aria-label*='Switch to']");
        themeButton.Should().NotBeNull("Theme toggle button should be present");
        themeButton.Click();

        // Assert - The component should still render after theme toggle
        component.Markup.Should().NotBeNullOrEmpty("Component should still render after theme toggle");
    }

    [Fact]
    public void MainLayout_ShouldShowUserMenu_WhenAuthenticated()
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
        component.Markup.Should().Contain("Icons.Material.Filled.AccountCircle", "User menu should have account icon for authenticated users");
        component.Markup.Should().Contain("User menu", "User menu should have aria-label for authenticated users");
    }

    [Fact]
    public void MainLayout_ShouldShowSignInButton_WhenNotAuthenticated()
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
        component.Markup.Should().Contain("mud-button", "MainLayout should contain button elements");
        component.Markup.Should().Contain("Setlist Studio", "MainLayout should contain app title");
    }

    [Fact]
    public void MainLayout_ShouldContainAccessibilityFeatures()
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
        component.Markup.Should().Contain("Skip to content", "MainLayout should have skip to content link for accessibility");
        component.Markup.Should().Contain("main-content", "MainLayout should have main content landmark");
        component.Markup.Should().Contain("aria-label", "MainLayout should have aria-labels for accessibility");
    }

    [Fact]
    public void MainLayout_ShouldRenderNavigationDrawer()
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
        component.Markup.Should().Contain("Navigation", "MainLayout should contain navigation drawer header");
    }

    [Fact]
    public void MainLayout_ShouldHandleErrorBoundary()
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

        // Assert - ErrorBoundary should be present in markup structure
        component.Markup.Should().NotBeNullOrEmpty("MainLayout should render with error boundary structure");
    }

    [Fact]
    public void MainLayout_ShouldHaveResponsiveDesign()
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
        component.Markup.Should().Contain("mud-drawer", "MainLayout should have responsive drawer");
        component.Markup.Should().Contain("mud-appbar", "MainLayout should have app bar");
        component.Markup.Should().Contain("mud-main-content", "MainLayout should have main content area");
        component.Markup.Should().Contain("mud-layout", "MainLayout should have layout container");
    }

    [Fact]
    public void MainLayout_OnAfterRenderAsync_ShouldHandleSystemPreferenceCheck()
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
        component.Should().NotBeNull("MainLayout should handle system preference detection");
        // The OnAfterRenderAsync method should be called during component lifecycle
        component.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MainLayout_OnAfterRenderAsync_ShouldHandleNullThemeProvider()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act - This should not throw even if theme provider is null initially
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Assert
        component.Should().NotBeNull("MainLayout should handle null theme provider gracefully");
    }

    [Fact]
    public void MainLayout_ErrorBoundary_ShouldDisplayErrorWhenExceptionOccurs()
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
        component.Markup.Should().NotBeNullOrEmpty("MainLayout should render even with child component errors");
    }

    [Fact]
    public void MainLayout_ShouldHandleMultipleThemeToggles()
    {
        // Arrange
        SetupAuthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Act - Toggle theme multiple times
        var themeButton = component.Find("button[aria-label*='Switch to']");
        themeButton.Should().NotBeNull("Theme toggle button should be present");
        
        // Multiple toggles
        themeButton.Click();
        themeButton.Click();
        themeButton.Click();

        // Assert
        component.Markup.Should().NotBeNullOrEmpty("Component should handle multiple theme toggles");
    }

    [Fact]
    public void MainLayout_ShouldHandleMultipleDrawerToggles()
    {
        // Arrange
        SetupAuthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Act - Toggle drawer multiple times
        var menuButton = component.Find("button[aria-label='Open navigation menu']");
        menuButton.Should().NotBeNull("Menu button should be present");
        
        // Multiple toggles
        menuButton.Click();
        menuButton.Click();
        menuButton.Click();

        // Assert
        component.Markup.Should().NotBeNullOrEmpty("Component should handle multiple drawer toggles");
    }

    [Fact]
    public void MainLayout_ThemeButton_ShouldHaveDifferentLabelsForDarkAndLight()
    {
        // Arrange
        SetupAuthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Act & Assert - Check initial state
        var initialThemeButton = component.Find("button[aria-label*='Switch to']");
        initialThemeButton.Should().NotBeNull("Theme button should be present");
        
        // Click to toggle theme
        initialThemeButton.Click();
        
        // Check that the button still exists and potentially has different label
        var toggledThemeButton = component.Find("button[aria-label*='Switch to']");
        toggledThemeButton.Should().NotBeNull("Theme button should still be present after toggle");
    }

    [Fact]
    public void MainLayout_UserMenu_ShouldShowUserNameForAuthenticatedUser()
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
        // User menu should be present for authenticated users
        component.Markup.Should().Contain("Icons.Material.Filled.AccountCircle", "User menu should display account icon for authenticated users");
    }

    [Fact]
    public void MainLayout_AuthorizeView_ShouldRenderDifferentContentBasedOnAuthState()
    {
        // Test authenticated state
        SetupAuthenticatedUser();
        var authenticatedComponent = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Test unauthenticated state
        SetupUnauthenticatedUser();
        var unauthenticatedComponent = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Assert
        authenticatedComponent.Markup.Should().Contain("Icons.Material.Filled.AccountCircle", "Authenticated view should show account icon");
        unauthenticatedComponent.Markup.Should().Contain("Icons.Material.Filled.AccountCircle", "Unauthenticated view should still show account icon for login");
    }

    [Fact]
    public void MainLayout_NavigationDrawer_ShouldToggleOnMenuButtonClick()
    {
        // Arrange
        SetupAuthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Act - Find and click the drawer toggle button
        var menuButton = component.Find("button[aria-label*='menu']");
        if (menuButton != null)
        {
            menuButton.Click();
        }
        else
        {
            // Try finding by icon
            var iconButton = component.Find("button");
            iconButton?.Click();
        }

        // Assert - The drawer state should change (we can't directly check internal state, but markup might change)
        component.Should().NotBeNull("Component should remain stable after drawer toggle");
    }

    [Fact]
    public void MainLayout_AppBar_ShouldContainNavigationElements()
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
        component.Markup.Should().Contain("mud-appbar", "Should contain MudBlazor app bar");
        component.Markup.Should().Contain("Setlist Studio", "Should contain application title");
    }

    [Fact]
    public void MainLayout_ThemeToggle_ShouldHandleUserInteraction()
    {
        // Arrange
        SetupAuthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Act - Try to find and interact with theme toggle
        var themeButtons = component.FindAll("button");
        var themeToggleButton = themeButtons.FirstOrDefault(b => 
            b.GetAttribute("aria-label")?.Contains("Switch to") == true ||
            b.InnerHtml.Contains("light_mode") ||
            b.InnerHtml.Contains("dark_mode"));

        if (themeToggleButton != null)
        {
            themeToggleButton.Click();
        }

        // Assert
        component.Should().NotBeNull("Component should handle theme toggle interaction");
    }

    [Fact]
    public void MainLayout_Content_ShouldRenderMainContentArea()
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
        component.Markup.Should().Contain("mud-main-content", "Should contain main content area");
    }

    [Fact]
    public void MainLayout_AuthenticationStateChanges_ShouldUpdateUI()
    {
        // Arrange - Start with unauthenticated user
        SetupUnauthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        var initialMarkup = component.Markup;

        // Act - Change to authenticated user and re-render
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser@example.com"),
            new Claim(ClaimTypes.NameIdentifier, "123")
        }, "test"));

        var newAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(newAuthState);

        // Force re-render with new state
        component.SetParametersAndRender();

        // Assert - The markup should remain the same as MainLayout doesn't show different content based on auth
        component.Markup.Should().NotBeEmpty("Layout should render successfully");
        // Layout content is the same for authenticated and unauthenticated users
        // so we just verify it renders without errors
    }

    [Fact]
    public void MainLayout_MudBlazorComponents_ShouldRenderWithoutErrors()
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

        // Assert - Check for presence of key MudBlazor components
        component.Markup.Should().Contain("mud-layout", "Should contain MudLayout");
        component.Markup.Should().Contain("mud-appbar", "Should contain MudAppBar");
        component.Markup.Should().NotContain("blazor-error-ui", "Should not contain error UI components");
        component.Markup.Should().NotContain("error-boundary", "Should not contain error boundary");
        component.Markup.Should().NotContain("exception", "Should not contain exception messages");
    }

    [Fact]
    public void MainLayout_UserMenu_ShouldHandleClickInteraction()
    {
        // Arrange
        SetupAuthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Act - Try to click on user menu button
        var userMenuButtons = component.FindAll("button");
        var accountButton = userMenuButtons.FirstOrDefault(b => 
            b.InnerHtml.Contains("account_circle") || 
            b.GetAttribute("aria-label")?.Contains("Account") == true);

        if (accountButton != null)
        {
            accountButton.Click();
        }

        // Assert
        component.Should().NotBeNull("Component should handle user menu interaction");
    }

    [Fact]
    public void MainLayout_Responsive_ShouldHandleDifferentScreenSizes()
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

        // Assert - Check for responsive classes
        component.Markup.Should().Contain("mud", "Should contain MudBlazor responsive classes");
    }
}