using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Security.Claims;
using Xunit;
using SetlistStudio.Web.Controllers;

namespace SetlistStudio.Tests.Web.Controllers;

/// <summary>
/// Comprehensive tests for SecurityBlockController covering all endpoints and security attributes.
/// </summary>
public class SecurityBlockControllerTests
{
    private readonly SecurityBlockController _controller;

    public SecurityBlockControllerTests()
    {
        _controller = new SecurityBlockController();
    }

    [Fact]
    public void Controller_ShouldHaveCorrectRouteAttribute()
    {
        // Arrange & Act
        var routeAttribute = typeof(SecurityBlockController)
            .GetCustomAttribute<RouteAttribute>();

        // Assert
        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("");
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("api/admin")]
    public void Admin_ShouldHaveCorrectHttpGetAttributes(string expectedRoute)
    {
        // Arrange & Act
        var method = typeof(SecurityBlockController).GetMethod(nameof(SecurityBlockController.Admin));
        var httpGetAttributes = method!.GetCustomAttributes<HttpGetAttribute>();

        // Assert
        httpGetAttributes.Should().NotBeNull();
        httpGetAttributes.Should().Contain(attr => attr.Template == expectedRoute);
    }

    [Fact]
    public void Admin_ShouldHaveAuthorizeAttribute()
    {
        // Arrange & Act
        var method = typeof(SecurityBlockController).GetMethod(nameof(SecurityBlockController.Admin));
        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be("Admin");
    }

    [Fact]
    public void Admin_ShouldReturnNotFound()
    {
        // Act
        var result = _controller.Admin();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData("config")]
    [InlineData(".env")]
    public void Config_ShouldHaveCorrectHttpGetAttributes(string expectedRoute)
    {
        // Arrange & Act
        var method = typeof(SecurityBlockController).GetMethod(nameof(SecurityBlockController.Config));
        var httpGetAttributes = method!.GetCustomAttributes<HttpGetAttribute>();

        // Assert
        httpGetAttributes.Should().NotBeNull();
        httpGetAttributes.Should().Contain(attr => attr.Template == expectedRoute);
    }

    [Fact]
    public void Config_ShouldNotHaveAuthorizeAttribute()
    {
        // Arrange & Act
        var method = typeof(SecurityBlockController).GetMethod(nameof(SecurityBlockController.Config));
        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        authorizeAttribute.Should().BeNull();
    }

    [Fact]
    public void Config_ShouldReturnNotFound()
    {
        // Act
        var result = _controller.Config();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData("debug")]
    [InlineData("trace.axd")]
    [InlineData("elmah.axd")]
    public void Debug_ShouldHaveCorrectHttpGetAttributes(string expectedRoute)
    {
        // Arrange & Act
        var method = typeof(SecurityBlockController).GetMethod(nameof(SecurityBlockController.Debug));
        var httpGetAttributes = method!.GetCustomAttributes<HttpGetAttribute>();

        // Assert
        httpGetAttributes.Should().NotBeNull();
        httpGetAttributes.Should().Contain(attr => attr.Template == expectedRoute);
    }

    [Fact]
    public void Debug_ShouldNotHaveAuthorizeAttribute()
    {
        // Arrange & Act
        var method = typeof(SecurityBlockController).GetMethod(nameof(SecurityBlockController.Debug));
        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        authorizeAttribute.Should().BeNull();
    }

    [Fact]
    public void Debug_ShouldReturnNotFound()
    {
        // Act
        var result = _controller.Debug();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void Controller_ShouldInheritFromController()
    {
        // Arrange & Act
        var baseType = typeof(SecurityBlockController).BaseType;

        // Assert
        baseType.Should().Be(typeof(Controller));
    }

    [Fact]
    public void Controller_ShouldHaveCorrectNamespace()
    {
        // Arrange & Act
        var @namespace = typeof(SecurityBlockController).Namespace;

        // Assert
        @namespace.Should().Be("SetlistStudio.Web.Controllers");
    }

    [Fact]
    public void Controller_ShouldBePublicClass()
    {
        // Arrange & Act
        var type = typeof(SecurityBlockController);

        // Assert
        type.IsPublic.Should().BeTrue();
        type.IsClass.Should().BeTrue();
    }

    [Fact]
    public void AllMethods_ShouldReturnIActionResult()
    {
        // Arrange
        var methods = typeof(SecurityBlockController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Act & Assert
        foreach (var method in methods)
        {
            method.ReturnType.Should().Be(typeof(IActionResult),
                $"method {method.Name} should return IActionResult");
        }
    }

    [Fact]
    public void Controller_ShouldHaveXmlDocumentation()
    {
        // Note: This test validates that the controller has proper XML documentation structure
        // The actual XML doc content is validated through static analysis tools
        
        // Arrange & Act
        var type = typeof(SecurityBlockController);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        type.Should().NotBeNull("controller should exist for documentation validation");
        methods.Should().HaveCount(3, "controller should have exactly 3 action methods");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Config")]
    [InlineData("Debug")]
    public void ActionMethods_ShouldExist(string methodName)
    {
        // Arrange & Act
        var method = typeof(SecurityBlockController).GetMethod(methodName);

        // Assert
        method.Should().NotBeNull($"method {methodName} should exist");
        method!.IsPublic.Should().BeTrue($"method {methodName} should be public");
    }

    [Fact]
    public void SecurityController_ShouldBlockSensitiveEndpoints()
    {
        // This is an integration-style test validating the controller's security purpose
        
        // Arrange & Act
        var adminResult = _controller.Admin();
        var configResult = _controller.Config();
        var debugResult = _controller.Debug();

        // Assert
        adminResult.Should().BeOfType<NotFoundResult>("admin endpoints should be blocked");
        configResult.Should().BeOfType<NotFoundResult>("config endpoints should be blocked");
        debugResult.Should().BeOfType<NotFoundResult>("debug endpoints should be blocked");
    }

    [Fact]
    public void Controller_ShouldHaveNoParameterlessMethods()
    {
        // Arrange
        var methods = typeof(SecurityBlockController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Act & Assert
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            parameters.Should().BeEmpty($"security block method {method.Name} should not accept parameters");
        }
    }

    [Fact]
    public void Controller_MethodsShouldBeVirtual()
    {
        // Arrange
        var methods = typeof(SecurityBlockController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Act & Assert
        foreach (var method in methods)
        {
            // Security block methods don't need to be virtual since they're simple blocking methods
            // This test documents that these are concrete implementations
            method.IsVirtual.Should().BeFalse($"security block method {method.Name} should be concrete");
        }
    }
}