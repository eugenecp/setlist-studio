using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Web.Services;
using Xunit;
using FluentAssertions;
using System.Security.Claims;

namespace SetlistStudio.Tests.Web.Services;

/// <summary>
/// Comprehensive tests for the EnhancedRateLimitingService covering multi-factor rate limiting,
/// CAPTCHA integration, and security monitoring to prevent rate limiting bypass attempts.
/// </summary>
public class EnhancedRateLimitingServiceTests
{
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<EnhancedRateLimitingService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly EnhancedRateLimitingService _service;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpRequest> _mockRequest;
    private readonly Mock<ConnectionInfo> _mockConnection;
    private readonly Mock<ClaimsPrincipal> _mockUser;
    private readonly Mock<ISession> _mockSession;

    public EnhancedRateLimitingServiceTests()
    {
        // Use real MemoryCache for testing instead of mocking it
        var serviceProvider = new ServiceCollection()
            .AddMemoryCache()
            .BuildServiceProvider();
        _memoryCache = serviceProvider.GetService<IMemoryCache>()!;
        
        _mockCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<EnhancedRateLimitingService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockRequest = new Mock<HttpRequest>();
        _mockConnection = new Mock<ConnectionInfo>();
        _mockUser = new Mock<ClaimsPrincipal>();
        _mockSession = new Mock<ISession>();

        // Setup basic configuration
        _mockConfiguration.Setup(x => x["Captcha:SecretKey"]).Returns("test-secret-key");

        // Setup HttpContext hierarchy
        _mockHttpContext.Setup(x => x.Request).Returns(_mockRequest.Object);
        _mockHttpContext.Setup(x => x.Connection).Returns(_mockConnection.Object);
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);
        _mockHttpContext.Setup(x => x.Session).Returns(_mockSession.Object);

        _service = new EnhancedRateLimitingService(_memoryCache, _mockLogger.Object, _mockConfiguration.Object);
    }

    #region Composite Partition Key Tests

    [Fact]
    public async Task GetCompositePartitionKeyAsync_WithAuthenticatedUser_ShouldIncludeUserFactor()
    {
        // Arrange
        var identity = new Mock<ClaimsIdentity>();
        identity.Setup(x => x.IsAuthenticated).Returns(true);
        identity.Setup(x => x.Name).Returns("test-user");
        _mockUser.Setup(x => x.Identity).Returns(identity.Object);
        
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.100"));
        _mockSession.Setup(x => x.Id).Returns("session-123");
        
        var headers = new HeaderDictionary { ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)" };
        _mockRequest.Setup(x => x.Headers).Returns(headers);

        // Act
        var result = await _service.GetCompositePartitionKeyAsync(_mockHttpContext.Object);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(16); // Hashed key length
    }

    [Fact]
    public async Task GetCompositePartitionKeyAsync_WithUnauthenticatedUser_ShouldUseFallbackFactors()
    {
        // Arrange
        var identity = new Mock<ClaimsIdentity>();
        identity.Setup(x => x.IsAuthenticated).Returns(false);
        _mockUser.Setup(x => x.Identity).Returns(identity.Object);
        
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("10.0.0.1"));
        _mockSession.Setup(x => x.Id).Returns("session-456");
        
        var headers = new HeaderDictionary { ["User-Agent"] = "curl/7.68.0" };
        _mockRequest.Setup(x => x.Headers).Returns(headers);

        // Act
        var result = await _service.GetCompositePartitionKeyAsync(_mockHttpContext.Object);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(16);
    }

    [Fact]
    public async Task GetCompositePartitionKeyAsync_WithForwardedHeaders_ShouldUseRealClientIp()
    {
        // Arrange
        var identity = new Mock<ClaimsIdentity>();
        identity.Setup(x => x.IsAuthenticated).Returns(false);
        _mockUser.Setup(x => x.Identity).Returns(identity.Object);
        
        var headers = new HeaderDictionary 
        { 
            ["X-Forwarded-For"] = "203.0.113.1, 192.168.1.1",
            ["User-Agent"] = "Mozilla/5.0"
        };
        _mockRequest.Setup(x => x.Headers).Returns(headers);
        _mockSession.Setup(x => x.Id).Returns("session-789");

        // Act
        var result = await _service.GetCompositePartitionKeyAsync(_mockHttpContext.Object);

        // Assert
        result.Should().NotBeNullOrEmpty();
        // The key should be based on the real client IP (203.0.113.1), not the proxy IP
    }

    [Fact]
    public async Task GetCompositePartitionKeyAsync_WithNullContext_ShouldReturnAnonymous()
    {
        // Act
        var result = await _service.GetCompositePartitionKeyAsync(null!);

        // Assert
        result.Should().Be("anonymous");
    }

    #endregion

    #region CAPTCHA Requirement Tests

    [Fact]
    public async Task ShouldRequireCaptchaAsync_WithMultipleTriggers_ShouldReturnTrue()
    {
        // Arrange
        var path = new PathString("/api/users");
        _mockRequest.Setup(x => x.Path).Returns(path);
        
        var headers = new HeaderDictionary { ["User-Agent"] = "bot-scanner" };
        _mockRequest.Setup(x => x.Headers).Returns(headers);
        
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.100"));

        // Setup cache to return violation history
        var cacheEntry = Mock.Of<ICacheEntry>();
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
            .Returns((object key, out object? value) =>
            {
                value = 5; // High violation count
                return true;
            });

        // Act
        var result = await _service.ShouldRequireCaptchaAsync(_mockHttpContext.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldRequireCaptchaAsync_WithSuspiciousUserAgent_ShouldReturnTrue()
    {
        // Arrange
        var headers = new HeaderDictionary { ["User-Agent"] = "python-requests/2.25.1" };
        _mockRequest.Setup(x => x.Headers).Returns(headers);
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.100"));

        var path = new PathString("/api/songs");
        _mockRequest.Setup(x => x.Path).Returns(path);

        // Act
        var result = await _service.ShouldRequireCaptchaAsync(_mockHttpContext.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldRequireCaptchaAsync_WithLegitimateRequest_ShouldReturnFalse()
    {
        // Arrange
        var headers = new HeaderDictionary { ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" };
        _mockRequest.Setup(x => x.Headers).Returns(headers);
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.100"));

        var path = new PathString("/songs");
        _mockRequest.Setup(x => x.Path).Returns(path);

        // Setup cache to return low violation counts
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
            .Returns((object key, out object? value) =>
            {
                value = 1; // Low violation count
                return true;
            });

        // Act
        var result = await _service.ShouldRequireCaptchaAsync(_mockHttpContext.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldRequireCaptchaAsync_WithHighRiskEndpoint_ShouldCheckCriteria()
    {
        // Arrange
        var path = new PathString("/account/settings");
        _mockRequest.Setup(x => x.Path).Returns(path);
        
        var headers = new HeaderDictionary { ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)" };
        _mockRequest.Setup(x => x.Headers).Returns(headers);
        
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.100"));

        // Pre-populate the cache with high request count for high-risk endpoint
        var endpointKey = "endpoint_requests:192.168.1.100:/account/settings";
        _memoryCache.Set(endpointKey, 6); // Exceeds threshold for high-risk endpoints (5)

        // Act
        var result = await _service.ShouldRequireCaptchaAsync(_mockHttpContext.Object);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Rate Limit Policy Tests

    [Fact]
    public void GetRateLimitPolicy_WithAuthenticationEndpoint_ShouldReturnAuthPolicy()
    {
        // Arrange
        var path = new PathString("/account/login");
        _mockRequest.Setup(x => x.Path).Returns(path);

        // Act
        var result = _service.GetRateLimitPolicy(_mockHttpContext.Object);

        // Assert
        result.Should().Be("AuthPolicy");
    }

    [Fact]
    public void GetRateLimitPolicy_WithApiEndpointAuthenticated_ShouldReturnAuthenticatedApiPolicy()
    {
        // Arrange
        var path = new PathString("/api/songs");
        _mockRequest.Setup(x => x.Path).Returns(path);

        var identity = new Mock<ClaimsIdentity>();
        identity.Setup(x => x.IsAuthenticated).Returns(true);
        _mockUser.Setup(x => x.Identity).Returns(identity.Object);

        // Act
        var result = _service.GetRateLimitPolicy(_mockHttpContext.Object);

        // Assert
        result.Should().Be("AuthenticatedApiPolicy");
    }

    [Fact]
    public void GetRateLimitPolicy_WithApiEndpointUnauthenticated_ShouldReturnApiPolicy()
    {
        // Arrange
        var path = new PathString("/api/songs");
        _mockRequest.Setup(x => x.Path).Returns(path);

        var identity = new Mock<ClaimsIdentity>();
        identity.Setup(x => x.IsAuthenticated).Returns(false);
        _mockUser.Setup(x => x.Identity).Returns(identity.Object);

        // Act
        var result = _service.GetRateLimitPolicy(_mockHttpContext.Object);

        // Assert
        result.Should().Be("ApiPolicy");
    }

    [Fact]
    public void GetRateLimitPolicy_WithHighRiskEndpoint_ShouldReturnStrictPolicy()
    {
        // Arrange
        var path = new PathString("/admin/settings");
        _mockRequest.Setup(x => x.Path).Returns(path);

        // Act
        var result = _service.GetRateLimitPolicy(_mockHttpContext.Object);

        // Assert
        result.Should().Be("StrictPolicy");
    }

    [Fact]
    public void GetRateLimitPolicy_WithSensitiveOperation_ShouldReturnSensitivePolicy()
    {
        // Arrange
        var path = new PathString("/api/users/delete");
        _mockRequest.Setup(x => x.Path).Returns(path);
        _mockRequest.Setup(x => x.Method).Returns("DELETE");

        // Act
        var result = _service.GetRateLimitPolicy(_mockHttpContext.Object);

        // Assert
        result.Should().Be("SensitivePolicy");
    }

    [Fact]
    public void GetRateLimitPolicy_WithRegularEndpointAuthenticated_ShouldReturnAuthenticatedPolicy()
    {
        // Arrange
        var path = new PathString("/songs");
        _mockRequest.Setup(x => x.Path).Returns(path);

        var identity = new Mock<ClaimsIdentity>();
        identity.Setup(x => x.IsAuthenticated).Returns(true);
        _mockUser.Setup(x => x.Identity).Returns(identity.Object);

        // Act
        var result = _service.GetRateLimitPolicy(_mockHttpContext.Object);

        // Assert
        result.Should().Be("AuthenticatedPolicy");
    }

    #endregion

    #region Violation Recording Tests

    [Fact]
    public async Task RecordRateLimitViolationAsync_WithValidContext_ShouldLogAndUpdateCounters()
    {
        // Arrange
        var partitionKey = "test-partition-key";
        var path = new PathString("/api/songs");
        _mockRequest.Setup(x => x.Path).Returns(path);
        
        var headers = new HeaderDictionary { ["User-Agent"] = "Mozilla/5.0" };
        _mockRequest.Setup(x => x.Headers).Returns(headers);
        
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.100"));

        var identity = new Mock<ClaimsIdentity>();
        identity.Setup(x => x.Name).Returns("test-user");
        _mockUser.Setup(x => x.Identity).Returns(identity.Object);

        var cacheEntry = Mock.Of<ICacheEntry>();
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);

        // Act
        await _service.RecordRateLimitViolationAsync(_mockHttpContext.Object, partitionKey);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate limit violation recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordRateLimitViolationAsync_WithNullContext_ShouldNotThrow()
    {
        // Arrange
        var partitionKey = "test-partition-key";

        // Act & Assert
        var act = async () => await _service.RecordRateLimitViolationAsync(null!, partitionKey);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RecordRateLimitViolationAsync_WithEmptyPartitionKey_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        var act = async () => await _service.RecordRateLimitViolationAsync(_mockHttpContext.Object, "");
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region CAPTCHA Validation Tests

    [Fact]
    public async Task ValidateCaptchaAsync_WithEmptyResponse_ShouldReturnFalse()
    {
        // Act
        var result = await _service.ValidateCaptchaAsync("", "192.168.1.100");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCaptchaAsync_WithEmptySecretKey_ShouldReturnFalse()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Captcha:SecretKey"]).Returns("");

        var service = new EnhancedRateLimitingService(_mockCache.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Act
        var result = await service.ValidateCaptchaAsync("valid-response", "192.168.1.100");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCaptchaAsync_WithNullInputs_ShouldReturnFalse()
    {
        // Act
        var result = await _service.ValidateCaptchaAsync(null!, null!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Edge Cases and Security Tests

    [Fact]
    public async Task GetCompositePartitionKeyAsync_WithIPv6Address_ShouldHandleCorrectly()
    {
        // Arrange
        var identity = new Mock<ClaimsIdentity>();
        identity.Setup(x => x.IsAuthenticated).Returns(false);
        _mockUser.Setup(x => x.Identity).Returns(identity.Object);
        
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"));
        _mockSession.Setup(x => x.Id).Returns("session-ipv6");
        
        var headers = new HeaderDictionary { ["User-Agent"] = "Mozilla/5.0" };
        _mockRequest.Setup(x => x.Headers).Returns(headers);

        // Act
        var result = await _service.GetCompositePartitionKeyAsync(_mockHttpContext.Object);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(16);
    }

    [Fact]
    public async Task ShouldRequireCaptchaAsync_WithEmptyUserAgent_ShouldReturnTrue()
    {
        // Arrange
        var headers = new HeaderDictionary { ["User-Agent"] = "" };
        _mockRequest.Setup(x => x.Headers).Returns(headers);
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.100"));

        var path = new PathString("/api/songs");
        _mockRequest.Setup(x => x.Path).Returns(path);

        // Act
        var result = await _service.ShouldRequireCaptchaAsync(_mockHttpContext.Object);

        // Assert
        result.Should().BeTrue(); // Empty user agent is suspicious
    }

    [Fact]
    public async Task ShouldRequireCaptchaAsync_WithMultipleForwardedIps_ShouldDetectDistributedAttack()
    {
        // Arrange
        var headers = new HeaderDictionary 
        { 
            ["X-Forwarded-For"] = "203.0.113.1, 203.0.113.2, 203.0.113.3",
            ["User-Agent"] = "Mozilla/5.0"
        };
        _mockRequest.Setup(x => x.Headers).Returns(headers);

        var path = new PathString("/api/songs");
        _mockRequest.Setup(x => x.Path).Returns(path);

        // Pre-populate the cache with high segment activity to simulate distributed attack
        var segmentKey = "segment_requests:203.0.113.0/24";
        _memoryCache.Set(segmentKey, 75); // More than 50 requests from this network segment

        // Act
        var result = await _service.ShouldRequireCaptchaAsync(_mockHttpContext.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetRateLimitPolicy_WithNullContext_ShouldReturnGlobalPolicy()
    {
        // Act
        var result = _service.GetRateLimitPolicy(null!);

        // Assert
        result.Should().Be("GlobalPolicy");
    }

    [Theory]
    [InlineData("bot")]
    [InlineData("crawler")]
    [InlineData("spider")]
    [InlineData("scraper")]
    [InlineData("curl")]
    [InlineData("wget")]
    [InlineData("python")]
    [InlineData("requests")]
    public async Task ShouldRequireCaptchaAsync_WithSuspiciousUserAgentPatterns_ShouldReturnTrue(string pattern)
    {
        // Arrange
        var headers = new HeaderDictionary { ["User-Agent"] = $"Test-{pattern}-Agent/1.0" };
        _mockRequest.Setup(x => x.Headers).Returns(headers);
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.100"));

        var path = new PathString("/api/songs");
        _mockRequest.Setup(x => x.Path).Returns(path);

        // Act
        var result = await _service.ShouldRequireCaptchaAsync(_mockHttpContext.Object);

        // Assert
        result.Should().BeTrue($"User agent containing '{pattern}' should trigger CAPTCHA");
    }

    #endregion

    #region Performance and Scalability Tests

    [Fact]
    public async Task GetCompositePartitionKeyAsync_WithConcurrentRequests_ShouldBeThreadSafe()
    {
        // Arrange
        var identity = new Mock<ClaimsIdentity>();
        identity.Setup(x => x.IsAuthenticated).Returns(true);
        identity.Setup(x => x.Name).Returns("concurrent-user");
        _mockUser.Setup(x => x.Identity).Returns(identity.Object);
        
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.100"));
        _mockSession.Setup(x => x.Id).Returns("concurrent-session");
        
        var headers = new HeaderDictionary { ["User-Agent"] = "Mozilla/5.0" };
        _mockRequest.Setup(x => x.Headers).Returns(headers);

        // Act - Run multiple concurrent requests
        var tasks = Enumerable.Range(0, 10).Select(async _ => 
            await _service.GetCompositePartitionKeyAsync(_mockHttpContext.Object));
        
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result => 
        {
            result.Should().NotBeNullOrEmpty();
            result.Length.Should().Be(16);
        });

        // All results should be identical for the same context
        results.Should().OnlyContain(result => result == results[0]);
    }

    #endregion
}