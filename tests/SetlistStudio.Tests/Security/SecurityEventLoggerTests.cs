using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Security;
using Xunit;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Tests for SecurityEventLogger to ensure proper logging of security events with correct formatting and sanitization.
/// Validates that all security event types are logged with appropriate severity levels and data protection.
/// </summary>
public class SecurityEventLoggerTests
{
    private readonly Mock<ILogger<SecurityEventLogger>> _mockLogger;
    private readonly SecurityEventLogger _securityEventLogger;

    public SecurityEventLoggerTests()
    {
        _mockLogger = new Mock<ILogger<SecurityEventLogger>>();
        _securityEventLogger = new SecurityEventLogger(_mockLogger.Object);
    }

    /// <summary>
    /// Tests that authentication success events are logged with correct information.
    /// </summary>
    [Fact]
    public void LogAuthenticationSuccess_ShouldLogWithCorrectInformation()
    {
        // Arrange
        var userId = "user123";
        var authMethod = "Password";
        var userAgent = "Mozilla/5.0 Test Browser";
        var ipAddress = "192.168.1.100";

        // Act
        _securityEventLogger.LogAuthenticationSuccess(userId, authMethod, userAgent, ipAddress);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SecurityEvent_Authentication")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that authentication failure events are logged with medium severity.
    /// </summary>
    [Fact]
    public void LogAuthenticationFailure_ShouldLogWithMediumSeverity()
    {
        // Arrange
        var attemptedUserId = "baduser@example.com";
        var authMethod = "Password";
        var failureReason = "Invalid credentials";
        var userAgent = "Mozilla/5.0 Test Browser";
        var ipAddress = "192.168.1.100";

        // Act
        _securityEventLogger.LogAuthenticationFailure(attemptedUserId, authMethod, failureReason, userAgent, ipAddress);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning, // Medium severity maps to Warning
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SecurityEvent_Authentication")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that authorization failure events are logged with high severity.
    /// </summary>
    [Fact]
    public void LogAuthorizationFailure_ShouldLogWithHighSeverity()
    {
        // Arrange
        var userId = "user123";
        var resourceType = "Song";
        var resourceId = "song456";
        var action = "Delete";

        // Act
        _securityEventLogger.LogAuthorizationFailure(userId, resourceType, resourceId, action);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error, // High severity maps to Error
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SecurityEvent_Authorization")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that authorization success events are logged with low severity.
    /// </summary>
    [Fact]
    public void LogAuthorizationSuccess_ShouldLogWithLowSeverity()
    {
        // Arrange
        var userId = "user123";
        var resourceType = "Setlist";
        var resourceId = "setlist789";
        var action = "Read";

        // Act
        _securityEventLogger.LogAuthorizationSuccess(userId, resourceType, resourceId, action);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information, // Low severity maps to Information
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SecurityEvent_Authorization")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that account lockout events are logged with high severity.
    /// </summary>
    [Fact]
    public void LogAccountLockout_ShouldLogWithHighSeverity()
    {
        // Arrange
        var userId = "user123";
        var lockoutDuration = TimeSpan.FromMinutes(5);
        var failedAttemptCount = 5;
        var ipAddress = "192.168.1.100";

        // Act
        _securityEventLogger.LogAccountLockout(userId, lockoutDuration, failedAttemptCount, ipAddress);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error, // High severity maps to Error
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SecurityEvent_AccountLockout")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that suspicious activity events are logged with appropriate severity.
    /// </summary>
    [Theory]
    [InlineData(SecurityEventSeverity.Low, LogLevel.Information)]
    [InlineData(SecurityEventSeverity.Medium, LogLevel.Warning)]
    [InlineData(SecurityEventSeverity.High, LogLevel.Error)]
    [InlineData(SecurityEventSeverity.Critical, LogLevel.Critical)]
    [InlineData((SecurityEventSeverity)999, LogLevel.Information)] // Test default case for invalid enum values
    public void LogSuspiciousActivity_ShouldMapSeverityToLogLevel(SecurityEventSeverity severity, LogLevel expectedLogLevel)
    {
        // Arrange
        var activityType = "TestActivity";
        var description = "Test suspicious activity";
        var userId = "user123";
        var additionalContext = new { TestData = "test" };

        // Act
        _securityEventLogger.LogSuspiciousActivity(activityType, description, userId, severity, additionalContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                expectedLogLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SecurityEvent_SuspiciousActivity")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that data access events are logged properly.
    /// </summary>
    [Fact]
    public void LogDataAccess_ShouldLogWithCorrectInformation()
    {
        // Arrange
        var userId = "user123";
        var resourceType = "Song";
        var resourceId = "song456";
        var action = "Create";
        var recordCount = 1;

        // Act
        _securityEventLogger.LogDataAccess(userId, resourceType, resourceId, action, recordCount);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information, // Data access is low severity
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SecurityEvent_DataAccess")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that data access events handle null record counts.
    /// </summary>
    [Fact]
    public void LogDataAccess_ShouldHandleNullRecordCount()
    {
        // Arrange
        var userId = "user123";
        var resourceType = "Setlist";
        var resourceId = "setlist789";
        var action = "Read";

        // Act
        _securityEventLogger.LogDataAccess(userId, resourceType, resourceId, action, null);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains("records)")), // Should not include record count
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that validation failure events are logged with appropriate information.
    /// </summary>
    [Theory]
    [InlineData("XSS_Detection", "UserInput", SecurityEventSeverity.High)]
    [InlineData("SQL_Injection", "SearchQuery", SecurityEventSeverity.High)]
    [InlineData("Invalid_Format", "BpmValue", SecurityEventSeverity.Medium)]
    [InlineData("Required_Field", "SongTitle", SecurityEventSeverity.Low)]
    public void LogValidationFailure_ShouldLogWithCorrectSeverity(string validationType, string fieldName, SecurityEventSeverity expectedSeverity)
    {
        // Arrange
        var userId = "user123";
        var additionalContext = new { InputValue = "test input" };

        // Act
        _securityEventLogger.LogValidationFailure(validationType, fieldName, userId, expectedSeverity, additionalContext);

        // Assert
        var expectedLogLevel = expectedSeverity switch
        {
            SecurityEventSeverity.Low => LogLevel.Information,
            SecurityEventSeverity.Medium => LogLevel.Warning,
            SecurityEventSeverity.High => LogLevel.Error,
            SecurityEventSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Information
        };

        _mockLogger.Verify(
            x => x.Log(
                expectedLogLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SecurityEvent_ValidationFailure")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that security events include proper timestamps and correlation IDs.
    /// </summary>
    [Fact]
    public void LogSecurityEvent_ShouldIncludeTimestampAndCorrelationId()
    {
        // Arrange
        var eventType = SecurityEventType.Authentication;
        var severity = SecurityEventSeverity.Medium;
        var message = "Test security event";
        var userId = "user123";

        // Act
        _securityEventLogger.LogSecurityEvent(eventType, severity, message, userId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("SecurityEvent_Authentication") &&
                    v.ToString()!.Contains("Medium") &&
                    v.ToString()!.Contains("Test security event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that sensitive data in security events is properly sanitized.
    /// </summary>
    [Fact]
    public void LogSecurityEvent_ShouldSanitizeSensitiveData()
    {
        // Arrange
        var eventType = SecurityEventType.Authentication;
        var severity = SecurityEventSeverity.High;
        var message = "Login failed with password: secret123";
        var userId = "user@example.com";
        var additionalData = new { Password = "secret123", Token = "jwt.abc.def" };

        // Act
        _securityEventLogger.LogSecurityEvent(eventType, severity, message, userId, "Login", null, additionalData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("[REDACTED]")), // Should contain redacted sensitive data
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that all security event types can be logged without errors.
    /// </summary>
    [Theory]
    [InlineData(SecurityEventType.Authentication)]
    [InlineData(SecurityEventType.Authorization)]
    [InlineData(SecurityEventType.AccountLockout)]
    [InlineData(SecurityEventType.SuspiciousActivity)]
    [InlineData(SecurityEventType.DataAccess)]
    [InlineData(SecurityEventType.ValidationFailure)]
    [InlineData(SecurityEventType.ConfigurationChange)]
    [InlineData(SecurityEventType.TokenManagement)]
    public void LogSecurityEvent_ShouldHandleAllEventTypes(SecurityEventType eventType)
    {
        // Arrange
        var severity = SecurityEventSeverity.Medium;
        var message = $"Test {eventType} event";
        var userId = "user123";

        // Act & Assert (should not throw)
        _securityEventLogger.LogSecurityEvent(eventType, severity, message, userId);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that security events can be logged without optional parameters.
    /// </summary>
    [Fact]
    public void LogSecurityEvent_ShouldHandleNullOptionalParameters()
    {
        // Arrange
        var eventType = SecurityEventType.SuspiciousActivity;
        var severity = SecurityEventSeverity.High;
        var message = "Anonymous suspicious activity";

        // Act
        _securityEventLogger.LogSecurityEvent(eventType, severity, message, null, null, null, null);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anonymous suspicious activity")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that the SecurityEventLogger constructor requires a logger.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowWhenLoggerIsNull()
    {
        // Act & Assert
        Action act = () => new SecurityEventLogger(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    /// <summary>
    /// Tests OAuth authentication success logging.
    /// </summary>
    [Theory]
    [InlineData("Google")]
    [InlineData("Microsoft")]
    [InlineData("Facebook")]
    public void LogAuthenticationSuccess_ShouldHandleOAuthProviders(string oauthProvider)
    {
        // Arrange
        var userId = "oauth-user-123";
        var authMethod = $"OAuth-{oauthProvider}";
        var userAgent = "Mozilla/5.0 Test Browser";
        var ipAddress = "192.168.1.100";

        // Act
        _securityEventLogger.LogAuthenticationSuccess(userId, authMethod, userAgent, ipAddress);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("successfully authenticated") &&
                    v.ToString()!.Contains(oauthProvider)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that large record count data access events are logged correctly.
    /// </summary>
    [Fact]
    public void LogDataAccess_ShouldHandleLargeRecordCounts()
    {
        // Arrange
        var userId = "user123";
        var resourceType = "Song";
        string? resourceId = null; // Bulk operation
        var action = "BulkRead";
        var recordCount = 10000;

        // Act
        _securityEventLogger.LogDataAccess(userId, resourceType, resourceId, action, recordCount);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("BulkRead") &&
                    v.ToString()!.Contains("10000 records")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that invalid severity enum values default to Information log level.
    /// This test covers the default case in the MapSeverityToLogLevel switch expression.
    /// </summary>
    [Fact]
    public void LogSecurityEvent_WithInvalidSeverity_ShouldDefaultToInformationLevel()
    {
        // Arrange
        var eventType = SecurityEventType.Authentication;
        var invalidSeverity = (SecurityEventSeverity)(-1); // Invalid enum value
        var message = "Test security event with invalid severity";
        var userId = "user123";
        var resourceType = "TestResource";
        var resourceId = "resource123";
        var additionalData = new { TestProperty = "TestValue" };

        // Act
        _securityEventLogger.LogSecurityEvent(eventType, invalidSeverity, message, userId, resourceType, resourceId, additionalData);

        // Assert - Should use Information level as default
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SecurityEvent_Authentication")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}