using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Services;

/// <summary>
/// Comprehensive tests for SetlistService covering all remaining uncovered scenarios
/// Target: Increase SetlistService coverage from 91% to 100%
/// </summary>
public class SetlistServiceTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SetlistService>> _mockLogger;
    private readonly SetlistService _service;

    public SetlistServiceTests()
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

    #region ValidateSetlist Method Tests

    [Fact]
    public void ValidateSetlist_ShouldReturnNoErrors_WhenSetlistIsValid()
    {
        // Arrange
        var validSetlist = new Setlist
        {
            Name = "Valid Setlist",
            Description = "A valid description",
            Venue = "Madison Square Garden",
            ExpectedDurationMinutes = 90,
            PerformanceNotes = "Some notes",
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(validSetlist);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenNameIsNull()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = null!,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Setlist name is required");
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenNameIsEmpty()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "",
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Setlist name is required");
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenNameIsWhitespace()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "   ",
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Setlist name is required");
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenNameExceeds200Characters()
    {
        // Arrange
        var longName = new string('A', 201);
        var setlist = new Setlist
        {
            Name = longName,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Setlist name cannot exceed 200 characters");
    }

    [Fact]
    public void ValidateSetlist_ShouldAcceptName_WhenNameIs200Characters()
    {
        // Arrange
        var maxLengthName = new string('A', 200);
        var setlist = new Setlist
        {
            Name = maxLengthName,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().NotContain(e => e.Contains("name"));
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenDescriptionExceeds1000Characters()
    {
        // Arrange
        var longDescription = new string('D', 1001);
        var setlist = new Setlist
        {
            Name = "Valid Name",
            Description = longDescription,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Description cannot exceed 1000 characters");
    }

    [Fact]
    public void ValidateSetlist_ShouldAcceptDescription_WhenDescriptionIs1000Characters()
    {
        // Arrange
        var maxLengthDescription = new string('D', 1000);
        var setlist = new Setlist
        {
            Name = "Valid Name",
            Description = maxLengthDescription,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().NotContain(e => e.Contains("Description"));
    }

    [Fact]
    public void ValidateSetlist_ShouldAcceptDescription_WhenDescriptionIsNull()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Valid Name",
            Description = null,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().NotContain(e => e.Contains("Description"));
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenVenueExceeds200Characters()
    {
        // Arrange
        var longVenue = new string('V', 201);
        var setlist = new Setlist
        {
            Name = "Valid Name",
            Venue = longVenue,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Venue cannot exceed 200 characters");
    }

    [Fact]
    public void ValidateSetlist_ShouldAcceptVenue_WhenVenueIs200Characters()
    {
        // Arrange
        var maxLengthVenue = new string('V', 200);
        var setlist = new Setlist
        {
            Name = "Valid Name",
            Venue = maxLengthVenue,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().NotContain(e => e.Contains("Venue"));
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenExpectedDurationIsZero()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Valid Name",
            ExpectedDurationMinutes = 0,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Expected duration must be at least 1 minute");
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenExpectedDurationIsNegative()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Valid Name",
            ExpectedDurationMinutes = -5,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Expected duration must be at least 1 minute");
    }

    [Fact]
    public void ValidateSetlist_ShouldAcceptExpectedDuration_WhenExpectedDurationIsOne()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Valid Name",
            ExpectedDurationMinutes = 1,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().NotContain(e => e.Contains("duration"));
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenPerformanceNotesExceeds2000Characters()
    {
        // Arrange
        var longNotes = new string('N', 2001);
        var setlist = new Setlist
        {
            Name = "Valid Name",
            PerformanceNotes = longNotes,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Performance notes cannot exceed 2000 characters");
    }

    [Fact]
    public void ValidateSetlist_ShouldAcceptPerformanceNotes_WhenPerformanceNotesIs2000Characters()
    {
        // Arrange
        var maxLengthNotes = new string('N', 2000);
        var setlist = new Setlist
        {
            Name = "Valid Name",
            PerformanceNotes = maxLengthNotes,
            UserId = "user-123"
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().NotContain(e => e.Contains("Performance notes"));
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenUserIdIsNull()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Valid Name",
            UserId = null!
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("User ID is required");
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenUserIdIsEmpty()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Valid Name",
            UserId = ""
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("User ID is required");
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenUserIdIsWhitespace()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Valid Name",
            UserId = "   "
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("User ID is required");
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnMultipleErrors_WhenMultipleFieldsAreInvalid()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = null!,
            Description = new string('D', 1001),
            Venue = new string('V', 201),
            ExpectedDurationMinutes = -1,
            PerformanceNotes = new string('N', 2001),
            UserId = ""
        };

        // Act
        var errors = _service.ValidateSetlist(setlist);

        // Assert
        errors.Should().HaveCount(6);
        errors.Should().Contain("Setlist name is required");
        errors.Should().Contain("Description cannot exceed 1000 characters");
        errors.Should().Contain("Venue cannot exceed 200 characters");
        errors.Should().Contain("Expected duration must be at least 1 minute");
        errors.Should().Contain("Performance notes cannot exceed 2000 characters");
        errors.Should().Contain("User ID is required");
    }

    #endregion

    #region CopySetlistAsync Method Tests

    [Fact]
    public async Task CopySetlistAsync_ShouldReturnNull_WhenSourceSetlistNotFound()
    {
        // Arrange
        var userId = "user-123";
        var nonExistentSetlistId = 999;
        var newName = "Copied Setlist";

        // Act
        var result = await _service.CopySetlistAsync(nonExistentSetlistId, newName, userId);

        // Assert
        result.Should().BeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Source setlist {nonExistentSetlistId} not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CopySetlistAsync_ShouldReturnNull_WhenUserUnauthorized()
    {
        // Arrange
        var ownerId = "owner-123";
        var unauthorizedUserId = "other-user-456";
        
        var sourceSetlist = new Setlist
        {
            Name = "Original Setlist",
            Description = "Original description",
            UserId = ownerId,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Setlists.Add(sourceSetlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CopySetlistAsync(sourceSetlist.Id, "Copied Setlist", unauthorizedUserId);

        // Assert
        result.Should().BeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Source setlist {sourceSetlist.Id} not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CopySetlistAsync_ShouldCreateCopyWithCorrectProperties_WhenSourceSetlistExists()
    {
        // Arrange
        var userId = "user-123";
        var sourceSetlist = new Setlist
        {
            Name = "Original Setlist",
            Description = "Original description",
            Venue = "Original venue",
            PerformanceDate = DateTime.Now.AddDays(7),
            ExpectedDurationMinutes = 120,
            IsTemplate = true,
            IsActive = true,
            PerformanceNotes = "Original performance notes",
            UserId = userId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.Setlists.Add(sourceSetlist);
        await _context.SaveChangesAsync();

        var newName = "Copied Setlist";

        // Act
        var result = await _service.CopySetlistAsync(sourceSetlist.Id, newName, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(newName);
        result.Description.Should().Be(sourceSetlist.Description);
        result.Venue.Should().BeNull(); // Venue should not be copied
        result.PerformanceDate.Should().BeNull(); // Performance date should not be copied
        result.ExpectedDurationMinutes.Should().Be(sourceSetlist.ExpectedDurationMinutes);
        result.IsTemplate.Should().BeFalse(); // Should default to false
        result.IsActive.Should().BeFalse(); // Should default to false
        result.PerformanceNotes.Should().Be(sourceSetlist.PerformanceNotes);
        result.UserId.Should().Be(userId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Copied setlist {sourceSetlist.Id} to new setlist {result.Id}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CopySetlistAsync_ShouldCopySetlistSongs_WhenSourceSetlistHasSongs()
    {
        // Arrange
        var userId = "user-123";
        
        // Create songs
        var song1 = new Song { Title = "Bohemian Rhapsody", Artist = "Queen", UserId = userId };
        var song2 = new Song { Title = "Stairway to Heaven", Artist = "Led Zeppelin", UserId = userId };
        _context.Songs.AddRange(song1, song2);
        await _context.SaveChangesAsync();

        // Create source setlist
        var sourceSetlist = new Setlist
        {
            Name = "Original Setlist",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Setlists.Add(sourceSetlist);
        await _context.SaveChangesAsync();

        // Add songs to source setlist
        var setlistSong1 = new SetlistSong
        {
            SetlistId = sourceSetlist.Id,
            SongId = song1.Id,
            Position = 1,
            TransitionNotes = "Dramatic intro",
            PerformanceNotes = "Heavy vocals",
            IsEncore = false,
            IsOptional = false,
            CustomBpm = 72,
            CustomKey = "Bb",
            CreatedAt = DateTime.UtcNow
        };

        var setlistSong2 = new SetlistSong
        {
            SetlistId = sourceSetlist.Id,
            SongId = song2.Id,
            Position = 2,
            TransitionNotes = "Slow build",
            PerformanceNotes = "Epic solo",
            IsEncore = true,
            IsOptional = true,
            CustomBpm = 82,
            CustomKey = "Am",
            CreatedAt = DateTime.UtcNow
        };

        _context.SetlistSongs.AddRange(setlistSong1, setlistSong2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CopySetlistAsync(sourceSetlist.Id, "Copied Setlist", userId);

        // Assert
        result.Should().NotBeNull();

        var copiedSongs = await _context.SetlistSongs
            .Where(ss => ss.SetlistId == result!.Id)
            .OrderBy(ss => ss.Position)
            .ToListAsync();

        copiedSongs.Should().HaveCount(2);

        // Verify first song copy
        var copiedSong1 = copiedSongs[0];
        copiedSong1.SongId.Should().Be(song1.Id);
        copiedSong1.Position.Should().Be(1);
        copiedSong1.TransitionNotes.Should().Be("Dramatic intro");
        copiedSong1.PerformanceNotes.Should().Be("Heavy vocals");
        copiedSong1.IsEncore.Should().BeFalse();
        copiedSong1.IsOptional.Should().BeFalse();
        copiedSong1.CustomBpm.Should().Be(72);
        copiedSong1.CustomKey.Should().Be("Bb");

        // Verify second song copy
        var copiedSong2 = copiedSongs[1];
        copiedSong2.SongId.Should().Be(song2.Id);
        copiedSong2.Position.Should().Be(2);
        copiedSong2.TransitionNotes.Should().Be("Slow build");
        copiedSong2.PerformanceNotes.Should().Be("Epic solo");
        copiedSong2.IsEncore.Should().BeTrue();
        copiedSong2.IsOptional.Should().BeTrue();
        copiedSong2.CustomBpm.Should().Be(82);
        copiedSong2.CustomKey.Should().Be("Am");
    }

    [Fact]
    public async Task CopySetlistAsync_ShouldHandleEmptySetlist_WhenSourceSetlistHasNoSongs()
    {
        // Arrange
        var userId = "user-123";
        var sourceSetlist = new Setlist
        {
            Name = "Empty Setlist",
            Description = "No songs here",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(sourceSetlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CopySetlistAsync(sourceSetlist.Id, "Copied Empty Setlist", userId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Copied Empty Setlist");

        var copiedSongs = await _context.SetlistSongs
            .Where(ss => ss.SetlistId == result.Id)
            .ToListAsync();

        copiedSongs.Should().BeEmpty();
    }

    #endregion

    #region Additional Edge Cases

    [Fact]
    public async Task CreateSetlistAsync_ShouldThrowArgumentException_WhenValidationFails()
    {
        // Arrange
        var invalidSetlist = new Setlist
        {
            Name = null!, // Invalid: name is required
            UserId = "user-123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.CreateSetlistAsync(invalidSetlist));
        
        exception.Message.Should().Contain("Validation failed");
        exception.Message.Should().Contain("Setlist name is required");
    }

    [Fact]
    public async Task UpdateSetlistAsync_ShouldThrowArgumentException_WhenValidationFails()
    {
        // Arrange
        var userId = "user-123";
        
        // Create existing setlist
        var existingSetlist = new Setlist
        {
            Name = "Existing Setlist",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Setlists.Add(existingSetlist);
        await _context.SaveChangesAsync();

        // Create invalid update
        var invalidUpdate = new Setlist
        {
            Id = existingSetlist.Id,
            Name = null!, // Invalid: name is required
            UserId = userId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.UpdateSetlistAsync(invalidUpdate, userId));
        
        exception.Message.Should().Contain("Validation failed");
        exception.Message.Should().Contain("Setlist name is required");
    }

    [Fact]
    public async Task ReorderSetlistSongsAsync_ShouldReturnFalse_WhenSongOrderingContainsInvalidSongIds()
    {
        // Arrange
        var userId = "user-123";
        
        // Create setlist with songs
        var song1 = new Song { Title = "Song 1", Artist = "Artist 1", UserId = userId };
        var song2 = new Song { Title = "Song 2", Artist = "Artist 2", UserId = userId };
        _context.Songs.AddRange(song1, song2);
        await _context.SaveChangesAsync();

        var setlist = new Setlist { Name = "Test Setlist", UserId = userId };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        var setlistSong1 = new SetlistSong { SetlistId = setlist.Id, SongId = song1.Id, Position = 1 };
        var setlistSong2 = new SetlistSong { SetlistId = setlist.Id, SongId = song2.Id, Position = 2 };
        _context.SetlistSongs.AddRange(setlistSong1, setlistSong2);
        await _context.SaveChangesAsync();

        // Try to reorder with an invalid song ID
        var invalidOrdering = new[] { song1.Id, song2.Id, 999 }; // 999 doesn't exist in setlist

        // Act
        var result = await _service.ReorderSetlistSongsAsync(setlist.Id, invalidOrdering, userId);

        // Assert
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Invalid song ordering for setlist {setlist.Id}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}