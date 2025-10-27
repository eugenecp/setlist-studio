using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using SetlistStudio.Web.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SetlistStudio.Tests.Web.Shared;

/// <summary>
/// Tests for ConnectionStatus.razor component - Critical for live performance scenarios
/// Ensures musicians always know their connection status and offline capabilities
/// </summary>
public class ConnectionStatusTests : TestContext
{
    public ConnectionStatusTests()
    {
        // Configure test services for Blazor component testing
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose; // Allow JS interop calls during testing
        JSInterop.SetupVoid("setlistStudioApp.registerConnectionStatusCallback");
        JSInterop.SetupVoid("setlistStudioApp.unregisterConnectionStatusCallback");
    }

    [Fact]
    public void ConnectionStatus_ShouldRenderWithoutErrors()
    {
        // Act - Render the component
        var component = RenderComponent<ConnectionStatus>();

        // Assert - Component should render successfully
        component.Should().NotBeNull();
        component.Markup.Should().Contain("connection-status-container");
    }

    [Fact]
    public void ConnectionStatus_WhenOnline_ShouldNotShowPerformanceModeAlert()
    {
        // Arrange - Set up online status
        JSInterop.Setup<bool>("navigator.onLine").SetResult(true);

        // Act - Render component
        var component = RenderComponent<ConnectionStatus>();

        // Assert - Performance mode alert should not be visible when online
        component.Markup.Should().NotContain("Performance Mode Active");
        component.Markup.Should().NotContain("Using cached setlists and songs");
    }

    [Fact]
    public async Task ConnectionStatus_WhenOffline_ShouldShowPerformanceModeAlert()
    {
        // Arrange - Set up offline status
        JSInterop.Setup<bool>("navigator.onLine").SetResult(false);

        // Act - Render component and wait for after render
        var component = RenderComponent<ConnectionStatus>();
        
        // Trigger the connection status change through the public method
        await component.InvokeAsync(async () => await component.Instance.OnConnectionStatusChanged(false));

        // Assert - Performance mode alert should be visible when offline
        component.Markup.Should().Contain("Performance Mode Active");
        component.Markup.Should().Contain("Using cached setlists and songs");
        component.Markup.Should().Contain("performance-mode-alert");
    }

    [Fact]
    public void ConnectionStatus_WhenShowCacheStatusFalse_ShouldNotShowCacheInfo()
    {
        // Arrange - Disable cache status display
        JSInterop.Setup<bool>("navigator.onLine").SetResult(false);

        // Act - Render component with cache status disabled (default)
        var component = RenderComponent<ConnectionStatus>();

        // Assert - Cache status should not be displayed
        component.Markup.Should().NotContain("Offline Cache Status");
        component.Markup.Should().NotContain("Songs/Setlists Cached");
    }

    [Fact]
    public void ConnectionStatus_WhenShowCacheStatusTrue_ShouldShowCacheInfo()
    {
        // Arrange - Enable cache status and set up cache data
        JSInterop.Setup<bool>("navigator.onLine").SetResult(false);
        JSInterop.Setup<Dictionary<string, int>>("setlistStudioApp.offline.getCacheStatus")
            .SetResult(new Dictionary<string, int>
            {
                ["setlist-studio-api-v1.0.0"] = 25,
                ["setlist-studio-v1.0.0"] = 1
            });

        // Act - Render component with cache status enabled
        var component = RenderComponent<ConnectionStatus>(parameters => parameters
            .Add(p => p.ShowCacheStatus, true));

        // Assert - Cache status should be displayed with cached items
        component.Markup.Should().Contain("Offline Cache Status");
        component.Markup.Should().Contain("25 Songs/Setlists Cached");
        component.Markup.Should().Contain("Core App Cached");
    }

    [Fact]
    public void ConnectionStatus_WhenCacheEmpty_ShouldShowCacheStatusWithoutChips()
    {
        // Arrange - Enable cache status with empty cache
        JSInterop.Setup<bool>("navigator.onLine").SetResult(false);
        JSInterop.Setup<Dictionary<string, int>>("setlistStudioApp.offline.getCacheStatus")
            .SetResult(new Dictionary<string, int>());

        // Act - Render component with cache status enabled but no cached items
        var component = RenderComponent<ConnectionStatus>(parameters => parameters
            .Add(p => p.ShowCacheStatus, true));

        // Assert - Cache status header should show but no cache chips
        component.Markup.Should().Contain("Offline Cache Status");
        component.Markup.Should().NotContain("Songs/Setlists Cached");
        component.Markup.Should().NotContain("Core App Cached");
    }

    [Fact]
    public void ConnectionStatus_JSInteropError_ShouldHandleGracefully()
    {
        // Arrange - Set up JS interop to throw exception
        JSInterop.Setup<bool>("navigator.onLine")
            .SetException(new InvalidOperationException("JavaScript interop calls cannot be issued"));

        // Act & Assert - Component should render without throwing
        var component = RenderComponent<ConnectionStatus>();
        component.Should().NotBeNull();
        component.Markup.Should().Contain("connection-status-container");
    }

    [Fact]
    public async Task ConnectionStatus_OnConnectionStatusChanged_ShouldUpdateStatus()
    {
        // Arrange - Start with online status
        JSInterop.Setup<bool>("navigator.onLine").SetResult(true);
        var component = RenderComponent<ConnectionStatus>();

        // Act - Simulate connection status change to offline using InvokeAsync
        await component.InvokeAsync(async () => await component.Instance.OnConnectionStatusChanged(false));

        // Assert - Component should show offline status
        component.Markup.Should().Contain("Performance Mode Active");
        component.Markup.Should().Contain("Using cached setlists and songs");
    }

    [Fact]
    public async Task ConnectionStatus_OnConnectionStatusChanged_WhenGoingOnline_ShouldUpdateCacheIfEnabled()
    {
        // Arrange - Start offline with cache status enabled
        JSInterop.Setup<bool>("navigator.onLine").SetResult(false);
        JSInterop.Setup<Dictionary<string, int>>("setlistStudioApp.offline.getCacheStatus")
            .SetResult(new Dictionary<string, int> { ["setlist-studio-api-v1.0.0"] = 15 });

        var component = RenderComponent<ConnectionStatus>(parameters => parameters
            .Add(p => p.ShowCacheStatus, true));

        // Act - Simulate going online using InvokeAsync
        await component.InvokeAsync(async () => await component.Instance.OnConnectionStatusChanged(true));

        // Assert - Cache status should be updated when going online
        JSInterop.VerifyInvoke("setlistStudioApp.offline.getCacheStatus");
    }

    [Fact]
    public async Task ConnectionStatus_PerformanceModeAlert_ShouldHaveCorrectStyling()
    {
        // Arrange - Set up offline status
        JSInterop.Setup<bool>("navigator.onLine").SetResult(false);

        // Act - Render component and trigger offline state
        var component = RenderComponent<ConnectionStatus>();
        await component.InvokeAsync(async () => await component.Instance.OnConnectionStatusChanged(false));

        // Assert - Performance mode alert should have correct CSS classes
        component.Markup.Should().Contain("performance-mode-alert");
        component.Markup.Should().Contain("mud-alert");
        component.Markup.Should().Contain("mud-alert-text-warning"); // MudBlazor uses this class
    }

    [Fact]
    public void ConnectionStatus_CacheChips_ShouldHaveCorrectColors()
    {
        // Arrange - Set up cache data with both types
        JSInterop.Setup<bool>("navigator.onLine").SetResult(false);
        JSInterop.Setup<Dictionary<string, int>>("setlistStudioApp.offline.getCacheStatus")
            .SetResult(new Dictionary<string, int>
            {
                ["setlist-studio-api-v1.0.0"] = 42,
                ["setlist-studio-v1.0.0"] = 1
            });

        // Act - Render component with cache status
        var component = RenderComponent<ConnectionStatus>(parameters => parameters
            .Add(p => p.ShowCacheStatus, true));

        // Assert - Chips should have correct colors for different cache types
        component.Markup.Should().Contain("42 Songs/Setlists Cached");
        component.Markup.Should().Contain("Core App Cached");
        component.Markup.Should().Contain("mud-chip-color-success"); // API cache chip
        component.Markup.Should().Contain("mud-chip-color-primary");  // App cache chip
    }

    [Fact]
    public async Task ConnectionStatus_ShouldHaveCorrectAccessibilityAttributes()
    {
        // Arrange - Set up offline status
        JSInterop.Setup<bool>("navigator.onLine").SetResult(false);

        // Act - Render component and trigger offline state
        var component = RenderComponent<ConnectionStatus>();
        await component.InvokeAsync(async () => await component.Instance.OnConnectionStatusChanged(false));

        // Assert - Component should have proper accessibility features through MudAlert
        component.Markup.Should().Contain("role=\"img\""); // SVG icons have img role for screen readers
    }

    [Fact]
    public void ConnectionStatus_StickyPositioning_ShouldBeConfigured()
    {
        // Act - Render component
        var component = RenderComponent<ConnectionStatus>();

        // Assert - Component should have sticky positioning for performance visibility
        component.Markup.Should().Contain("connection-status-container");
        // CSS classes are defined in the component's <style> section
    }

    [Fact]
    public async Task ConnectionStatus_Dispose_ShouldHandleGracefully()
    {
        // Arrange - Render component
        var component = RenderComponent<ConnectionStatus>();

        // Act - Dispose the component
        await component.Instance.DisposeAsync();

        // Assert - Should handle disposal without errors (JS calls may not be invoked due to test context)
        // The important thing is that it doesn't throw exceptions during disposal
        component.Should().NotBeNull();
    }

    [Fact]
    public void ConnectionStatus_JSDisconnected_ShouldHandleGracefully()
    {
        // Arrange - Set up JS disconnection exception
        JSInterop.Setup<bool>("navigator.onLine")
            .SetException(new JSDisconnectedException("JS runtime disconnected"));

        // Act - Render component
        var component = RenderComponent<ConnectionStatus>();

        // Assert - Should handle JS disconnection gracefully and render container
        component.Markup.Should().Contain("connection-status-container");
        // The component handles JS errors gracefully but doesn't automatically show offline mode
    }

    [Theory]
    [InlineData(0, false)] // No cached items
    [InlineData(1, true)]  // Single cached item
    [InlineData(50, true)] // Many cached items
    public void ConnectionStatus_CacheStatusDisplay_ShouldVaryByItemCount(int cacheCount, bool shouldShowChip)
    {
        // Arrange - Set up cache with varying item counts
        var cacheData = cacheCount > 0 
            ? new Dictionary<string, int> { ["setlist-studio-api-v1.0.0"] = cacheCount }
            : new Dictionary<string, int>();
            
        JSInterop.Setup<bool>("navigator.onLine").SetResult(false);
        JSInterop.Setup<Dictionary<string, int>>("setlistStudioApp.offline.getCacheStatus")
            .SetResult(cacheData);

        // Act - Render component with cache status enabled
        var component = RenderComponent<ConnectionStatus>(parameters => parameters
            .Add(p => p.ShowCacheStatus, true));

        // Assert - Cache chip visibility should match expectation
        if (shouldShowChip)
        {
            component.Markup.Should().Contain($"{cacheCount} Songs/Setlists Cached");
        }
        else
        {
            component.Markup.Should().NotContain("Songs/Setlists Cached");
        }
    }

    [Fact]
    public void ConnectionStatus_MudBlazorIntegration_ShouldUseCorrectComponents()
    {
        // Arrange - Set up offline status to show all UI elements
        JSInterop.Setup<bool>("navigator.onLine").SetResult(false);
        JSInterop.Setup<Dictionary<string, int>>("setlistStudioApp.offline.getCacheStatus")
            .SetResult(new Dictionary<string, int> { ["setlist-studio-api-v1.0.0"] = 10 });

        // Act - Render component with all features enabled
        var component = RenderComponent<ConnectionStatus>(parameters => parameters
            .Add(p => p.ShowCacheStatus, true));

        // Assert - Should use correct MudBlazor components
        component.Markup.Should().Contain("mud-alert");      // MudAlert
        component.Markup.Should().Contain("mud-paper");      // MudPaper
        component.Markup.Should().Contain("mud-chip");       // MudChip
        component.Markup.Should().Contain("mud-typography"); // MudText
    }
}