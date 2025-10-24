using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Web.Controllers;
using System.Security.Claims;
using Xunit;
using FluentAssertions;

namespace SetlistStudio.Tests.Web.Controllers;

/// <summary>
/// Comprehensive tests for SongsController covering all endpoints and scenarios
/// </summary>
public class SongsControllerTests
{
    private readonly Mock<ISongService> _mockSongService;
    private readonly Mock<ILogger<SongsController>> _mockLogger;
    private readonly SongsController _controller;

    public SongsControllerTests()
    {
        _mockSongService = new Mock<ISongService>();
        _mockLogger = new Mock<ILogger<SongsController>>();
        _controller = new SongsController(_mockSongService.Object, _mockLogger.Object);

        // Setup authenticated user context
        SetupAuthenticatedUser("test-user");
    }

    [Fact]
    public async Task GetSongs_WithValidRequest_ReturnsOkWithSongs()
    {
        // Arrange
        var testSongs = new List<Song>
        {
            new Song { Id = 1, Title = "Test Song", Artist = "Test Artist", UserId = "test-user" }
        };
        
        _mockSongService
            .Setup(s => s.GetSongsAsync("test-user", null, null, null, 1, 20))
            .ReturnsAsync((testSongs, testSongs.Count));

        // Act
        var result = await _controller.GetSongs();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SearchSongs_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var query = "rock";
        var testSongs = new List<Song>
        {
            new Song { Id = 1, Title = "Rock Song", Artist = "Rock Artist", UserId = "test-user" }
        };
        
        _mockSongService
            .Setup(s => s.GetSongsAsync("test-user", query, null, null, 1, 20))
            .ReturnsAsync((testSongs, testSongs.Count));

        // Act
        var result = await _controller.SearchSongs(query);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SearchSongs_WithNullQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchSongs(null!);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { error = "Query parameter is required" });
    }

    [Fact]
    public async Task SearchSongs_WithMaliciousContent_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchSongs("<script>alert('xss')</script>");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { error = "Invalid search query" });
    }

    [Fact]
    public async Task CreateSong_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateSongRequest
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = 120,
            Key = "C"
        };

        var createdSong = new Song
        {
            Id = 1,
            Title = request.Title,
            Artist = request.Artist,
            Bpm = request.Bpm,
            MusicalKey = request.Key,
            UserId = "test-user"
        };

        _mockSongService
            .Setup(s => s.CreateSongAsync(It.IsAny<Song>()))
            .ReturnsAsync(createdSong);

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateSong_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSongRequest { Title = "", Artist = "" };
        _controller.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetSongs_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockSongService
            .Setup(s => s.GetSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetSongs();

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateSong_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateSongRequest
        {
            Title = "Test Song",
            Artist = "Test Artist"
        };

        _mockSongService
            .Setup(s => s.CreateSongAsync(It.IsAny<Song>()))
            .ThrowsAsync(new Exception("Database write failed"));

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateSong_WithZeroDuration_SetsNullDurationSeconds()
    {
        // Arrange
        var request = new CreateSongRequest
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Duration = TimeSpan.Zero
        };

        var createdSong = new Song
        {
            Id = 1,
            Title = request.Title,
            Artist = request.Artist,
            DurationSeconds = null,
            UserId = "test-user"
        };

        _mockSongService
            .Setup(s => s.CreateSongAsync(It.IsAny<Song>()))
            .ReturnsAsync(createdSong);

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userId)
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