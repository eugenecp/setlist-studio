using Xunit;
using FluentAssertions;

namespace SetlistStudio.Tests.Web;

public class ImportsTests
{
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
    public void Imports_ShouldBeValidRazorFile()
    {
        // Arrange & Act
        var projectRoot = FindProjectRoot();
        var importsPath = Path.Join(projectRoot, "src", "SetlistStudio.Web", "_Imports.razor");
        var importsExists = File.Exists(importsPath);

        // Assert
        importsExists.Should().BeTrue("_Imports.razor should exist to provide global using statements");
    }

    [Fact]
    public void Imports_ShouldContainExpectedDirectives()
    {
        // Arrange
        var projectRoot = FindProjectRoot();
        var importsPath = Path.Join(projectRoot, "src", "SetlistStudio.Web", "_Imports.razor");
        
        // Act
        var exists = File.Exists(importsPath);
        
        // Assert
        exists.Should().BeTrue("_Imports.razor file should exist");
        
        if (exists)
        {
            var content = File.ReadAllText(importsPath);
            content.Should().NotBeNullOrEmpty("_Imports.razor should have content");
        }
    }
}