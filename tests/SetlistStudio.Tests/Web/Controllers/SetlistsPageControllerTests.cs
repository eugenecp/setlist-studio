using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;
using FluentAssertions;
using SetlistStudio.Web.Controllers;

namespace SetlistStudio.Tests.Web.Controllers;

/// <summary>
/// Tests for SetlistsPageController MVC endpoints
/// Focuses on authentication requirements and page routing
/// </summary>
public class SetlistsPageControllerTests
{
    private readonly SetlistsPageController _controller;

    public SetlistsPageControllerTests()
    {
        _controller = new SetlistsPageController();
    }

    [Fact]
    public void Create_ReturnsOkWithCreateMessage()
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
        okResult!.Value.Should().Be("Create setlist page (authenticated)");
    }

    [Fact]
    public void Create_WithUnauthenticatedUser_StillReturnsOk()
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
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Create setlist page (authenticated)");
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
        okResult!.Value.Should().Be("Setlists index page (authenticated)");
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
        okResult!.Value.Should().Be("Setlists index page (authenticated)");
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
        message.Should().Contain("Create setlist page");
        message.Should().Contain("authenticated");
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
            new Claim("custom_permission", "manage_setlists")
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
        message.Should().Contain("Setlists index page");
        message.Should().Contain("authenticated");
    }

    [Fact]
    public void Create_WithNullHttpContext_StillReturnsOk()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext();

        // Act
        var result = _controller.Create();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Create setlist page (authenticated)");
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
        okResult!.Value.Should().Be("Setlists index page (authenticated)");
    }

    [Fact]
    public void Create_HasAuthorizeAttribute()
    {
        // Arrange & Act
        var controllerType = typeof(SetlistsPageController);

        // Assert
        var authorizeAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        authorizeAttributes.Should().NotBeEmpty("Controller should have Authorize attribute");
    }

    [Fact]
    public void Create_HasCorrectRouteAttribute()
    {
        // Arrange & Act
        var controllerType = typeof(SetlistsPageController);

        // Assert
        var routeAttributes = controllerType.GetCustomAttributes(typeof(RouteAttribute), false);
        routeAttributes.Should().NotBeEmpty("Controller should have Route attribute");
        
        var routeAttribute = routeAttributes.First() as RouteAttribute;
        routeAttribute!.Template.Should().Be("Setlists");
    }

    [Fact]
    public void Create_HasCorrectHttpGetAttribute()
    {
        // Arrange & Act
        var method = typeof(SetlistsPageController).GetMethod("Create");

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
        var method = typeof(SetlistsPageController).GetMethod("Index");

        // Assert
        var httpGetAttributes = method!.GetCustomAttributes(typeof(HttpGetAttribute), false);
        httpGetAttributes.Should().NotBeEmpty("Index method should have HttpGet attribute");
        
        var httpGetAttribute = httpGetAttributes.First() as HttpGetAttribute;
        httpGetAttribute!.Template.Should().Be("");
    }
}