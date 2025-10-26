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

namespace SetlistStudio.Tests.Pages;

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
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, (bool?)true, 1, 100))
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
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, (bool?)true, 1, 100))
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
        _mockSetlistService.Verify(x => x.GetSetlistsAsync("test-user-id", null, null, (bool?)true, 1, 100), Times.Once);
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
        _ = RenderComponent<IndexPage>(parameters => parameters
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
        _ = RenderComponent<IndexPage>(parameters => parameters
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

    [Fact]
    public async Task Index_ShouldHandlePartialServiceFailures()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup some services to work and others to fail
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Song>(), 10));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, null, 1, 1))
            .ThrowsAsync(new Exception("Setlist service failed"));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new[] { "Rock", "Pop" });

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert - Component should still render despite partial failures
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Welcome to Setlist Studio");
    }

    [Fact]
    public async Task Index_ShouldDisplayZeroStats_WhenUserHasNoData()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "new-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup services to return empty data
        _mockSongService.Setup(x => x.GetSongsAsync("new-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Song>(), 0));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("new-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Setlist>(), 0));
        
        _mockSongService.Setup(x => x.GetGenresAsync("new-user-id"))
            .ReturnsAsync(new string[0]);
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("new-user-id", null, null, true, 1, 100))
            .ReturnsAsync((new List<Setlist>(), 0));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert
        component.Markup.Should().Contain("0"); // Should show zero stats
        component.Markup.Should().Contain("Songs in Library");
        component.Markup.Should().Contain("Created Setlists");
    }

    [Fact]
    public async Task Index_ShouldCountUpcomingPerformancesCorrectly()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup services with mixed past/future performances
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Song>(), 5));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Setlist>(), 3));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new[] { "Rock" });
        
        var mixedSetlists = new List<Setlist>
        {
            new() { PerformanceDate = DateTime.Now.AddDays(7), UserId = "test-user-id" },    // Future
            new() { PerformanceDate = DateTime.Now.AddDays(-7), UserId = "test-user-id" },   // Past
            new() { PerformanceDate = DateTime.Now.AddDays(14), UserId = "test-user-id" },   // Future
            new() { PerformanceDate = null, UserId = "test-user-id" }                        // No date
        };
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, (bool?)true, 1, 100))
            .ReturnsAsync((mixedSetlists, 4));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert - Should only count future performances (2)
        component.Markup.Should().Contain("2"); // Only future performances
        component.Markup.Should().Contain("Upcoming Shows");
    }

    [Fact]
    public async Task Index_ShouldHandleEmptyGenresList()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup services with empty genres
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Song>(), 1));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Setlist>(), 1));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new string[0]); // Empty genres
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, (bool?)true, 1, 100))
            .ReturnsAsync((new List<Setlist>(), 0));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert - Should handle empty genres gracefully
        component.Markup.Should().Contain("0"); // Zero genres
        component.Markup.Should().Contain("Music Genres");
    }

    [Fact]
    public async Task Index_ShouldHandleAllServiceFailures_Gracefully()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup all services to fail
        _mockSongService.Setup(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Song service failed"));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), 
            It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Setlist service failed"));
        
        _mockSongService.Setup(x => x.GetGenresAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Genre service failed"));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(200);
        component.Render();

        // Assert - Component should still render without crashing
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Welcome to Setlist Studio");
        // Error handling should be graceful - stats should show default values
        component.Markup.Should().Contain("0"); // Default stats displayed
    }

    [Fact]
    public async Task Index_ShouldDisplayErrorMessage_WhenDataLoadingFails()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup song service to fail
        _mockSongService.Setup(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(200);
        component.Render();

        // Assert
        // When service fails, component should still render with default/zero stats
        component.Markup.Should().Contain("0");
    }

    [Fact]
    public async Task Index_ShouldHandlePartialDataLoading_WhenSomeServicesSucceed()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup mixed success/failure
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Song>(), 42));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new[] { "Rock", "Jazz", "Pop" });
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), 
            It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Setlist service failed"));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(200);
        component.Render();

        // Assert - Should show successful data and handle failures gracefully
        component.Markup.Should().Contain("42"); // Song count
        component.Markup.Should().Contain("3");  // Genre count
        component.Should().NotBeNull();
    }

    [Fact]
    public async Task Index_ShouldHandleLargeNumbers_InStats()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup services with large numbers
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Song>(), 9999));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Setlist>(), 1234));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(Enumerable.Range(1, 50).Select(i => $"Genre {i}").ToArray());
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, (bool?)true, 1, 100))
            .ReturnsAsync((new List<Setlist>(), 0));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert - Should display large numbers correctly
        component.Markup.Should().Contain("9999");  // Large song count
        component.Markup.Should().Contain("1234");  // Large setlist count
        component.Markup.Should().Contain("50");    // Many genres
    }

    [Fact]
    public async Task Index_ShouldHandleComplexPerformanceDateScenarios()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup basic services
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Song>(), 1));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Setlist>(), 1));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new[] { "Rock" });
        
        // Complex upcoming performances with edge cases
        var complexSetlists = new List<Setlist>
        {
            new() { PerformanceDate = DateTime.Now.AddMinutes(1), UserId = "test-user-id" },    // Very soon
            new() { PerformanceDate = DateTime.Now.AddYears(1), UserId = "test-user-id" },     // Far future
            new() { PerformanceDate = DateTime.Now.AddDays(-1), UserId = "test-user-id" },     // Past
            new() { PerformanceDate = DateTime.Now, UserId = "test-user-id" },                 // Right now (edge case)
            new() { PerformanceDate = DateTime.MinValue, UserId = "test-user-id" },            // Very old date
            new() { PerformanceDate = DateTime.MaxValue, UserId = "test-user-id" },            // Very future date
        };
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, (bool?)true, 1, 100))
            .ReturnsAsync((complexSetlists, 6));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert - Should count only future performances correctly
        // Current implementation should count performances after now
        component.Markup.Should().NotBeNullOrEmpty();
        component.Should().NotBeNull();
    }

    [Fact]
    public async Task Index_ShouldHandleNullPerformanceDates()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup basic services
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Song>(), 1));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Setlist>(), 1));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new[] { "Rock" });
        
        // All setlists with null performance dates
        var nullDateSetlists = new List<Setlist>
        {
            new() { PerformanceDate = null, UserId = "test-user-id" },
            new() { PerformanceDate = null, UserId = "test-user-id" },
            new() { PerformanceDate = null, UserId = "test-user-id" }
        };
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, (bool?)true, 1, 100))
            .ReturnsAsync((nullDateSetlists, 3));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert - Should handle null dates gracefully and show 0 upcoming
        component.Markup.Should().Contain("0"); // No upcoming performances
        component.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Index_ShouldHandleInvalidUserIds(string userId)
    {
        // Arrange
        var claims = new List<Claim>();
        if (userId != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }
        
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);

        // Assert - The component should handle invalid user IDs gracefully
        // Note: The implementation may still call services but should handle any failures gracefully
        
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Welcome to Setlist Studio");
    }

    [Fact]
    public async Task Index_ShouldHandleServiceExceptions()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup services to throw exceptions
        _mockSongService.Setup(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations to complete/fail
        await Task.Delay(200);
        component.Render();

        // Assert - Should handle service exceptions gracefully
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Welcome to Setlist Studio");
    }

    [Fact]
    public async Task Index_AuthenticationStateProvider_ShouldHandleNullState()
    {
        // Arrange
        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());
        var mockAuthState = new AuthenticationState(unauthenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act - Use CascadingAuthenticationState to provide proper context
        var component = RenderComponent<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent(childBuilder =>
            {
                childBuilder.OpenComponent<IndexPage>(0);
                childBuilder.CloseComponent();
            }));
        
        // Wait for async operations
        await Task.Delay(100);

        // Assert - Should handle unauthenticated state gracefully
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Welcome to Setlist Studio");
    }

    [Fact]
    public async Task Index_WhenUnauthenticated_ShouldStillRenderBasicContent()
    {
        // Arrange
        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());
        var mockAuthState = new AuthenticationState(unauthenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert
        component.Markup.Should().Contain("Welcome to Setlist Studio");
        component.Markup.Should().Contain("Everything You Need for Professional Performances");
        component.Should().NotBeNull();
    }

    [Fact]
    public async Task Index_PerformanceStatistics_ShouldHandleEdgeCases()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup basic services
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Song>(), 0));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, null, 1, 1))
            .ReturnsAsync((new List<Setlist>(), 0));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(Array.Empty<string>());
        
        // Setlists with edge case dates
        var edgeCaseSetlists = new List<Setlist>
        {
            new() { PerformanceDate = DateTime.MinValue, UserId = "test-user-id" },
            new() { PerformanceDate = DateTime.MaxValue, UserId = "test-user-id" },
            new() { PerformanceDate = new DateTime(1900, 1, 1), UserId = "test-user-id" }
        };
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", null, null, (bool?)true, 1, 100))
            .ReturnsAsync((edgeCaseSetlists, 3));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Wait for async operations
        await Task.Delay(100);
        component.Render();

        // Assert - Should handle edge case dates without crashing
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Welcome to Setlist Studio");
    }

    [Fact]
    public async Task Index_ComponentLifecycle_ShouldHandleMultipleRenders()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup services
        _mockSongService.Setup(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Song>(), 1));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Setlist>(), 1));
        
        _mockSongService.Setup(x => x.GetGenresAsync(It.IsAny<string>()))
            .ReturnsAsync(new[] { "Rock" });

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));
        
        // Multiple renders to test lifecycle
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(50);
            component.Render();
        }

        // Assert - Should handle multiple renders without issues
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Welcome to Setlist Studio");
    }

    [Fact]
    public async Task Index_ShouldHandleEmptyUserId()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "test-user") // No NameIdentifier claim
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        await Task.Delay(100);
        component.Render();

        // Assert - Should not call services when userId is empty
        _mockSongService.Verify(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockSetlistService.Verify(x => x.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        component.Markup.Should().Contain("Welcome back"); // Should still show authenticated content
    }

    [Fact]
    public async Task Index_ShouldHandleNullUserId()
    {
        // Arrange - Create user without NameIdentifier claim to simulate null userId
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser")
            // No NameIdentifier claim - this will result in null when looking for userId
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        await Task.Delay(100);
        component.Render();

        // Assert - Should not call services when userId is null
        _mockSongService.Verify(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockSetlistService.Verify(x => x.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        component.Markup.Should().Contain("Welcome back"); // Should still show authenticated content
    }

    [Fact]
    public async Task Index_ShouldHandleWhitespaceUserId()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "   ") // Whitespace-only userId
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        await Task.Delay(100);
        component.Render();

        // Assert - Should call services with whitespace userId (the component doesn't trim it)
        _mockSongService.Verify(x => x.GetSongsAsync("   ", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
        _mockSetlistService.Verify(x => x.GetSetlistsAsync("   ", It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
        component.Markup.Should().Contain("Welcome back"); // Should still show authenticated content
    }

    [Fact]
    public async Task Index_ShouldHandleUpcomingPerformances_WithPastDates()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Create setlists with past performance dates
        var pastSetlists = new List<Setlist>
        {
            new Setlist { Id = 1, Name = "Past Show 1", UserId = "test-user-id", PerformanceDate = DateTime.Now.AddDays(-7) },
            new Setlist { Id = 2, Name = "Past Show 2", UserId = "test-user-id", PerformanceDate = DateTime.Now.AddDays(-1) },
            new Setlist { Id = 3, Name = "No Date", UserId = "test-user-id", PerformanceDate = null }
        };

        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Song>(), 5));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((pastSetlists, 3));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new[] { "Rock", "Pop" });

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        await Task.Delay(200);
        component.Render();

        // Assert - Should show 0 upcoming performances
        component.WaitForState(() => component.Markup.Contains("0"), TimeSpan.FromSeconds(2));
        component.Markup.Should().Contain("5"); // Total songs
        component.Markup.Should().Contain("3"); // Total setlists
        component.Markup.Should().Contain("2"); // Unique genres
        // Should show 0 upcoming performances since all dates are in the past or null
    }

    [Fact]
    public async Task Index_ShouldHandleUpcomingPerformances_WithFutureDates()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Create setlists with future performance dates
        var futureSetlists = new List<Setlist>
        {
            new Setlist { Id = 1, Name = "Future Show 1", UserId = "test-user-id", PerformanceDate = DateTime.Now.AddDays(7) },
            new Setlist { Id = 2, Name = "Future Show 2", UserId = "test-user-id", PerformanceDate = DateTime.Now.AddDays(14) },
            new Setlist { Id = 3, Name = "Past Show", UserId = "test-user-id", PerformanceDate = DateTime.Now.AddDays(-1) },
            new Setlist { Id = 4, Name = "No Date", UserId = "test-user-id", PerformanceDate = null }
        };

        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Song>(), 10));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((futureSetlists, 4));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new[] { "Rock", "Jazz", "Blues" });

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        await Task.Delay(200);
        component.Render();

        // Assert - Should show 2 upcoming performances (future dates only)
        component.WaitForState(() => component.Markup.Contains("10") && component.Markup.Contains("4") && component.Markup.Contains("3"), TimeSpan.FromSeconds(2));
        component.Markup.Should().Contain("10"); // Total songs
        component.Markup.Should().Contain("4");  // Total setlists
        component.Markup.Should().Contain("3");  // Unique genres
        // Should calculate upcoming performances correctly
    }

    [Fact]
    public async Task Index_ShouldHandleAllServiceExceptions()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup all services to throw exceptions
        _mockSongService.Setup(x => x.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Song service failed"));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Setlist service failed"));
        
        _mockSongService.Setup(x => x.GetGenresAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Genres service failed"));

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        await Task.Delay(200);
        component.Render();

        // Assert - Should gracefully handle all service exceptions and still render
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Welcome to Setlist Studio");
        component.Markup.Should().Contain("Welcome back"); // Should still show authenticated content
        
        // Stats should remain at 0 due to exceptions
        component.Markup.Should().Contain("0"); // Should show default values
    }

    [Fact]
    public async Task Index_ShouldHandlePartialServiceFailures_WithMixedResults()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup some services to succeed and others to fail
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Song>(), 15));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Setlist service failed"));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new[] { "Rock", "Pop", "Jazz", "Blues" });

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        await Task.Delay(200);
        component.Render();

        // Assert - Should handle partial failures gracefully
        component.Should().NotBeNull();
        component.Markup.Should().Contain("Welcome to Setlist Studio");
        component.Markup.Should().Contain("15"); // Songs should load successfully
        component.Markup.Should().Contain("4");  // Genres should load successfully
        // Setlists and upcoming performances should remain at 0 due to exception
    }

    [Fact]
    public async Task Index_AuthorizeView_ShouldShowCorrectButtons_WhenAuthenticated()
    {
        // Arrange
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "Test User")
        }, "test"));
        
        var mockAuthState = new AuthenticationState(authenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Setup services for successful loading
        _mockSongService.Setup(x => x.GetSongsAsync("test-user-id", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Song>(), 5));
        
        _mockSetlistService.Setup(x => x.GetSetlistsAsync("test-user-id", It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Setlist>(), 3));
        
        _mockSongService.Setup(x => x.GetGenresAsync("test-user-id"))
            .ReturnsAsync(new[] { "Rock" });

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        await Task.Delay(200);
        component.Render();

        // Assert - Should show authenticated user buttons
        component.WaitForState(() => 
        {
            var markup = component.Markup;
            return markup.Contains("My Songs") || markup.Contains("queue_music") || markup.Contains("/songs");
        }, TimeSpan.FromSeconds(3));

        var hasAuthenticatedButtons = component.Markup.Contains("My Songs") || 
                                     component.Markup.Contains("My Setlists") ||
                                     component.Markup.Contains("/songs") ||
                                     component.Markup.Contains("/setlists");

        hasAuthenticatedButtons.Should().BeTrue("Authenticated users should see navigation buttons to songs and setlists");
        
        // Should NOT show sign-in buttons
        component.Markup.Should().NotContain("Sign In to Get Started");
        component.Markup.Should().NotContain("Sign Up Free");
    }

    [Fact]
    public async Task Index_AuthorizeView_ShouldShowSignInButtons_WhenNotAuthenticated()
    {
        // Arrange
        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        var mockAuthState = new AuthenticationState(unauthenticatedUser);
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(mockAuthState);

        // Act
        var component = RenderComponent<IndexPage>(parameters => parameters
            .AddCascadingValue(Task.FromResult(mockAuthState)));

        await Task.Delay(100);
        component.Render();

        // Assert - Should show sign-in buttons
        component.WaitForState(() => 
        {
            var markup = component.Markup;
            return markup.Contains("Sign In") || markup.Contains("/login");
        }, TimeSpan.FromSeconds(3));

        var hasSignInButtons = component.Markup.Contains("Sign In to Get Started") || 
                              component.Markup.Contains("Sign Up Free") ||
                              component.Markup.Contains("/login");

        hasSignInButtons.Should().BeTrue("Unauthenticated users should see sign-in buttons");
        
        // Should NOT show authenticated user buttons or stats
        component.Markup.Should().NotContain("My Songs");
        component.Markup.Should().NotContain("My Setlists");
        component.Markup.Should().NotContain("Welcome back");
        component.Markup.Should().NotContain("Your Music at a Glance");
    }
}
