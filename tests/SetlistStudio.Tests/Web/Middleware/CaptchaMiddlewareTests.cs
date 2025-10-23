using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Web.Middleware;
using SetlistStudio.Web.Services;
using Xunit;
using FluentAssertions;
using System.Text;

namespace SetlistStudio.Tests.Web.Middleware;

/// <summary>
/// Comprehensive tests for CaptchaMiddleware covering CAPTCHA challenge enforcement,
/// bypass mechanisms, and integration with the enhanced rate limiting system.
/// </summary>
public class CaptchaMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<IEnhancedRateLimitingService> _mockRateLimitingService;
    private readonly Mock<ILogger<CaptchaMiddleware>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly CaptchaMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public CaptchaMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockRateLimitingService = new Mock<IEnhancedRateLimitingService>();
        _mockLogger = new Mock<ILogger<CaptchaMiddleware>>();
        _mockCache = new Mock<IMemoryCache>();

        _middleware = new CaptchaMiddleware(
            _mockNext.Object,
            _mockRateLimitingService.Object,
            _mockLogger.Object,
            _mockCache.Object);

        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    #region Middleware Flow Tests

    [Fact]
    public async Task InvokeAsync_WithStaticFileRequest_ShouldSkipCaptcha()
    {
        // Arrange
        _httpContext.Request.Path = "/css/styles.css";

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockRateLimitingService.Verify(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithHealthCheckRequest_ShouldSkipCaptcha()
    {
        // Arrange
        _httpContext.Request.Path = "/health";

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockRateLimitingService.Verify(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithCaptchaBypassToken_ShouldSkipCaptchaCheck()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        var cacheEntry = Mock.Of<ICacheEntry>();
        _mockCache.Setup(x => x.TryGetValue("captcha_bypass:192.168.1.100", out It.Ref<object>.IsAny))
            .Returns(true);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockRateLimitingService.Verify(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithNormalRequest_ShouldCheckCaptchaRequirement()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(false);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockRateLimitingService.Verify(x => x.ShouldRequireCaptchaAsync(_httpContext), Times.Once);
    }

    #endregion

    #region CAPTCHA Challenge Tests

    [Fact]
    public async Task InvokeAsync_WithCaptchaRequired_ShouldReturnChallenge()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Request.ContentType = "application/json";
        _httpContext.Request.Headers.Accept = "application/json";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(429);
        _httpContext.Response.ContentType.Should().Be("application/json");
        _mockNext.Verify(x => x(_httpContext), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithCaptchaRequiredForWebRequest_ShouldReturnHtmlChallenge()
    {
        // Arrange
        _httpContext.Request.Path = "/songs";
        _httpContext.Request.ContentType = "text/html";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(429);
        _httpContext.Response.ContentType.Should().Be("text/html");
        
        // Check that HTML content contains CAPTCHA elements
        _httpContext.Response.Body.Position = 0;
        var reader = new StreamReader(_httpContext.Response.Body);
        var content = await reader.ReadToEndAsync();
        
        content.Should().Contain("Security Verification Required");
        content.Should().Contain("g-recaptcha");
        content.Should().Contain("data-sitekey");
        
        _mockNext.Verify(x => x(_httpContext), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithValidCaptchaResponse_ShouldGrantBypass()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Request.Method = "POST";
        _httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        // Simulate form data with CAPTCHA response
        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["g-recaptcha-response"] = "valid-captcha-token"
        });
        _httpContext.Request.Form = formCollection;

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(true);

        _mockRateLimitingService.Setup(x => x.ValidateCaptchaAsync("valid-captcha-token", "192.168.1.100"))
            .ReturnsAsync(true);

        var cacheEntry = Mock.Of<ICacheEntry>();
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockCache.Verify(x => x.CreateEntry("captcha_bypass:192.168.1.100"), Times.Once);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CAPTCHA challenge passed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidCaptchaResponse_ShouldReturnChallengeWithError()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Request.Method = "POST";
        _httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["g-recaptcha-response"] = "invalid-captcha-token"
        });
        _httpContext.Request.Form = formCollection;

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(true);

        _mockRateLimitingService.Setup(x => x.ValidateCaptchaAsync("invalid-captcha-token", "192.168.1.100"))
            .ReturnsAsync(false);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(429);
        _mockNext.Verify(x => x(_httpContext), Times.Never);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid CAPTCHA response")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region IP Address Handling Tests

    [Fact]
    public async Task InvokeAsync_WithForwardedForHeader_ShouldUseRealClientIp()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.1, 192.168.1.1";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1"); // Proxy IP

        _mockCache.Setup(x => x.TryGetValue("captcha_bypass:203.0.113.1", out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(false);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        // The middleware should check bypass for the real client IP (203.0.113.1), not proxy IP
    }

    [Fact]
    public async Task InvokeAsync_WithXRealIpHeader_ShouldUseRealClientIp()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Request.Headers["X-Real-IP"] = "198.51.100.1";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        _mockCache.Setup(x => x.TryGetValue("captcha_bypass:198.51.100.1", out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(false);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownIpAddress_ShouldHandleGracefully()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Connection.RemoteIpAddress = null; // No IP address available

        _mockCache.Setup(x => x.TryGetValue("captcha_bypass:unknown", out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(false);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
    }

    #endregion

    #region CAPTCHA Response Sources Tests

    [Fact]
    public async Task InvokeAsync_WithCaptchaInHeaders_ShouldValidateCorrectly()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Request.Headers["X-Captcha-Response"] = "header-captcha-token";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(true);

        _mockRateLimitingService.Setup(x => x.ValidateCaptchaAsync("header-captcha-token", "192.168.1.100"))
            .ReturnsAsync(true);

        var cacheEntry = Mock.Of<ICacheEntry>();
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockRateLimitingService.Verify(x => x.ValidateCaptchaAsync("header-captcha-token", "192.168.1.100"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithCaptchaInQueryString_ShouldValidateCorrectly()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Request.QueryString = new QueryString("?captcha=query-captcha-token");
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(true);

        _mockRateLimitingService.Setup(x => x.ValidateCaptchaAsync("query-captcha-token", "192.168.1.100"))
            .ReturnsAsync(true);

        var cacheEntry = Mock.Of<ICacheEntry>();
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockRateLimitingService.Verify(x => x.ValidateCaptchaAsync("query-captcha-token", "192.168.1.100"), Times.Once);
    }

    #endregion

    #region Skip Patterns Tests

    [Theory]
    [InlineData("/css/bootstrap.min.css")]
    [InlineData("/js/site.js")]
    [InlineData("/images/logo.png")]
    [InlineData("/fonts/roboto.woff2")]
    [InlineData("/favicon.ico")]
    [InlineData("/_framework/blazor.server.js")]
    [InlineData("/_content/MudBlazor/MudBlazor.min.css")]
    [InlineData("/health")]
    [InlineData("/ready")]
    [InlineData("/live")]
    [InlineData("/metrics")]
    public async Task InvokeAsync_WithStaticResources_ShouldSkipCaptcha(string path)
    {
        // Arrange
        _httpContext.Request.Path = path;

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(x => x(_httpContext), Times.Once);
        _mockRateLimitingService.Verify(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    #endregion

    #region API vs Web Request Detection Tests

    [Theory]
    [InlineData("/api/songs", "application/json", true)]
    [InlineData("/songs", "text/html", false)]
    [InlineData("/api/users", "application/xml", true)]
    [InlineData("/setlists", "text/plain", false)]
    public async Task InvokeAsync_ShouldDetectRequestTypeCorrectly(string path, string acceptHeader, bool isApiRequest)
    {
        // Arrange
        _httpContext.Request.Path = path;
        _httpContext.Request.Headers.Accept = acceptHeader;
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(429);
        
        if (isApiRequest)
        {
            _httpContext.Response.ContentType.Should().Be("application/json");
        }
        else
        {
            _httpContext.Response.ContentType.Should().Be("text/html");
        }
    }

    [Fact]
    public async Task InvokeAsync_WithXRequestedWithHeader_ShouldTreatAsApiRequest()
    {
        // Arrange
        _httpContext.Request.Path = "/songs";
        _httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(429);
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task InvokeAsync_WithRateLimitingServiceException_ShouldContinueWithoutCaptcha()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new Exception("Service unavailable"));

        // Act & Assert
        var act = async () => await _middleware.InvokeAsync(_httpContext);
        await act.Should().NotThrowAsync();

        // Should continue to next middleware even if rate limiting service fails
        _mockNext.Verify(x => x(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithCaptchaValidationException_ShouldReturnChallenge()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Request.Method = "POST";
        _httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["g-recaptcha-response"] = "captcha-token"
        });
        _httpContext.Request.Form = formCollection;

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(true);

        _mockRateLimitingService.Setup(x => x.ValidateCaptchaAsync("captcha-token", "192.168.1.100"))
            .ThrowsAsync(new HttpRequestException("CAPTCHA service unavailable"));

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(429);
        _mockNext.Verify(x => x(_httpContext), Times.Never);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task InvokeAsync_WithCaptchaChallenge_ShouldLogChallenge()
    {
        // Arrange
        _httpContext.Request.Path = "/api/songs";
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        _mockRateLimitingService.Setup(x => x.ShouldRequireCaptchaAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CAPTCHA challenge issued")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}