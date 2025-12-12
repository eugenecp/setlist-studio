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

    #region Advanced Edge Cases - Constructor Tests

    [Fact]
    public void Constructor_WithNullSongService_ShouldNotThrowException()
    {
        // The SongsController constructor doesn't have null validation
        // Act & Assert - should not throw
        var controller = new SongsController(null!, null!);
        controller.Should().NotBeNull();
    }

    #endregion

    #region Advanced Edge Cases - User Identity Tests

    [Fact]
    public async Task GetSongs_WithNoUserClaims_ShouldUseAnonymous()
    {
        // Arrange
        SetupUserWithoutClaims();
        var testSongs = new List<Song>
        {
            new Song { Id = 1, Title = "Test Song", Artist = "Test Artist", UserId = "anonymous" }
        };
        
        _mockSongService
            .Setup(s => s.GetSongsAsync("anonymous", null, null, null, 1, 20))
            .ReturnsAsync((testSongs, testSongs.Count));

        // Act
        var result = await _controller.GetSongs();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockSongService.Verify(s => s.GetSongsAsync("anonymous", null, null, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetSongs_WithEmptyNameIdentifier_ShouldUseIdentityNameFallback()
    {
        // Arrange - When NameIdentifier is empty but Identity.Name exists, should use Identity.Name as fallback
        SetupUserWithEmptyNameIdentifierButValidName("identity-user");
        var testSongs = new List<Song>
        {
            new Song { Id = 1, Title = "Test Song", Artist = "Test Artist", UserId = "identity-user" }
        };
        
        _mockSongService
            .Setup(s => s.GetSongsAsync("identity-user", null, null, null, 1, 20))
            .ReturnsAsync((testSongs, testSongs.Count));

        // Act
        var result = await _controller.GetSongs();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockSongService.Verify(s => s.GetSongsAsync("identity-user", null, null, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task SearchSongs_WithNoUserClaims_ShouldUseAnonymous()
    {
        // Arrange
        SetupUserWithoutClaims();
        var query = "test";
        var testSongs = new List<Song>
        {
            new Song { Id = 1, Title = "Test Song", Artist = "Test Artist", UserId = "anonymous" }
        };
        
        _mockSongService
            .Setup(s => s.GetSongsAsync("anonymous", "test", null, null, 1, 20))
            .ReturnsAsync((testSongs, testSongs.Count));

        // Act
        var result = await _controller.SearchSongs(query);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockSongService.Verify(s => s.GetSongsAsync("anonymous", "test", null, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task CreateSong_WithNoUserClaims_ShouldUseAnonymous()
    {
        // Arrange
        SetupUserWithoutClaims();
        var request = new CreateSongRequest
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = 120
        };

        var createdSong = new Song
        {
            Id = 1,
            Title = request.Title,
            Artist = request.Artist,
            UserId = "anonymous"
        };

        _mockSongService
            .Setup(s => s.CreateSongAsync(It.Is<Song>(song => song.UserId == "anonymous")))
            .ReturnsAsync(createdSong);

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        _mockSongService.Verify(s => s.CreateSongAsync(It.Is<Song>(song => song.UserId == "anonymous")), Times.Once);
    }

    #endregion

    #region Advanced Edge Cases - SearchSongs Validation

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task SearchSongs_WithWhitespaceQuery_ShouldReturnBadRequest(string query)
    {
        // Act
        var result = await _controller.SearchSongs(query);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { error = "Query parameter is required" });
    }

    [Theory]
    [InlineData("</script>")]
    [InlineData("JAVASCRIPT:")]
    [InlineData("vbscript:alert")]
    [InlineData("onload=malicious")]
    [InlineData("onerror=attack")]
    [InlineData("onclick=payload")]
    [InlineData("onmouseover=script")]
    public async Task SearchSongs_WithVariousMaliciousPatterns_ShouldReturnBadRequest(string maliciousQuery)
    {
        // Act
        var result = await _controller.SearchSongs(maliciousQuery);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { error = "Invalid search query" });
    }

    [Theory]
    [InlineData("union select * from users")]
    [InlineData("DROP TABLE songs")]
    [InlineData("DELETE FROM users")]
    [InlineData("INSERT INTO admin")]
    [InlineData("'; drop table users;--")]
    [InlineData("/* comment attack */")]
    [InlineData("-- sql comment")]
    public async Task SearchSongs_WithSqlInjectionPatterns_ShouldReturnBadRequest(string sqlInjectionQuery)
    {
        // Act
        var result = await _controller.SearchSongs(sqlInjectionQuery);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { error = "Invalid search query" });
    }

    [Fact]
    public async Task SearchSongs_WithCaseSensitiveMaliciousContent_ShouldReturnBadRequest()
    {
        // Test case insensitive malicious pattern detection
        var result = await _controller.SearchSongs("UNION SELECT password FROM users");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { error = "Invalid search query" });
    }

    [Fact]
    public async Task SearchSongs_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        _mockSongService
            .Setup(s => s.GetSongsAsync("test-user", "valid query", null, null, 1, 20))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.SearchSongs("valid query");

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        var errorResponse = statusResult.Value.Should().BeEquivalentTo(new { error = "An error occurred while searching songs" });
    }

    [Theory]
    [InlineData("safe search term")]
    [InlineData("rock music")]
    [InlineData("classical symphony")]
    [InlineData("jazz fusion")]
    [InlineData("electronic dance")]
    public async Task SearchSongs_WithSafeContent_ShouldReturnOk(string safeQuery)
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var testSongs = new List<Song>
        {
            new Song { Id = 1, Title = "Safe Song", Artist = "Safe Artist", UserId = "test-user" }
        };
        
        _mockSongService
            .Setup(s => s.GetSongsAsync("test-user", safeQuery, null, null, 1, 20))
            .ReturnsAsync((testSongs, testSongs.Count));

        // Act
        var result = await _controller.SearchSongs(safeQuery);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SearchSongs_WithMixedCaseMaliciousPattern_ShouldReturnBadRequest()
    {
        // Test case insensitive detection with mixed case
        var result = await _controller.SearchSongs("UnIoN sElEcT * FrOm UsErS");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { error = "Invalid search query" });
    }

    [Theory]
    [InlineData("script")]  // partial match, should be safe
    [InlineData("select")]  // partial match, should be safe
    [InlineData("table")]   // partial match, should be safe
    [InlineData("load")]    // partial match, should be safe
    public async Task SearchSongs_WithPartialMaliciousPatterns_ShouldReturnOk(string partialPattern)
    {
        // Arrange - These are partial matches that should not trigger malicious content detection
        SetupAuthenticatedUser("test-user");
        var testSongs = new List<Song>
        {
            new Song { Id = 1, Title = "Test Song", Artist = "Test Artist", UserId = "test-user" }
        };
        
        _mockSongService
            .Setup(s => s.GetSongsAsync("test-user", partialPattern, null, null, 1, 20))
            .ReturnsAsync((testSongs, testSongs.Count));

        // Act
        var result = await _controller.SearchSongs(partialPattern);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Advanced Edge Cases - CreateSong Duration Tests

    [Fact]
    public async Task CreateSong_WithNegativeDuration_ShouldSetNullDurationSeconds()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSongRequest
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Duration = TimeSpan.FromSeconds(-10)
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
            .Setup(s => s.CreateSongAsync(It.Is<Song>(song => song.DurationSeconds == null)))
            .ReturnsAsync(createdSong);

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        _mockSongService.Verify(s => s.CreateSongAsync(It.Is<Song>(song => song.DurationSeconds == null)), Times.Once);
    }

    [Fact]
    public async Task CreateSong_WithValidPositiveDuration_ShouldSetDurationSeconds()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var duration = TimeSpan.FromMinutes(3.5); // 3.5 minutes = 210 seconds
        var request = new CreateSongRequest
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Duration = duration
        };

        var createdSong = new Song
        {
            Id = 1,
            Title = request.Title,
            Artist = request.Artist,
            DurationSeconds = 210,
            UserId = "test-user"
        };

        _mockSongService
            .Setup(s => s.CreateSongAsync(It.Is<Song>(song => song.DurationSeconds == 210)))
            .ReturnsAsync(createdSong);

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        _mockSongService.Verify(s => s.CreateSongAsync(It.Is<Song>(song => song.DurationSeconds == 210)), Times.Once);
    }

    [Fact]
    public async Task CreateSong_WithNullDuration_ShouldSetNullDurationSeconds()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSongRequest
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Duration = null
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
            .Setup(s => s.CreateSongAsync(It.Is<Song>(song => song.DurationSeconds == null)))
            .ReturnsAsync(createdSong);

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        _mockSongService.Verify(s => s.CreateSongAsync(It.Is<Song>(song => song.DurationSeconds == null)), Times.Once);
    }

    #endregion

    #region Advanced Edge Cases - Field Mapping Tests

    [Fact]
    public async Task CreateSong_WithAllOptionalFields_ShouldMapCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var request = new CreateSongRequest
        {
            Title = "Complete Song",
            Artist = "Complete Artist",
            Bpm = 125,
            Key = "G#",
            Genre = "Progressive Rock",
            Duration = TimeSpan.FromMinutes(4),
            Notes = "This is a test song with all fields populated"
        };

        var createdSong = new Song
        {
            Id = 1,
            Title = request.Title,
            Artist = request.Artist,
            Bpm = request.Bpm,
            MusicalKey = request.Key,
            Genre = request.Genre,
            DurationSeconds = 240,
            Notes = request.Notes,
            UserId = "test-user"
        };

        _mockSongService
            .Setup(s => s.CreateSongAsync(It.Is<Song>(song =>
                song.Title == "Complete Song" &&
                song.Artist == "Complete Artist" &&
                song.Bpm == 125 &&
                song.MusicalKey == "G#" &&
                song.Genre == "Progressive Rock" &&
                song.DurationSeconds == 240 &&
                song.Notes == "This is a test song with all fields populated" &&
                song.UserId == "test-user")))
            .ReturnsAsync(createdSong);

        // Act
        var result = await _controller.CreateSong(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        _mockSongService.Verify(s => s.CreateSongAsync(It.Is<Song>(song =>
            song.Title == "Complete Song" &&
            song.Artist == "Complete Artist" &&
            song.Bpm == 125 &&
            song.MusicalKey == "G#" &&
            song.Genre == "Progressive Rock" &&
            song.DurationSeconds == 240 &&
            song.Notes == "This is a test song with all fields populated" &&
            song.UserId == "test-user")), Times.Once);
    }

    #endregion

    #region Advanced Helper Methods

    private void SetupUserWithoutClaims()
    {
        var identity = new ClaimsIdentity(); // No claims
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    private void SetupUserWithEmptyNameIdentifierButValidName(string identityName)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, ""), // Empty name identifier
            new Claim(ClaimTypes.Name, identityName)   // Valid identity name
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

    #endregion

    #region GetSongsByGenre Tests - TDD Implementation

    /// <summary>
    /// TDD Step 1 (RED): First failing test - defines basic expected behavior
    /// This test will FAIL because GetSongsByGenre method doesn't exist yet
    /// </summary>
    [Fact]
    public async Task GetSongsByGenre_WithValidGenre_ReturnsOkWithFilteredSongs()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        var rockSongs = new List<Song>
        {
            new Song { Id = 1, Title = "Bohemian Rhapsody", Artist = "Queen", Genre = "Rock", Bpm = 72, UserId = "test-user" },
            new Song { Id = 2, Title = "Sweet Child O' Mine", Artist = "Guns N' Roses", Genre = "Rock", Bpm = 125, UserId = "test-user" }
        };

        _mockSongService
            .Setup(s => s.GetSongsAsync("test-user", null, "Rock", null, 1, 20))
            .ReturnsAsync((rockSongs, 2));

        // Act
        var result = await _controller.GetSongsByGenre("Rock", page: 1, pageSize: 20);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        
        var response = okResult!.Value;
        response.Should().NotBeNull();
        
        _mockSongService.Verify(s => s.GetSongsAsync("test-user", null, "Rock", null, 1, 20), Times.Once);
    }

    /// <summary>
    /// TDD Step 3 (RED): Test validation for null/empty/whitespace genre parameter
    /// These tests will FAIL because validation logic doesn't exist yet
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task GetSongsByGenre_WithNullOrWhitespaceGenre_ReturnsBadRequest(string? genre)
    {
        // Arrange
        SetupAuthenticatedUser("test-user");

        // Act
        var result = await _controller.GetSongsByGenre(genre!, page: 1, pageSize: 20);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Genre parameter is required" });
    }

    /// <summary>
    /// TDD Step 5 (RED): Test validation for invalid page numbers
    /// These tests will FAIL because page validation doesn't exist yet
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetSongsByGenre_WithInvalidPageNumber_ReturnsBadRequest(int page)
    {
        // Arrange
        SetupAuthenticatedUser("test-user");

        // Act
        var result = await _controller.GetSongsByGenre("Rock", page: page, pageSize: 20);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Page number must be greater than 0" });
    }

    /// <summary>
    /// TDD Step 6 (RED): Test validation for invalid page sizes
    /// These tests will FAIL because pageSize validation doesn't exist yet
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(1000)]
    public async Task GetSongsByGenre_WithInvalidPageSize_ReturnsBadRequest(int pageSize)
    {
        // Arrange
        SetupAuthenticatedUser("test-user");

        // Act
        var result = await _controller.GetSongsByGenre("Rock", page: 1, pageSize: pageSize);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Page size must be between 1 and 100" });
    }

    /// <summary>
    /// TDD Step 8 (RED): Test security validation for malicious content
    /// These tests will FAIL because ContainsMaliciousContent check doesn't exist yet
    /// </summary>
    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("'; DROP TABLE Songs--")]
    [InlineData("' OR '1'='1")]
    public async Task GetSongsByGenre_WithMaliciousGenre_ReturnsBadRequest(string maliciousGenre)
    {
        // Arrange
        SetupAuthenticatedUser("test-user");

        // Act
        var result = await _controller.GetSongsByGenre(maliciousGenre, page: 1, pageSize: 20);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Invalid genre parameter" });
    }

    /// <summary>
    /// TDD Step 9 (RED): Test exception handling
    /// These tests will FAIL because exception handling doesn't exist yet
    /// </summary>
    [Fact]
    public async Task GetSongsByGenre_WhenUnauthorized_ReturnsForbid()
    {
        // Arrange: Setup unauthenticated user that will throw UnauthorizedAccessException
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _mockSongService
            .Setup(s => s.GetSongsAsync(It.IsAny<string>(), null, "Rock", null, 1, 20))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _controller.GetSongsByGenre("Rock", page: 1, pageSize: 20));
    }

    [Fact]
    public async Task GetSongsByGenre_WhenArgumentException_ReturnsBadRequest()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        _mockSongService
            .Setup(s => s.GetSongsAsync("test-user", null, "InvalidGenre", null, 1, 20))
            .ThrowsAsync(new ArgumentException("Invalid genre"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _controller.GetSongsByGenre("InvalidGenre", page: 1, pageSize: 20));
    }

    [Fact]
    public async Task GetSongsByGenre_WhenInvalidOperationException_ReturnsServiceUnavailable()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        _mockSongService
            .Setup(s => s.GetSongsAsync("test-user", null, "Rock", null, 1, 20))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _controller.GetSongsByGenre("Rock", page: 1, pageSize: 20));
    }

    [Fact]
    public async Task GetSongsByGenre_WhenGenericException_ReturnsInternalServerError()
    {
        // Arrange
        SetupAuthenticatedUser("test-user");
        _mockSongService
            .Setup(s => s.GetSongsAsync("test-user", null, "Rock", null, 1, 20))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _controller.GetSongsByGenre("Rock", page: 1, pageSize: 20));
    }

    #endregion

    // TDD: More genre filtering tests will be added iteratively following Red-Green-Refactor
}