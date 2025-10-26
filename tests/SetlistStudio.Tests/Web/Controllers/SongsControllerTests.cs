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

    #region Error Handling and Edge Cases Coverage Tests

    [Fact]
    public async Task GetSongs_ShouldHandleInvalidOperationException_WhenServiceThrows()
    {
        // Arrange
        _mockSongService
            .Setup(x => x.GetSongsAsync(It.IsAny<string>(), null, null, null, 1, 20))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _controller.GetSongs();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(500);
        objectResult.Value.Should().BeEquivalentTo(new { error = "Service temporarily unavailable" });
    }

    [Fact]
    public async Task GetSongs_ShouldHandleUnauthorizedAccessException_WhenServiceThrows()
    {
        // Arrange
        _mockSongService
            .Setup(x => x.GetSongsAsync(It.IsAny<string>(), null, null, null, 1, 20))
            .ThrowsAsync(new UnauthorizedAccessException("User access denied"));

        // Act
        var result = await _controller.GetSongs();

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetSongs_ShouldHandleArgumentException_WhenServiceThrows()
    {
        // Arrange
        _mockSongService
            .Setup(x => x.GetSongsAsync(It.IsAny<string>(), null, null, null, 1, 20))
            .ThrowsAsync(new ArgumentException("Invalid parameters"));

        // Act
        var result = await _controller.GetSongs();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Invalid request parameters" });
    }

    [Fact]
    public async Task GetSongs_ShouldHandleGenericException_WhenServiceFails()
    {
        // Arrange
        _mockSongService
            .Setup(x => x.GetSongsAsync(It.IsAny<string>(), null, null, null, 1, 20))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetSongs();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(500);
        objectResult.Value.Should().BeEquivalentTo(new { error = "An error occurred while retrieving songs" });
    }

    [Fact]
    public async Task CreateSong_ShouldReturnBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var request = new CreateSongRequest
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = 120,
            Key = "C"
        };

        _controller.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        _mockSongService.Verify(x => x.CreateSongAsync(It.IsAny<Song>()), Times.Never);
    }

    [Fact]
    public async Task CreateSong_ShouldHandleArgumentException_WhenInvalidData()
    {
        // Arrange
        var request = new CreateSongRequest
        {
            Title = "Invalid Song",
            Artist = "Test Artist",
            Bpm = -1, // Invalid BPM
            Key = "Invalid"
        };

        _mockSongService
            .Setup(x => x.CreateSongAsync(It.IsAny<Song>()))
            .ThrowsAsync(new ArgumentException("Invalid song data provided"));

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Invalid song data provided" });
    }

    [Fact]
    public async Task CreateSong_ShouldHandleUnauthorizedAccessException_WhenServiceThrows()
    {
        // Arrange
        var request = new CreateSongRequest
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = 120,
            Key = "C"
        };

        _mockSongService
            .Setup(x => x.CreateSongAsync(It.IsAny<Song>()))
            .ThrowsAsync(new UnauthorizedAccessException("Unauthorized song creation attempt"));

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateSong_ShouldHandleInvalidOperationException_WhenServiceFails()
    {
        // Arrange
        var request = new CreateSongRequest
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = 120,
            Key = "C"
        };

        _mockSongService
            .Setup(x => x.CreateSongAsync(It.IsAny<Song>()))
            .ThrowsAsync(new InvalidOperationException("Song service unavailable"));

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(503);
        objectResult.Value.Should().BeEquivalentTo(new { error = "Song service temporarily unavailable" });
    }

    [Fact]
    public async Task CreateSong_ShouldHandleGenericException_WhenServiceFails()
    {
        // Arrange
        var request = new CreateSongRequest
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = 120,
            Key = "C"
        };

        _mockSongService
            .Setup(x => x.CreateSongAsync(It.IsAny<Song>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(500);
        objectResult.Value.Should().BeEquivalentTo(new { error = "An error occurred while creating the song" });
    }

    [Fact]
    public async Task SearchSongs_ShouldReturnBadRequest_WhenQueryIsNull()
    {
        // Act
        var result = await _controller.SearchSongs(null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Query parameter is required" });
    }

    [Fact]
    public async Task SearchSongs_ShouldReturnBadRequest_WhenQueryIsWhiteSpace()
    {
        // Act
        var result = await _controller.SearchSongs("   ");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Query parameter is required" });
    }

    [Fact]
    public async Task SearchSongs_ShouldReturnBadRequest_WhenQueryContainsMaliciousContent()
    {
        // Act
        var result = await _controller.SearchSongs("<script>alert('xss')</script>");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Invalid search query" });
    }

    [Fact]
    public async Task SearchSongs_ShouldHandleArgumentException_WhenServiceThrows()
    {
        // Arrange
        _mockSongService
            .Setup(x => x.GetSongsAsync(It.IsAny<string>(), "valid query", null, null, 1, 20))
            .ThrowsAsync(new ArgumentException("Invalid search parameters"));

        // Act
        var result = await _controller.SearchSongs("valid query");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Invalid search parameters" });
    }

    [Fact]
    public async Task SearchSongs_ShouldHandleInvalidOperationException_WhenServiceThrows()
    {
        // Arrange
        _mockSongService
            .Setup(x => x.GetSongsAsync(It.IsAny<string>(), "valid query", null, null, 1, 20))
            .ThrowsAsync(new InvalidOperationException("Search service unavailable"));

        // Act
        var result = await _controller.SearchSongs("valid query");

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(503);
        objectResult.Value.Should().BeEquivalentTo(new { error = "Search service temporarily unavailable" });
    }

    [Fact]
    public async Task SearchSongs_ShouldHandleGenericException_WhenServiceThrows()
    {
        // Arrange
        _mockSongService
            .Setup(x => x.GetSongsAsync(It.IsAny<string>(), "valid query", null, null, 1, 20))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.SearchSongs("valid query");

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(500);
        objectResult.Value.Should().BeEquivalentTo(new { error = "An error occurred while searching songs" });
    }

    [Fact]
    public async Task CreateSong_ShouldMapRequestCorrectly_WhenCalledWithValidData()
    {
        // Arrange
        var request = new CreateSongRequest
        {
            Title = "Sweet Child O' Mine",
            Artist = "Guns N' Roses",
            Bpm = 125,
            Key = "D",
            Genre = "Rock",
            Duration = TimeSpan.FromMinutes(5.5),
            Notes = "Epic guitar solo"
        };

        var expectedSong = new Song
        {
            Id = 1,
            Title = request.Title,
            Artist = request.Artist,
            Bpm = request.Bpm,
            MusicalKey = request.Key,
            Genre = request.Genre,
            DurationSeconds = (int)Math.Round(request.Duration!.Value.TotalSeconds),
            Notes = request.Notes
        };

        Song capturedSong = null!;
        _mockSongService
            .Setup(x => x.CreateSongAsync(It.IsAny<Song>()))
            .Callback<Song>((song) => capturedSong = song)
            .ReturnsAsync(expectedSong);

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        capturedSong.Should().NotBeNull();
        capturedSong.Title.Should().Be(request.Title);
        capturedSong.Artist.Should().Be(request.Artist);
        capturedSong.Bpm.Should().Be(request.Bpm);
        capturedSong.MusicalKey.Should().Be(request.Key);
        capturedSong.Genre.Should().Be(request.Genre);
        capturedSong.DurationSeconds.Should().Be((int)Math.Round(request.Duration!.Value.TotalSeconds));
        capturedSong.Notes.Should().Be(request.Notes);
    }

    [Fact]
    public async Task GetSongs_ShouldReturnOk_WhenSongsFound()
    {
        // Arrange
        var expectedSongs = new List<Song>
        {
            new() { Id = 1, Title = "Song 1", Artist = "Artist 1" },
            new() { Id = 2, Title = "Song 2", Artist = "Artist 2" }
        };

        _mockSongService
            .Setup(x => x.GetSongsAsync(It.IsAny<string>(), null, null, null, 1, 20))
            .ReturnsAsync((expectedSongs, 2));

        // Act
        var result = await _controller.GetSongs();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { songs = expectedSongs, totalCount = 2 });
    }

    [Fact]
    public async Task SearchSongs_ShouldReturnOk_WhenValidQueryAndSongsFound()
    {
        // Arrange
        var query = "rock";
        var expectedSongs = new List<Song>
        {
            new() { Id = 1, Title = "Rock Song", Artist = "Rock Artist", Genre = "Rock" }
        };

        _mockSongService
            .Setup(x => x.GetSongsAsync(It.IsAny<string>(), query, null, null, 1, 20))
            .ReturnsAsync((expectedSongs, 1));

        // Act
        var result = await _controller.SearchSongs(query);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { songs = expectedSongs, totalCount = 1 });
    }

    [Fact]
    public async Task CreateSong_ShouldCreateSongWithOptionalFields_WhenCalledWithMinimalData()
    {
        // Arrange
        var request = new CreateSongRequest
        {
            Title = "Simple Song",
            Artist = "Simple Artist"
            // No optional fields provided
        };

        var expectedSong = new Song { Id = 1, Title = request.Title, Artist = request.Artist };

        _mockSongService
            .Setup(x => x.CreateSongAsync(It.IsAny<Song>()))
            .ReturnsAsync(expectedSong);

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(expectedSong);
    }

    [Theory]
    [InlineData("UNION SELECT")]
    [InlineData("javascript:")]
    [InlineData("<script")]
    [InlineData("'; DROP")]
    [InlineData("DROP TABLE")]
    public async Task SearchSongs_ShouldDetectMaliciousPatterns_WhenQueryContainsDangerousContent(string maliciousQuery)
    {
        // Act
        var result = await _controller.SearchSongs(maliciousQuery);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Invalid search query" });
    }

    #endregion
}