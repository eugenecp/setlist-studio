using SetlistStudio.Core.Entities;

namespace SetlistStudio.Core.Interfaces;

/// <summary>
/// Service interface for managing songs in the user's library
/// Provides CRUD operations and search functionality
/// </summary>
public interface ISongService
{
    /// <summary>
    /// Gets all songs for a specific user with optional filtering and pagination
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="searchTerm">Optional search term to filter by title, artist, or album</param>
    /// <param name="genre">Optional genre filter</param>
    /// <param name="tags">Optional tags filter</param>
    /// <param name="pageNumber">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of songs</returns>
    Task<(IEnumerable<Song> Songs, int TotalCount)> GetSongsAsync(
        string userId,
        string? searchTerm = null,
        string? genre = null,
        string? tags = null,
        int pageNumber = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets a specific song by ID, ensuring it belongs to the user
    /// </summary>
    /// <param name="songId">The song ID</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>The song if found and belongs to user, null otherwise</returns>
    Task<Song?> GetSongByIdAsync(int songId, string userId);

    /// <summary>
    /// Creates a new song for the user
    /// </summary>
    /// <param name="song">The song to create</param>
    /// <returns>The created song with assigned ID</returns>
    Task<Song> CreateSongAsync(Song song);

    /// <summary>
    /// Updates an existing song, ensuring it belongs to the user
    /// </summary>
    /// <param name="song">The song with updated information</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>The updated song if successful, null if not found or unauthorized</returns>
    Task<Song?> UpdateSongAsync(Song song, string userId);

    /// <summary>
    /// Deletes a song, ensuring it belongs to the user
    /// </summary>
    /// <param name="songId">The song ID to delete</param>
    /// <param name="userId">The user's ID</param>
    /// <returns>True if deleted successfully, false if not found or unauthorized</returns>
    Task<bool> DeleteSongAsync(int songId, string userId);

    /// <summary>
    /// Gets all unique genres from the user's songs
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <returns>List of unique genres</returns>
    Task<IEnumerable<string>> GetGenresAsync(string userId);

    /// <summary>
    /// Gets all unique tags from the user's songs
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <returns>List of unique tags</returns>
    Task<IEnumerable<string>> GetTagsAsync(string userId);

    /// <summary>
    /// Validates song data before saving
    /// </summary>
    /// <param name="song">The song to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    IEnumerable<string> ValidateSong(Song song);
}