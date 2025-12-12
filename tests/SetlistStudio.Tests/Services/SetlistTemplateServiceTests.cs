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
/// Comprehensive tests for SetlistTemplateService
/// Following TDD approach for Challenge 5: Build Setlist Templates
/// Target: >80% line and branch coverage
/// </summary>
public class SetlistTemplateServiceTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly ISetlistTemplateService _service;
    private readonly ISetlistService _setlistService;
    private readonly Mock<ILogger<SetlistService>> _mockLogger;
    private readonly Mock<IQueryCacheService> _mockCacheService;
    private const string TestUserId = "test-user-123";
    private const string OtherUserId = "other-user-456";

    public SetlistTemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
        _mockLogger = new Mock<ILogger<SetlistService>>();
        _mockCacheService = new Mock<IQueryCacheService>();
        _setlistService = new SetlistService(_context, _mockLogger.Object, _mockCacheService.Object);
        _service = new SetlistTemplateService(_context, _setlistService);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Create Template Tests

    [Fact(DisplayName = "CreateTemplateAsync: Creates template with valid data")]
    public async Task CreateTemplateAsync_WithValidData_CreatesTemplate()
    {
        // Arrange
        var template = new SetlistTemplate
        {
            Name = "Wedding Ceremony Set",
            Description = "Romantic songs for ceremony entrance and signing",
            Category = "Wedding",
            EstimatedDurationMinutes = 45,
            UserId = TestUserId
        };

        // Act
        var result = await _service.CreateTemplateAsync(template, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Wedding Ceremony Set");
        result.Description.Should().Be("Romantic songs for ceremony entrance and signing");
        result.Category.Should().Be("Wedding");
        result.EstimatedDurationMinutes.Should().Be(45);
        result.UserId.Should().Be(TestUserId);
        result.IsPublic.Should().BeFalse(); // Default
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "CreateTemplateAsync: Sets CreatedAt to current time")]
    public async Task CreateTemplateAsync_SetsCreatedAtToCurrentTime()
    {
        // Arrange
        var template = new SetlistTemplate
        {
            Name = "Rock Bar Night",
            UserId = TestUserId
        };

        // Act
        var result = await _service.CreateTemplateAsync(template, TestUserId);

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "CreateTemplateAsync: Throws ArgumentNullException for null template")]
    public async Task CreateTemplateAsync_WithNullTemplate_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.CreateTemplateAsync(null!, TestUserId));
    }

    #endregion

    #region Get Templates Tests

    [Fact(DisplayName = "GetTemplatesAsync: Returns all templates for user")]
    public async Task GetTemplatesAsync_ReturnsAllTemplatesForUser()
    {
        // Arrange
        var template1 = await CreateTestTemplate("Template 1", TestUserId, "Wedding");
        var template2 = await CreateTestTemplate("Template 2", TestUserId, "Concert");
        var templateOtherUser = await CreateTestTemplate("Other User Template", OtherUserId);

        // Act
        var (templates, totalCount) = await _service.GetTemplatesAsync(TestUserId);

        // Assert
        templates.Should().HaveCount(2);
        totalCount.Should().Be(2);
        templates.Should().Contain(t => t.Id == template1.Id);
        templates.Should().Contain(t => t.Id == template2.Id);
        templates.Should().NotContain(t => t.Id == templateOtherUser.Id);
    }

    [Fact(DisplayName = "GetTemplatesAsync: Filters by category")]
    public async Task GetTemplatesAsync_WithCategory_FiltersCorrectly()
    {
        // Arrange
        await CreateTestTemplate("Wedding Template 1", TestUserId, "Wedding");
        await CreateTestTemplate("Wedding Template 2", TestUserId, "Wedding");
        await CreateTestTemplate("Rock Template", TestUserId, "Rock Bar");

        // Act
        var (templates, totalCount) = await _service.GetTemplatesAsync(TestUserId, category: "Wedding");

        // Assert
        templates.Should().HaveCount(2);
        totalCount.Should().Be(2);
        templates.Should().OnlyContain(t => t.Category == "Wedding");
    }

    [Fact(DisplayName = "GetTemplatesAsync: Supports pagination")]
    public async Task GetTemplatesAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            await CreateTestTemplate($"Template {i}", TestUserId);
        }

        // Act
        var (templates, totalCount) = await _service.GetTemplatesAsync(TestUserId, pageNumber: 2, pageSize: 10);

        // Assert
        templates.Should().HaveCount(10);
        totalCount.Should().Be(25);
    }

    [Fact(DisplayName = "GetTemplatesAsync: Returns empty for user with no templates")]
    public async Task GetTemplatesAsync_WithNoTemplates_ReturnsEmpty()
    {
        // Act
        var (templates, totalCount) = await _service.GetTemplatesAsync(TestUserId);

        // Assert
        templates.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    #endregion

    #region Get Template By ID Tests

    [Fact(DisplayName = "GetTemplateByIdAsync: Returns template with songs")]
    public async Task GetTemplateByIdAsync_WithValidId_ReturnsTemplateWithSongs()
    {
        // Arrange
        var template = await CreateTestTemplate("Test Template", TestUserId);
        var song1 = await CreateTestSong("Song 1", TestUserId);
        var song2 = await CreateTestSong("Song 2", TestUserId);
        
        await _service.AddSongToTemplateAsync(template.Id, song1.Id, 1, TestUserId);
        await _service.AddSongToTemplateAsync(template.Id, song2.Id, 2, TestUserId);

        // Act
        var result = await _service.GetTemplateByIdAsync(template.Id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.TemplateSongs.Should().HaveCount(2);
        result.TemplateSongs.Should().BeInAscendingOrder(ts => ts.Position);
    }

    [Fact(DisplayName = "GetTemplateByIdAsync: Returns null for non-existent template")]
    public async Task GetTemplateByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetTemplateByIdAsync(9999, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "GetTemplateByIdAsync: Returns null for other user's template")]
    public async Task GetTemplateByIdAsync_WithOtherUsersTemplate_ReturnsNull()
    {
        // Arrange: Create template for User A
        var templateUserA = await CreateTestTemplate("User A Template", OtherUserId);

        // Act: User B tries to access User A's template
        var result = await _service.GetTemplateByIdAsync(templateUserA.Id, TestUserId);

        // Assert: Access denied
        result.Should().BeNull();
    }

    #endregion

    #region Update Template Tests

    [Fact(DisplayName = "UpdateTemplateAsync: Updates template properties")]
    public async Task UpdateTemplateAsync_WithValidData_UpdatesTemplate()
    {
        // Arrange
        var template = await CreateTestTemplate("Original Name", TestUserId, "Wedding");
        var updatedTemplate = new SetlistTemplate
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Category = "Concert",
            EstimatedDurationMinutes = 90
        };

        // Act
        var result = await _service.UpdateTemplateAsync(template.Id, updatedTemplate, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
        result.Category.Should().Be("Concert");
        result.EstimatedDurationMinutes.Should().Be(90);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "UpdateTemplateAsync: Returns null for other user's template")]
    public async Task UpdateTemplateAsync_WithOtherUsersTemplate_ReturnsNull()
    {
        // Arrange
        var templateUserA = await CreateTestTemplate("User A Template", OtherUserId);
        var updatedTemplate = new SetlistTemplate { Name = "Hacked Name" };

        // Act
        var result = await _service.UpdateTemplateAsync(templateUserA.Id, updatedTemplate, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Delete Template Tests

    [Fact(DisplayName = "DeleteTemplateAsync: Deletes template successfully")]
    public async Task DeleteTemplateAsync_WithValidId_DeletesTemplate()
    {
        // Arrange
        var template = await CreateTestTemplate("To Delete", TestUserId);

        // Act
        var result = await _service.DeleteTemplateAsync(template.Id, TestUserId);

        // Assert
        result.Should().BeTrue();
        var deleted = await _context.SetlistTemplates.FindAsync(template.Id);
        deleted.Should().BeNull();
    }

    [Fact(DisplayName = "DeleteTemplateAsync: Returns false for other user's template")]
    public async Task DeleteTemplateAsync_WithOtherUsersTemplate_ReturnsFalse()
    {
        // Arrange
        var templateUserA = await CreateTestTemplate("User A Template", OtherUserId);

        // Act
        var result = await _service.DeleteTemplateAsync(templateUserA.Id, TestUserId);

        // Assert
        result.Should().BeFalse();
        var stillExists = await _context.SetlistTemplates.FindAsync(templateUserA.Id);
        stillExists.Should().NotBeNull();
    }

    [Fact(DisplayName = "DeleteTemplateAsync: Returns false for non-existent template")]
    public async Task DeleteTemplateAsync_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteTemplateAsync(9999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Add Song to Template Tests

    [Fact(DisplayName = "AddSongToTemplateAsync: Adds song to template")]
    public async Task AddSongToTemplateAsync_WithValidData_AddsSong()
    {
        // Arrange
        var template = await CreateTestTemplate("Test Template", TestUserId);
        var song = await CreateTestSong("Test Song", TestUserId);

        // Act
        var result = await _service.AddSongToTemplateAsync(template.Id, song.Id, 1, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.TemplateSongs.Should().HaveCount(1);
        result.TemplateSongs.First().SongId.Should().Be(song.Id);
        result.TemplateSongs.First().Position.Should().Be(1);
    }

    [Fact(DisplayName = "AddSongToTemplateAsync: Maintains song order")]
    public async Task AddSongToTemplateAsync_WithMultipleSongs_MaintainsOrder()
    {
        // Arrange
        var template = await CreateTestTemplate("Test Template", TestUserId);
        var song1 = await CreateTestSong("Song 1", TestUserId);
        var song2 = await CreateTestSong("Song 2", TestUserId);
        var song3 = await CreateTestSong("Song 3", TestUserId);

        // Act
        await _service.AddSongToTemplateAsync(template.Id, song1.Id, 1, TestUserId);
        await _service.AddSongToTemplateAsync(template.Id, song2.Id, 2, TestUserId);
        var result = await _service.AddSongToTemplateAsync(template.Id, song3.Id, 3, TestUserId);

        // Assert
        result!.TemplateSongs.Should().HaveCount(3);
        result.TemplateSongs.Should().BeInAscendingOrder(ts => ts.Position);
    }

    [Fact(DisplayName = "AddSongToTemplateAsync: Returns null for other user's template")]
    public async Task AddSongToTemplateAsync_WithOtherUsersTemplate_ReturnsNull()
    {
        // Arrange
        var templateUserA = await CreateTestTemplate("User A Template", OtherUserId);
        var song = await CreateTestSong("Test Song", TestUserId);

        // Act
        var result = await _service.AddSongToTemplateAsync(templateUserA.Id, song.Id, 1, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Remove Song from Template Tests

    [Fact(DisplayName = "RemoveSongFromTemplateAsync: Removes song from template")]
    public async Task RemoveSongFromTemplateAsync_WithValidData_RemovesSong()
    {
        // Arrange
        var template = await CreateTestTemplate("Test Template", TestUserId);
        var song = await CreateTestSong("Test Song", TestUserId);
        await _service.AddSongToTemplateAsync(template.Id, song.Id, 1, TestUserId);

        // Act
        var result = await _service.RemoveSongFromTemplateAsync(template.Id, song.Id, TestUserId);

        // Assert
        result.Should().BeTrue();
        var updated = await _service.GetTemplateByIdAsync(template.Id, TestUserId);
        updated!.TemplateSongs.Should().BeEmpty();
    }

    [Fact(DisplayName = "RemoveSongFromTemplateAsync: Returns false for other user's template")]
    public async Task RemoveSongFromTemplateAsync_WithOtherUsersTemplate_ReturnsFalse()
    {
        // Arrange
        var templateUserA = await CreateTestTemplate("User A Template", OtherUserId);
        var song = await CreateTestSong("Test Song", OtherUserId);
        await _service.AddSongToTemplateAsync(templateUserA.Id, song.Id, 1, OtherUserId);

        // Act
        var result = await _service.RemoveSongFromTemplateAsync(templateUserA.Id, song.Id, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Reorder Template Songs Tests

    [Fact(DisplayName = "ReorderTemplateSongsAsync: Reorders songs correctly")]
    public async Task ReorderTemplateSongsAsync_WithValidOrder_ReordersSongs()
    {
        // Arrange
        var template = await CreateTestTemplate("Test Template", TestUserId);
        var song1 = await CreateTestSong("Song 1", TestUserId);
        var song2 = await CreateTestSong("Song 2", TestUserId);
        var song3 = await CreateTestSong("Song 3", TestUserId);
        
        await _service.AddSongToTemplateAsync(template.Id, song1.Id, 1, TestUserId);
        await _service.AddSongToTemplateAsync(template.Id, song2.Id, 2, TestUserId);
        await _service.AddSongToTemplateAsync(template.Id, song3.Id, 3, TestUserId);

        // Act: Reverse order
        var result = await _service.ReorderTemplateSongsAsync(template.Id, 
            new List<int> { song3.Id, song2.Id, song1.Id }, TestUserId);

        // Assert
        result.Should().NotBeNull();
        var songs = result!.TemplateSongs.OrderBy(ts => ts.Position).Select(ts => ts.SongId).ToList();
        songs.Should().ContainInOrder(song3.Id, song2.Id, song1.Id);
    }

    [Fact(DisplayName = "ReorderTemplateSongsAsync: Returns null for other user's template")]
    public async Task ReorderTemplateSongsAsync_WithOtherUsersTemplate_ReturnsNull()
    {
        // Arrange
        var templateUserA = await CreateTestTemplate("User A Template", OtherUserId);

        // Act
        var result = await _service.ReorderTemplateSongsAsync(templateUserA.Id, new List<int>(), TestUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Convert Template to Setlist Tests

    [Fact(DisplayName = "ConvertTemplateToSetlistAsync: Creates setlist from template")]
    public async Task ConvertTemplateToSetlistAsync_WithValidTemplate_CreatesSetlist()
    {
        // Arrange
        var template = await CreateTestTemplate("Wedding Template", TestUserId);
        var song1 = await CreateTestSong("Song 1", TestUserId);
        var song2 = await CreateTestSong("Song 2", TestUserId);
        await _service.AddSongToTemplateAsync(template.Id, song1.Id, 1, TestUserId);
        await _service.AddSongToTemplateAsync(template.Id, song2.Id, 2, TestUserId);

        var performanceDate = DateTime.UtcNow.AddDays(7);

        // Act
        var result = await _service.ConvertTemplateToSetlistAsync(
            template.Id, "Smith Wedding", performanceDate, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Smith Wedding");
        result.PerformanceDate.Should().Be(performanceDate);
        result.UserId.Should().Be(TestUserId);
        result.SetlistSongs.Should().HaveCount(2);
    }

    [Fact(DisplayName = "ConvertTemplateToSetlistAsync: Preserves song order")]
    public async Task ConvertTemplateToSetlistAsync_PreservesSongOrder()
    {
        // Arrange
        var template = await CreateTestTemplate("Test Template", TestUserId);
        var song1 = await CreateTestSong("Song 1", TestUserId);
        var song2 = await CreateTestSong("Song 2", TestUserId);
        var song3 = await CreateTestSong("Song 3", TestUserId);
        
        await _service.AddSongToTemplateAsync(template.Id, song1.Id, 1, TestUserId);
        await _service.AddSongToTemplateAsync(template.Id, song2.Id, 2, TestUserId);
        await _service.AddSongToTemplateAsync(template.Id, song3.Id, 3, TestUserId);

        // Act
        var result = await _service.ConvertTemplateToSetlistAsync(
            template.Id, "Test Setlist", null, TestUserId);

        // Assert
        var songIds = result.SetlistSongs.OrderBy(s => s.Position).Select(s => s.SongId).ToList();
        songIds.Should().ContainInOrder(song1.Id, song2.Id, song3.Id);
    }

    [Fact(DisplayName = "ConvertTemplateToSetlistAsync: Throws UnauthorizedAccessException for other user's template")]
    public async Task ConvertTemplateToSetlistAsync_WithOtherUsersTemplate_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var templateUserA = await CreateTestTemplate("User A Template", OtherUserId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _service.ConvertTemplateToSetlistAsync(templateUserA.Id, "Test", null, TestUserId));
    }

    [Fact(DisplayName = "ConvertTemplateToSetlistAsync: Throws UnauthorizedAccessException for non-existent template")]
    public async Task ConvertTemplateToSetlistAsync_WithNonExistentTemplate_ThrowsUnauthorizedAccessException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _service.ConvertTemplateToSetlistAsync(9999, "Test", null, TestUserId));
    }

    #endregion

    #region Get Categories Tests

    [Fact(DisplayName = "GetCategoriesAsync: Returns unique categories")]
    public async Task GetCategoriesAsync_ReturnsUniqueCategories()
    {
        // Arrange
        await CreateTestTemplate("Template 1", TestUserId, "Wedding");
        await CreateTestTemplate("Template 2", TestUserId, "Wedding");
        await CreateTestTemplate("Template 3", TestUserId, "Concert");
        await CreateTestTemplate("Template 4", TestUserId, "Bar Gig");

        // Act
        var result = await _service.GetCategoriesAsync(TestUserId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Wedding");
        result.Should().Contain("Concert");
        result.Should().Contain("Bar Gig");
    }

    [Fact(DisplayName = "GetCategoriesAsync: Returns empty for user with no templates")]
    public async Task GetCategoriesAsync_WithNoTemplates_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetCategoriesAsync(TestUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "GetCategoriesAsync: Excludes null categories")]
    public async Task GetCategoriesAsync_ExcludesNullCategories()
    {
        // Arrange
        await CreateTestTemplate("Template 1", TestUserId, "Wedding");
        await CreateTestTemplate("Template 2", TestUserId, null);

        // Act
        var result = await _service.GetCategoriesAsync(TestUserId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("Wedding");
    }

    #endregion

    #region Helper Methods

    private async Task<SetlistTemplate> CreateTestTemplate(string name, string userId, string? category = null)
    {
        var template = new SetlistTemplate
        {
            Name = name,
            Category = category,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.SetlistTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    private async Task<Song> CreateTestSong(string title, string userId)
    {
        var song = new Song
        {
            Title = title,
            Artist = "Test Artist",
            UserId = userId
        };
        
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();
        return song;
    }

    #endregion
}
