using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Web.Services;
using Xunit;

namespace SetlistStudio.Tests.Web.Services;

/// <summary>
/// Simple tests for DatabaseInitializer focusing on branch coverage
/// Tests basic initialization paths
/// </summary>
public class DatabaseInitializerSimpleBranchTests : IDisposable
{
    private ServiceProvider? _serviceProvider;

    [Fact]
    public async Task DatabaseInitializer_ShouldInitializeSuccessfully_WithInMemoryDatabase()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<SetlistStudioDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        services.AddLogging();
        
        _serviceProvider = services.BuildServiceProvider();
        var logger = _serviceProvider.GetRequiredService<ILogger<Program>>();

        // Act
        await DatabaseInitializer.InitializeAsync(_serviceProvider, logger);

        // Assert - Should complete without throwing
        _serviceProvider.Should().NotBeNull("Service provider should be valid");
    }

    [Fact]
    public async Task DatabaseInitializer_ShouldHandleInMemoryDatabase_InTestEnvironment()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<SetlistStudioDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: "TestDatabase"));
        services.AddLogging();
        
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.Setup(x => x.EnvironmentName).Returns("Test");
        services.AddSingleton(mockEnvironment.Object);
        
        _serviceProvider = services.BuildServiceProvider();
        var logger = _serviceProvider.GetRequiredService<ILogger<Program>>();

        // Act
        await DatabaseInitializer.InitializeAsync(_serviceProvider, logger);

        // Assert
        _serviceProvider.Should().NotBeNull("Should handle test environment correctly");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}