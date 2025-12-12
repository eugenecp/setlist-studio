using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace SetlistStudio.Tests.Services;

/// <summary>
/// Comprehensive security tests for SongService
/// Covers: Authorization, input validation, malicious input handling, XSS/SQL injection prevention
/// Following TDD approach: Write tests first to define expected security behavior
/// </summary>
[Trait("Category", "Security")]
public class SongServiceSecurityTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SongService>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IQueryCacheService> _mockCacheService;
    private readonly SongService _songService;
    private readonly string _testUserId = "test-user-123";
    private readonly string _otherUserId = "other-user-456";

    public SongServiceSecurityTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SongService>>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockCacheService = new Mock<IQueryCacheService>();
        
        // Configure cache mock to always execute the callback
        _mockCacheService.Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<It.IsAnyType>>>()))
            .Returns(new InvocationFunc(invocation =>
            {
                var callback = invocation.Arguments[1] as Delegate;
                return callback?.DynamicInvoke() ?? Task.FromResult<object?>(null);
            }));
            
        _songService = new SongService(_context, _mockLogger.Object, _mockAuditLogService.Object, _mockCacheService.Object);
    }

    #region Authorization & User Ownership Tests

    [Fact]
    public async Task GetSongByIdAsync_ShouldReturnNull_WhenUserDoesNotOwnSong()
    {
        // Arrange: Create song owned by different user
        var song = new Song 
        { 
            Title = "Unauthorized Song", 
            Artist = "Test Artist", 
            UserId = _otherUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();
        
        // Act: Try to access with different userId
        var result = await _songService.GetSongByIdAsync(song.Id, _testUserId);
        
        // Assert: Access denied
        result.Should().BeNull("users should not be able to access songs they don't own");
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found or unauthorized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "unauthorized access attempts should be logged for security monitoring");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldNotReturnOtherUsersSongs_WhenFilteringByGenre()
    {
        // Arrange: Create songs for multiple users with same genre
        var testUserSongs = new[]
        {
            new Song { Title = "My Rock Song", Artist = "My Band", Genre = "Rock", UserId = _testUserId, CreatedAt = DateTime.UtcNow },
            new Song { Title = "My Jazz Song", Artist = "My Band", Genre = "Jazz", UserId = _testUserId, CreatedAt = DateTime.UtcNow }
        };
        
        var otherUserSongs = new[]
        {
            new Song { Title = "Their Rock Song", Artist = "Their Band", Genre = "Rock", UserId = _otherUserId, CreatedAt = DateTime.UtcNow },
            new Song { Title = "Another Rock Song", Artist = "Another Band", Genre = "Rock", UserId = "third-user", CreatedAt = DateTime.UtcNow }
        };
        
        _context.Songs.AddRange(testUserSongs);
        _context.Songs.AddRange(otherUserSongs);
        await _context.SaveChangesAsync();
        
        // Act: Get rock songs for test user
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, genre: "Rock");
        
        // Assert: Should only return test user's rock songs
        result.Should().HaveCount(1, "should only return songs owned by the requesting user");
        result.Should().OnlyContain(s => s.UserId == _testUserId, "userId filter must prevent cross-user data access");
        result.First().Title.Should().Be("My Rock Song");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSongsAsync_ShouldNotReturnOtherUsersSongs_WhenSearching()
    {
        // Arrange: Create songs with same title/artist for different users
        var testUserSong = new Song 
        { 
            Title = "Bohemian Rhapsody", 
            Artist = "Queen", 
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        
        var otherUserSongs = new[]
        {
            new Song { Title = "Bohemian Rhapsody", Artist = "Queen", UserId = _otherUserId, CreatedAt = DateTime.UtcNow },
            new Song { Title = "Bohemian Ballad", Artist = "Rock Band", UserId = "third-user", CreatedAt = DateTime.UtcNow }
        };
        
        _context.Songs.Add(testUserSong);
        _context.Songs.AddRange(otherUserSongs);
        await _context.SaveChangesAsync();
        
        // Act: Search for "Bohemian"
        var (result, totalCount) = await _songService.GetSongsAsync(_testUserId, searchTerm: "Bohemian");
        
        // Assert: Should only return test user's songs
        result.Should().HaveCount(1, "search must be scoped to user's own songs only");
        result.Should().OnlyContain(s => s.UserId == _testUserId);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldReturnNull_WhenUserDoesNotOwnSong()
    {
        // Arrange: Create song owned by different user
        var existingSong = new Song 
        { 
            Title = "Original Title", 
            Artist = "Original Artist", 
            UserId = _otherUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Songs.Add(existingSong);
        await _context.SaveChangesAsync();
        
        var updatedSong = new Song
        {
            Id = existingSong.Id,
            Title = "Hacked Title",
            Artist = "Hacker",
            UserId = _testUserId  // Trying to claim ownership
        };
        
        // Act: Try to update someone else's song
        var result = await _songService.UpdateSongAsync(updatedSong, _testUserId);
        
        // Assert: Update denied
        result.Should().BeNull("users should not be able to update songs they don't own");
        
        // Verify original song unchanged
        var unchanged = await _context.Songs.FindAsync(existingSong.Id);
        unchanged!.Title.Should().Be("Original Title", "unauthorized update attempts must not modify data");
        unchanged.Artist.Should().Be("Original Artist");
        unchanged.UserId.Should().Be(_otherUserId, "userId should never be changeable");
        
        // Verify warning was logged
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
    public async Task DeleteSongAsync_ShouldReturnFalse_WhenUserDoesNotOwnSong()
    {
        // Arrange: Create song owned by different user
        var song = new Song 
        { 
            Title = "Protected Song", 
            Artist = "Protected Artist", 
            UserId = _otherUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();
        
        // Act: Try to delete someone else's song
        var result = await _songService.DeleteSongAsync(song.Id, _testUserId);
        
        // Assert: Deletion denied
        result.Should().BeFalse("users should not be able to delete songs they don't own");
        
        // Verify song still exists
        var stillExists = await _context.Songs.FindAsync(song.Id);
        stillExists.Should().NotBeNull("unauthorized delete attempts must not remove data");
        stillExists!.Title.Should().Be("Protected Song");
        
        // Verify warning was logged
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
    public async Task GetGenresAsync_ShouldOnlyReturnUserGenres_NotAllGenres()
    {
        // Arrange: Create songs with different genres for different users
        var testUserSongs = new[]
        {
            new Song { Title = "Song 1", Artist = "Artist 1", Genre = "Rock", UserId = _testUserId, CreatedAt = DateTime.UtcNow },
            new Song { Title = "Song 2", Artist = "Artist 2", Genre = "Jazz", UserId = _testUserId, CreatedAt = DateTime.UtcNow }
        };
        
        var otherUserSongs = new[]
        {
            new Song { Title = "Song 3", Artist = "Artist 3", Genre = "Classical", UserId = _otherUserId, CreatedAt = DateTime.UtcNow },
            new Song { Title = "Song 4", Artist = "Artist 4", Genre = "Electronic", UserId = _otherUserId, CreatedAt = DateTime.UtcNow }
        };
        
        _context.Songs.AddRange(testUserSongs);
        _context.Songs.AddRange(otherUserSongs);
        await _context.SaveChangesAsync();
        
        // Setup cache to execute callback
        _mockCacheService.Setup(x => x.GetGenresAsync(It.IsAny<string>(), It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, callback) => callback());
        
        // Act: Get genres for test user
        var result = await _songService.GetGenresAsync(_testUserId);
        
        // Assert: Should only return test user's genres
        result.Should().HaveCount(2);
        result.Should().Contain("Rock");
        result.Should().Contain("Jazz");
        result.Should().NotContain("Classical", "other users' genres must not be exposed");
        result.Should().NotContain("Electronic", "genre lists must be scoped to user's own data");
    }

    [Fact]
    public async Task GetArtistsAsync_ShouldOnlyReturnUserArtists_NotAllArtists()
    {
        // Arrange: Create songs with different artists for different users
        var testUserSongs = new[]
        {
            new Song { Title = "Song 1", Artist = "My Artist 1", UserId = _testUserId, CreatedAt = DateTime.UtcNow },
            new Song { Title = "Song 2", Artist = "My Artist 2", UserId = _testUserId, CreatedAt = DateTime.UtcNow }
        };
        
        var otherUserSongs = new[]
        {
            new Song { Title = "Song 3", Artist = "Their Artist 1", UserId = _otherUserId, CreatedAt = DateTime.UtcNow },
            new Song { Title = "Song 4", Artist = "Their Artist 2", UserId = _otherUserId, CreatedAt = DateTime.UtcNow }
        };
        
        _context.Songs.AddRange(testUserSongs);
        _context.Songs.AddRange(otherUserSongs);
        await _context.SaveChangesAsync();
        
        // Setup cache to execute callback
        _mockCacheService.Setup(x => x.GetArtistsAsync(It.IsAny<string>(), It.IsAny<Func<Task<IEnumerable<string>>>>()))
            .Returns<string, Func<Task<IEnumerable<string>>>>((userId, callback) => callback());
        
        // Act: Get artists for test user
        var result = await _songService.GetArtistsAsync(_testUserId);
        
        // Assert: Should only return test user's artists
        result.Should().HaveCount(2);
        result.Should().Contain("My Artist 1");
        result.Should().Contain("My Artist 2");
        result.Should().NotContain("Their Artist 1", "other users' artists must not be exposed");
        result.Should().NotContain("Their Artist 2", "artist lists must be scoped to user's own data");
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public void ValidateSong_ShouldRejectInvalidBpm_BelowMinimum()
    {
        // Arrange: Song with BPM below realistic range (40-250)
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = 39,  // Below minimum
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("BPM") && e.Contains("40") && e.Contains("250"),
            "BPM validation must reject values below 40 to prevent invalid/malicious data");
    }

    [Fact]
    public void ValidateSong_ShouldRejectInvalidBpm_AboveMaximum()
    {
        // Arrange: Song with BPM above realistic range
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = 251,  // Above maximum
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("BPM") && e.Contains("40") && e.Contains("250"),
            "BPM validation must reject values above 250 to prevent DoS attacks with extreme values");
    }

    [Fact]
    public void ValidateSong_ShouldRejectNegativeBpm()
    {
        // Arrange: Song with negative BPM (potential attack vector)
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = -100,
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("BPM"),
            "negative BPM values must be rejected as they indicate malicious input");
    }

    [Fact]
    public void ValidateSong_ShouldRejectExtremelyHighBpm()
    {
        // Arrange: Song with unrealistically high BPM
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = 999999,  // Potential DoS vector
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("BPM"),
            "extreme BPM values must be rejected to prevent buffer overflow or integer overflow attacks");
    }

    [Fact]
    public void ValidateSong_ShouldAcceptValidBpm_MinimumBoundary()
    {
        // Arrange: Song with minimum valid BPM
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = 40,  // Minimum valid
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().NotContain(e => e.Contains("BPM"),
            "BPM of 40 should be accepted as valid (slow ballad tempo)");
    }

    [Fact]
    public void ValidateSong_ShouldAcceptValidBpm_MaximumBoundary()
    {
        // Arrange: Song with maximum valid BPM
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Bpm = 250,  // Maximum valid
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().NotContain(e => e.Contains("BPM"),
            "BPM of 250 should be accepted as valid (very fast tempo like speedcore)");
    }

    [Fact]
    public void ValidateSong_ShouldRejectInvalidDuration_NegativeValue()
    {
        // Arrange: Song with negative duration
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            DurationSeconds = -100,
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("Duration"),
            "negative duration values must be rejected");
    }

    [Fact]
    public void ValidateSong_ShouldRejectInvalidDuration_Zero()
    {
        // Arrange: Song with zero duration
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            DurationSeconds = 0,
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("Duration"),
            "zero duration must be rejected (minimum is 1 second)");
    }

    [Fact]
    public void ValidateSong_ShouldRejectInvalidDuration_TooLong()
    {
        // Arrange: Song with duration over 1 hour (3600 seconds)
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            DurationSeconds = 7200,  // 2 hours
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("Duration"),
            "durations over 1 hour must be rejected to prevent unrealistic/malicious data");
    }

    [Fact]
    public void ValidateSong_ShouldRejectInvalidDifficultyRating_BelowMinimum()
    {
        // Arrange: Song with difficulty below 1
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            DifficultyRating = 0,
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("Difficulty") && e.Contains("1") && e.Contains("5"),
            "difficulty rating must be between 1 and 5");
    }

    [Fact]
    public void ValidateSong_ShouldRejectInvalidDifficultyRating_AboveMaximum()
    {
        // Arrange: Song with difficulty above 5
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            DifficultyRating = 10,
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("Difficulty") && e.Contains("1") && e.Contains("5"),
            "difficulty rating above 5 must be rejected to prevent UI/reporting issues");
    }

    [Fact]
    public void ValidateSong_ShouldRejectTooLongTitle()
    {
        // Arrange: Song with title exceeding 200 characters
        var song = new Song
        {
            Title = new string('A', 201),  // 201 characters
            Artist = "Test Artist",
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("title") && e.Contains("200"),
            "titles over 200 characters must be rejected to prevent buffer overflow attacks");
    }

    [Fact]
    public void ValidateSong_ShouldRejectTooLongArtist()
    {
        // Arrange: Song with artist exceeding 200 characters
        var song = new Song
        {
            Title = "Test Song",
            Artist = new string('B', 201),  // 201 characters
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("Artist") && e.Contains("200"),
            "artist names over 200 characters must be rejected");
    }

    [Fact]
    public void ValidateSong_ShouldRejectMissingTitle()
    {
        // Arrange: Song without title
        var song = new Song
        {
            Title = "",
            Artist = "Test Artist",
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("title") && e.Contains("required"),
            "title is a required field");
    }

    [Fact]
    public void ValidateSong_ShouldRejectMissingArtist()
    {
        // Arrange: Song without artist
        var song = new Song
        {
            Title = "Test Song",
            Artist = "",
            UserId = _testUserId
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("Artist") && e.Contains("required"),
            "artist is a required field");
    }

    [Fact]
    public void ValidateSong_ShouldRejectMissingUserId()
    {
        // Arrange: Song without userId
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            UserId = ""
        };
        
        // Act
        var errors = _songService.ValidateSong(song);
        
        // Assert
        errors.Should().Contain(e => e.Contains("User ID") && e.Contains("required"),
            "userId is required to prevent orphaned data");
    }

    #endregion

    #region Malicious Input Tests

    [Theory]
    [InlineData("<script>alert('XSS')</script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("'; DROP TABLE Songs; --")]
    [InlineData("1' OR '1'='1")]
    [InlineData("UNION SELECT * FROM Users")]
    public async Task CreateSongAsync_ShouldThrowException_WhenTitleContainsMaliciousContent(string maliciousTitle)
    {
        // Arrange: Song with malicious title
        var song = new Song
        {
            Title = maliciousTitle,
            Artist = "Test Artist",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        
        // Act & Assert: Should throw validation exception
        // Note: This assumes validation attribute throws or validation method is called
        var act = async () => await _songService.CreateSongAsync(song);
        
        // The service validates and throws ArgumentException for validation failures
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Validation failed*",
                "malicious content in title must be rejected to prevent XSS and SQL injection attacks");
    }

    [Theory]
    [InlineData("<script>alert('XSS')</script>")]
    [InlineData("javascript:void(0)")]
    [InlineData("<iframe src='evil.com'>")]
    [InlineData("'; DELETE FROM Songs WHERE '1'='1")]
    public async Task CreateSongAsync_ShouldThrowException_WhenArtistContainsMaliciousContent(string maliciousArtist)
    {
        // Arrange: Song with malicious artist name
        var song = new Song
        {
            Title = "Test Song",
            Artist = maliciousArtist,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        
        // Act & Assert: Should throw validation exception
        var act = async () => await _songService.CreateSongAsync(song);
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Validation failed*",
                "malicious content in artist must be rejected");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleSearchTermSafely_PreventSqlInjection()
    {
        // Arrange: Normal songs in database
        var songs = new[]
        {
            new Song { Title = "Normal Song", Artist = "Normal Artist", UserId = _testUserId, CreatedAt = DateTime.UtcNow },
            new Song { Title = "Another Song", Artist = "Another Artist", UserId = _testUserId, CreatedAt = DateTime.UtcNow }
        };
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();
        
        // SQL injection attempt in search term
        var maliciousSearch = "'; DROP TABLE Songs; --";
        
        // Act: Should handle safely with parameterized query
        var act = async () => await _songService.GetSongsAsync(_testUserId, searchTerm: maliciousSearch);
        
        // Assert: Should not throw exception, should return empty results
        await act.Should().NotThrowAsync("Entity Framework LINQ should prevent SQL injection");
        
        var (result, count) = await _songService.GetSongsAsync(_testUserId, searchTerm: maliciousSearch);
        result.Should().BeEmpty("malicious SQL should not match any songs");
        
        // Verify songs still exist (table not dropped)
        var stillExists = await _context.Songs.CountAsync();
        stillExists.Should().Be(2, "SQL injection attempts must not modify database");
    }

    [Fact]
    public async Task GetSongsAsync_ShouldHandleGenreFilterSafely_PreventSqlInjection()
    {
        // Arrange: Songs with normal genres
        var song = new Song 
        { 
            Title = "Rock Song", 
            Artist = "Rock Band", 
            Genre = "Rock",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();
        
        // SQL injection attempt in genre filter
        var maliciousGenre = "Rock' OR '1'='1";
        
        // Act: Should handle safely
        var (result, count) = await _songService.GetSongsAsync(_testUserId, genre: maliciousGenre);
        
        // Assert: Should not return results (exact match only)
        result.Should().BeEmpty("malicious genre filter should not match with SQL injection attempt");
        count.Should().Be(0);
        
        // Verify correct genre still works
        var (validResult, validCount) = await _songService.GetSongsAsync(_testUserId, genre: "Rock");
        validResult.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateSongAsync_ShouldRejectEmptyUserId_PreventOrphanedData(string? emptyUserId)
    {
        // Arrange: Song with missing userId
        var song = new Song
        {
            Title = "Test Song",
            Artist = "Test Artist",
            UserId = emptyUserId!,
            CreatedAt = DateTime.UtcNow
        };
        
        // Act & Assert: Should throw validation exception
        var act = async () => await _songService.CreateSongAsync(song);
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*UserId*required*",
                "songs without userId must be rejected to prevent orphaned data");
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldNotAllowUserIdChange_PreventOwnershipHijacking()
    {
        // Arrange: Create song for test user
        var originalSong = new Song
        {
            Title = "Original Song",
            Artist = "Original Artist",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Songs.Add(originalSong);
        await _context.SaveChangesAsync();
        
        // Try to change ownership by updating userId
        var updatedSong = new Song
        {
            Id = originalSong.Id,
            Title = "Updated Song",
            Artist = "Updated Artist",
            UserId = _otherUserId,  // Attempting to change ownership!
            CreatedAt = originalSong.CreatedAt
        };
        
        // Act: Update with original user's credentials
        var result = await _songService.UpdateSongAsync(updatedSong, _testUserId);
        
        // Assert: Update should succeed but userId should remain unchanged
        result.Should().NotBeNull();
        result!.UserId.Should().Be(_testUserId, "userId should never be changeable through update");
        result.Title.Should().Be("Updated Song", "other fields should be updatable");
        
        // Verify in database
        var dbSong = await _context.Songs.FindAsync(originalSong.Id);
        dbSong!.UserId.Should().Be(_testUserId, "userId must remain immutable in database");
    }

    #endregion

    #region Error Handling & Security Logging Tests

    [Fact]
    public async Task GetSongByIdAsync_ShouldLogSecurityWarning_WhenUnauthorizedAccessAttempted()
    {
        // Arrange: Song owned by other user
        var song = new Song
        {
            Title = "Protected Song",
            Artist = "Protected Artist",
            UserId = _otherUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();
        
        // Act: Unauthorized access attempt
        await _songService.GetSongByIdAsync(song.Id, _testUserId);
        
        // Assert: Security warning should be logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("not found or unauthorized") &&
                    v.ToString()!.Contains(song.Id.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "unauthorized access attempts must be logged for security monitoring and incident response");
    }

    [Fact]
    public async Task UpdateSongAsync_ShouldLogSecurityWarning_WhenUnauthorizedUpdateAttempted()
    {
        // Arrange: Song owned by other user
        var song = new Song
        {
            Title = "Original",
            Artist = "Original",
            UserId = _otherUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();
        
        var maliciousUpdate = new Song
        {
            Id = song.Id,
            Title = "Hacked",
            Artist = "Hacker",
            UserId = _testUserId
        };
        
        // Act: Unauthorized update attempt
        await _songService.UpdateSongAsync(maliciousUpdate, _testUserId);
        
        // Assert: Security warning should be logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("not found or unauthorized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "unauthorized update attempts must be logged");
    }

    [Fact]
    public async Task DeleteSongAsync_ShouldLogSecurityWarning_WhenUnauthorizedDeleteAttempted()
    {
        // Arrange: Song owned by other user
        var song = new Song
        {
            Title = "Protected",
            Artist = "Protected",
            UserId = _otherUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();
        
        // Act: Unauthorized delete attempt
        await _songService.DeleteSongAsync(song.Id, _testUserId);
        
        // Assert: Security warning should be logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("not found or unauthorized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "unauthorized delete attempts must be logged for security audit trail");
    }

    [Fact]
    public async Task CreateSongAsync_ShouldThrowArgumentException_WhenValidationFails_NotExposeInternalDetails()
    {
        // Arrange: Invalid song (missing required fields)
        var invalidSong = new Song
        {
            Title = "",  // Invalid: empty title
            Artist = "",  // Invalid: empty artist
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        
        // Act
        var act = async () => await _songService.CreateSongAsync(invalidSong);
        
        // Assert: Should throw ArgumentException with validation errors
        var exception = await act.Should().ThrowAsync<ArgumentException>();
        
        // Verify error message doesn't expose internal details
        exception.Which.Message.Should().Contain("Validation failed");
        exception.Which.Message.Should().NotContain("database", "error messages must not expose internal system details");
        exception.Which.Message.Should().NotContain("SQL", "error messages must not expose database technology");
        exception.Which.Message.Should().NotContain("Exception", "error messages should not expose internal exception types");
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
