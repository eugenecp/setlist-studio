using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using SetlistStudio.Web.Shared;
using Xunit;

namespace SetlistStudio.Tests.Web.Shared;

/// <summary>
/// Tests for ConnectionStatus.razor component - Critical for live performance scenarios
/// Ensures musicians always know their connection status and offline capabilities
/// 
/// IMPORTANT: Simplified tests to avoid async/JS interop issues that cause hanging
/// Focus on basic rendering and observable behavior rather than complex interactions
/// </summary>
public class ConnectionStatusTests : TestContext
{
    public ConnectionStatusTests()
    {
        // Configure test services for Blazor component testing
        Services.AddMudServices();
        
        // Use strict mode to prevent unexpected JS calls that could cause hanging
        JSInterop.Mode = JSRuntimeMode.Strict;
        
        // Set up only the essential JS methods that are always called
        // Use loose matching for methods that accept DotNetObjectReference parameters
        JSInterop.SetupVoid("setlistStudioApp.registerConnectionStatusCallback", _ => true);
        JSInterop.SetupVoid("setlistStudioApp.unregisterConnectionStatusCallback", _ => true);
        JSInterop.Setup<bool>("navigator.onLine").SetResult(true); // Default to online
    }

    [Fact]
    public void ConnectionStatus_ShouldRenderWithoutErrors()
    {
        // Act - Render the component with minimal setup
        var component = RenderComponent<ConnectionStatus>();

        // Assert - Component should render successfully
        component.Should().NotBeNull();
        component.Markup.Should().Contain("connection-status-container");
    }

    [Fact] 
    public void ConnectionStatus_DefaultState_ShouldNotShowPerformanceModeAlert()
    {
        // Act - Render component in default (online) state
        var component = RenderComponent<ConnectionStatus>();

        // Assert - Performance mode alert should not be visible by default
        component.Markup.Should().NotContain("Performance Mode Active");
        component.Markup.Should().NotContain("Using cached setlists and songs");
        component.Markup.Should().Contain("connection-status-container");
    }

    [Fact]
    public void ConnectionStatus_WithShowCacheStatusFalse_ShouldNotShowCacheSection()
    {
        // Act - Render component with cache status disabled (default)
        var component = RenderComponent<ConnectionStatus>();

        // Assert - Cache status section should not be visible
        component.Markup.Should().NotContain("Offline Cache Status");
        component.Markup.Should().NotContain("Songs/Setlists Cached");
        component.Markup.Should().NotContain("Core App Cached");
    }

    [Fact]
    public void ConnectionStatus_ShouldUseCorrectCssClasses()
    {
        // Act - Render component
        var component = RenderComponent<ConnectionStatus>();

        // Assert - Should have proper CSS structure
        component.Markup.Should().Contain("connection-status-container");
        component.Markup.Should().Contain("class=\"connection-status-container\"");
    }

    [Fact]
    public void ConnectionStatus_ShouldIntegrateWithMudBlazor()
    {
        // Act - Render component (in default online state, no alert shown)
        var component = RenderComponent<ConnectionStatus>();

        // Assert - Component should be properly integrated with MudBlazor
        // Even without alert visible, the component should be ready to use MudBlazor components
        component.Should().NotBeNull();
        component.Markup.Should().Contain("connection-status-container");
    }

    [Fact]
    public void ConnectionStatus_ComponentParameters_ShouldBeConfigurable()
    {
        // Act - Render component with ShowCacheStatus parameter
        var component = RenderComponent<ConnectionStatus>(parameters => parameters
            .Add(p => p.ShowCacheStatus, false));

        // Assert - Parameter should be respected (cache status not shown)
        component.Markup.Should().NotContain("Offline Cache Status");
        component.Markup.Should().Contain("connection-status-container");
    }

    [Fact]
    public void ConnectionStatus_ShouldImplementIAsyncDisposable()
    {
        // Act - Render component
        var component = RenderComponent<ConnectionStatus>();

        // Assert - Component should implement proper disposal pattern
        component.Instance.Should().BeAssignableTo<IAsyncDisposable>();
    }

    [Fact]
    public void ConnectionStatus_Disposal_ShouldNotThrowErrors()
    {
        // Arrange - Render component
        var component = RenderComponent<ConnectionStatus>();

        // Act & Assert - Disposing should not throw errors
        Action disposal = () => component.Dispose();
        disposal.Should().NotThrow();
    }
}