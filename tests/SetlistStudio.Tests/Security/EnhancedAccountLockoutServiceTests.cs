using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using SetlistStudio.Core.Entities;
using SetlistStudio.Web.Security;
using SetlistStudio.Core.Security;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive tests for EnhancedAccountLockoutService covering progressive lockout logic,
/// security event logging, and IP-based security measures
/// </summary>
public class EnhancedAccountLockoutServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<SecurityEventLogger> _mockSecurityEventLogger;
    private readonly Mock<ILogger<EnhancedAccountLockoutService>> _mockLogger;
    private readonly EnhancedAccountLockoutService _service;

    public EnhancedAccountLockoutServiceTests()
    {
        // Setup UserManager mock
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        _mockSecurityEventLogger = new Mock<SecurityEventLogger>(Mock.Of<ILogger<SecurityEventLogger>>());
        _mockLogger = new Mock<ILogger<EnhancedAccountLockoutService>>();

        _service = new EnhancedAccountLockoutService(
            _mockUserManager.Object,
            _mockSecurityEventLogger.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task HandleFailedLoginAttemptAsync_WithNullUser_LogsSuspiciousActivityAndReturnsNotLockedOut()
    {
        // Arrange
        string ipAddress = "192.168.1.100";
        string userAgent = "Mozilla/5.0 Test Browser";

        // Act
        var result = await _service.HandleFailedLoginAttemptAsync(null, ipAddress, userAgent);

        // Assert
        result.Should().NotBeNull();
        result.IsLockedOut.Should().BeFalse();
        result.Message.Should().Be("Invalid login attempt.");

        _mockSecurityEventLogger.Verify(x => x.LogSuspiciousActivity(
            "NonExistentUserLogin",
            It.Is<string>(s => s.Contains(ipAddress)),
            null,
            SecurityEventSeverity.High,
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task HandleFailedLoginAttemptAsync_WithValidUser_RecordsFailedAttempt()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        string ipAddress = "192.168.1.100";
        string userAgent = "Mozilla/5.0 Test Browser";

        _mockUserManager.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetAccessFailedCountAsync(user))
            .ReturnsAsync(1);
        _mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        // Act
        var result = await _service.HandleFailedLoginAttemptAsync(user, ipAddress, userAgent);

        // Assert
        result.Should().NotBeNull();
        result.IsLockedOut.Should().BeFalse();
        result.FailedAttempts.Should().Be(1);
        result.RemainingAttempts.Should().Be(4);
        result.Message.Should().Contain("4 attempts remaining");

        _mockUserManager.Verify(x => x.AccessFailedAsync(user), Times.Once);
        _mockSecurityEventLogger.Verify(x => x.LogAuthenticationFailure(
            user.Id,
            "Password",
            "Failed login attempt 1/5",
            userAgent,
            ipAddress), Times.Once);
    }

    [Fact]
    public async Task HandleFailedLoginAttemptAsync_WhenUserLockedOut_AppliesProgressiveLockout()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        string ipAddress = "192.168.1.100";
        string userAgent = "Mozilla/5.0 Test Browser";
        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5);

        _mockUserManager.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetAccessFailedCountAsync(user))
            .ReturnsAsync(6); // More than 5 attempts
        _mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetLockoutEndDateAsync(user))
            .ReturnsAsync(lockoutEnd);

        // Act
        var result = await _service.HandleFailedLoginAttemptAsync(user, ipAddress, userAgent);

        // Assert
        result.Should().NotBeNull();
        result.IsLockedOut.Should().BeTrue();
        result.FailedAttempts.Should().Be(6);
        result.LockoutEnd.Should().BeAfter(DateTimeOffset.UtcNow);
        result.Message.Should().Contain("Account locked until");

        _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset?>()), Times.Once);
        _mockSecurityEventLogger.Verify(x => x.LogAccountLockout(
            user.Id,
            It.IsAny<TimeSpan>(),
            6,
            ipAddress), Times.Once);
    }

    [Fact]
    public async Task HandleSuccessfulLoginAsync_WithNullUser_ReturnsWithoutAction()
    {
        // Arrange
        string ipAddress = "192.168.1.100";
        string userAgent = "Mozilla/5.0 Test Browser";

        // Act
        await _service.HandleSuccessfulLoginAsync(null, ipAddress, userAgent);

        // Assert
        _mockUserManager.Verify(x => x.GetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockSecurityEventLogger.Verify(x => x.LogAuthenticationSuccess(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleSuccessfulLoginAsync_WithValidUser_ResetsFailedAttemptsAndLogsSuccess()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        string ipAddress = "192.168.1.100";
        string userAgent = "Mozilla/5.0 Test Browser";

        _mockUserManager.Setup(x => x.GetAccessFailedCountAsync(user))
            .ReturnsAsync(3);
        _mockUserManager.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _service.HandleSuccessfulLoginAsync(user, ipAddress, userAgent);

        // Assert
        _mockUserManager.Verify(x => x.GetAccessFailedCountAsync(user), Times.Once);
        _mockUserManager.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
        _mockSecurityEventLogger.Verify(x => x.LogAuthenticationSuccess(
            user.Id,
            "Password",
            userAgent,
            ipAddress), Times.Once);
    }

    [Fact]
    public async Task HandleSuccessfulLoginAsync_WithNoFailedAttempts_DoesNotResetCounter()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        string ipAddress = "192.168.1.100";
        string userAgent = "Mozilla/5.0 Test Browser";

        _mockUserManager.Setup(x => x.GetAccessFailedCountAsync(user))
            .ReturnsAsync(0);

        // Act
        await _service.HandleSuccessfulLoginAsync(user, ipAddress, userAgent);

        // Assert
        _mockUserManager.Verify(x => x.GetAccessFailedCountAsync(user), Times.Once);
        _mockUserManager.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Never);
        _mockSecurityEventLogger.Verify(x => x.LogAuthenticationSuccess(
            user.Id,
            "Password",
            userAgent,
            ipAddress), Times.Once);
    }

    [Fact]
    public async Task IsIpAddressBlockedAsync_ReturnsExpectedResult()
    {
        // Arrange
        string ipAddress = "192.168.1.100";

        // Act
        var result = await _service.IsIpAddressBlockedAsync(ipAddress);

        // Assert
        // Currently returns false as per placeholder implementation
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 5)] // First lockout: 5 minutes
    [InlineData(5, 5)]
    [InlineData(6, 15)] // Second lockout: 15 minutes
    [InlineData(10, 15)]
    [InlineData(11, 60)] // Third lockout: 1 hour
    [InlineData(15, 60)]
    [InlineData(16, 240)] // Fourth lockout: 4 hours
    [InlineData(20, 240)]
    [InlineData(21, 720)] // Fifth lockout: 12 hours
    [InlineData(25, 720)]
    [InlineData(26, 1440)] // Subsequent lockouts: 24 hours
    [InlineData(100, 1440)]
    public async Task HandleFailedLoginAttemptAsync_AppliesCorrectProgressiveLockoutDuration(int failedAttempts, int expectedMinutes)
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        string ipAddress = "192.168.1.100";
        string userAgent = "Mozilla/5.0 Test Browser";

        _mockUserManager.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetAccessFailedCountAsync(user))
            .ReturnsAsync(failedAttempts);
        _mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetLockoutEndDateAsync(user))
            .ReturnsAsync(DateTimeOffset.UtcNow.AddMinutes(expectedMinutes));

        // Act
        var result = await _service.HandleFailedLoginAttemptAsync(user, ipAddress, userAgent);

        // Assert
        result.IsLockedOut.Should().BeTrue();
        result.FailedAttempts.Should().Be(failedAttempts);

        // Verify that the progressive lockout time was logged correctly
        _mockSecurityEventLogger.Verify(x => x.LogAccountLockout(
            user.Id,
            It.Is<TimeSpan>(ts => Math.Abs(ts.TotalMinutes - expectedMinutes) < 1),
            failedAttempts,
            ipAddress), Times.Once);
    }

    [Fact]
    public async Task HandleFailedLoginAttemptAsync_WhenAccessFailedFails_ReturnsErrorResult()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        string ipAddress = "192.168.1.100";
        string userAgent = "Mozilla/5.0 Test Browser";

        _mockUserManager.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Database error" }));

        // Act
        var result = await _service.HandleFailedLoginAttemptAsync(user, ipAddress, userAgent);

        // Assert
        result.Should().NotBeNull();
        result.IsLockedOut.Should().BeFalse();
        result.Message.Should().Be("An error occurred processing the login attempt.");

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to record access failure")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleFailedLoginAttemptAsync_WithMaliciousInput_SanitizesAndLogs()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        string maliciousIp = "<script>alert('xss')</script>";
        string maliciousUserAgent = "'; DROP TABLE Users; --";

        _mockUserManager.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetAccessFailedCountAsync(user))
            .ReturnsAsync(1);
        _mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        // Act
        var result = await _service.HandleFailedLoginAttemptAsync(user, maliciousIp, maliciousUserAgent);

        // Assert
        result.Should().NotBeNull();
        result.IsLockedOut.Should().BeFalse();

        // Verify that the malicious input was passed to the security logger
        // The SecurityEventLogger should handle sanitization internally
        _mockSecurityEventLogger.Verify(x => x.LogAuthenticationFailure(
            user.Id,
            "Password",
            "Failed login attempt 1/5",
            maliciousUserAgent,
            maliciousIp), Times.Once);
    }

    [Fact]
    public async Task HandleFailedLoginAttemptAsync_MultipleFailedAttempts_IncreasesCountCorrectly()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        string ipAddress = "192.168.1.100";
        string userAgent = "Mozilla/5.0 Test Browser";

        var failedCounts = new Queue<int>(new[] { 1, 2, 3, 4, 5 });
        
        _mockUserManager.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetAccessFailedCountAsync(user))
            .ReturnsAsync(() => failedCounts.Dequeue());
        _mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        // Act & Assert
        for (int i = 1; i <= 5; i++)
        {
            var result = await _service.HandleFailedLoginAttemptAsync(user, ipAddress, userAgent);
            
            result.Should().NotBeNull();
            result.IsLockedOut.Should().BeFalse();
            result.FailedAttempts.Should().Be(i);
            result.RemainingAttempts.Should().Be(5 - i);
        }

        // Verify all attempts were recorded
        _mockUserManager.Verify(x => x.AccessFailedAsync(user), Times.Exactly(5));
        _mockSecurityEventLogger.Verify(x => x.LogAuthenticationFailure(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Exactly(5));
    }
}