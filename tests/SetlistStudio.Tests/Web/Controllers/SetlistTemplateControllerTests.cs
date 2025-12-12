using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Web.Controllers;
using SetlistStudio.Web.Models;
using Xunit;

namespace SetlistStudio.Tests.Web.Controllers;

/// <summary>
/// Tests for SetlistTemplateController
/// Covers CRUD operations, song management, template conversion, and statistics
/// </summary>
public class SetlistTemplateControllerTests
{
    private readonly Mock<ISetlistTemplateService> _mockTemplateService;
    private readonly Mock<ISongService> _mockSongService;
    private readonly Mock<ISetlistService> _mockSetlistService;
    private readonly Mock<ILogger<SetlistTemplateController>> _mockLogger;
    private readonly SetlistTemplateController _controller;
    private readonly string _testUserId = "test-user-id";

    public SetlistTemplateControllerTests()
    {
        _mockTemplateService = new Mock<ISetlistTemplateService>();
        _mockSongService = new Mock<ISongService>();
        _mockSetlistService = new Mock<ISetlistService>();
        _mockLogger = new Mock<ILogger<SetlistTemplateController>>();

        _controller = new SetlistTemplateController(
            _mockTemplateService.Object,
            _mockSongService.Object,
            _mockSetlistService.Object,
            _mockLogger.Object);

        // Setup controller context with authenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, _testUserId)
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetTemplates_ReturnsOkResult_WithPaginatedTemplates()
    {
        // Arrange
        var templates = new List<SetlistTemplate>
        {
            CreateTemplate(1, "Rock Template"),
            CreateTemplate(2, "Jazz Template")
        };

        _mockTemplateService
            .Setup(s => s.GetTemplatesAsync(_testUserId, false, null))
            .ReturnsAsync(templates);

        // Act
        var result = await _controller.GetTemplates();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TemplateListResponse>().Subject;
        response.Templates.Should().HaveCount(2);
        response.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTemplates_WithCategoryFilter_ReturnsFilteredTemplates()
    {
        // Arrange
        var templates = new List<SetlistTemplate>
        {
            CreateTemplate(1, "Rock Template", "Rock")
        };

        _mockTemplateService
            .Setup(s => s.GetTemplatesAsync(_testUserId, false, "Rock"))
            .ReturnsAsync(templates);

        // Act
        var result = await _controller.GetTemplates(category: "Rock");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TemplateListResponse>().Subject;
        response.Templates.Should().HaveCount(1);
        response.Templates[0].Category.Should().Be("Rock");
    }

    [Fact]
    public async Task GetTemplates_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var templates = Enumerable.Range(1, 50)
            .Select(i => CreateTemplate(i, $"Template {i}"))
            .ToList();

        _mockTemplateService
            .Setup(s => s.GetTemplatesAsync(_testUserId, false, null))
            .ReturnsAsync(templates);

        // Act
        var result = await _controller.GetTemplates(pageNumber: 2, pageSize: 20);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TemplateListResponse>().Subject;
        response.Templates.Should().HaveCount(20);
        response.PageNumber.Should().Be(2);
        response.TotalCount.Should().Be(50);
        response.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetPublicTemplates_ReturnsOkResult_WithPublicTemplates()
    {
        // Arrange
        var templates = new List<SetlistTemplate>
        {
            CreateTemplate(1, "Public Template 1", isPublic: true),
            CreateTemplate(2, "Public Template 2", isPublic: true)
        };

        _mockTemplateService
            .Setup(s => s.GetPublicTemplatesAsync(null, 1, 20))
            .ReturnsAsync((templates, 2));

        // Act
        var result = await _controller.GetPublicTemplates();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TemplateListResponse>().Subject;
        response.Templates.Should().HaveCount(2);
        response.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTemplate_WhenExists_ReturnsOkResult()
    {
        // Arrange
        var template = CreateTemplate(1, "Test Template");

        _mockTemplateService
            .Setup(s => s.GetTemplateByIdAsync(1, _testUserId))
            .ReturnsAsync(template);

        // Act
        var result = await _controller.GetTemplate(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<TemplateDto>().Subject;
        dto.Name.Should().Be("Test Template");
    }

    [Fact]
    public async Task GetTemplate_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockTemplateService
            .Setup(s => s.GetTemplateByIdAsync(999, _testUserId))
            .ReturnsAsync((SetlistTemplate?)null);

        // Act
        var result = await _controller.GetTemplate(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateTemplate_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = "New Template",
            Description = "Test description",
            Category = "Rock",
            EstimatedDurationMinutes = 60,
            IsPublic = false,
            SongIds = new List<int> { 1, 2, 3 }
        };

        var createdTemplate = CreateTemplate(1, request.Name);

        _mockTemplateService
            .Setup(s => s.CreateTemplateAsync(It.IsAny<SetlistTemplate>(), request.SongIds, _testUserId))
            .ReturnsAsync(createdTemplate);

        // Act
        var result = await _controller.CreateTemplate(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var dto = createdResult.Value.Should().BeOfType<TemplateDto>().Subject;
        dto.Name.Should().Be("New Template");
    }

    [Fact]
    public async Task CreateTemplate_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Name", "Name is required");

        var request = new CreateTemplateRequest();

        // Act
        var result = await _controller.CreateTemplate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateTemplate_WithUnauthorizedSongs_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = "Test Template",
            SongIds = new List<int> { 1, 2 }
        };

        _mockTemplateService
            .Setup(s => s.CreateTemplateAsync(It.IsAny<SetlistTemplate>(), request.SongIds, _testUserId))
            .ThrowsAsync(new UnauthorizedAccessException("Cannot add songs you don't own"));

        // Act
        var result = await _controller.CreateTemplate(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateTemplate_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var existing = CreateTemplate(1, "Old Name");
        var request = new UpdateTemplateRequest
        {
            Name = "Updated Name",
            Description = "Updated description",
            Category = "Jazz",
            EstimatedDurationMinutes = 90
        };

        _mockTemplateService
            .Setup(s => s.GetTemplateByIdAsync(1, _testUserId))
            .ReturnsAsync(existing);

        _mockTemplateService
            .Setup(s => s.UpdateTemplateAsync(It.IsAny<SetlistTemplate>(), _testUserId))
            .ReturnsAsync(existing);

        // Act
        var result = await _controller.UpdateTemplate(1, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<TemplateDto>().Subject;
        dto.Id.Should().Be(1);
    }

    [Fact]
    public async Task UpdateTemplate_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateTemplateRequest { Name = "Test" };

        _mockTemplateService
            .Setup(s => s.GetTemplateByIdAsync(999, _testUserId))
            .ReturnsAsync((SetlistTemplate?)null);

        // Act
        var result = await _controller.UpdateTemplate(999, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateTemplate_WhenNotOwner_ReturnsNotFound()
    {
        // Arrange
        var template = CreateTemplate(1, "Test", userId: "other-user");
        var request = new UpdateTemplateRequest { Name = "Updated" };

        _mockTemplateService
            .Setup(s => s.GetTemplateByIdAsync(1, _testUserId))
            .ReturnsAsync(template);

        // Act
        var result = await _controller.UpdateTemplate(1, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteTemplate_WhenExists_ReturnsNoContent()
    {
        // Arrange
        _mockTemplateService
            .Setup(s => s.DeleteTemplateAsync(1, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteTemplate(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteTemplate_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockTemplateService
            .Setup(s => s.DeleteTemplateAsync(999, _testUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteTemplate(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddSongToTemplate_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        var request = new AddSongToTemplateRequest
        {
            SongId = 5,
            Position = 3,
            Notes = "Extended solo"
        };

        _mockTemplateService
            .Setup(s => s.AddSongToTemplateAsync(1, 5, 3, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.AddSongToTemplate(1, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AddSongToTemplate_WhenTemplateNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new AddSongToTemplateRequest
        {
            SongId = 5,
            Position = 1
        };

        _mockTemplateService
            .Setup(s => s.AddSongToTemplateAsync(999, 5, 1, _testUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.AddSongToTemplate(999, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddSongToTemplate_WithUnauthorizedSong_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddSongToTemplateRequest
        {
            SongId = 5,
            Position = 1
        };

        _mockTemplateService
            .Setup(s => s.AddSongToTemplateAsync(1, 5, 1, _testUserId))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.AddSongToTemplate(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RemoveSongFromTemplate_WhenExists_ReturnsNoContent()
    {
        // Arrange
        _mockTemplateService
            .Setup(s => s.RemoveSongFromTemplateAsync(1, 5, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RemoveSongFromTemplate(1, 5);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveSongFromTemplate_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockTemplateService
            .Setup(s => s.RemoveSongFromTemplateAsync(1, 999, _testUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.RemoveSongFromTemplate(1, 999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ReorderTemplateSongs_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        var request = new ReorderTemplateSongsRequest
        {
            SongIds = new List<int> { 3, 1, 2 }
        };

        _mockTemplateService
            .Setup(s => s.ReorderTemplateSongsAsync(1, request.SongIds, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReorderTemplateSongs(1, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ReorderTemplateSongs_WhenTemplateNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new ReorderTemplateSongsRequest
        {
            SongIds = new List<int> { 1, 2 }
        };

        _mockTemplateService
            .Setup(s => s.ReorderTemplateSongsAsync(999, request.SongIds, _testUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ReorderTemplateSongs(999, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetTemplateVisibility_ToPublic_ReturnsNoContent()
    {
        // Arrange
        _mockTemplateService
            .Setup(s => s.SetTemplateVisibilityAsync(1, true, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SetTemplateVisibility(1, true);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetTemplateVisibility_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockTemplateService
            .Setup(s => s.SetTemplateVisibilityAsync(999, true, _testUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SetTemplateVisibility(999, true);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ConvertTemplateToSetlist_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new ConvertTemplateToSetlistRequest
        {
            PerformanceDate = DateTime.UtcNow.AddDays(7),
            Venue = "Red Rocks Amphitheatre"
        };

        var setlist = new Setlist
        {
            Id = 100,
            Name = "Rock Template - Red Rocks",
            PerformanceDate = request.PerformanceDate,
            Venue = request.Venue,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SetlistSongs = new List<SetlistSong>
            {
                new SetlistSong
                {
                    Id = 1,
                    Position = 1,
                    Song = new Song
                    {
                        Id = 1,
                        Title = "Sweet Child O' Mine",
                        Artist = "Guns N' Roses",
                        UserId = _testUserId
                    }
                }
            }
        };

        _mockTemplateService
            .Setup(s => s.ConvertTemplateToSetlistAsync(1, request.PerformanceDate, request.Venue, _testUserId))
            .ReturnsAsync(setlist);

        // Act
        var result = await _controller.ConvertTemplateToSetlist(1, request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var dto = createdResult.Value.Should().BeOfType<SetlistDto>().Subject;
        dto.Venue.Should().Be("Red Rocks Amphitheatre");
    }

    [Fact]
    public async Task ConvertTemplateToSetlist_WhenTemplateNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new ConvertTemplateToSetlistRequest
        {
            PerformanceDate = DateTime.UtcNow.AddDays(1)
        };

        _mockTemplateService
            .Setup(s => s.ConvertTemplateToSetlistAsync(999, request.PerformanceDate, request.Venue, _testUserId))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.ConvertTemplateToSetlist(999, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTemplateStatistics_WhenExists_ReturnsStatistics()
    {
        // Arrange
        var template = CreateTemplate(1, "Rock Template");
        template.Songs = new List<SetlistTemplateSong>
        {
            new SetlistTemplateSong
            {
                Position = 1,
                Song = new Song
                {
                    Title = "Song 1",
                    Artist = "Artist 1",
                    Genre = "Rock",
                    MusicalKey = "E",
                    Bpm = 120,
                    UserId = _testUserId
                }
            },
            new SetlistTemplateSong
            {
                Position = 2,
                Song = new Song
                {
                    Title = "Song 2",
                    Artist = "Artist 2",
                    Genre = "Rock",
                    MusicalKey = "A",
                    Bpm = 140,
                    UserId = _testUserId
                }
            }
        };

        _mockTemplateService
            .Setup(s => s.GetTemplateByIdAsync(1, _testUserId))
            .ReturnsAsync(template);

        // Act
        var result = await _controller.GetTemplateStatistics(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var stats = okResult.Value.Should().BeOfType<TemplateStatisticsDto>().Subject;
        stats.TotalSongs.Should().Be(2);
        stats.AverageBpm.Should().Be(130);
        stats.GenreDistribution.Should().ContainKey("Rock").WhoseValue.Should().Be(2);
        stats.KeyDistribution.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTemplateStatistics_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockTemplateService
            .Setup(s => s.GetTemplateByIdAsync(999, _testUserId))
            .ReturnsAsync((SetlistTemplate?)null);

        // Act
        var result = await _controller.GetTemplateStatistics(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Trait("Category", "Security")]
    [Fact]
    public async Task CreateTemplate_AlwaysUsesAuthenticatedUserId()
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = "Test",
            SongIds = new List<int> { 1 }
        };

        SetlistTemplate? capturedTemplate = null;
        _mockTemplateService
            .Setup(s => s.CreateTemplateAsync(It.IsAny<SetlistTemplate>(), It.IsAny<IEnumerable<int>>(), _testUserId))
            .Callback<SetlistTemplate, IEnumerable<int>, string>((t, s, u) => capturedTemplate = t)
            .ReturnsAsync(CreateTemplate(1, "Test"));

        // Act
        await _controller.CreateTemplate(request);

        // Assert
        capturedTemplate.Should().NotBeNull();
        capturedTemplate!.UserId.Should().Be(_testUserId);
    }

    private SetlistTemplate CreateTemplate(int id, string name, string? category = null, bool isPublic = false, string? userId = null)
    {
        return new SetlistTemplate
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            Category = category,
            UserId = userId ?? _testUserId,
            IsPublic = isPublic,
            EstimatedDurationMinutes = 60,
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Songs = new List<SetlistTemplateSong>()
        };
    }
}
