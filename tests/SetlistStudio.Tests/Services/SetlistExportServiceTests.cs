using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using System.Text;
using Xunit;

namespace SetlistStudio.Tests.Services;

/// <summary>
/// Comprehensive tests for SetlistExportService covering all scenarios
/// Target: Maintain 80%+ line and branch coverage
/// </summary>
public class SetlistExportServiceTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SetlistExportService>> _mockLogger;
    private readonly SetlistExportService _service;
    private const string TestUserId = "test-user-123";
    private const string OtherUserId = "other-user-456";

    public SetlistExportServiceTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SetlistExportService>>();
        _service = new SetlistExportService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region ExportSetlistToCsvAsync Tests

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldReturnNull_WhenSetlistNotFound()
    {
        // Arrange
        var nonExistentSetlistId = 999;

        // Act
        var result = await _service.ExportSetlistToCsvAsync(nonExistentSetlistId, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldReturnNull_WhenUserDoesNotOwnSetlist()
    {
        // Arrange
        var setlist = CreateTestSetlist("My Setlist", OtherUserId);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExportSetlistToCsvAsync(setlist.Id, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldReturnCsvBytes_WhenSetlistExists()
    {
        // Arrange
        var setlist = CreateTestSetlistWithSongs();
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExportSetlistToCsvAsync(setlist.Id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldIncludeSetlistMetadata_InCsvHeader()
    {
        // Arrange
        var setlist = CreateTestSetlist("Rock Concert", TestUserId);
        setlist.Description = "Amazing performance";
        setlist.Venue = "Madison Square Garden";
        setlist.PerformanceDate = new DateTime(2024, 12, 31, 20, 0, 0);
        setlist.ExpectedDurationMinutes = 120;
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExportSetlistToCsvAsync(setlist.Id, TestUserId);
        var csvContent = Encoding.UTF8.GetString(result!);

        // Assert
        csvContent.Should().Contain("# Name: Rock Concert");
        csvContent.Should().Contain("# Description: Amazing performance");
        csvContent.Should().Contain("# Venue: Madison Square Garden");
        csvContent.Should().Contain("# Performance Date: 2024-12-31");
        csvContent.Should().Contain("# Expected Duration: 120 minutes");
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldIncludeColumnHeaders()
    {
        // Arrange
        var setlist = CreateTestSetlist("Test Setlist", TestUserId);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExportSetlistToCsvAsync(setlist.Id, TestUserId);
        var csvContent = Encoding.UTF8.GetString(result!);

        // Assert
        csvContent.Should().Contain("Position,Title,Artist,Key,BPM,Duration (sec),Genre,Difficulty,Notes,Transition Notes,Encore,Optional");
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldExportSongsInCorrectOrder()
    {
        // Arrange
        var setlist = CreateTestSetlistWithSongs();
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExportSetlistToCsvAsync(setlist.Id, TestUserId);
        var csvContent = Encoding.UTF8.GetString(result!);
        var lines = csvContent.Split('\n');

        // Assert
        var dataLines = lines.Where(l => !l.StartsWith("#") && !l.Contains("Position,Title") && !string.IsNullOrWhiteSpace(l)).ToArray();
        dataLines.Should().HaveCount(2);
        dataLines[0].Should().StartWith("1,");
        dataLines[1].Should().StartWith("2,");
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldEscapeCommasInValues()
    {
        // Arrange
        var setlist = CreateTestSetlist("Test Setlist", TestUserId);
        var song = CreateTestSong("Song, with, commas", "Artist, Name", TestUserId);
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        var setlistSong = new SetlistSong
        {
            SetlistId = setlist.Id,
            SongId = song.Id,
            Position = 1
        };
        setlist.SetlistSongs.Add(setlistSong);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExportSetlistToCsvAsync(setlist.Id, TestUserId);
        var csvContent = Encoding.UTF8.GetString(result!);

        // Assert
        csvContent.Should().Contain("\"Song, with, commas\"");
        csvContent.Should().Contain("\"Artist, Name\"");
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldEscapeQuotesInValues()
    {
        // Arrange
        var setlist = CreateTestSetlist("Test Setlist", TestUserId);
        var song = CreateTestSong("Song \"with\" quotes", "Artist Name", TestUserId);
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        var setlistSong = new SetlistSong
        {
            SetlistId = setlist.Id,
            SongId = song.Id,
            Position = 1
        };
        setlist.SetlistSongs.Add(setlistSong);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExportSetlistToCsvAsync(setlist.Id, TestUserId);
        var csvContent = Encoding.UTF8.GetString(result!);

        // Assert
        csvContent.Should().Contain("\"Song \"\"with\"\" quotes\"");
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldHandleEmptySetlist()
    {
        // Arrange
        var setlist = CreateTestSetlist("Empty Setlist", TestUserId);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExportSetlistToCsvAsync(setlist.Id, TestUserId);
        var csvContent = Encoding.UTF8.GetString(result!);

        // Assert
        csvContent.Should().Contain("# Total Songs: 0");
        csvContent.Should().Contain("Position,Title,Artist");
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldIncludeCustomBpmAndKey()
    {
        // Arrange
        var setlist = CreateTestSetlist("Test Setlist", TestUserId);
        var song = CreateTestSong("Test Song", "Test Artist", TestUserId);
        song.BPM = 120;
        song.Key = "C";
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        var setlistSong = new SetlistSong
        {
            SetlistId = setlist.Id,
            SongId = song.Id,
            Position = 1,
            CustomBpm = 140,
            CustomKey = "D"
        };
        setlist.SetlistSongs.Add(setlistSong);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExportSetlistToCsvAsync(setlist.Id, TestUserId);
        var csvContent = Encoding.UTF8.GetString(result!);

        // Assert
        csvContent.Should().Contain(",D,140,");
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldIncludePerformanceNotes()
    {
        // Arrange
        var setlist = CreateTestSetlist("Test Setlist", TestUserId);
        var song = CreateTestSong("Test Song", "Test Artist", TestUserId);
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        var setlistSong = new SetlistSong
        {
            SetlistId = setlist.Id,
            SongId = song.Id,
            Position = 1,
            PerformanceNotes = "Play softly",
            TransitionNotes = "Quick transition"
        };
        setlist.SetlistSongs.Add(setlistSong);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExportSetlistToCsvAsync(setlist.Id, TestUserId);
        var csvContent = Encoding.UTF8.GetString(result!);

        // Assert
        csvContent.Should().Contain("Play softly");
        csvContent.Should().Contain("Quick transition");
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldIndicateEncoreAndOptionalSongs()
    {
        // Arrange
        var setlist = CreateTestSetlist("Test Setlist", TestUserId);
        var song = CreateTestSong("Encore Song", "Test Artist", TestUserId);
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        var setlistSong = new SetlistSong
        {
            SetlistId = setlist.Id,
            SongId = song.Id,
            Position = 1,
            IsEncore = true,
            IsOptional = true
        };
        setlist.SetlistSongs.Add(setlistSong);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExportSetlistToCsvAsync(setlist.Id, TestUserId);
        var csvContent = Encoding.UTF8.GetString(result!);

        // Assert
        csvContent.Should().Contain(",Yes,Yes");
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldThrowArgumentNullException_WhenUserIdIsNull()
    {
        // Arrange
        var setlist = CreateTestSetlist("Test Setlist", TestUserId);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.ExportSetlistToCsvAsync(setlist.Id, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("userId");
    }

    [Fact]
    public async Task ExportSetlistToCsvAsync_ShouldThrowArgumentNullException_WhenUserIdIsEmpty()
    {
        // Arrange
        var setlist = CreateTestSetlist("Test Setlist", TestUserId);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.ExportSetlistToCsvAsync(setlist.Id, "");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GenerateCsvFilename Tests

    [Fact]
    public void GenerateCsvFilename_ShouldGenerateFilenameWithName()
    {
        // Arrange
        var setlist = CreateTestSetlist("My Concert", TestUserId);

        // Act
        var filename = _service.GenerateCsvFilename(setlist);

        // Assert
        filename.Should().Contain("My_Concert");
        filename.Should().EndWith(".csv");
    }

    [Fact]
    public void GenerateCsvFilename_ShouldIncludePerformanceDate_WhenDateIsSet()
    {
        // Arrange
        var setlist = CreateTestSetlist("Concert", TestUserId);
        setlist.PerformanceDate = new DateTime(2024, 12, 31);

        // Act
        var filename = _service.GenerateCsvFilename(setlist);

        // Assert
        filename.Should().Contain("2024-12-31");
    }

    [Fact]
    public void GenerateCsvFilename_ShouldUseCurrentDate_WhenPerformanceDateIsNull()
    {
        // Arrange
        var setlist = CreateTestSetlist("Concert", TestUserId);
        setlist.PerformanceDate = null;

        // Act
        var filename = _service.GenerateCsvFilename(setlist);

        // Assert
        filename.Should().MatchRegex(@"setlist_.*_\d{4}-\d{2}-\d{2}\.csv");
    }

    [Fact]
    public void GenerateCsvFilename_ShouldSanitizeInvalidCharacters()
    {
        // Arrange
        var setlist = CreateTestSetlist("Concert/With\\Invalid:Chars*", TestUserId);

        // Act
        var filename = _service.GenerateCsvFilename(setlist);

        // Assert
        filename.Should().NotContain("/");
        filename.Should().NotContain("\\");
        filename.Should().NotContain(":");
        filename.Should().NotContain("*");
    }

    [Fact]
    public void GenerateCsvFilename_ShouldLimitLength()
    {
        // Arrange
        var longName = new string('A', 100);
        var setlist = CreateTestSetlist(longName, TestUserId);

        // Act
        var filename = _service.GenerateCsvFilename(setlist);

        // Assert
        // Should be shortened but still valid
        filename.Length.Should().BeLessThan(longName.Length + 20);
    }

    [Fact]
    public void GenerateCsvFilename_ShouldThrowArgumentNullException_WhenSetlistIsNull()
    {
        // Act
        Action act = () => _service.GenerateCsvFilename(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("setlist");
    }

    #endregion

    #region Helper Methods

    private Setlist CreateTestSetlist(string name, string userId)
    {
        return new Setlist
        {
            Name = name,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            SetlistSongs = new List<SetlistSong>()
        };
    }

    private Song CreateTestSong(string title, string artist, string userId)
    {
        return new Song
        {
            Title = title,
            Artist = artist,
            BPM = 120,
            Key = "C",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private Setlist CreateTestSetlistWithSongs()
    {
        var setlist = CreateTestSetlist("Test Setlist", TestUserId);

        var song1 = CreateTestSong("First Song", "Artist 1", TestUserId);
        var song2 = CreateTestSong("Second Song", "Artist 2", TestUserId);

        song1.Genre = "Rock";
        song1.Difficulty = 3;
        song1.DurationSeconds = 240;

        song2.Genre = "Pop";
        song2.Difficulty = 2;
        song2.DurationSeconds = 180;

        setlist.SetlistSongs.Add(new SetlistSong
        {
            Setlist = setlist,
            Song = song1,
            Position = 1,
            CreatedAt = DateTime.UtcNow
        });

        setlist.SetlistSongs.Add(new SetlistSong
        {
            Setlist = setlist,
            Song = song2,
            Position = 2,
            CreatedAt = DateTime.UtcNow
        });

        return setlist;
    }

    #endregion
}
