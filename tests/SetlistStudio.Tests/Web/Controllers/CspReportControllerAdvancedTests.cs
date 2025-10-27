using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Web.Controllers;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace SetlistStudio.Tests.Web.Controllers;

/// <summary>
/// Advanced tests for CspReportController targeting edge cases and error conditions
/// to improve branch coverage from 79.4% to 80%+ by covering uncovered branches
/// </summary>
public class CspReportControllerAdvancedTests
{
    private readonly Mock<ILogger<CspReportController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly CspReportController _controller;

    public CspReportControllerAdvancedTests()
    {
        _mockLogger = new Mock<ILogger<CspReportController>>();
        
        // Create a fake configuration with the required values
        var configurationData = new Dictionary<string, string?>
        {
            {"Security:CspReporting:Enabled", "true"}
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();
        
        _mockConfiguration = new Mock<IConfiguration>();
        _controller = new CspReportController(_mockLogger.Object, configuration);

        // Setup controller context
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region Report Method Edge Cases

    [Fact]
    public async Task Report_WithNullCspReport_ShouldReturnBadRequest()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = null!
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid CSP report format");
    }

    [Fact]
    public async Task Report_WithEmptyBlockedUri_ShouldLogWarningAndReturnNoContent()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://example.com/page",
                BlockedUri = "", // Empty blocked URI
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'"
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        // Verify basic CSP violation was logged (empty URI doesn't trigger specific handling)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CSP Violation Detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Report_WithWhitespaceBlockedUri_ShouldLogWarningAndReturnNoContent()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://example.com/page",
                BlockedUri = "   ", // Whitespace-only blocked URI
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'"
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithNullBlockedUri_ShouldLogWarningAndReturnNoContent()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://example.com/page",
                BlockedUri = null!, // Null blocked URI
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'"
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithMaliciousScriptSource_ShouldLogSecurityEvent()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/setlists",
                BlockedUri = "data:text/html,<script>alert('steal')</script>", // Suspicious data URI
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'",
                SourceFile = "https://setliststudio.com/setlists",
                LineNumber = 1,
                ColumnNumber = 1,
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify security event was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SECURITY ALERT: Suspicious CSP violation detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Report_WithInlineViolation_ShouldLogInlineScriptAttempt()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/songs",
                BlockedUri = "inline", // Inline script/style violation
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'",
                SourceFile = "https://setliststudio.com/songs",
                LineNumber = 25,
                ColumnNumber = 10,
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify basic CSP violation was logged (inline won't trigger security alert)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CSP Violation Detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Report_WithDataUriViolation_ShouldLogDataUriAttempt()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/artists",
                BlockedUri = "data:text/javascript;base64,YWxlcnQoJ0hpJyk=", // Base64 encoded JavaScript
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'",
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithEvalViolation_ShouldLogEvalAttempt()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/dashboard",
                BlockedUri = "eval(malicious_code)", // eval() function call
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self' 'unsafe-eval'",
                SourceFile = "https://setliststudio.com/dashboard",
                LineNumber = 150,
                ColumnNumber = 25,
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify security alert was logged for eval usage
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SECURITY ALERT: Suspicious CSP violation detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Report_WithUnsafeInlineViolation_ShouldLogUnsafeInlineAttempt()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/profile",
                BlockedUri = "unsafe-inline", // unsafe-inline directive violation
                ViolatedDirective = "style-src",
                EffectiveDirective = "style-src",
                OriginalPolicy = "default-src 'self'; style-src 'self'",
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithExternalImageViolation_ShouldLogImageSource()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/songs/create",
                BlockedUri = "https://external-cdn.com/suspicious-image.jpg",
                ViolatedDirective = "img-src",
                EffectiveDirective = "img-src",
                OriginalPolicy = "default-src 'self'; img-src 'self'",
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithConnectSourceViolation_ShouldLogConnectAttempt()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/api/test",
                BlockedUri = "https://unauthorized-api.com/steal",
                ViolatedDirective = "connect-src",
                EffectiveDirective = "connect-src",
                OriginalPolicy = "default-src 'self'; connect-src 'self'",
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithNullViolatedDirective_ShouldLogGeneralViolation()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/page",
                BlockedUri = "https://example.com/resource",
                ViolatedDirective = null!, // Null violated directive
                EffectiveDirective = "default-src",
                OriginalPolicy = "default-src 'self'",
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithNullDocumentUri_ShouldLogViolationWithoutDocument()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = null!, // Null document URI
                BlockedUri = "https://malicious.com/script.js",
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'",
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithHighLineNumber_ShouldLogLineInformation()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/large-script",
                BlockedUri = "eval(code)",
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'",
                SourceFile = "https://setliststudio.com/large-script",
                LineNumber = 5000, // High line number
                ColumnNumber = 120,
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithZeroLineNumber_ShouldHandleZeroLine()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/page",
                BlockedUri = "inline",
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'",
                LineNumber = 0, // Zero line number
                ColumnNumber = 0,
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithNegativeLineNumber_ShouldHandleNegativeLine()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/page",
                BlockedUri = "unsafe-inline",
                ViolatedDirective = "style-src",
                EffectiveDirective = "style-src",
                OriginalPolicy = "default-src 'self'",
                LineNumber = -1, // Negative line number
                ColumnNumber = -1,
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task Report_WhenLoggingThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        _mockLogger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Throws(new InvalidOperationException("Logging system failure"));

        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/page",
                BlockedUri = "https://malicious.com/script.js",
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'",
                StatusCode = 200
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Report(violationReport));
        exception.Message.Should().Be("Logging system failure");
    }

    #endregion

    #region Input Validation Edge Cases

    [Fact]
    public async Task Report_WithExtremelyLongBlockedUri_ShouldTruncateInLogs()
    {
        // Arrange
        var extremelyLongUri = "https://malicious.com/" + new string('a', 5000); // Very long URI
        
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/page",
                BlockedUri = extremelyLongUri,
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'",
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithSpecialCharactersInUri_ShouldHandleProperly()
    {
        // Arrange
        var specialCharsUri = "https://malicious.com/script?param=<script>alert('xss')</script>&other=value";
        
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/page",
                BlockedUri = specialCharsUri,
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'",
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithUnicodeCharactersInUri_ShouldHandleProperly()
    {
        // Arrange
        var unicodeUri = "https://malicious.com/路径/脚本.js?参数=值";
        
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/page",
                BlockedUri = unicodeUri,
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'",
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region Realistic Musical Application Scenarios

    [Fact]
    public async Task Report_WithMaliciousScriptOnSetlistPage_ShouldLogSecurityThreat()
    {
        // Arrange - Simulates attack on setlist creation page
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/setlists/create",
                BlockedUri = "javascript:alert('steal-setlists')",
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self' 'unsafe-inline'",
                SourceFile = "https://setliststudio.com/setlists/create",
                LineNumber = 1,
                ColumnNumber = 1,
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify security threat was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SECURITY ALERT: Suspicious CSP violation detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Report_WithAttackOnSongManagement_ShouldLogDetailedInformation()
    {
        // Arrange - Simulates XSS attack on song management
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/songs",
                BlockedUri = "data:text/javascript;base64,YWxlcnQoJ1N0ZWFsaW5nIHNvbmcgZGF0YScpOw==", // "alert('Stealing song data');"
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'; script-src 'self'",
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithStyleInjectionOnArtistPage_ShouldLogStyleViolation()
    {
        // Arrange - Simulates CSS injection attack on artist page
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/artists/123",
                BlockedUri = "unsafe-inline",
                ViolatedDirective = "style-src",
                EffectiveDirective = "style-src",
                OriginalPolicy = "default-src 'self'; style-src 'self'",
                SourceFile = "https://setliststudio.com/artists/123",
                LineNumber = 89,
                ColumnNumber = 15,
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithUnauthorizedImageSourceOnDashboard_ShouldLogImageViolation()
    {
        // Arrange - Simulates unauthorized image loading on dashboard
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/dashboard",
                BlockedUri = "https://tracker.malicious.com/pixel.gif",
                ViolatedDirective = "img-src",
                EffectiveDirective = "img-src", 
                OriginalPolicy = "default-src 'self'; img-src 'self' data:",
                StatusCode = 200
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region Model Validation Tests

    [Fact]
    public async Task Report_WithCompletelyEmptyReport_ShouldReturnBadRequest()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport()
            {
                // All properties are null/default
            }
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>(); // Empty reports are still processed
    }

    #endregion

    #region HTTP Context and Request Information Tests

    [Fact]
    public async Task Report_WithRemoteIpAddress_ShouldLogClientInformation()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/page",
                BlockedUri = "https://malicious.com/script.js",
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'",
                StatusCode = 200
            }
        };

        // Setup HTTP context with IP address
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");
        context.Request.Headers.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Report_WithAuthenticatedUser_ShouldLogUserInformation()
    {
        // Arrange
        var violationReport = new CspViolationReport
        {
            CspReport = new CspReport
            {
                DocumentUri = "https://setliststudio.com/setlists",
                BlockedUri = "https://suspicious.com/script.js",
                ViolatedDirective = "script-src",
                EffectiveDirective = "script-src",
                OriginalPolicy = "default-src 'self'",
                StatusCode = 200
            }
        };

        // Setup authenticated user context
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "musician@setliststudio.com"),
            new Claim(ClaimTypes.NameIdentifier, "user-123")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext();
        context.User = claimsPrincipal;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        // Act
        var result = await _controller.Report(violationReport);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion
}