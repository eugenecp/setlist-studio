using FluentAssertions;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Interfaces;

/// <summary>
/// Tests for ISongService interface contract validation
/// Ensures interface contract is properly defined and consistent
/// </summary>
public class ISongServiceTests
{
    [Fact]
    public void ISongService_ShouldHaveCorrectInterfaceDefinition()
    {
        // Arrange
        var interfaceType = typeof(ISongService);
        
        // Act & Assert
        interfaceType.Should().BeAssignableTo<object>("ISongService should be an interface");
        interfaceType.Namespace.Should().Be("SetlistStudio.Core.Interfaces");
    }

    [Fact]
    public void ISongService_ShouldHaveRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(ISongService);
        
        // Act
        var methods = interfaceType.GetMethods();
        var methodNames = methods.Select(m => m.Name).ToArray();
        
        // Assert
        methodNames.Should().Contain("GetSongsAsync", "interface should have GetSongsAsync method");
        methodNames.Should().Contain("GetSongByIdAsync", "interface should have GetSongByIdAsync method");
        methodNames.Should().Contain("CreateSongAsync", "interface should have CreateSongAsync method");
        methodNames.Should().Contain("UpdateSongAsync", "interface should have UpdateSongAsync method");
        methodNames.Should().Contain("DeleteSongAsync", "interface should have DeleteSongAsync method");
    }

    [Fact]
    public void ISongService_ShouldBeImplementedBySongService()
    {
        // Arrange
        var serviceType = typeof(SongService);
        var interfaceType = typeof(ISongService);
        
        // Act & Assert
        serviceType.Should().Implement(interfaceType, "SongService should implement ISongService");
    }
}