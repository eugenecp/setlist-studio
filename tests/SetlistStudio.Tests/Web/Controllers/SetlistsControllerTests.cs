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
/// Comprehensive tests for SetlistsController covering all endpoints and scenarios
/// </summary>
public class SetlistsControllerTests
{
    private readonly Mock<ISetlistService> _mockSetlistService;
    private readonly Mock<IPerformanceDateService> _mockPerformanceDateService;
    private readonly Mock<ILogger<SetlistsController>> _mockLogger;
    private readonly SetlistsController _controller;

    public SetlistsControllerTests()
    {
        _mockSetlistService = new Mock<ISetlistService>();
        _mockPerformanceDateService = new Mock<IPerformanceDateService>();
        _mockLogger = new Mock<ILogger<SetlistsController>>();
        _controller = new SetlistsController(
            _mockSetlistService.Object, 
            _mockPerformanceDateService.Object,
            _mockLogger.Object);

        // Setup authenticated user context
        SetupAuthenticatedUser("test-user");
    }

    [Fact]
    public async Task GetSetlists_WithValidRequest_ReturnsOkWithSetlists()
    {
        // Arrange
        var testSetlists = new List<Setlist>
        {
            new Setlist { Id = 1, Name = "Test Setlist", UserId = "test-user", CreatedAt = DateTime.UtcNow }
        };
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", null, null, null, 1, 20))
            .ReturnsAsync((testSetlists, testSetlists.Count));

        // Act
        var result = await _controller.GetSetlists();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateSetlist_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateSetlistRequest
        {
            Name = "Test Setlist",
            Description = "Test Description"
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
    public async Task SearchSetlists_WithMaliciousContent_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchSetlists("<script>alert('xss')</script>");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid search query");
    }

    [Fact]
    public async Task GetSetlist_WithValidId_ReturnsOkWithSetlist()
    {
        // Arrange
        var setlist = new Setlist
        {
            Id = 1,
            Name = "Test Setlist",
            Description = "Test Description",
            UserId = "test-user",
            CreatedAt = DateTime.UtcNow
        };

        _mockSetlistService
            .Setup(s => s.GetSetlistByIdAsync(1, "test-user"))
            .ReturnsAsync(setlist);

        // Act
        var result = await _controller.GetSetlist(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSetlist_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        _mockSetlistService
            .Setup(s => s.GetSetlistByIdAsync(999, "test-user"))
            .ReturnsAsync((Setlist?)null);

        // Act
        var result = await _controller.GetSetlist(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateSetlist_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSetlistRequest { Name = "", Description = "" };
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateSetlist_WithMaliciousContent_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSetlistRequest 
        { 
            Name = "<script>alert('xss')</script>", 
            Description = "Clean description" 
        };

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid setlist data");
    }

    [Fact]
    public async Task SearchSetlists_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var query = "rock";
        var testSetlists = new List<Setlist>
        {
            new Setlist { Id = 1, Name = "Rock Songs", UserId = "test-user", CreatedAt = DateTime.UtcNow }
        };
        
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync("test-user", query, null, null, 1, 20))
            .ReturnsAsync((testSetlists, testSetlists.Count));

        // Act
        var result = await _controller.SearchSetlists(query);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSetlists_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockSetlistService
            .Setup(s => s.GetSetlistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetSetlists();

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Internal server error");
    }

    [Fact]
    public async Task CreateSetlist_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateSetlistRequest
        {
            Name = "Test Setlist",
            Description = "Test Description"
        };

        _mockSetlistService
            .Setup(s => s.CreateSetlistAsync(It.IsAny<Setlist>()))
            .ThrowsAsync(new Exception("Database write failed"));

        // Act
        var result = await _controller.CreateSetlist(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Internal server error");
    }

    #region Performance Date Endpoint Tests

    [Fact]
    public async Task GetPerformanceDates_WithAuthorizedUser_ReturnsOkWithDates()
    {
        // Arrange
        var setlistId = 1;
        var performanceDates = new List<PerformanceDate>
        {
            new PerformanceDate
            {
                Id = 1,
                SetlistId = setlistId,
                Date = DateTime.UtcNow.AddDays(7),
                UserId = "test-user",
                Venue = "Test Venue",
                Setlist = new Setlist { Id = setlistId, Name = "Test Setlist", UserId = "test-user" }
            }
        };

        _mockPerformanceDateService
            .Setup(s => s.GetPerformanceDatesAsync(setlistId, "test-user"))
            .ReturnsAsync(performanceDates);

        // Act
        var result = await _controller.GetPerformanceDates(setlistId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<PerformanceDateResponse>>().Subject;
        response.Should().HaveCount(1);
        response.First().Venue.Should().Be("Test Venue");
    }

    [Fact]
    public async Task GetPerformanceDates_WithUnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        var setlistId = 1;
        _mockPerformanceDateService
            .Setup(s => s.GetPerformanceDatesAsync(setlistId, "test-user"))
            .ReturnsAsync((IEnumerable<PerformanceDate>?)null);

        // Act
        var result = await _controller.GetPerformanceDates(setlistId);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetPerformanceDates_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var setlistId = 1;
        _mockPerformanceDateService
            .Setup(s => s.GetPerformanceDatesAsync(setlistId, "test-user"))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetPerformanceDates(setlistId);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreatePerformanceDate_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var setlistId = 1;
        var request = new CreatePerformanceDateRequest
        {
            Date = DateTime.UtcNow.AddDays(7),
            Venue = "New Venue",
            Notes = "Performance notes"
        };

        var created = new PerformanceDate
        {
            Id = 1,
            SetlistId = setlistId,
            Date = request.Date,
            Venue = request.Venue,
            Notes = request.Notes,
            UserId = "test-user",
            Setlist = new Setlist { Id = setlistId, Name = "Test Setlist", UserId = "test-user" }
        };

        _mockPerformanceDateService
            .Setup(s => s.CreatePerformanceDateAsync(It.IsAny<PerformanceDate>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.CreatePerformanceDate(setlistId, request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<PerformanceDateResponse>().Subject;
        response.Venue.Should().Be("New Venue");
        response.Notes.Should().Be("Performance notes");
    }

    [Fact]
    public async Task CreatePerformanceDate_WithPastDate_ReturnsBadRequest()
    {
        // Arrange
        var setlistId = 1;
        var request = new CreatePerformanceDateRequest
        {
            Date = DateTime.UtcNow.AddDays(-7), // Past date
            Venue = "Test Venue"
        };

        // Act
        var result = await _controller.CreatePerformanceDate(setlistId, request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Cannot schedule performance date in the past");
    }

    [Fact]
    public async Task CreatePerformanceDate_WithUnauthorizedSetlist_ReturnsForbid()
    {
        // Arrange
        var setlistId = 1;
        var request = new CreatePerformanceDateRequest
        {
            Date = DateTime.UtcNow.AddDays(7),
            Venue = "Test Venue"
        };

        _mockPerformanceDateService
            .Setup(s => s.CreatePerformanceDateAsync(It.IsAny<PerformanceDate>()))
            .ReturnsAsync((PerformanceDate?)null);

        // Act
        var result = await _controller.CreatePerformanceDate(setlistId, request);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreatePerformanceDate_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var setlistId = 1;
        var request = new CreatePerformanceDateRequest
        {
            Date = DateTime.UtcNow.AddDays(7),
            Venue = "Test Venue"
        };

        _mockPerformanceDateService
            .Setup(s => s.CreatePerformanceDateAsync(It.IsAny<PerformanceDate>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreatePerformanceDate(setlistId, request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task DeletePerformanceDate_WithAuthorizedUser_ReturnsNoContent()
    {
        // Arrange
        var setlistId = 1;
        var dateId = 1;

        _mockPerformanceDateService
            .Setup(s => s.DeletePerformanceDateAsync(dateId, "test-user"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeletePerformanceDate(setlistId, dateId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeletePerformanceDate_WithUnauthorizedUser_ReturnsNotFound()
    {
        // Arrange
        var setlistId = 1;
        var dateId = 1;

        _mockPerformanceDateService
            .Setup(s => s.DeletePerformanceDateAsync(dateId, "test-user"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeletePerformanceDate(setlistId, dateId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Performance date not found or unauthorized");
    }

    [Fact]
    public async Task DeletePerformanceDate_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var setlistId = 1;
        var dateId = 1;

        _mockPerformanceDateService
            .Setup(s => s.DeletePerformanceDateAsync(dateId, "test-user"))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeletePerformanceDate(setlistId, dateId);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

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
}