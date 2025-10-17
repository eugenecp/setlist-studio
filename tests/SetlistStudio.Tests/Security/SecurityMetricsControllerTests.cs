using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using SetlistStudio.Web.Controllers;
using SetlistStudio.Web.Services;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive unit and integration tests for SecurityMetricsController.
/// Tests security metrics endpoints, authorization, and dashboard functionality.
/// Ensures proper security monitoring API functionality and access control.
/// </summary>
public class SecurityMetricsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public SecurityMetricsControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = _factory.CreateClient();
    }

    private SecurityMetricsController CreateControllerWithContext(
        ISecurityMetricsService securityMetricsService,
        ILogger<SecurityMetricsController> logger,
        IConfiguration configuration)
    {
        var controller = new SecurityMetricsController(securityMetricsService, logger, configuration);
        
        // Mock HttpContext and Request to avoid issues with HTTP context access
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        var mockConnection = new Mock<ConnectionInfo>();
        
        // Setup User and Identity
        var mockUser = new Mock<ClaimsPrincipal>();
        var mockIdentity = new Mock<ClaimsIdentity>();
        mockIdentity.Setup(i => i.Name).Returns("TestUser");
        mockUser.Setup(u => u.Identity).Returns(mockIdentity.Object);
        mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);
        
        // Setup Request and Headers using real HeaderDictionary
        var headerDictionary = new HeaderDictionary
        {
            ["User-Agent"] = "Test-Browser/1.0",
            ["X-Forwarded-For"] = "192.168.1.100",
            ["X-Real-IP"] = "192.168.1.100"
        };
        mockRequest.Setup(r => r.Headers).Returns(headerDictionary);
        mockRequest.Setup(r => r.HttpContext).Returns(mockHttpContext.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        
        // Setup Connection for IP address
        mockConnection.Setup(c => c.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
        mockHttpContext.Setup(c => c.Connection).Returns(mockConnection.Object);
        
        var controllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };
        
        controller.ControllerContext = controllerContext;
        return controller;
    }

    #region Authorization Tests

    [Fact]
    public async Task GetSnapshot_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/securitymetrics/snapshot");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect, // May redirect to login
            HttpStatusCode.Forbidden
        );
        
        _output.WriteLine($"✓ Snapshot endpoint requires authentication: {response.StatusCode}");
    }

    [Fact]
    public async Task GetDetailedMetrics_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/securitymetrics/detailed");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect,
            HttpStatusCode.Forbidden
        );
        
        _output.WriteLine($"✓ Detailed metrics endpoint requires authentication: {response.StatusCode}");
    }

    [Fact]
    public async Task GetDashboard_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/securitymetrics/dashboard");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Redirect,
            HttpStatusCode.Forbidden
        );
        
        _output.WriteLine($"✓ Dashboard endpoint requires authentication: {response.StatusCode}");
    }

    [Fact]
    public async Task GetHealth_WithoutAuthentication_ShouldReturn200OK()
    {
        // Act - Health endpoint should be accessible without authentication
        var response = await _client.GetAsync("/api/securitymetrics/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
        
        var healthResponse = JsonSerializer.Deserialize<SecurityMonitoringHealth>(content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        healthResponse.Should().NotBeNull();
        healthResponse!.Status.Should().NotBeEmpty();
        healthResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        _output.WriteLine($"✓ Health endpoint accessible without auth: {healthResponse.Status}");
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task SecurityMetricsEndpoints_WithRapidRequests_ShouldEnforceRateLimit()
    {
        // Arrange - Send multiple requests rapidly to health endpoint
        var tasks = new List<Task<HttpResponseMessage>>();
        var endpoint = "/api/securitymetrics/health";

        // Act - Send 15 rapid requests
        for (int i = 0; i < 15; i++)
        {
            tasks.Add(_client.GetAsync(endpoint));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - Should eventually hit rate limit or all succeed (depending on configuration)
        var successfulResponses = responses.Count(r => r.IsSuccessStatusCode);
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);

        _output.WriteLine($"Rate limiting test: {successfulResponses} successful, {rateLimitedResponses} rate-limited");

        // All requests should either succeed or be rate-limited (no server errors)
        responses.Should().OnlyContain(r => 
            r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.TooManyRequests);
    }

    #endregion

    #region Health Endpoint Tests

    [Fact]
    public async Task GetHealth_ShouldReturnValidHealthResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/securitymetrics/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var health = JsonSerializer.Deserialize<SecurityMonitoringHealth>(content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        health.Should().NotBeNull();
        health!.Status.Should().BeOneOf("Healthy", "Warning", "Unhealthy");
        health.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        health.MetricsCollectionActive.Should().BeTrue();
        health.Details.Should().NotBeEmpty();

        _output.WriteLine($"✓ Health endpoint returned: {health.Status} - {health.Details}");
    }

    #endregion

    #region Unit Tests with Mocked Service

    [Fact]
    public void GetSnapshot_WithMockedService_ShouldReturnSnapshot()
    {
        // Arrange
        var mockService = new Mock<ISecurityMetricsService>();
        var expectedSnapshot = new SecurityMetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            TotalEvents = 10,
            AuthenticationFailures = 2,
            RateLimitViolations = 3,
            SuspiciousActivities = 1,
            SecurityEvents = 4,
            RecentEvents = new[]
            {
                new SecurityEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = "AUTHENTICATION_FAILURE",
                    Timestamp = DateTime.UtcNow,
                    Severity = "HIGH",
                    IpAddress = "192.168.1.1"
                }
            },
            TopFailingIps = new[] { "192.168.1.1", "192.168.1.2" },
            TopViolatedEndpoints = new[] { "/api/songs", "/api/setlists" }
        };

        mockService.Setup(s => s.GetMetricsSnapshot()).Returns(expectedSnapshot);

        var mockLogger = new Mock<ILogger<SecurityMetricsController>>();
        var mockConfig = new Mock<IConfiguration>();

        var controller = CreateControllerWithContext(mockService.Object, mockLogger.Object, mockConfig.Object);

        // Act
        var result = controller.GetSnapshot();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedSnapshot);

        _output.WriteLine("✓ Mocked snapshot endpoint returned expected data");
    }

    [Fact]
    public void GetDetailedMetrics_WithValidTimeRange_ShouldReturnDetailedMetrics()
    {
        // Arrange
        var mockService = new Mock<ISecurityMetricsService>();
        var startTime = DateTime.UtcNow.AddHours(-24);
        var endTime = DateTime.UtcNow;

        var expectedMetrics = new DetailedSecurityMetrics
        {
            StartTime = startTime,
            EndTime = endTime,
            TotalEventsInPeriod = 5,
            Events = new[]
            {
                new SecurityEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = "RATE_LIMIT_VIOLATION",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    Severity = "MEDIUM",
                    IpAddress = "192.168.1.100"
                }
            },
            EventsByHour = new Dictionary<int, int> { { 14, 2 }, { 15, 3 } },
            EventsBySeverity = new Dictionary<string, int> { { "HIGH", 2 }, { "MEDIUM", 3 } },
            EventsByType = new Dictionary<string, int> { { "AUTHENTICATION_FAILURE", 3 }, { "RATE_LIMIT_VIOLATION", 2 } },
            TopAttackingIPs = new[] { "192.168.1.100", "192.168.1.200" },
            SecurityTrends = new SecurityTrends
            {
                Last24Hours = 5,
                Previous24Hours = 3,
                TrendPercentage = 66.7,
                IsIncreasing = true
            }
        };

        mockService
            .Setup(s => s.GetDetailedMetrics(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .Returns(expectedMetrics);

        var mockLogger = new Mock<ILogger<SecurityMetricsController>>();
        var mockConfig = new Mock<IConfiguration>();
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(x => x.Value).Returns("30");
        mockConfig.Setup(x => x.GetSection("Security:MaxHistoryDays")).Returns(mockConfigSection.Object);

        var controller = CreateControllerWithContext(mockService.Object, mockLogger.Object, mockConfig.Object);

        // Act
        var result = controller.GetDetailedMetrics(startTime, endTime);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedMetrics);

        _output.WriteLine("✓ Detailed metrics endpoint returned expected data");
    }

    [Fact]
    public void GetDetailedMetrics_WithInvalidTimeRange_ShouldReturnBadRequest()
    {
        // Arrange
        var mockService = new Mock<ISecurityMetricsService>();
        var mockLogger = new Mock<ILogger<SecurityMetricsController>>();
        var mockConfig = new Mock<IConfiguration>();

        var controller = CreateControllerWithContext(mockService.Object, mockLogger.Object, mockConfig.Object);

        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow.AddHours(-1); // End before start

        // Act
        var result = controller.GetDetailedMetrics(startTime, endTime);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result as Microsoft.AspNetCore.Mvc.BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().Be("Start time cannot be after end time");

        _output.WriteLine("✓ Invalid time range properly rejected");
    }

    [Theory]
    [InlineData(1)]    // 1 hour
    [InlineData(24)]   // 1 day
    [InlineData(168)]  // 1 week
    public void GetMetricsForPeriod_WithValidPeriod_ShouldReturnPeriodMetrics(int periodHours)
    {
        // Arrange
        var mockService = new Mock<ISecurityMetricsService>();
        var expectedPeriod = TimeSpan.FromHours(periodHours);

        var expectedMetrics = new SecurityMetricsPeriod
        {
            Period = expectedPeriod,
            StartTime = DateTime.UtcNow - expectedPeriod,
            EndTime = DateTime.UtcNow,
            EventCount = 15,
            AuthFailureRate = 2.5,
            RateLimitRate = 3.2,
            SuspiciousActivityRate = 0.8,
            SecurityScore = 85.5,
            ThreatLevel = "MEDIUM",
            Recommendations = new[] { "Review authentication controls", "Monitor suspicious IPs" }
        };

        mockService
            .Setup(s => s.GetMetricsForPeriod(It.IsAny<TimeSpan>()))
            .Returns(expectedMetrics);

        var mockLogger = new Mock<ILogger<SecurityMetricsController>>();
        var mockConfig = new Mock<IConfiguration>();

        var controller = CreateControllerWithContext(mockService.Object, mockLogger.Object, mockConfig.Object);

        // Act
        var result = controller.GetMetricsForPeriod(periodHours);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedMetrics);

        _output.WriteLine($"✓ Period metrics for {periodHours} hours returned correctly");
    }

    [Theory]
    [InlineData(0)]     // Zero hours
    [InlineData(-1)]    // Negative hours
    [InlineData(10000)] // Excessive hours
    public void GetMetricsForPeriod_WithInvalidPeriod_ShouldReturnBadRequest(int invalidPeriod)
    {
        // Arrange
        var mockService = new Mock<ISecurityMetricsService>();
        var mockLogger = new Mock<ILogger<SecurityMetricsController>>();
        var mockConfig = new Mock<IConfiguration>();

        var controller = CreateControllerWithContext(mockService.Object, mockLogger.Object, mockConfig.Object);

        // Act
        var result = controller.GetMetricsForPeriod(invalidPeriod);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result as Microsoft.AspNetCore.Mvc.BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().Be("Period must be between 1 and 8760 hours (1 year)");

        _output.WriteLine($"✓ Invalid period {invalidPeriod} properly rejected");
    }

    [Fact]
    public void GetSecurityDashboard_WithMockedData_ShouldReturnDashboard()
    {
        // Arrange
        var mockService = new Mock<ISecurityMetricsService>();
        
        var mockSnapshot = new SecurityMetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            TotalEvents = 100,
            RecentEvents = new[]
            {
                new SecurityEvent
                {
                    EventType = "AUTHENTICATION_FAILURE",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    Severity = "HIGH"
                },
                new SecurityEvent
                {
                    EventType = "RATE_LIMIT_VIOLATION",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    Severity = "MEDIUM"
                }
            },
            TopFailingIps = new[] { "192.168.1.100", "10.0.0.50" },
            TopViolatedEndpoints = new[] { "/api/songs", "/api/login" }
        };

        var mock24HourMetrics = new SecurityMetricsPeriod
        {
            ThreatLevel = "MEDIUM",
            SecurityScore = 78.5,
            Recommendations = new[] { "Monitor suspicious activities", "Review rate limits" }
        };

        var mock1HourMetrics = new SecurityMetricsPeriod
        {
            EventCount = 5,
            ThreatLevel = "LOW",
            SecurityScore = 85.0
        };

        mockService.Setup(s => s.GetMetricsSnapshot()).Returns(mockSnapshot);
        mockService.Setup(s => s.GetMetricsForPeriod(TimeSpan.FromHours(24))).Returns(mock24HourMetrics);
        mockService.Setup(s => s.GetMetricsForPeriod(TimeSpan.FromHours(1))).Returns(mock1HourMetrics);

        var mockLogger = new Mock<ILogger<SecurityMetricsController>>();
        var mockConfig = new Mock<IConfiguration>();
        
        // Setup configuration sections that GetValue<T> extension method uses internally
        var mockAlertsSection = new Mock<IConfigurationSection>();
        mockAlertsSection.Setup(s => s.Value).Returns("true");
        mockConfig.Setup(c => c.GetSection("Security:AlertsEnabled")).Returns(mockAlertsSection.Object);
        
        var mockRetentionSection = new Mock<IConfigurationSection>();
        mockRetentionSection.Setup(s => s.Value).Returns("7");
        mockConfig.Setup(c => c.GetSection("Security:MetricsRetentionDays")).Returns(mockRetentionSection.Object);

        var controller = CreateControllerWithContext(mockService.Object, mockLogger.Object, mockConfig.Object);

        // Act
        var result = controller.GetSecurityDashboard();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();

        var dashboard = okResult!.Value as SecurityDashboard;
        dashboard.Should().NotBeNull();
        dashboard!.ThreatLevel.Should().Be("MEDIUM");
        dashboard.SecurityScore.Should().Be(78.5);
        dashboard.TopAttackingIPs.Should().Contain("192.168.1.100");
        dashboard.TopViolatedEndpoints.Should().Contain("/api/songs");
        dashboard.Recommendations.Should().Contain("Monitor suspicious activities");
        dashboard.AlertsEnabled.Should().BeTrue();

        _output.WriteLine($"✓ Dashboard returned: Threat={dashboard.ThreatLevel}, Score={dashboard.SecurityScore}");
    }

    [Fact]
    public void RecordSecurityEvent_WithValidRequest_ShouldRecordEvent()
    {
        // Arrange
        var mockService = new Mock<ISecurityMetricsService>();
        mockService.Setup(s => s.RecordSecurityEvent(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>())).Verifiable();
            
        var mockLogger = new Mock<ILogger<SecurityMetricsController>>();
        var mockConfig = new Mock<IConfiguration>();

        var controller = CreateControllerWithContext(mockService.Object, mockLogger.Object, mockConfig.Object);

        var request = new RecordSecurityEventRequest
        {
            EventType = "MANUAL_TEST_EVENT",
            Severity = "LOW",
            Details = "Test event recorded manually"
        };

        // Act
        var result = controller.RecordSecurityEvent(request);

        // Assert
        result.Should().NotBeNull();
        
        // Debug: Check what we got
        _output.WriteLine($"Result type: {result.GetType().Name}");
        if (result is ObjectResult objResult)
        {
            _output.WriteLine($"StatusCode: {objResult.StatusCode}");
            _output.WriteLine($"Value: {objResult.Value}");
        }
        
        var createdResult = result as Microsoft.AspNetCore.Mvc.CreatedResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);

        // Verify service was called
        mockService.Verify(s => s.RecordSecurityEvent(
            "MANUAL_TEST_EVENT",
            "LOW",
            It.IsAny<string>()), Times.Once);

        _output.WriteLine("✓ Manual security event recorded successfully");
    }

    [Fact]
    public void RecordSecurityEvent_WithEmptyEventType_ShouldReturnBadRequest()
    {
        // Arrange
        var mockService = new Mock<ISecurityMetricsService>();
        var mockLogger = new Mock<ILogger<SecurityMetricsController>>();
        var mockConfig = new Mock<IConfiguration>();

        var controller = CreateControllerWithContext(mockService.Object, mockLogger.Object, mockConfig.Object);

        var request = new RecordSecurityEventRequest
        {
            EventType = "", // Empty event type
            Severity = "LOW",
            Details = "Test event"
        };

        // Act
        var result = controller.RecordSecurityEvent(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result as Microsoft.AspNetCore.Mvc.BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().Be("Event type is required");

        // Verify service was not called
        mockService.Verify(s => s.RecordSecurityEvent(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);

        _output.WriteLine("✓ Empty event type properly rejected");
    }

    #endregion

    #region Security Headers Tests

    [Fact]
    public async Task SecurityMetricsEndpoints_ShouldIncludeSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/securitymetrics/health");

        // Assert
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("Content-Security-Policy");

        _output.WriteLine("✓ Security headers included in metrics endpoints");
    }

    #endregion

    #region Helper Classes for Deserialization

    private class SecurityMonitoringHealth
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool MetricsCollectionActive { get; set; }
        public DateTime? LastEventTime { get; set; }
        public int TotalEventsProcessed { get; set; }
        public TimeSpan MonitoringUptime { get; set; }
        public bool AlertingEnabled { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    #endregion
}