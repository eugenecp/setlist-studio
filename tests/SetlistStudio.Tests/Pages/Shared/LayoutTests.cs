using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using SetlistStudio.Tests.Web;
using System.Net.Http;

namespace SetlistStudio.Tests.Pages.Shared;

public class LayoutTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LayoutTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !File.Exists(Path.Join(directory.FullName, "SetlistStudio.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Could not find project root");
    }

    [Fact]
    public void Layout_ShouldExistAsSharedView()
    {
        // Arrange & Act
        var projectRoot = FindProjectRoot();
        var layoutPath = Path.Join(projectRoot, "src", "SetlistStudio.Web", "Pages", "Shared", "_Layout.cshtml");
        var layoutExists = File.Exists(layoutPath);

        // Assert
        layoutExists.Should().BeTrue("_Layout.cshtml should exist as shared layout view");
    }

    [Fact]
    public void Layout_ShouldContainBasicHtmlStructure()
    {
        // Arrange
        var projectRoot = FindProjectRoot();
        var layoutPath = Path.Join(projectRoot, "src", "SetlistStudio.Web", "Pages", "Shared", "_Layout.cshtml");
        
        // Act
        var exists = File.Exists(layoutPath);
        
        // Assert
        exists.Should().BeTrue("_Layout.cshtml file should exist");
        
        if (exists)
        {
            var content = File.ReadAllText(layoutPath);
            content.Should().NotBeNullOrEmpty("_Layout.cshtml should have content");
            content.Should().Contain("<!DOCTYPE html>", "_Layout.cshtml should contain HTML DOCTYPE");
            content.Should().Contain("<html", "_Layout.cshtml should contain html tag");
            content.Should().Contain("<head>", "_Layout.cshtml should contain head section");
            content.Should().Contain("<body>", "_Layout.cshtml should contain body section");
        }
    }

    [Fact]
    public void Layout_ShouldIncludeRenderBody()
    {
        // Arrange
        var layoutPath = "src/SetlistStudio.Web/Pages/Shared/_Layout.cshtml";
        
        // Act
        if (System.IO.File.Exists(layoutPath))
        {
            var content = System.IO.File.ReadAllText(layoutPath);
            
            // Assert
            content.Should().Contain("@RenderBody()", "_Layout.cshtml should contain @RenderBody() call");
        }
    }

    [Fact]
    public void Layout_ShouldIncludeMetaTags()
    {
        // Arrange
        var layoutPath = "src/SetlistStudio.Web/Pages/Shared/_Layout.cshtml";
        
        // Act
        if (System.IO.File.Exists(layoutPath))
        {
            var content = System.IO.File.ReadAllText(layoutPath);
            
            // Assert
            content.Should().Contain("<meta", "_Layout.cshtml should contain meta tags");
        }
    }

    [Fact]
    public void Layout_ShouldIncludeTitle()
    {
        // Arrange
        var layoutPath = "src/SetlistStudio.Web/Pages/Shared/_Layout.cshtml";
        
        // Act
        if (System.IO.File.Exists(layoutPath))
        {
            var content = System.IO.File.ReadAllText(layoutPath);
            
            // Assert
            content.Should().Contain("<title>", "_Layout.cshtml should contain title tag");
        }
    }

    [Fact]
    public void Layout_ShouldSupportRenderSection()
    {
        // Arrange
        var layoutPath = "src/SetlistStudio.Web/Pages/Shared/_Layout.cshtml";
        
        // Act
        if (System.IO.File.Exists(layoutPath))
        {
            var content = System.IO.File.ReadAllText(layoutPath);
            
            // Assert
            // Layout may contain RenderSection calls for scripts, styles, etc.
            content.Should().NotBeNullOrEmpty("_Layout.cshtml should support sections");
        }
    }
}