using SetlistStudio.Core.Entities;

namespace SetlistStudio.Core.Interfaces;

/// <summary>
/// Service interface for managing setlist templates
/// Provides CRUD operations, sharing, conversion, and analytics for reusable template blueprints
/// SECURE Principle: All methods include userId parameter for authorization
/// </summary>
public interface ISetlistTemplateService
{
    #region CRUD Operations (WORKS Principle)

    /// <summary>
    /// Creates a new template with specified songs
    /// SECURE: Validates user owns all songs before creating template
    /// </summary>
    /// <param name="template">The template to create</param>
    /// <param name="songIds">IDs of songs to add to template</param>
    /// <param name="userId">The user's ID (from authentication context)</param>
    /// <returns>The created template with assigned ID</returns>
    Task<SetlistTemplate> CreateTemplateAsync(SetlistTemplate template, IEnumerable<int> songIds, string userId);

    /// <summary>
    /// Gets a specific template by ID
    /// SECURE: Returns template only if public OR owned by user
    /// </summary>
    /// <param name="templateId">The template ID</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>The template if found and accessible, null otherwise</returns>
    Task<SetlistTemplate?> GetTemplateByIdAsync(int templateId, string userId);

    /// <summary>
    /// Gets all templates accessible to the user
    /// SECURE: Returns user's templates and optionally public templates
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="includePublic">Whether to include public templates from other users</param>
    /// <param name="category">Optional category filter</param>
    /// <returns>List of accessible templates</returns>
    Task<IEnumerable<SetlistTemplate>> GetTemplatesAsync(
        string userId, 
        bool includePublic = true, 
        string? category = null);

    /// <summary>
    /// Updates an existing template
    /// SECURE: Only owner can modify template
    /// </summary>
    /// <param name="template">The template with updated information</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>The updated template if successful, null if not found or unauthorized</returns>
    Task<SetlistTemplate?> UpdateTemplateAsync(SetlistTemplate template, string userId);

    /// <summary>
    /// Deletes a template
    /// SECURE: Only owner can delete template
    /// </summary>
    /// <param name="templateId">The template ID</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>True if deleted successfully, false if not found or unauthorized</returns>
    Task<bool> DeleteTemplateAsync(int templateId, string userId);

    #endregion

    #region Song Management (USER DELIGHT Principle)

    /// <summary>
    /// Adds a song to a template at specified position
    /// SECURE: Validates user owns template and song
    /// </summary>
    /// <param name="templateId">The template ID</param>
    /// <param name="songId">The song ID to add</param>
    /// <param name="position">Position in template (1-based)</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>True if added successfully, false if unauthorized</returns>
    Task<bool> AddSongToTemplateAsync(int templateId, int songId, int position, string userId);

    /// <summary>
    /// Removes a song from a template
    /// SECURE: Only owner can modify template
    /// </summary>
    /// <param name="templateId">The template ID</param>
    /// <param name="songId">The song ID to remove</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>True if removed successfully, false if not found or unauthorized</returns>
    Task<bool> RemoveSongFromTemplateAsync(int templateId, int songId, string userId);

    /// <summary>
    /// Reorders songs in a template
    /// SECURE: Only owner can modify template
    /// </summary>
    /// <param name="templateId">The template ID</param>
    /// <param name="songIds">Song IDs in desired order</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>True if reordered successfully, false if unauthorized</returns>
    Task<bool> ReorderTemplateSongsAsync(int templateId, IEnumerable<int> songIds, string userId);

    #endregion

    #region Sharing & Discovery (SCALE Principle)

    /// <summary>
    /// Sets template visibility (public/private)
    /// SECURE: Only owner can change visibility
    /// </summary>
    /// <param name="templateId">The template ID</param>
    /// <param name="isPublic">Whether template should be public</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>True if updated successfully, false if unauthorized</returns>
    Task<bool> SetTemplateVisibilityAsync(int templateId, bool isPublic, string userId);

    /// <summary>
    /// Gets publicly shared templates with pagination
    /// SCALE: Efficient pagination for large template catalogs
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of public templates</returns>
    Task<(IEnumerable<SetlistTemplate> Templates, int TotalCount)> GetPublicTemplatesAsync(
        string? category = null, 
        int pageNumber = 1, 
        int pageSize = 20);

    #endregion

    #region Conversion (WORKS Principle)

    /// <summary>
    /// Converts a template to an actual setlist for a performance
    /// SECURE: Creates setlist owned by current user using their own songs
    /// USER DELIGHT: Matches songs by Artist+Title, gracefully handles missing songs
    /// </summary>
    /// <param name="templateId">The template ID to convert</param>
    /// <param name="performanceDate">Date of the performance</param>
    /// <param name="venue">Performance venue</param>
    /// <param name="userId">The user's ID (owns the new setlist)</param>
    /// <returns>The created setlist</returns>
    Task<Setlist> ConvertTemplateToSetlistAsync(
        int templateId, 
        DateTime performanceDate, 
        string? venue, 
        string userId);

    #endregion

    #region Analytics (USER DELIGHT Principle)

    /// <summary>
    /// Gets usage statistics for a template
    /// USER DELIGHT: Shows how popular/useful the template is
    /// </summary>
    /// <param name="templateId">The template ID</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>Template statistics</returns>
    Task<TemplateStatistics> GetTemplateStatisticsAsync(int templateId, string userId);

    #endregion
}

/// <summary>
/// Statistics and analytics for a setlist template
/// USER DELIGHT: Provides insights into template usage and effectiveness
/// </summary>
public class TemplateStatistics
{
    /// <summary>
    /// Number of times this template has been used to create setlists
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Total number of songs in the template
    /// </summary>
    public int TotalSongs { get; set; }

    /// <summary>
    /// Estimated duration in minutes
    /// </summary>
    public int EstimatedDurationMinutes { get; set; }

    /// <summary>
    /// Date the template was last used to create a setlist
    /// </summary>
    public DateTime? LastUsedDate { get; set; }
}
