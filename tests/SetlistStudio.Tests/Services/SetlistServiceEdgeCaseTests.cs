using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Services;

/// <summary>
/// Comprehensive edge case and error handling tests for SetlistService
/// Testing uncovered branches, exception scenarios, and error conditions
/// </summary>
public class SetlistServiceEdgeCaseTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SetlistService>> _mockLogger;
    private readonly SetlistService _setlistService;
    private const string TestUserId = "test-user-123";

    public SetlistServiceEdgeCaseTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SetlistService>>();
        _setlistService = new SetlistService(_context, _mockLogger.Object);
    }

    #region Null and ArgumentException Tests

    [Fact]
    public async Task CreateSetlistAsync_ShouldThrowArgumentNullException_WhenSetlistIsNull()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _setlistService.CreateSetlistAsync(null!));

        exception.ParamName.Should().Be("setlist");
    }

    [Fact]
    public async Task UpdateSetlistAsync_ShouldThrowArgumentNullException_WhenSetlistIsNull()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _setlistService.UpdateSetlistAsync(null!, TestUserId));

        exception.ParamName.Should().Be("setlist");
    }

    [Fact]
    public async Task UpdateSetlistAsync_ShouldReturnNull_WhenSetlistNotFound()
    {
        // Arrange
        var nonExistentSetlist = new Setlist
        {
            Id = 999,
            Name = "Non-existent Setlist",
            UserId = TestUserId
        };

        // Act
        var result = await _setlistService.UpdateSetlistAsync(nonExistentSetlist, TestUserId);

        // Assert
        result.Should().BeNull("Update should return null when setlist doesn't exist");
    }

    [Fact]
    public async Task DeleteSetlistAsync_ShouldReturnFalse_WhenSetlistNotFound()
    {
        // Act
        var result = await _setlistService.DeleteSetlistAsync(999, TestUserId);

        // Assert
        result.Should().Be(false, "Delete should return false when setlist doesn't exist");
    }

    #endregion

    #region Database Exception Handling Tests

    [Fact]
    public async Task GetSetlistsAsync_ShouldThrowAndLog_WhenDatabaseError()
    {
        // Arrange - Dispose the context to simulate database connection issues
        await _context.DisposeAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _setlistService.GetSetlistsAsync(TestUserId));

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Error retrieving setlists for user {TestUserId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSetlistByIdAsync_ShouldThrowAndLog_WhenDatabaseError()
    {
        // Arrange - Dispose the context
        await _context.DisposeAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _setlistService.GetSetlistByIdAsync(1, TestUserId));

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving setlist 1 for user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Complex Query Edge Cases

    [Fact]
    public async Task GetSetlistsAsync_ShouldHandleExtremelyLongSearchTerm()
    {
        // Arrange
        var extremelyLongSearchTerm = new string('a', 10000); // 10,000 characters

        // Act & Assert - Should not crash
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, searchTerm: extremelyLongSearchTerm);

        setlists.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldHandleSpecialCharactersInSearch()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Rock & Roll (Live at O'Malley's)",
            Description = "Special chars: !@#$%^&*()[]{}|\\:;\"'<>,.?/~`",
            UserId = TestUserId
        };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act - Search with special characters
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, searchTerm: "Rock & Roll");

        // Assert
        setlists.Should().HaveCount(1);
        setlists.First().Name.Should().Be("Rock & Roll (Live at O'Malley's)");
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldHandleUnicodeCharactersInSearch()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Ñiños Café 音楽 🎵",
            Description = "Unicode test: αβγδε العربية 中文",
            UserId = TestUserId
        };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, searchTerm: "音楽");

        // Assert
        setlists.Should().HaveCount(1);
        setlists.First().Name.Should().Contain("音楽");
    }

    [Theory]
    [InlineData(0, 20)] // Page 0 should be handled
    [InlineData(-1, 20)] // Negative page should be handled
    [InlineData(1, 0)] // Zero page size should be handled
    [InlineData(1, -5)] // Negative page size should be handled
    [InlineData(1, 1000)] // Very large page size should be handled
    [InlineData(999999, 20)] // Very large page number should be handled
    public async Task GetSetlistsAsync_ShouldHandleInvalidPaginationParameters(int pageNumber, int pageSize)
    {
        // Act & Assert - Should not crash with invalid pagination
        var getAction = async () => await _setlistService.GetSetlistsAsync(
            TestUserId, pageNumber: pageNumber, pageSize: pageSize);

        await getAction.Should().NotThrowAsync("Invalid pagination parameters should be handled gracefully");
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task UpdateSetlistAsync_ShouldHandleConcurrentModification()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Concurrent Test Setlist",
            UserId = TestUserId
        };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Simulate concurrent modification by creating a second context
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: $"ConcurrentTestDb_{Guid.NewGuid()}") // Use unique database name
            .Options;

        using var concurrentContext = new SetlistStudioDbContext(options);
        
        // Add the same setlist to the concurrent context
        var concurrentSetlist = new Setlist
        {
            Id = setlist.Id,
            Name = "Concurrent Test Setlist",
            UserId = TestUserId
        };
        concurrentContext.Setlists.Add(concurrentSetlist);
        await concurrentContext.SaveChangesAsync();

        var concurrentService = new SetlistService(concurrentContext, _mockLogger.Object);

        // Act - Modify in both contexts
        setlist.Name = "Modified by first context";
        var setlistCopy = await concurrentContext.Setlists.FindAsync(setlist.Id);
        if (setlistCopy != null)
        {
            setlistCopy.Name = "Modified by concurrent context";
            // Save concurrent modification first
            await concurrentContext.SaveChangesAsync();
        }

        // Then try to save original modification
        var result = await _setlistService.UpdateSetlistAsync(setlist, TestUserId);

        // Assert - Should handle gracefully (result may vary based on EF behavior)
        result.Should().NotBeNull("Update should succeed or handle concurrency gracefully");
    }

    #endregion

    #region Boundary Value Tests

    [Fact]
    public async Task GetSetlistsAsync_ShouldHandleMaxIntValues()
    {
        // Act & Assert - Should handle maximum integer values
        var getAction = async () => await _setlistService.GetSetlistsAsync(
            TestUserId, pageNumber: int.MaxValue, pageSize: int.MaxValue);

        await getAction.Should().NotThrowAsync("Should handle maximum integer values");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    [InlineData(null)]
    public async Task GetSetlistsAsync_ShouldHandleEmptyOrWhitespaceUserId(string? userId)
    {
        // Act
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(userId!);

        // Assert - Should return empty results for invalid userId
        setlists.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    #endregion

    #region Complex Validation Edge Cases

    [Fact]
    public async Task CreateSetlistAsync_ShouldHandleSetlistWithNullOptionalFields()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Minimal Setlist",
            Description = null,
            Venue = null,
            ExpectedDurationMinutes = null,
            PerformanceNotes = null,
            UserId = TestUserId,
            IsTemplate = false,
            IsActive = true
        };

        // Act
        var result = await _setlistService.CreateSetlistAsync(setlist);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Minimal Setlist");
        result.Description.Should().BeNull();
        result.Venue.Should().BeNull();
        result.ExpectedDurationMinutes.Should().BeNull();
        result.PerformanceNotes.Should().BeNull();
    }

    [Fact]
    public async Task CreateSetlistAsync_ShouldHandleSetlistWithMaxLengthFields()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = new string('N', 200), // Max length
            Description = new string('D', 1000), // Max length
            Venue = new string('V', 200), // Max length
            PerformanceNotes = new string('P', 2000), // Max length
            ExpectedDurationMinutes = 1, // Min valid value
            UserId = TestUserId
        };

        // Act
        var result = await _setlistService.CreateSetlistAsync(setlist);

        // Assert
        result.Should().NotBeNull();
        result.Name.Length.Should().Be(200);
        result.Description!.Length.Should().Be(1000);
        result.Venue!.Length.Should().Be(200);
        result.PerformanceNotes!.Length.Should().Be(2000);
        result.ExpectedDurationMinutes.Should().Be(1);
    }

    #endregion

    #region Memory and Resource Tests

    [Fact]
    public async Task GetSetlistsAsync_ShouldHandleLargeResultSet()
    {
        // Arrange - Create many setlists
        var setlists = Enumerable.Range(1, 1000).Select(i => new Setlist
        {
            Name = $"Setlist {i}",
            UserId = TestUserId
        }).ToList();

        _context.Setlists.AddRange(setlists);
        await _context.SaveChangesAsync();

        // Act
        var (results, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, pageSize: 100);

        // Assert
        results.Should().HaveCount(100, "Should respect page size");
        totalCount.Should().Be(1000, "Should count all setlists");
    }

    [Fact]
    public async Task GetSetlistByIdAsync_ShouldLogInformation_WhenSetlistFound()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Test Setlist for Logging",
            UserId = TestUserId
        };
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _setlistService.GetSetlistByIdAsync(setlist.Id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        
        // Verify information logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Retrieved setlist {setlist.Id} for user {TestUserId}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSetlistByIdAsync_ShouldNotLog_WhenSetlistNotFound()
    {
        // Act
        var result = await _setlistService.GetSetlistByIdAsync(999, TestUserId);

        // Assert
        result.Should().BeNull();
        
        // Verify no information logging for not found
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved setlist 999")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}