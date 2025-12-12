using SetlistStudio.Core.Entities;

namespace SetlistStudio.Core.Interfaces;

/// <summary>
/// Service for managing reusable setlist templates
/// </summary>
public interface ISetlistTemplateService
{
    /// <summary>
    /// Creates a new template for the specified user
    /// </summary>
    Task<SetlistTemplate> CreateTemplateAsync(SetlistTemplate template, string userId);
    
    /// <summary>
    /// Gets all templates for a user with optional filtering and pagination
    /// </summary>
    Task<(IEnumerable<SetlistTemplate> Templates, int TotalCount)> GetTemplatesAsync(
        string userId, 
        string? category = null,
        int pageNumber = 1, 
        int pageSize = 20);
    
    /// <summary>
    /// Gets a specific template by ID (with user ownership verification)
    /// </summary>
    Task<SetlistTemplate?> GetTemplateByIdAsync(int templateId, string userId);
    
    /// <summary>
    /// Updates an existing template (with user ownership verification)
    /// </summary>
    Task<SetlistTemplate?> UpdateTemplateAsync(int templateId, SetlistTemplate updatedTemplate, string userId);
    
    /// <summary>
    /// Deletes a template (with user ownership verification)
    /// </summary>
    Task<bool> DeleteTemplateAsync(int templateId, string userId);
    
    /// <summary>
    /// Adds a song to a template at the specified position
    /// </summary>
    Task<SetlistTemplate?> AddSongToTemplateAsync(int templateId, int songId, int position, string userId);
    
    /// <summary>
    /// Removes a song from a template
    /// </summary>
    Task<bool> RemoveSongFromTemplateAsync(int templateId, int songId, string userId);
    
    /// <summary>
    /// Reorders songs in a template
    /// </summary>
    Task<SetlistTemplate?> ReorderTemplateSongsAsync(int templateId, List<int> songIds, string userId);
    
    /// <summary>
    /// Converts a template to an actual setlist
    /// </summary>
    Task<Setlist> ConvertTemplateToSetlistAsync(int templateId, string setlistName, DateTime? performanceDate, string userId);
    
    /// <summary>
    /// Gets all unique categories for a user's templates
    /// </summary>
    Task<IEnumerable<string>> GetCategoriesAsync(string userId);
}
