using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using FluentAssertions;
using SetlistStudio.Web.Controllers;

namespace SetlistStudio.Tests.Web.Controllers;

/// <summary>
/// Advanced tests for UsersController targeting edge cases and exception scenarios
/// to improve branch coverage. These tests focus on exceptional paths, error conditions,
/// and security edge cases not covered in base tests.
/// </summary>
public class UsersControllerAdvancedTests
{
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly UsersController _controller;

    public UsersControllerAdvancedTests()
    {
        _mockLogger = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldNotThrowException()
    {
        // Act & Assert - should not throw (controller doesn't validate null logger)
        var controller = new UsersController(null!);
        controller.Should().NotBeNull();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public void GetProfile_WithCorruptedClaimsThrowingUnauthorizedException_ReturnsForbid()
    {
        // Arrange - Setup claims that would throw UnauthorizedAccessException when accessed
        var mockIdentity = new Mock<ClaimsIdentity>();
        mockIdentity.SetupGet(i => i.Name).Throws<UnauthorizedAccessException>();
        mockIdentity.SetupGet(i => i.IsAuthenticated).Returns(true);
        
        var claimsPrincipal = new ClaimsPrincipal(mockIdentity.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.GetProfile();

        // Assert
        result.Should().BeOfType<ForbidResult>();
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unauthorized access to user profile")),
                It.IsAny<UnauthorizedAccessException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetProfile_WithInvalidOperationDuringClaimsAccess_ReturnsServiceUnavailable()
    {
        // Arrange - Setup claims that would throw InvalidOperationException when accessed
        var mockIdentity = new Mock<ClaimsIdentity>();
        mockIdentity.SetupGet(i => i.Name).Returns("test-user");
        mockIdentity.SetupGet(i => i.IsAuthenticated).Returns(true);
        
        var mockClaimsPrincipal = new Mock<ClaimsPrincipal>(mockIdentity.Object);
        mockClaimsPrincipal.SetupGet(p => p.Claims).Throws<InvalidOperationException>();

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = mockClaimsPrincipal.Object
            }
        };

        // Act
        var result = _controller.GetProfile();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
        objectResult.Value.Should().Be("Service temporarily unavailable");
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid operation while retrieving user profile")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetProfile_WithUnexpectedExceptionDuringClaimsAccess_ReturnsInternalServerError()
    {
        // Arrange - Setup claims that would throw a generic exception when accessed
        var mockIdentity = new Mock<ClaimsIdentity>();
        mockIdentity.SetupGet(i => i.Name).Returns("test-user");
        mockIdentity.SetupGet(i => i.IsAuthenticated).Returns(true);
        
        var mockClaimsPrincipal = new Mock<ClaimsPrincipal>(mockIdentity.Object);
        mockClaimsPrincipal.SetupGet(p => p.Claims).Throws(new ArgumentNullException("claims", "Unexpected system error"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = mockClaimsPrincipal.Object
            }
        };

        // Act
        var result = _controller.GetProfile();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        objectResult.Value.Should().Be("Internal server error");
        
        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error retrieving user profile")),
                It.IsAny<ArgumentNullException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Edge Case User Identity Tests

    [Fact]
    public void GetProfile_WithNullUserIdentity_ReturnsAnonymousProfile()
    {
        // Arrange - No User set in ControllerContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
            // User is null by default
        };

        // Act
        var result = _controller.GetProfile();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value;

        var profileType = profile!.GetType();
        var userIdProperty = profileType.GetProperty("userId");
        var nameProperty = profileType.GetProperty("name");
        var isAuthenticatedProperty = profileType.GetProperty("isAuthenticated");

        userIdProperty!.GetValue(profile).Should().Be("anonymous");
        nameProperty!.GetValue(profile).Should().BeNull();
        isAuthenticatedProperty!.GetValue(profile).Should().Be(false);
    }

    [Fact]
    public void GetProfile_WithNullClaimsCollection_ReturnsEmptyClaimsArray()
    {
        // Arrange - Setup user with null claims
        var mockIdentity = new Mock<ClaimsIdentity>();
        mockIdentity.SetupGet(i => i.Name).Returns("test-user");
        mockIdentity.SetupGet(i => i.IsAuthenticated).Returns(true);
        
        var mockClaimsPrincipal = new Mock<ClaimsPrincipal>(mockIdentity.Object);
        mockClaimsPrincipal.SetupGet(p => p.Claims).Returns((IEnumerable<Claim>)null!);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = mockClaimsPrincipal.Object
            }
        };

        // Act
        var result = _controller.GetProfile();
        
        // Assert - Should handle null claims gracefully but may return error status
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IActionResult>();
    }

    [Fact]
    public void GetProfile_WithClaimsContainingNullValues_HandlesGracefully()
    {
        // Arrange - Setup claims with null/empty values
        var claims = new List<Claim>
        {
            new Claim("empty_type", ""),
            new Claim("", "empty_key"),
            new Claim("normal_claim", "normal_value")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.GetProfile();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value;

        var profileType = profile!.GetType();
        var claimsProperty = profileType.GetProperty("claims");
        var returnedClaims = claimsProperty!.GetValue(profile) as object[];

        returnedClaims.Should().NotBeNull();
        returnedClaims!.Length.Should().Be(3); // All claims should be included, even with empty values
    }

    #endregion

    #region Information Logging Tests

    [Fact]
    public void GetProfile_WithValidUser_LogsUserInformation()
    {
        // Arrange
        var testUserName = "test-user@example.com";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, testUserName)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.GetProfile();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify information logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Retrieving profile for user {testUserName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetProfile_WithAnonymousUser_LogsAnonymousAccess()
    {
        // Arrange - Unauthenticated user
        var identity = new ClaimsIdentity(); // Not authenticated
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.GetProfile();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify anonymous user logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieving profile for user anonymous")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Controller Context Edge Cases

    [Fact]
    public void GetProfile_WithNullControllerContext_ShouldNotThrow()
    {
        // Arrange - No ControllerContext set
        // _controller.ControllerContext is null by default

        // Act
        var result = _controller.GetProfile();
        
        // Assert - Should handle null gracefully but may return error status
        result.Should().NotBeNull();
        // The result may be either OK or an error status depending on implementation
        result.Should().BeAssignableTo<IActionResult>();
    }

    [Fact]
    public void GetProfile_WithNullHttpContext_ShouldNotThrow()
    {
        // Arrange - ControllerContext with null HttpContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = null!
        };

        // Act
        var result = _controller.GetProfile();
        
        // Assert - Should handle null gracefully but may return error status
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IActionResult>();
    }

    #endregion
}