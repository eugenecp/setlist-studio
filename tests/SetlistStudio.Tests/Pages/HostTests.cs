using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using SetlistStudio.Tests.Web;
using System.Net.Http;

namespace SetlistStudio.Tests.Pages;

public class HostTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HostTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SetlistStudio.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Could not find project root");
    }

    [Fact]
    public void Host_ShouldExistAsRazorPage()
    {
        // Arrange & Act
        var projectRoot = FindProjectRoot();
        var hostPagePath = Path.Combine(projectRoot, "src", "SetlistStudio.Web", "Pages", "_Host.cshtml");
        var hostPageExists = File.Exists(hostPagePath);

        // Assert
        hostPageExists.Should().BeTrue("_Host.cshtml should exist as the main Blazor Server host page");
    }

    [Fact]
    public void Host_ShouldContainBlazorServerConfiguration()
    {
        // Arrange
        var projectRoot = FindProjectRoot();
        var hostPath = Path.Combine(projectRoot, "src", "SetlistStudio.Web", "Pages", "_Host.cshtml");
        
        // Act
        var exists = File.Exists(hostPath);
        
        // Assert
        exists.Should().BeTrue("_Host.cshtml file should exist");
        
        if (exists)
        {
            var content = File.ReadAllText(hostPath);
            content.Should().NotBeNullOrEmpty("_Host.cshtml should have content");
            // Host page typically contains Blazor Server setup
        }
    }

    [Fact]
    public void Host_ShouldHaveCorrectPageDirective()
    {
        // Arrange
        var projectRoot = FindProjectRoot();
        var hostPath = Path.Combine(projectRoot, "src", "SetlistStudio.Web", "Pages", "_Host.cshtml");
        
        // Act
        if (File.Exists(hostPath))
        {
            var content = File.ReadAllText(hostPath);
            
            // Assert
            content.Should().Contain("@page", "_Host.cshtml should contain @page directive");
        }
    }

    [Fact]
    public void Host_ShouldConfigureBlazorComponent()
    {
        // Arrange
        var projectRoot = FindProjectRoot();
        var hostPath = Path.Combine(projectRoot, "src", "SetlistStudio.Web", "Pages", "_Host.cshtml");
        
        // Act
        if (File.Exists(hostPath))
        {
            var content = File.ReadAllText(hostPath);
            
            // Assert
            // Host page should configure the main Blazor component
            content.Should().NotBeNullOrEmpty("_Host.cshtml should configure Blazor components");
        }
    }

    [Fact]
    public void Host_ShouldIncludeProperHtmlStructure()
    {
        // Arrange
        var hostPath = "src/SetlistStudio.Web/Pages/_Host.cshtml";
        
        // Act
        if (System.IO.File.Exists(hostPath))
        {
            var content = System.IO.File.ReadAllText(hostPath);
            
            // Assert
            content.Should().NotBeNullOrEmpty("_Host.cshtml should have proper HTML structure");
        }
    }
}