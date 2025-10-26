using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SetlistStudio.Web.Pages;
using System.Diagnostics;
using Xunit;
using FluentAssertions;

namespace SetlistStudio.Tests.Web.Pages;

/// <summary>
/// Tests for ErrorModel page handling and security error display
/// </summary>
public class ErrorModelTests
{
    [Fact]
    public void RequestId_ShouldBeNull_Initially()
    {
        // Arrange
        var errorModel = new ErrorModel();

        // Act & Assert
        errorModel.RequestId.Should().BeNull();
    }

    [Fact]
    public void ShowRequestId_ShouldReturnFalse_WhenRequestIdIsNull()
    {
        // Arrange
        var errorModel = new ErrorModel();

        // Act
        var result = errorModel.ShowRequestId;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShowRequestId_ShouldReturnFalse_WhenRequestIdIsEmpty()
    {
        // Arrange
        var errorModel = new ErrorModel { RequestId = string.Empty };

        // Act
        var result = errorModel.ShowRequestId;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShowRequestId_ShouldReturnTrue_WhenRequestIdHasValue()
    {
        // Arrange
        var errorModel = new ErrorModel { RequestId = "test-request-id" };

        // Act
        var result = errorModel.ShowRequestId;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OnGet_ShouldSetRequestId_WhenActivityCurrentExists()
    {
        // Arrange
        var errorModel = new ErrorModel();
        using var activity = new Activity("TestActivity");
        activity.Start();

        try
        {
            // Create HttpContext
            var httpContext = new DefaultHttpContext();
            errorModel.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            // Act
            errorModel.OnGet();

            // Assert
            errorModel.RequestId.Should().NotBeNullOrEmpty();
            errorModel.RequestId.Should().Be(activity.Id);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void OnGet_ShouldSetRequestIdToTraceIdentifier_WhenNoCurrentActivity()
    {
        // Arrange
        var errorModel = new ErrorModel();
        var expectedTraceId = "test-trace-id";
        
        // Create HttpContext with trace identifier
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = expectedTraceId;
        
        errorModel.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        // Ensure no current activity
        Activity.Current?.Stop();

        // Act
        errorModel.OnGet();

        // Assert
        errorModel.RequestId.Should().Be(expectedTraceId);
    }

    [Fact]
    public void OnGet_ShouldHandleNullActivity_AndNullTraceIdentifier()
    {
        // Arrange
        var errorModel = new ErrorModel();
        
        // Create HttpContext without trace identifier
        var httpContext = new DefaultHttpContext();
        
        errorModel.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        // Ensure no current activity
        Activity.Current?.Stop();

        // Act
        errorModel.OnGet();

        // Assert
        errorModel.RequestId.Should().NotBeNullOrEmpty();
        errorModel.RequestId.Should().Be(httpContext.TraceIdentifier);
    }

    [Fact]
    public void ErrorModel_ShouldHaveCorrectAttributes()
    {
        // Arrange & Act
        var type = typeof(ErrorModel);
        
        // Assert - Check ResponseCache attribute
        var responseCacheAttribute = type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ResponseCacheAttribute), false)
            .Cast<Microsoft.AspNetCore.Mvc.ResponseCacheAttribute>()
            .FirstOrDefault();
        
        responseCacheAttribute.Should().NotBeNull();
        responseCacheAttribute!.Duration.Should().Be(0);
        responseCacheAttribute.Location.Should().Be(Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None);
        responseCacheAttribute.NoStore.Should().BeTrue();

        // Assert - Check IgnoreAntiforgeryToken attribute
        var ignoreAntiforgeryAttribute = type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute), false)
            .Cast<Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute>()
            .FirstOrDefault();
        
        ignoreAntiforgeryAttribute.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShowRequestId_ShouldReturnFalse_ForNullOrEmptyRequestId(string? requestId)
    {
        // Arrange
        var errorModel = new ErrorModel { RequestId = requestId };

        // Act
        var result = errorModel.ShowRequestId;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShowRequestId_ShouldReturnTrue_ForWhitespaceRequestId()
    {
        // Arrange
        var errorModel = new ErrorModel { RequestId = "   " };

        // Act
        var result = errorModel.ShowRequestId;

        // Assert
        // Note: ShowRequestId uses string.IsNullOrEmpty which returns false for whitespace
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("simple-id")]
    [InlineData("complex-trace-id-12345")]
    [InlineData("guid-style-id")]
    public void ShowRequestId_ShouldReturnTrue_ForValidRequestIds(string requestId)
    {
        // Arrange
        var errorModel = new ErrorModel { RequestId = requestId };

        // Act
        var result = errorModel.ShowRequestId;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ErrorModel_ShouldInheritFromPageModel()
    {
        // Arrange & Act
        var errorModel = new ErrorModel();

        // Assert
        errorModel.Should().BeAssignableTo<PageModel>();
    }

    [Fact]
    public void OnGet_ShouldNotExposeExceptionDetails()
    {
        // Arrange
        var errorModel = new ErrorModel();
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "safe-trace-id";
        
        errorModel.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        // Act
        errorModel.OnGet();

        // Assert
        // Verify that only safe trace identifier is exposed, no exception details
        errorModel.RequestId.Should().Be("safe-trace-id");
        errorModel.RequestId.Should().NotContain("exception");
        errorModel.RequestId.Should().NotContain("error");
        errorModel.RequestId.Should().NotContain("stack");
    }
}