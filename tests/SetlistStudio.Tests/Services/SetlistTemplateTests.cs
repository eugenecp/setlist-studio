using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using Xunit;
using FluentAssertions;
using Moq;

namespace SetlistStudio.Tests.Services;

/// <summary>
/// Comprehensive test suite for template functionality including:
/// - Template CRUD operations
/// - Authorization scenarios
/// - Validation tests  
/// - Template to setlist conversion
/// 
/// Follows TDD principles with tests created BEFORE implementation.
/// </summary>
public class SetlistTemplateTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly SetlistService _service;
    private readonly Mock<ILogger<SetlistService>> _mockLogger;
    private readonly string _testUserId = "test-user-123";

    public SetlistTemplateTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SetlistService>>();
        var mockCacheService = new Mock<IQueryCacheService>();
        _service = new SetlistService(_context, _mockLogger.Object, mockCacheService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Template CRUD Operations Tests

    [Fact]
    public async Task CreateSetlistAsync_ShouldCreateTemplate_WhenIsTemplateIsTrue()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Jazz Standards Template",
            Description = "Collection of jazz standards for club performances",
            IsTemplate = true,
            IsActive = false,
            ExpectedDurationMinutes = 120,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _service.CreateSetlistAsync(template);

        // Assert
        result.Should().NotBeNull();
        result!.IsTemplate.Should().BeTrue("Setlist should be marked as template");
        result.IsActive.Should().BeFalse("Templates should not be active");
        result.Venue.Should().BeNull("Templates should not have venue");
        result.PerformanceDate.Should().BeNull("Templates should not have performance date");

        // Verify database persistence
        var dbTemplate = await _context.Setlists.FindAsync(result.Id);
        dbTemplate.Should().NotBeNull();
        dbTemplate!.IsTemplate.Should().BeTrue();
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldReturnOnlyTemplates_WhenIsTemplateFilterIsTrue()
    {
        // Arrange
        var template1 = new Setlist
        {
            Name = "Wedding Template",
            IsTemplate = true,
            IsActive = false,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var template2 = new Setlist
        {
            Name = "Corporate Event Template",
            IsTemplate = true,
            IsActive = false,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var performance = new Setlist
        {
            Name = "Smith Wedding - June 2025",
            IsTemplate = false,
            IsActive = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        _context.Setlists.AddRange(template1, template2, performance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSetlistsAsync(
            _testUserId,
            searchTerm: null,
            isTemplate: true,
            isActive: null);

        // Assert
        result.Setlists.Should().HaveCount(2, "Should return only templates");
        result.Setlists.Should().OnlyContain(s => s.IsTemplate == true);
        result.Setlists.Should().Contain(s => s.Name == "Wedding Template");
        result.Setlists.Should().Contain(s => s.Name == "Corporate Event Template");
        result.Setlists.Should().NotContain(s => s.Name == "Smith Wedding - June 2025");
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldReturnOnlyPerformances_WhenIsTemplateFilterIsFalse()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var performance1 = new Setlist
        {
            Name = "Performance 1",
            IsTemplate = false,
            IsActive = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var performance2 = new Setlist
        {
            Name = "Performance 2",
            IsTemplate = false,
            IsActive = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        _context.Setlists.AddRange(template, performance1, performance2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSetlistsAsync(
            _testUserId,
            searchTerm: null,
            isTemplate: false,
            isActive: null);

        // Assert
        result.Setlists.Should().HaveCount(2, "Should return only performances");
        result.Setlists.Should().OnlyContain(s => s.IsTemplate == false);
    }

    [Fact]
    public async Task UpdateSetlistAsync_ShouldUpdateTemplate_PreservingTemplateStatus()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Original Template Name",
            Description = "Original Description",
            IsTemplate = true,
            IsActive = false,
            ExpectedDurationMinutes = 90,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        var updatedTemplate = new Setlist
        {
            Id = template.Id,
            Name = "Updated Template Name",
            Description = "Updated Description",
            ExpectedDurationMinutes = 120,
            IsTemplate = true,
            IsActive = false,
            UserId = _testUserId
        };

        // Act
        var result = await _service.UpdateSetlistAsync(updatedTemplate, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Template Name");
        result.Description.Should().Be("Updated Description");
        result.ExpectedDurationMinutes.Should().Be(120);
        result.IsTemplate.Should().BeTrue("Template status should be preserved");
        result.IsActive.Should().BeFalse("Template active status should be preserved");
    }

    [Fact]
    public async Task DeleteSetlistAsync_ShouldDeleteTemplate_WhenUserOwnsTemplate()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Template to Delete",
            IsTemplate = true,
            IsActive = false,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        var templateId = template.Id;

        // Act
        var result = await _service.DeleteSetlistAsync(templateId, _testUserId);

        // Assert
        result.Should().BeTrue("Template should be successfully deleted");

        var deletedTemplate = await _context.Setlists.FindAsync(templateId);
        deletedTemplate.Should().BeNull("Template should no longer exist in database");
    }

    #endregion

    #region Template Authorization Scenarios

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldReturnNull_WhenTemplateDoesNotExist()
    {
        // Arrange
        var nonExistentTemplateId = 99999;

        // Act
        var result = await _service.CreateFromTemplateAsync(
            nonExistentTemplateId,
            _testUserId,
            "New Performance");

        // Assert
        result.Should().BeNull("Should return null for non-existent template");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldReturnNull_WhenUserDoesNotOwnTemplate()
    {
        // Arrange
        var templateOwnerId = "owner-user-123";
        var unauthorizedUserId = "unauthorized-user-456";

        var template = new Setlist
        {
            Name = "Owner's Template",
            IsTemplate = true,
            IsActive = false,
            UserId = templateOwnerId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateFromTemplateAsync(
            template.Id,
            unauthorizedUserId,
            "Unauthorized Copy");

        // Assert
        result.Should().BeNull("Should return null when user doesn't own template");

        // Verify no setlist was created for unauthorized user
        var unauthorizedSetlists = await _context.Setlists
            .Where(s => s.UserId == unauthorizedUserId)
            .ToListAsync();
        unauthorizedSetlists.Should().BeEmpty("No setlist should be created for unauthorized user");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldReturnNull_WhenSetlistIsNotTemplate()
    {
        // Arrange
        var regularSetlist = new Setlist
        {
            Name = "Regular Performance Setlist",
            IsTemplate = false,
            IsActive = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(regularSetlist);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateFromTemplateAsync(
            regularSetlist.Id,
            _testUserId,
            "Copy of Performance");

        // Assert
        result.Should().BeNull("Should return null when trying to copy non-template setlist");
    }

    [Fact]
    public async Task GetSetlistsAsync_ShouldNotReturnOtherUsersTemplates()
    {
        // Arrange
        var user1Id = "user-1";
        var user2Id = "user-2";

        var user1Template = new Setlist
        {
            Name = "User 1 Template",
            IsTemplate = true,
            UserId = user1Id,
            CreatedAt = DateTime.UtcNow
        };

        var user2Template = new Setlist
        {
            Name = "User 2 Template",
            IsTemplate = true,
            UserId = user2Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.AddRange(user1Template, user2Template);
        await _context.SaveChangesAsync();

        // Act
        var user1Results = await _service.GetSetlistsAsync(
            user1Id,
            isTemplate: true);

        // Assert
        user1Results.Setlists.Should().HaveCount(1, "User should only see their own templates");
        user1Results.Setlists.First().Name.Should().Be("User 1 Template");
        user1Results.Setlists.Should().NotContain(s => s.Name == "User 2 Template");
    }

    [Fact]
    public async Task UpdateSetlistAsync_ShouldReturnNull_WhenUserDoesNotOwnTemplate()
    {
        // Arrange
        var ownerUserId = "owner-123";
        var unauthorizedUserId = "unauthorized-456";

        var template = new Setlist
        {
            Name = "Owner's Template",
            IsTemplate = true,
            UserId = ownerUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        var updatedTemplate = new Setlist
        {
            Id = template.Id,
            Name = "Unauthorized Update",
            IsTemplate = true,
            UserId = ownerUserId
        };

        // Act
        var result = await _service.UpdateSetlistAsync(updatedTemplate, unauthorizedUserId);

        // Assert
        result.Should().BeNull("Should return null when unauthorized user tries to update");

        // Verify template was not modified
        var dbTemplate = await _context.Setlists.FindAsync(template.Id);
        dbTemplate!.Name.Should().Be("Owner's Template", "Template should remain unchanged");
    }

    [Fact]
    public async Task DeleteSetlistAsync_ShouldReturnFalse_WhenUserDoesNotOwnTemplate()
    {
        // Arrange
        var ownerUserId = "owner-123";
        var unauthorizedUserId = "unauthorized-456";

        var template = new Setlist
        {
            Name = "Owner's Template",
            IsTemplate = true,
            UserId = ownerUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteSetlistAsync(template.Id, unauthorizedUserId);

        // Assert
        result.Should().BeFalse("Should return false when unauthorized user tries to delete");

        // Verify template still exists
        var dbTemplate = await _context.Setlists.FindAsync(template.Id);
        dbTemplate.Should().NotBeNull("Template should not be deleted");
    }

    #endregion

    #region Template Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateFromTemplateAsync_ShouldThrowArgumentException_WhenNameIsNullOrWhitespace(string invalidName)
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Valid Template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateFromTemplateAsync(
                template.Id,
                _testUserId,
                invalidName!));
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldThrowArgumentException_WhenNameExceedsMaxLength()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Valid Template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        var tooLongName = new string('A', 201); // Exceeds 200 character limit

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateFromTemplateAsync(
                template.Id,
                _testUserId,
                tooLongName));
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldThrowArgumentException_WhenVenueExceedsMaxLength()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Valid Template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        var tooLongVenue = new string('V', 201); // Exceeds 200 character limit

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateFromTemplateAsync(
                template.Id,
                _testUserId,
                "Valid Name",
                DateTime.UtcNow.AddDays(7),
                tooLongVenue));
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldThrowArgumentException_WhenPerformanceNotesExceedMaxLength()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Valid Template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        var tooLongNotes = new string('N', 2001); // Exceeds 2000 character limit

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateFromTemplateAsync(
                template.Id,
                _testUserId,
                "Valid Name",
                DateTime.UtcNow.AddDays(7),
                "Valid Venue",
                tooLongNotes));
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<img src=x onerror=alert('xss')>")]
    [InlineData("javascript:alert('xss')")]
    [InlineData("<iframe src='javascript:alert(1)'>")]
    public async Task CreateFromTemplateAsync_ShouldRejectMaliciousInput_InName(string maliciousName)
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Valid Template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateFromTemplateAsync(
                template.Id,
                _testUserId,
                maliciousName));
    }

    [Theory]
    [InlineData("'; DROP TABLE Setlists; --")]
    [InlineData("' OR '1'='1")]
    [InlineData("1'; DELETE FROM Setlists WHERE '1'='1")]
    public async Task CreateFromTemplateAsync_ShouldRejectSqlInjectionAttempts_InName(string sqlInjection)
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Valid Template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateFromTemplateAsync(
                template.Id,
                _testUserId,
                sqlInjection));
    }

    #endregion

    #region Template to Setlist Conversion Tests

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldInheritDescriptionFromTemplate()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Jazz Standards Template",
            Description = "Collection of classic jazz standards for sophisticated audiences",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "Blue Note - Friday Night");

        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().Be(template.Description, "Description should be inherited from template");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldInheritExpectedDurationFromTemplate()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Corporate Event Template",
            ExpectedDurationMinutes = 90,
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "ABC Corp Holiday Party");

        // Assert
        result.Should().NotBeNull();
        result!.ExpectedDurationMinutes.Should().Be(90, "Expected duration should be inherited from template");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldSetPerformanceDate_WhenProvided()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Wedding Template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        var performanceDate = new DateTime(2025, 6, 15, 14, 0, 0);

        // Act
        var result = await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "Smith Wedding",
            performanceDate);

        // Assert
        result.Should().NotBeNull();
        result!.PerformanceDate.Should().Be(performanceDate);
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldSetVenue_WhenProvided()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Concert Template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "Rock Concert",
            null,
            "Madison Square Garden");

        // Assert
        result.Should().NotBeNull();
        result!.Venue.Should().Be("Madison Square Garden");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldSetPerformanceNotes_WhenProvided()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Standard Template",
            PerformanceNotes = "Default performance notes",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "Special Performance",
            null,
            null,
            "Custom notes for this specific performance");

        // Assert
        result.Should().NotBeNull();
        result!.PerformanceNotes.Should().Be("Custom notes for this specific performance");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldUseTemplateNotesAsDefault_WhenPerformanceNotesNotProvided()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Standard Template",
            PerformanceNotes = "Default performance notes from template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "Performance Without Custom Notes");

        // Assert
        result.Should().NotBeNull();
        result!.PerformanceNotes.Should().Be("Default performance notes from template");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldNotModifyOriginalTemplate()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Original Template Name",
            Description = "Original Description",
            ExpectedDurationMinutes = 90,
            IsTemplate = true,
            IsActive = false,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        var originalTemplateId = template.Id;

        // Act
        await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "New Performance",
            DateTime.UtcNow.AddDays(7),
            "New Venue");

        // Assert - Verify template remains unchanged
        var unchangedTemplate = await _context.Setlists.FindAsync(originalTemplateId);
        unchangedTemplate.Should().NotBeNull();
        unchangedTemplate!.Name.Should().Be("Original Template Name");
        unchangedTemplate.Description.Should().Be("Original Description");
        unchangedTemplate.ExpectedDurationMinutes.Should().Be(90);
        unchangedTemplate.IsTemplate.Should().BeTrue();
        unchangedTemplate.IsActive.Should().BeFalse();
        unchangedTemplate.Venue.Should().BeNull("Template should not have venue");
        unchangedTemplate.PerformanceDate.Should().BeNull("Template should not have performance date");
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldCreateMultiplePerformances_FromSameTemplate()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Wedding Template",
            Description = "Standard wedding ceremony",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        await _context.SaveChangesAsync();

        // Act - Create multiple performances from same template
        var performance1 = await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "Smith Wedding",
            new DateTime(2025, 6, 15));

        var performance2 = await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "Johnson Wedding",
            new DateTime(2025, 7, 20));

        var performance3 = await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "Davis Wedding",
            new DateTime(2025, 8, 10));

        // Assert
        performance1.Should().NotBeNull();
        performance2.Should().NotBeNull();
        performance3.Should().NotBeNull();

        performance1!.Name.Should().Be("Smith Wedding");
        performance2!.Name.Should().Be("Johnson Wedding");
        performance3!.Name.Should().Be("Davis Wedding");

        // All should be performances, not templates
        performance1.IsTemplate.Should().BeFalse();
        performance2.IsTemplate.Should().BeFalse();
        performance3.IsTemplate.Should().BeFalse();

        // All should inherit description
        performance1.Description.Should().Be("Standard wedding ceremony");
        performance2.Description.Should().Be("Standard wedding ceremony");
        performance3.Description.Should().Be("Standard wedding ceremony");

        // Verify template still exists
        var unchangedTemplate = await _context.Setlists.FindAsync(template.Id);
        unchangedTemplate.Should().NotBeNull();
        unchangedTemplate!.IsTemplate.Should().BeTrue();
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldCopyAllSongsWithCompleteMetadata()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Rock Template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var song1 = new Song
        {
            Title = "Sweet Child O' Mine",
            Artist = "Guns N' Roses",
            Bpm = 125,
            MusicalKey = "D",
            Genre = "Rock",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var song2 = new Song
        {
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            Bpm = 72,
            MusicalKey = "Bb",
            Genre = "Rock",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var song3 = new Song
        {
            Title = "Stairway to Heaven",
            Artist = "Led Zeppelin",
            Bpm = 82,
            MusicalKey = "Am",
            Genre = "Rock",
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(template);
        _context.Songs.AddRange(song1, song2, song3);
        await _context.SaveChangesAsync();

        // Add songs to template with complete metadata
        var setlistSong1 = new SetlistSong
        {
            SetlistId = template.Id,
            SongId = song1.Id,
            Position = 1,
            TransitionNotes = "Fade in from silence",
            PerformanceNotes = "Extended intro solo",
            CustomBpm = 130,
            CustomKey = "E",
            IsEncore = false,
            IsOptional = false
        };

        var setlistSong2 = new SetlistSong
        {
            SetlistId = template.Id,
            SongId = song2.Id,
            Position = 2,
            TransitionNotes = "Pause for dramatic effect",
            PerformanceNotes = "Full band harmony on 'Mama mia'",
            IsEncore = false,
            IsOptional = true
        };

        var setlistSong3 = new SetlistSong
        {
            SetlistId = template.Id,
            SongId = song3.Id,
            Position = 3,
            TransitionNotes = "Smooth continuation",
            PerformanceNotes = "Save energy for big finish",
            CustomBpm = 85,
            IsEncore = true,
            IsOptional = false
        };

        _context.SetlistSongs.AddRange(setlistSong1, setlistSong2, setlistSong3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "Rock Concert - Summer Tour");

        // Assert
        result.Should().NotBeNull();

        var copiedSongs = await _context.SetlistSongs
            .Where(ss => ss.SetlistId == result!.Id)
            .OrderBy(ss => ss.Position)
            .ToListAsync();

        copiedSongs.Should().HaveCount(3, "All songs should be copied");

        // Verify first song metadata
        var copied1 = copiedSongs[0];
        copied1.SongId.Should().Be(song1.Id);
        copied1.Position.Should().Be(1);
        copied1.TransitionNotes.Should().Be("Fade in from silence");
        copied1.PerformanceNotes.Should().Be("Extended intro solo");
        copied1.CustomBpm.Should().Be(130);
        copied1.CustomKey.Should().Be("E");
        copied1.IsEncore.Should().BeFalse();
        copied1.IsOptional.Should().BeFalse();

        // Verify second song metadata
        var copied2 = copiedSongs[1];
        copied2.SongId.Should().Be(song2.Id);
        copied2.Position.Should().Be(2);
        copied2.TransitionNotes.Should().Be("Pause for dramatic effect");
        copied2.PerformanceNotes.Should().Be("Full band harmony on 'Mama mia'");
        copied2.CustomBpm.Should().BeNull();
        copied2.CustomKey.Should().BeNull();
        copied2.IsEncore.Should().BeFalse();
        copied2.IsOptional.Should().BeTrue();

        // Verify third song metadata
        var copied3 = copiedSongs[2];
        copied3.SongId.Should().Be(song3.Id);
        copied3.Position.Should().Be(3);
        copied3.TransitionNotes.Should().Be("Smooth continuation");
        copied3.PerformanceNotes.Should().Be("Save energy for big finish");
        copied3.CustomBpm.Should().Be(85);
        copied3.CustomKey.Should().BeNull();
        copied3.IsEncore.Should().BeTrue();
        copied3.IsOptional.Should().BeFalse();
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldPreserveSongOrder_WhenCopyingFromTemplate()
    {
        // Arrange
        var template = new Setlist
        {
            Name = "Ordered Template",
            IsTemplate = true,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        var songs = new List<Song>();
        for (int i = 1; i <= 10; i++)
        {
            songs.Add(new Song
            {
                Title = $"Song {i}",
                Artist = "Test Artist",
                UserId = _testUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.Setlists.Add(template);
        _context.Songs.AddRange(songs);
        await _context.SaveChangesAsync();

        // Add songs in specific order
        for (int i = 0; i < songs.Count; i++)
        {
            _context.SetlistSongs.Add(new SetlistSong
            {
                SetlistId = template.Id,
                SongId = songs[i].Id,
                Position = i + 1
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateFromTemplateAsync(
            template.Id,
            _testUserId,
            "Performance with Ordered Songs");

        // Assert
        var copiedSongs = await _context.SetlistSongs
            .Include(ss => ss.Song)
            .Where(ss => ss.SetlistId == result!.Id)
            .OrderBy(ss => ss.Position)
            .ToListAsync();

        copiedSongs.Should().HaveCount(10);

        for (int i = 0; i < copiedSongs.Count; i++)
        {
            copiedSongs[i].Position.Should().Be(i + 1, $"Song position {i + 1} should be preserved");
            copiedSongs[i].Song!.Title.Should().Be($"Song {i + 1}", $"Song {i + 1} should be in correct order");
        }
    }

    #endregion
}
