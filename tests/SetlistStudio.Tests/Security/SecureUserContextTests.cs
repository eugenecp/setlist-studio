using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using SetlistStudio.Web.Security;
using System.Net;
using System.Security.Claims;
using Xunit;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive tests for SecureUserContext security utilities.
/// Tests all methods for proper sanitization and edge case handling.
/// </summary>
public class SecureUserContextTests
{
    #region GetSanitizedUserId Tests

    [Fact]
    public void GetSanitizedUserId_ShouldReturnAnonymous_WhenUserIsNull()
    {
        // Act
        var result = SecureUserContext.GetSanitizedUserId(null);

        // Assert
        result.Should().Be("anonymous");
    }

    [Fact]
    public void GetSanitizedUserId_ShouldReturnAnonymous_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = SecureUserContext.GetSanitizedUserId(user);

        // Assert
        result.Should().Be("anonymous");
    }

    [Fact]
    public void GetSanitizedUserId_ShouldReturnSanitizedUserId_WhenUserHasNameIdentifierClaim()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = SecureUserContext.GetSanitizedUserId(user);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("anonymous");
    }

    [Fact]
    public void GetSanitizedUserId_ShouldFallbackToIdentityName_WhenNoNameIdentifierClaim()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = SecureUserContext.GetSanitizedUserId(user);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("anonymous");
    }

    [Fact]
    public void GetSanitizedUserId_ShouldReturnAnonymous_WhenNoUserIdAvailable()
    {
        // Arrange
        var identity = new ClaimsIdentity(Array.Empty<Claim>(), "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = SecureUserContext.GetSanitizedUserId(user);

        // Assert
        result.Should().Be("anonymous");
    }

    [Fact]
    public void GetSanitizedUserId_ShouldSanitizeMaliciousInput()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "<script>alert('xss')</script>")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = SecureUserContext.GetSanitizedUserId(user);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("alert");
    }

    #endregion

    #region GetSanitizedUserName Tests

    [Fact]
    public void GetSanitizedUserName_ShouldReturnAnonymousUser_WhenUserIsNull()
    {
        // Act
        var result = SecureUserContext.GetSanitizedUserName(null);

        // Assert
        result.Should().Be("Anonymous User");
    }

    [Fact]
    public void GetSanitizedUserName_ShouldReturnAnonymousUser_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = SecureUserContext.GetSanitizedUserName(user);

        // Assert
        result.Should().Be("Anonymous User");
    }

    [Fact]
    public void GetSanitizedUserName_ShouldReturnSanitizedUserName_WhenUserIsAuthenticated()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "John Doe")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = SecureUserContext.GetSanitizedUserName(user);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("Anonymous User");
    }

    [Fact]
    public void GetSanitizedUserName_ShouldReturnUnknownUser_WhenNameIsNull()
    {
        // Arrange
        var identity = new ClaimsIdentity(Array.Empty<Claim>(), "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = SecureUserContext.GetSanitizedUserName(user);

        // Assert
        result.Should().Be("Unknown User");
    }

    [Fact]
    public void GetSanitizedUserName_ShouldSanitizeMaliciousInput()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "<script>alert('xss')</script>")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = SecureUserContext.GetSanitizedUserName(user);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("alert");
    }

    #endregion

    #region GetSanitizedUserEmail Tests

    [Fact]
    public void GetSanitizedUserEmail_ShouldReturnAnonymousEmail_WhenUserIsNull()
    {
        // Act
        var result = SecureUserContext.GetSanitizedUserEmail(null);

        // Assert
        result.Should().Be("anonymous@localhost");
    }

    [Fact]
    public void GetSanitizedUserEmail_ShouldReturnAnonymousEmail_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = SecureUserContext.GetSanitizedUserEmail(user);

        // Assert
        result.Should().Be("anonymous@localhost");
    }

    [Fact]
    public void GetSanitizedUserEmail_ShouldReturnSanitizedEmail_WhenEmailClaimExists()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "user@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = SecureUserContext.GetSanitizedUserEmail(user);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("anonymous@localhost");
    }

    [Fact]
    public void GetSanitizedUserEmail_ShouldFallbackToGenericEmailClaim()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("email", "user@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = SecureUserContext.GetSanitizedUserEmail(user);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("anonymous@localhost");
    }

    [Fact]
    public void GetSanitizedUserEmail_ShouldReturnUnknownEmail_WhenNoEmailClaimExists()
    {
        // Arrange
        var identity = new ClaimsIdentity(Array.Empty<Claim>(), "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = SecureUserContext.GetSanitizedUserEmail(user);

        // Assert
        result.Should().Be("unknown@localhost");
    }

    [Fact]
    public void GetSanitizedUserEmail_ShouldSanitizeMaliciousInput()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "<script>alert('xss')</script>@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = SecureUserContext.GetSanitizedUserEmail(user);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("alert");
    }

    #endregion

    #region GetSanitizedClientIp Tests

    [Fact]
    public void GetSanitizedClientIp_ShouldReturnUnknown_WhenContextIsNull()
    {
        // Act
        var result = SecureUserContext.GetSanitizedClientIp(null);

        // Assert
        result.Should().Be("unknown");
    }

    [Fact]
    public void GetSanitizedClientIp_ShouldReturnSanitizedIp_FromRemoteIpAddress()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");

        // Act
        var result = SecureUserContext.GetSanitizedClientIp(context);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("unknown");
    }

    [Fact]
    public void GetSanitizedClientIp_ShouldFallbackToXForwardedFor_WhenRemoteIpIsNull()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Connection.RemoteIpAddress = null;
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.1";

        // Act
        var result = SecureUserContext.GetSanitizedClientIp(context);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("unknown");
    }

    [Fact]
    public void GetSanitizedClientIp_ShouldFallbackToXRealIp_WhenOthersAreNull()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Connection.RemoteIpAddress = null;
        context.Request.Headers["X-Real-IP"] = "203.0.113.2";

        // Act
        var result = SecureUserContext.GetSanitizedClientIp(context);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("unknown");
    }

    [Fact]
    public void GetSanitizedClientIp_ShouldReturnUnknown_WhenNoIpSourcesAvailable()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Connection.RemoteIpAddress = null;

        // Act
        var result = SecureUserContext.GetSanitizedClientIp(context);

        // Assert
        result.Should().Be("unknown");
    }

    [Fact]
    public void GetSanitizedClientIp_ShouldHandleMaliciousInput()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Connection.RemoteIpAddress = null;
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.1<script>alert('xss')</script>";

        // Act
        var result = SecureUserContext.GetSanitizedClientIp(context);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("alert");
    }

    [Fact]
    public void GetSanitizedClientIp_ShouldMaskLastOctet_ForPrivacyProtection()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");

        // Act
        var result = SecureUserContext.GetSanitizedClientIp(context);

        // Assert
        result.Should().Be("192.168.1.xxx");
        result.Should().NotContain("100"); // Last octet should be masked
    }

    [Fact]
    public void GetSanitizedClientIp_ShouldMaskIPv6LastSegments_ForPrivacyProtection()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8::1234");

        // Act
        var result = SecureUserContext.GetSanitizedClientIp(context);

        // Assert
        result.Should().Be("2001:db8::xxxx");
        result.Should().NotContain("1234"); // Last segments should be masked
    }

    [Fact]
    public void GetSanitizedClientIp_ShouldPrioritizeForwardedFor_AndSanitize()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.45, 192.168.1.1";

        // Act
        var result = SecureUserContext.GetSanitizedClientIp(context);

        // Assert
        result.Should().Be("203.0.113.xxx"); // Should use first IP from X-Forwarded-For and mask it
        result.Should().NotContain("45"); // Original last octet should be masked
        result.Should().NotContain("10.0.0.1"); // Should not use Connection.RemoteIpAddress
    }

    #endregion

    #region GetSanitizedUserAgent Tests

    [Fact]
    public void GetSanitizedUserAgent_ShouldReturnUnknown_WhenContextIsNull()
    {
        // Act
        var result = SecureUserContext.GetSanitizedUserAgent(null);

        // Assert
        result.Should().Be("Unknown");
    }

    [Fact]
    public void GetSanitizedUserAgent_ShouldReturnSanitizedUserAgent_WhenPresent()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Headers.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        // Act
        var result = SecureUserContext.GetSanitizedUserAgent(context);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("Unknown");
    }

    [Fact]
    public void GetSanitizedUserAgent_ShouldSanitizeMaliciousInput()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Headers.UserAgent = "<script>alert('xss')</script>";

        // Act
        var result = SecureUserContext.GetSanitizedUserAgent(context);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("alert");
    }

    [Fact]
    public void GetSanitizedUserAgent_ShouldHandleEmptyUserAgent()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Headers.UserAgent = StringValues.Empty;

        // Act
        var result = SecureUserContext.GetSanitizedUserAgent(context);

        // Assert
        result.Should().Be("Unknown");
    }

    #endregion

    #region GetSanitizedRequestPath Tests

    [Fact]
    public void GetSanitizedRequestPath_ShouldReturnUnknownPath_WhenContextIsNull()
    {
        // Act
        var result = SecureUserContext.GetSanitizedRequestPath(null);

        // Assert
        result.Should().Be("/unknown");
    }

    [Fact]
    public void GetSanitizedRequestPath_ShouldReturnSanitizedPath_WhenPresent()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Path = "/api/songs";

        // Act
        var result = SecureUserContext.GetSanitizedRequestPath(context);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("/unknown");
    }

    [Fact]
    public void GetSanitizedRequestPath_ShouldSanitizeMaliciousInput()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Path = "/api/<script>alert('xss')</script>";

        // Act
        var result = SecureUserContext.GetSanitizedRequestPath(context);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("alert");
    }

    [Fact]
    public void GetSanitizedRequestPath_ShouldHandlePathTraversalAttempts()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Path = "/api/../../../etc/passwd";

        // Act
        var result = SecureUserContext.GetSanitizedRequestPath(context);

        // Assert
        result.Should().NotContain("../");
        result.Should().NotContain("passwd");
    }

    #endregion

    #region CreateSecureLoggingContext Tests

    [Fact]
    public void CreateSecureLoggingContext_ShouldReturnPopulatedDictionary_WhenContextIsValid()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.Name, "John Doe")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var context = CreateMockHttpContext();
        context.User = user;
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");
        context.Request.Headers.UserAgent = "Mozilla/5.0";
        context.Request.Path = "/api/songs";
        context.Request.Method = "GET";

        // Act
        var result = SecureUserContext.CreateSecureLoggingContext(context);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("UserId").WhoseValue.Should().NotBe("anonymous");
        result.Should().ContainKey("UserName").WhoseValue.Should().NotBe("Anonymous User");
        result.Should().ContainKey("ClientIp").WhoseValue.Should().NotBe("unknown");
        result.Should().ContainKey("UserAgent").WhoseValue.Should().NotBe("Unknown");
        result.Should().ContainKey("RequestPath").WhoseValue.Should().NotBe("/unknown");
        result.Should().ContainKey("RequestMethod").WhoseValue.Should().Be("GET");
    }

    [Fact]
    public void CreateSecureLoggingContext_ShouldReturnDefaultValues_WhenContextIsNull()
    {
        // Act
        var result = SecureUserContext.CreateSecureLoggingContext(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("UserId").WhoseValue.Should().Be("anonymous");
        result.Should().ContainKey("UserName").WhoseValue.Should().Be("Anonymous User");
        result.Should().ContainKey("ClientIp").WhoseValue.Should().Be("unknown");
        result.Should().ContainKey("UserAgent").WhoseValue.Should().Be("Unknown");
        result.Should().ContainKey("RequestPath").WhoseValue.Should().Be("/unknown");
        result.Should().ContainKey("RequestMethod").WhoseValue.Should().Be("UNKNOWN");
    }

    [Fact]
    public void CreateSecureLoggingContext_ShouldSanitizeAllValues()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "<script>alert('xss')</script>"),
            new Claim(ClaimTypes.Name, "<script>alert('xss')</script>")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var context = CreateMockHttpContext();
        context.User = user;
        context.Request.Headers["X-Forwarded-For"] = "<script>alert('xss')</script>";
        context.Request.Headers.UserAgent = "<script>alert('xss')</script>";
        context.Request.Path = "/api/<script>alert('xss')</script>";
        context.Request.Method = "<script>alert('xss')</script>";

        // Act
        var result = SecureUserContext.CreateSecureLoggingContext(context);

        // Assert
        result.Should().NotBeNull();
        foreach (var value in result.Values)
        {
            value.Should().NotContain("<script>");
            value.Should().NotContain("alert");
        }
    }

    [Fact]
    public void CreateSecureLoggingContext_ShouldIncludeAllRequiredKeys()
    {
        // Arrange
        var context = CreateMockHttpContext();

        // Act
        var result = SecureUserContext.CreateSecureLoggingContext(context);

        // Assert
        var expectedKeys = new[] { "UserId", "UserName", "ClientIp", "UserAgent", "RequestPath", "RequestMethod" };
        result.Keys.Should().Contain(expectedKeys);
    }

    #endregion

    #region Helper Methods

    private static DefaultHttpContext CreateMockHttpContext()
    {
        var context = new DefaultHttpContext();
        
        // Initialize required collections
        context.Request.Headers.Clear();
        
        return context;
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void AllMethods_ShouldHandleNullInputsGracefully()
    {
        // Act & Assert - All methods should handle null gracefully without throwing
        var userId = SecureUserContext.GetSanitizedUserId(null);
        var userName = SecureUserContext.GetSanitizedUserName(null);
        var userEmail = SecureUserContext.GetSanitizedUserEmail(null);
        var clientIp = SecureUserContext.GetSanitizedClientIp(null);
        var userAgent = SecureUserContext.GetSanitizedUserAgent(null);
        var requestPath = SecureUserContext.GetSanitizedRequestPath(null);
        var loggingContext = SecureUserContext.CreateSecureLoggingContext(null);

        // All should return safe default values
        userId.Should().Be("anonymous");
        userName.Should().Be("Anonymous User");
        userEmail.Should().Be("anonymous@localhost");
        clientIp.Should().Be("unknown");
        userAgent.Should().Be("Unknown");
        requestPath.Should().Be("/unknown");
        loggingContext.Should().NotBeNull();
    }

    [Fact]
    public void AllMethods_ShouldReturnNonNullValues()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        // Act & Assert - No method should return null
        SecureUserContext.GetSanitizedUserId(user).Should().NotBeNull();
        SecureUserContext.GetSanitizedUserName(user).Should().NotBeNull();
        SecureUserContext.GetSanitizedUserEmail(user).Should().NotBeNull();
        SecureUserContext.GetSanitizedClientIp(context).Should().NotBeNull();
        SecureUserContext.GetSanitizedUserAgent(context).Should().NotBeNull();
        SecureUserContext.GetSanitizedRequestPath(context).Should().NotBeNull();
        SecureUserContext.CreateSecureLoggingContext(context).Should().NotBeNull();
    }

    #endregion
}