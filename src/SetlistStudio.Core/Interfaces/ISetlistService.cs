using SetlistStudio.Core.Entities;

namespace SetlistStudio.Core.Interfaces;

/// <summary>
/// Service interface for managing setlists and their songs
/// Provides CRUD operations and setlist management functionality
/// </summary>
public interface ISetlistService
{
    /// <summary>
    /// Gets all setlists for a specific user with optional filtering and pagination
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="searchTerm">Optional search term to filter by name, venue, or description</param>
    /// <param name="isTemplate">Optional filter for template setlists</param>
    /// <param name="isActive">Optional filter for active setlists</param>
    /// <param name="pageNumber">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of setlists</returns>
    Task<(IEnumerable<Setlist> Setlists, int TotalCount)> GetSetlistsAsync(
        string userId,
        string? searchTerm = null,
        bool? isTemplate = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets a specific setlist by ID with its songs, ensuring it belongs to the user
    /// </summary>
    /// <param name="setlistId">The setlist ID</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>The setlist with songs if found and belongs to user, null otherwise</returns>
    Task<Setlist?> GetSetlistByIdAsync(int setlistId, string userId);

    /// <summary>
    /// Creates a new setlist for the user
    /// </summary>
    /// <param name="setlist">The setlist to create</param>
    /// <returns>The created setlist with assigned ID</returns>
    Task<Setlist> CreateSetlistAsync(Setlist setlist);

    /// <summary>
    /// Updates an existing setlist, ensuring it belongs to the user
    /// </summary>
    /// <param name="setlist">The setlist with updated information</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>The updated setlist if successful, null if not found or unauthorized</returns>
    Task<Setlist?> UpdateSetlistAsync(Setlist setlist, string userId);

    /// <summary>
    /// Deletes a setlist, ensuring it belongs to the user
    /// </summary>
    /// <param name="setlistId">The setlist ID to delete</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>True if deleted successfully, false if not found or unauthorized</returns>
    Task<bool> DeleteSetlistAsync(int setlistId, string userId);

    /// <summary>
    /// Adds a song to a setlist at the specified position
    /// </summary>
    /// <param name="setlistId">The setlist ID</param>
    /// <param name="songId">The song ID to add</param>
    /// <param name="userId">The user's ID</param>
    /// <param name="position">The position in the setlist (optional, adds to end if not specified)</param>
    /// <returns>The created SetlistSong if successful, null if unauthorized or song/setlist not found</returns>
    Task<SetlistSong?> AddSongToSetlistAsync(int setlistId, int songId, string userId, int? position = null);

    /// <summary>
    /// Removes a song from a setlist
    /// </summary>
    /// <param name="setlistId">The setlist ID</param>
    /// <param name="songId">The song ID to remove</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>True if removed successfully, false if not found or unauthorized</returns>
    Task<bool> RemoveSongFromSetlistAsync(int setlistId, int songId, string userId);

    /// <summary>
    /// Reorders songs in a setlist
    /// </summary>
    /// <param name="setlistId">The setlist ID</param>
    /// <param name="songOrdering">Array of song IDs in the desired order</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>True if reordered successfully, false if unauthorized or invalid</returns>
    Task<bool> ReorderSetlistSongsAsync(int setlistId, int[] songOrdering, string userId);

    /// <summary>
    /// Updates setlist song metadata (performance notes, custom BPM/key, etc.)
    /// </summary>
    /// <param name="setlistSongId">The SetlistSong ID</param>
    /// <param name="userId">The user's ID</param>
    /// <param name="performanceNotes">Performance notes</param>
    /// <param name="transitionNotes">Transition notes</param>
    /// <param name="customBpm">Custom BPM</param>
    /// <param name="customKey">Custom key</param>
    /// <param name="isEncore">Whether this is an encore song</param>
    /// <param name="isOptional">Whether this song is optional</param>
    /// <returns>The updated SetlistSong if successful, null if not found or unauthorized</returns>
    Task<SetlistSong?> UpdateSetlistSongAsync(
        int setlistSongId,
        string userId,
        string? performanceNotes = null,
        string? transitionNotes = null,
        int? customBpm = null,
        string? customKey = null,
        bool? isEncore = null,
        bool? isOptional = null);

    /// <summary>
    /// Creates a copy of an existing setlist
    /// </summary>
    /// <param name="sourceSetlistId">The setlist ID to copy from</param>
    /// <param name="newName">Name for the new setlist</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>The copied setlist if successful, null if not found or unauthorized</returns>
    Task<Setlist?> CopySetlistAsync(int sourceSetlistId, string newName, string userId);

    /// <summary>
    /// Validates setlist data before saving
    /// </summary>
    /// <param name="setlist">The setlist to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    IEnumerable<string> ValidateSetlist(Setlist setlist);
}