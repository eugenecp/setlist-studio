using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace SetlistStudio.Tests.Services;

/// <summary>
/// Comprehensive tests for SongService covering all methods, edge cases, and error scenarios
/// Covers: Search filters, pagination, validation, error handling, and authorization
/// </summary>
public class SongServiceTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SongService>> _mockLogger;
    private readonly SongService _songService;
    private readonly string _testUserId = "test-user-123";
    private readonly string _otherUserId = "other-user-456";

    public SongServiceTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SongService>>();
        _songService = new SongService(_context, _mockLogger.Object);
    }

    #region GetSongsAsync Comprehensive Tests

    [Fact]
    public async Task GetSongsAsync_ShouldReturnEmptyResult_WhenUserHasNoSongs()
    {
        // Arrange - Add songs for other users only
        var otherUserSongs = new List<Song>
        {
            new Song { Title = "Other Song", Artist = "Other Artist", UserId = _otherUserId }
        };
        _context.Songs.AddRange(otherUserSongs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId);

        // Assert
        result.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldFilterBySearchTerm_WhenSearchTermProvided()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Bohemian Rhapsody", Artist = "Queen", Album = "A Night at the Opera", UserId = _testUserId },
            new Song { Title = "Another One Bites the Dust", Artist = "Queen", Album = "The Game", UserId = _testUserId },
            new Song { Title = "Billie Jean", Artist = "Michael Jackson", Album = "Thriller", UserId = _testUserId },
            new Song { Title = "Rock with You", Artist = "Michael Jackson", Album = "Off the Wall", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - Search by artist
        var (resultByArtist, countByArtist) = await _songService.GetSongsAsync(_testUserId, searchTerm: "queen");

        // Assert
        resultByArtist.Should().HaveCount(2);
        countByArtist.Should().Be(2);
        resultByArtist.Should().OnlyContain(s => s.Artist.ToLower().Contains("queen"));

        // Act - Search by title
        var (resultByTitle, countByTitle) = await _songService.GetSongsAsync(_testUserId, searchTerm: "billie");

        // Assert
        resultByTitle.Should().HaveCount(1);
        countByTitle.Should().Be(1);
        resultByTitle.First().Title.Should().Be("Billie Jean");

        // Act - Search by album
        var (resultByAlbum, countByAlbum) = await _songService.GetSongsAsync(_testUserId, searchTerm: "thriller");

        // Assert
        resultByAlbum.Should().HaveCount(1);
        countByAlbum.Should().Be(1);
        resultByAlbum.First().Album.Should().Be("Thriller");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldFilterByGenre_WhenGenreProvided()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Rock Song 1", Artist = "Rock Artist 1", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Rock Song 2", Artist = "Rock Artist 2", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Jazz Song", Artist = "Jazz Artist", Genre = "Jazz", UserId = _testUserId },
            new Song { Title = "Pop Song", Artist = "Pop Artist", Genre = "Pop", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: "Rock");

        // Assert
        result.Should().HaveCount(2);
        totalCount.Should().Be(2);
        result.Should().OnlyContain(s => s.Genre == "Rock");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldFilterByTags_WhenTagsProvided()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Guitar Song 1", Artist = "Artist 1", Tags = "rock, guitar, classic", UserId = _testUserId },
            new Song { Title = "Guitar Song 2", Artist = "Artist 2", Tags = "blues, guitar, solo", UserId = _testUserId },
            new Song { Title = "Piano Song", Artist = "Artist 3", Tags = "jazz, piano, instrumental", UserId = _testUserId },
            new Song { Title = "Vocal Song", Artist = "Artist 4", Tags = "pop, vocals, modern", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, tags: "guitar");

        // Assert
        result.Should().HaveCount(2);
        totalCount.Should().Be(2);
        result.Should().OnlyContain(s => s.Tags != null && s.Tags.Contains("guitar"));
    }

    [Fact]
    public async Task GetSongsAsync_ShouldCombineAllFilters_WhenMultipleFiltersProvided()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Rock Guitar Song", Artist = "Rock Artist", Genre = "Rock", Tags = "rock, guitar", UserId = _testUserId },
            new Song { Title = "Jazz Guitar Song", Artist = "Jazz Artist", Genre = "Jazz", Tags = "jazz, guitar", UserId = _testUserId },
            new Song { Title = "Rock Piano Song", Artist = "Rock Artist", Genre = "Rock", Tags = "rock, piano", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - Search for rock songs with guitar
        var (result, totalCount) = await _songService.GetSongsAsync(
            _testUserId, 
            searchTerm: "rock", 
            genre: "Rock", 
            tags: "guitar");

        // Assert
        result.Should().HaveCount(1);
        totalCount.Should().Be(1);
        result.First().Title.Should().Be("Rock Guitar Song");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandlePagination_WhenPageParametersProvided()
    {
        // Arrange - Create 15 songs
        var songs = new List<Song>();
        for (int i = 1; i <= 15; i++)
        {
            songs.Add(new Song 
            { 
                Title = $"Song {i:D2}", 
                Artist = $"Artist {(char)('A' + (i - 1) % 26)}", // Artist A, B, C, etc.
                UserId = _testUserId 
            });
        }
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - Get first page (10 items)
        var (page1, totalCount1) = await _songService.GetSongsAsync(_testUserId, pageNumber: 1, pageSize: 10);

        // Assert
        page1.Should().HaveCount(10);
        totalCount1.Should().Be(15);

        // Act - Get second page (5 remaining items)
        var (page2, totalCount2) = await _songService.GetSongsAsync(_testUserId, pageNumber: 2, pageSize: 10);

        // Assert
        page2.Should().HaveCount(5);
        totalCount2.Should().Be(15);

        // Act - Get page beyond available data
        var (page3, totalCount3) = await _songService.GetSongsAsync(_testUserId, pageNumber: 3, pageSize: 10);

        // Assert
        page3.Should().BeEmpty();
        totalCount3.Should().Be(15);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldOrderByArtistThenTitle_Always()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Z Song", Artist = "A Artist", UserId = _testUserId },
            new Song { Title = "A Song", Artist = "Z Artist", UserId = _testUserId },
            new Song { Title = "B Song", Artist = "A Artist", UserId = _testUserId },
            new Song { Title = "A Song", Artist = "A Artist", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, _) = await _songService.GetSongsAsync(_testUserId);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(4);
        
        // Should be ordered by Artist first, then by Title
        resultList[0].Should().Match<Song>(s => s.Artist == "A Artist" && s.Title == "A Song");
        resultList[1].Should().Match<Song>(s => s.Artist == "A Artist" && s.Title == "B Song");
        resultList[2].Should().Match<Song>(s => s.Artist == "A Artist" && s.Title == "Z Song");
        resultList[3].Should().Match<Song>(s => s.Artist == "Z Artist" && s.Title == "A Song");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldIgnoreEmptyFilters_WhenEmptyStringsProvided()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Test Song", Artist = "Test Artist", Genre = "Rock", Tags = "guitar", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - Pass empty strings for filters
        var (result, totalCount) = await _songService.GetSongsAsync(
            _testUserId,
            searchTerm: "",
            genre: "",
            tags: "");

        // Assert - Should return all songs (no filtering applied)
        result.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    #endregion

    #region GetSongByIdAsync Comprehensive Tests

    [Fact]
    public async Task GetSongByIdAsync_ShouldReturnSong_WhenSongExistsAndBelongsToUser()
    {
        // Arrange
        var song = new Song 
        { 
            Title = "Test Song", 
            Artist = "Test Artist", 
            Genre = "Rock",
            Bpm = 120,
            MusicalKey = "C",
            UserId = _testUserId 
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.GetSongByIdAsync(song.Id, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(song.Id);
        result.Title.Should().Be("Test Song");
        result.UserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task GetSongByIdAsync_ShouldReturnNull_WhenSongDoesNotExist()
    {
        // Act
        var result = await _songService.GetSongByIdAsync(999, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSongByIdAsync_ShouldReturnNull_WhenSongBelongsToOtherUser()
    {
        // Arrange
        var song = new Song 
        { 
            Title = "Other User Song", 
            Artist = "Other Artist", 
            UserId = _otherUserId 
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.GetSongByIdAsync(song.Id, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateSongAsync Comprehensive Tests

    [Fact]
    public async Task CreateSongAsync_ShouldSetTimestamps_WhenCreatingSong()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            UserId = _testUserId
        };

        // Act
        var result = await _songService.CreateSongAsync(song);

        // Assert
        var afterCreate = DateTime.UtcNow;
        result.CreatedAt.Should().BeAfter(beforeCreate.AddSeconds(-1));
        result.CreatedAt.Should().BeBefore(afterCreate.AddSeconds(1));
        result.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateSongAsync_ShouldCreateSongWithAllProperties_WhenValidSong()
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
            DurationSeconds = 354,
            Notes = "Complex song with multiple sections",
            Tags = "rock, classic, opera",
            DifficultyRating = 5,
            UserId = _testUserId
        };

        // Act
        var result = await _songService.CreateSongAsync(song);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Bohemian Rhapsody");
        result.Artist.Should().Be("Queen");
        result.Album.Should().Be("A Night at the Opera");
        result.Genre.Should().Be("Rock");
        result.Bpm.Should().Be(72);
        result.MusicalKey.Should().Be("Bb");
        result.DurationSeconds.Should().Be(354);
        result.Notes.Should().Be("Complex song with multiple sections");
        result.Tags.Should().Be("rock, classic, opera");
        result.DifficultyRating.Should().Be(5);
        result.UserId.Should().Be(_testUserId);
    }

    #endregion

    #region UpdateSongAsync Comprehensive Tests

    [Fact]
    public async Task UpdateSongAsync_ShouldUpdateAllProperties_WhenValidUpdate()
    {
        // Arrange
        var originalSong = new Song
        {
            Title = "Original Title",
            Artist = "Original Artist",
            Album = "Original Album",
            Genre = "Original Genre",
            Bpm = 100,
            MusicalKey = "C",
            DurationSeconds = 180,
            Notes = "Original notes",
            Tags = "original, tags",
            DifficultyRating = 3,
            UserId = _testUserId
        };
        _context.Songs.Add(originalSong);
        await _context.SaveChangesAsync();

        var updatedSong = new Song
        {
            Id = originalSong.Id,
            Title = "Updated Title",
            Artist = "Updated Artist",
            Album = "Updated Album",
            Genre = "Updated Genre",
            Bpm = 120,
            MusicalKey = "G",
            DurationSeconds = 240,
            Notes = "Updated notes",
            Tags = "updated, tags",
            DifficultyRating = 4,
            UserId = _testUserId
        };

        // Act
        var result = await _songService.UpdateSongAsync(updatedSong, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Artist.Should().Be("Updated Artist");
        result.Album.Should().Be("Updated Album");
        result.Genre.Should().Be("Updated Genre");
        result.Bpm.Should().Be(120);
        result.MusicalKey.Should().Be("G");
        result.DurationSeconds.Should().Be(240);
        result.Notes.Should().Be("Updated notes");
        result.Tags.Should().Be("updated, tags");
        result.DifficultyRating.Should().Be(4);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldThrowException_WhenValidationFails()
    {
        // Arrange
        var originalSong = new Song
        {
            Title = "Valid Song",
            Artist = "Valid Artist",
            UserId = _testUserId
        };
        _context.Songs.Add(originalSong);
        await _context.SaveChangesAsync();

        var invalidUpdate = new Song
        {
            Id = originalSong.Id,
            Title = "", // Invalid: empty title
            Artist = "Valid Artist",
            UserId = _testUserId
        };

        // Act & Assert
        await _songService.Invoking(s => s.UpdateSongAsync(invalidUpdate, _testUserId))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Validation failed*");
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldReturnNull_WhenSongNotFound()
    {
        // Arrange
        var nonExistentSong = new Song
        {
            Id = 999,
            Title = "Non-existent Song",
            Artist = "Non-existent Artist",
            UserId = _testUserId
        };

        // Act
        var result = await _songService.UpdateSongAsync(nonExistentSong, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteSongAsync Comprehensive Tests

    [Fact]
    public async Task DeleteSongAsync_ShouldReturnFalse_WhenSongNotFound()
    {
        // Act
        var result = await _songService.DeleteSongAsync(999, _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetGenresAsync Comprehensive Tests

    [Fact]
    public async Task GetGenresAsync_ShouldReturnEmptyList_WhenUserHasNoSongsWithGenres()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Genre = null, UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Genre = "", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.GetGenresAsync(_testUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGenresAsync_ShouldReturnOrderedGenres_WhenUserHasGenres()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Genre = "Jazz", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Genre = "Blues", UserId = _testUserId },
            new Song { Title = "Song 4", Artist = "Artist 4", Genre = "Rock", UserId = _testUserId } // Duplicate
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.GetGenresAsync(_testUserId);

        // Assert
        var genreList = result.ToList();
        genreList.Should().HaveCount(3);
        genreList.Should().Equal("Blues", "Jazz", "Rock"); // Ordered alphabetically
    }

    #endregion

    #region GetTagsAsync Comprehensive Tests

    [Fact]
    public async Task GetTagsAsync_ShouldReturnEmptyList_WhenUserHasNoSongsWithTags()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Tags = null, UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Tags = "", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.GetTagsAsync(_testUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTagsAsync_ShouldHandleComplexTagStrings_WhenProcessingTags()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Tags = "rock, classic,guitar", UserId = _testUserId }, // Mixed spacing
            new Song { Title = "Song 2", Artist = "Artist 2", Tags = " blues , modern , bass ", UserId = _testUserId }, // Extra spaces
            new Song { Title = "Song 3", Artist = "Artist 3", Tags = "jazz,piano,instrumental,", UserId = _testUserId }, // Trailing comma
            new Song { Title = "Song 4", Artist = "Artist 4", Tags = "rock,classic", UserId = _testUserId } // Duplicates
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.GetTagsAsync(_testUserId);

        // Assert
        var tagList = result.ToList();
        tagList.Should().HaveCount(9);
        tagList.Should().Equal("bass", "blues", "classic", "guitar", "instrumental", "jazz", "modern", "piano", "rock");
        tagList.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region ValidateSong Comprehensive Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateSong_ShouldReturnTitleError_WhenTitleIsNullOrWhitespace(string? invalidTitle)
    {
        // Arrange
        var song = new Song
        {
            Title = invalidTitle!,
            Artist = "Valid Artist",
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("title") && e.Contains("required"));
    }

    [Fact]
    public void ValidateSong_ShouldReturnTitleLengthError_WhenTitleTooLong()
    {
        // Arrange
        var song = new Song
        {
            Title = new string('A', 201), // 201 characters
            Artist = "Valid Artist",
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("title") && e.Contains("200 characters"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateSong_ShouldReturnArtistError_WhenArtistIsNullOrWhitespace(string? invalidArtist)
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = invalidArtist!,
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("Artist") && e.Contains("required"));
    }

    [Fact]
    public void ValidateSong_ShouldReturnArtistLengthError_WhenArtistTooLong()
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = new string('B', 201), // 201 characters
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("Artist") && e.Contains("200 characters"));
    }

    [Fact]
    public void ValidateSong_ShouldReturnAlbumLengthError_WhenAlbumTooLong()
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            Album = new string('C', 201), // 201 characters
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("Album") && e.Contains("200 characters"));
    }

    [Fact]
    public void ValidateSong_ShouldReturnGenreLengthError_WhenGenreTooLong()
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            Genre = new string('D', 51), // 51 characters
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("Genre") && e.Contains("50 characters"));
    }

    [Theory]
    [InlineData(39)] // Below minimum
    [InlineData(251)] // Above maximum
    [InlineData(0)] // Edge case
    [InlineData(-1)] // Negative
    public void ValidateSong_ShouldReturnBpmError_WhenBpmOutOfRange(int invalidBpm)
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            Bpm = invalidBpm,
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("BPM") && e.Contains("between 40 and 250"));
    }

    [Theory]
    [InlineData(40)] // Minimum valid
    [InlineData(120)] // Common valid
    [InlineData(250)] // Maximum valid
    public void ValidateSong_ShouldNotReturnBpmError_WhenBpmValid(int validBpm)
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            Bpm = validBpm,
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().NotContain(e => e.Contains("BPM"));
    }

    [Fact]
    public void ValidateSong_ShouldReturnMusicalKeyLengthError_WhenMusicalKeyTooLong()
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            MusicalKey = new string('E', 11), // 11 characters
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("Musical key") && e.Contains("10 characters"));
    }

    [Theory]
    [InlineData(0)] // Below minimum
    [InlineData(3601)] // Above maximum (1 hour + 1 second)
    [InlineData(-1)] // Negative
    public void ValidateSong_ShouldReturnDurationError_WhenDurationOutOfRange(int invalidDuration)
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            DurationSeconds = invalidDuration,
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("Duration") && e.Contains("between 1 second and 1 hour"));
    }

    [Theory]
    [InlineData(1)] // Minimum valid
    [InlineData(180)] // Common valid (3 minutes)
    [InlineData(3600)] // Maximum valid (1 hour)
    public void ValidateSong_ShouldNotReturnDurationError_WhenDurationValid(int validDuration)
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            DurationSeconds = validDuration,
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().NotContain(e => e.Contains("Duration"));
    }

    [Fact]
    public void ValidateSong_ShouldReturnNotesLengthError_WhenNotesTooLong()
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            Notes = new string('F', 2001), // 2001 characters
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("Notes") && e.Contains("2000 characters"));
    }

    [Fact]
    public void ValidateSong_ShouldReturnTagsLengthError_WhenTagsTooLong()
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            Tags = new string('G', 501), // 501 characters
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("Tags") && e.Contains("500 characters"));
    }

    [Theory]
    [InlineData(0)] // Below minimum
    [InlineData(6)] // Above maximum
    [InlineData(-1)] // Negative
    public void ValidateSong_ShouldReturnDifficultyError_WhenDifficultyOutOfRange(int invalidDifficulty)
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            DifficultyRating = invalidDifficulty,
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("Difficulty rating") && e.Contains("between 1 and 5"));
    }

    [Theory]
    [InlineData(1)] // Minimum valid
    [InlineData(3)] // Common valid
    [InlineData(5)] // Maximum valid
    public void ValidateSong_ShouldNotReturnDifficultyError_WhenDifficultyValid(int validDifficulty)
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            DifficultyRating = validDifficulty,
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().NotContain(e => e.Contains("Difficulty"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateSong_ShouldReturnUserIdError_WhenUserIdIsNullOrWhitespace(string? invalidUserId)
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            UserId = invalidUserId!
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("User ID") && e.Contains("required"));
    }

    [Fact]
    public void ValidateSong_ShouldReturnMultipleErrors_WhenMultipleFieldsInvalid()
    {
        // Arrange
        var song = new Song
        {
            Title = "", // Invalid
            Artist = "", // Invalid
            Bpm = 300, // Invalid
            DifficultyRating = 10, // Invalid
            UserId = "" // Invalid
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().HaveCountGreaterOrEqualTo(5);
        errors.Should().Contain(e => e.Contains("title"));
        errors.Should().Contain(e => e.Contains("Artist"));
        errors.Should().Contain(e => e.Contains("BPM"));
        errors.Should().Contain(e => e.Contains("Difficulty"));
        errors.Should().Contain(e => e.Contains("User ID"));
    }

    [Fact]
    public void ValidateSong_ShouldAcceptNullOptionalFields_WhenRequiredFieldsValid()
    {
        // Arrange
        var song = new Song
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            UserId = _testUserId,
            // All optional fields are null
            Album = null,
            Genre = null,
            Bpm = null,
            MusicalKey = null,
            DurationSeconds = null,
            Notes = null,
            Tags = null,
            DifficultyRating = null
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}