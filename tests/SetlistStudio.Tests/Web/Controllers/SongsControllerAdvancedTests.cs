using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Web.Controllers;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Xunit;
using CreateSongRequest = SetlistStudio.Web.Controllers.CreateSongRequest;

namespace SetlistStudio.Tests.Web.Controllers;

/// <summary>
/// Advanced tests for SongsController targeting edge cases, error conditions, 
/// and boundary scenarios to improve branch coverage. These tests focus on exceptional 
/// paths, validation boundaries, and security edge cases not covered in base tests.
/// </summary>
public class SongsControllerAdvancedTests
{
    private readonly Mock<ISongService> _mockSongService;
    private readonly Mock<ILogger<SongsController>> _mockLogger;
    private readonly SongsController _controller;

    public SongsControllerAdvancedTests()
    {
        _mockSongService = new Mock<ISongService>();
        _mockLogger = new Mock<ILogger<SongsController>>();
        _controller = new SongsController(_mockSongService.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSongService_ShouldNotThrowException()
    {
        // The SongsController constructor doesn't have null validation
        // Act & Assert - should not throw
        var controller = new SongsController(null!, null!);
        controller.Should().NotBeNull();
    }

    #endregion

    #region User Identity Edge Cases

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
    public async Task GetSongs_WithEmptyNameIdentifier_ShouldUseEmptyString()
    {
        // Arrange - When NameIdentifier is empty string (not null), it uses the empty string
        SetupUserWithEmptyNameIdentifierButValidName("identity-user");
        var testSongs = new List<Song>
        {
            new Song { Id = 1, Title = "Test Song", Artist = "Test Artist", UserId = "" }
        };
        
        _mockSongService
            .Setup(s => s.GetSongsAsync("", null, null, null, 1, 20))
            .ReturnsAsync((testSongs, testSongs.Count));

        // Act
        var result = await _controller.GetSongs();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockSongService.Verify(s => s.GetSongsAsync("", null, null, null, 1, 20), Times.Once);
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

    #region SearchSongs Validation Edge Cases

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

    #endregion

    #region CreateSong Duration Edge Cases

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

    #region CreateSong Field Mapping Tests

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

    #region ContainsMaliciousContent Edge Cases

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

    #region Helper Methods

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
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
}