using Bunit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using System.Security.Claims;
using Xunit;
using FluentAssertions;
using MudBlazor;
using MudBlazor.Services;
using IndexPage = SetlistStudio.Web.Pages.Index;

namespace SetlistStudio.Tests.Web.Pages;

public class IndexTests : TestContext
{
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;
    private readonly Mock<ISongService> _mockSongService;
    private readonly Mock<ISetlistService> _mockSetlistService;

    public IndexTests()
    {
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
        _mockSongService = new Mock<ISongService>();
        _mockSetlistService = new Mock<ISetlistService>();
        
        // Register MudBlazor services
        Services.AddMudServices();
        
        // Register authorization services 
        Services.AddAuthorizationCore();
        
        // Add a mock authorization service to handle AuthorizeView components
        var mockAuthService = new Mock<IAuthorizationService>();
        mockAuthService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                      .ReturnsAsync((ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements) =>
                      {
                          // Return success if user is authenticated, failure if not
                          return user.Identity?.IsAuthenticated == true 
                              ? AuthorizationResult.Success() 
                              : AuthorizationResult.Failed();
                      });
        Services.AddScoped<IAuthorizationService>(_ => mockAuthService.Object);
        
        // Register mocked services
        Services.AddScoped<AuthenticationStateProvider>(_ => _mockAuthStateProvider.Object);
        Services.AddScoped<ISongService>(_ => _mockSongService.Object);
        Services.AddScoped<ISetlistService>(_ => _mockSetlistService.Object);
    }

    [Fact]
    public void Index_ShouldRenderWelcomeMessage()
    {
        // Arrange
        var mockAuthState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        // Assert
        component.Markup.Should().Contain("Welcome to Setlist Studio");
        component.Markup.Should().Contain("Organize your music library and create professional setlists");
    }

    [Fact]
    public void Index_ShouldDisplayFeaturesSections()
    {
        // Arrange
        var mockAuthState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        // Assert
        component.Markup.Should().Contain("Everything You Need for Professional Performances");
        component.Markup.Should().Contain("Song Library");
        component.Markup.Should().Contain("Smart Setlists");
        component.Markup.Should().Contain("Performance Ready");
    }

    [Fact]
    public void Index_ShouldShowSignInButton_WhenUserNotAuthenticated()
    {
        // Arrange
        var unauthenticatedUser = new ClaimsPrincipal();
        var mockAuthState = new AuthenticationState(unauthenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        // Assert
        var signInButton = component.Find("a[href='/login']");
        signInButton.Should().NotBeNull();
        signInButton.TextContent.Should().Contain("Sign In to Get Started");
    }

    [Fact]
    public async Task Index_ShouldShowUserContent_WhenUserAuthenticated()
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

        // Setup mock services
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Song>(), 5));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Setlist>(), 3));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new[] { "Rock", "Pop", "Jazz" });
        
        var upcomingSetlists = new List<Setlist>
        {
            new() { PerformanceDate = DateTime.Now.AddDays(7), UserId = "test-user-id" },
            new() { PerformanceDate = DateTime.Now.AddDays(-1), UserId = "test-user-id" }
        };
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, true, 1, 100))
            .ReturnsAsync((upcomingSetlists, 2));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert
        component.Markup.Should().Contain("Welcome back, testuser@example.com");
        component.Markup.Should().Contain("My Songs");
        component.Markup.Should().Contain("My Setlists");
        component.Markup.Should().Contain("Your Music at a Glance");
    }

    [Fact] 
    public async Task OnInitializedAsync_ShouldLoadUserStats_WhenUserAuthenticated()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup mock services with specific data
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Song>(), 15));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Setlist>(), 8));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new[] { "Rock", "Pop", "Jazz", "Classical" });
        
        var upcomingSetlists = new List<Setlist>
        {
            new() { PerformanceDate = DateTime.Now.AddDays(7), UserId = "test-user-id" },
            new() { PerformanceDate = DateTime.Now.AddDays(14), UserId = "test-user-id" },
            new() { PerformanceDate = DateTime.Now.AddDays(-1), UserId = "test-user-id" }
        };
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, true, 1, 100))
            .ReturnsAsync((upcomingSetlists, 3));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations to complete
        await Task.Delay(200);
        component.Render();

        // Assert
        component.Markup.Should().Contain("15"); // Total songs
        component.Markup.Should().Contain("8");  // Total setlists
        component.Markup.Should().Contain("4");  // Unique genres
        component.Markup.Should().Contain("2");  // Upcoming performances (only future dates)
        
        // Verify service calls
        _mockSongService.Verify(x => x.GetSongsAsync("test-user-id", null, null, null, 1, 1), Times.Once);
        _mockSetlistService.Verify(x => x.GetSetlistsAsync("test-user-id", null, null, null, 1, 1), Times.Once);
        _mockSongService.Verify(x => x.GetGenresAsync("test-user-id"), Times.Once);
        _mockSetlistService.Verify(x => x.GetSetlistsAsync("test-user-id", null, null, true, 1, 100), Times.Once);
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldNotLoadStats_WhenUserNotAuthenticated()
    {
        // Arrange
        var unauthenticatedUser = new ClaimsPrincipal();
        var mockAuthState = new AuthenticationState(unauthenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);

        // Assert
        _mockSongService.Verify(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockSetlistService.Verify(x => x.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), 
            It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldNotLoadStats_WhenUserIdIsEmpty()
    {
        // Arrange
        var authenticatedUserWithoutId = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser@example.com")
            // No NameIdentifier claim
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUserWithoutId);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);

        // Assert
        _mockSongService.Verify(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockSetlistService.Verify(x => x.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), 
            It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task LoadUserStatsAsync_ShouldHandleExceptions_Gracefully()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup mock to throw exception
        _mockSongService.Setup(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);

        // Assert - Component should still render without crashing
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Welcome to Setlist Studio");
    }

    [Fact]
    public void Index_ShouldDisplayCallToAction_WithCorrectButtons()
    {
        // Arrange
        var mockAuthState = new AuthenticationState(new ClaimsPrincipal());
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        // Assert
        component.Markup.Should().Contain("Ready to Transform Your Music Performances?");
        component.Markup.Should().Contain("Join musicians worldwide");
        
    // Should show sign in button for unauthenticated users
    var signInButton = component.Find("a[href='/login']");
    signInButton.Should().NotBeNull();
    signInButton.TextContent.Should().Contain("Sign In to Get Started");
    }

    [Fact]
    public async Task Index_ShouldShowCreateSetlistButton_WhenUserAuthenticated()
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

        // Setup minimal mock services
        _mockSongService.Setup(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Song>(), 0));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), 
            It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Setlist>(), 0));
        
        _mockSongService.Setup(x => x.GetGenresAsync(It.IsAny<string>()))
            .ReturnsAsync(new string[0]);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert
        var createButton = component.Find("a[href='/setlists/create']");
        createButton.Should().NotBeNull();
        createButton.TextContent.Should().Contain("Create Your First Setlist");
    }

    [Fact]
    public async Task Index_ShouldHandleNullUserName_InWelcomeMessage()
    {
        // Arrange
        var authenticatedUserWithoutName = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            // No Name claim
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUserWithoutName);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup minimal mock services
        _mockSongService.Setup(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Song>(), 0));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), 
            It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Setlist>(), 0));
        
        _mockSongService.Setup(x => x.GetGenresAsync(It.IsAny<string>()))
            .ReturnsAsync(new string[0]);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert - Should handle null name gracefully
        component.Markup.Should().Contain("Welcome back,");
        // The component should still render without throwing an exception
        component.Should().NotBeNull();
    }
}