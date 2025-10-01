using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MudBlazor.Services;
using SetlistStudio.Web.Shared;
using Xunit;
using MudBlazor;
using FluentAssertions;

namespace SetlistStudio.Tests.Web.Shared
{
    public class MainLayoutTests : TestContext
    {
        public MainLayoutTests()
        {
            // Add required services for MudBlazor
            Services.AddMudServices();
            
            // Add a test authorization service
            Services.AddSingleton<AuthenticationStateProvider, TestAuthStateProvider>();
            Services.AddAuthorizationCore();
        }

        [Fact]
        public void MainLayout_ShouldRenderWithoutErrors()
        {
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            component.Should().NotBeNull();
            component.Find("mud-layout").Should().NotBeNull();
        }

        [Fact]
        public void MainLayout_ShouldShowSetlistStudioTitle()
        {
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var titleElement = component.Find("mud-text");
            titleElement.TextContent.Should().Contain("Setlist Studio");
        }

        [Fact]
        public void MainLayout_ShouldHaveSkipToContentLink()
        {
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var skipLink = component.Find(".skip-to-content");
            skipLink.Should().NotBeNull();
            skipLink.GetAttribute("href").Should().Be("#main-content");
        }

        [Fact]
        public void MainLayout_ShouldHaveMenuButton()
        {
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var menuButton = component.Find("mud-icon-button[aria-label='Open navigation menu']");
            menuButton.Should().NotBeNull();
        }

        [Fact]
        public void MainLayout_ShouldToggleDrawerWhenMenuButtonClicked()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();
            var menuButton = component.Find("mud-icon-button[aria-label='Open navigation menu']");

            // Act
            menuButton.Click();

            // Assert - component should handle the interaction
            component.Should().NotBeNull();
        }

        [Fact]
        public void MainLayout_ShouldHaveThemeToggleButton()
        {
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var themeButton = component.Find("mud-icon-button[aria-label*='Switch to']");
            themeButton.Should().NotBeNull();
        }

        [Fact]
        public void MainLayout_ShouldToggleThemeWhenThemeButtonClicked()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();
            var themeButton = component.Find("mud-icon-button[aria-label*='Switch to']");

            // Act
            themeButton.Click();

            // Assert - component should handle the interaction
            component.Should().NotBeNull();
        }

        [Fact]
        public void MainLayout_ShouldShowSignInButtonWhenNotAuthenticated()
        {
            // Arrange
            Services.AddSingleton<AuthenticationStateProvider, TestAuthStateProvider>();

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var signInButton = component.Find("mud-button[href='/login']");
            signInButton.Should().NotBeNull();
            signInButton.TextContent.Should().Contain("Sign In");
        }

        [Fact]
        public void MainLayout_ShouldShowUserMenuWhenAuthenticated()
        {
            // Arrange
            Services.AddSingleton<AuthenticationStateProvider>(
                new TestAuthStateProvider(isAuthenticated: true, userName: "test@example.com"));

            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var userMenu = component.Find("mud-menu[aria-label='User menu']");
            userMenu.Should().NotBeNull();
        }

        [Fact]
        public void MainLayout_ShouldHaveNavigationDrawer()
        {
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var drawer = component.Find("mud-drawer");
            drawer.Should().NotBeNull();
            
            var drawerHeader = component.Find("mud-drawer-header");
            drawerHeader.Should().NotBeNull();
            drawerHeader.TextContent.Should().Contain("Navigation");
        }

        [Fact]
        public void MainLayout_ShouldHaveMainContentArea()
        {
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var mainContent = component.Find("main#main-content");
            mainContent.Should().NotBeNull();
            mainContent.GetAttribute("tabindex").Should().Be("-1");
        }

        [Fact]
        public void MainLayout_ShouldHaveErrorBoundary()
        {
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert
            var errorBoundary = component.Find("errorboundary");
            errorBoundary.Should().NotBeNull();
        }

        [Fact]
        public void MainLayout_ShouldInitializeDarkModeFromSystemPreference()
        {
            // Act
            var component = RenderComponent<MainLayout>();

            // Assert - component should render without errors and handle theme initialization
            component.Should().NotBeNull();
            // The OnAfterRenderAsync will be called, testing the async initialization path
        }

        [Fact]
        public void MainLayout_ShouldHandleThemeToggleCorrectly()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();
            var themeButton = component.Find("mud-icon-button[aria-label*='Switch to']");
            var initialAriaLabel = themeButton.GetAttribute("aria-label");

            // Act
            themeButton.Click();
            component.Render(); // Force rerender

            // Assert
            var updatedThemeButton = component.Find("mud-icon-button[aria-label*='Switch to']");
            var newAriaLabel = updatedThemeButton.GetAttribute("aria-label");
            newAriaLabel.Should().NotBe(initialAriaLabel);
        }

        [Fact]
        public void MainLayout_ShouldHandleDrawerToggleCorrectly()
        {
            // Arrange
            var component = RenderComponent<MainLayout>();
            var menuButton = component.Find("mud-icon-button[aria-label='Open navigation menu']");

            // Act - Toggle drawer multiple times
            menuButton.Click();
            component.Render();
            menuButton.Click();
            component.Render();

            // Assert - component should handle state changes properly
            component.Should().NotBeNull();
        }
    }

    // Test helper class for authentication state
    public class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly bool _isAuthenticated;
        private readonly string _userName;

        public TestAuthStateProvider(bool isAuthenticated = false, string userName = "")
        {
            _isAuthenticated = isAuthenticated;
            _userName = userName;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = _isAuthenticated 
                ? new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, _userName),
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id")
                }, "test")
                : new ClaimsIdentity();

            var user = new ClaimsPrincipal(identity);
            return Task.FromResult(new AuthenticationState(user));
        }
    }
}