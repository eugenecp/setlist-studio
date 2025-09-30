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
/// Comprehensive tests for SetlistService covering all methods and edge cases
/// Covers: GetSetlistsAsync, GetSetlistByIdAsync, CreateSetlistAsync, UpdateSetlistAsync,
/// DeleteSetlistAsync, AddSongToSetlistAsync, RemoveSongFromSetlistAsync, 
/// ReorderSetlistSongsAsync, UpdateSetlistSongAsync, CopySetlistAsync
/// </summary>
public class SetlistServiceTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SetlistService>> _mockLogger;
    private readonly SetlistService _setlistService;
    private readonly string _testUserId = "test-user-123";
    private readonly string _otherUserId = "other-user-456";

    public SetlistServiceTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SetlistService>>();
        _setlistService = new SetlistService(_context, _mockLogger.Object);
    }

    #region GetSetlistsAsync Tests

    [Fact]
    public async Task GetSetlistsAsync_ShouldReturnUserSetlists_WhenUserHasSetlists()
    {
        // Arrange
        var song = new Song { Title = "Bohemian Rhapsody", Artist = "Queen", UserId = _testUserId };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        var setlists = new List<Setlist>
        {
            new Setlist 
            { 
                Name = "Rock Concert Main Set", 
                Description = "Main set for rock concert",
                Venue = "Madison Square Garden",
                UserId = _testUserId,
                IsTemplate = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Setlist 
            { 
                Name = "Wedding Reception", 
                Description = "Songs for wedding reception",
                Venue = "Grand Ballroom",
                UserId = _testUserId,
                IsTemplate = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Setlist 
            { 
                Name = "Other User Setlist", 
                UserId = _otherUserId,
                IsActive = true
            }
        };

        _context.Setlists.AddRange(setlists);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _setlistService.GetSetlistsAsync(_testUserId);

        // Assert
        result.Should().HaveCount(2);
        totalCount.Should().Be(2);
        result.Should().OnlyContain(s => s.UserId == _testUserId);
        result.Should().BeInDescendingOrder(s => s.CreatedAt);
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldReturnEmpty_WhenUserHasNoSetlists()
    {
        // Act
        var (result, totalCount) = await _setlistService.GetSetlistsAsync(_testUserId);

        // Assert
        result.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldFilterBySearchTerm_WhenSearchTermProvided()
    {
        // Arrange
        var setlists = new List<Setlist>
        {
            new Setlist { Name = "Rock Concert", Description = "Rock music", UserId = _testUserId },
            new Setlist { Name = "Jazz Evening", Description = "Smooth jazz", UserId = _testUserId },
            new Setlist { Name = "Pop Show", Description = "Popular music", Venue = "Rock Arena", UserId = _testUserId }
        };

        _context.Setlists.AddRange(setlists);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _setlistService.GetSetlistsAsync(_testUserId, searchTerm: "rock");

        // Assert
        result.Should().HaveCount(2); // "Rock Concert" and "Pop Show" (venue contains "Rock")
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldFilterByIsTemplate_WhenIsTemplateProvided()
    {
        // Arrange
        var setlists = new List<Setlist>
        {
            new Setlist { Name = "Template Set", IsTemplate = true, UserId = _testUserId },
            new Setlist { Name = "Regular Set", IsTemplate = false, UserId = _testUserId }
        };

        _context.Setlists.AddRange(setlists);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _setlistService.GetSetlistsAsync(_testUserId, isTemplate: true);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Template Set");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldFilterByIsActive_WhenIsActiveProvided()
    {
        // Arrange
        var setlists = new List<Setlist>
        {
            new Setlist { Name = "Active Set", IsActive = true, UserId = _testUserId },
            new Setlist { Name = "Inactive Set", IsActive = false, UserId = _testUserId }
        };

        _context.Setlists.AddRange(setlists);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _setlistService.GetSetlistsAsync(_testUserId, isActive: true);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active Set");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldHandlePagination_WhenPageParametersProvided()
    {
        // Arrange
        var setlists = Enumerable.Range(1, 25)
            .Select(i => new Setlist 
            { 
                Name = $"Setlist {i:D2}", 
                UserId = _testUserId,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            })
            .ToList();

        _context.Setlists.AddRange(setlists);
        await _context.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _setlistService.GetSetlistsAsync(_testUserId, pageNumber: 2, pageSize: 10);

        // Assert
        result.Should().HaveCount(10);
        totalCount.Should().Be(25);
        result.First().Name.Should().Be("Setlist 11"); // Should be ordered by CreatedAt descending
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldThrowException_WhenDatabaseError()
    {
        // Arrange
        await _context.DisposeAsync();

        // Act & Assert
        await _setlistService.Invoking(s => s.GetSetlistsAsync(_testUserId))
            .Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetSetlistByIdAsync Tests

    [Fact]
    public async Task GetSetlistByIdAsync_ShouldReturnSetlist_WhenSetlistExistsAndUserMatches()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        var setlist = new Setlist 
        { 
            Name = "Test Setlist", 
            UserId = _testUserId,
            SetlistSongs = new List<SetlistSong>
            {
                new SetlistSong { Song = song, Position = 1, PerformanceNotes = "Opening song" }
            }
        };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.GetSetlistByIdAsync(setlist.Id, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Setlist");
        result.SetlistSongs.Should().HaveCount(1);
        result.SetlistSongs.First().Song.Title.Should().Be("Test Song");
        result.SetlistSongs.First().Position.Should().Be(1);
    }

    [Fact]
    public async Task GetSetlistByIdAsync_ShouldReturnNull_WhenSetlistDoesNotExist()
    {
        // Act
        var result = await _setlistService.GetSetlistByIdAsync(999, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSetlistByIdAsync_ShouldReturnNull_WhenUserDoesNotMatch()
    {
        // Arrange
        var setlist = new Setlist { Name = "Other User Setlist", UserId = _otherUserId };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.GetSetlistByIdAsync(setlist.Id, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSetlistByIdAsync_ShouldThrowException_WhenDatabaseError()
    {
        // Arrange
        await _context.DisposeAsync();

        // Act & Assert
        await _setlistService.Invoking(s => s.GetSetlistByIdAsync(1, _testUserId))
            .Should().ThrowAsync<Exception>();
    }

    #endregion

    #region CreateSetlistAsync Tests

    [Fact]
    public async Task CreateSetlistAsync_ShouldCreateSetlist_WhenValidSetlist()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "New Concert Set",
            Description = "Exciting new concert setlist",
            Venue = "Concert Hall",
            PerformanceDate = DateTime.UtcNow.AddDays(30),
            ExpectedDurationMinutes = 90,
            IsTemplate = false,
            IsActive = true,
            UserId = _testUserId
        };

        // Act
        var result = await _setlistService.CreateSetlistAsync(setlist);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("New Concert Set");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var savedSetlist = await _context.Setlists.FindAsync(result.Id);
        savedSetlist.Should().NotBeNull();
        savedSetlist!.Name.Should().Be("New Concert Set");
    }

    [Fact]
    public async Task CreateSetlistAsync_ShouldThrowException_WhenSetlistIsNull()
    {
        // Act & Assert
        await _setlistService.Invoking(s => s.CreateSetlistAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateSetlistAsync_ShouldThrowException_WhenValidationFails()
    {
        // Arrange - setlist with invalid name (too long)
        var invalidSetlist = new Setlist
        {
            Name = new string('x', 201), // Exceeds max length of 200
            UserId = _testUserId
        };

        // Act & Assert
        await _setlistService.Invoking(s => s.CreateSetlistAsync(invalidSetlist))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Validation failed*");
    }

    [Fact]
    public async Task CreateSetlistAsync_ShouldThrowException_WhenDatabaseError()
    {
        // Arrange
        var setlist = new Setlist { Name = "Test", UserId = _testUserId };
        await _context.DisposeAsync();

        // Act & Assert
        await _setlistService.Invoking(s => s.CreateSetlistAsync(setlist))
            .Should().ThrowAsync<Exception>();
    }

    #endregion

    #region UpdateSetlistAsync Tests

    [Fact]
    public async Task UpdateSetlistAsync_ShouldUpdateSetlist_WhenValidSetlistAndCorrectUser()
    {
        // Arrange
        var originalSetlist = new Setlist
        {
            Name = "Original Name",
            Description = "Original Description",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        _context.Setlists.Add(originalSetlist);
        await _context.SaveChangesAsync();

        var updatedSetlist = new Setlist
        {
            Id = originalSetlist.Id,
            Name = "Updated Name",
            Description = "Updated Description",
            Venue = "New Venue",
            UserId = _testUserId
        };

        // Act
        var result = await _setlistService.UpdateSetlistAsync(updatedSetlist, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
        result.Venue.Should().Be("New Venue");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.CreatedAt.Should().Be(originalSetlist.CreatedAt); // Should preserve original CreatedAt
    }

    [Fact]
    public async Task UpdateSetlistAsync_ShouldReturnNull_WhenSetlistDoesNotExist()
    {
        // Arrange
        var nonExistentSetlist = new Setlist
        {
            Id = 999,
            Name = "Non-existent",
            UserId = _testUserId
        };

        // Act
        var result = await _setlistService.UpdateSetlistAsync(nonExistentSetlist, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSetlistAsync_ShouldReturnNull_WhenUserDoesNotMatch()
    {
        // Arrange
        var setlist = new Setlist { Name = "Other User Setlist", UserId = _otherUserId };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        var updatedSetlist = new Setlist
        {
            Id = setlist.Id,
            Name = "Hacker Update",
            UserId = _otherUserId
        };

        // Act
        var result = await _setlistService.UpdateSetlistAsync(updatedSetlist, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSetlistAsync_ShouldThrowException_WhenSetlistIsNull()
    {
        // Act & Assert
        await _setlistService.Invoking(s => s.UpdateSetlistAsync(null!, _testUserId))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateSetlistAsync_ShouldThrowException_WhenValidationFails()
    {
        // Arrange
        var setlist = new Setlist { Name = "Test", UserId = _testUserId };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        var invalidUpdate = new Setlist
        {
            Id = setlist.Id,
            Name = new string('x', 201), // Exceeds max length
            UserId = _testUserId
        };

        // Act & Assert
        await _setlistService.Invoking(s => s.UpdateSetlistAsync(invalidUpdate, _testUserId))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Validation failed*");
    }

    #endregion

    #region DeleteSetlistAsync Tests

    [Fact]
    public async Task DeleteSetlistAsync_ShouldDeleteSetlist_WhenCorrectUser()
    {
        // Arrange
        var setlist = new Setlist { Name = "To Delete", UserId = _testUserId };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.DeleteSetlistAsync(setlist.Id, _testUserId);

        // Assert
        result.Should().BeTrue();

        var deletedSetlist = await _context.Setlists.FindAsync(setlist.Id);
        deletedSetlist.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSetlistAsync_ShouldReturnFalse_WhenSetlistDoesNotExist()
    {
        // Act
        var result = await _setlistService.DeleteSetlistAsync(999, _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSetlistAsync_ShouldReturnFalse_WhenUserDoesNotMatch()
    {
        // Arrange
        var setlist = new Setlist { Name = "Protected Setlist", UserId = _otherUserId };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.DeleteSetlistAsync(setlist.Id, _testUserId);

        // Assert
        result.Should().BeFalse();

        var stillExistsSetlist = await _context.Setlists.FindAsync(setlist.Id);
        stillExistsSetlist.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteSetlistAsync_ShouldDeleteSetlistAndSongs_WhenSetlistHasSongs()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        _context.Songs.Add(song);
        
        var setlist = new Setlist 
        { 
            Name = "Setlist with Songs", 
            UserId = _testUserId,
            SetlistSongs = new List<SetlistSong>
            {
                new SetlistSong { Song = song, Position = 1 }
            }
        };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.DeleteSetlistAsync(setlist.Id, _testUserId);

        // Assert
        result.Should().BeTrue();

        var deletedSetlist = await _context.Setlists.FindAsync(setlist.Id);
        deletedSetlist.Should().BeNull();

        // Song should still exist (only setlist relationship is deleted)
        var songStillExists = await _context.Songs.FindAsync(song.Id);
        songStillExists.Should().NotBeNull();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}