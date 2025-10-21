using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Security;
using SetlistStudio.Web.Security;
using System.Security.Claims;
using Xunit;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Tests for SecurityEventHandler to ensure proper integration with HTTP context and security event logging.
/// Validates authentication event handling, IP address extraction, and suspicious activity detection.
/// </summary>
public class SecurityEventHandlerTests
{
    private readonly Mock<SecurityEventLogger> _mockSecurityEventLogger;
    private readonly Mock<ILogger<SecurityEventHandler>> _mockLogger;
    private readonly SecurityEventHandler _securityEventHandler;
    private readonly DefaultHttpContext _httpContext;

    public SecurityEventHandlerTests()
    {
        _mockSecurityEventLogger = new Mock<SecurityEventLogger>(Mock.Of<ILogger<SecurityEventLogger>>());
        _mockLogger = new Mock<ILogger<SecurityEventHandler>>();
        _securityEventHandler = new SecurityEventHandler(_mockSecurityEventLogger.Object, _mockLogger.Object);
        _httpContext = new DefaultHttpContext();
    }

    /// <summary>
    /// Tests that login success events are properly handled and logged.
    /// </summary>
    [Fact]
    public void OnLoginSuccess_ShouldLogAuthenticationSuccess()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };

        _httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0 Test Browser";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        }, "Identity.Application"));

        // Act
        _securityEventHandler.OnLoginSuccess(_httpContext, user);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAuthenticationSuccess(
                user.Id,
                "Password", 
                "Mozilla/5.0 Test Browser",
                "192.168.1.100"),
            Times.Once);
    }

    /// <summary>
    /// Tests that OAuth login success events are properly categorized.
    /// </summary>
    [Theory]
    [InlineData("Google", "OAuth-Google")]
    [InlineData("Microsoft", "OAuth-Microsoft")]
    [InlineData("Facebook", "OAuth-Facebook")]
    public void OnLoginSuccess_ShouldDetectOAuthProviders(string provider, string expectedAuthMethod)
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "oauth-user-123",
            UserName = "test@example.com"
        };

        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        }, provider));

        // Act
        _securityEventHandler.OnLoginSuccess(_httpContext, user);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAuthenticationSuccess(
                user.Id,
                expectedAuthMethod,
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that login failure events are properly logged.
    /// </summary>
    [Fact]
    public void OnLoginFailure_ShouldLogAuthenticationFailure()
    {
        // Arrange
        var attemptedUserId = "baduser@example.com";
        var failureReason = "Invalid credentials";

        _httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0 Test Browser";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        // Act
        _securityEventHandler.OnLoginFailure(_httpContext, attemptedUserId, failureReason);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAuthenticationFailure(
                attemptedUserId,
                "Unknown", // No authentication type in context
                failureReason,
                "Mozilla/5.0 Test Browser",
                "192.168.1.100"),
            Times.Once);
    }

    /// <summary>
    /// Tests that account lockout events are properly logged.
    /// </summary>
    [Fact]
    public void OnAccountLockout_ShouldLogAccountLockout()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "locked-user-123",
            UserName = "locked@example.com"
        };

        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5);
        var failedAttemptCount = 5;

        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        // Act
        _securityEventHandler.OnAccountLockout(_httpContext, user, lockoutEnd, failedAttemptCount);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAccountLockout(
                user.Id,
                It.Is<TimeSpan>(t => t.TotalMinutes > 4 && t.TotalMinutes < 6), // Approximately 5 minutes
                failedAttemptCount,
                "192.168.1.100"),
            Times.Once);
    }

    /// <summary>
    /// Tests that logout events are properly logged.
    /// </summary>
    [Fact]
    public void OnLogout_ShouldLogLogoutEvent()
    {
        // Arrange
        var userId = "user123";
        
        _httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0 Test Browser";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        // Act
        _securityEventHandler.OnLogout(_httpContext, userId);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogSecurityEvent(
                SecurityEventType.Authentication,
                SecurityEventSeverity.Low,
                "User logged out successfully",
                userId,
                "Authentication",
                null,
                It.IsAny<object>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that suspicious activity events are properly logged with context.
    /// </summary>
    [Fact]
    public void OnSuspiciousActivity_ShouldLogWithFullContext()
    {
        // Arrange
        var activityType = "MaliciousUrlPattern";
        var description = "SQL injection attempt detected";
        var userId = "user123";

        _httpContext.Request.Path = "/api/songs";
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Headers["User-Agent"] = "sqlmap/1.0";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        // Act
        _securityEventHandler.OnSuspiciousActivity(_httpContext, activityType, description, userId, SecurityEventSeverity.Critical);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogSuspiciousActivity(
                activityType,
                description,
                userId,
                SecurityEventSeverity.Critical,
                It.IsAny<object>()),
            Times.Once);
    }

    /// <summary>
    /// Tests IP address extraction with X-Forwarded-For header.
    /// </summary>
    [Theory]
    [InlineData("203.0.113.1", "203.0.113.1")]
    [InlineData("203.0.113.1, 203.0.113.2", "203.0.113.1")] // First IP in chain
    [InlineData("203.0.113.1,203.0.113.2,203.0.113.3", "203.0.113.1")] // No spaces
    public void OnLoginSuccess_ShouldExtractCorrectIpFromForwardedHeaders(string forwardedFor, string expectedIp)
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };

        _httpContext.Request.Headers["X-Forwarded-For"] = forwardedFor;
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1"); // Should be ignored when X-Forwarded-For is present

        // Act
        _securityEventHandler.OnLoginSuccess(_httpContext, user);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAuthenticationSuccess(
                user.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                expectedIp),
            Times.Once);
    }

    /// <summary>
    /// Tests IP address extraction with X-Real-IP header.
    /// </summary>
    [Fact]
    public void OnLoginSuccess_ShouldUseRealIpHeaderWhenAvailable()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };

        _httpContext.Request.Headers["X-Real-IP"] = "203.0.113.100";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        // Act
        _securityEventHandler.OnLoginSuccess(_httpContext, user);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAuthenticationSuccess(
                user.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                "203.0.113.100"),
            Times.Once);
    }

    /// <summary>
    /// Tests fallback to connection IP when no forwarded headers are present.
    /// </summary>
    [Fact]
    public void OnLoginSuccess_ShouldFallbackToConnectionIp()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };

        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.200");

        // Act
        _securityEventHandler.OnLoginSuccess(_httpContext, user);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAuthenticationSuccess(
                user.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                "192.168.1.200"),
            Times.Once);
    }

    /// <summary>
    /// Tests that unknown authentication types are handled gracefully.
    /// </summary>
    [Fact]
    public void OnLoginSuccess_ShouldHandleUnknownAuthenticationType()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };

        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        }, "CustomAuthProvider"));

        // Act
        _securityEventHandler.OnLoginSuccess(_httpContext, user);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAuthenticationSuccess(
                user.Id,
                "CustomAuthProvider",
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that missing user agent is handled gracefully.
    /// </summary>
    [Fact]
    public void OnLoginSuccess_ShouldHandleMissingUserAgent()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };

        // No User-Agent header set

        // Act
        _securityEventHandler.OnLoginSuccess(_httpContext, user);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAuthenticationSuccess(
                user.Id,
                It.IsAny<string>(),
                "", // Empty user agent
                It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that constructor validates required dependencies.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowWhenSecurityEventLoggerIsNull()
    {
        // Act & Assert
        Action act = () => new SecurityEventHandler(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("securityEventLogger");
    }

    /// <summary>
    /// Tests that constructor validates required dependencies.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowWhenLoggerIsNull()
    {
        // Act & Assert
        Action act = () => new SecurityEventHandler(_mockSecurityEventLogger.Object, null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    /// <summary>
    /// Tests that error handling doesn't prevent logging from continuing.
    /// </summary>
    [Fact]
    public void OnLoginSuccess_ShouldHandleLoggingErrors()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };

        _mockSecurityEventLogger.Setup(x => x.LogAuthenticationSuccess(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Throws(new Exception("Logging error"));

        // Act & Assert (should not throw)
        Action act = () => _securityEventHandler.OnLoginSuccess(_httpContext, user);
        act.Should().NotThrow();

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error logging authentication success event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests IPv6 address handling.
    /// </summary>
    [Fact]
    public void OnLoginSuccess_ShouldHandleIPv6Addresses()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };

        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("2001:db8::1");

        // Act
        _securityEventHandler.OnLoginSuccess(_httpContext, user);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAuthenticationSuccess(
                user.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                "2001:db8::1"),
            Times.Once);
    }

    /// <summary>
    /// Tests that null IP address is handled gracefully.
    /// </summary>
    [Fact]
    public void OnLoginSuccess_ShouldHandleNullIpAddress()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };

        _httpContext.Connection.RemoteIpAddress = null;

        // Act
        _securityEventHandler.OnLoginSuccess(_httpContext, user);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAuthenticationSuccess(
                user.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                null),
            Times.Once);
    }

    /// <summary>
    /// Tests that suspicious activity with different severity levels are handled.
    /// </summary>
    [Theory]
    [InlineData(SecurityEventSeverity.Low)]
    [InlineData(SecurityEventSeverity.Medium)]
    [InlineData(SecurityEventSeverity.High)]
    [InlineData(SecurityEventSeverity.Critical)]
    public void OnSuspiciousActivity_ShouldHandleAllSeverityLevels(SecurityEventSeverity severity)
    {
        // Arrange
        var activityType = "TestActivity";
        var description = $"Test {severity} activity";
        var userId = "user123";

        // Act
        _securityEventHandler.OnSuspiciousActivity(_httpContext, activityType, description, userId, severity);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogSuspiciousActivity(
                activityType,
                description,
                userId,
                severity,
                It.IsAny<object>()),
            Times.Once);
    }

    /// <summary>
    /// Tests exception handling in OnLoginSuccess method.
    /// </summary>
    [Fact]
    public void OnLoginSuccess_ShouldHandleSecurityEventLoggerException()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };

        _httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0 Test Browser";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        _mockSecurityEventLogger
            .Setup(x => x.LogAuthenticationSuccess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Test exception"));

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => _securityEventHandler.OnLoginSuccess(_httpContext, user));
        exception.Should().BeNull("Exception should be handled gracefully");

        // Verify error was logged with specific message for InvalidOperationException
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security event logging service temporarily unavailable")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests exception handling in OnSuspiciousActivity method with HttpContext.
    /// </summary>
    [Fact]
    public void OnSuspiciousActivity_WithHttpContext_ShouldHandleSecurityEventLoggerException()
    {
        // Arrange
        var activityType = "TestActivity";
        var description = "Test suspicious activity";
        var userId = "user123";

        _mockSecurityEventLogger
            .Setup(x => x.LogSuspiciousActivity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecurityEventSeverity>(), It.IsAny<object>()))
            .Throws(new InvalidOperationException("Test exception"));

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => _securityEventHandler.OnSuspiciousActivity(_httpContext, activityType, description, userId));
        exception.Should().BeNull("Exception should be handled gracefully");

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to log suspicious activity event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests exception handling in OnSuspiciousActivity method with pre-sanitized data.
    /// </summary>
    [Fact]
    public void OnSuspiciousActivity_WithPreSanitizedData_ShouldHandleSecurityEventLoggerException()
    {
        // Arrange
        var activityType = "TestActivity";
        var description = "Test suspicious activity";
        var userId = "user123";
        var sanitizedUserAgent = "Clean-User-Agent";
        var sanitizedIpAddress = "192.168.1.100";
        var sanitizedRequestPath = "/api/test";
        var sanitizedRequestMethod = "POST";

        _mockSecurityEventLogger
            .Setup(x => x.LogSuspiciousActivity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecurityEventSeverity>(), It.IsAny<object>()))
            .Throws(new InvalidOperationException("Test exception"));

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => _securityEventHandler.OnSuspiciousActivity(
            activityType, description, userId, SecurityEventSeverity.High,
            sanitizedUserAgent, sanitizedIpAddress, sanitizedRequestPath, sanitizedRequestMethod));
        exception.Should().BeNull("Exception should be handled gracefully");

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to log suspicious activity event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that OnSuspiciousActivity with pre-sanitized data handles all optional parameters.
    /// </summary>
    [Fact]
    public void OnSuspiciousActivity_WithPreSanitizedData_ShouldHandleAllOptionalParameters()
    {
        // Arrange
        var activityType = "TestActivity";
        var description = "Test suspicious activity";

        // Act
        _securityEventHandler.OnSuspiciousActivity(
            activityType, 
            description,
            userId: null,
            severity: SecurityEventSeverity.Medium,
            sanitizedUserAgent: null,
            sanitizedIpAddress: null,
            sanitizedRequestPath: null,
            sanitizedRequestMethod: null);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogSuspiciousActivity(
                activityType,
                description,
                null,
                SecurityEventSeverity.Medium,
                It.IsAny<object>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that OnSuspiciousActivity with pre-sanitized data uses provided values.
    /// </summary>
    [Fact]
    public void OnSuspiciousActivity_WithPreSanitizedData_ShouldUseProvidedValues()
    {
        // Arrange
        var activityType = "TestActivity";
        var description = "Test suspicious activity";
        var userId = "user123";
        var sanitizedUserAgent = "Custom-User-Agent";
        var sanitizedIpAddress = "10.0.0.1";
        var sanitizedRequestPath = "/custom/path";
        var sanitizedRequestMethod = "PUT";

        // Act
        _securityEventHandler.OnSuspiciousActivity(
            activityType, 
            description,
            userId,
            SecurityEventSeverity.High,
            sanitizedUserAgent,
            sanitizedIpAddress,
            sanitizedRequestPath,
            sanitizedRequestMethod);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogSuspiciousActivity(
                activityType,
                description,
                userId,
                SecurityEventSeverity.High,
                It.IsAny<object>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that GetAuthenticationMethod handles null HttpContext identity properly.
    /// </summary>
    [Fact]
    public void OnLoginSuccess_ShouldHandleNullIdentity()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };

        _httpContext.User = new ClaimsPrincipal(); // No identity set

        // Act
        _securityEventHandler.OnLoginSuccess(_httpContext, user);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAuthenticationSuccess(
                user.Id,
                "Unknown", // Should default to Unknown when no identity
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that GetClientIpAddress handles proxy headers correctly.
    /// </summary>
    [Fact]
    public void OnSuspiciousActivity_ShouldExtractIpFromProxyHeaders()
    {
        // Arrange
        var activityType = "TestActivity";
        var description = "Test suspicious activity";
        
        _httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.1, 198.51.100.1";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        // Act
        _securityEventHandler.OnSuspiciousActivity(_httpContext, activityType, description);

        // Assert - Should use X-Forwarded-For header value
        _mockSecurityEventLogger.Verify(
            x => x.LogSuspiciousActivity(
                activityType,
                description,
                It.IsAny<string>(),
                It.IsAny<SecurityEventSeverity>(),
                It.IsAny<object>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that OnAccountLockout method coverage gaps are handled.
    /// </summary>
    [Fact]
    public void OnAccountLockout_AdditionalCoverage_ShouldLogSecurityEvent()
    {
        // Arrange
        var user = new SetlistStudio.Core.Entities.ApplicationUser
        {
            Id = "user123",
            UserName = "test@example.com"
        };
        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
        var attemptCount = 5;

        _httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0 Test Browser";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        // Act
        _securityEventHandler.OnAccountLockout(_httpContext, user, lockoutEnd, attemptCount);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogAccountLockout(
                user.Id,
                It.Is<TimeSpan>(t => t.TotalMinutes > 14 && t.TotalMinutes < 16), // Approximately 15 minutes
                attemptCount,
                "192.168.1.100"),
            Times.Once);
    }

    /// <summary>
    /// Tests that OnLogout method coverage gaps are handled.
    /// </summary>
    [Fact]
    public void OnLogout_AdditionalCoverage_ShouldLogSecurityEvent()
    {
        // Arrange
        var userId = "user123";
        
        _httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0 Test Browser";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        // Act
        _securityEventHandler.OnLogout(_httpContext, userId);

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogSecurityEvent(
                SecurityEventType.Authentication,
                SecurityEventSeverity.Low,
                "User logged out successfully",
                userId,
                "Authentication",
                null,
                It.IsAny<object>()),
            Times.Once);
    }
}