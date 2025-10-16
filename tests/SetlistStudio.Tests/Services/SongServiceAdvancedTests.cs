using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using FluentAssertions;
using Xunit;
using System.Data.Common;

namespace SetlistStudio.Tests.Services;

/// <summary>
/// Advanced tests for SongService targeting specific coverage gaps
/// Focus on error handling, edge cases, and validation scenarios
/// </summary>
public class SongServiceAdvancedTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SongService>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly SongService _songService;
    private readonly string _testUserId = "test-user-123";

    public SongServiceAdvancedTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SongService>>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _songService = new SongService(_context, _mockLogger.Object, _mockAuditLogService.Object);
    }

    #region Edge Case and Error Handling Tests

    [Fact]
    public async Task GetSongsAsync_ShouldHandleNullAlbumFields_InSearchFilter()
    {
        // Arrange - Create songs with null albums
        var songsWithNullAlbums = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Album = null, UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Album = "Album 2", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Album = "", UserId = _testUserId }
        };
        _context.Songs.AddRange(songsWithNullAlbums);
        await _context.SaveChangesAsync();

        // Act - Search for term that might match null album
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: "album");

        // Assert - Should only match the song with the actual album
        result.Should().HaveCount(1);
        result.First().Album.Should().Be("Album 2");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleEmptyStringFilters_Gracefully()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Test Song", Artist = "Test Artist", Genre = "Rock", Tags = "rock, classic", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - Use empty strings for all filters
        var (result, totalCount) = await _songService.GetSongsAsync(
            _testUserId, 
            searchTerm: "", 
            genre: "", 
            tags: "");

        // Assert - Should return all songs (filters ignored when empty)
        result.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleWhitespaceFilters_Gracefully()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Test Song", Artist = "Test Artist", Genre = "Rock", Tags = "rock, classic", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - Use whitespace for all filters
        var (result, totalCount) = await _songService.GetSongsAsync(
            _testUserId, 
            searchTerm: "   ", 
            genre: "   ", 
            tags: "   ");

        // Assert - Should return all songs (filters ignored when whitespace)
        result.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTagsAsync_ShouldHandleNullAndEmptyTagFields()
    {
        // Arrange - Mix of null, empty, and valid tags
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Tags = null, UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Tags = "", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Tags = "rock, blues", UserId = _testUserId },
            new Song { Title = "Song 4", Artist = "Artist 4", Tags = "  jazz  ,  funk  ", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var tags = await _songService.GetTagsAsync(_testUserId);

        // Assert
        tags.Should().BeEquivalentTo(new[] { "blues", "funk", "jazz", "rock" }, "Should extract and clean tags properly");
    }

    [Fact]
    public async Task GetTagsAsync_ShouldHandleCommaVariations_AndWhitespace()
    {
        // Arrange - Various comma and whitespace scenarios
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Tags = "rock,blues,jazz", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Tags = "pop, dance, electronic", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Tags = "  classical  ,  , opera", UserId = _testUserId },
            new Song { Title = "Song 4", Artist = "Artist 4", Tags = "funk,,soul", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var tags = await _songService.GetTagsAsync(_testUserId);

        // Assert
        var expectedTags = new[] { "blues", "classical", "dance", "electronic", "funk", "jazz", "opera", "pop", "rock", "soul" };
        tags.Should().BeEquivalentTo(expectedTags, "Should handle various comma and whitespace patterns");
    }

    #endregion

    #region Comprehensive Validation Tests

    [Fact]
    public void ValidateSong_ShouldReturnNullError_WhenSongIsNull()
    {
        // Act
        var errors = _songService.ValidateSong(null!);

        // Assert
        errors.Should().ContainSingle().Which.Should().Be("Song cannot be null");
    }

    [Theory]
    [InlineData(null, "Title")]
    [InlineData("", "Title")]
    [InlineData("   ", "Title")]
    [InlineData(null, "Artist")]
    [InlineData("", "Artist")]
    [InlineData("   ", "Artist")]
    public void ValidateSong_ShouldReturnError_WhenRequiredFieldsAreMissing(string? value, string fieldType)
    {
        // Arrange
        var song = new Song
        {
            Title = fieldType == "Title" ? value ?? string.Empty : "Valid Title",
            Artist = fieldType == "Artist" ? value ?? string.Empty : "Valid Artist",
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        var expectedError = fieldType == "Title" ? "Song title is required" : "Artist name is required";
        errors.Should().Contain(expectedError);
    }

    [Theory]
    [InlineData("Title", 201)]
    [InlineData("Artist", 201)]
    [InlineData("Album", 201)]
    [InlineData("Genre", 51)]
    [InlineData("MusicalKey", 11)]
    [InlineData("Notes", 2001)]
    [InlineData("Tags", 501)]
    public void ValidateSong_ShouldReturnError_WhenFieldsExceedMaxLength(string fieldType, int length)
    {
        // Arrange
        var longValue = new string('x', length);
        var song = new Song
        {
            Title = fieldType == "Title" ? longValue : "Valid Title",
            Artist = fieldType == "Artist" ? longValue : "Valid Artist",
            Album = fieldType == "Album" ? longValue : "Valid Album",
            Genre = fieldType == "Genre" ? longValue : "Rock",
            MusicalKey = fieldType == "MusicalKey" ? longValue : "C",
            Notes = fieldType == "Notes" ? longValue : "Valid notes",
            Tags = fieldType == "Tags" ? longValue : "rock, pop",
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        var expectedMaxLength = fieldType switch
        {
            "Title" or "Artist" or "Album" => 200,
            "Genre" => 50,
            "MusicalKey" => 10,
            "Notes" => 2000,
            "Tags" => 500,
            _ => 0
        };
        
        var fieldName = fieldType switch
        {
            "Title" => "Song title",
            "Artist" => "Artist name",
            "Album" => "Album name",
            "Genre" => "Genre",
            "MusicalKey" => "Musical key",
            "Notes" => "Notes",
            "Tags" => "Tags",
            _ => fieldType
        };

        errors.Should().Contain($"{fieldName} cannot exceed {expectedMaxLength} characters");
    }

    [Theory]
    [InlineData(39)] // Below minimum BPM
    [InlineData(251)] // Above maximum BPM
    public void ValidateSong_ShouldReturnError_WhenBpmOutOfRange(int bpm)
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            UserId = _testUserId,
            Bpm = bpm
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain("BPM must be between 40 and 250");
    }

    [Theory]
    [InlineData(0)] // Below minimum duration
    [InlineData(3601)] // Above maximum duration (1 hour)
    public void ValidateSong_ShouldReturnError_WhenDurationOutOfRange(int duration)
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            UserId = _testUserId,
            DurationSeconds = duration
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain("Duration must be between 1 second and 1 hour");
    }

    [Theory]
    [InlineData(0)] // Below minimum difficulty
    [InlineData(6)] // Above maximum difficulty
    public void ValidateSong_ShouldReturnError_WhenDifficultyRatingOutOfRange(int rating)
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            UserId = _testUserId,
            DifficultyRating = rating
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain("Difficulty rating must be between 1 and 5");
    }

    [Fact]
    public void ValidateSong_ShouldReturnError_WhenUserIdMissing()
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            UserId = "" // Empty user ID
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain("User ID is required");
    }

    [Fact]
    public void ValidateSong_ShouldReturnNoErrors_WhenAllValidOptionalFieldsProvided()
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            Album = "Valid Album",
            Genre = "Rock",
            MusicalKey = "C",
            Notes = "Some notes",
            Tags = "rock, classic",
            Bpm = 120,
            DurationSeconds = 240,
            DifficultyRating = 3,
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().BeEmpty("Valid song should have no validation errors");
    }

    [Fact]
    public void ValidateSong_ShouldReturnNoErrors_WhenOptionalFieldsAreNull()
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            Album = null,
            Genre = null,
            MusicalKey = null,
            Notes = null,
            Tags = null,
            Bpm = null,
            DurationSeconds = null,
            DifficultyRating = null,
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().BeEmpty("Song with null optional fields should be valid");
    }

    [Fact]
    public void ValidateSong_ShouldReturnMultipleErrors_WhenMultipleValidationsFail()
    {
        // Arrange
        var song = new Song
        {
            Title = "", // Required field missing
            Artist = new string('x', 201), // Too long
            UserId = "", // Required field missing
            Bpm = 300, // Out of range
            DurationSeconds = 0, // Out of range
            DifficultyRating = 10 // Out of range
        };

        // Act
        var errors = _songService.ValidateSong(song).ToList();

        // Assert
        errors.Should().HaveCountGreaterThan(1, "Multiple validation failures should produce multiple errors");
        errors.Should().Contain("Song title is required");
        errors.Should().Contain("Artist name cannot exceed 200 characters");
        errors.Should().Contain("User ID is required");
        errors.Should().Contain("BPM must be between 40 and 250");
        errors.Should().Contain("Duration must be between 1 second and 1 hour");
        errors.Should().Contain("Difficulty rating must be between 1 and 5");
    }

    #endregion

    #region Error Handling and Exception Tests

    [Fact]
    public async Task CreateSongAsync_ShouldThrowException_WhenValidationFails()
    {
        // Arrange
        var invalidSong = new Song
        {
            Title = "", // Invalid - required field missing
            Artist = "Valid Artist",
            UserId = _testUserId
        };

        // Act & Assert
        await FluentActions.Invoking(() => _songService.CreateSongAsync(invalidSong))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Validation failed: Song title is required");
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldThrowException_WhenValidationFails()
    {
        // Arrange
        var existingSong = new Song
        {
            Title = "Original Title",
            Artist = "Original Artist",
            UserId = _testUserId
        };
        _context.Songs.Add(existingSong);
        await _context.SaveChangesAsync();

        var invalidUpdate = new Song
        {
            Id = existingSong.Id,
            Title = "", // Invalid - required field missing
            Artist = "Updated Artist",
            UserId = _testUserId
        };

        // Act & Assert
        await FluentActions.Invoking(() => _songService.UpdateSongAsync(invalidUpdate, _testUserId))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Validation failed: Song title is required");
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldUpdateTimestamp_WhenUpdateSucceeds()
    {
        // Arrange
        var originalTime = DateTime.UtcNow.AddHours(-1);
        var existingSong = new Song
        {
            Title = "Original Title",
            Artist = "Original Artist",
            UserId = _testUserId,
            CreatedAt = originalTime,
            UpdatedAt = null
        };
        _context.Songs.Add(existingSong);
        await _context.SaveChangesAsync();

        var updatedSong = new Song
        {
            Id = existingSong.Id,
            Title = "Updated Title",
            Artist = "Updated Artist",
            UserId = _testUserId
        };

        // Act
        var result = await _songService.UpdateSongAsync(updatedSong, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.UpdatedAt.Should().NotBeNull("UpdatedAt should be set when song is updated");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.CreatedAt.Should().Be(originalTime, "CreatedAt should not change during update");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldSetTimestamps_WhenCreatingNewSong()
    {
        // Arrange
        var newSong = new Song
        {
            Title = "New Song",
            Artist = "New Artist",
            UserId = _testUserId
        };

        // Act
        var result = await _songService.CreateSongAsync(newSong);

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.UpdatedAt.Should().BeNull("UpdatedAt should be null for new songs");
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}