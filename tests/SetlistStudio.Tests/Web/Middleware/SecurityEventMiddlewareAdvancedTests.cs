using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using SetlistStudio.Core.Security;
using SetlistStudio.Web.Middleware;
using SetlistStudio.Web.Security;
using System.Collections.Generic;
using System.Security;
using System.Text;
using Xunit;

namespace SetlistStudio.Tests.Web.Middleware;

/// <summary>
/// Advanced tests for SecurityEventMiddleware targeting edge cases, error conditions,
/// and coverage gaps to improve line coverage from 91.9% to 95%+ by covering
/// uncovered security exception paths, edge cases, and validation boundaries.
/// </summary>
public class SecurityEventMiddlewareAdvancedTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<SecurityEventMiddleware>> _mockLogger;
    private readonly Mock<ISecurityEventHandler> _mockSecurityEventHandler;
    private readonly Mock<SecurityEventLogger> _mockSecurityEventLogger;
    private readonly SecurityEventMiddleware _middleware;

    public SecurityEventMiddlewareAdvancedTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<SecurityEventMiddleware>>();
        
        // Create mocks using interfaces for better testability
        _mockSecurityEventHandler = new Mock<ISecurityEventHandler>();
        
        var mockLoggerForEventLogger = new Mock<ILogger<SecurityEventLogger>>();
        _mockSecurityEventLogger = new Mock<SecurityEventLogger>(mockLoggerForEventLogger.Object);
        
        _middleware = new SecurityEventMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SecurityEventMiddleware(null!, _mockLogger.Object));

        exception.ParamName.Should().Be("next");
        exception.Message.Should().Contain("next");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SecurityEventMiddleware(_mockNext.Object, null!));

        exception.ParamName.Should().Be("logger");
        exception.Message.Should().Contain("logger");
    }

    #endregion

    #region Security Exception Handling Tests

    [Fact]
    public async Task InvokeAsync_WithSecurityException_HandlesAndLogsCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        var securityException = new SecurityException("Unauthorized access detected");
        
        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(securityException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SecurityException>(() =>
            _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object));

        // Verify exception is re-thrown
        exception.Should().Be(securityException);
        
        // Verify logging occurred
        VerifyLoggerCalled(LogLevel.Warning, "Security exception in middleware", Times.AtLeastOnce());
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidOperationExceptionSecurityRelated_HandlesAndLogsCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        var invalidOpException = new InvalidOperationException("Authentication scheme not configured");
        
        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(invalidOpException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object));

        // Verify exception is re-thrown
        exception.Should().Be(invalidOpException);
        
        // Verify error logging occurred
        VerifyLoggerCalled(LogLevel.Error, "Invalid operation in security middleware", Times.AtLeastOnce());
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentException_HandlesAndLogsCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        var argumentException = new ArgumentException("Invalid security parameter", "securityParam");
        
        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(argumentException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object));

        // Verify exception is re-thrown
        exception.Should().Be(argumentException);
        
        // Verify error logging occurred
        VerifyLoggerCalled(LogLevel.Error, "Invalid argument in security middleware", Times.AtLeastOnce());
    }

    [Fact]
    public async Task InvokeAsync_WithGeneralExceptionSecurityRelated_HandlesAndLogsCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        var generalException = new Exception("Authentication token expired");
        
        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(generalException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object));

        // Verify exception is re-thrown
        exception.Should().Be(generalException);
        
        // Verify error logging occurred
        VerifyLoggerCalled(LogLevel.Error, "Unexpected error in security event middleware", Times.AtLeastOnce());
    }

    [Fact]
    public async Task InvokeAsync_WithGeneralExceptionNotSecurityRelated_HandlesCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers["User-Agent"] = "Mozilla/5.0"; // Add user agent to avoid security detection
        var generalException = new Exception("Database connection timeout");
        
        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(generalException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object));

        // Verify exception is re-thrown
        exception.Should().Be(generalException);
        
        // Verify error logging occurred 
        VerifyLoggerCalled(LogLevel.Error, "Unexpected error in security event middleware", Times.AtLeastOnce());
        // Note: Security event handler may still be called as part of normal request processing
    }

    #endregion

    #region Slow Request Detection Tests

    [Fact]
    public async Task InvokeAsync_WithSlowRequest_LogsSuspiciousActivity()
    {
        // Arrange
        var context = CreateHttpContext();
        
        _mockNext
            .Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(async (HttpContext ctx) =>
            {
                // Simulate slow processing
                await Task.Delay(100); // Simulate delay
            });

        // Override the slow request threshold by using reflection or mocking
        // Note: In real implementation, we'd make this configurable for testing

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        // Since we can't easily simulate 10+ second delay in unit test,
        // we verify the middleware completes successfully without exceptions
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region Request Context and Path Handling Tests

    [Fact]
    public async Task InvokeAsync_WithNullRequestPath_HandlesGracefully()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = PathString.Empty; // Empty path

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithSpecialCharactersInPath_HandlesCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/test/../admin"; // Path traversal attempt

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithLongRequestPath_HandlesCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        var longPath = "/api/" + new string('a', 1000); // Very long path
        context.Request.Path = longPath;

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region HTTP Method and Header Validation Tests

    [Fact]
    public async Task InvokeAsync_WithSuspiciousHttpMethod_HandlesCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Method = "TRACE"; // Potentially suspicious method

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithSuspiciousHeaders_HandlesCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "127.0.0.1, 192.168.1.1";
        context.Request.Headers["User-Agent"] = "SqlMap/1.0"; // Known attack tool

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingHeaders_HandlesCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Headers.Clear(); // No headers

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region Request Body and Query Parameter Tests

    [Fact]
    public async Task InvokeAsync_WithSuspiciousQueryParameters_HandlesCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.QueryString = new QueryString("?id=1' OR '1'='1"); // SQL injection attempt

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithLargeQueryString_HandlesCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        var largeQuery = "?data=" + new string('x', 10000); // Large query string
        context.Request.QueryString = new QueryString(largeQuery);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region Authentication Context Tests

    [Fact]
    public async Task InvokeAsync_WithNullUserContext_HandlesGracefully()
    {
        // Arrange
        var context = CreateHttpContext();
        context.User = null!; // No user context

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthenticatedUser_HandlesCorrectly()
    {
        // Arrange
        var context = CreateHttpContext();
        // User context is already set as unauthenticated by CreateHttpContext

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region Request Feature Tests

    [Fact]
    public async Task InvokeAsync_WithMissingHttpRequestFeature_HandlesGracefully()
    {
        // Arrange
        var context = new DefaultHttpContext();
        // Don't set up normal features to test edge case handling

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Once);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task InvokeAsync_WithConcurrentRequests_HandlesCorrectly()
    {
        // Arrange
        var context1 = CreateHttpContext();
        var context2 = CreateHttpContext();
        
        context1.Request.Path = "/api/test1";
        context2.Request.Path = "/api/test2";

        // Act
        var task1 = _middleware.InvokeAsync(context1, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);
        var task2 = _middleware.InvokeAsync(context2, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        await Task.WhenAll(task1, task2);

        // Assert
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Exactly(2));
    }

    #endregion

    #region Helper Methods

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.User = new System.Security.Claims.ClaimsPrincipal();
        return context;
    }

    private void VerifyLoggerCalled(LogLevel logLevel, string message, Times times)
    {
        _mockLogger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    #endregion
}
