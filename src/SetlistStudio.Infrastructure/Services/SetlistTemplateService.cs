using Microsoft.EntityFrameworkCore;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Service for managing setlist templates - reusable blueprints for creating setlists
/// </summary>
public class SetlistTemplateService : ISetlistTemplateService
{
    private readonly SetlistStudioDbContext _context;
    private readonly ISetlistService _setlistService;

    public SetlistTemplateService(SetlistStudioDbContext context, ISetlistService setlistService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _setlistService = setlistService ?? throw new ArgumentNullException(nameof(setlistService));
    }

    /// <summary>
    /// Creates a new setlist template
    /// </summary>
    public async Task<SetlistTemplate> CreateTemplateAsync(SetlistTemplate template, string userId)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        template.UserId = userId;
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = null;

        _context.SetlistTemplates.Add(template);
        await _context.SaveChangesAsync();

        return template;
    }

    /// <summary>
    /// Gets all templates for a user with optional filtering and pagination
    /// </summary>
    public async Task<(IEnumerable<SetlistTemplate> Templates, int TotalCount)> GetTemplatesAsync(
        string userId,
        string? category = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _context.SetlistTemplates
            .Where(t => t.UserId == userId);

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(t => t.Category == category);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination and ordering
        var templates = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (templates, totalCount);
    }

    /// <summary>
    /// Gets a template by ID with songs included
    /// </summary>
    public async Task<SetlistTemplate?> GetTemplateByIdAsync(int templateId, string userId)
    {
        return await _context.SetlistTemplates
            .Include(t => t.TemplateSongs)
                .ThenInclude(ts => ts.Song)
            .Where(t => t.Id == templateId && t.UserId == userId)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Updates an existing template
    /// </summary>
    public async Task<SetlistTemplate?> UpdateTemplateAsync(int templateId, SetlistTemplate updatedTemplate, string userId)
    {
        var template = await _context.SetlistTemplates
            .Where(t => t.Id == templateId && t.UserId == userId)
            .FirstOrDefaultAsync();

        if (template == null)
            return null;

        // Update properties
        template.Name = updatedTemplate.Name;
        template.Description = updatedTemplate.Description;
        template.Category = updatedTemplate.Category;
        template.EstimatedDurationMinutes = updatedTemplate.EstimatedDurationMinutes;
        template.IsPublic = updatedTemplate.IsPublic;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return template;
    }

    /// <summary>
    /// Deletes a template
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(int templateId, string userId)
    {
        var template = await _context.SetlistTemplates
            .Where(t => t.Id == templateId && t.UserId == userId)
            .FirstOrDefaultAsync();

        if (template == null)
            return false;

        _context.SetlistTemplates.Remove(template);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Adds a song to a template at specified position
    /// </summary>
    public async Task<SetlistTemplate?> AddSongToTemplateAsync(int templateId, int songId, int position, string userId)
    {
        var template = await _context.SetlistTemplates
            .Include(t => t.TemplateSongs)
            .Where(t => t.Id == templateId && t.UserId == userId)
            .FirstOrDefaultAsync();

        if (template == null)
            return null;

        var templateSong = new SetlistTemplateSong
        {
            SetlistTemplateId = templateId,
            SongId = songId,
            Position = position
        };

        _context.SetlistTemplateSongs.Add(templateSong);
        await _context.SaveChangesAsync();

        // Reload with songs
        return await GetTemplateByIdAsync(templateId, userId);
    }

    /// <summary>
    /// Removes a song from a template
    /// </summary>
    public async Task<bool> RemoveSongFromTemplateAsync(int templateId, int songId, string userId)
    {
        var template = await _context.SetlistTemplates
            .Where(t => t.Id == templateId && t.UserId == userId)
            .FirstOrDefaultAsync();

        if (template == null)
            return false;

        var templateSong = await _context.SetlistTemplateSongs
            .Where(ts => ts.SetlistTemplateId == templateId && ts.SongId == songId)
            .FirstOrDefaultAsync();

        if (templateSong == null)
            return false;

        _context.SetlistTemplateSongs.Remove(templateSong);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Reorders songs in a template
    /// </summary>
    public async Task<SetlistTemplate?> ReorderTemplateSongsAsync(int templateId, List<int> songIds, string userId)
    {
        var template = await _context.SetlistTemplates
            .Include(t => t.TemplateSongs)
            .Where(t => t.Id == templateId && t.UserId == userId)
            .FirstOrDefaultAsync();

        if (template == null)
            return null;

        // Update positions based on new order
        for (int i = 0; i < songIds.Count; i++)
        {
            var songId = songIds[i];
            var templateSong = template.TemplateSongs.FirstOrDefault(ts => ts.SongId == songId);
            if (templateSong != null)
            {
                templateSong.Position = i + 1;
            }
        }

        await _context.SaveChangesAsync();

        return await GetTemplateByIdAsync(templateId, userId);
    }

    /// <summary>
    /// Converts a template to a new setlist
    /// </summary>
    public async Task<Setlist> ConvertTemplateToSetlistAsync(int templateId, string setlistName, DateTime? performanceDate, string userId)
    {
        // 1. Load template with authorization check
        var template = await GetTemplateByIdAsync(templateId, userId);
        if (template == null)
            throw new UnauthorizedAccessException("Template not found or access denied");

        // 2. Create new setlist from template
        var setlist = new Setlist
        {
            Name = setlistName,
            PerformanceDate = performanceDate,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // 3. Copy songs from template to setlist
        foreach (var templateSong in template.TemplateSongs.OrderBy(ts => ts.Position))
        {
            var setlistSong = new SetlistSong
            {
                SetlistId = setlist.Id,
                SongId = templateSong.SongId,
                Position = templateSong.Position
            };
            _context.SetlistSongs.Add(setlistSong);
        }

        await _context.SaveChangesAsync();

        // 4. Return fully loaded setlist
        var createdSetlist = await _setlistService.GetSetlistByIdAsync(setlist.Id, userId);
        return createdSetlist ?? throw new InvalidOperationException("Failed to load created setlist");
    }

    /// <summary>
    /// Gets all unique categories for a user's templates
    /// </summary>
    public async Task<IEnumerable<string>> GetCategoriesAsync(string userId)
    {
        return await _context.SetlistTemplates
            .Where(t => t.UserId == userId && !string.IsNullOrEmpty(t.Category))
            .Select(t => t.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }
}
