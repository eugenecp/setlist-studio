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
/// Additional tests for SetlistService covering song management methods
/// Covers: AddSongToSetlistAsync, RemoveSongFromSetlistAsync, ReorderSetlistSongsAsync,
/// UpdateSetlistSongAsync, CopySetlistAsync, and validation methods
/// </summary>
public class SetlistServiceSongManagementTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SetlistService>> _mockLogger;
    private readonly SetlistService _setlistService;
    private readonly string _testUserId = "test-user-123";
    private readonly string _otherUserId = "other-user-456";

    public SetlistServiceSongManagementTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SetlistService>>();
        _setlistService = new SetlistService(_context, _mockLogger.Object);
    }

    #region AddSongToSetlistAsync Tests

    [Fact]
    public async Task AddSongToSetlistAsync_ShouldAddSong_WhenValidParameters()
    {
        // Arrange
        var song = new Song { Title = "Bohemian Rhapsody", Artist = "Queen", UserId = _testUserId };
        var setlist = new Setlist { Name = "Rock Concert", UserId = _testUserId };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.AddSongToSetlistAsync(setlist.Id, song.Id, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.SetlistId.Should().Be(setlist.Id);
        result.SongId.Should().Be(song.Id);
        result.Position.Should().Be(1); // First song
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AddSongToSetlistAsync_ShouldAddSongAtSpecificPosition_WhenPositionProvided()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", UserId = _testUserId }
        };
        var setlist = new Setlist { Name = "Test Setlist", UserId = _testUserId };
        
        _context.Songs.AddRange(songs);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Add first two songs
        await _setlistService.AddSongToSetlistAsync(setlist.Id, songs[0].Id, _testUserId);
        await _setlistService.AddSongToSetlistAsync(setlist.Id, songs[1].Id, _testUserId);

        // Act - Insert third song at position 2
        var result = await _setlistService.AddSongToSetlistAsync(setlist.Id, songs[2].Id, _testUserId, position: 2);

        // Assert
        result.Should().NotBeNull();
        result!.Position.Should().Be(2);

        // Verify reordering occurred - Query results separately and order them
        var setlistSongs = await _context.SetlistSongs
            .Where(ss => ss.SetlistId == setlist.Id)
            .OrderBy(ss => ss.Position)
            .ToListAsync();

        setlistSongs.Should().HaveCount(3);
        setlistSongs[0].SongId.Should().Be(songs[0].Id);
        setlistSongs[0].Position.Should().Be(1);
        setlistSongs[1].SongId.Should().Be(songs[2].Id);
        setlistSongs[1].Position.Should().Be(2);
        setlistSongs[2].SongId.Should().Be(songs[1].Id);
        setlistSongs[2].Position.Should().Be(3);
    }

    [Fact]
    public async Task AddSongToSetlistAsync_ShouldReturnNull_WhenSetlistDoesNotExist()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.AddSongToSetlistAsync(999, song.Id, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddSongToSetlistAsync_ShouldReturnNull_WhenSongDoesNotExist()
    {
        // Arrange
        var setlist = new Setlist { Name = "Test Setlist", UserId = _testUserId };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.AddSongToSetlistAsync(setlist.Id, 999, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddSongToSetlistAsync_ShouldReturnNull_WhenUserDoesNotOwnSetlist()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        var setlist = new Setlist { Name = "Other User Setlist", UserId = _otherUserId };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.AddSongToSetlistAsync(setlist.Id, song.Id, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddSongToSetlistAsync_ShouldReturnNull_WhenUserDoesNotOwnSong()
    {
        // Arrange
        var song = new Song { Title = "Other User Song", Artist = "Test Artist", UserId = _otherUserId };
        var setlist = new Setlist { Name = "Test Setlist", UserId = _testUserId };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.AddSongToSetlistAsync(setlist.Id, song.Id, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddSongToSetlistAsync_ShouldReturnNull_WhenSongAlreadyInSetlist()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        var setlist = new Setlist { Name = "Test Setlist", UserId = _testUserId };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Add song first time
        await _setlistService.AddSongToSetlistAsync(setlist.Id, song.Id, _testUserId);

        // Act - Try to add same song again
        var result = await _setlistService.AddSongToSetlistAsync(setlist.Id, song.Id, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RemoveSongFromSetlistAsync Tests

    [Fact]
    public async Task RemoveSongFromSetlistAsync_ShouldRemoveSong_WhenValidParameters()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        var setlist = new Setlist { Name = "Test Setlist", UserId = _testUserId };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        await _setlistService.AddSongToSetlistAsync(setlist.Id, song.Id, _testUserId);

        // Act
        var result = await _setlistService.RemoveSongFromSetlistAsync(setlist.Id, song.Id, _testUserId);

        // Assert
        result.Should().BeTrue();

        var updatedSetlist = await _context.Setlists
            .Include(s => s.SetlistSongs)
            .FirstAsync(s => s.Id == setlist.Id);
        updatedSetlist.SetlistSongs.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveSongFromSetlistAsync_ShouldReorderRemainingSongs_WhenSongRemovedFromMiddle()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", UserId = _testUserId }
        };
        var setlist = new Setlist { Name = "Test Setlist", UserId = _testUserId };
        
        _context.Songs.AddRange(songs);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Add all songs
        foreach (var song in songs)
        {
            await _setlistService.AddSongToSetlistAsync(setlist.Id, song.Id, _testUserId);
        }

        // Act - Remove middle song
        var result = await _setlistService.RemoveSongFromSetlistAsync(setlist.Id, songs[1].Id, _testUserId);

        // Assert
        result.Should().BeTrue();

        var updatedSetlist = await _context.Setlists
            .Include(s => s.SetlistSongs.OrderBy(ss => ss.Position))
            .FirstAsync(s => s.Id == setlist.Id);

        updatedSetlist.SetlistSongs.Should().HaveCount(2);
        updatedSetlist.SetlistSongs.ElementAt(0).SongId.Should().Be(songs[0].Id);
        updatedSetlist.SetlistSongs.ElementAt(0).Position.Should().Be(1);
        updatedSetlist.SetlistSongs.ElementAt(1).SongId.Should().Be(songs[2].Id);
        updatedSetlist.SetlistSongs.ElementAt(1).Position.Should().Be(2);
    }

    [Fact]
    public async Task RemoveSongFromSetlistAsync_ShouldReturnFalse_WhenSetlistDoesNotExist()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.RemoveSongFromSetlistAsync(999, song.Id, _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveSongFromSetlistAsync_ShouldReturnFalse_WhenUserDoesNotOwnSetlist()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        var setlist = new Setlist { Name = "Other User Setlist", UserId = _otherUserId };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.RemoveSongFromSetlistAsync(setlist.Id, song.Id, _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveSongFromSetlistAsync_ShouldReturnFalse_WhenSongNotInSetlist()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        var setlist = new Setlist { Name = "Test Setlist", UserId = _testUserId };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act - Try to remove song that was never added
        var result = await _setlistService.RemoveSongFromSetlistAsync(setlist.Id, song.Id, _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ReorderSetlistSongsAsync Tests

    [Fact]
    public async Task ReorderSetlistSongsAsync_ShouldReorderSongs_WhenValidParameters()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", UserId = _testUserId },
            new Song { Title = "Song 3", Artist = "Artist 3", UserId = _testUserId }
        };
        var setlist = new Setlist { Name = "Test Setlist", UserId = _testUserId };
        
        _context.Songs.AddRange(songs);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Add songs in order
        foreach (var song in songs)
        {
            await _setlistService.AddSongToSetlistAsync(setlist.Id, song.Id, _testUserId);
        }

        // Act - Reorder: [3, 1, 2]
        var newOrder = new[] { songs[2].Id, songs[0].Id, songs[1].Id };
        var result = await _setlistService.ReorderSetlistSongsAsync(setlist.Id, newOrder, _testUserId);

        // Assert
        result.Should().BeTrue();

        var setlistSongs = await _context.SetlistSongs
            .Where(ss => ss.SetlistId == setlist.Id)
            .OrderBy(ss => ss.Position)
            .ToListAsync();

        setlistSongs[0].SongId.Should().Be(songs[2].Id);
        setlistSongs[0].Position.Should().Be(1);
        setlistSongs[1].SongId.Should().Be(songs[0].Id);
        setlistSongs[1].Position.Should().Be(2);
        setlistSongs[2].SongId.Should().Be(songs[1].Id);
        setlistSongs[2].Position.Should().Be(3);
    }

    [Fact]
    public async Task ReorderSetlistSongsAsync_ShouldReturnFalse_WhenSetlistDoesNotExist()
    {
        // Act
        var result = await _setlistService.ReorderSetlistSongsAsync(999, new[] { 1, 2, 3 }, _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReorderSetlistSongsAsync_ShouldReturnFalse_WhenUserDoesNotOwnSetlist()
    {
        // Arrange
        var setlist = new Setlist { Name = "Other User Setlist", UserId = _otherUserId };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.ReorderSetlistSongsAsync(setlist.Id, new[] { 1, 2, 3 }, _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReorderSetlistSongsAsync_ShouldReturnFalse_WhenSongCountMismatch()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        var setlist = new Setlist { Name = "Test Setlist", UserId = _testUserId };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        await _setlistService.AddSongToSetlistAsync(setlist.Id, song.Id, _testUserId);

        // Act - Try to reorder with wrong number of songs
        var result = await _setlistService.ReorderSetlistSongsAsync(setlist.Id, new[] { song.Id, 999 }, _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region UpdateSetlistSongAsync Tests

    [Fact]
    public async Task UpdateSetlistSongAsync_ShouldUpdateSetlistSong_WhenValidParameters()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        var setlist = new Setlist { Name = "Test Setlist", UserId = _testUserId };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        var addedSetlistSong = await _setlistService.AddSongToSetlistAsync(setlist.Id, song.Id, _testUserId);
        addedSetlistSong.Should().NotBeNull();

        // Act
        var result = await _setlistService.UpdateSetlistSongAsync(
            addedSetlistSong!.Id, _testUserId,
            performanceNotes: "Great opening song",
            transitionNotes: "Fade into next song",
            customBpm: 120,
            customKey: "A");

        // Assert
        result.Should().NotBeNull();
        result!.PerformanceNotes.Should().Be("Great opening song");
        result.TransitionNotes.Should().Be("Fade into next song");
        result.CustomBpm.Should().Be(120);
        result.CustomKey.Should().Be("A");
    }

    [Fact]
    public async Task UpdateSetlistSongAsync_ShouldReturnNull_WhenSetlistSongDoesNotExist()
    {
        // Act - Try to update non-existent setlist song
        var result = await _setlistService.UpdateSetlistSongAsync(999, _testUserId, performanceNotes: "Test");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSetlistSongAsync_ShouldReturnNull_WhenUserDoesNotOwnSetlist()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _otherUserId };
        var setlist = new Setlist { Name = "Other User Setlist", UserId = _otherUserId };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Create a setlist song owned by other user
        var setlistSong = new SetlistSong 
        { 
            SetlistId = setlist.Id, 
            SongId = song.Id, 
            Position = 1,
            Setlist = setlist,
            Song = song
        };
        _context.SetlistSongs.Add(setlistSong);
        await _context.SaveChangesAsync();

        // Act - Try to update with wrong user
        var result = await _setlistService.UpdateSetlistSongAsync(setlistSong.Id, _testUserId, performanceNotes: "Test");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CopySetlistAsync Tests

    [Fact]
    public async Task CopySetlistAsync_ShouldCopySetlist_WhenValidParameters()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", UserId = _testUserId },
            new Song { Title = "Song 2", Artist = "Artist 2", UserId = _testUserId }
        };
        var originalSetlist = new Setlist 
        { 
            Name = "Original Setlist",
            Description = "Original description",
            Venue = "Original venue",
            UserId = _testUserId 
        };
        
        _context.Songs.AddRange(songs);
        _context.Setlists.Add(originalSetlist);
        await _context.SaveChangesAsync();

        // Add songs to original setlist
        foreach (var song in songs)
        {
            await _setlistService.AddSongToSetlistAsync(originalSetlist.Id, song.Id, _testUserId);
        }

        // Act
        var result = await _setlistService.CopySetlistAsync(originalSetlist.Id, "Copied Setlist", _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Copied Setlist");
        result.Description.Should().Be("Original description");
        result.Venue.Should().BeNull(); // Venue should not be copied (performance-specific)
        result.IsTemplate.Should().BeFalse(); // Copies should not be templates by default
        result.IsActive.Should().BeFalse(); // Copies should not be active by default
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify songs were copied
        var copiedSetlist = await _context.Setlists
            .Include(s => s.SetlistSongs.OrderBy(ss => ss.Position))
            .FirstAsync(s => s.Id == result.Id);

        copiedSetlist.SetlistSongs.Should().HaveCount(2);
        copiedSetlist.SetlistSongs.ElementAt(0).SongId.Should().Be(songs[0].Id);
        copiedSetlist.SetlistSongs.ElementAt(1).SongId.Should().Be(songs[1].Id);
    }

    [Fact]
    public async Task CopySetlistAsync_ShouldCopySetlistWithCustomNotes_WhenSetlistHasCustomizations()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = _testUserId };
        var originalSetlist = new Setlist { Name = "Original", UserId = _testUserId };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(originalSetlist);
        await _context.SaveChangesAsync();

        var addedSong = await _setlistService.AddSongToSetlistAsync(originalSetlist.Id, song.Id, _testUserId);
        await _setlistService.UpdateSetlistSongAsync(
            addedSong!.Id, _testUserId,
            performanceNotes: "Custom notes",
            transitionNotes: "Custom transition",
            customBpm: 130,
            customKey: "Bb");

        // Act
        var result = await _setlistService.CopySetlistAsync(originalSetlist.Id, "Copy", _testUserId);

        // Assert
        result.Should().NotBeNull();

        var copiedSetlist = await _context.Setlists
            .Include(s => s.SetlistSongs)
            .FirstAsync(s => s.Id == result!.Id);

        var copiedSong = copiedSetlist.SetlistSongs.First();
        copiedSong.PerformanceNotes.Should().Be("Custom notes");
        copiedSong.TransitionNotes.Should().Be("Custom transition");
        copiedSong.CustomBpm.Should().Be(130);
        copiedSong.CustomKey.Should().Be("Bb");
    }

    [Fact]
    public async Task CopySetlistAsync_ShouldReturnNull_WhenSourceSetlistDoesNotExist()
    {
        // Act
        var result = await _setlistService.CopySetlistAsync(999, "Copy", _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CopySetlistAsync_ShouldReturnNull_WhenUserDoesNotOwnSourceSetlist()
    {
        // Arrange
        var setlist = new Setlist { Name = "Other User Setlist", UserId = _otherUserId };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.CopySetlistAsync(setlist.Id, "Copy", _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CopySetlistAsync_ShouldThrowException_WhenNewNameIsInvalid(string invalidName)
    {
        // Arrange
        var setlist = new Setlist { Name = "Test Setlist", UserId = _testUserId };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act & Assert
        await _setlistService.Invoking(s => s.CopySetlistAsync(setlist.Id, invalidName, _testUserId))
            .Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}