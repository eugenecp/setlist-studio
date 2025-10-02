using FluentAssertions;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Interfaces;

/// <summary>
/// Tests for ISetlistService interface contract validation
/// Ensures interface contract is properly defined and consistent
/// </summary>
public class ISetlistServiceTests
{
    [Fact]
    public void ISetlistService_ShouldHaveCorrectInterfaceDefinition()
    {
        // Arrange
        var interfaceType = typeof(ISetlistService);
        
        // Act & Assert
        interfaceType.Should().BeAssignableTo<object>("ISetlistService should be an interface");
        interfaceType.Namespace.Should().Be("SetlistStudio.Core.Interfaces");
    }

    [Fact]
    public void ISetlistService_ShouldHaveRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(ISetlistService);
        
        // Act
        var methods = interfaceType.GetMethods();
        var methodNames = methods.Select(m => m.Name).ToArray();
        
        // Assert
        methodNames.Should().Contain("GetSetlistsAsync", "interface should have GetSetlistsAsync method");
        methodNames.Should().Contain("GetSetlistByIdAsync", "interface should have GetSetlistByIdAsync method");
        methodNames.Should().Contain("CreateSetlistAsync", "interface should have CreateSetlistAsync method");
        methodNames.Should().Contain("UpdateSetlistAsync", "interface should have UpdateSetlistAsync method");
        methodNames.Should().Contain("DeleteSetlistAsync", "interface should have DeleteSetlistAsync method");
    }

    [Fact]
    public void ISetlistService_ShouldBeImplementedBySetlistService()
    {
        // Arrange
        var serviceType = typeof(SetlistService);
        var interfaceType = typeof(ISetlistService);
        
        // Act & Assert
        serviceType.Should().Implement(interfaceType, "SetlistService should implement ISetlistService");
    }
}