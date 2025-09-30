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
/// Tests for SongService error handling and exception scenarios
/// Covers: Database exceptions, logging verification, and error recovery
/// </summary>
public class SongServiceErrorHandlingTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SongService>> _mockLogger;
    private readonly SongService _songService;
    private readonly string _testUserId = "test-user-123";

    public SongServiceErrorHandlingTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SongService>>();
        _songService = new SongService(_context, _mockLogger.Object);
    }

    #region GetSongsAsync Error Handling Tests

    [Fact]
    public async Task GetSongsAsync_ShouldLogAndRethrowException_WhenDatabaseErrorOccurs()
    {
        // Arrange
        _context.Dispose(); // Force database error

        // Act & Assert
        await _songService.Invoking(s => s.GetSongsAsync(_testUserId))
            .Should().ThrowAsync<ObjectDisposedException>();

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving songs")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldLogSuccessfulRetrieval_WhenOperationSucceeds()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        await _songService.GetSongsAsync(_testUserId);

        // Assert - Verify information logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved") && v.ToString()!.Contains("songs")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetSongByIdAsync Error Handling Tests

    [Fact]
    public async Task GetSongByIdAsync_ShouldLogAndRethrowException_WhenDatabaseErrorOccurs()
    {
        // Arrange
        _context.Dispose(); // Force database error

        // Act & Assert
        await _songService.Invoking(s => s.GetSongByIdAsync(1, _testUserId))
            .Should().ThrowAsync<ObjectDisposedException>();

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSongByIdAsync_ShouldLogSuccessfulRetrieval_WhenSongFound()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        await _songService.GetSongByIdAsync(song.Id, _testUserId);

        // Assert - Verify information logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSongByIdAsync_ShouldNotLogSuccess_WhenSongNotFound()
    {
        // Act
        var result = await _songService.GetSongByIdAsync(999, _testUserId);

        // Assert
        result.Should().BeNull();

        // Verify no success logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion

    #region CreateSongAsync Error Handling Tests

    [Fact]
    public async Task CreateSongAsync_ShouldLogAndRethrowException_WhenDatabaseErrorOccurs()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        _context.Dispose(); // Force database error

        // Act & Assert
        await _songService.Invoking(s => s.CreateSongAsync(song))
            .Should().ThrowAsync<ObjectDisposedException>();

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateSongAsync_ShouldLogSuccessfulCreation_WhenOperationSucceeds()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };

        // Act
        await _songService.CreateSongAsync(song);

        // Assert - Verify information logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateSongAsync_ShouldNotLogCreation_WhenValidationFailsBeforeDatabaseOperation()
    {
        // Arrange
        var invalidSong = new Song { Title = "", Artist = "", UserId = _testUserId }; // Invalid

        // Act & Assert
        await _songService.Invoking(s => s.CreateSongAsync(invalidSong))
            .Should().ThrowAsync<ArgumentException>();

        // Verify no creation logging occurred (validation failed first)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

        // But error should be logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region UpdateSongAsync Error Handling Tests

    [Fact]
    public async Task UpdateSongAsync_ShouldLogAndRethrowException_WhenDatabaseErrorOccurs()
    {
        // Arrange
        var song = new Song { Id = 1, Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        _context.Dispose(); // Force database error

        // Act & Assert
        await _songService.Invoking(s => s.UpdateSongAsync(song, _testUserId))
            .Should().ThrowAsync<ObjectDisposedException>();

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error updating song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldLogWarning_WhenSongNotFoundOrUnauthorized()
    {
        // Arrange
        var song = new Song { Id = 999, Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };

        // Act
        var result = await _songService.UpdateSongAsync(song, _testUserId);

        // Assert
        result.Should().BeNull();

        // Verify warning logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found or unauthorized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldLogSuccessfulUpdate_WhenOperationSucceeds()
    {
        // Arrange
        var originalSong = new Song { Title = "Original", Artist = "Original Artist", UserId = _testUserId };
        _context.Songs.Add(originalSong);
        await _context.SaveChangesAsync();

        var updatedSong = new Song 
        { 
            Id = originalSong.Id, 
            Title = "Updated", 
            Artist = "Updated Artist", 
            UserId = _testUserId 
        };

        // Act
        await _songService.UpdateSongAsync(updatedSong, _testUserId);

        // Assert - Verify information logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region DeleteSongAsync Error Handling Tests

    [Fact]
    public async Task DeleteSongAsync_ShouldLogAndRethrowException_WhenDatabaseErrorOccurs()
    {
        // Arrange
        _context.Dispose(); // Force database error

        // Act & Assert
        await _songService.Invoking(s => s.DeleteSongAsync(1, _testUserId))
            .Should().ThrowAsync<ObjectDisposedException>();

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error deleting song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteSongAsync_ShouldLogWarning_WhenSongNotFoundOrUnauthorized()
    {
        // Act
        var result = await _songService.DeleteSongAsync(999, _testUserId);

        // Assert
        result.Should().BeFalse();

        // Verify warning logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found or unauthorized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteSongAsync_ShouldLogSuccessfulDeletion_WhenOperationSucceeds()
    {
        // Arrange
        var song = new Song { Title = "To Delete", Artist = "Test Artist", UserId = _testUserId };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        await _songService.DeleteSongAsync(song.Id, _testUserId);

        // Assert - Verify information logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleted song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetGenresAsync Error Handling Tests

    [Fact]
    public async Task GetGenresAsync_ShouldLogAndRethrowException_WhenDatabaseErrorOccurs()
    {
        // Arrange
        _context.Dispose(); // Force database error

        // Act & Assert
        await _songService.Invoking(s => s.GetGenresAsync(_testUserId))
            .Should().ThrowAsync<ObjectDisposedException>();

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving genres")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetTagsAsync Error Handling Tests

    [Fact]
    public async Task GetTagsAsync_ShouldLogAndRethrowException_WhenDatabaseErrorOccurs()
    {
        // Arrange
        _context.Dispose(); // Force database error

        // Act & Assert
        await _songService.Invoking(s => s.GetTagsAsync(_testUserId))
            .Should().ThrowAsync<ObjectDisposedException>();

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving tags")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Edge Case Scenarios

    [Fact]
    public async Task GetSongsAsync_ShouldHandleCaseInsensitiveSearch_Correctly()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "BOHEMIAN Rhapsody", Artist = "Queen", UserId = _testUserId },
            new Song { Title = "billie jean", Artist = "MICHAEL JACKSON", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - Test case insensitive search
        var (resultLower, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "bohemian");
        var (resultUpper, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "BILLIE");
        var (resultMixed, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "Jackson");

        // Assert
        resultLower.Should().HaveCount(1);
        resultLower.First().Title.Should().Be("BOHEMIAN Rhapsody");

        resultUpper.Should().HaveCount(1);
        resultUpper.First().Title.Should().Be("billie jean");

        resultMixed.Should().HaveCount(1);
        resultMixed.First().Artist.Should().Be("MICHAEL JACKSON");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleNullAlbumInSearch_Gracefully()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Album = null, UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Album = "Test Album", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act - Search for album term
        var (result, _) = await _songService.GetSongsAsync(_testUserId, searchTerm: "album");

        // Assert - Should only find the song with the album
        result.Should().HaveCount(1);
        result.First().Album.Should().Be("Test Album");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldHandleNullLogger_Gracefully()
    {
        // Arrange
        var nullLoggerService = new SongService(_context, null!);
        var song = new Song { Title = "Test", Artist = "Test", UserId = _testUserId };

        // Act & Assert - Should not throw null reference exception
        var result = await nullLoggerService.CreateSongAsync(song);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTagsAsync_ShouldHandleEmptyTagSegments_Correctly()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Tags = "tag1,,tag2,,,tag3", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", Tags = ",,,", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", Tags = "   ,  ,   ", UserId = _testUserId }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _songService.GetTagsAsync(_testUserId);

        // Assert - Should only contain valid, non-empty tags
        var tagList = result.ToList();
        tagList.Should().HaveCount(3);
        tagList.Should().Equal("tag1", "tag2", "tag3");
    }

    [Fact]
    public void ValidateSong_ShouldHandleNullSong_WithoutException()
    {
        // Act & Assert - Should not throw null reference exception
        var errors = _songService.ValidateSong(null!);
        
        // Should return validation errors for null song
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldPreserveCreatedAt_WhenUpdating()
    {
        // Arrange
        var originalTime = DateTime.UtcNow.AddDays(-1);
        var originalSong = new Song 
        { 
            Title = "Original", 
            Artist = "Original", 
            UserId = _testUserId,
            CreatedAt = originalTime
        };
        _context.Songs.Add(originalSong);
        await _context.SaveChangesAsync();

        var updateSong = new Song
        {
            Id = originalSong.Id,
            Title = "Updated", 
            Artist = "Updated", 
            UserId = _testUserId
        };

        // Act
        var result = await _songService.UpdateSongAsync(updateSong, _testUserId);

        // Assert - CreatedAt should remain unchanged
        result!.CreatedAt.Should().Be(originalTime);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}