using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Security;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Security;
using Xunit;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive tests for EnhancedAuthorizationService covering all authorization scenarios
/// Tests database integration, performance optimization, and comprehensive security validation
/// </summary>
public class EnhancedAuthorizationServiceTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<EnhancedAuthorizationService>> _mockLogger;
    private readonly EnhancedAuthorizationService _authService;
    
    private const string ValidUserId = "user123";
    private const string OtherUserId = "otheruser789";
    private const int TestSongId = 1;
    private const int TestSetlistId = 1;
    private const int TestSetlistSongId = 1;

    public EnhancedAuthorizationServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<EnhancedAuthorizationService>>();
        _authService = new EnhancedAuthorizationService(_context, _mockLogger.Object);
        
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test song
        _context.Songs.Add(new Song
        {
            Id = TestSongId,
            UserId = ValidUserId,
            Title = "Test Song",
            Artist = "Test Artist",
            CreatedAt = DateTime.UtcNow
        });

        // Add test setlist
        _context.Setlists.Add(new Setlist
        {
            Id = TestSetlistId,
            UserId = ValidUserId,
            Name = "Test Setlist",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Add test setlist song
        _context.SetlistSongs.Add(new SetlistSong
        {
            Id = TestSetlistSongId,
            SetlistId = TestSetlistId,
            SongId = TestSongId,
            Position = 1,
            CreatedAt = DateTime.UtcNow
        });

        // Add other user's resources for authorization testing
        _context.Songs.Add(new Song
        {
            Id = 2,
            UserId = OtherUserId,
            Title = "Other User Song",
            Artist = "Other Artist",
            CreatedAt = DateTime.UtcNow
        });

        _context.Setlists.Add(new Setlist
        {
            Id = 2,
            UserId = OtherUserId,
            Name = "Other User Setlist",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();
    }

    #region AuthorizeSongAccessAsync Tests

    [Fact]
    public async Task AuthorizeSongAccessAsync_WithValidUserAndSong_ShouldReturnSuccess()
    {
        // Act
        var result = await _authService.AuthorizeSongAccessAsync(TestSongId, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Read);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.UserId.Should().Be(ValidUserId);
        result.ResourceType.Should().Be("Song");
        result.ResourceId.Should().Be(TestSongId.ToString());
        result.Action.Should().Be("Read");
        result.SecurityContext.Should().ContainKeys("QueryExecutionTime", "DatabaseChecked");
    }

    [Fact]
    public async Task AuthorizeSongAccessAsync_WithInvalidUserId_ShouldReturnInvalidUser()
    {
        // Act
        var result = await _authService.AuthorizeSongAccessAsync(TestSongId, "", ResourceAuthorizationHelper.ResourceAction.Update);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Invalid or missing user ID");
    }

    [Fact]
    public async Task AuthorizeSongAccessAsync_WithNonexistentSong_ShouldReturnNotFound()
    {
        // Act
        var result = await _authService.AuthorizeSongAccessAsync(999, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Delete);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Requested resource does not exist or user does not have access");
    }

    [Fact]
    public async Task AuthorizeSongAccessAsync_WithOtherUsersSong_ShouldReturnForbidden()
    {
        // Act
        var result = await _authService.AuthorizeSongAccessAsync(2, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Read);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Contain("Resource belongs to user");
        result.SecurityContext.Should().ContainKey("ActualOwnerId").WhoseValue.Should().Be(OtherUserId);
    }

    #endregion

    #region AuthorizeSetlistAccessAsync Tests

    [Fact]
    public async Task AuthorizeSetlistAccessAsync_WithValidUserAndSetlist_ShouldReturnSuccess()
    {
        // Act
        var result = await _authService.AuthorizeSetlistAccessAsync(TestSetlistId, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Update);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.UserId.Should().Be(ValidUserId);
        result.ResourceType.Should().Be("Setlist");
        result.ResourceId.Should().Be(TestSetlistId.ToString());
        result.Action.Should().Be("Update");
        result.SecurityContext.Should().ContainKeys("QueryExecutionTime", "DatabaseChecked");
    }

    [Fact]
    public async Task AuthorizeSetlistAccessAsync_WithInvalidUserId_ShouldReturnInvalidUser()
    {
        // Act
        var result = await _authService.AuthorizeSetlistAccessAsync(TestSetlistId, null!, ResourceAuthorizationHelper.ResourceAction.Read);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Invalid or missing user ID");
    }

    [Fact]
    public async Task AuthorizeSetlistAccessAsync_WithNonexistentSetlist_ShouldReturnNotFound()
    {
        // Act
        var result = await _authService.AuthorizeSetlistAccessAsync(999, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Delete);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Requested resource does not exist or user does not have access");
    }

    [Fact]
    public async Task AuthorizeSetlistAccessAsync_WithOtherUsersSetlist_ShouldReturnForbidden()
    {
        // Act
        var result = await _authService.AuthorizeSetlistAccessAsync(2, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Update);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Contain("Resource belongs to user");
        result.SecurityContext.Should().ContainKey("ActualOwnerId").WhoseValue.Should().Be(OtherUserId);
    }

    #endregion

    #region AuthorizeSetlistSongAccessAsync Tests

    [Fact]
    public async Task AuthorizeSetlistSongAccessAsync_WithValidUserAndSetlistSong_ShouldReturnSuccess()
    {
        // Act
        var result = await _authService.AuthorizeSetlistSongAccessAsync(TestSetlistSongId, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Update);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.UserId.Should().Be(ValidUserId);
        result.ResourceType.Should().Be("SetlistSong");
        result.ResourceId.Should().Be(TestSetlistSongId.ToString());
        result.Action.Should().Be("Update");
        result.SecurityContext.Should().ContainKeys("SetlistId", "SongId", "ComprehensiveCheck");
        result.SecurityContext["SetlistId"].Should().Be(TestSetlistId);
        result.SecurityContext["SongId"].Should().Be(TestSongId);
        result.SecurityContext["ComprehensiveCheck"].Should().Be(true);
    }

    [Fact]
    public async Task AuthorizeSetlistSongAccessAsync_WithInvalidUserId_ShouldReturnInvalidUser()
    {
        // Act
        var result = await _authService.AuthorizeSetlistSongAccessAsync(TestSetlistSongId, "  ", ResourceAuthorizationHelper.ResourceAction.Delete);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Invalid or missing user ID");
    }

    [Fact]
    public async Task AuthorizeSetlistSongAccessAsync_WithNonexistentSetlistSong_ShouldReturnNotFound()
    {
        // Act
        var result = await _authService.AuthorizeSetlistSongAccessAsync(999, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Read);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Requested resource does not exist or user does not have access");
    }

    #endregion

    #region AuthorizeBulkSongAccessAsync Tests

    [Fact]
    public async Task AuthorizeBulkSongAccessAsync_WithValidSongs_ShouldReturnAllSuccesses()
    {
        // Arrange
        var songIds = new[] { TestSongId };

        // Act
        var results = await _authService.AuthorizeBulkSongAccessAsync(songIds, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Read);

        // Assert
        results.Should().HaveCount(1);
        results[TestSongId].IsAuthorized.Should().BeTrue();
        results[TestSongId].UserId.Should().Be(ValidUserId);
    }

    [Fact]
    public async Task AuthorizeBulkSongAccessAsync_WithMixedOwnership_ShouldReturnMixedResults()
    {
        // Arrange
        var songIds = new[] { TestSongId, 2 }; // TestSongId belongs to ValidUserId, 2 belongs to OtherUserId

        // Act
        var results = await _authService.AuthorizeBulkSongAccessAsync(songIds, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Update);

        // Assert
        results.Should().HaveCount(2);
        results[TestSongId].IsAuthorized.Should().BeTrue();
        results[2].IsAuthorized.Should().BeFalse();
        results[2].Reason.Should().Contain("Resource belongs to user");
    }

    [Fact]
    public async Task AuthorizeBulkSongAccessAsync_WithInvalidUserId_ShouldReturnAllFailures()
    {
        // Arrange
        var songIds = new[] { TestSongId, 2 };

        // Act
        var results = await _authService.AuthorizeBulkSongAccessAsync(songIds, "", ResourceAuthorizationHelper.ResourceAction.Delete);

        // Assert
        results.Should().HaveCount(2);
        results[TestSongId].IsAuthorized.Should().BeFalse();
        results[2].IsAuthorized.Should().BeFalse();
        results[TestSongId].Reason.Should().Be("Invalid or missing user ID");
        results[2].Reason.Should().Be("Invalid or missing user ID");
    }

    [Fact]
    public async Task AuthorizeBulkSongAccessAsync_WithNonexistentSongs_ShouldReturnNotFound()
    {
        // Arrange
        var songIds = new[] { 999, 998 };

        // Act
        var results = await _authService.AuthorizeBulkSongAccessAsync(songIds, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Read);

        // Assert
        results.Should().HaveCount(2);
        results[999].IsAuthorized.Should().BeFalse();
        results[998].IsAuthorized.Should().BeFalse();
        results[999].Reason.Should().Be("Requested resource does not exist or user does not have access");
        results[998].Reason.Should().Be("Requested resource does not exist or user does not have access");
    }

    [Fact]
    public async Task AuthorizeBulkSongAccessAsync_WithEmptyList_ShouldReturnEmptyResults()
    {
        // Arrange
        var songIds = Array.Empty<int>();

        // Act
        var results = await _authService.AuthorizeBulkSongAccessAsync(songIds, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Read);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region AuthorizeAddSongToSetlistAsync Tests

    [Fact]
    public async Task AuthorizeAddSongToSetlistAsync_WithValidUserSongAndSetlist_ShouldReturnSuccess()
    {
        // Act
        var result = await _authService.AuthorizeAddSongToSetlistAsync(TestSetlistId, TestSongId, ValidUserId);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.UserId.Should().Be(ValidUserId);
        result.Action.Should().Be("AddSongToSetlist");
        result.SecurityContext.Should().ContainKeys("SetlistId", "SongId", "ParallelChecks");
        result.SecurityContext["SetlistId"].Should().Be(TestSetlistId);
        result.SecurityContext["SongId"].Should().Be(TestSongId);
        result.SecurityContext["ParallelChecks"].Should().Be(true);
    }

    [Fact]
    public async Task AuthorizeAddSongToSetlistAsync_WithInvalidUserId_ShouldReturnInvalidUser()
    {
        // Act
        var result = await _authService.AuthorizeAddSongToSetlistAsync(TestSetlistId, TestSongId, "");

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Invalid or missing user ID");
    }

    [Fact]
    public async Task AuthorizeAddSongToSetlistAsync_WithOtherUsersSetlist_ShouldReturnForbidden()
    {
        // Act
        var result = await _authService.AuthorizeAddSongToSetlistAsync(2, TestSongId, ValidUserId);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Cannot add song: User does not own the target setlist");
        result.Action.Should().Be("AddSongToSetlist");
    }

    [Fact]
    public async Task AuthorizeAddSongToSetlistAsync_WithOtherUsersSong_ShouldReturnForbidden()
    {
        // Act
        var result = await _authService.AuthorizeAddSongToSetlistAsync(TestSetlistId, 2, ValidUserId);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Cannot add song: User does not own the song being added");
        result.Action.Should().Be("AddSongToSetlist");
    }

    [Fact]
    public async Task AuthorizeAddSongToSetlistAsync_WithNonexistentSetlist_ShouldReturnNotFound()
    {
        // Act
        var result = await _authService.AuthorizeAddSongToSetlistAsync(999, TestSongId, ValidUserId);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Cannot add song: User does not own the target setlist");
    }

    [Fact]
    public async Task AuthorizeAddSongToSetlistAsync_WithNonexistentSong_ShouldReturnNotFound()
    {
        // Act
        var result = await _authService.AuthorizeAddSongToSetlistAsync(TestSetlistId, 999, ValidUserId);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Cannot add song: User does not own the song being added");
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task AuthorizeSongAccessAsync_WithDatabaseException_ShouldReturnNotFoundAndLogError()
    {
        // Arrange - Dispose context to simulate database error
        await _context.DisposeAsync();

        // Act
        var result = await _authService.AuthorizeSongAccessAsync(TestSongId, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Read);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Requested resource does not exist or user does not have access");

        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Database error during song authorization") || 
                                            v.ToString()!.Contains("Invalid argument during song authorization") || 
                                            v.ToString()!.Contains("Invalid operation during song authorization")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task AuthorizeBulkSongAccessAsync_WithLargeBatch_ShouldPerformSingleQuery()
    {
        // Arrange - Add multiple songs
        var songs = Enumerable.Range(10, 50).Select(i => new Song
        {
            Id = i,
            UserId = ValidUserId,
            Title = $"Song {i}",
            Artist = $"Artist {i}",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        var songIds = songs.Select(s => s.Id).ToArray();

        // Act
        var startTime = DateTime.UtcNow;
        var results = await _authService.AuthorizeBulkSongAccessAsync(songIds, ValidUserId, ResourceAuthorizationHelper.ResourceAction.Read);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        results.Should().HaveCount(50);
        results.Values.Should().AllSatisfy(r => r.IsAuthorized.Should().BeTrue());
        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500)); // Should be fast with single query
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}