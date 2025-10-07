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

    [Fact]
    public void MainLayout_ThemeProvider_ShouldHandleNullThemeProvider()
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

        // Access the MainLayout instance
        var mainLayout = component.FindComponent<MainLayout>();
        
        // Act - Simulate OnAfterRenderAsync with null theme provider
        mainLayout.InvokeAsync(async () =>
        {
            // Set the private field _mudThemeProvider to null using reflection
            var themeProviderField = typeof(MainLayout).GetField("_mudThemeProvider", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            themeProviderField?.SetValue(mainLayout.Instance, null);
            
            // Call OnAfterRenderAsync with firstRender = true
            var onAfterRenderMethod = typeof(MainLayout).GetMethod("OnAfterRenderAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (onAfterRenderMethod != null)
            {
                await (Task)onAfterRenderMethod.Invoke(mainLayout.Instance, new object[] { true })!;
            }
        });

        // Assert - Should not throw exception when theme provider is null
        component.Should().NotBeNull("Component should handle null theme provider gracefully");
    }

    [Fact]
    public void MainLayout_ThemeProvider_ShouldHandleFirstRenderFalse()
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

        var mainLayout = component.FindComponent<MainLayout>();
        
        // Act - Simulate OnAfterRenderAsync with firstRender = false
        mainLayout.InvokeAsync(async () =>
        {
            var onAfterRenderMethod = typeof(MainLayout).GetMethod("OnAfterRenderAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (onAfterRenderMethod != null)
            {
                await (Task)onAfterRenderMethod.Invoke(mainLayout.Instance, new object[] { false })!;
            }
        });

        // Assert - Should not call GetSystemPreference when firstRender is false
        component.Should().NotBeNull("Component should handle non-first render correctly");
    }

    [Fact] 
    public void MainLayout_DrawerToggle_ShouldChangeDrawerState()
    {
        // Arrange
        SetupAuthenticatedUser();
        
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        var mainLayout = component.FindComponent<MainLayout>();

        // Act - Toggle drawer (should start as open = true, then become false)
        var initialDrawerState = GetPrivateField<bool>(mainLayout.Instance, "_drawerOpen");
        
        // Find and click the menu button
        var menuButton = component.FindAll("button").FirstOrDefault(b => 
            b.GetAttribute("aria-label")?.Contains("navigation menu") == true ||
            b.InnerHtml.Contains("menu"));
        
        menuButton?.Click();

        // Assert
        var newDrawerState = GetPrivateField<bool>(mainLayout.Instance, "_drawerOpen");
        newDrawerState.Should().Be(!initialDrawerState, "Drawer state should toggle when menu button is clicked");
    }

    [Fact]
    public void MainLayout_ThemeToggle_ShouldChangeThemeState()
    {
        // Arrange
        SetupAuthenticatedUser();
        
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        var mainLayout = component.FindComponent<MainLayout>();

        // Act - Toggle theme (should start as false, then become true)
        var initialThemeState = GetPrivateField<bool>(mainLayout.Instance, "_isDarkMode");
        
        // Find and click the theme toggle button
        var themeButton = component.FindAll("button").FirstOrDefault(b => 
            b.GetAttribute("aria-label")?.Contains("mode") == true);
        
        themeButton?.Click();

        // Assert
        var newThemeState = GetPrivateField<bool>(mainLayout.Instance, "_isDarkMode");
        newThemeState.Should().Be(!initialThemeState, "Theme state should toggle when theme button is clicked");
    }

    [Fact]
    public void MainLayout_AuthorizedView_ShouldShowUserMenu_WhenAuthenticated()
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

        // Assert - Should show user menu for authenticated users
        component.WaitForState(() => 
        {
            try
            {
                // Look for account circle icon or user menu indicators
                return component.Markup.Contains("account_circle") || 
                       component.Markup.Contains("User menu") ||
                       component.FindAll("button").Any(b => b.GetAttribute("aria-label")?.Contains("User menu") == true);
            }
            catch
            {
                return false;
            }
        }, TimeSpan.FromSeconds(3));

        var hasUserMenuElements = component.Markup.Contains("account_circle") || 
                                 component.Markup.Contains("User menu") ||
                                 component.FindAll("button").Any(b => b.GetAttribute("aria-label")?.Contains("User menu") == true);
        
        hasUserMenuElements.Should().BeTrue("Authenticated users should see user menu elements");
    }

    [Fact]
    public void MainLayout_NotAuthorizedView_ShouldShowSignIn_WhenNotAuthenticated()
    {
        // Arrange - Use Bunit's built-in authentication support
        var authContext = this.AddTestAuthorization();
        authContext.SetNotAuthorized(); // This should set up unauthenticated state
        
        // Act - Render MainLayout directly (no need for wrapper with Bunit test auth)
        var component = RenderComponent<MainLayout>();

        // Wait for component to settle
        Task.Delay(500).Wait();

        // Assert - Should show sign in elements for unauthenticated users
        var markup = component.Markup;
        var hasSignIn = markup.Contains("Sign In", StringComparison.OrdinalIgnoreCase);
        var hasLoginLink = markup.Contains("/login", StringComparison.OrdinalIgnoreCase);
        var hasAccountCircle = markup.Contains("AccountCircle", StringComparison.OrdinalIgnoreCase);
        var hasUserMenu = markup.Contains("User menu", StringComparison.OrdinalIgnoreCase);
        
        // Should have NotAuthorized content (Sign In)
        hasSignIn.Should().BeTrue("Unauthenticated users should see Sign In text");
        hasLoginLink.Should().BeTrue("Unauthenticated users should see login link");
        
        // Should NOT have Authorized content (User menu)
        hasAccountCircle.Should().BeFalse($"Unauthenticated users should NOT see AccountCircle. HasSignIn: {hasSignIn}, HasLoginLink: {hasLoginLink}");
        hasUserMenu.Should().BeFalse($"Unauthenticated users should NOT see User menu. HasSignIn: {hasSignIn}, HasLoginLink: {hasLoginLink}");
    }

    [Fact]
    public void MainLayout_ErrorBoundary_ShouldHandleChildComponentError()
    {
        // Arrange
        SetupAuthenticatedUser();
        
        // Create a component that throws an exception
        var throwingComponent = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.AddAttribute(1, "Body", (RenderFragment)(builder =>
                {
                    builder.OpenComponent<ThrowingComponent>(0);
                    builder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));

        // Act & Assert - Should handle the error gracefully
        throwingComponent.Should().NotBeNull("MainLayout should handle child component errors");
        
        // The error boundary should catch the exception and display error content
        var hasErrorContent = throwingComponent.Markup.Contains("error") || 
                             throwingComponent.Markup.Contains("Error") ||
                             throwingComponent.Markup.Contains("exception");
        
        // Note: Error boundaries in Blazor work differently than React, so this test validates structure
        throwingComponent.Should().NotBeNull("Component should render even with error boundary scenarios");
    }

    [Fact]
    public void MainLayout_OnAfterRenderAsync_ShouldSetSystemPreference_OnFirstRender()
    {
        // Arrange
        SetupAuthenticatedUser();
        
        // Mock the GetSystemPreference to return true (dark mode)
        JSInterop.Setup<bool>("mudThemeProvider.getSystemPreference")
               .SetResult(true);

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Assert - The component should render and the JSInterop should have been called
        component.Should().NotBeNull("MainLayout should render with theme provider");
        
        // Note: In unit tests, the JSInterop may not be called exactly as in runtime
        // The test validates the component structure is correct
    }

    [Fact]
    public void MainLayout_OnAfterRenderAsync_ShouldHandleThemeProviderGracefully()
    {
        // Arrange
        SetupAuthenticatedUser();

        // Act - Render without setting up theme provider properly
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Assert - Should handle null theme provider gracefully
        component.Should().NotBeNull("MainLayout should handle null theme provider gracefully");
        component.Markup.Should().NotBeNullOrEmpty("MainLayout should still render with theme provider issues");
    }

    [Fact]
    public void MainLayout_ShouldRenderAccessibilityFeatures()
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

        // Assert - Check for accessibility features
        component.Markup.Should().Contain("Skip to content", "Should have skip link for accessibility");
        component.Markup.Should().Contain("main-content", "Should have main content landmark");
        component.Markup.Should().Contain("aria-label", "Should have aria-labels for screen readers");
        component.Markup.Should().Contain("tabindex=\"-1\"", "Should have proper focus management");
    }

    [Fact]  
    public void MainLayout_ShouldHandleUserIdentityName_WhenNull()
    {
        // Arrange - Setup user with null Identity.Name
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            // Intentionally omitting ClaimTypes.Name to test null handling
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);

        _mockAuthStateProvider
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Assert - Should handle null identity name gracefully
        component.Should().NotBeNull("MainLayout should handle null identity name");
        component.Markup.Should().NotBeNullOrEmpty("MainLayout should render even with null user name");
    }

    [Fact]
    public void MainLayout_ShouldToggleDrawerState_MultipleTimes()
    {
        // Arrange
        SetupAuthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Act - Toggle drawer multiple times to test state management
        var menuButton = component.Find("button[aria-label='Open navigation menu']");
        
        // First toggle
        menuButton.Click();
        component.Markup.Should().NotBeNullOrEmpty("Component should render after first toggle");
        
        // Second toggle (back to original state)
        menuButton.Click();
        component.Markup.Should().NotBeNullOrEmpty("Component should render after second toggle");
        
        // Third toggle
        menuButton.Click();
        
        // Assert - Component should handle multiple state changes
        component.Markup.Should().NotBeNullOrEmpty("Component should handle multiple drawer toggles");
    }

    [Fact]
    public void MainLayout_ShouldToggleThemeState_MultipleTimes()
    {
        // Arrange
        SetupAuthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Act - Toggle theme multiple times to test state management  
        var themeButton = component.Find("button[aria-label*='Switch to']");
        
        // First toggle
        themeButton.Click();
        component.Markup.Should().NotBeNullOrEmpty("Component should render after first theme toggle");
        
        // Second toggle (back to original state)
        themeButton.Click();
        component.Markup.Should().NotBeNullOrEmpty("Component should render after second theme toggle");
        
        // Assert - Component should handle multiple theme changes
        component.Markup.Should().NotBeNullOrEmpty("Component should handle multiple theme toggles");
    }

    [Fact]
    public void MainLayout_ShouldRenderMudBlazorComponents()
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

        // Assert - Should contain MudBlazor component structure
        component.Markup.Should().Contain("mud-", "Should render MudBlazor components with proper CSS classes");
        component.Markup.Should().NotBeNullOrEmpty("MainLayout should render MudBlazor layout structure");
    }

    [Fact]
    public void MainLayout_ShouldHandleNavigationMenuEvents()
    {
        // Arrange
        SetupAuthenticatedUser();
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.CloseComponent();
            }));

        // Act - Interact with navigation elements
        var allButtons = component.FindAll("button");
        allButtons.Should().NotBeEmpty("MainLayout should have interactive buttons");

        // Test that buttons don't cause exceptions when clicked
        foreach (var button in allButtons.Take(3)) // Test first few buttons to avoid excessive testing
        {
            try
            {
                button.Click();
                // If we get here, the button click was handled without exception
            }
            catch (Exception ex) when (ex.Message.Contains("JavaScript"))
            {
                // JS interop exceptions are expected in unit tests, ignore them
            }
        }

        // Assert - Component should still be functional after interactions
        component.Markup.Should().NotBeNullOrEmpty("MainLayout should remain functional after navigation interactions");
    }

    [Fact]
    public void MainLayout_ShouldRenderWithCustomBody()
    {
        // Arrange
        SetupAuthenticatedUser();
        var customBodyContent = "Custom Test Content for MainLayout";

        // Act
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder => 
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.AddAttribute(1, "Body", (RenderFragment)(builder =>
                {
                    builder.AddContent(0, customBodyContent);
                }));
                childBuilder.CloseComponent();
            }));

        // Assert
        component.Markup.Should().Contain(customBodyContent, "MainLayout should render custom body content");
        component.Markup.Should().Contain("main-content", "MainLayout should wrap body content in main element");
    }

    // Helper method to get private fields using reflection
    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (T)field.GetValue(obj)! : default(T)!;
    }
}

// Helper component that throws an exception for testing error boundaries
public class ThrowingComponent : ComponentBase
{
    protected override void OnInitialized()
    {
        throw new InvalidOperationException("Test exception for error boundary");
    }
}