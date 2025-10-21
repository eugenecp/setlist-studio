using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using Xunit;
using FluentAssertions;
using Moq;

namespace SetlistStudio.Tests.Services;

/// <summary>
/// Advanced tests for SetlistService focusing on edge cases, error handling, 
/// and coverage gaps not covered in the base SetlistServiceTests.cs file.
/// Created because base test file exceeded 1,400 lines.
/// </summary>
public class SetlistServiceAdvancedTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly SetlistService _service;
    private readonly Mock<ILogger<SetlistService>> _mockLogger;
    private readonly string _testUserId = "test-user-123";

    public SetlistServiceAdvancedTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SetlistService>>();
        _service = new SetlistService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region DeleteSetlistAsync Advanced Coverage Tests

    [Fact]
    public async Task DeleteSetlistAsync_ShouldReturnTrue_WhenSetlistSuccessfullyDeleted()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Test Setlist to Delete",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        var setlistId = setlist.Id;

        // Act
        var result = await _service.DeleteSetlistAsync(setlistId, _testUserId);

        // Assert
        result.Should().BeTrue("Setlist should be successfully deleted");
        
        // Verify setlist is actually removed from database
        var deletedSetlist = await _context.Setlists.FindAsync(setlistId);
        deletedSetlist.Should().BeNull("Setlist should no longer exist in database");
    }

    [Fact]
    public async Task DeleteSetlistAsync_ShouldDeleteSetlistWithSongs_WhenSetlistContainsSongs()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Setlist with Songs",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            UserId = _testUserId
        };

        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        var setlistSong = new SetlistSong
        {
            SetlistId = setlist.Id,
            SongId = song.Id,
            Position = 1
        };

        _context.SetlistSongs.Add(setlistSong);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteSetlistAsync(setlist.Id, _testUserId);

        // Assert
        result.Should().BeTrue("Setlist with songs should be successfully deleted");
        
        // Verify setlist is removed
        var deletedSetlist = await _context.Setlists.FindAsync(setlist.Id);
        deletedSetlist.Should().BeNull("Setlist should no longer exist in database");
        
        // Verify setlist songs are also removed (cascade delete)
        var remainingSetlistSongs = await _context.SetlistSongs
            .Where(ss => ss.SetlistId == setlist.Id)
            .ToListAsync();
        remainingSetlistSongs.Should().BeEmpty("SetlistSongs should be removed with setlist");
    }

    [Fact]
    public async Task DeleteSetlistAsync_ShouldLogInformation_WhenSetlistSuccessfullyDeleted()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Setlist for Logging Test",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteSetlistAsync(setlist.Id, _testUserId);

        // Assert
        result.Should().BeTrue();
        
        // Verify logging (would need more sophisticated mock setup for detailed verification)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleted setlist")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteSetlistAsync_ShouldLogWarning_WhenSetlistNotFound()
    {
        // Act
        var result = await _service.DeleteSetlistAsync(999, _testUserId);

        // Assert
        result.Should().BeFalse();
        
        // Verify warning logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found or unauthorized")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteSetlistAsync_ShouldThrowException_WhenDatabaseErrorOccurs()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Test Setlist",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Dispose context to force database error
        await _context.DisposeAsync();

        // Act & Assert
        var act = async () => await _service.DeleteSetlistAsync(setlist.Id, _testUserId);
        await act.Should().ThrowAsync<Exception>("Database errors should be propagated");
    }

    [Fact]
    public async Task DeleteSetlistAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Test Setlist",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Dispose context to force database error
        await _context.DisposeAsync();

        // Act & Assert
        var act = async () => await _service.DeleteSetlistAsync(setlist.Id, _testUserId);
        await act.Should().ThrowAsync<Exception>();
        
        // Verify error logging - should be database error or concurrency error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Database error deleting setlist") || 
                                            v.ToString()!.Contains("Concurrency error deleting setlist")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ReorderSetlistSongsAsync Advanced Coverage Tests

    [Fact]
    public async Task ReorderSetlistSongsAsync_ShouldReturnTrue_WhenSuccessfullyReordered()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Test Setlist for Reorder",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", UserId = _testUserId }
        };

        _context.Songs.AddRange(songs);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        var setlistSongs = new List<SetlistSong>
        {
            new SetlistSong { SetlistId = setlist.Id, SongId = songs[0].Id, Position = 1 },
            new SetlistSong { SetlistId = setlist.Id, SongId = songs[1].Id, Position = 2 },
            new SetlistSong { SetlistId = setlist.Id, SongId = songs[2].Id, Position = 3 }
        };

        _context.SetlistSongs.AddRange(setlistSongs);
        await _context.SaveChangesAsync();

        // New order: reverse the songs
        var newOrder = new int[] { songs[2].Id, songs[1].Id, songs[0].Id };

        // Act
        var result = await _service.ReorderSetlistSongsAsync(setlist.Id, newOrder, _testUserId);

        // Assert
        result.Should().BeTrue("Reordering should succeed");
        
        // Verify new positions
        var reorderedSongs = await _context.SetlistSongs
            .Where(ss => ss.SetlistId == setlist.Id)
            .OrderBy(ss => ss.Position)
            .ToListAsync();

        reorderedSongs.Should().HaveCount(3);
        reorderedSongs[0].SongId.Should().Be(songs[2].Id, "First song should be the previously third song");
        reorderedSongs[1].SongId.Should().Be(songs[1].Id, "Second song should remain in middle");
        reorderedSongs[2].SongId.Should().Be(songs[0].Id, "Third song should be the previously first song");
    }

    [Fact]
    public async Task ReorderSetlistSongsAsync_ShouldThrowException_WhenDatabaseErrorOccurs()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Test Setlist",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        var songOrder = new int[] { 1, 2, 3 };

        // Dispose context to force database error
        await _context.DisposeAsync();

        // Act & Assert
        var act = async () => await _service.ReorderSetlistSongsAsync(setlist.Id, songOrder, _testUserId);
        await act.Should().ThrowAsync<Exception>("Database errors should be propagated");
    }

    #endregion

    #region UpdateSetlistAsync Advanced Coverage Tests

    [Fact]
    public async Task UpdateSetlistAsync_ShouldReturnUpdatedSetlist_WhenValidDataProvided()
    {
        // Arrange
        var originalSetlist = new Setlist
        {
            Name = "Original Name",
            Description = "Original Description",
            Venue = "Original Venue",
            PerformanceDate = DateTime.Today,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(originalSetlist);
        await _context.SaveChangesAsync();

        var updatedSetlist = new Setlist
        {
            Id = originalSetlist.Id,
            Name = "Updated Name",
            Description = "Updated Description",
            Venue = "Updated Venue",
            PerformanceDate = DateTime.Today.AddDays(7),
            UserId = _testUserId
        };

        // Act
        var result = await _service.UpdateSetlistAsync(updatedSetlist, _testUserId);

        // Assert
        result.Should().NotBeNull("Update should succeed");
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
        result.Venue.Should().Be("Updated Venue");
        result.PerformanceDate.Should().Be(DateTime.Today.AddDays(7));
        
        // Verify in database
        var dbSetlist = await _context.Setlists.FindAsync(originalSetlist.Id);
        dbSetlist!.Name.Should().Be("Updated Name");
        dbSetlist.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task UpdateSetlistAsync_ShouldThrowException_WhenDatabaseErrorOccurs()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Test Setlist",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        var updateSetlist = new Setlist
        {
            Id = setlist.Id,
            Name = "Updated Name",
            UserId = _testUserId
        };

        // Dispose context to force database error
        await _context.DisposeAsync();

        // Act & Assert
        var act = async () => await _service.UpdateSetlistAsync(updateSetlist, _testUserId);
        await act.Should().ThrowAsync<Exception>("Database errors should be propagated");
    }

    #endregion
}