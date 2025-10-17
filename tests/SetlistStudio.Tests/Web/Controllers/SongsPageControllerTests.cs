using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Principal;
using Xunit;
using FluentAssertions;
using SetlistStudio.Web.Controllers;
using Moq;

namespace SetlistStudio.Tests.Web.Controllers;

/// <summary>
/// Tests for SongsPageController MVC endpoints
/// Focuses on authentication requirements and page routing
/// </summary>
public class SongsPageControllerTests
{
    private readonly SongsPageController _controller;

    public SongsPageControllerTests()
    {
        _controller = new SongsPageController();
    }

    [Fact]
    public void Create_WithAuthenticatedUser_ReturnsOkWithCreateMessage()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.Create();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Create song page (authenticated)");
    }

    [Fact]
    public void Create_WithUnauthenticatedUser_ReturnsChallengeResult()
    {
        // Arrange
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
        var result = _controller.Create();

        // Assert
        result.Should().BeOfType<ChallengeResult>();
    }

    [Fact]
    public void Create_WithNullUserIdentity_ReturnsChallengeResult()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal() // No identity
            }
        };

        // Act
        var result = _controller.Create();

        // Assert
        result.Should().BeOfType<ChallengeResult>();
    }

    [Fact]
    public void Create_WithAuthenticatedUser_HasCorrectMessageContent()
    {
        // Arrange
        var testUserName = "musician@example.com";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, testUserName),
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Role, "Musician")
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
        var result = _controller.Create();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var message = okResult!.Value as string;
        
        message.Should().NotBeNull();
        message.Should().Contain("Create song page");
        message.Should().Contain("authenticated");
    }

    [Fact]
    public void Index_ReturnsOkWithIndexMessage()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.Index();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Songs index page (authenticated)");
    }

    [Fact]
    public void Index_WithUnauthenticatedUser_StillReturnsOk()
    {
        // Arrange
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
        var result = _controller.Index();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Songs index page (authenticated)");
    }

    [Fact]
    public void Index_WithMultipleRoles_ReturnsCorrectMessage()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "admin@example.com"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Musician"),
            new Claim("custom_permission", "manage_songs")
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
        var result = _controller.Index();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var message = okResult!.Value as string;
        
        message.Should().NotBeNull();
        message.Should().Contain("Songs index page");
        message.Should().Contain("authenticated");
    }

    [Fact]
    public void Create_WithNullHttpContext_ReturnsChallengeResult()
    {
        // Arrange
        var controller = new SongsPageController();
        var httpContext = new Mock<HttpContext>();
        var user = new Mock<ClaimsPrincipal>();
        var identity = new Mock<IIdentity>();
        
        identity.Setup(x => x.IsAuthenticated).Returns(false);
        user.Setup(x => x.Identity).Returns(identity.Object);
        httpContext.Setup(x => x.User).Returns(user.Object);
        
        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext.Object
        };

        // Act
        var result = controller.Create();

        // Assert
        result.Should().BeOfType<ChallengeResult>();
    }

    [Fact]
    public void Index_WithNullHttpContext_StillReturnsOk()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext();

        // Act
        var result = _controller.Index();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Songs index page (authenticated)");
    }

    [Fact]
    public void Create_HasAuthorizeAttribute()
    {
        // Arrange & Act
        var controllerType = typeof(SongsPageController);

        // Assert
        var authorizeAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        authorizeAttributes.Should().NotBeEmpty("Controller should have Authorize attribute");
    }

    [Fact]
    public void Create_HasCorrectRouteAttribute()
    {
        // Arrange & Act
        var controllerType = typeof(SongsPageController);

        // Assert
        var routeAttributes = controllerType.GetCustomAttributes(typeof(RouteAttribute), false);
        routeAttributes.Should().NotBeEmpty("Controller should have Route attribute");
        
        var routeAttribute = routeAttributes.First() as RouteAttribute;
        routeAttribute!.Template.Should().Be("Songs");
    }

    [Fact]
    public void Create_HasCorrectHttpGetAttribute()
    {
        // Arrange & Act
        var method = typeof(SongsPageController).GetMethod("Create");

        // Assert
        var httpGetAttributes = method!.GetCustomAttributes(typeof(HttpGetAttribute), false);
        httpGetAttributes.Should().NotBeEmpty("Create method should have HttpGet attribute");
        
        var httpGetAttribute = httpGetAttributes.First() as HttpGetAttribute;
        httpGetAttribute!.Template.Should().Be("Create");
    }

    [Fact]
    public void Index_HasCorrectHttpGetAttribute()
    {
        // Arrange & Act
        var method = typeof(SongsPageController).GetMethod("Index");

        // Assert
        var httpGetAttributes = method!.GetCustomAttributes(typeof(HttpGetAttribute), false);
        httpGetAttributes.Should().NotBeEmpty("Index method should have HttpGet attribute");
        
        var httpGetAttribute = httpGetAttributes.First() as HttpGetAttribute;
        httpGetAttribute!.Template.Should().Be("");
    }

    [Fact]
    public void Create_WithValidAuthenticatedUser_ChecksAuthenticationProperly()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "validuser@example.com"),
            new Claim(ClaimTypes.NameIdentifier, "valid-user-123")
        };

        var identity = new ClaimsIdentity(claims, "ValidAuthType");
        identity.AddClaim(new Claim("authenticated", "true"));
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.Create();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Create song page (authenticated)");
    }

    [Fact]
    public void Create_WithEmptyClaimsButAuthenticated_ReturnsOk()
    {
        // Arrange
        var identity = new ClaimsIdentity(new List<Claim>(), "EmptyClaimsAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.Create();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Create song page (authenticated)");
    }

    [Fact]
    public void Create_AuthenticationLogic_HandlesDifferentIdentityStates()
    {
        // Test Case 1: Null User.Identity
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var result1 = _controller.Create();
        result1.Should().BeOfType<ChallengeResult>();

        // Test Case 2: User.Identity exists but IsAuthenticated is false
        var unauthenticatedIdentity = new ClaimsIdentity(); // No authenticationType means not authenticated
        var unauthenticatedPrincipal = new ClaimsPrincipal(unauthenticatedIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = unauthenticatedPrincipal
            }
        };
        var result2 = _controller.Create();
        result2.Should().BeOfType<ChallengeResult>();

        // Test Case 3: User.Identity exists and IsAuthenticated is true
        var authenticatedIdentity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "user") }, "TestAuth");
        var authenticatedPrincipal = new ClaimsPrincipal(authenticatedIdentity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = authenticatedPrincipal
            }
        };
        var result3 = _controller.Create();
        result3.Should().BeOfType<OkObjectResult>();
    }
}