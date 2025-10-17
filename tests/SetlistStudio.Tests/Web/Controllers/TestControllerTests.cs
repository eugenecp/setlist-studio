using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Security.Claims;
using Xunit;
using FluentAssertions;
using SetlistStudio.Web.Controllers;

namespace SetlistStudio.Tests.Web.Controllers;

/// <summary>
/// Test implementation of IWebHostEnvironment for controller testing
/// </summary>
public class TestWebHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Test";
    public string ApplicationName { get; set; } = "SetlistStudio.Tests";
    public string ContentRootPath { get; set; } = "/test/content";
    public IFileProvider ContentRootFileProvider { get; set; } = new Mock<IFileProvider>().Object;
    public string WebRootPath { get; set; } = "/test/wwwroot";
    public IFileProvider WebRootFileProvider { get; set; } = new Mock<IFileProvider>().Object;
}

/// <summary>
/// Tests for TestController endpoints
/// Focuses on environment-specific behavior and security test functionality
/// </summary>
public class TestControllerTests
{
    private readonly TestWebHostEnvironment _testEnvironment;
    private readonly TestController _controller;

    public TestControllerTests()
    {
        _testEnvironment = new TestWebHostEnvironment();
        _controller = new TestController(_testEnvironment);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Test")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task CreateTestSession_InAllowedEnvironments_CreatesSession(string environmentName)
    {
        // Arrange
        _testEnvironment.EnvironmentName = environmentName;
        
        var session = new Mock<ISession>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };

        var testContent = "test-session-content";

        // Act
        var result = await _controller.CreateTestSession(testContent);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        session.Verify(x => x.SetString("TestKey", testContent), Times.Once);
        session.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTestSession_InProductionWithoutTestFactory_ReturnsNotFound()
    {
        // Arrange
        _testEnvironment.EnvironmentName = "Production";

        // Act
        var result = await _controller.CreateTestSession("test");

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateTestSession_WithNullContent_UsesDefaultValue()
    {
        // Arrange
        _testEnvironment.EnvironmentName = "Development";
        
        var session = new Mock<ISession>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };

        // Act
        var result = await _controller.CreateTestSession(null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        session.Verify(x => x.SetString("TestKey", "test-value"), Times.Once);
    }

    [Fact]
    public async Task CreateTestSession_ReturnsSessionId()
    {
        // Arrange
        _testEnvironment.EnvironmentName = "Development";
        
        var sessionId = "test-session-id-123";
        var session = new Mock<ISession>();
        session.Setup(x => x.Id).Returns(sessionId);
        
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };

        // Act
        var result = await _controller.CreateTestSession("test");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;

        var responseType = response!.GetType();
        var sessionIdProperty = responseType.GetProperty("sessionId");
        sessionIdProperty!.GetValue(response).Should().Be(sessionId);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Test")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void TestAuthEndpoint_InAllowedEnvironments_ReturnsOk(string environmentName)
    {
        // Arrange
        _testEnvironment.EnvironmentName = environmentName;
        
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
        var result = _controller.TestAuthEndpoint();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;

        var responseType = response!.GetType();
        var messageProperty = responseType.GetProperty("message");
        var userProperty = responseType.GetProperty("user");

        messageProperty!.GetValue(response).Should().Be("Authenticated");
        userProperty!.GetValue(response).Should().Be("testuser");
    }

    [Fact]
    public void TestAuthEndpoint_InDisallowedEnvironment_ReturnsNotFound()
    {
        // Arrange
        _testEnvironment.EnvironmentName = "SomeOtherEnvironment";

        // Act
        var result = _controller.TestAuthEndpoint();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void TestAuthEndpoint_WithUnauthenticatedUser_ReturnsOkWithNullUser()
    {
        // Arrange
        _testEnvironment.EnvironmentName = "Test";
        
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
        var result = _controller.TestAuthEndpoint();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;

        var responseType = response!.GetType();
        var userProperty = responseType.GetProperty("user");
        userProperty!.GetValue(response).Should().BeNull();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Test")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void TestAntiforgery_InAllowedEnvironments_ReturnsToken(string environmentName)
    {
        // Arrange
        _testEnvironment.EnvironmentName = environmentName;
        
        var antiforgeryTokens = new AntiforgeryTokenSet("test-request-token", "test-cookie-token", "test-form-field", "test-header");
        var mockAntiforgery = new Mock<IAntiforgery>();
        mockAntiforgery.Setup(x => x.GetAndStoreTokens(It.IsAny<HttpContext>()))
                      .Returns(antiforgeryTokens);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetRequiredService<IAntiforgery>())
                      .Returns(mockAntiforgery.Object);

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.RequestServices).Returns(serviceProvider.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };

        // Act
        var result = _controller.TestAntiforgery();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;

        var responseType = response!.GetType();
        var tokenProperty = responseType.GetProperty("token");
        tokenProperty!.GetValue(response).Should().Be("test-request-token");

        mockAntiforgery.Verify(x => x.GetAndStoreTokens(httpContext.Object), Times.Once);
    }

    [Fact]
    public void TestAntiforgery_InDisallowedEnvironment_ReturnsNotFound()
    {
        // Arrange
        _testEnvironment.EnvironmentName = "SomeOtherEnvironment";

        // Act
        var result = _controller.TestAntiforgery();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void TestAuthEndpoint_InDevelopmentEnvironment_ReturnsOk()
    {
        // Arrange
        _testEnvironment.EnvironmentName = "Development";
        
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "devuser") }, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        // Act
        var result = _controller.TestAuthEndpoint();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;

        var responseType = response!.GetType();
        var userProperty = responseType.GetProperty("user");
        userProperty!.GetValue(response).Should().Be("devuser");
    }

    [Fact]
    public void TestAntiforgery_InDevelopmentEnvironment_ReturnsToken()
    {
        // Arrange
        _testEnvironment.EnvironmentName = "Development";
        
        var antiforgeryTokens = new AntiforgeryTokenSet("dev-request-token", "dev-cookie-token", "dev-form-field", "dev-header");
        var mockAntiforgery = new Mock<IAntiforgery>();
        mockAntiforgery.Setup(x => x.GetAndStoreTokens(It.IsAny<HttpContext>()))
                      .Returns(antiforgeryTokens);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetRequiredService<IAntiforgery>())
                      .Returns(mockAntiforgery.Object);

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.RequestServices).Returns(serviceProvider.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };

        // Act
        var result = _controller.TestAntiforgery();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;

        var responseType = response!.GetType();
        var tokenProperty = responseType.GetProperty("token");
        tokenProperty!.GetValue(response).Should().Be("dev-request-token");
    }

    [Fact]
    public async Task CreateTestSession_InDevelopmentEnvironment_CreatesSession()
    {
        // Arrange
        _testEnvironment.EnvironmentName = "Development";
        
        var session = new Mock<ISession>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };

        // Act
        var result = await _controller.CreateTestSession("dev-test-content");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        session.Verify(x => x.SetString("TestKey", "dev-test-content"), Times.Once);
        session.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}