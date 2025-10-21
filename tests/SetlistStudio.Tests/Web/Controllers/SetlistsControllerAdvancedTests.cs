using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Web.Controllers;
using SetlistStudio.Web.Models;
using System.Security.Claims;
using Xunit;
using FluentAssertions;

namespace SetlistStudio.Tests.Web.Controllers;

/// <summary>
/// Advanced tests for SetlistsController targeting edge cases, error conditions, and validation boundaries
/// to improve branch coverage from 58.8% to 80%+ by covering uncovered branches
/// </summary>
public class SetlistsControllerAdvancedTests
{
    private readonly Mock<ISetlistService> _mockSetlistService;
    private readonly Mock<ILogger<SetlistsController>> _mockLogger;
    private readonly SetlistsController _controller;

    public SetlistsControllerAdvancedTests()
    {
        _mockSetlistService = new Mock<ISetlistService>();
        _mockLogger = new Mock<ILogger<SetlistsController>>();
        _controller = new SetlistsController(_mockSetlistService.Object, _mockLogger.Object);
    }

    #region GetSetlists Advanced Tests

    [Fact]
    public async Task GetSetlists_WithoutAuthenticatedUser_ThrowsException()
    {
        // Arrange - No authentication setup (User.Identity?.Name returns null)
        // This causes the service to throw an exception because of authentication issues
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("anonymous", null, null, null, 1, 20))
            .ThrowsAsync(new UnauthorizedAccessException("User not authenticated"));

        // Act
        var result = await _controller.GetSetlists();

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Internal server error");
    }

    [Fact]
    public async Task GetSetlists_WithLargePage_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>();
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", null, null, null, 999, 100))
            .ReturnsAsync((testSetlists, 0));

        // Act
        var result = await _controller.GetSetlists(page: 999, limit: 100);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSetlists_WithSetlistContainingNullSongs_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>
        {
            new Setlist 
            { 
                Id = 1, 
                Name = "Test Setlist", 
                UserId = "test-user", 
                CreatedAt = DateTime.UtcNow,
                SetlistSongs = null! // This should trigger the null check branch
            }
        };
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", null, null, null, 1, 20))
            .ReturnsAsync((testSetlists, 1));

        // Act
        var result = await _controller.GetSetlists();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as IEnumerable<SetlistResponse>;
        response?.First().SongCount.Should().Be(0); // Should handle null SetlistSongs
    }

    #endregion

    #region SearchSetlists Advanced Tests

    [Fact]
    public async Task SearchSetlists_WithNullQuery_HandlesGracefully()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>();
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", null, null, null, 1, 20))
            .ReturnsAsync((testSetlists, 0));

        // Act
        var result = await _controller.SearchSetlists(null!);

        // Assert - Controller handles null query by treating it as normal search
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SearchSetlists_WithEmptyQuery_HandlesGracefully()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>();
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", "", null, null, 1, 20))
            .ReturnsAsync((testSetlists, 0));

        // Act
        var result = await _controller.SearchSetlists("");

        // Assert - Controller handles empty query as normal search
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("vbscript:msgbox('test')")]
    [InlineData("<iframe src='malicious'></iframe>")]
    [InlineData("<object data='evil'></object>")]
    [InlineData("<embed src='bad'></embed>")]
    [InlineData("onload=alert('xss')")]
    [InlineData("onerror=alert('test')")]
    [InlineData("document.cookie")]
    [InlineData("window.location")]
    [InlineData("eval(malicious)")]
    [InlineData("base64,dGVzdA==")]
    [InlineData("data:text/html,<script>")]
    public async Task SearchSetlists_WithVariousMaliciousPatterns_ReturnsBadRequest(string maliciousQuery)
    {
        // Arrange
        SetupAuthenticatedUser("test-user");

        // Act
        var result = await _controller.SearchSetlists(maliciousQuery);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid search query");
    }

    [Fact]
    public async Task SearchSetlists_WithMaliciousQueryCaseInsensitive_ReturnsBadRequest()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");

        // Act
        var result = await _controller.SearchSetlists("<SCRIPT>alert('XSS')</SCRIPT>");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid search query");
    }

    [Fact]
    public async Task SearchSetlists_WithoutAuthenticatedUser_ThrowsException()
    {
        // Arrange - No authentication setup
        var query = "test query";
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("anonymous", query, null, null, 1, 20))
            .ThrowsAsync(new UnauthorizedAccessException("User not authenticated"));

        // Act
        var result = await _controller.SearchSetlists(query);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Internal server error");
    }

    [Fact]
    public async Task SearchSetlists_WhenServiceThrowsException_ReturnsServiceUnavailable()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var query = "valid query";
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", query, null, null, 1, 20))
            .ThrowsAsync(new InvalidOperationException("Search service unavailable"));

        // Act
        var result = await _controller.SearchSetlists(query);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
        statusResult.Value.Should().Be("Search service temporarily unavailable");
    }

    #endregion

    #region CreateSetlist Advanced Tests

    [Theory]
    [InlineData("javascript:alert('xss')", "Valid description")]
    [InlineData("Valid name", "vbscript:msgbox('test')")]
    [InlineData("<iframe>malicious</iframe>", "Valid description")]
    [InlineData("Valid name", "<object>malicious</object>")]
    [InlineData("onload=alert('xss')", "Valid description")]
    [InlineData("Valid name", "onerror=eval('bad')")]
    [InlineData("document.cookie", "Valid description")]
    [InlineData("Valid name", "window.location")]
    [InlineData("base64,dGVzdA==", "Valid description")]
    [InlineData("Valid name", "data:text/html,<script>")]
    public async Task CreateSetlist_WithMaliciousContentInEitherField_ReturnsBadRequest(string name, string description)
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSetlistRequest
        {
            Name = name,
            Description = description
        };

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid setlist data");
    }

    [Fact]
    public async Task CreateSetlist_WithNullName_ReturnsBadRequestFromModelValidation()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSetlistRequest
        {
            Name = null!,
            Description = "Valid description"
        };
        
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateSetlist_WithNullDescription_AllowsNull()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSetlistRequest
        {
            Name = "Valid Name",
            Description = null
        };

        var createdSetlist = new Setlist
        {
            Id = 1,
            Name = request.Name,
            Description = request.Description,
            UserId = "test-user",
            CreatedAt = DateTime.UtcNow
        };

        _mockSetlistService
            .Setup(s => s.CreateSetlistAsync(It.IsAny<Setlist>()))
            .ReturnsAsync(createdSetlist);

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateSetlist_WithoutAuthenticatedUser_ThrowsException()
    {
        // Arrange - No authentication setup
        var request = new CreateSetlistRequest
        {
            Name = "Test Setlist",
            Description = "Test Description"
        };

        _mockSetlistService
            .Setup(s => s.CreateSetlistAsync(It.Is<Setlist>(s => s.UserId == "anonymous")))
            .ThrowsAsync(new UnauthorizedAccessException("User not authenticated"));

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Internal server error");
    }

    #endregion

    #region GetSetlist Advanced Tests

    [Fact]
    public async Task GetSetlist_WithZeroId_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        
        _mockSetlistService
            .Setup(s => s.GetSetlistByIdAsync(0, "test-user"))
            .ReturnsAsync((Setlist?)null);

        // Act
        var result = await _controller.GetSetlist(0);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSetlist_WithNegativeId_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        
        _mockSetlistService
            .Setup(s => s.GetSetlistByIdAsync(-1, "test-user"))
            .ReturnsAsync((Setlist?)null);

        // Act
        var result = await _controller.GetSetlist(-1);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSetlist_WithoutAuthenticatedUser_ThrowsException()
    {
        // Arrange - No authentication setup
        _mockSetlistService
            .Setup(s => s.GetSetlistByIdAsync(1, "anonymous"))
            .ThrowsAsync(new UnauthorizedAccessException("User not authenticated"));

        // Act
        var result = await _controller.GetSetlist(1);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Internal server error");
    }

    [Fact]
    public async Task GetSetlist_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        
        _mockSetlistService
            .Setup(s => s.GetSetlistByIdAsync(1, "test-user"))
            .ThrowsAsync(new TimeoutException("Database timeout"));

        // Act
        var result = await _controller.GetSetlist(1);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Internal server error");
    }

    [Fact]
    public async Task GetSetlist_WithSetlistContainingNullSongs_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var setlist = new Setlist
        {
            Id = 1,
            Name = "Test Setlist",
            Description = "Test Description",
            UserId = "test-user",
            CreatedAt = DateTime.UtcNow,
            SetlistSongs = null! // This should trigger the null check branch
        };

        _mockSetlistService
            .Setup(s => s.GetSetlistByIdAsync(1, "test-user"))
            .ReturnsAsync(setlist);

        // Act
        var result = await _controller.GetSetlist(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as SetlistResponse;
        response?.SongCount.Should().Be(0); // Should handle null SetlistSongs
    }

    #endregion

    #region Malicious Content Detection Tests

    [Theory]
    [InlineData(null, false)] // Null content should return false
    [InlineData("", false)]   // Empty content should return false
    [InlineData("   ", false)] // Whitespace content should return false
    [InlineData("Safe content", false)] // Safe content should return false
    [InlineData("Normal <b>HTML</b>", false)] // Normal HTML should return false
    public async Task ContainsMaliciousContent_WithVariousInputs_ReturnsExpectedResult(string? input, bool expectedResult)
    {
        // This tests the private method indirectly through CreateSetlist
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSetlistRequest
        {
            Name = input ?? "Safe Name",
            Description = "Safe Description"
        };

        if (expectedResult)
        {
            // Act
            var result = await _controller.CreateSetlist(request);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
        else
        {
            var createdSetlist = new Setlist
            {
                Id = 1,
                Name = request.Name,
                Description = request.Description,
                UserId = "test-user",
                CreatedAt = DateTime.UtcNow
            };

            _mockSetlistService
                .Setup(s => s.CreateSetlistAsync(It.IsAny<Setlist>()))
                .ReturnsAsync(createdSetlist);

            // Act
            var result = await _controller.CreateSetlist(request);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }
    }

    #endregion

    #region Additional Edge Cases for Branch Coverage

    [Fact]
    public async Task GetSetlists_WithSpecificPaginationEdgeCases_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>();
        
        // Test with page 0 (should default to 1)
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", null, null, null, 0, 20))
            .ReturnsAsync((testSetlists, 0));

        // Act
        var result = await _controller.GetSetlists(page: 0);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSetlists_WithLimitZero_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>();
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", null, null, null, 1, 0))
            .ReturnsAsync((testSetlists, 0));

        // Act
        var result = await _controller.GetSetlists(limit: 0);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData("SELECT * FROM")]
    [InlineData("'; DROP TABLE")]
    [InlineData("UNION SELECT")]
    [InlineData("INSERT INTO")]
    [InlineData("DELETE FROM")]
    [InlineData("UPDATE SET")]
    public async Task SearchSetlists_WithSqlInjectionPatterns_AllowedByCurrentValidator(string sqlPattern)
    {
        // NOTE: Current ContainsMaliciousContent only checks specific patterns, not SQL injection
        // These patterns will pass through and be treated as valid searches
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>();
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", $"song {sqlPattern} hack", null, null, 1, 20))
            .ReturnsAsync((testSetlists, 0));

        // Act
        var result = await _controller.SearchSetlists($"song {sqlPattern} hack");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>(); // Should pass through current validation
    }

    [Theory]
    [InlineData("&#xss")]
    [InlineData("%3Cscript%3E")]
    [InlineData("&lt;script&gt;")]
    public async Task SearchSetlists_WithEncodedXssPatterns_AllowedByCurrentValidator(string encodedPattern)
    {
        // NOTE: Current ContainsMaliciousContent only checks specific literal patterns
        // Encoded patterns will pass through current validation
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>();
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", $"test {encodedPattern}script", null, null, 1, 20))
            .ReturnsAsync((testSetlists, 0));

        // Act
        var result = await _controller.SearchSetlists($"test {encodedPattern}script");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>(); // Should pass through current validation
    }

    [Fact]
    public async Task CreateSetlist_WithExtremelylongName_PassesMaliciousCheckButFailsValidation()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var longName = new string('A', 1000); // Very long but not malicious
        var request = new CreateSetlistRequest
        {
            Name = longName,
            Description = "Valid description"
        };

        // Add model validation error for long name
        _controller.ModelState.AddModelError("Name", "Name is too long");

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateSetlist_WithEmptyName_FailsModelValidation()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSetlistRequest
        {
            Name = "",
            Description = "Valid description"
        };

        _controller.ModelState.AddModelError("Name", "Name cannot be empty");

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateSetlist_WithWhitespaceOnlyName_FailsModelValidation()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSetlistRequest
        {
            Name = "   ",
            Description = "Valid description"
        };

        _controller.ModelState.AddModelError("Name", "Name cannot be whitespace only");

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetSetlist_WithMaxIntId_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        
        _mockSetlistService
            .Setup(s => s.GetSetlistByIdAsync(int.MaxValue, "test-user"))
            .ReturnsAsync((Setlist?)null);

        // Act
        var result = await _controller.GetSetlist(int.MaxValue);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SearchSetlists_WhenServiceReturnsEmptySetlists_HandlesGracefully()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", "test", null, null, 1, 20))
            .ReturnsAsync((new List<Setlist>(), 0)); // Return empty list instead of null

        // Act
        var result = await _controller.SearchSetlists("test");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSetlists_WhenServiceReturnsEmptySetlists_HandlesGracefully()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", null, null, null, 1, 20))
            .ReturnsAsync((new List<Setlist>(), 0)); // Return empty list instead of null

        // Act
        var result = await _controller.GetSetlists();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateSetlist_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSetlistRequest
        {
            Name = "Test Setlist",
            Description = "Test Description"
        };

        _mockSetlistService
            .Setup(s => s.CreateSetlistAsync(It.IsAny<Setlist>()))
            .ThrowsAsync(new NullReferenceException("Setlist service returned null"));

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Internal server error");
    }

    [Theory]
    [InlineData("test<SCRIPT>")]
    [InlineData("<IFRAME src=x>")]
    [InlineData("JAVASCRIPT:alert")]
    [InlineData("VBSCRIPT:msgbox")]
    [InlineData("ONLOAD=evil")]
    [InlineData("DOCUMENT.COOKIE")]
    public async Task CreateSetlist_WithMaliciousContentUppercase_ReturnsBadRequest(string maliciousContent)
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSetlistRequest
        {
            Name = maliciousContent,
            Description = "Valid description"
        };

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid setlist data");
    }

    [Fact]
    public async Task GetSetlists_WithDatabaseConnectionException_ReturnsServiceUnavailable()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", null, null, null, 1, 20))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _controller.GetSetlists();

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
        statusResult.Value.Should().Be("Service temporarily unavailable");
    }

    [Fact]
    public async Task CreateSetlist_WithDatabaseConstraintException_ReturnsServiceUnavailable()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSetlistRequest
        {
            Name = "Test Setlist",
            Description = "Test Description"
        };

        _mockSetlistService
            .Setup(s => s.CreateSetlistAsync(It.IsAny<Setlist>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate setlist name"));

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
        statusResult.Value.Should().Be("Service temporarily unavailable");
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #region Additional Coverage Tests

    [Fact]
    public async Task GetSetlists_WithDifferentPageCombinations_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>
        {
            new Setlist { Id = 1, Name = "Test Setlist 1", UserId = "test-user", CreatedAt = DateTime.UtcNow },
            new Setlist { Id = 2, Name = "Test Setlist 2", UserId = "test-user", CreatedAt = DateTime.UtcNow }
        };

        // Setup different parameter combinations to ensure all branches are hit
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", null, null, null, 2, 10))
            .ReturnsAsync((testSetlists, 2));

        // Act - Test with different page and limit combination
        var result = await _controller.GetSetlists(2, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var setlists = okResult.Value as IEnumerable<SetlistResponse>;
        setlists.Should().NotBeNull();
        setlists!.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchSetlists_WithDifferentPageCombinations_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>
        {
            new Setlist { Id = 1, Name = "Jazz Setlist", UserId = "test-user", CreatedAt = DateTime.UtcNow }
        };

        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", "jazz", null, null, 3, 5))
            .ReturnsAsync((testSetlists, 1));

        // Act - Test with different page and limit combination for search
        var result = await _controller.SearchSetlists("jazz", 3, 5);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var setlists = okResult.Value as IEnumerable<SetlistResponse>;
        setlists.Should().NotBeNull();
        setlists!.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("safe content")]
    public async Task SearchSetlists_WithSafeContent_AllowsRequest(string query)
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>();

        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", query, null, null, 1, 20))
            .ReturnsAsync((testSetlists, 0));

        // Act
        var result = await _controller.SearchSetlists(query);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("safe name")]
    public async Task CreateSetlist_WithSafeName_AllowsCreation(string name)
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSetlistRequest
        {
            Name = name,
            Description = "Safe description"
        };

        var createdSetlist = new Setlist
        {
            Id = 1,
            Name = name,
            Description = "Safe description",
            UserId = "test-user",
            CreatedAt = DateTime.UtcNow
        };

        _mockSetlistService
            .Setup(s => s.CreateSetlistAsync(It.IsAny<Setlist>()))
            .ReturnsAsync(createdSetlist);

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("safe description")]
    public async Task CreateSetlist_WithSafeDescription_AllowsCreation(string description)
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSetlistRequest
        {
            Name = "Safe Name",
            Description = description
        };

        var createdSetlist = new Setlist
        {
            Id = 1,
            Name = "Safe Name",
            Description = description,
            UserId = "test-user",
            CreatedAt = DateTime.UtcNow
        };

        _mockSetlistService
            .Setup(s => s.CreateSetlistAsync(It.IsAny<Setlist>()))
            .ReturnsAsync(createdSetlist);

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task GetSetlist_WithZeroId_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        _mockSetlistService
            .Setup(s => s.GetSetlistByIdAsync(0, "test-user"))
            .ReturnsAsync((Setlist?)null);

        // Act
        var result = await _controller.GetSetlist(0);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSetlist_WithNegativeId_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        _mockSetlistService
            .Setup(s => s.GetSetlistByIdAsync(-1, "test-user"))
            .ReturnsAsync((Setlist?)null);

        // Act
        var result = await _controller.GetSetlist(-1);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSetlists_WithMaximumPageValues_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>();

        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", null, null, null, int.MaxValue, int.MaxValue))
            .ReturnsAsync((testSetlists, 0));

        // Act
        var result = await _controller.GetSetlists(int.MaxValue, int.MaxValue);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SearchSetlists_WithMaximumPageValues_HandlesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSetlists = new List<Setlist>();

        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", "test", null, null, int.MaxValue, int.MaxValue))
            .ReturnsAsync((testSetlists, 0));

        // Act
        var result = await _controller.SearchSetlists("test", int.MaxValue, int.MaxValue);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #endregion
}