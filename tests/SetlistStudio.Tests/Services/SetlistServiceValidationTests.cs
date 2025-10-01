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
/// Comprehensive tests for SetlistService validation and edge cases
/// Testing all validation branches and error scenarios to improve branch coverage
/// </summary>
public class SetlistServiceValidationTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SetlistService>> _mockLogger;
    private readonly SetlistService _setlistService;
    private const string TestUserId = "test-user-123";

    public SetlistServiceValidationTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SetlistService>>();
        _setlistService = new SetlistService(_context, _mockLogger.Object);
    }

    #region Setlist Validation Tests - All Branches

    [Theory]
    [InlineData("", "Setlist name is required")]
    [InlineData("   ", "Setlist name is required")]
    [InlineData(null, "Setlist name is required")]
    public void ValidateSetlist_ShouldReturnError_WhenNameIsInvalid(string? name, string expectedError)
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.Name = name!;

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain(expectedError);
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenNameExceedsMaxLength()
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.Name = new string('A', 201); // Exceeds 200 character limit

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Setlist name cannot exceed 200 characters");
    }

    [Theory]
    [InlineData("", true)] // Empty string should not cause error
    [InlineData(null, true)] // Null should not cause error
    public void ValidateSetlist_ShouldNotReturnError_WhenDescriptionIsEmpty(string? description, bool shouldBeValid)
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.Description = description;

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        if (shouldBeValid)
        {
            errors.Should().NotContain(e => e.Contains("Description"));
        }
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.Description = new string('D', 1001); // Exceeds 1000 character limit

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Description cannot exceed 1000 characters");
    }

    [Theory]
    [InlineData("", true)] // Empty string should not cause error
    [InlineData(null, true)] // Null should not cause error
    public void ValidateSetlist_ShouldNotReturnError_WhenVenueIsEmpty(string? venue, bool shouldBeValid)
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.Venue = venue;

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        if (shouldBeValid)
        {
            errors.Should().NotContain(e => e.Contains("Venue"));
        }
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenVenueExceedsMaxLength()
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.Venue = new string('V', 201); // Exceeds 200 character limit

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Venue cannot exceed 200 characters");
    }

    [Theory]
    [InlineData(null, true)] // Null should be valid
    [InlineData(1, true)]    // Minimum valid value
    [InlineData(60, true)]   // Normal valid value
    [InlineData(240, true)]  // Large valid value
    public void ValidateSetlist_ShouldNotReturnError_WhenDurationIsValid(int? duration, bool shouldBeValid)
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.ExpectedDurationMinutes = duration;

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        if (shouldBeValid)
        {
            errors.Should().NotContain(e => e.Contains("duration"));
        }
    }

    [Theory]
    [InlineData(0, "Expected duration must be at least 1 minute")]
    [InlineData(-1, "Expected duration must be at least 1 minute")]
    [InlineData(-10, "Expected duration must be at least 1 minute")]
    public void ValidateSetlist_ShouldReturnError_WhenDurationIsInvalid(int duration, string expectedError)
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.ExpectedDurationMinutes = duration;

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain(expectedError);
    }

    [Theory]
    [InlineData("", true)] // Empty string should not cause error
    [InlineData(null, true)] // Null should not cause error
    public void ValidateSetlist_ShouldNotReturnError_WhenPerformanceNotesIsEmpty(string? notes, bool shouldBeValid)
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.PerformanceNotes = notes;

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        if (shouldBeValid)
        {
            errors.Should().NotContain(e => e.Contains("Performance notes"));
        }
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnError_WhenPerformanceNotesExceedsMaxLength()
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.PerformanceNotes = new string('N', 2001); // Exceeds 2000 character limit

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain("Performance notes cannot exceed 2000 characters");
    }

    [Theory]
    [InlineData("", "User ID is required")]
    [InlineData("   ", "User ID is required")]
    [InlineData(null, "User ID is required")]
    public void ValidateSetlist_ShouldReturnError_WhenUserIdIsInvalid(string? userId, string expectedError)
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.UserId = userId!;

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        errors.Should().Contain(expectedError);
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnMultipleErrors_WhenMultipleValidationsFail()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "", // Invalid - empty
            Description = new string('D', 1001), // Invalid - too long
            Venue = new string('V', 201), // Invalid - too long
            ExpectedDurationMinutes = -5, // Invalid - negative
            PerformanceNotes = new string('N', 2001), // Invalid - too long
            UserId = "" // Invalid - empty
        };

        // Act
        var errors = _setlistService.ValidateSetlist(setlist).ToList();

        // Assert
        errors.Should().HaveCount(6);
        errors.Should().Contain("Setlist name is required");
        errors.Should().Contain("Description cannot exceed 1000 characters");
        errors.Should().Contain("Venue cannot exceed 200 characters");
        errors.Should().Contain("Expected duration must be at least 1 minute");
        errors.Should().Contain("Performance notes cannot exceed 2000 characters");
        errors.Should().Contain("User ID is required");
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnNoErrors_WhenSetlistIsCompletelyValid()
    {
        // Arrange
        var setlist = CreateValidSetlist();

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnNoErrors_WhenOptionalFieldsAreNull()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Test Setlist",
            Description = null,
            Venue = null,
            ExpectedDurationMinutes = null,
            PerformanceNotes = null,
            UserId = TestUserId
        };

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnNoErrors_WhenOptionalFieldsAreEmpty()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Test Setlist",
            Description = "",
            Venue = "",
            ExpectedDurationMinutes = null,
            PerformanceNotes = "",
            UserId = TestUserId
        };

        // Act
        var errors = _setlistService.ValidateSetlist(setlist);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region Service Method Validation Integration Tests

    [Fact]
    public async Task CreateSetlistAsync_ShouldThrowArgumentException_WhenValidationFails()
    {
        // Arrange
        var invalidSetlist = CreateValidSetlist();
        invalidSetlist.Name = ""; // Invalid

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _setlistService.CreateSetlistAsync(invalidSetlist));
        
        exception.Message.Should().Contain("Validation failed");
        exception.Message.Should().Contain("Setlist name is required");
    }

    [Fact]
    public async Task UpdateSetlistAsync_ShouldThrowArgumentException_WhenValidationFails()
    {
        // Arrange
        var validSetlist = CreateValidSetlist();
        _context.Setlists.Add(validSetlist);
        await _context.SaveChangesAsync();

        var updateSetlist = CreateValidSetlist();
        updateSetlist.Id = validSetlist.Id;
        updateSetlist.Name = ""; // Invalid

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _setlistService.UpdateSetlistAsync(updateSetlist, TestUserId));
        
        exception.Message.Should().Contain("Validation failed");
        exception.Message.Should().Contain("Setlist name is required");
    }

    #endregion

    #region Filter and Search Branch Tests

    [Fact]
    public async Task GetSetlistsAsync_ShouldHandleNullSearchTerm_WithoutError()
    {
        // Arrange
        var setlist = CreateValidSetlist();
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, 
            searchTerm: null);

        // Assert
        setlists.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldHandleEmptySearchTerm_WithoutError()
    {
        // Arrange
        var setlist = CreateValidSetlist();
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, 
            searchTerm: "");

        // Assert
        setlists.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldHandleWhitespaceSearchTerm_WithoutError()
    {
        // Arrange
        var setlist = CreateValidSetlist();
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, 
            searchTerm: "   ");

        // Assert
        setlists.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Theory]
    [InlineData(true, 1)]   // Filter for templates should return 1
    [InlineData(false, 0)]  // Filter for non-templates should return 0
    [InlineData(null, 1)]   // No filter should return 1
    public async Task GetSetlistsAsync_ShouldFilterByIsTemplate_Correctly(bool? isTemplate, int expectedCount)
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.IsTemplate = true; // Set as template
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, 
            isTemplate: isTemplate);

        // Assert
        setlists.Should().HaveCount(expectedCount);
        totalCount.Should().Be(expectedCount);
    }

    [Theory]
    [InlineData(true, 1)]   // Filter for active should return 1
    [InlineData(false, 0)]  // Filter for inactive should return 0
    [InlineData(null, 1)]   // No filter should return 1
    public async Task GetSetlistsAsync_ShouldFilterByIsActive_Correctly(bool? isActive, int expectedCount)
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.IsActive = true; // Set as active
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, 
            isActive: isActive);

        // Assert
        setlists.Should().HaveCount(expectedCount);
        totalCount.Should().Be(expectedCount);
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldSearchInDescription_WhenProvided()
    {
        // Arrange
        var setlist1 = CreateValidSetlist();
        setlist1.Name = "First Setlist";
        setlist1.Description = "This contains the keyword 'special'";
        
        var setlist2 = CreateValidSetlist();
        setlist2.Id = 0; // Let EF assign ID
        setlist2.Name = "Second Setlist";
        setlist2.Description = "This is a normal description";

        _context.Setlists.AddRange(setlist1, setlist2);
        await _context.SaveChangesAsync();

        // Act
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, 
            searchTerm: "special");

        // Assert
        setlists.Should().HaveCount(1);
        setlists.First().Name.Should().Be("First Setlist");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldSearchInVenue_WhenProvided()
    {
        // Arrange
        var setlist1 = CreateValidSetlist();
        setlist1.Name = "First Setlist";
        setlist1.Venue = "Madison Square Garden";
        
        var setlist2 = CreateValidSetlist();
        setlist2.Id = 0; // Let EF assign ID
        setlist2.Name = "Second Setlist";
        setlist2.Venue = "Local Club";

        _context.Setlists.AddRange(setlist1, setlist2);
        await _context.SaveChangesAsync();

        // Act
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, 
            searchTerm: "madison");

        // Assert
        setlists.Should().HaveCount(1);
        setlists.First().Name.Should().Be("First Setlist");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldHandleNullDescriptionAndVenue_WhenSearching()
    {
        // Arrange
        var setlist = CreateValidSetlist();
        setlist.Description = null;
        setlist.Venue = null;
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act
        var (setlists, totalCount) = await _setlistService.GetSetlistsAsync(
            TestUserId, 
            searchTerm: "anything");

        // Assert - Should not crash and should return no results (since name doesn't match)
        setlists.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private Setlist CreateValidSetlist()
    {
        return new Setlist
        {
            Name = "Rock Concert Setlist",
            Description = "High-energy rock songs for the concert",
            Venue = "The Rock Arena",
            ExpectedDurationMinutes = 90,
            PerformanceNotes = "Start with slower songs, build energy",
            IsTemplate = false,
            IsActive = true,
            UserId = TestUserId
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #endregion
}