using Microsoft.AspNetCore.Mvc;
using SetlistStudio.Web.Controllers;
using FluentAssertions;
using Xunit;

namespace SetlistStudio.Tests.Controllers;

/// <summary>
/// Comprehensive tests for StatusController covering all endpoints
/// Covers: Get (main status endpoint), Ping (ping endpoint)
/// Tests response structure, data validation, and environment variable handling
/// </summary>
public class StatusControllerTests
{
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
        // Arrange
        var controller = new StatusController();
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

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

    [Fact]
    public void Get_ShouldReturnUnknown_WhenASPNETCORE_ENVIRONMENTIsNotSet()
    {
        // Arrange
        var controller = new StatusController();
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

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
        // Arrange
        var controller = new StatusController();
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentValue);

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

    [Fact]
    public void Get_And_Ping_ShouldBothReturnOkResult_WhenCalledConcurrently()
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

        Task.WaitAll(tasks.ToArray());

        // Assert
        foreach (var task in tasks)
        {
            task.Result.Should().BeOfType<OkObjectResult>();
        }
    }
}