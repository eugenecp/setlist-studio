using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Services;

/// <summary>
/// Comprehensive tests for SongService validation and edge cases
/// Testing all validation branches and error scenarios to improve branch coverage
/// </summary>
public class SongServiceValidationTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SongService>> _mockLogger;
    private readonly SongService _songService;
    private const string TestUserId = "test-user-123";

    public SongServiceValidationTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SongService>>();
        _songService = new SongService(_context, _mockLogger.Object);
    }

    #region Validation Tests - All Branches

    [Fact]
    public void ValidateSong_ShouldReturnError_WhenSongIsNull()
    {
        // Act
        var errors = _songService.ValidateSong(null!);

        // Assert
        errors.Should().ContainSingle().Which.Should().Be("Song cannot be null");
    }

    [Theory]
    [InlineData("", "Song title is required")]
    [InlineData("   ", "Song title is required")]
    [InlineData(null, "Song title is required")]
    public void ValidateSong_ShouldReturnError_WhenTitleIsInvalid(string? title, string expectedError)
    {
        // Arrange
        var song = CreateValidSong();
        song.Title = title!;

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(expectedError);
    }

    [Fact]
    public void ValidateSong_ShouldReturnError_WhenTitleExceedsMaxLength()
    {
        // Arrange
        var song = CreateValidSong();
        song.Title = new string('A', 201); // Exceeds 200 character limit

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain("Song title cannot exceed 200 characters");
    }

    [Theory]
    [InlineData("", "Artist name is required")]
    [InlineData("   ", "Artist name is required")]
    [InlineData(null, "Artist name is required")]
    public void ValidateSong_ShouldReturnError_WhenArtistIsInvalid(string? artist, string expectedError)
    {
        // Arrange
        var song = CreateValidSong();
        song.Artist = artist!;

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(expectedError);
    }

    [Fact]
    public void ValidateSong_ShouldReturnError_WhenArtistExceedsMaxLength()
    {
        // Arrange
        var song = CreateValidSong();
        song.Artist = new string('B', 201); // Exceeds 200 character limit

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain("Artist name cannot exceed 200 characters");
    }

    [Theory]
    [InlineData("", true)] // Empty string should not cause error
    [InlineData(null, true)] // Null should not cause error
    public void ValidateSong_ShouldNotReturnError_WhenOptionalStringFieldsAreEmpty(string? value, bool shouldBeValid)
    {
        // Arrange
        var song = CreateValidSong();
        song.Album = value;
        song.Genre = value;
        song.MusicalKey = value;
        song.Notes = value;
        song.Tags = value;

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        if (shouldBeValid)
        {
            errors.Should().NotContain(e => e.Contains("Album") || e.Contains("Genre") || 
                                          e.Contains("Musical key") || e.Contains("Notes") || e.Contains("Tags"));
        }
    }

    [Theory]
    [InlineData("Album", 201, "Album name cannot exceed 200 characters")]
    [InlineData("Genre", 51, "Genre cannot exceed 50 characters")]
    [InlineData("MusicalKey", 11, "Musical key cannot exceed 10 characters")]
    [InlineData("Notes", 2001, "Notes cannot exceed 2000 characters")]
    [InlineData("Tags", 501, "Tags cannot exceed 500 characters")]
    public void ValidateSong_ShouldReturnError_WhenOptionalStringFieldsExceedMaxLength(string fieldName, int length, string expectedError)
    {
        // Arrange
        var song = CreateValidSong();
        var longValue = new string('X', length);
        
        switch (fieldName)
        {
            case "Album": song.Album = longValue; break;
            case "Genre": song.Genre = longValue; break;
            case "MusicalKey": song.MusicalKey = longValue; break;
            case "Notes": song.Notes = longValue; break;
            case "Tags": song.Tags = longValue; break;
        }

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(expectedError);
    }

    [Theory]
    [InlineData(39, "BPM must be between 40 and 250")] // Below minimum
    [InlineData(251, "BPM must be between 40 and 250")] // Above maximum
    public void ValidateSong_ShouldReturnError_WhenBpmIsOutOfRange(int bpm, string expectedError)
    {
        // Arrange
        var song = CreateValidSong();
        song.Bpm = bpm;

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(expectedError);
    }

    [Theory]
    [InlineData(null)] // Null should be valid
    [InlineData(40)]   // Minimum valid
    [InlineData(120)]  // Middle valid
    [InlineData(250)]  // Maximum valid
    public void ValidateSong_ShouldNotReturnError_WhenBpmIsValid(int? bpm)
    {
        // Arrange
        var song = CreateValidSong();
        song.Bpm = bpm;

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().NotContain(e => e.Contains("BPM"));
    }

    [Theory]
    [InlineData(0, "Duration must be between 1 second and 1 hour")] // Below minimum
    [InlineData(3601, "Duration must be between 1 second and 1 hour")] // Above maximum
    public void ValidateSong_ShouldReturnError_WhenDurationIsOutOfRange(int duration, string expectedError)
    {
        // Arrange
        var song = CreateValidSong();
        song.DurationSeconds = duration;

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(expectedError);
    }

    [Theory]
    [InlineData(null)] // Null should be valid
    [InlineData(1)]    // Minimum valid
    [InlineData(180)]  // Middle valid (3 minutes)
    [InlineData(3600)] // Maximum valid (1 hour)
    public void ValidateSong_ShouldNotReturnError_WhenDurationIsValid(int? duration)
    {
        // Arrange
        var song = CreateValidSong();
        song.DurationSeconds = duration;

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().NotContain(e => e.Contains("Duration"));
    }

    [Theory]
    [InlineData(0, "Difficulty rating must be between 1 and 5")] // Below minimum
    [InlineData(6, "Difficulty rating must be between 1 and 5")] // Above maximum
    public void ValidateSong_ShouldReturnError_WhenDifficultyRatingIsOutOfRange(int rating, string expectedError)
    {
        // Arrange
        var song = CreateValidSong();
        song.DifficultyRating = rating;

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(expectedError);
    }

    [Theory]
    [InlineData(null)] // Null should be valid
    [InlineData(1)]    // Minimum valid
    [InlineData(3)]    // Middle valid
    [InlineData(5)]    // Maximum valid
    public void ValidateSong_ShouldNotReturnError_WhenDifficultyRatingIsValid(int? rating)
    {
        // Arrange
        var song = CreateValidSong();
        song.DifficultyRating = rating;

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().NotContain(e => e.Contains("Difficulty"));
    }

    [Theory]
    [InlineData("", "User ID is required")]
    [InlineData("   ", "User ID is required")]
    [InlineData(null, "User ID is required")]
    public void ValidateSong_ShouldReturnError_WhenUserIdIsInvalid(string? userId, string expectedError)
    {
        // Arrange
        var song = CreateValidSong();
        song.UserId = userId!;

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(expectedError);
    }

    [Fact]
    public void ValidateSong_ShouldReturnMultipleErrors_WhenMultipleValidationsFail()
    {
        // Arrange
        var song = new Song
        {
            Title = "", // Invalid - empty
            Artist = new string('A', 201), // Invalid - too long
            Album = new string('B', 201), // Invalid - too long
            Genre = new string('C', 51), // Invalid - too long
            MusicalKey = new string('D', 11), // Invalid - too long
            Notes = new string('E', 2001), // Invalid - too long
            Tags = new string('F', 501), // Invalid - too long
            Bpm = 300, // Invalid - too high
            DurationSeconds = 0, // Invalid - too low
            DifficultyRating = 10, // Invalid - too high
            UserId = "" // Invalid - empty
        };

        // Act
        var errors = _songService.ValidateSong(song).ToList();

        // Assert
        errors.Should().HaveCountGreaterThan(5); // Should have multiple validation errors
        errors.Should().Contain("Song title is required");
        errors.Should().Contain("Artist name cannot exceed 200 characters");
        errors.Should().Contain("Album name cannot exceed 200 characters");
        errors.Should().Contain("BPM must be between 40 and 250");
        errors.Should().Contain("User ID is required");
    }

    [Fact]
    public void ValidateSong_ShouldReturnNoErrors_WhenSongIsCompletelyValid()
    {
        // Arrange
        var song = CreateValidSong();

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region Service Method Validation Integration Tests

    [Fact]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenValidationFails()
    {
        // Arrange
        var invalidSong = CreateValidSong();
        invalidSong.Title = ""; // Invalid

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _songService.CreateSongAsync(invalidSong));
        
        exception.Message.Should().Contain("Validation failed");
        exception.Message.Should().Contain("Song title is required");
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldThrowArgumentException_WhenValidationFails()
    {
        // Arrange
        var validSong = CreateValidSong();
        _context.Songs.Add(validSong);
        await _context.SaveChangesAsync();

        var updateSong = CreateValidSong();
        updateSong.Id = validSong.Id;
        updateSong.Artist = ""; // Invalid

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _songService.UpdateSongAsync(updateSong, TestUserId));
        
        exception.Message.Should().Contain("Validation failed");
        exception.Message.Should().Contain("Artist name is required");
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public async Task GetSongsAsync_ShouldHandleNullSearchTerms_WithoutError()
    {
        // Arrange
        var song = CreateValidSong();
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(
            TestUserId, 
            searchTerm: null, 
            genre: null, 
            tags: null);

        // Assert
        songs.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleEmptySearchTerms_WithoutError()
    {
        // Arrange
        var song = CreateValidSong();
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(
            TestUserId, 
            searchTerm: "", 
            genre: "", 
            tags: "");

        // Assert
        songs.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleWhitespaceSearchTerms_WithoutError()
    {
        // Arrange
        var song = CreateValidSong();
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(
            TestUserId, 
            searchTerm: "   ", 
            genre: "   ", 
            tags: "   ");

        // Assert
        songs.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTagsAsync_ShouldHandleEmptyTags_WithoutError()
    {
        // Arrange
        var song1 = CreateValidSong();
        song1.Tags = null;
        
        var song2 = CreateValidSong();
        song2.Id = 0; // Let EF assign ID
        song2.Tags = "";
        
        var song3 = CreateValidSong();
        song3.Id = 0; // Let EF assign ID
        song3.Tags = "rock,pop,blues";

        _context.Songs.AddRange(song1, song2, song3);
        await _context.SaveChangesAsync();

        // Act
        var tags = await _songService.GetTagsAsync(TestUserId);

        // Assert
        tags.Should().BeEquivalentTo(new[] { "blues", "pop", "rock" });
    }

    [Fact]
    public async Task GetTagsAsync_ShouldTrimAndSplitTags_Correctly()
    {
        // Arrange
        var song = CreateValidSong();
        song.Tags = " rock , pop , blues , jazz ";

        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var tags = await _songService.GetTagsAsync(TestUserId);

        // Assert
        tags.Should().BeEquivalentTo(new[] { "blues", "jazz", "pop", "rock" });
    }

    #endregion

    #region Helper Methods

    private Song CreateValidSong()
    {
        return new Song
        {
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            Album = "A Night at the Opera",
            Genre = "Rock",
            Bpm = 72,
            MusicalKey = "Bb",
            DurationSeconds = 355,
            Notes = "Classic rock masterpiece",
            Tags = "classic,rock,opera",
            DifficultyRating = 4,
            UserId = TestUserId
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #endregion
}