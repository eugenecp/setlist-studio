using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Security;
using SetlistStudio.Infrastructure.Data;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Service implementation for managing setlist templates
/// Implements SECURE, WORKS, SCALE, MAINTAINABLE, and USER DELIGHT principles
/// </summary>
public class SetlistTemplateService : ISetlistTemplateService
{
    private readonly SetlistStudioDbContext _context;
    private readonly ILogger<SetlistTemplateService> _logger;

    public SetlistTemplateService(
        SetlistStudioDbContext context, 
        ILogger<SetlistTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region CRUD Operations (WORKS Principle)

    /// <summary>
    /// Creates a new template with specified songs
    /// SECURE: Validates user owns all songs before creating template
    /// </summary>
    public async Task<SetlistTemplate> CreateTemplateAsync(
        SetlistTemplate template, 
        IEnumerable<int> songIds, 
        string userId)
    {
        try
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);

            // SECURE: Verify user owns all songs
            var userSongs = await _context.Songs
                .Where(s => s.UserId == userId && songIds.Contains(s.Id))
                .ToListAsync();

            if (userSongs.Count != songIds.Count())
            {
                _logger.LogWarning("User {UserId} attempted to create template with unauthorized songs", sanitizedUserId);
                throw new UnauthorizedAccessException("Cannot add songs you don't own");
            }

            // SECURE: Set userId from authentication context, not request
            template.UserId = userId;
            template.IsPublic = false; // Default private (SECURE)
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            template.UsageCount = 0;

            _context.SetlistTemplates.Add(template);
            await _context.SaveChangesAsync();

            // Add songs with positions
            var position = 1;
            foreach (var songId in songIds)
            {
                var templateSong = new SetlistTemplateSong
                {
                    SetlistTemplateId = template.Id,
                    SongId = songId,
                    Position = position++
                };
                _context.SetlistTemplateSongs.Add(templateSong);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Template {TemplateId} created by user {UserId} with {SongCount} songs", 
                template.Id, sanitizedUserId, songIds.Count());

            // Reload with songs
            return (await GetTemplateByIdAsync(template.Id, userId))!;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template for user {UserId}", SecureLoggingHelper.SanitizeUserId(userId));
            throw;
        }
    }

    /// <summary>
    /// Gets a specific template by ID
    /// SECURE: Returns template only if public OR owned by user
    /// </summary>
    public async Task<SetlistTemplate?> GetTemplateByIdAsync(int templateId, string userId)
    {
        try
        {
            var template = await _context.SetlistTemplates
                .Include(t => t.Songs.OrderBy(ts => ts.Position))
                .ThenInclude(ts => ts.Song)
                .FirstOrDefaultAsync(t => 
                    t.Id == templateId && 
                    (t.UserId == userId || t.IsPublic)); // SECURE: Authorization by filtering

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found or unauthorized for user {UserId}", 
                    templateId, SecureLoggingHelper.SanitizeUserId(userId));
            }

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId} for user {UserId}", 
                templateId, SecureLoggingHelper.SanitizeUserId(userId));
            throw;
        }
    }

    /// <summary>
    /// Gets all templates accessible to the user
    /// SECURE: Returns user's templates and optionally public templates
    /// </summary>
    public async Task<IEnumerable<SetlistTemplate>> GetTemplatesAsync(
        string userId, 
        bool includePublic = true, 
        string? category = null)
    {
        try
        {
            var query = _context.SetlistTemplates
                .Include(t => t.Songs)
                .ThenInclude(ts => ts.Song)
                .AsQueryable();

            // SECURE: Filter by user ownership or public visibility
            if (includePublic)
            {
                query = query.Where(t => t.UserId == userId || t.IsPublic);
            }
            else
            {
                query = query.Where(t => t.UserId == userId);
            }

            // SCALE: Category filtering
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(t => t.Category == category);
            }

            var templates = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} templates for user {UserId}", 
                templates.Count, SecureLoggingHelper.SanitizeUserId(userId));

            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates for user {UserId}", 
                SecureLoggingHelper.SanitizeUserId(userId));
            throw;
        }
    }

    /// <summary>
    /// Updates an existing template
    /// SECURE: Only owner can modify template
    /// </summary>
    public async Task<SetlistTemplate?> UpdateTemplateAsync(SetlistTemplate template, string userId)
    {
        try
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);

            // SECURE: Verify ownership before modification
            var existing = await _context.SetlistTemplates
                .FirstOrDefaultAsync(t => t.Id == template.Id && t.UserId == userId);

            if (existing == null)
            {
                _logger.LogWarning("Template {TemplateId} not found or unauthorized for user {UserId}", 
                    template.Id, sanitizedUserId);
                return null;
            }

            // Update allowed fields
            existing.Name = template.Name;
            existing.Description = template.Description;
            existing.Category = template.Category;
            existing.EstimatedDurationMinutes = template.EstimatedDurationMinutes;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Template {TemplateId} updated by user {UserId}", 
                template.Id, sanitizedUserId);

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId} for user {UserId}", 
                template.Id, SecureLoggingHelper.SanitizeUserId(userId));
            throw;
        }
    }

    /// <summary>
    /// Deletes a template
    /// SECURE: Only owner can delete template
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(int templateId, string userId)
    {
        try
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);

            // SECURE: Verify ownership before deletion
            var template = await _context.SetlistTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.UserId == userId);

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found or unauthorized for user {UserId}", 
                    templateId, sanitizedUserId);
                return false;
            }

            _context.SetlistTemplates.Remove(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template {TemplateId} deleted by user {UserId}", 
                templateId, sanitizedUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId} for user {UserId}", 
                templateId, SecureLoggingHelper.SanitizeUserId(userId));
            throw;
        }
    }

    #endregion

    #region Song Management (USER DELIGHT Principle)

    /// <summary>
    /// Adds a song to a template at specified position
    /// SECURE: Validates user owns template and song
    /// </summary>
    public async Task<bool> AddSongToTemplateAsync(int templateId, int songId, int position, string userId)
    {
        try
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);

            // SECURE: Verify template ownership
            var template = await _context.SetlistTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.UserId == userId);

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found or unauthorized for user {UserId}", 
                    templateId, sanitizedUserId);
                return false;
            }

            // SECURE: Verify song ownership
            var song = await _context.Songs
                .FirstOrDefaultAsync(s => s.Id == songId && s.UserId == userId);

            if (song == null)
            {
                _logger.LogWarning("Song {SongId} not found or unauthorized for user {UserId}", 
                    songId, sanitizedUserId);
                return false;
            }

            var templateSong = new SetlistTemplateSong
            {
                SetlistTemplateId = templateId,
                SongId = songId,
                Position = position
            };

            _context.SetlistTemplateSongs.Add(templateSong);
            template.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Song {SongId} added to template {TemplateId} by user {UserId}", 
                songId, templateId, sanitizedUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding song {SongId} to template {TemplateId} for user {UserId}", 
                songId, templateId, SecureLoggingHelper.SanitizeUserId(userId));
            throw;
        }
    }

    /// <summary>
    /// Removes a song from a template
    /// SECURE: Only owner can modify template
    /// </summary>
    public async Task<bool> RemoveSongFromTemplateAsync(int templateId, int songId, string userId)
    {
        try
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);

            // SECURE: Verify template ownership
            var template = await _context.SetlistTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.UserId == userId);

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found or unauthorized for user {UserId}", 
                    templateId, sanitizedUserId);
                return false;
            }

            var templateSong = await _context.SetlistTemplateSongs
                .FirstOrDefaultAsync(ts => ts.SetlistTemplateId == templateId && ts.SongId == songId);

            if (templateSong == null)
            {
                return false;
            }

            _context.SetlistTemplateSongs.Remove(templateSong);
            template.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Song {SongId} removed from template {TemplateId} by user {UserId}", 
                songId, templateId, sanitizedUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing song {SongId} from template {TemplateId} for user {UserId}", 
                songId, templateId, SecureLoggingHelper.SanitizeUserId(userId));
            throw;
        }
    }

    /// <summary>
    /// Reorders songs in a template
    /// SECURE: Only owner can modify template
    /// </summary>
    public async Task<bool> ReorderTemplateSongsAsync(int templateId, IEnumerable<int> songIds, string userId)
    {
        try
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);

            // SECURE: Verify template ownership
            var template = await _context.SetlistTemplates
                .Include(t => t.Songs)
                .FirstOrDefaultAsync(t => t.Id == templateId && t.UserId == userId);

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found or unauthorized for user {UserId}", 
                    templateId, sanitizedUserId);
                return false;
            }

            var position = 1;
            foreach (var songId in songIds)
            {
                var templateSong = template.Songs.FirstOrDefault(ts => ts.SongId == songId);
                if (templateSong != null)
                {
                    templateSong.Position = position++;
                }
            }

            template.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Songs reordered in template {TemplateId} by user {UserId}", 
                templateId, sanitizedUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering songs in template {TemplateId} for user {UserId}", 
                templateId, SecureLoggingHelper.SanitizeUserId(userId));
            throw;
        }
    }

    #endregion

    #region Sharing & Discovery (SCALE Principle)

    /// <summary>
    /// Sets template visibility (public/private)
    /// SECURE: Only owner can change visibility
    /// </summary>
    public async Task<bool> SetTemplateVisibilityAsync(int templateId, bool isPublic, string userId)
    {
        try
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);

            // SECURE: Verify ownership
            var template = await _context.SetlistTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.UserId == userId);

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found or unauthorized for user {UserId}", 
                    templateId, sanitizedUserId);
                return false;
            }

            template.IsPublic = isPublic;
            template.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template {TemplateId} visibility changed to {IsPublic} by user {UserId}", 
                templateId, isPublic, sanitizedUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing visibility of template {TemplateId} for user {UserId}", 
                templateId, SecureLoggingHelper.SanitizeUserId(userId));
            throw;
        }
    }

    /// <summary>
    /// Gets publicly shared templates with pagination
    /// SCALE: Efficient pagination for large template catalogs
    /// </summary>
    public async Task<(IEnumerable<SetlistTemplate> Templates, int TotalCount)> GetPublicTemplatesAsync(
        string? category = null, 
        int pageNumber = 1, 
        int pageSize = 20)
    {
        try
        {
            var query = _context.SetlistTemplates
                .Include(t => t.Songs)
                .ThenInclude(ts => ts.Song)
                .Where(t => t.IsPublic);

            // SCALE: Category filtering
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(t => t.Category == category);
            }

            var totalCount = await query.CountAsync();

            var templates = await query
                .OrderByDescending(t => t.UsageCount) // Most popular first
                .ThenByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} public templates (page {Page})", 
                templates.Count, pageNumber);

            return (templates, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public templates");
            throw;
        }
    }

    #endregion

    #region Conversion (WORKS Principle)

    /// <summary>
    /// Converts a template to an actual setlist for a performance
    /// SECURE: Creates setlist owned by current user using their own songs
    /// USER DELIGHT: Matches songs by Artist+Title, gracefully handles missing songs
    /// </summary>
    public async Task<Setlist> ConvertTemplateToSetlistAsync(
        int templateId, 
        DateTime performanceDate, 
        string? venue, 
        string userId)
    {
        try
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);

            // SECURE: Verify user can access template (public OR owned)
            var template = await _context.SetlistTemplates
                .Include(t => t.Songs.OrderBy(ts => ts.Position))
                .ThenInclude(ts => ts.Song)
                .FirstOrDefaultAsync(t => 
                    t.Id == templateId && 
                    (t.UserId == userId || t.IsPublic));

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found or access denied for user {UserId}", 
                    templateId, sanitizedUserId);
                throw new InvalidOperationException("Template not found or access denied");
            }

            // Create new setlist
            var setlist = new Setlist
            {
                Name = template.Name,
                Description = template.Description,
                PerformanceDate = performanceDate,
                Venue = venue,
                UserId = userId, // CRITICAL: New setlist owned by current user
                SourceTemplateId = templateId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Setlists.Add(setlist);
            await _context.SaveChangesAsync();

            // SECURE: Match songs by Artist+Title in user's library
            var matchedSongs = 0;
            foreach (var templateSong in template.Songs)
            {
                // Find matching song in current user's library
                var userSong = await _context.Songs
                    .FirstOrDefaultAsync(s => 
                        s.UserId == userId &&
                        s.Artist == templateSong.Song.Artist &&
                        s.Title == templateSong.Song.Title);

                if (userSong != null)
                {
                    var setlistSong = new SetlistSong
                    {
                        SetlistId = setlist.Id,
                        SongId = userSong.Id,
                        Position = templateSong.Position,
                        PerformanceNotes = templateSong.Notes
                    };

                    _context.SetlistSongs.Add(setlistSong);
                    matchedSongs++;
                }
                else
                {
                    _logger.LogInformation("Song '{Artist} - {Title}' not found in user {UserId} library during template conversion", 
                        templateSong.Song.Artist, templateSong.Song.Title, sanitizedUserId);
                }
            }

            // MAINTAINABLE: Increment usage counter
            template.UsageCount++;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Template {TemplateId} converted to setlist {SetlistId} for user {UserId} ({MatchedSongs}/{TotalSongs} songs)", 
                templateId, setlist.Id, sanitizedUserId, matchedSongs, template.Songs.Count);

            // Reload with songs
            return (await _context.Setlists
                .Include(s => s.SetlistSongs.OrderBy(ss => ss.Position))
                .ThenInclude(ss => ss.Song)
                .FirstAsync(s => s.Id == setlist.Id))!;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting template {TemplateId} to setlist for user {UserId}", 
                templateId, SecureLoggingHelper.SanitizeUserId(userId));
            throw;
        }
    }

    #endregion

    #region Analytics (USER DELIGHT Principle)

    /// <summary>
    /// Gets usage statistics for a template
    /// USER DELIGHT: Shows how popular/useful the template is
    /// </summary>
    public async Task<TemplateStatistics> GetTemplateStatisticsAsync(int templateId, string userId)
    {
        try
        {
            var template = await _context.SetlistTemplates
                .Include(t => t.Songs)
                .Include(t => t.GeneratedSetlists)
                .FirstOrDefaultAsync(t => 
                    t.Id == templateId && 
                    (t.UserId == userId || t.IsPublic));

            if (template == null)
            {
                throw new InvalidOperationException("Template not found or access denied");
            }

            var lastUsedDate = template.GeneratedSetlists
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault()?.CreatedAt;

            return new TemplateStatistics
            {
                UsageCount = template.UsageCount,
                TotalSongs = template.Songs.Count,
                EstimatedDurationMinutes = template.EstimatedDurationMinutes,
                LastUsedDate = lastUsedDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for template {TemplateId}", templateId);
            throw;
        }
    }

    #endregion
}
