using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace SetlistStudio.Tests.Services;

public class SongServiceTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SongService>> _mockLogger;
    private readonly SongService _songService;
    private readonly string _testUserId = "test-user-123";

    public SongServiceTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SongService>>();
        _songService = new SongService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldReturnUserSongs_WhenUserHasSongs()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Bohemian Rhapsody", Artist = "Queen", Genre = "Rock", Bpm = 72, MusicalKey = "Bb", UserId = _testUserId },
            new Song { Title = "Billie Jean", Artist = "Michael Jackson", Genre = "Pop", Bpm = 117, MusicalKey = "F#m", UserId = _testUserId },
            new Song { Title = "Other User Song", Artist = "Other Artist", Genre = "Jazz", UserId = "other-user" }
        };

        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId);

        // Assert
        result.Should().HaveCount(2);
        totalCount.Should().Be(2);
        result.Should().OnlyContain(s => s.UserId == _testUserId);
    }

    [Fact]
    public async Task CreateSongAsync_ShouldCreateSong_WhenValidSong()
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Genre = "Test Genre",
            Bpm = 120,
            MusicalKey = "C",
            UserId = _testUserId
        };

        // Act
        var result = await _songService.CreateSongAsync(song);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Test Song");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var savedSong = await _context.Songs.FindAsync(result.Id);
        savedSong.Should().NotBeNull();
        savedSong!.Title.Should().Be("Test Song");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldThrowException_WhenInvalidSong()
    {
        // Arrange
        var invalidSong = new Song
        {
            // Missing required Title and Artist
            Genre = "Test Genre",
            UserId = _testUserId
        };

        // Act & Assert
        await _songService.Invoking(s => s.CreateSongAsync(invalidSong))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Validation failed*");
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldUpdateSong_WhenValidSongAndCorrectUser()
    {
        // Arrange
        var originalSong = new Song
        {
            Title = "Original Title",
            Artist = "Original Artist",
            UserId = _testUserId
        };

        _context.Songs.Add(originalSong);
        await _context.SaveChangesAsync();

        var updatedSong = new Song
        {
            Id = originalSong.Id,
            Title = "Updated Title",
            Artist = "Updated Artist",
            UserId = _testUserId
        };

        // Act
        var result = await _songService.UpdateSongAsync(updatedSong, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Artist.Should().Be("Updated Artist");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldReturnNull_WhenWrongUser()
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            UserId = "other-user"
        };

        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        var updatedSong = new Song
        {
            Id = song.Id,
            Title = "Hacker Update",
            Artist = "Hacker Artist",
            UserId = "other-user"
        };

        // Act
        var result = await _songService.UpdateSongAsync(updatedSong, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSongAsync_ShouldDeleteSong_WhenCorrectUser()
    {
        // Arrange
        var song = new Song
        {
            Title = "To Delete",
            Artist = "Test Artist",
            UserId = _testUserId
        };

        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.DeleteSongAsync(song.Id, _testUserId);

        // Assert
        result.Should().BeTrue();

        var deletedSong = await _context.Songs.FindAsync(song.Id);
        deletedSong.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSongAsync_ShouldReturnFalse_WhenWrongUser()
    {
        // Arrange
        var song = new Song
        {
            Title = "Protected Song",
            Artist = "Test Artist",
            UserId = "other-user"
        };

        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.DeleteSongAsync(song.Id, _testUserId);

        // Assert
        result.Should().BeFalse();

        var stillExistsSong = await _context.Songs.FindAsync(song.Id);
        stillExistsSong.Should().NotBeNull();
    }

    [Fact]
    public void ValidateSong_ShouldReturnErrors_WhenRequiredFieldsMissing()
    {
        // Arrange
        var invalidSong = new Song
        {
            // Missing Title, Artist, and UserId
            Genre = "Test"
        };

        // Act
        var errors = _songService.ValidateSong(invalidSong);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("title"));
        errors.Should().Contain(e => e.Contains("Artist"));
        errors.Should().Contain(e => e.Contains("User ID"));
    }

    [Fact]
    public void ValidateSong_ShouldReturnEmpty_WhenValidSong()
    {
        // Arrange
        var validSong = new Song
        {
            Title = "Valid Song",
            Artist = "Valid Artist",
            Genre = "Rock",
            Bpm = 120,
            MusicalKey = "C",
            DurationSeconds = 180,
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(validSong);

        // Assert
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(39)] // Below minimum
    [InlineData(251)] // Above maximum
    public void ValidateSong_ShouldReturnBpmError_WhenBpmOutOfRange(int invalidBpm)
    {
        // Arrange
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = invalidBpm,
            UserId = _testUserId
        };

        // Act
        var errors = _songService.ValidateSong(song);

        // Assert
        errors.Should().Contain(e => e.Contains("BPM"));
    }

    [Fact]
    public async Task GetGenresAsync_ShouldReturnUniqueGenres_ForUser()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Rock Song 1", Artist = "Artist 1", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Rock Song 2", Artist = "Artist 2", Genre = "Rock", UserId = _testUserId },
            new Song { Title = "Jazz Song", Artist = "Artist 3", Genre = "Jazz", UserId = _testUserId },
            new Song { Title = "Other User Song", Artist = "Other Artist", Genre = "Pop", UserId = "other-user" }
        };

        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.GetGenresAsync(_testUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Rock");
        result.Should().Contain("Jazz");
        result.Should().NotContain("Pop"); // Other user's genre
    }

    [Fact]
    public async Task GetTagsAsync_ShouldReturnUniqueTags_ForUser()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Tags = "rock, classic, guitar", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Tags = "rock, modern, drums", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Tags = "jazz, piano", UserId = _testUserId },
            new Song { Title = "Other Song", Artist = "Other Artist", Tags = "pop, vocals", UserId = "other-user" }
        };

        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.GetTagsAsync(_testUserId);

        // Assert
        result.Should().HaveCount(7);
        result.Should().Contain("rock");
        result.Should().Contain("classic");
        result.Should().Contain("guitar");
        result.Should().Contain("modern");
        result.Should().Contain("drums");
        result.Should().Contain("jazz");
        result.Should().Contain("piano");
        result.Should().NotContain("pop"); // Other user's tag
        result.Should().NotContain("vocals"); // Other user's tag
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}