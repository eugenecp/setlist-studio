using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Infrastructure.Services;

/// <summary>
/// Tests for PerformanceDateService - verifies CRUD operations, authorization, and data isolation
/// </summary>
public class PerformanceDateServiceTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<PerformanceDateService>> _mockLogger;
    private readonly PerformanceDateService _service;
    private readonly string _testUserId = "test-user-123";
    private readonly string _otherUserId = "other-user-456";

    public PerformanceDateServiceTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<PerformanceDateService>>();
        _service = new PerformanceDateService(_context, _mockLogger.Object);

        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        // Add test user
        var user = new ApplicationUser
        {
            Id = _testUserId,
            UserName = "testuser@example.com",
            Email = "testuser@example.com"
        };
        _context.Users.Add(user);

        // Add other user
        var otherUser = new ApplicationUser
        {
            Id = _otherUserId,
            UserName = "otheruser@example.com",
            Email = "otheruser@example.com"
        };
        _context.Users.Add(otherUser);

        // Add test setlist
        var setlist = new Setlist
        {
            Id = 1,
            Name = "Test Setlist",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Setlists.Add(setlist);

        // Add other user's setlist
        var otherSetlist = new Setlist
        {
            Id = 2,
            Name = "Other User Setlist",
            UserId = _otherUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Setlists.Add(otherSetlist);

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetPerformanceDatesAsync Tests

    [Fact]
    public async Task GetPerformanceDatesAsync_WithAuthorizedUser_ShouldReturnPerformanceDates()
    {
        // Arrange
        var setlistId = 1;
        var performanceDate = new PerformanceDate
        {
            SetlistId = setlistId,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId,
            Venue = "Test Venue"
        };
        _context.PerformanceDates.Add(performanceDate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPerformanceDatesAsync(setlistId, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result!.First().Venue.Should().Be("Test Venue");
    }

    [Fact]
    public async Task GetPerformanceDatesAsync_WithUnauthorizedUser_ShouldReturnNull()
    {
        // Arrange
        var setlistId = 1;

        // Act
        var result = await _service.GetPerformanceDatesAsync(setlistId, _otherUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPerformanceDatesAsync_WithNonExistentSetlist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentSetlistId = 999;

        // Act
        var result = await _service.GetPerformanceDatesAsync(nonExistentSetlistId, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPerformanceDatesAsync_WithMultipleDates_ShouldReturnOrderedByDate()
    {
        // Arrange
        var setlistId = 1;
        var date1 = new PerformanceDate
        {
            SetlistId = setlistId,
            Date = DateTime.UtcNow.AddDays(14),
            UserId = _testUserId
        };
        var date2 = new PerformanceDate
        {
            SetlistId = setlistId,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId
        };
        _context.PerformanceDates.AddRange(date1, date2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPerformanceDatesAsync(setlistId, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result!.First().Date.Should().Be(date2.Date);
        result!.Last().Date.Should().Be(date1.Date);
    }

    #endregion

    #region GetPerformanceDateByIdAsync Tests

    [Fact]
    public async Task GetPerformanceDateByIdAsync_WithAuthorizedUser_ShouldReturnPerformanceDate()
    {
        // Arrange
        var performanceDate = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId,
            Venue = "Test Venue",
            Notes = "Test notes"
        };
        _context.PerformanceDates.Add(performanceDate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPerformanceDateByIdAsync(performanceDate.Id, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Venue.Should().Be("Test Venue");
        result.Notes.Should().Be("Test notes");
    }

    [Fact]
    public async Task GetPerformanceDateByIdAsync_WithUnauthorizedUser_ShouldReturnNull()
    {
        // Arrange
        var performanceDate = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId
        };
        _context.PerformanceDates.Add(performanceDate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPerformanceDateByIdAsync(performanceDate.Id, _otherUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPerformanceDateByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var result = await _service.GetPerformanceDateByIdAsync(nonExistentId, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreatePerformanceDateAsync Tests

    [Fact]
    public async Task CreatePerformanceDateAsync_WithValidData_ShouldCreatePerformanceDate()
    {
        // Arrange
        var performanceDate = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId,
            Venue = "New Venue",
            Notes = "Performance notes"
        };

        // Act
        var result = await _service.CreatePerformanceDateAsync(performanceDate);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
        result.Venue.Should().Be("New Venue");
        result.Notes.Should().Be("Performance notes");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreatePerformanceDateAsync_WithUnauthorizedSetlist_ShouldReturnNull()
    {
        // Arrange
        var performanceDate = new PerformanceDate
        {
            SetlistId = 2, // Belongs to other user
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId
        };

        // Act
        var result = await _service.CreatePerformanceDateAsync(performanceDate);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreatePerformanceDateAsync_WithNonExistentSetlist_ShouldReturnNull()
    {
        // Arrange
        var performanceDate = new PerformanceDate
        {
            SetlistId = 999,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId
        };

        // Act
        var result = await _service.CreatePerformanceDateAsync(performanceDate);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreatePerformanceDateAsync_WithoutVenue_ShouldCreateSuccessfully()
    {
        // Arrange
        var performanceDate = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId
        };

        // Act
        var result = await _service.CreatePerformanceDateAsync(performanceDate);

        // Assert
        result.Should().NotBeNull();
        result!.Venue.Should().BeNull();
    }

    #endregion

    #region DeletePerformanceDateAsync Tests

    [Fact]
    public async Task DeletePerformanceDateAsync_WithAuthorizedUser_ShouldDeleteSuccessfully()
    {
        // Arrange
        var performanceDate = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId
        };
        _context.PerformanceDates.Add(performanceDate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeletePerformanceDateAsync(performanceDate.Id, _testUserId);

        // Assert
        result.Should().BeTrue();
        var deletedDate = await _context.PerformanceDates.FindAsync(performanceDate.Id);
        deletedDate.Should().BeNull();
    }

    [Fact]
    public async Task DeletePerformanceDateAsync_WithUnauthorizedUser_ShouldReturnFalse()
    {
        // Arrange
        var performanceDate = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId
        };
        _context.PerformanceDates.Add(performanceDate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeletePerformanceDateAsync(performanceDate.Id, _otherUserId);

        // Assert
        result.Should().BeFalse();
        var existingDate = await _context.PerformanceDates.FindAsync(performanceDate.Id);
        existingDate.Should().NotBeNull();
    }

    [Fact]
    public async Task DeletePerformanceDateAsync_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var result = await _service.DeletePerformanceDateAsync(nonExistentId, _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetUpcomingPerformanceDatesAsync Tests

    [Fact]
    public async Task GetUpcomingPerformanceDatesAsync_WithFutureDates_ShouldReturnOnlyUpcoming()
    {
        // Arrange
        var pastDate = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(-7),
            UserId = _testUserId
        };
        var futureDate1 = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId
        };
        var futureDate2 = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(14),
            UserId = _testUserId
        };
        _context.PerformanceDates.AddRange(pastDate, futureDate1, futureDate2);
        await _context.SaveChangesAsync();

        // Act
        var (performanceDates, totalCount) = await _service.GetUpcomingPerformanceDatesAsync(_testUserId);

        // Assert
        performanceDates.Should().HaveCount(2);
        totalCount.Should().Be(2);
        performanceDates.Should().NotContain(pd => pd.Id == pastDate.Id);
    }

    [Fact]
    public async Task GetUpcomingPerformanceDatesAsync_ShouldReturnOrderedByDate()
    {
        // Arrange
        var date1 = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(14),
            UserId = _testUserId
        };
        var date2 = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId
        };
        _context.PerformanceDates.AddRange(date1, date2);
        await _context.SaveChangesAsync();

        // Act
        var (performanceDates, _) = await _service.GetUpcomingPerformanceDatesAsync(_testUserId);

        // Assert
        var datesList = performanceDates.ToList();
        datesList[0].Date.Should().BeBefore(datesList[1].Date);
    }

    [Fact]
    public async Task GetUpcomingPerformanceDatesAsync_WithPagination_ShouldRespectPageSize()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            _context.PerformanceDates.Add(new PerformanceDate
            {
                SetlistId = 1,
                Date = DateTime.UtcNow.AddDays(i),
                UserId = _testUserId
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var (performanceDates, totalCount) = await _service.GetUpcomingPerformanceDatesAsync(
            _testUserId, 
            pageNumber: 1, 
            pageSize: 3);

        // Assert
        performanceDates.Should().HaveCount(3);
        totalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetUpcomingPerformanceDatesAsync_WithNoUpcomingDates_ShouldReturnEmpty()
    {
        // Arrange
        var pastDate = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(-7),
            UserId = _testUserId
        };
        _context.PerformanceDates.Add(pastDate);
        await _context.SaveChangesAsync();

        // Act
        var (performanceDates, totalCount) = await _service.GetUpcomingPerformanceDatesAsync(_testUserId);

        // Assert
        performanceDates.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUpcomingPerformanceDatesAsync_ShouldIsolateUserData()
    {
        // Arrange
        var testUserDate = new PerformanceDate
        {
            SetlistId = 1,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _testUserId
        };
        var otherUserDate = new PerformanceDate
        {
            SetlistId = 2,
            Date = DateTime.UtcNow.AddDays(7),
            UserId = _otherUserId
        };
        _context.PerformanceDates.AddRange(testUserDate, otherUserDate);
        await _context.SaveChangesAsync();

        // Act
        var (performanceDates, totalCount) = await _service.GetUpcomingPerformanceDatesAsync(_testUserId);

        // Assert
        performanceDates.Should().HaveCount(1);
        performanceDates.First().UserId.Should().Be(_testUserId);
        totalCount.Should().Be(1);
    }

    #endregion
}
