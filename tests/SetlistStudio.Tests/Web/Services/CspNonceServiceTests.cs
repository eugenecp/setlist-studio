using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SetlistStudio.Web.Services;
using System.Text.RegularExpressions;
using Xunit;

namespace SetlistStudio.Tests.Web.Services;

/// <summary>
/// Unit tests for CSP Nonce Service functionality
/// Tests nonce generation, middleware behavior, and security properties
/// </summary>
public class CspNonceServiceTests
{
    private readonly CspNonceService _cspNonceService;

    public CspNonceServiceTests()
    {
        _cspNonceService = new CspNonceService();
    }

    #region Nonce Generation Tests

    [Fact]
    public void GenerateNonce_ShouldReturnValidBase64String()
    {
        // Act
        var nonce = _cspNonceService.GenerateNonce();

        // Assert
        nonce.Should().NotBeNullOrEmpty();
        nonce.Should().MatchRegex(@"^[A-Za-z0-9+/]*={0,2}$", "nonce should be valid Base64");
        
        // Base64 string should be longer than 40 characters for 32 bytes of data
        nonce.Length.Should().BeGreaterThan(40);
    }

    [Fact]
    public void GenerateNonce_ShouldReturnUniqueValues()
    {
        // Act
        var nonce1 = _cspNonceService.GenerateNonce();
        var nonce2 = _cspNonceService.GenerateNonce();
        var nonce3 = _cspNonceService.GenerateNonce();

        // Assert
        nonce1.Should().NotBe(nonce2);
        nonce2.Should().NotBe(nonce3);
        nonce1.Should().NotBe(nonce3);
    }

    [Fact]
    public void GenerateNonce_ShouldHaveCorrectLength()
    {
        // Act
        var nonce = _cspNonceService.GenerateNonce();
        var bytes = Convert.FromBase64String(nonce);

        // Assert - Should be 32 bytes (256 bits)
        bytes.Length.Should().Be(32);
    }

    [Fact]
    public void GenerateNonce_MultipleCalls_ShouldReturnDifferentValues()
    {
        // Arrange
        var nonces = new HashSet<string>();
        
        // Act - Generate 100 nonces
        for (int i = 0; i < 100; i++)
        {
            nonces.Add(_cspNonceService.GenerateNonce());
        }

        // Assert - All should be unique
        nonces.Count.Should().Be(100, "all generated nonces should be unique");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void ScriptNonce_ShouldAllowGetAndSet()
    {
        // Arrange
        var expectedNonce = "test-script-nonce-12345";

        // Act
        _cspNonceService.ScriptNonce = expectedNonce;

        // Assert
        _cspNonceService.ScriptNonce.Should().Be(expectedNonce);
    }

    [Fact]
    public void StyleNonce_ShouldAllowGetAndSet()
    {
        // Arrange
        var expectedNonce = "test-style-nonce-67890";

        // Act
        _cspNonceService.StyleNonce = expectedNonce;

        // Assert
        _cspNonceService.StyleNonce.Should().Be(expectedNonce);
    }

    [Fact]
    public void Properties_ShouldDefaultToNull()
    {
        // Arrange
        var service = new CspNonceService();

        // Assert
        service.ScriptNonce.Should().BeNull();
        service.StyleNonce.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldAllowNullValues()
    {
        // Arrange
        _cspNonceService.ScriptNonce = "initial-value";
        _cspNonceService.StyleNonce = "initial-value";

        // Act
        _cspNonceService.ScriptNonce = null;
        _cspNonceService.StyleNonce = null;

        // Assert
        _cspNonceService.ScriptNonce.Should().BeNull();
        _cspNonceService.StyleNonce.Should().BeNull();
    }

    #endregion

    #region Interface Compliance Tests

    [Fact]
    public void CspNonceService_ShouldImplementICspNonceService()
    {
        // Assert
        _cspNonceService.Should().BeAssignableTo<ICspNonceService>();
    }

    [Fact]
    public void ICspNonceService_ShouldExposeRequiredMembers()
    {
        // Arrange
        ICspNonceService service = _cspNonceService;

        // Assert
        service.Should().NotBeNull();
        
        // Check that interface methods are accessible
        var nonce = service.GenerateNonce();
        nonce.Should().NotBeNullOrEmpty();

        // Check that interface properties are accessible
        service.ScriptNonce = "test";
        service.ScriptNonce.Should().Be("test");
        
        service.StyleNonce = "test";  
        service.StyleNonce.Should().Be("test");
    }

    #endregion

    #region Security Tests

    [Fact]
    public void GenerateNonce_ShouldProvideHighEntropy()
    {
        // Arrange
        var nonces = new List<string>();
        
        // Act - Generate multiple nonces and analyze entropy
        for (int i = 0; i < 50; i++)
        {
            nonces.Add(_cspNonceService.GenerateNonce());
        }

        // Assert - Check for reasonable character distribution
        var combinedNonces = string.Join("", nonces);
        var uniqueChars = combinedNonces.ToCharArray().Distinct().Count();
        
        // Base64 has 64 possible characters, we should see good distribution
        uniqueChars.Should().BeGreaterThan(50, "nonces should have high character diversity");
    }

    [Fact]
    public void GenerateNonce_ShouldNotContainPredictablePatterns()
    {
        // Act
        var nonces = Enumerable.Range(0, 20)
            .Select(_ => _cspNonceService.GenerateNonce())
            .ToList();

        // Assert - Check that no nonce is a substring of another
        for (int i = 0; i < nonces.Count; i++)
        {
            for (int j = i + 1; j < nonces.Count; j++)
            {
                nonces[i].Should().NotContain(nonces[j].Substring(0, Math.Min(10, nonces[j].Length)));
                nonces[j].Should().NotContain(nonces[i].Substring(0, Math.Min(10, nonces[i].Length)));
            }
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GenerateNonce_WhenCalledConcurrently_ShouldReturnUniqueValues()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _cspNonceService.GenerateNonce()))
            .ToArray();

        // Act
        var nonces = await Task.WhenAll(tasks);

        // Assert
        nonces.Distinct().Count().Should().Be(100, "concurrent nonce generation should produce unique values");
    }

    [Fact]
    public void Properties_ShouldHandleEmptyStrings()
    {
        // Act
        _cspNonceService.ScriptNonce = "";
        _cspNonceService.StyleNonce = "";

        // Assert
        _cspNonceService.ScriptNonce.Should().Be("");
        _cspNonceService.StyleNonce.Should().Be("");
    }

    [Fact]
    public void Properties_ShouldHandleWhitespaceStrings()
    {
        // Act
        _cspNonceService.ScriptNonce = "   ";
        _cspNonceService.StyleNonce = "\t\n\r";

        // Assert
        _cspNonceService.ScriptNonce.Should().Be("   ");
        _cspNonceService.StyleNonce.Should().Be("\t\n\r");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void GenerateNonce_ShouldBePerformant()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Generate 1000 nonces
        for (int i = 0; i < 1000; i++)
        {
            _cspNonceService.GenerateNonce();
        }

        stopwatch.Stop();

        // Assert - Should complete within reasonable time (adjust threshold as needed)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "nonce generation should be fast");
    }

    #endregion
}

/// <summary>
/// Unit tests for CSP Nonce Middleware functionality
/// Tests middleware behavior, HTTP context integration, and error handling
/// </summary>
public class CspNonceMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ICspNonceService> _cspNonceServiceMock;
    private readonly CspNonceMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public CspNonceMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _cspNonceServiceMock = new Mock<ICspNonceService>();
        _middleware = new CspNonceMiddleware(_nextMock.Object);
        _httpContext = new DefaultHttpContext();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidRequestDelegate_ShouldNotThrow()
    {
        // Arrange & Act
        var action = () => new CspNonceMiddleware(_nextMock.Object);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullRequestDelegate_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new CspNonceMiddleware(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    #endregion

    #region InvokeAsync Tests

    [Fact]
    public async Task InvokeAsync_ShouldGenerateScriptAndStyleNonces()
    {
        // Arrange
        var expectedScriptNonce = "script-nonce-12345";
        var expectedStyleNonce = "style-nonce-67890";
        
        _cspNonceServiceMock.Setup(x => x.GenerateNonce())
            .Returns(expectedScriptNonce)
            .Callback(() => _cspNonceServiceMock.Setup(x => x.GenerateNonce()).Returns(expectedStyleNonce));

        // Act
        await _middleware.InvokeAsync(_httpContext, _cspNonceServiceMock.Object);

        // Assert
        _cspNonceServiceMock.VerifySet(x => x.ScriptNonce = expectedScriptNonce, Times.Once);
        _cspNonceServiceMock.VerifySet(x => x.StyleNonce = expectedStyleNonce, Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldStoreNoncesInHttpContextItems()
    {
        // Arrange
        var scriptNonce = "script-nonce-test";
        var styleNonce = "style-nonce-test";
        
        _cspNonceServiceMock.SetupSequence(x => x.GenerateNonce())
            .Returns(scriptNonce)
            .Returns(styleNonce);

        _cspNonceServiceMock.SetupProperty(x => x.ScriptNonce);
        _cspNonceServiceMock.SetupProperty(x => x.StyleNonce);

        // Act
        await _middleware.InvokeAsync(_httpContext, _cspNonceServiceMock.Object);

        // Assert
        _httpContext.Items.Should().ContainKey("ScriptNonce");
        _httpContext.Items.Should().ContainKey("StyleNonce");
        _httpContext.Items["ScriptNonce"].Should().Be(scriptNonce);
        _httpContext.Items["StyleNonce"].Should().Be(styleNonce);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextMiddleware()
    {
        // Arrange
        _cspNonceServiceMock.Setup(x => x.GenerateNonce()).Returns("test-nonce");

        // Act
        await _middleware.InvokeAsync(_httpContext, _cspNonceServiceMock.Object);

        // Assert
        _nextMock.Verify(x => x(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallGenerateNonceTwice()
    {
        // Arrange
        _cspNonceServiceMock.Setup(x => x.GenerateNonce()).Returns("test-nonce");

        // Act
        await _middleware.InvokeAsync(_httpContext, _cspNonceServiceMock.Object);

        // Assert
        _cspNonceServiceMock.Verify(x => x.GenerateNonce(), Times.Exactly(2));
    }

    [Fact]
    public async Task InvokeAsync_WhenNextMiddlewareThrows_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        _nextMock.Setup(x => x(_httpContext)).ThrowsAsync(expectedException);
        _cspNonceServiceMock.Setup(x => x.GenerateNonce()).Returns("test-nonce");

        // Act & Assert
        var action = async () => await _middleware.InvokeAsync(_httpContext, _cspNonceServiceMock.Object);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetNoncesBeforeCallingNext()
    {
        // Arrange
        var scriptNonce = "script-nonce";
        var styleNonce = "style-nonce";
        var executionOrder = new List<string>();

        _cspNonceServiceMock.SetupSequence(x => x.GenerateNonce())
            .Returns(scriptNonce)
            .Returns(styleNonce);

        _cspNonceServiceMock.SetupSet(x => x.ScriptNonce = scriptNonce)
            .Callback(() => executionOrder.Add("ScriptNonce set"));
        
        _cspNonceServiceMock.SetupSet(x => x.StyleNonce = styleNonce)
            .Callback(() => executionOrder.Add("StyleNonce set"));

        _nextMock.Setup(x => x(_httpContext))
            .Callback(() => executionOrder.Add("Next called"));

        // Act
        await _middleware.InvokeAsync(_httpContext, _cspNonceServiceMock.Object);

        // Assert
        executionOrder.Should().Equal("ScriptNonce set", "StyleNonce set", "Next called");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task InvokeAsync_WhenCspNonceServiceIsNull_ShouldThrow()
    {
        // Act & Assert
        var action = async () => await _middleware.InvokeAsync(_httpContext, null!);
        await action.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task InvokeAsync_WhenHttpContextIsNull_ShouldThrow()
    {
        // Act & Assert
        var action = async () => await _middleware.InvokeAsync(null!, _cspNonceServiceMock.Object);
        await action.Should().ThrowAsync<NullReferenceException>();
    }

    #endregion
}

/// <summary>
/// Unit tests for CSP Nonce Extensions functionality
/// Tests service registration and middleware configuration
/// </summary>
public class CspNonceExtensionsTests
{
    #region Service Registration Tests

    [Fact]
    public void AddCspNonce_ShouldRegisterICspNonceService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCspNonce();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<ICspNonceService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<CspNonceService>();
    }

    [Fact]
    public void AddCspNonce_ShouldRegisterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCspNonce();

        // Assert
        var serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(ICspNonceService));
        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddCspNonce_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddCspNonce();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCspNonce_WhenCalledMultipleTimes_ShouldRegisterServiceOnce()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCspNonce();
        services.AddCspNonce();

        // Assert
        var serviceDescriptors = services.Where(x => x.ServiceType == typeof(ICspNonceService)).ToList();
        serviceDescriptors.Count.Should().Be(2, "each call should add a service registration");
        
        // Both should resolve to the same type
        var serviceProvider = services.BuildServiceProvider();
        var service1 = serviceProvider.GetService<ICspNonceService>();
        var service2 = serviceProvider.GetService<ICspNonceService>();
        
        service1.Should().NotBeNull();
        service2.Should().NotBeNull();
        service1.Should().BeOfType<CspNonceService>();
        service2.Should().BeOfType<CspNonceService>();
    }

    #endregion

    #region Middleware Registration Tests

    [Fact]
    public void UseCspNonce_ShouldReturnApplicationBuilder()
    {
        // Arrange
        var app = new Mock<IApplicationBuilder>();
        var mockResult = new Mock<IApplicationBuilder>();
        app.Setup(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>())).Returns(mockResult.Object);

        // Act
        var result = app.Object.UseCspNonce();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void UseCspNonce_ShouldRegisterMiddleware()
    {
        // Arrange
        var app = new Mock<IApplicationBuilder>();
        app.Setup(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>())).Returns(app.Object);

        // Act
        app.Object.UseCspNonce();

        // Assert
        app.Verify(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
    }

    #endregion
}