using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using SetlistStudio.Web.Shared;
using MudBlazor.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components.Web;

namespace SetlistStudio.Tests.Shared;

public class EmptyLayoutTests : TestContext
{
    public EmptyLayoutTests()
    {
        // Register basic services needed for layout components
        Services.AddLogging();
        Services.AddMudServices();
        
        // Setup JSInterop for MudBlazor components
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void EmptyLayout_ShouldRenderWithoutErrors()
    {
        // Arrange & Act
        var component = RenderComponent<EmptyLayout>();

        // Assert
        component.Should().NotBeNull("EmptyLayout component should render successfully");
        component.Markup.Should().NotBeNullOrEmpty("EmptyLayout component should have markup");
    }

    [Fact]
    public void EmptyLayout_ShouldHaveBasicStructure()
    {
        // Arrange & Act
        var component = RenderComponent<EmptyLayout>();

        // Assert
        component.Markup.Should().Contain("page", "EmptyLayout should have page div");
    }

    [Fact]
    public void EmptyLayout_ShouldBeMinimalLayout()
    {
        // Arrange & Act
        var component = RenderComponent<EmptyLayout>();

        // Assert
        // EmptyLayout should be minimal without navigation or complex structure
        component.Markup.Should().NotContain("nav", "EmptyLayout should not contain navigation");
        component.Markup.Should().NotContain("sidebar", "EmptyLayout should not contain sidebar");
    }
}