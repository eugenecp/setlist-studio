using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Security;
using SetlistStudio.Web.Models;
using SetlistStudio.Web.Security;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// API controller for managing setlist templates
/// Provides endpoints for CRUD operations, song management, template conversion, and statistics
/// </summary>
[ApiController]
[Route("api/templates")]
[Authorize]
[EnableRateLimiting("api")]
public class SetlistTemplateController : ControllerBase
{
    private readonly ISetlistTemplateService _templateService;
    private readonly ISongService _songService;
    private readonly ISetlistService _setlistService;
    private readonly ILogger<SetlistTemplateController> _logger;

    public SetlistTemplateController(
        ISetlistTemplateService templateService,
        ISongService songService,
        ISetlistService setlistService,
        ILogger<SetlistTemplateController> logger)
    {
        _templateService = templateService;
        _songService = songService;
        _setlistService = setlistService;
        _logger = logger;
    }

    /// <summary>
    /// Get all templates for the current user
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated list of user's templates</returns>
    [HttpGet]
    [ProducesResponseType(typeof(TemplateListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TemplateListResponse>> GetTemplates(
        [FromQuery] string? category = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        // Validate pagination parameters
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        
        var templates = await _templateService.GetTemplatesAsync(userId, includePublic: false, category);
        
        var totalCount = templates.Count();
        var pagedTemplates = templates
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto)
            .ToList();
        
        return Ok(new TemplateListResponse
        {
            Templates = pagedTemplates,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Browse public templates shared by other users
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated list of public templates</returns>
    [HttpGet("public")]
    [ProducesResponseType(typeof(TemplateListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TemplateListResponse>> GetPublicTemplates(
        [FromQuery] string? category = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        // Validate pagination parameters
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        
        var (templates, totalCount) = await _templateService.GetPublicTemplatesAsync(category, pageNumber, pageSize);
        
        var templateDtos = templates.Select(MapToDto).ToList();
        
        return Ok(new TemplateListResponse
        {
            Templates = templateDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Template details with songs</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TemplateDto>> GetTemplate(int id)
    {
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        var template = await _templateService.GetTemplateByIdAsync(id, userId);
        
        if (template == null)
        {
            return NotFound(new { message = "Template not found or access denied" });
        }
        
        return Ok(MapToDto(template));
    }

    /// <summary>
    /// Create a new template
    /// </summary>
    /// <param name="request">Template creation request</param>
    /// <returns>Created template</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TemplateDto>> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        try
        {
            var template = new SetlistTemplate
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                EstimatedDurationMinutes = request.EstimatedDurationMinutes,
                IsPublic = request.IsPublic,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            var created = await _templateService.CreateTemplateAsync(template, request.SongIds, userId);
            
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogInformation("Template created: {TemplateId} by user {UserId}", created.Id, sanitizedUserId);
            
            return CreatedAtAction(
                nameof(GetTemplate),
                new { id = created.Id },
                MapToDto(created));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized template creation attempt by user {UserId}", 
                SecureLoggingHelper.SanitizeUserId(userId));
            return BadRequest(new { message = "Cannot add songs you don't own to template" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template for user {UserId}", 
                SecureLoggingHelper.SanitizeUserId(userId));
            return Problem("An error occurred while creating the template");
        }
    }

    /// <summary>
    /// Update an existing template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated template</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TemplateDto>> UpdateTemplate(int id, [FromBody] UpdateTemplateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        var existing = await _templateService.GetTemplateByIdAsync(id, userId);
        if (existing == null || existing.UserId != userId)
        {
            return NotFound(new { message = "Template not found or access denied" });
        }
        
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Category = request.Category;
        existing.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        existing.UpdatedAt = DateTime.UtcNow;
        
        var updated = await _templateService.UpdateTemplateAsync(existing, userId);
        
        if (updated == null)
        {
            return NotFound(new { message = "Template not found or access denied" });
        }
        
        _logger.LogInformation("Template updated: {TemplateId} by user {UserId}", 
            id, SecureLoggingHelper.SanitizeUserId(userId));
        
        return Ok(MapToDto(updated));
    }

    /// <summary>
    /// Delete a template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        var success = await _templateService.DeleteTemplateAsync(id, userId);
        
        if (!success)
        {
            return NotFound(new { message = "Template not found or access denied" });
        }
        
        _logger.LogInformation("Template deleted: {TemplateId} by user {UserId}", 
            id, SecureLoggingHelper.SanitizeUserId(userId));
        
        return NoContent();
    }

    /// <summary>
    /// Add a song to a template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Add song request</param>
    /// <returns>No content on success</returns>
    [HttpPost("{id}/songs")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddSongToTemplate(int id, [FromBody] AddSongToTemplateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        try
        {
            var success = await _templateService.AddSongToTemplateAsync(id, request.SongId, request.Position, userId);
            
            if (!success)
            {
                return NotFound(new { message = "Template not found or access denied" });
            }
            
            _logger.LogInformation("Song {SongId} added to template {TemplateId} by user {UserId}", 
                request.SongId, id, SecureLoggingHelper.SanitizeUserId(userId));
            
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized song addition attempt to template {TemplateId}", id);
            return BadRequest(new { message = "Cannot add songs you don't own" });
        }
    }

    /// <summary>
    /// Remove a song from a template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="songId">Song ID to remove</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}/songs/{songId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveSongFromTemplate(int id, int songId)
    {
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        var success = await _templateService.RemoveSongFromTemplateAsync(id, songId, userId);
        
        if (!success)
        {
            return NotFound(new { message = "Template or song not found, or access denied" });
        }
        
        _logger.LogInformation("Song {SongId} removed from template {TemplateId} by user {UserId}", 
            songId, id, SecureLoggingHelper.SanitizeUserId(userId));
        
        return NoContent();
    }

    /// <summary>
    /// Reorder songs in a template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Reorder request with new song order</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id}/songs/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReorderTemplateSongs(int id, [FromBody] ReorderTemplateSongsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        var success = await _templateService.ReorderTemplateSongsAsync(id, request.SongIds, userId);
        
        if (!success)
        {
            return NotFound(new { message = "Template not found or access denied" });
        }
        
        _logger.LogInformation("Template {TemplateId} songs reordered by user {UserId}", 
            id, SecureLoggingHelper.SanitizeUserId(userId));
        
        return NoContent();
    }

    /// <summary>
    /// Set template visibility (public/private)
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="isPublic">True to make public, false for private</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id}/visibility")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetTemplateVisibility(int id, [FromQuery] bool isPublic)
    {
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        var success = await _templateService.SetTemplateVisibilityAsync(id, isPublic, userId);
        
        if (!success)
        {
            return NotFound(new { message = "Template not found or access denied" });
        }
        
        _logger.LogInformation("Template {TemplateId} visibility changed to {Visibility} by user {UserId}", 
            id, isPublic ? "public" : "private", SecureLoggingHelper.SanitizeUserId(userId));
        
        return NoContent();
    }

    /// <summary>
    /// Convert a template to a setlist for a specific performance
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Conversion request with performance details</param>
    /// <returns>Created setlist</returns>
    [HttpPost("{id}/convert-to-setlist")]
    [ProducesResponseType(typeof(SetlistDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SetlistDto>> ConvertTemplateToSetlist(int id, [FromBody] ConvertTemplateToSetlistRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        try
        {
            var setlist = await _templateService.ConvertTemplateToSetlistAsync(
                id,
                request.PerformanceDate,
                request.Venue,
                userId);
            
            _logger.LogInformation("Template {TemplateId} converted to setlist {SetlistId} by user {UserId}", 
                id, setlist.Id, SecureLoggingHelper.SanitizeUserId(userId));
            
            var setlistDto = new SetlistDto
            {
                Id = setlist.Id,
                Name = setlist.Name,
                PerformanceDate = setlist.PerformanceDate,
                Venue = setlist.Venue,
                UserId = setlist.UserId,
                CreatedAt = setlist.CreatedAt,
                UpdatedAt = setlist.UpdatedAt ?? setlist.CreatedAt,
                Songs = setlist.SetlistSongs.Select(ss => new SetlistSongDto
                {
                    Id = ss.Id,
                    Position = ss.Position,
                    Notes = ss.PerformanceNotes,
                    Song = new SongDto
                    {
                        Id = ss.Song.Id,
                        Title = ss.Song.Title,
                        Artist = ss.Song.Artist,
                        Album = ss.Song.Album,
                        DurationSeconds = ss.Song.DurationSeconds,
                        Bpm = ss.Song.Bpm,
                        MusicalKey = ss.Song.MusicalKey,
                        Genre = ss.Song.Genre,
                        Tags = ss.Song.Tags
                    }
                }).ToList()
            };
            
            return CreatedAtAction(
                nameof(SetlistsController.GetSetlist),
                "Setlists",
                new { id = setlist.Id },
                setlistDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized template conversion attempt: {TemplateId}", id);
            return NotFound(new { message = "Template not found or access denied" });
        }
    }

    /// <summary>
    /// Get statistics for a template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Template statistics</returns>
    [HttpGet("{id}/statistics")]
    [ProducesResponseType(typeof(TemplateStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TemplateStatisticsDto>> GetTemplateStatistics(int id)
    {
        var userId = SecureUserContext.GetSanitizedUserId(User);
        
        var template = await _templateService.GetTemplateByIdAsync(id, userId);
        
        if (template == null)
        {
            return NotFound(new { message = "Template not found or access denied" });
        }
        
        var statistics = new TemplateStatisticsDto
        {
            TemplateId = template.Id,
            TemplateName = template.Name,
            TotalSongs = template.Songs.Count,
            EstimatedDurationMinutes = template.EstimatedDurationMinutes,
            UsageCount = template.UsageCount,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
        
        // Calculate average BPM
        var songsWithBpm = template.Songs.Where(ts => ts.Song.Bpm.HasValue).ToList();
        if (songsWithBpm.Any())
        {
            statistics.AverageBpm = (int)songsWithBpm.Average(ts => ts.Song.Bpm!.Value);
        }
        
        // Genre distribution
        statistics.GenreDistribution = template.Songs
            .Where(ts => !string.IsNullOrWhiteSpace(ts.Song.Genre))
            .GroupBy(ts => ts.Song.Genre!)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Key distribution
        statistics.KeyDistribution = template.Songs
            .Where(ts => !string.IsNullOrWhiteSpace(ts.Song.MusicalKey))
            .GroupBy(ts => ts.Song.MusicalKey!)
            .ToDictionary(g => g.Key, g => g.Count());
        
        return Ok(statistics);
    }

    private static TemplateDto MapToDto(SetlistTemplate template)
    {
        return new TemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            UserId = template.UserId,
            IsPublic = template.IsPublic,
            EstimatedDurationMinutes = template.EstimatedDurationMinutes,
            UsageCount = template.UsageCount,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            Songs = template.Songs
                .OrderBy(ts => ts.Position)
                .Select(ts => new TemplateSongDto
                {
                    Id = ts.Id,
                    Position = ts.Position,
                    Notes = ts.Notes,
                    Song = new SongSummaryDto
                    {
                        Id = ts.Song.Id,
                        Title = ts.Song.Title,
                        Artist = ts.Song.Artist,
                        Album = ts.Song.Album,
                        DurationSeconds = ts.Song.DurationSeconds,
                        Bpm = ts.Song.Bpm,
                        MusicalKey = ts.Song.MusicalKey
                    }
                }).ToList()
        };
    }
}
