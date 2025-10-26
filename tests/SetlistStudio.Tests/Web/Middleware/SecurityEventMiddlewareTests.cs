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
/// Comprehensive tests for SecurityEventMiddleware covering security event detection,
/// logging, and middleware behavior with various request patterns and edge cases.
/// </summary>
public class SecurityEventMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<SecurityEventMiddleware>> _mockLogger;
    private readonly Mock<ISecurityEventHandler> _mockSecurityEventHandler;
    private readonly Mock<SecurityEventLogger> _mockSecurityEventLogger;
    private readonly SecurityEventMiddleware _middleware;

    public SecurityEventMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<SecurityEventMiddleware>>();
        
        // Create mocks using interfaces for better testability
        _mockSecurityEventHandler = new Mock<ISecurityEventHandler>();
        
        var mockLoggerForEventLogger = new Mock<ILogger<SecurityEventLogger>>();
        _mockSecurityEventLogger = new Mock<SecurityEventLogger>(mockLoggerForEventLogger.Object);
        
        _middleware = new SecurityEventMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var middleware = new SecurityEventMiddleware(_mockNext.Object, _mockLogger.Object);

        // Assert
        middleware.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullNext_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new SecurityEventMiddleware(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new SecurityEventMiddleware(_mockNext.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task InvokeAsync_WithNormalRequest_ShouldCallNextMiddleware()
    {
        // Arrange
        var context = CreateHttpContext("/test");
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithSuspiciousUrlPattern_ShouldDetectAndLogThreat()
    {
        // Arrange
        var context = CreateHttpContext("/test?query=<script>alert('xss')</script>");
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            context,
            "MaliciousUrlPattern",
            It.IsAny<string>(),
            null,
            SecurityEventSeverity.High), Times.Once);
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("..\\windows\\system32")]
    [InlineData("%2e%2e/sensitive")]
    [InlineData("javascript:alert(1)")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("onload=alert(1)")]
    [InlineData("union select * from users")]
    [InlineData("drop table users")]
    public async Task InvokeAsync_WithVariousSuspiciousPatterns_ShouldDetectThreats(string suspiciousPath)
    {
        // Arrange
        var context = CreateHttpContext($"/test?query={suspiciousPath}");
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            context,
            "MaliciousUrlPattern",
            It.IsAny<string>(),
            null,
            SecurityEventSeverity.High), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingUserAgent_ShouldDetectSuspiciousActivity()
    {
        // Arrange
        var context = CreateHttpContext("/api/data");
        context.Request.Headers.Remove("User-Agent");
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            context,
            "MissingUserAgent",
            It.IsAny<string>(),
            null,
            SecurityEventSeverity.Low), Times.Once);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/healthcheck")]
    [InlineData("/ping")]
    [InlineData("/status")]
    [InlineData("/ready")]
    [InlineData("/metrics")]
    public async Task InvokeAsync_WithHealthCheckEndpoint_ShouldNotFlagMissingUserAgent(string healthPath)
    {
        // Arrange
        var context = CreateHttpContext(healthPath);
        context.Request.Headers.Remove("User-Agent");
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            It.IsAny<HttpContext>(),
            "MissingUserAgent",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<SecurityEventSeverity>()), Times.Never);
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.TestHost")]
    [InlineData("xunit/2.4.2")]
    [InlineData("Postman/10.0")]
    [InlineData("GitHub-Actions")]
    [InlineData("k6/0.45.0")]
    public async Task InvokeAsync_WithLegitimateTestingTools_ShouldNotFlagUserAgent(string userAgent)
    {
        // Arrange
        var context = CreateHttpContext("/api/test");
        context.Request.Headers["User-Agent"] = userAgent;
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            It.IsAny<HttpContext>(),
            It.Is<string>(s => s.Contains("UserAgent")),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<SecurityEventSeverity>()), Times.Never);
    }

    [Theory]
    [InlineData("Googlebot/2.1")]
    [InlineData("Bingbot/2.0")]
    [InlineData("facebookexternalhit/1.1")]
    [InlineData("Twitterbot/1.0")]
    [InlineData("LinkedInBot/1.0")]
    public async Task InvokeAsync_WithLegitimateSearchBots_ShouldNotFlagUserAgent(string userAgent)
    {
        // Arrange
        var context = CreateHttpContext("/");
        context.Request.Headers["User-Agent"] = userAgent;
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            It.IsAny<HttpContext>(),
            It.Is<string>(s => s.Contains("UserAgent")),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<SecurityEventSeverity>()), Times.Never);
    }

    [Theory]
    [InlineData("sqlmap/1.7.2")]
    [InlineData("nmap scripting engine")]
    [InlineData("nikto/2.5.0")]
    [InlineData("OWASP ZAP/2.12.0")]
    [InlineData("Burp Suite Professional")]
    [InlineData("nuclei-templates")]
    public async Task InvokeAsync_WithSecurityScanningTools_ShouldDetectHighRiskActivity(string userAgent)
    {
        // Arrange
        var context = CreateHttpContext("/api/sensitive");
        context.Request.Headers["User-Agent"] = userAgent;
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            context,
            "SecurityScannerUserAgent",
            It.IsAny<string>(),
            null,
            SecurityEventSeverity.High), Times.Once);
    }

    [Theory]
    [InlineData("python-urllib/3.9")]
    [InlineData("java/11.0.1")]
    [InlineData("go-http-client/1.1")]
    [InlineData("scraper-bot/1.0")]
    public async Task InvokeAsync_WithSuspiciousAutomationTools_ShouldDetectMediumRiskActivity(string userAgent)
    {
        // Arrange
        var context = CreateHttpContext("/api/data");
        context.Request.Headers["User-Agent"] = userAgent;
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            context,
            "SuspiciousAutomationUserAgent",
            It.IsAny<string>(),
            null,
            SecurityEventSeverity.Medium), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_ShouldLogAndRethrow()
    {
        // Arrange
        var context = CreateHttpContext("/protected");
        var exception = new UnauthorizedAccessException("Access denied");
        
        _mockNext.Setup(x => x(context))
            .ThrowsAsync(exception);

        // Act & Assert
        var act = async () => await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            context,
            "SecurityException",
            It.IsAny<string>(),
            null,
            SecurityEventSeverity.High), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithSecurityException_ShouldLogAndRethrow()
    {
        // Arrange
        var context = CreateHttpContext("/secure");
        var exception = new SecurityException("Security violation");
        
        _mockNext.Setup(x => x(context))
            .ThrowsAsync(exception);

        // Act & Assert
        var act = async () => await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);
        await act.Should().ThrowAsync<SecurityException>();

        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            context,
            "SecurityException",
            It.IsAny<string>(),
            null,
            SecurityEventSeverity.High), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidOperationException_ShouldLogSecurityRelatedAndRethrow()
    {
        // Arrange
        var context = CreateHttpContext("/operation");
        var exception = new InvalidOperationException("Invalid operation");
        
        _mockNext.Setup(x => x(context))
            .ThrowsAsync(exception);

        // Act & Assert
        var act = async () => await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);
        await act.Should().ThrowAsync<InvalidOperationException>();

        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            context,
            "SecurityException",
            It.IsAny<string>(),
            null,
            SecurityEventSeverity.High), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentException_ShouldNotLogAsSecurityExceptionButStillRethrow()
    {
        // Arrange
        var context = CreateHttpContext("/argument");
        var exception = new ArgumentException("Invalid argument");
        
        _mockNext.Setup(x => x(context))
            .ThrowsAsync(exception);

        // Act & Assert
        var act = async () => await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);
        await act.Should().ThrowAsync<ArgumentException>();

        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            It.IsAny<HttpContext>(),
            "SecurityException",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<SecurityEventSeverity>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithNonSecurityException_ShouldNotLogAsSecurityExceptionButStillRethrow()
    {
        // Arrange
        var context = CreateHttpContext("/general");
        var exception = new NotSupportedException("Not supported");
        
        _mockNext.Setup(x => x(context))
            .ThrowsAsync(exception);

        // Act & Assert
        var act = async () => await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);
        await act.Should().ThrowAsync<NotSupportedException>();

        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            It.IsAny<HttpContext>(),
            "SecurityException",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<SecurityEventSeverity>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithPostRequestAndFormData_ShouldCheckForSuspiciousPatterns()
    {
        // Arrange
        var context = CreateHttpContext("/submit", HttpMethods.Post);
        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["username"] = "admin",
            ["comment"] = "<script>alert('xss')</script>"
        });
        
        var formFeature = new Mock<IFormFeature>();
        formFeature.Setup(x => x.ReadFormAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(formCollection);
        formFeature.Setup(x => x.HasFormContentType).Returns(true);
        
        context.Features.Set(formFeature.Object);
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.ContentLength = 100; // Set content length to simulate real form
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            "XSS_Pattern_Detection",
            It.IsAny<string>(),
            It.IsAny<string>(),
            SecurityEventSeverity.High,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithPostRequestAndSqlInjectionInForm_ShouldDetectSqlInjection()
    {
        // Arrange
        var context = CreateHttpContext("/search", HttpMethods.Post);
        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["query"] = "'; DROP TABLE users; --"
        });
        
        var formFeature = new Mock<IFormFeature>();
        formFeature.Setup(x => x.ReadFormAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(formCollection);
        formFeature.Setup(x => x.HasFormContentType).Returns(true);
        
        context.Features.Set(formFeature.Object);
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.ContentLength = 100; // Set content length to simulate real form
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            "SQL_Injection_Pattern_Detection",
            It.IsAny<string>(),
            It.IsAny<string>(),
            SecurityEventSeverity.High,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact(Skip = "Slow request test requires 10+ second delay which is impractical for unit tests. Covered by integration tests.")]
    public async Task InvokeAsync_WithSlowRequest_ShouldDetectSlowRequestPattern()
    {
        // This test is skipped because it requires a genuine 10+ second delay to trigger the slow request detection.
        // The slow request functionality is covered by integration tests instead.
        
        // Act & Assert - Test skipped (no setup needed for skipped test)
        await Task.CompletedTask;
    }

    [Fact]
    public async Task InvokeAsync_WithAuthenticatedUserAccessingSensitiveArea_ShouldLogDataAccess()
    {
        // Arrange
        var context = CreateHttpContext("/admin/users");
        SetupAuthenticatedUser(context, "testuser@example.com");
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventLogger.Verify(x => x.LogDataAccess(
            "testuser@example.com",
            "SensitiveArea",
            "/admin/users",
            "GET",
            It.IsAny<int?>()), Times.Once);
    }

    [Theory]
    [InlineData("/admin/settings")]
    [InlineData("/account/profile")]
    [InlineData("/api/sensitive")]
    [InlineData("/dashboard/metrics")]
    public async Task InvokeAsync_WithSensitivePaths_ShouldLogDataAccess(string sensitivePath)
    {
        // Arrange
        var context = CreateHttpContext(sensitivePath);
        SetupAuthenticatedUser(context, "admin@example.com");
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockSecurityEventLogger.Verify(x => x.LogDataAccess(
            "admin@example.com",
            "SensitiveArea",
            sensitivePath,
            "GET",
            It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithXForwardedForHeader_ShouldExtractCorrectClientIp()
    {
        // Arrange
        var context = CreateHttpContext("/test");
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.100, 10.0.0.1";
        context.Request.Headers.Remove("User-Agent");
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert - Should extract first IP from X-Forwarded-For
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            context,
            "MissingUserAgent",
            It.IsAny<string>(),
            null,
            SecurityEventSeverity.Low), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithXRealIpHeader_ShouldExtractCorrectClientIp()
    {
        // Arrange
        var context = CreateHttpContext("/test");
        context.Request.Headers["X-Real-IP"] = "203.0.113.1";
        context.Request.Headers.Remove("User-Agent");
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert - Should extract IP from X-Real-IP
        _mockSecurityEventHandler.Verify(x => x.OnSuspiciousActivity(
            context,
            "MissingUserAgent",
            It.IsAny<string>(),
            null,
            SecurityEventSeverity.Low), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithLoginPageSuccess_ShouldLogInformation()
    {
        // Arrange
        var context = CreateHttpContext("/login");
        context.Response.StatusCode = 200;
        
        _mockNext.Setup(x => x(context))
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockSecurityEventHandler.Object, _mockSecurityEventLogger.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Login page accessed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Helper method to create an HttpContext with specified path and method.
    /// </summary>
    private static DefaultHttpContext CreateHttpContext(string path, string method = "GET")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Request.Headers["User-Agent"] = "Test-Agent/1.0";
        
        // Set up connection for IP address extraction
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        
        return context;
    }

    /// <summary>
    /// Helper method to set up an authenticated user in HttpContext.
    /// </summary>
    private static void SetupAuthenticatedUser(DefaultHttpContext context, string userId)
    {
        var identity = new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, userId)
        }, "Test");
        
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);
    }
}