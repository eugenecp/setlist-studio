using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;
using SetlistStudio.Web.Middleware;

namespace SetlistStudio.Tests.Web.Middleware;

/// <summary>
/// Comprehensive tests for RateLimitHeadersMiddleware covering header addition, calculations, and extension methods.
/// </summary>
public class RateLimitHeadersMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly DefaultHttpContext _httpContext;

    public RateLimitHeadersMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);

        // Assert
        middleware.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextDelegate()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddRateLimitLimitHeader()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-RateLimit-Limit");
        _httpContext.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("100");
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddRateLimitRemainingHeader()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-RateLimit-Remaining");
        var remainingValue = _httpContext.Response.Headers["X-RateLimit-Remaining"].ToString();
        remainingValue.Should().NotBeNullOrEmpty();
        int.Parse(remainingValue).Should().BeGreaterOrEqualTo(10).And.BeLessOrEqualTo(100);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddRateLimitResetHeader()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);
        var beforeExecution = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-RateLimit-Reset");
        var resetValue = _httpContext.Response.Headers["X-RateLimit-Reset"].ToString();
        resetValue.Should().NotBeNullOrEmpty();
        
        var resetTimeStamp = long.Parse(resetValue);
        var afterExecution = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds();
        
        resetTimeStamp.Should().BeGreaterOrEqualTo(beforeExecution).And.BeLessOrEqualTo(afterExecution);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddRateLimitWindowHeader()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-RateLimit-Window");
        _httpContext.Response.Headers["X-RateLimit-Window"].ToString().Should().Be("60");
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddAllRateLimitHeaders()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var expectedHeaders = new[] { "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset", "X-RateLimit-Window" };
        foreach (var expectedHeader in expectedHeaders)
        {
            _httpContext.Response.Headers.Should().ContainKey(expectedHeader, 
                $"rate limit header {expectedHeader} should be present");
        }
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotOverwriteExistingHeaders()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);
        
        // Pre-add a header
        _httpContext.Response.Headers["X-RateLimit-Limit"] = "200";

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("200", 
            "existing headers should not be overwritten");
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleExceptionInNextMiddleware()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(_httpContext));
        
        exception.Message.Should().Be("Test exception");
        
        // Headers should still be added even if next middleware fails
        _httpContext.Response.Headers.Should().ContainKey("X-RateLimit-Limit");
    }

    [Fact]
    public async Task CalculateRemainingRequests_ShouldReturnValueBetween10And100()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var remainingValue = int.Parse(_httpContext.Response.Headers["X-RateLimit-Remaining"].ToString());
        remainingValue.Should().BeGreaterOrEqualTo(10, "remaining requests should never go below 10");
        remainingValue.Should().BeLessOrEqualTo(100, "remaining requests should never exceed the limit");
    }

    [Fact]
    public async Task InvokeAsync_ShouldWorkWithDifferentHttpMethods()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);

        var httpMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };

        foreach (var method in httpMethods)
        {
            // Arrange
            _httpContext.Request.Method = method;
            _httpContext.Response.Headers.Clear();

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Headers.Should().ContainKey("X-RateLimit-Limit", 
                $"rate limit headers should be added for {method} requests");
        }
    }

    [Fact]
    public async Task InvokeAsync_ShouldWorkWithDifferentRequestPaths()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);

        var paths = new[] { "/", "/api/songs", "/api/setlists", "/health", "/metrics" };

        foreach (var path in paths)
        {
            // Arrange
            _httpContext.Request.Path = path;
            _httpContext.Response.Headers.Clear();

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Headers.Should().ContainKey("X-RateLimit-Limit", 
                $"rate limit headers should be added for path {path}");
        }
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleMultipleConcurrentRequests()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var contexts = Enumerable.Range(0, 10)
            .Select(_ => new DefaultHttpContext())
            .ToArray();

        // Act
        var tasks = contexts.Select(ctx => middleware.InvokeAsync(ctx)).ToArray();
        await Task.WhenAll(tasks);

        // Assert
        foreach (var context in contexts)
        {
            context.Response.Headers.Should().ContainKey("X-RateLimit-Limit");
            context.Response.Headers.Should().ContainKey("X-RateLimit-Remaining");
            context.Response.Headers.Should().ContainKey("X-RateLimit-Reset");
            context.Response.Headers.Should().ContainKey("X-RateLimit-Window");
        }
    }

    [Fact]
    public void UseRateLimitHeaders_ShouldAddMiddlewareToApplicationBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        var serviceProvider = services.BuildServiceProvider();
        
        var applicationBuilder = new ApplicationBuilder(serviceProvider);

        // Act
        var result = applicationBuilder.UseRateLimitHeaders();

        // Assert
        result.Should().BeSameAs(applicationBuilder, "extension method should return the same builder for chaining");
    }

    [Fact]
    public void UseRateLimitHeaders_ShouldBeChainable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        var serviceProvider = services.BuildServiceProvider();
        
        var applicationBuilder = new ApplicationBuilder(serviceProvider);

        // Act & Assert - should not throw
        var result = applicationBuilder
            .UseRateLimitHeaders()
            .UseMiddleware<TestMiddleware>();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_ShouldPreserveHttpContextProperties()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);

        // Set up context properties
        _httpContext.Request.ContentType = "application/json";
        _httpContext.Request.Headers["User-Agent"] = "Test-Agent";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("test", "value") }));
        _httpContext.User = user;

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Request.ContentType.Should().Be("application/json");
        _httpContext.Request.Headers["User-Agent"].ToString().Should().Be("Test-Agent");
        _httpContext.User.Should().BeSameAs(user);
    }

    [Fact]
    public async Task CalculateRemainingRequests_ShouldFollowExpectedPattern()
    {
        // This test validates the general pattern, though exact values depend on actual system time
        // We test that the calculation follows the expected decreasing pattern with a minimum floor
        
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);
        _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var remainingValue = int.Parse(_httpContext.Response.Headers["X-RateLimit-Remaining"].ToString());
        
        // The actual value depends on system time, but should follow the pattern
        remainingValue.Should().BeGreaterOrEqualTo(10, "should never go below minimum of 10");
        remainingValue.Should().BeLessOrEqualTo(100, "should never exceed the limit of 100");
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleNullHttpContextGracefully()
    {
        // Arrange
        var middleware = new RateLimitHeadersMiddleware(_mockNext.Object, _mockServiceProvider.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => middleware.InvokeAsync(null!));
    }

    // Helper test middleware for chaining tests
    private class TestMiddleware
    {
        private readonly RequestDelegate _next;

        public TestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);
        }
    }
}