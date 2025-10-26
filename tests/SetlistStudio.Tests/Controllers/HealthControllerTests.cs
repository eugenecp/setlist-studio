using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Web.Controllers;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace SetlistStudio.Tests.Controllers;

/// <summary>
/// Comprehensive tests for HealthController covering all endpoints and scenarios
/// Covers: Get (full health check), GetSimple (simple health check), CheckDatabaseHealth (private method)
/// Tests normal operation, database connection success/failure, and error handling
/// </summary>
public class HealthControllerTests : IDisposable
{
    private readonly Mock<ILogger<HealthController>> _mockLogger;
    private readonly SetlistStudioDbContext? _context;

    public HealthControllerTests()
    {
        _mockLogger = new Mock<ILogger<HealthController>>();

        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new SetlistStudioDbContext(options);
    }

    [Fact]
    public async Task Get_ShouldReturnHealthStatus_WhenDatabaseIsConnected()
    {
        // Arrange
        var controller = new HealthController(_mockLogger.Object, _context);

        // Act
        var result = await controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().NotBeNull();
        
        // Use reflection to check the anonymous object properties
        var healthStatus = okResult!.Value!;
        var statusProperty = healthStatus.GetType().GetProperty("Status");
        var timestampProperty = healthStatus.GetType().GetProperty("Timestamp");
        var serviceProperty = healthStatus.GetType().GetProperty("Service");
        var versionProperty = healthStatus.GetType().GetProperty("Version");
        var databaseProperty = healthStatus.GetType().GetProperty("Database");

        statusProperty!.GetValue(healthStatus).Should().Be("Healthy");
        timestampProperty!.GetValue(healthStatus).Should().BeOfType<DateTime>();
        serviceProperty!.GetValue(healthStatus).Should().Be("Setlist Studio");
        versionProperty!.GetValue(healthStatus).Should().Be("1.0.0");
        databaseProperty!.GetValue(healthStatus).Should().Be("Connected");

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Health check requested")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturnHealthStatusWithDatabaseError_WhenContextIsNull()
    {
        // Arrange
        var controller = new HealthController(_mockLogger.Object, context: null);

        // Act
        var result = await controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        var healthStatus = okResult!.Value!;
        var databaseProperty = healthStatus.GetType().GetProperty("Database");
        
        databaseProperty!.GetValue(healthStatus).Should().Be("Database context not available");
    }

    [Fact]
    public async Task Get_ShouldReturnHealthStatusWithDatabaseError_WhenDatabaseConnectionFails()
    {
        // Arrange - Dispose context to simulate connection failure
        await _context!.DisposeAsync();
        var controller = new HealthController(_mockLogger.Object, _context);

        // Act
        var result = await controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        var healthStatus = okResult!.Value!;
        var databaseProperty = healthStatus.GetType().GetProperty("Database");
        var statusProperty = healthStatus.GetType().GetProperty("Status");
        
        // Should still return "Healthy" for service, but database should show error
        statusProperty!.GetValue(healthStatus).Should().Be("Healthy");
        databaseProperty!.GetValue(healthStatus).Should().NotBe("Connected");
        databaseProperty!.GetValue(healthStatus)?.ToString().Should().Be("Database configuration error");

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Database configuration invalid during health check")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetSimple_ShouldReturnSimpleHealthStatus_WithoutDatabaseCheck()
    {
        // Arrange
        var controller = new HealthController(_mockLogger.Object, _context);

        // Act
        var result = controller.GetSimple();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().NotBeNull();
        
        var healthStatus = okResult!.Value!;
        var statusProperty = healthStatus.GetType().GetProperty("Status");
        var timestampProperty = healthStatus.GetType().GetProperty("Timestamp");
        var serviceProperty = healthStatus.GetType().GetProperty("Service");
        var versionProperty = healthStatus.GetType().GetProperty("Version");
        var databaseProperty = healthStatus.GetType().GetProperty("Database");

        statusProperty!.GetValue(healthStatus).Should().Be("Healthy");
        timestampProperty!.GetValue(healthStatus).Should().BeOfType<DateTime>();
        serviceProperty!.GetValue(healthStatus).Should().Be("Setlist Studio");
        versionProperty!.GetValue(healthStatus).Should().Be("1.0.0");
        databaseProperty.Should().BeNull(); // Simple check doesn't include database

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Simple health check requested")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetSimple_ShouldReturnHealthy_EvenWhenDatabaseContextIsNull()
    {
        // Arrange - Test that simple health check works without database dependency
        var controller = new HealthController(_mockLogger.Object, context: null);

        // Act
        var result = controller.GetSimple();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        var healthStatus = okResult!.Value!;
        var statusProperty = healthStatus.GetType().GetProperty("Status");
        
        statusProperty!.GetValue(healthStatus).Should().Be("Healthy");
    }

    [Fact]
    public async Task Get_TimestampShouldBeRecent_WhenCalled()
    {
        // Arrange
        var controller = new HealthController(_mockLogger.Object, _context);
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = await controller.Get();

        // Assert
        var afterCall = DateTime.UtcNow;
        var okResult = result as OkObjectResult;
        var healthStatus = okResult!.Value!;
        var timestampProperty = healthStatus.GetType().GetProperty("Timestamp");
        var timestamp = (DateTime)timestampProperty!.GetValue(healthStatus)!;

        timestamp.Should().BeAfter(beforeCall.AddSeconds(-1));
        timestamp.Should().BeBefore(afterCall.AddSeconds(1));
    }

    [Fact]
    public void GetSimple_TimestampShouldBeRecent_WhenCalled()
    {
        // Arrange
        var controller = new HealthController(_mockLogger.Object, _context);
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = controller.GetSimple();

        // Assert
        var afterCall = DateTime.UtcNow;
        var okResult = result as OkObjectResult;
        var healthStatus = okResult!.Value!;
        var timestampProperty = healthStatus.GetType().GetProperty("Timestamp");
        var timestamp = (DateTime)timestampProperty!.GetValue(healthStatus)!;

        timestamp.Should().BeAfter(beforeCall.AddSeconds(-1));
        timestamp.Should().BeBefore(afterCall.AddSeconds(1));
    }

    [Fact]
    public async Task Get_LoggerShouldBeCalledWithCorrectParameters_WhenCalled()
    {
        // Arrange
        var controller = new HealthController(_mockLogger.Object, _context);

        // Act
        await controller.Get();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Health check requested") && 
                    v.ToString()!.Contains("Status: Healthy") &&
                    v.ToString()!.Contains("Database: Connected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetSimple_LoggerShouldBeCalledWithCorrectMessage_WhenCalled()
    {
        // Arrange
        var controller = new HealthController(_mockLogger.Object, _context);

        // Act
        controller.GetSimple();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Simple health check requested - Status: Healthy")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void HealthController_ShouldHaveEnableRateLimitingAttribute_WithApiPolicy()
    {
        // Arrange
        var controllerType = typeof(HealthController);

        // Act
        var rateLimitingAttribute = controllerType.GetCustomAttribute<EnableRateLimitingAttribute>();

        // Assert
        rateLimitingAttribute.Should().NotBeNull("HealthController should have EnableRateLimiting attribute for security");
        rateLimitingAttribute!.PolicyName.Should().Be("ApiPolicy", "HealthController should use the ApiPolicy rate limiting policy");
    }

    [Fact]
    public void HealthController_ShouldHaveAllowAnonymousAttribute_ForPublicAccess()
    {
        // Arrange
        var controllerType = typeof(HealthController);

        // Act
        var allowAnonymousAttribute = controllerType.GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert
        allowAnonymousAttribute.Should().NotBeNull("HealthController should allow anonymous access for health monitoring");
    }

    [Fact]
    public void HealthController_ShouldHaveApiControllerAttribute_ForApiBehavior()
    {
        // Arrange
        var controllerType = typeof(HealthController);

        // Act
        var apiControllerAttribute = controllerType.GetCustomAttribute<ApiControllerAttribute>();

        // Assert
        apiControllerAttribute.Should().NotBeNull("HealthController should have ApiController attribute for API behavior");
    }

    [Fact]
    public void HealthController_ShouldHaveCorrectRouteAttribute()
    {
        // Arrange
        var controllerType = typeof(HealthController);

        // Act
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();

        // Assert
        routeAttribute.Should().NotBeNull("HealthController should have Route attribute");
        routeAttribute!.Template.Should().Be("api/[controller]", "HealthController should use the correct route template");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}