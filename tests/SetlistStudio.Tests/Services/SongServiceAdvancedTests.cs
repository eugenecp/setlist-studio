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
        var mockCacheService = new Mock<IQueryCacheService>();
        _songService = new SongService(_context, _mockLogger.Object, _mockAuditLogService.Object, mockCacheService.Object);
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

    #region Pagination Boundary Edge Cases

    [Fact]
    public async Task GetSongsAsync_ShouldConvertPageNumberZeroToOne_WhenPageNumberIsZero()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Artist", UserId = _testUserId };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, pageNumber: 0, pageSize: 10);

        // Assert - Should treat page 0 as page 1
        result.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldConvertNegativePageNumberToOne_WhenPageNumberIsNegative()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Artist", UserId = _testUserId };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, pageNumber: -5, pageSize: 10);

        // Assert - Should treat negative as 1
        result.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldClampPageSizeToOne_WhenPageSizeIsZero()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, pageNumber: 1, pageSize: 0);

        // Assert - Should clamp to minimum 1
        result.Should().HaveCount(1);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldClampPageSizeToOne_WhenPageSizeIsNegative()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, pageNumber: 1, pageSize: -50);

        // Assert - Should clamp to minimum 1
        result.Should().HaveCount(1);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldReturnExactlyMaxPageSize_WhenPageSizeIsExactlyOneHundred()
    {
        // Arrange
        var songs = new List<Song>();
        for (int i = 0; i < 100; i++)
        {
            songs.Add(new Song { Title = $"Song {i:D3}", Artist = $"Artist {i}", UserId = _testUserId });
        }
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, pageNumber: 1, pageSize: 100);

        // Assert - Should return exactly 100 items
        result.Should().HaveCount(100);
        totalCount.Should().Be(100);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldReturnSingleSongWhenMultipleExist_WhenPageSizeIsOne()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist A", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist B", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist C", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, pageNumber: 1, pageSize: 1);

        // Assert
        result.Should().HaveCount(1);
        totalCount.Should().Be(3);
    }

    #endregion

    #region Filter Input Edge Cases - Whitespace and Null Handling

    [Fact]
    public async Task GetSongsAsync_ShouldIgnoreGenreFilter_WhenGenreIsWhitespaceOnly()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Rock Song", Artist = "Artist", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Jazz Song", Artist = "Artist", Genre = "Jazz", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: "   ");

        // Assert - Whitespace should be treated as null filter
        result.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldIgnoreSearchTermFilter_WhenSearchTermIsWhitespaceOnly()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Test Song", Artist = "Artist", UserId = _testUserId },
            new Song { Title = "Another Song", Artist = "Another", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: "\t\n  ");

        // Assert - Whitespace should be treated as null
        result.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldSkipSongWithNullGenre_WhenGenreFilterApplied()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Rock Song", Artist = "Artist 1", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "No Genre Song", Artist = "Artist 2", Genre = null, UserId = _testUserId },
            new Song { Title = "Another Rock", Artist = "Artist 3", Genre = "Rock", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: "Rock");

        // Assert - Should exclude null genre
        result.Should().HaveCount(2);
        totalCount.Should().Be(2);
        result.Should().OnlyContain(s => s.Genre == "Rock");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldSkipSongWithNullTags_WhenTagsFilterApplied()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Tagged Song 1", Artist = "Artist 1", Tags = "guitar, rock", UserId = _testUserId },
            new Song { Title = "No Tags Song", Artist = "Artist 2", Tags = null, UserId = _testUserId },
            new Song { Title = "Tagged Song 2", Artist = "Artist 3", Tags = "guitar, acoustic", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, tags: "guitar");

        // Assert - Should not crash, should skip null tags
        result.Should().HaveCount(2);
        totalCount.Should().Be(2);
        result.Should().OnlyContain(s => s.Tags != null && s.Tags.Contains("guitar"));
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleSongWithEmptyStringGenre_WhenGenreFilterApplied()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Rock Song", Artist = "Artist 1", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Empty Genre Song", Artist = "Artist 2", Genre = "", UserId = _testUserId },
            new Song { Title = "Another Rock", Artist = "Artist 3", Genre = "Rock", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: "Rock");

        // Assert - Empty string genre is distinct from "Rock"
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.Genre == "Rock");
    }

    #endregion

    #region Genre Filtering Edge Cases

    [Fact]
    public async Task GetSongsAsync_ShouldMatchGenreWithLeadingAndTrailingSpaces_WhenGenreHasWhitespace()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Rock Song", Artist = "Artist", Genre = "Rock", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: "  Rock  ");

        // Assert - Should trim and match
        result.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldMatchGenreWithMixedCases_WhenProvidingDifferentCases()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Heavy Metal Song", Artist = "Artist", Genre = "Heavy Metal", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - Test various case combinations
        var (result1, _) = await _songService.GetSongsAsync(_testUserId, genre: "heavy metal");
        var (result2, _) = await _songService.GetSongsAsync(_testUserId, genre: "HEAVY METAL");
        var (result3, _) = await _songService.GetSongsAsync(_testUserId, genre: "HeAvY mEtAl");

        // Assert - All should match despite case differences
        result1.Should().HaveCount(1);
        result2.Should().HaveCount(1);
        result3.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldNotPartialMatchGenre_WhenGenreHasSpacesInName()
    {
        // Arrange - "Heavy Metal" should not match "Heavy" alone
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Genre = "Heavy Metal", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Genre = "Rock", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: "Heavy");

        // Assert - Should not match (looking for exact "Heavy", not "Heavy Metal")
        result.Should().HaveCount(0);
        totalCount.Should().Be(0);
    }

    #endregion

    #region Tags Filtering Edge Cases

    [Fact]
    public async Task GetSongsAsync_ShouldMatchTagsWithCommaSpaceSeparation_WhenTagsAreCommaSeparated()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Tags = "rock, guitar, classic", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Tags = "jazz, piano, smooth", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, tags: "guitar");

        // Assert
        result.Should().HaveCount(1);
        totalCount.Should().Be(1);
        result.First().Title.Should().Be("Song 1");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldNotPartialMatchTags_WhenSearchingSubstring()
    {
        // Arrange - "guitar" should NOT match "gutarra"
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Tags = "gutarra, spanish, classic", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Tags = "guitar, acoustic, rock", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, tags: "guitar");

        // Assert - Should only match exact "guitar", not "gutarra"
        result.Should().HaveCount(1);
        result.First().Tags.Should().Contain("guitar");
    }

    #endregion

    #region Unicode and Special Character Tests

    [Fact]
    public async Task GetSongsAsync_ShouldFilterWithUnicodeCharacters_WhenSearchTermContainsUnicode()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "いのちの火", Artist = "日本のアーティスト", Genre = "J-Pop", UserId = _testUserId },
            new Song { Title = "Ode to Joy", Artist = "Beethoven", Genre = "Classical", UserId = _testUserId },
            new Song { Title = "Café Music", Artist = "French Artist", Genre = "Jazz", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (jpResult, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "日本");
        var (frResult, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "café");

        // Assert
        jpResult.Should().HaveCount(1);
        jpResult.First().Artist.Should().Contain("日本");
        frResult.Should().HaveCount(1);
        frResult.First().Title.Should().Contain("Café");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldFilterWithSpecialCharactersInArtist_WhenArtistNameHasSpecialChars()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "AC/DC", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Wu-Tang Clan", Genre = "Hip-Hop", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "a.m. radio", Genre = "Pop", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (acResult, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "AC/DC");
        var (wuResult, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "Wu-Tang");
        var (radioResult, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "a.m.");

        // Assert
        acResult.Should().HaveCount(1);
        wuResult.Should().HaveCount(1);
        radioResult.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldFilterWithDiacriticalMarks_WhenTitleContainsDiacriticals()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Café Au Lait", Artist = "Artist 1", Genre = "Jazz", UserId = _testUserId },
            new Song { Title = "Zürich Nights", Artist = "Artist 2", Genre = "Electronic", UserId = _testUserId },
            new Song { Title = "São Paulo Dreams", Artist = "Artist 3", Genre = "Bossa Nova", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (cafeResult, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "café");
        var (zurichResult, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "zürich");
        var (saoResult, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "são");

        // Assert
        cafeResult.Should().HaveCount(1);
        zurichResult.Should().HaveCount(1);
        saoResult.Should().HaveCount(1);
    }

    #endregion

    #region Data Consistency and Ordering Tests

    [Fact]
    public async Task GetSongsAsync_ShouldNotReturnDuplicates_WhenPaginatingLargeResultSet()
    {
        // Arrange - Create 250 songs with same artist to stress ordering
        var songs = new List<Song>();
        for (int i = 0; i < 250; i++)
        {
            songs.Add(new Song
            {
                Title = $"Song {i:D3}",
                Artist = "The Same Artist",
                Genre = "Rock",
                UserId = _testUserId
            });
        }
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - Get all pages
        var allResults = new List<Song>();
        for (int page = 1; page <= 3; page++)
        {
            var (pageResults, _) = await _songService.GetSongsAsync(_testUserId, pageNumber: page, pageSize: 100);
            allResults.AddRange(pageResults);
        }

        // Assert - No duplicates despite pagination
        allResults.Should().HaveCount(250);
        allResults.Select(s => s.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetSongsAsync_ShouldUseIdTiebreaker_WhenSongsHaveIdenticalArtistAndTitle()
    {
        // Arrange
        var song1 = new Song { Title = "Same Title", Artist = "Same Artist", UserId = _testUserId };
        var song2 = new Song { Title = "Same Title", Artist = "Same Artist", UserId = _testUserId };
        var song3 = new Song { Title = "Same Title", Artist = "Same Artist", UserId = _testUserId };

        _context.Songs.AddRange(song1, song2, song3);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId);

        // Assert - All returned in ID order without duplicates
        result.Should().HaveCount(3);
        totalCount.Should().Be(3);
        var resultList = result.ToList();
        resultList[0].Id.Should().BeLessThan(resultList[1].Id);
        resultList[1].Id.Should().BeLessThan(resultList[2].Id);
    }

    #endregion

    #region Authorization and User Isolation Tests

    [Fact]
    public async Task GetSongsAsync_ShouldNotReturnOtherUsersSongsWithGenreFilter_WhenFilteringByGenre()
    {
        // Arrange
        var otherUserId = "other-user-999";
        var testUserSongs = new List<Song>
        {
            new Song { Title = "My Rock Song 1", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "My Rock Song 2", Genre = "Rock", UserId = _testUserId }
        };
        var otherUserSongs = new List<Song>
        {
            new Song { Title = "Other Rock Song", Genre = "Rock", UserId = otherUserId },
            new Song { Title = "Other Jazz Song", Genre = "Jazz", UserId = otherUserId }
        };

        _context.Songs.AddRange(testUserSongs);
        _context.Songs.AddRange(otherUserSongs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: "Rock");

        // Assert - Should only return test user's songs
        result.Should().HaveCount(2);
        totalCount.Should().Be(2);
        result.Should().OnlyContain(s => s.UserId == _testUserId);
    }

    #endregion

    #region Search Term Edge Cases

    [Fact]
    public async Task GetSongsAsync_ShouldPerformPartialWordSearch_WhenSearchTermIsPartialWord()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Bohemian Rhapsody", Artist = "Queen", UserId = _testUserId },
            new Song { Title = "Bohemian Grove", Artist = "Artist", UserId = _testUserId },
            new Song { Title = "Rhapsody in Blue", Artist = "Gershwin", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "bohemi");

        // Assert - Should find partial matches
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.Title.ToLower().Contains("bohemi"));
    }

    [Fact]
    public async Task GetSongsAsync_ShouldFindSongByTitlePartial_WhenSearchTermMatchesPartOfTitle()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Welcome to the Machine", Artist = "Artist", UserId = _testUserId },
            new Song { Title = "Machine Learning 101", Artist = "Tech Artist", UserId = _testUserId },
            new Song { Title = "Welcome Back", Artist = "Another", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "machine");

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Combination Filter Tests

    [Fact]
    public async Task GetSongsAsync_ShouldReturnEmpty_WhenAllFiltersAppliedButNoMatch()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Rock Guitar Song", Artist = "Rock Artist", Genre = "Rock", Tags = "rock, guitar", UserId = _testUserId },
            new Song { Title = "Jazz Piano Song", Artist = "Jazz Artist", Genre = "Jazz", Tags = "jazz, piano", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(
            _testUserId,
            searchTerm: "Jazz",
            genre: "Rock", // Conflicting with search
            tags: "guitar");

        // Assert
        result.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldApplyAllFiltersWithAnd_WhenMultipleFiltersProvided()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Rock Guitar Song", Artist = "Rock Artist", Genre = "Rock", Tags = "rock, guitar, classic", UserId = _testUserId },
            new Song { Title = "Electric Guitar Song", Artist = "Rock Artist", Genre = "Rock", Tags = "rock, electric, modern", UserId = _testUserId },
            new Song { Title = "Jazz Guitar Song", Artist = "Jazz Artist", Genre = "Jazz", Tags = "jazz, guitar, smooth", UserId = _testUserId },
            new Song { Title = "Rock Piano Song", Artist = "Rock Artist", Genre = "Rock", Tags = "rock, piano, classic", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - All filters must match
        var (result, totalCount) = await _songService.GetSongsAsync(
            _testUserId,
            searchTerm: "Guitar",
            genre: "Rock",
            tags: "classic");

        // Assert
        result.Should().HaveCount(1);
        totalCount.Should().Be(1);
        result.First().Title.Should().Be("Rock Guitar Song");
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}