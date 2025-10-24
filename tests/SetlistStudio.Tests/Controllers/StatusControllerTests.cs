using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SetlistStudio.Web.Controllers;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace SetlistStudio.Tests.Controllers;

/// <summary>
/// Comprehensive tests for StatusController covering all endpoints
/// Covers: Get (main status endpoint), Ping (ping endpoint)
/// Tests response structure, data validation, and environment variable handling
/// </summary>
[Collection("EnvironmentVariable")]
public class StatusControllerTests
{
    // Shared lock to prevent environment variable race conditions in tests
    private static readonly object _environmentLock = new object();
    [Fact]
    public void Get_ShouldReturnStatusObject_WithCorrectProperties()
    {
        // Arrange
        var controller = new StatusController();

        // Act
        var result = controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var status = okResult.Value!;
        var statusProperty = status.GetType().GetProperty("Status");
        var timestampProperty = status.GetType().GetProperty("Timestamp");
        var serviceProperty = status.GetType().GetProperty("Service");
        var versionProperty = status.GetType().GetProperty("Version");
        var environmentProperty = status.GetType().GetProperty("Environment");

        statusProperty!.GetValue(status).Should().Be("Healthy");
        timestampProperty!.GetValue(status).Should().BeOfType<DateTime>();
        serviceProperty!.GetValue(status).Should().Be("Setlist Studio");
        versionProperty!.GetValue(status).Should().Be("1.0.0");
        environmentProperty!.GetValue(status).Should().NotBeNull();
    }

    [Fact]
    public void Get_TimestampShouldBeRecent_WhenCalled()
    {
        // Arrange
        var controller = new StatusController();
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = controller.Get();

        // Assert
        var afterCall = DateTime.UtcNow;
        var okResult = result as OkObjectResult;
        var status = okResult!.Value!;
        var timestampProperty = status.GetType().GetProperty("Timestamp");
        var timestamp = (DateTime)timestampProperty!.GetValue(status)!;

        timestamp.Should().BeAfter(beforeCall.AddSeconds(-1));
        timestamp.Should().BeBefore(afterCall.AddSeconds(1));
    }

    [Fact]
    public void Get_ShouldReturnEnvironmentVariable_WhenASPNETCORE_ENVIRONMENTIsSet()
    {
        lock (_environmentLock)
        {
            // Arrange
            var controller = new StatusController();
            var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            
            try
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
                Thread.Sleep(200); // Allow environment change to propagate

                // Act
                var result = controller.Get();

                // Assert
                var okResult = result as OkObjectResult;
                var status = okResult!.Value!;
                var environmentProperty = status.GetType().GetProperty("Environment");
                
                environmentProperty!.GetValue(status).Should().Be("Testing");
            }
            finally
            {
                // Restore original environment variable
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
            }
        }
    }

    [Fact]
    public void Get_ShouldReturnUnknown_WhenASPNETCORE_ENVIRONMENTIsNotSet()
    {
        lock (_environmentLock)
        {
            // Arrange
            var controller = new StatusController();
            var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            
            try
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
                Thread.Sleep(200); // Allow environment change to propagate

                // Act
                var result = controller.Get();

                // Assert
                var okResult = result as OkObjectResult;
                var status = okResult!.Value!;
                var environmentProperty = status.GetType().GetProperty("Environment");
                
                environmentProperty!.GetValue(status).Should().Be("Unknown");
            }
            finally
            {
                // Restore original environment variable
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
            }
        }
    }

    [Fact]
    public void Ping_ShouldReturnPongResponse_WithCorrectProperties()
    {
        // Arrange
        var controller = new StatusController();

        // Act
        var result = controller.Ping();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var pingResponse = okResult.Value!;
        var statusProperty = pingResponse.GetType().GetProperty("Status");
        var timestampProperty = pingResponse.GetType().GetProperty("Timestamp");

        statusProperty!.GetValue(pingResponse).Should().Be("Pong");
        timestampProperty!.GetValue(pingResponse).Should().BeOfType<DateTime>();
    }

    [Fact]
    public void Ping_TimestampShouldBeRecent_WhenCalled()
    {
        // Arrange
        var controller = new StatusController();
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = controller.Ping();

        // Assert
        var afterCall = DateTime.UtcNow;
        var okResult = result as OkObjectResult;
        var pingResponse = okResult!.Value!;
        var timestampProperty = pingResponse.GetType().GetProperty("Timestamp");
        var timestamp = (DateTime)timestampProperty!.GetValue(pingResponse)!;

        timestamp.Should().BeAfter(beforeCall.AddSeconds(-1));
        timestamp.Should().BeBefore(afterCall.AddSeconds(1));
    }

    [Fact]
    public async Task Get_ShouldReturnDifferentTimestamps_WhenCalledMultipleTimes()
    {
        // Arrange
        var controller = new StatusController();

        // Act
        var result1 = controller.Get();
        await Task.Delay(10); // Small delay to ensure different timestamps
        var result2 = controller.Get();

        // Assert
        var okResult1 = result1 as OkObjectResult;
        var okResult2 = result2 as OkObjectResult;
        
        var status1 = okResult1!.Value!;
        var status2 = okResult2!.Value!;
        
        var timestamp1 = (DateTime)status1.GetType().GetProperty("Timestamp")!.GetValue(status1)!;
        var timestamp2 = (DateTime)status2.GetType().GetProperty("Timestamp")!.GetValue(status2)!;

        timestamp2.Should().BeAfter(timestamp1);
    }

    [Fact]
    public async Task Ping_ShouldReturnDifferentTimestamps_WhenCalledMultipleTimes()
    {
        // Arrange
        var controller = new StatusController();

        // Act
        var result1 = controller.Ping();
        await Task.Delay(10); // Small delay to ensure different timestamps
        var result2 = controller.Ping();

        // Assert
        var okResult1 = result1 as OkObjectResult;
        var okResult2 = result2 as OkObjectResult;
        
        var ping1 = okResult1!.Value!;
        var ping2 = okResult2!.Value!;
        
        var timestamp1 = (DateTime)ping1.GetType().GetProperty("Timestamp")!.GetValue(ping1)!;
        var timestamp2 = (DateTime)ping2.GetType().GetProperty("Timestamp")!.GetValue(ping2)!;

        timestamp2.Should().BeAfter(timestamp1);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("CustomEnvironment")]
    public void Get_ShouldReturnCorrectEnvironment_ForDifferentEnvironmentValues(string environmentValue)
    {
        lock (_environmentLock)
        {
            // Arrange
            var controller = new StatusController();
            var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            
            try
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentValue);
                Thread.Sleep(200); // Allow environment change to propagate

                // Act
                var result = controller.Get();

                // Assert
                var okResult = result as OkObjectResult;
                var status = okResult!.Value!;
                var environmentProperty = status.GetType().GetProperty("Environment");
                
                environmentProperty!.GetValue(status).Should().Be(environmentValue);
            }
            finally
            {
                // Restore original environment variable
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
            }
        }
    }

    [Fact]
    public async Task Get_And_Ping_ShouldBothReturnOkResult_WhenCalledConcurrently()
    {
        // Arrange
        var controller = new StatusController();
        var tasks = new List<Task<IActionResult>>();

        // Act - Call both endpoints concurrently multiple times
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => controller.Get()));
            tasks.Add(Task.Run(() => controller.Ping()));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            result.Should().BeOfType<OkObjectResult>();
        }
    }

    [Fact]
    public void StatusController_ShouldHaveEnableRateLimitingAttribute_WithApiPolicy()
    {
        // Arrange
        var controllerType = typeof(StatusController);

        // Act
        var rateLimitingAttribute = controllerType.GetCustomAttribute<EnableRateLimitingAttribute>();

        // Assert
        rateLimitingAttribute.Should().NotBeNull("StatusController should have EnableRateLimiting attribute for security");
        rateLimitingAttribute!.PolicyName.Should().Be("ApiPolicy", "StatusController should use the ApiPolicy rate limiting policy");
    }

    [Fact]
    public void StatusController_ShouldHaveAllowAnonymousAttribute_ForPublicAccess()
    {
        // Arrange
        var controllerType = typeof(StatusController);

        // Act
        var allowAnonymousAttribute = controllerType.GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert
        allowAnonymousAttribute.Should().NotBeNull("StatusController should allow anonymous access for status monitoring");
    }

    [Fact]
    public void StatusController_ShouldHaveApiControllerAttribute_ForApiBehavior()
    {
        // Arrange
        var controllerType = typeof(StatusController);

        // Act
        var apiControllerAttribute = controllerType.GetCustomAttribute<ApiControllerAttribute>();

        // Assert
        apiControllerAttribute.Should().NotBeNull("StatusController should have ApiController attribute for API behavior");
    }

    [Fact]
    public void StatusController_ShouldHaveCorrectRouteAttribute()
    {
        // Arrange
        var controllerType = typeof(StatusController);

        // Act
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();

        // Assert
        routeAttribute.Should().NotBeNull("StatusController should have Route attribute");
        routeAttribute!.Template.Should().Be("api/[controller]", "StatusController should use the correct API route template");
    }
}