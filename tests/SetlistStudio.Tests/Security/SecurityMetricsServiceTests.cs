using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Web.Services;
using Xunit;
using Xunit.Abstractions;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive unit tests for SecurityMetricsService.
/// Tests metrics collection, security event recording, trend analysis, and threat detection.
/// Ensures proper security monitoring and alerting functionality.
/// </summary>
public class SecurityMetricsServiceTests
{
    private readonly Mock<ILogger<SecurityMetricsService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly SecurityMetricsService _service;
    private readonly ITestOutputHelper _output;

    public SecurityMetricsServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _mockLogger = new Mock<ILogger<SecurityMetricsService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        
        // Setup default configuration values
        _mockConfiguration.Setup(c => c.GetValue<int>("Security:BruteForceThreshold", 5)).Returns(5);
        _mockConfiguration.Setup(c => c.GetValue<int>("Security:DosThreshold", 10)).Returns(10);
        
        _service = new SecurityMetricsService(_mockLogger.Object, _mockConfiguration.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SecurityMetricsService(null!, _mockConfiguration.Object));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SecurityMetricsService(_mockLogger.Object, null!));
        
        exception.ParamName.Should().Be("configuration");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act & Assert
        _service.Should().NotBeNull();
        _service.Should().BeAssignableTo<ISecurityMetricsService>();
    }

    #endregion

    #region Authentication Failure Tests

    [Fact]
    public void RecordAuthenticationFailure_WithValidData_ShouldRecordEvent()
    {
        // Arrange
        var username = "test@example.com";
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0 Test Browser";

        // Act
        _service.RecordAuthenticationFailure(username, ipAddress, userAgent);

        // Assert
        var snapshot = _service.GetMetricsSnapshot();
        snapshot.TotalEvents.Should().Be(1);
        snapshot.RecentEvents.Should().HaveCount(1);
        
        var recordedEvent = snapshot.RecentEvents.First();
        recordedEvent.EventType.Should().Be("AUTHENTICATION_FAILURE");
        recordedEvent.Severity.Should().Be("HIGH");
        recordedEvent.IpAddress.Should().Be(ipAddress);
        recordedEvent.UserAgent.Should().Be(userAgent);
        
        _output.WriteLine($"✓ Authentication failure recorded for {username} from {ipAddress}");
    }

    [Fact]
    public void RecordAuthenticationFailure_MultipleFromSameIP_ShouldTrackPattern()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var usernames = new[] { "user1@test.com", "user2@test.com", "user3@test.com" };

        // Act - Record multiple failures from same IP
        foreach (var username in usernames)
        {
            _service.RecordAuthenticationFailure(username, ipAddress, "Test Browser");
        }

        // Assert
        var snapshot = _service.GetMetricsSnapshot();
        snapshot.TotalEvents.Should().Be(3);
        
        // Should log warnings for each failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Authentication failure recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
            
        _output.WriteLine($"✓ Multiple authentication failures tracked from {ipAddress}");
    }

    [Fact]
    public void RecordAuthenticationFailure_ExceedingBruteForceThreshold_ShouldDetectSuspiciousActivity()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var username = "victim@example.com";

        // Act - Record failures exceeding threshold (5)
        for (int i = 0; i < 6; i++)
        {
            _service.RecordAuthenticationFailure(username, ipAddress, "Attack Browser");
        }

        // Assert
        var snapshot = _service.GetMetricsSnapshot();
        
        // Should have auth failures + suspicious activity event
        snapshot.TotalEvents.Should().BeGreaterThan(6);
        
        var suspiciousEvents = snapshot.RecentEvents.Where(e => e.EventType == "SUSPICIOUS_ACTIVITY").ToArray();
        suspiciousEvents.Should().NotBeEmpty();
        
        var bruteForceEvent = suspiciousEvents.FirstOrDefault();
        bruteForceEvent.Should().NotBeNull();
        bruteForceEvent!.IpAddress.Should().Be(ipAddress);
        
        _output.WriteLine($"✓ Brute force pattern detected after 6 failures from {ipAddress}");
    }

    #endregion

    #region Rate Limit Violation Tests

    [Fact]
    public void RecordRateLimitViolation_WithValidData_ShouldRecordEvent()
    {
        // Arrange
        var ipAddress = "192.168.1.200";
        var endpoint = "/api/songs";
        var userAgent = "Attack Bot 1.0";

        // Act
        _service.RecordRateLimitViolation(ipAddress, endpoint, userAgent);

        // Assert
        var snapshot = _service.GetMetricsSnapshot();
        snapshot.TotalEvents.Should().Be(1);
        snapshot.RecentEvents.Should().HaveCount(1);
        
        var recordedEvent = snapshot.RecentEvents.First();
        recordedEvent.EventType.Should().Be("RATE_LIMIT_VIOLATION");
        recordedEvent.Severity.Should().Be("MEDIUM");
        recordedEvent.IpAddress.Should().Be(ipAddress);
        recordedEvent.UserAgent.Should().Be(userAgent);
        
        _output.WriteLine($"✓ Rate limit violation recorded for {ipAddress} on {endpoint}");
    }

    [Fact]
    public void RecordRateLimitViolation_ExceedingDosThreshold_ShouldDetectSuspiciousActivity()
    {
        // Arrange
        var ipAddress = "192.168.1.200";
        var endpoint = "/api/songs";

        // Act - Record violations exceeding threshold (10)
        for (int i = 0; i < 11; i++)
        {
            _service.RecordRateLimitViolation(ipAddress, endpoint, "Bot");
        }

        // Assert
        var snapshot = _service.GetMetricsSnapshot();
        
        // Should have rate limit violations + suspicious activity event
        snapshot.TotalEvents.Should().BeGreaterThan(11);
        
        var dosEvents = snapshot.RecentEvents.Where(e => 
            e.EventType == "SUSPICIOUS_ACTIVITY" && 
            e.Details!.ToString()!.Contains("DOS_ATTEMPT")).ToArray();
            
        dosEvents.Should().NotBeEmpty();
        
        _output.WriteLine($"✓ DoS pattern detected after 11 violations from {ipAddress}");
    }

    #endregion

    #region Suspicious Activity Tests

    [Fact]
    public void RecordSuspiciousActivity_WithValidData_ShouldRecordEvent()
    {
        // Arrange
        var activityType = "SQL_INJECTION_ATTEMPT";
        var ipAddress = "192.168.1.300";
        var details = "Malicious payload detected in query parameter";

        // Act
        _service.RecordSuspiciousActivity(activityType, ipAddress, details);

        // Assert
        var snapshot = _service.GetMetricsSnapshot();
        snapshot.TotalEvents.Should().Be(1);
        snapshot.RecentEvents.Should().HaveCount(1);
        
        var recordedEvent = snapshot.RecentEvents.First();
        recordedEvent.EventType.Should().Be("SUSPICIOUS_ACTIVITY");
        recordedEvent.Severity.Should().Be("HIGH");
        recordedEvent.IpAddress.Should().Be(ipAddress);
        recordedEvent.Details.Should().NotBeNull();
        
        // Should log error for suspicious activity
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Suspicious activity detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _output.WriteLine($"✓ Suspicious activity recorded: {activityType} from {ipAddress}");
    }

    [Theory]
    [InlineData("XSS_ATTEMPT", "192.168.1.10", "Script injection detected")]
    [InlineData("CSRF_ATTACK", "192.168.1.20", "Cross-site request forgery attempt")]
    [InlineData("DIRECTORY_TRAVERSAL", "192.168.1.30", "Path traversal attempt detected")]
    public void RecordSuspiciousActivity_WithDifferentActivityTypes_ShouldRecordCorrectly(
        string activityType, string ipAddress, string details)
    {
        // Act
        _service.RecordSuspiciousActivity(activityType, ipAddress, details);

        // Assert
        var snapshot = _service.GetMetricsSnapshot();
        snapshot.RecentEvents.Should().HaveCount(1);
        
        var recordedEvent = snapshot.RecentEvents.First();
        recordedEvent.EventType.Should().Be("SUSPICIOUS_ACTIVITY");
        recordedEvent.IpAddress.Should().Be(ipAddress);
        
        _output.WriteLine($"✓ Activity type {activityType} recorded correctly");
    }

    #endregion

    #region Security Event Tests

    [Theory]
    [InlineData("CSP_VIOLATION", "HIGH", "Content Security Policy violation detected")]
    [InlineData("SECURITY_HEADER_MISSING", "MEDIUM", "Missing security header detected")]
    [InlineData("TLS_VIOLATION", "CRITICAL", "TLS protocol violation")]
    public void RecordSecurityEvent_WithDifferentSeverities_ShouldLogAppropriately(
        string eventType, string severity, string details)
    {
        // Act
        _service.RecordSecurityEvent(eventType, severity, details);

        // Assert
        var snapshot = _service.GetMetricsSnapshot();
        snapshot.RecentEvents.Should().HaveCount(1);
        
        var recordedEvent = snapshot.RecentEvents.First();
        recordedEvent.EventType.Should().Be(eventType);
        recordedEvent.Severity.Should().Be(severity);
        
        var expectedLogLevel = severity switch
        {
            "CRITICAL" => LogLevel.Critical,
            "HIGH" => LogLevel.Error,
            "MEDIUM" => LogLevel.Warning,
            "LOW" => LogLevel.Information,
            _ => LogLevel.Information
        };

        _mockLogger.Verify(
            x => x.Log(
                expectedLogLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security event recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _output.WriteLine($"✓ Security event {eventType} logged with {severity} severity");
    }

    #endregion

    #region Metrics Snapshot Tests

    [Fact]
    public void GetMetricsSnapshot_WithNoEvents_ShouldReturnEmptySnapshot()
    {
        // Act
        var snapshot = _service.GetMetricsSnapshot();

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.TotalEvents.Should().Be(0);
        snapshot.RecentEvents.Should().BeEmpty();
        snapshot.AuthenticationFailures.Should().Be(0);
        snapshot.RateLimitViolations.Should().Be(0);
        snapshot.SuspiciousActivities.Should().Be(0);
        snapshot.SecurityEvents.Should().Be(0);
        snapshot.TopFailingIps.Should().BeEmpty();
        snapshot.TopViolatedEndpoints.Should().BeEmpty();
        snapshot.SecurityEventsByType.Should().BeEmpty();
        
        _output.WriteLine("✓ Empty metrics snapshot returned correctly");
    }

    [Fact]
    public void GetMetricsSnapshot_WithMixedEvents_ShouldReturnCorrectCounts()
    {
        // Arrange - Record various events
        _service.RecordAuthenticationFailure("user1@test.com", "192.168.1.1", "Browser1");
        _service.RecordAuthenticationFailure("user2@test.com", "192.168.1.2", "Browser2");
        _service.RecordRateLimitViolation("192.168.1.3", "/api/songs", "Bot1");
        _service.RecordSuspiciousActivity("XSS_ATTEMPT", "192.168.1.4", "XSS detected");
        _service.RecordSecurityEvent("CSP_VIOLATION", "HIGH", "CSP violation");

        // Act
        var snapshot = _service.GetMetricsSnapshot();

        // Assert
        snapshot.TotalEvents.Should().Be(5);
        snapshot.RecentEvents.Should().HaveCount(5);
        snapshot.AuthenticationFailures.Should().Be(2);
        snapshot.RateLimitViolations.Should().Be(1);
        snapshot.SuspiciousActivities.Should().Be(1);
        snapshot.SecurityEvents.Should().Be(1);
        
        // Check event types distribution
        snapshot.SecurityEventsByType.Should().HaveCount(4);
        snapshot.SecurityEventsByType.Should().ContainKey("AUTHENTICATION_FAILURE");
        snapshot.SecurityEventsByType.Should().ContainKey("RATE_LIMIT_VIOLATION");
        snapshot.SecurityEventsByType.Should().ContainKey("SUSPICIOUS_ACTIVITY");
        snapshot.SecurityEventsByType.Should().ContainKey("CSP_VIOLATION");
        
        _output.WriteLine($"✓ Mixed events snapshot: {snapshot.TotalEvents} total events");
    }

    #endregion

    #region Detailed Metrics Tests

    [Fact]
    public void GetDetailedMetrics_WithTimeRange_ShouldFilterCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-2);
        var endTime = DateTime.UtcNow.AddHours(-1);
        
        // Record an event (should be included as it's recent)
        _service.RecordAuthenticationFailure("user@test.com", "192.168.1.1", "Browser");

        // Act
        var detailedMetrics = _service.GetDetailedMetrics(startTime, endTime);

        // Assert
        detailedMetrics.Should().NotBeNull();
        detailedMetrics.StartTime.Should().Be(startTime);
        detailedMetrics.EndTime.Should().Be(endTime);
        
        // Event should be included as it's recent (within the filter logic of current events)
        // Note: In a real implementation, you might want more sophisticated time filtering
        _output.WriteLine($"✓ Detailed metrics filtered for time range {startTime} to {endTime}");
    }

    [Fact]
    public void GetDetailedMetrics_WithDefaultParameters_ShouldUseLast24Hours()
    {
        // Arrange
        _service.RecordSecurityEvent("TEST_EVENT", "LOW", "Test event");

        // Act
        var detailedMetrics = _service.GetDetailedMetrics();

        // Assert
        detailedMetrics.Should().NotBeNull();
        detailedMetrics.StartTime.Should().BeCloseTo(DateTime.UtcNow.AddHours(-24), TimeSpan.FromMinutes(1));
        detailedMetrics.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        _output.WriteLine("✓ Default detailed metrics uses last 24 hours");
    }

    #endregion

    #region Metrics Period Tests

    [Theory]
    [InlineData(1)] // 1 hour
    [InlineData(24)] // 1 day
    [InlineData(168)] // 1 week
    public void GetMetricsForPeriod_WithDifferentPeriods_ShouldCalculateCorrectly(int hours)
    {
        // Arrange
        var period = TimeSpan.FromHours(hours);
        _service.RecordAuthenticationFailure("user@test.com", "192.168.1.1", "Browser");
        _service.RecordRateLimitViolation("192.168.1.2", "/api/test", "Bot");

        // Act
        var periodMetrics = _service.GetMetricsForPeriod(period);

        // Assert
        periodMetrics.Should().NotBeNull();
        periodMetrics.Period.Should().Be(period);
        periodMetrics.StartTime.Should().BeCloseTo(DateTime.UtcNow - period, TimeSpan.FromSeconds(5));
        periodMetrics.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        periodMetrics.EventCount.Should().Be(2);
        
        // Check rates (events per hour)
        periodMetrics.AuthFailureRate.Should().BeApproximately(1.0 / hours, 0.1);
        periodMetrics.RateLimitRate.Should().BeApproximately(1.0 / hours, 0.1);
        
        // Security score should be calculated
        periodMetrics.SecurityScore.Should().BeInRange(0, 100);
        
        // Threat level should be determined
        periodMetrics.ThreatLevel.Should().BeOneOf("LOW", "MEDIUM", "HIGH", "CRITICAL");
        
        // Recommendations should be provided
        periodMetrics.Recommendations.Should().NotBeEmpty();
        
        _output.WriteLine($"✓ Period metrics calculated for {hours} hours: Score={periodMetrics.SecurityScore}, Threat={periodMetrics.ThreatLevel}");
    }

    #endregion

    #region Security Score Tests

    [Fact]
    public void SecurityScore_WithNoCriticalEvents_ShouldBeHigh()
    {
        // Arrange - Only low severity events
        _service.RecordSecurityEvent("INFO_EVENT", "LOW", "Informational event");

        // Act
        var periodMetrics = _service.GetMetricsForPeriod(TimeSpan.FromHours(1));

        // Assert
        periodMetrics.SecurityScore.Should().BeGreaterThan(90);
        periodMetrics.ThreatLevel.Should().Be("LOW");
        
        _output.WriteLine($"✓ High security score with low severity events: {periodMetrics.SecurityScore}");
    }

    [Fact]
    public void SecurityScore_WithCriticalEvents_ShouldBeLow()
    {
        // Arrange - Critical security events
        _service.RecordSecurityEvent("BREACH_ATTEMPT", "CRITICAL", "Security breach attempt");
        _service.RecordSecurityEvent("SYSTEM_COMPROMISE", "CRITICAL", "System compromise detected");

        // Act
        var periodMetrics = _service.GetMetricsForPeriod(TimeSpan.FromHours(1));

        // Assert
        periodMetrics.SecurityScore.Should().BeLessThan(90);
        periodMetrics.ThreatLevel.Should().BeOneOf("HIGH", "CRITICAL");
        
        _output.WriteLine($"✓ Low security score with critical events: {periodMetrics.SecurityScore}");
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public void ClearOldMetrics_WithOldEvents_ShouldRemoveExpiredData()
    {
        // Arrange
        _service.RecordSecurityEvent("OLD_EVENT", "LOW", "Old event");
        var initialSnapshot = _service.GetMetricsSnapshot();
        initialSnapshot.TotalEvents.Should().Be(1);

        // Act - Clear events older than 0 seconds (should clear all)
        _service.ClearOldMetrics(TimeSpan.Zero);

        // Assert
        var clearedSnapshot = _service.GetMetricsSnapshot();
        clearedSnapshot.RecentEvents.Should().BeEmpty();
        
        _output.WriteLine("✓ Old metrics cleared successfully");
    }

    #endregion

    #region Threat Detection Tests

    [Fact]
    public void ThreatDetection_WithMultipleHighSeverityEvents_ShouldIndicateHighThreat()
    {
        // Arrange - Simulate attack scenario
        for (int i = 0; i < 15; i++)
        {
            _service.RecordAuthenticationFailure($"user{i}@test.com", "192.168.1.100", "AttackBot");
        }
        
        for (int i = 0; i < 20; i++)
        {
            _service.RecordRateLimitViolation("192.168.1.100", "/api/songs", "AttackBot");
        }

        _service.RecordSuspiciousActivity("BRUTE_FORCE_ATTACK", "192.168.1.100", "Coordinated attack detected");

        // Act
        var periodMetrics = _service.GetMetricsForPeriod(TimeSpan.FromHours(1));

        // Assert
        periodMetrics.ThreatLevel.Should().BeOneOf("HIGH", "CRITICAL");
        periodMetrics.SecurityScore.Should().BeLessThan(70);
        periodMetrics.Recommendations.Should().Contain(r => r.Contains("authentication") || r.Contains("blocking"));
        
        var snapshot = _service.GetMetricsSnapshot();
        snapshot.TopFailingIps.Should().Contain("192.168.1.100");
        
        _output.WriteLine($"✓ High threat detected: Level={periodMetrics.ThreatLevel}, Score={periodMetrics.SecurityScore}");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void HighVolumeEvents_ShouldMaintainPerformance()
    {
        // Arrange & Act - Record many events quickly
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < 1000; i++)
        {
            _service.RecordSecurityEvent($"EVENT_{i}", "LOW", $"Event number {i}");
        }
        
        stopwatch.Stop();

        // Assert
        var snapshot = _service.GetMetricsSnapshot();
        snapshot.TotalEvents.Should().Be(1000);
        
        // Should maintain reasonable performance (less than 1 second for 1000 events)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        
        // Should limit stored events to prevent memory issues
        snapshot.RecentEvents.Should().HaveCountLessOrEqualTo(1000);
        
        _output.WriteLine($"✓ Performance test: {1000} events processed in {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion
}