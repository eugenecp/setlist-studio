using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Services;

/// <summary>
/// Comprehensive tests for SetlistTemplateService covering all WORKS, SECURE, SCALE, MAINTAINABLE, and USER DELIGHT principles
/// Target: Maintain 80%+ line and branch coverage
/// </summary>
public class SetlistTemplateServiceTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<ILogger<SetlistTemplateService>> _mockLogger;
    private readonly SetlistTemplateService _service;
    private const string TestUserId = "test-user-123";
    private const string OtherUserId = "other-user-456";

    public SetlistTemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SetlistTemplateService>>();
        _service = new SetlistTemplateService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region WORKS Principle Tests - Core Functionality

    [Fact]
    public async Task CreateTemplateAsync_WithValidData_CreatesTemplate()
    {
        // Arrange
        var songs = CreateTestSongs(TestUserId, 3);
        await _context.Songs.AddRangeAsync(songs);
        await _context.SaveChangesAsync();

        var template = new SetlistTemplate
        {
            Name = "Wedding Set - Classic Rock",
            Description = "3-hour wedding reception set",
            Category = "Wedding",
            EstimatedDurationMinutes = 180
        };

        var songIds = songs.Select(s => s.Id).ToArray();

        // Act
        var result = await _service.CreateTemplateAsync(template, songIds, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Wedding Set - Classic Rock");
        result.UserId.Should().Be(TestUserId);
        result.IsPublic.Should().BeFalse(); // Default private
        result.Songs.Should().HaveCount(3);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UsageCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTemplateByIdAsync_WhenOwner_ReturnsTemplate()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(TestUserId, "My Template");

        // Act
        var result = await _service.GetTemplateByIdAsync(template.Id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Name.Should().Be("My Template");
    }

    [Fact]
    public async Task GetTemplateByIdAsync_WhenPublic_ReturnsTemplateForAnyUser()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(TestUserId, "Public Template");
        template.IsPublic = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTemplateByIdAsync(template.Id, OtherUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Name.Should().Be("Public Template");
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsUserTemplatesAndPublicTemplates()
    {
        // Arrange
        var userTemplate1 = await CreateTestTemplateAsync(TestUserId, "My Template 1");
        var userTemplate2 = await CreateTestTemplateAsync(TestUserId, "My Template 2");
        var otherUserTemplate = await CreateTestTemplateAsync(OtherUserId, "Other Template");
        otherUserTemplate.IsPublic = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTemplatesAsync(TestUserId, includePublic: true);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.Id == userTemplate1.Id);
        result.Should().Contain(t => t.Id == userTemplate2.Id);
        result.Should().Contain(t => t.Id == otherUserTemplate.Id);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithIncludePublicFalse_ReturnsOnlyUserTemplates()
    {
        // Arrange
        var userTemplate = await CreateTestTemplateAsync(TestUserId, "My Template");
        var otherUserTemplate = await CreateTestTemplateAsync(OtherUserId, "Other Template");
        otherUserTemplate.IsPublic = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTemplatesAsync(TestUserId, includePublic: false);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(t => t.Id == userTemplate.Id);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WhenOwner_UpdatesTemplate()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(TestUserId, "Original Name");
        template.Name = "Updated Name";
        template.Description = "Updated Description";

        // Act
        var result = await _service.UpdateTemplateAsync(template, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteTemplateAsync_WhenOwner_DeletesTemplate()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(TestUserId, "Template to Delete");

        // Act
        var result = await _service.DeleteTemplateAsync(template.Id, TestUserId);

        // Assert
        result.Should().BeTrue();
        var deletedTemplate = await _context.SetlistTemplates.FindAsync(template.Id);
        deletedTemplate.Should().BeNull();
    }

    [Fact]
    public async Task ConvertTemplateToSetlistAsync_WithValidTemplate_CreatesSetlist()
    {
        // Arrange
        var songs = CreateTestSongs(TestUserId, 3);
        await _context.Songs.AddRangeAsync(songs);
        await _context.SaveChangesAsync();

        var template = new SetlistTemplate
        {
            Name = "Template to Convert",
            UserId = TestUserId,
            EstimatedDurationMinutes = 120
        };
        await _context.SetlistTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        foreach (var (song, index) in songs.Select((s, i) => (s, i)))
        {
            await _context.SetlistTemplateSongs.AddAsync(new SetlistTemplateSong
            {
                SetlistTemplateId = template.Id,
                SongId = song.Id,
                Position = index + 1,
                Notes = $"Note {index + 1}"
            });
        }
        await _context.SaveChangesAsync();

        var performanceDate = DateTime.UtcNow.AddDays(7);
        var venue = "Test Venue";

        // Act
        var result = await _service.ConvertTemplateToSetlistAsync(
            template.Id, performanceDate, venue, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Template to Convert");
        result.PerformanceDate.Should().Be(performanceDate);
        result.Venue.Should().Be(venue);
        result.UserId.Should().Be(TestUserId);
        result.SourceTemplateId.Should().Be(template.Id);
        result.SetlistSongs.Should().HaveCount(3);
    }

    #endregion

    #region SECURE Principle Tests - Authorization & Security

    [Fact]
    public async Task CreateTemplateAsync_WithUnauthorizedSongs_ThrowsException()
    {
        // Arrange
        var songs = CreateTestSongs(OtherUserId, 2);
        await _context.Songs.AddRangeAsync(songs);
        await _context.SaveChangesAsync();

        var template = new SetlistTemplate
        {
            Name = "Unauthorized Template"
        };

        var songIds = songs.Select(s => s.Id).ToArray();

        // Act
        Func<Task> act = async () => await _service.CreateTemplateAsync(template, songIds, TestUserId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Cannot add songs you don't own");
    }

    [Fact]
    public async Task GetTemplateByIdAsync_WhenPrivateAndNotOwner_ReturnsNull()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(OtherUserId, "Private Template");
        template.IsPublic = false;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTemplateByIdAsync(template.Id, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTemplateAsync_WhenNotOwner_ReturnsNull()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(OtherUserId, "Other's Template");
        template.Name = "Attempted Update";

        // Act
        var result = await _service.UpdateTemplateAsync(template, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTemplateAsync_WhenNotOwner_ReturnsFalse()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(OtherUserId, "Other's Template");

        // Act
        var result = await _service.DeleteTemplateAsync(template.Id, TestUserId);

        // Assert
        result.Should().BeFalse();
        var existingTemplate = await _context.SetlistTemplates.FindAsync(template.Id);
        existingTemplate.Should().NotBeNull();
    }

    [Fact]
    public async Task ConvertTemplateToSetlistAsync_UsesCurrentUserSongs_NotTemplateOwnerSongs()
    {
        // Arrange: Template owner has songs
        var ownerSongs = new[]
        {
            new Song { Title = "Song A", Artist = "Artist 1", UserId = OtherUserId },
            new Song { Title = "Song B", Artist = "Artist 2", UserId = OtherUserId }
        };
        await _context.Songs.AddRangeAsync(ownerSongs);
        await _context.SaveChangesAsync();

        var template = new SetlistTemplate
        {
            Name = "Public Template",
            UserId = OtherUserId,
            IsPublic = true
        };
        await _context.SetlistTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        foreach (var (song, index) in ownerSongs.Select((s, i) => (s, i)))
        {
            await _context.SetlistTemplateSongs.AddAsync(new SetlistTemplateSong
            {
                SetlistTemplateId = template.Id,
                SongId = song.Id,
                Position = index + 1
            });
        }
        await _context.SaveChangesAsync();

        // Arrange: Current user has same songs
        var userSongs = new[]
        {
            new Song { Title = "Song A", Artist = "Artist 1", UserId = TestUserId },
            new Song { Title = "Song B", Artist = "Artist 2", UserId = TestUserId }
        };
        await _context.Songs.AddRangeAsync(userSongs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ConvertTemplateToSetlistAsync(
            template.Id, DateTime.UtcNow.AddDays(1), "Test Venue", TestUserId);

        // Assert
        result.SetlistSongs.Should().HaveCount(2);
        result.SetlistSongs.Should().OnlyContain(s => userSongs.Any(us => us.Id == s.SongId));
        result.SetlistSongs.Should().NotContain(s => ownerSongs.Any(os => os.Id == s.SongId));
    }

    [Fact]
    public async Task ConvertTemplateToSetlistAsync_WhenTemplateNotFound_ThrowsException()
    {
        // Act
        Func<Task> act = async () => await _service.ConvertTemplateToSetlistAsync(
            9999, DateTime.UtcNow, "Venue", TestUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Template not found or access denied");
    }

    [Fact]
    public async Task ConvertTemplateToSetlistAsync_WhenPrivateTemplateNotOwned_ThrowsException()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(OtherUserId, "Private Template");
        template.IsPublic = false;
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.ConvertTemplateToSetlistAsync(
            template.Id, DateTime.UtcNow, "Venue", TestUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Template not found or access denied");
    }

    #endregion

    #region SCALE Principle Tests - Performance & Pagination

    [Fact]
    public async Task GetPublicTemplatesAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange: Create 25 public templates
        for (int i = 1; i <= 25; i++)
        {
            var template = await CreateTestTemplateAsync(OtherUserId, $"Public Template {i}");
            template.IsPublic = true;
        }
        await _context.SaveChangesAsync();

        // Act: Get page 2 with 10 items per page
        var result = await _service.GetPublicTemplatesAsync(category: null, pageNumber: 2, pageSize: 10);

        // Assert
        result.Templates.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task GetPublicTemplatesAsync_WithCategoryFilter_ReturnsFilteredResults()
    {
        // Arrange
        var weddingTemplate = await CreateTestTemplateAsync(OtherUserId, "Wedding Template");
        weddingTemplate.IsPublic = true;
        weddingTemplate.Category = "Wedding";

        var barTemplate = await CreateTestTemplateAsync(OtherUserId, "Bar Template");
        barTemplate.IsPublic = true;
        barTemplate.Category = "Bar Gig";

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPublicTemplatesAsync(category: "Wedding", pageNumber: 1, pageSize: 20);

        // Assert
        result.Templates.Should().HaveCount(1);
        result.Templates.First().Name.Should().Be("Wedding Template");
    }

    #endregion

    #region MAINTAINABLE Principle Tests - Audit & Tracking

    [Fact]
    public async Task CreateTemplateAsync_SetsAuditFields_Correctly()
    {
        // Arrange
        var songs = CreateTestSongs(TestUserId, 1);
        await _context.Songs.AddRangeAsync(songs);
        await _context.SaveChangesAsync();

        var template = new SetlistTemplate
        {
            Name = "Audit Test Template"
        };

        // Act
        var result = await _service.CreateTemplateAsync(template, songs.Select(s => s.Id), TestUserId);

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task ConvertTemplateToSetlistAsync_IncrementsUsageCount()
    {
        // Arrange
        var songs = CreateTestSongs(TestUserId, 2);
        await _context.Songs.AddRangeAsync(songs);
        await _context.SaveChangesAsync();

        var template = new SetlistTemplate
        {
            Name = "Usage Counter Template",
            UserId = TestUserId,
            UsageCount = 0
        };
        await _context.SetlistTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        foreach (var (song, index) in songs.Select((s, i) => (s, i)))
        {
            await _context.SetlistTemplateSongs.AddAsync(new SetlistTemplateSong
            {
                SetlistTemplateId = template.Id,
                SongId = song.Id,
                Position = index + 1
            });
        }
        await _context.SaveChangesAsync();

        // Act
        await _service.ConvertTemplateToSetlistAsync(template.Id, DateTime.UtcNow, "Venue", TestUserId);

        // Assert
        var updatedTemplate = await _context.SetlistTemplates.FindAsync(template.Id);
        updatedTemplate!.UsageCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateTemplateAsync_UpdatesAuditTimestamp()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(TestUserId, "Original Template");
        var originalUpdateTime = template.UpdatedAt;
        await Task.Delay(100); // Ensure time difference

        template.Name = "Updated Template";

        // Act
        var result = await _service.UpdateTemplateAsync(template, TestUserId);

        // Assert
        result!.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }

    #endregion

    #region USER DELIGHT Principle Tests - Analytics & UX

    [Fact]
    public async Task GetTemplateStatisticsAsync_ReturnsAccurateMetrics()
    {
        // Arrange
        var songs = CreateTestSongs(TestUserId, 5);
        await _context.Songs.AddRangeAsync(songs);
        await _context.SaveChangesAsync();

        var template = new SetlistTemplate
        {
            Name = "Statistics Template",
            UserId = TestUserId,
            EstimatedDurationMinutes = 150,
            UsageCount = 3
        };
        await _context.SetlistTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        foreach (var (song, index) in songs.Select((s, i) => (s, i)))
        {
            await _context.SetlistTemplateSongs.AddAsync(new SetlistTemplateSong
            {
                SetlistTemplateId = template.Id,
                SongId = song.Id,
                Position = index + 1
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTemplateStatisticsAsync(template.Id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.UsageCount.Should().Be(3);
        result.TotalSongs.Should().Be(5);
        result.EstimatedDurationMinutes.Should().Be(150);
    }

    [Fact]
    public async Task ConvertTemplateToSetlistAsync_PreservesNotesFromTemplate()
    {
        // Arrange
        var songs = CreateTestSongs(TestUserId, 2);
        await _context.Songs.AddRangeAsync(songs);
        await _context.SaveChangesAsync();

        var template = new SetlistTemplate
        {
            Name = "Notes Test Template",
            UserId = TestUserId
        };
        await _context.SetlistTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        await _context.SetlistTemplateSongs.AddAsync(new SetlistTemplateSong
        {
            SetlistTemplateId = template.Id,
            SongId = songs[0].Id,
            Position = 1,
            Notes = "Acoustic version"
        });
        await _context.SetlistTemplateSongs.AddAsync(new SetlistTemplateSong
        {
            SetlistTemplateId = template.Id,
            SongId = songs[1].Id,
            Position = 2,
            Notes = "Extended solo"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ConvertTemplateToSetlistAsync(
            template.Id, DateTime.UtcNow, "Venue", TestUserId);

        // Assert
        result.SetlistSongs.Should().HaveCount(2);
        result.SetlistSongs.First(s => s.Position == 1).PerformanceNotes.Should().Be("Acoustic version");
        result.SetlistSongs.First(s => s.Position == 2).PerformanceNotes.Should().Be("Extended solo");
    }

    [Fact]
    public async Task ConvertTemplateToSetlistAsync_WhenSongMissing_ContinuesWithAvailableSongs()
    {
        // Arrange: Template owner has 3 songs
        var ownerSongs = new[]
        {
            new Song { Title = "Song A", Artist = "Artist 1", UserId = OtherUserId },
            new Song { Title = "Song B", Artist = "Artist 2", UserId = OtherUserId },
            new Song { Title = "Song C", Artist = "Artist 3", UserId = OtherUserId }
        };
        await _context.Songs.AddRangeAsync(ownerSongs);
        await _context.SaveChangesAsync();

        var template = new SetlistTemplate
        {
            Name = "Public Template with Missing Songs",
            UserId = OtherUserId,
            IsPublic = true
        };
        await _context.SetlistTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        foreach (var (song, index) in ownerSongs.Select((s, i) => (s, i)))
        {
            await _context.SetlistTemplateSongs.AddAsync(new SetlistTemplateSong
            {
                SetlistTemplateId = template.Id,
                SongId = song.Id,
                Position = index + 1
            });
        }
        await _context.SaveChangesAsync();

        // Current user only has 2 of the 3 songs
        var userSongs = new[]
        {
            new Song { Title = "Song A", Artist = "Artist 1", UserId = TestUserId },
            new Song { Title = "Song C", Artist = "Artist 3", UserId = TestUserId }
        };
        await _context.Songs.AddRangeAsync(userSongs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ConvertTemplateToSetlistAsync(
            template.Id, DateTime.UtcNow, "Venue", TestUserId);

        // Assert
        result.SetlistSongs.Should().HaveCount(2); // Only found 2 matching songs
        result.SetlistSongs.Should().Contain(s => s.Song!.Title == "Song A");
        result.SetlistSongs.Should().Contain(s => s.Song!.Title == "Song C");
    }

    [Fact]
    public async Task SetTemplateVisibilityAsync_ChangesPublicFlag()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(TestUserId, "Visibility Test");
        template.IsPublic = false;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SetTemplateVisibilityAsync(template.Id, true, TestUserId);

        // Assert
        result.Should().BeTrue();
        var updatedTemplate = await _context.SetlistTemplates.FindAsync(template.Id);
        updatedTemplate!.IsPublic.Should().BeTrue();
    }

    [Fact]
    public async Task SetTemplateVisibilityAsync_WhenNotOwner_ReturnsFalse()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(OtherUserId, "Other's Template");

        // Act
        var result = await _service.SetTemplateVisibilityAsync(template.Id, true, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Song Management Tests

    [Fact]
    public async Task AddSongToTemplateAsync_WithValidSong_AddsSong()
    {
        // Arrange
        var template = await CreateTestTemplateAsync(TestUserId, "Song Management Template");
        var song = new Song { Title = "New Song", Artist = "New Artist", UserId = TestUserId };
        await _context.Songs.AddAsync(song);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AddSongToTemplateAsync(template.Id, song.Id, 1, TestUserId);

        // Assert
        result.Should().BeTrue();
        var templateSongs = await _context.SetlistTemplateSongs
            .Where(ts => ts.SetlistTemplateId == template.Id)
            .ToListAsync();
        templateSongs.Should().HaveCount(1);
        templateSongs.First().SongId.Should().Be(song.Id);
    }

    [Fact]
    public async Task RemoveSongFromTemplateAsync_WithExistingSong_RemovesSong()
    {
        // Arrange
        var songs = CreateTestSongs(TestUserId, 2);
        await _context.Songs.AddRangeAsync(songs);
        await _context.SaveChangesAsync();

        var template = new SetlistTemplate
        {
            Name = "Remove Song Template",
            UserId = TestUserId
        };
        await _context.SetlistTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        await _context.SetlistTemplateSongs.AddAsync(new SetlistTemplateSong
        {
            SetlistTemplateId = template.Id,
            SongId = songs[0].Id,
            Position = 1
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RemoveSongFromTemplateAsync(template.Id, songs[0].Id, TestUserId);

        // Assert
        result.Should().BeTrue();
        var templateSongs = await _context.SetlistTemplateSongs
            .Where(ts => ts.SetlistTemplateId == template.Id)
            .ToListAsync();
        templateSongs.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private List<Song> CreateTestSongs(string userId, int count)
    {
        var songs = new List<Song>();
        for (int i = 1; i <= count; i++)
        {
            songs.Add(new Song
            {
                Title = $"Test Song {i}",
                Artist = $"Test Artist {i}",
                Album = $"Test Album {i}",
                Bpm = 120 + i,
                MusicalKey = "C",
                Genre = "Rock",
                UserId = userId
            });
        }
        return songs;
    }

    private async Task<SetlistTemplate> CreateTestTemplateAsync(string userId, string name)
    {
        var template = new SetlistTemplate
        {
            Name = name,
            Description = "Test description",
            Category = "Test",
            UserId = userId,
            EstimatedDurationMinutes = 60,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.SetlistTemplates.AddAsync(template);
        await _context.SaveChangesAsync();

        return template;
    }

    #endregion
}
