using SetlistStudio.Core.Entities;

namespace SetlistStudio.Core.Interfaces;

/// <summary>
/// Interface for caching expensive query results to improve performance
/// Provides cache management for genre listings, artist aggregations, and frequently accessed data
/// </summary>
public interface IQueryCacheService
{
    /// <summary>
    /// Gets cached genres for a user or retrieves and caches if not present
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="factory">Factory function to retrieve genres if not cached</param>
    /// <returns>List of genres for the user</returns>
    Task<IEnumerable<string>> GetGenresAsync(string userId, Func<Task<IEnumerable<string>>> factory);

    /// <summary>
    /// Gets cached artists for a user or retrieves and caches if not present
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="factory">Factory function to retrieve artists if not cached</param>
    /// <returns>List of artists for the user</returns>
    Task<IEnumerable<string>> GetArtistsAsync(string userId, Func<Task<IEnumerable<string>>> factory);

    /// <summary>
    /// Gets cached song count for a user or retrieves and caches if not present
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="factory">Factory function to retrieve count if not cached</param>
    /// <returns>Total song count for the user</returns>
    Task<int> GetSongCountAsync(string userId, Func<Task<int>> factory);

    /// <summary>
    /// Gets cached setlist count for a user or retrieves and caches if not present
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="factory">Factory function to retrieve count if not cached</param>
    /// <returns>Total setlist count for the user</returns>
    Task<int> GetSetlistCountAsync(string userId, Func<Task<int>> factory);

    /// <summary>
    /// Gets cached frequently accessed songs for a user
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="factory">Factory function to retrieve songs if not cached</param>
    /// <returns>List of frequently accessed songs</returns>
    Task<IEnumerable<Song>> GetRecentSongsAsync(string userId, Func<Task<IEnumerable<Song>>> factory);

    /// <summary>
    /// Invalidates all cached data for a specific user
    /// Should be called when user data is modified
    /// </summary>
    /// <param name="userId">The user's ID</param>
    Task InvalidateUserCacheAsync(string userId);

    /// <summary>
    /// Invalidates cached genres for a specific user
    /// Should be called when user's song genres are modified
    /// </summary>
    /// <param name="userId">The user's ID</param>
    Task InvalidateGenreCacheAsync(string userId);

    /// <summary>
    /// Invalidates cached artists for a specific user
    /// Should be called when user's song artists are modified
    /// </summary>
    /// <param name="userId">The user's ID</param>
    Task InvalidateArtistCacheAsync(string userId);

    /// <summary>
    /// Gets cache statistics for monitoring and debugging
    /// </summary>
    /// <returns>Cache performance metrics</returns>
    Task<CacheStatistics> GetCacheStatisticsAsync();
}

/// <summary>
/// Cache performance statistics
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Total number of cache hits
    /// </summary>
    public long HitCount { get; set; }

    /// <summary>
    /// Total number of cache misses
    /// </summary>
    public long MissCount { get; set; }

    /// <summary>
    /// Cache hit ratio (0.0 to 1.0)
    /// </summary>
    public double HitRatio => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0.0;

    /// <summary>
    /// Number of cache evictions
    /// </summary>
    public long EvictionCount { get; set; }

    /// <summary>
    /// Current number of cached entries
    /// </summary>
    public int CachedEntryCount { get; set; }

    /// <summary>
    /// Estimated memory size of cache in bytes
    /// </summary>
    public long EstimatedSize { get; set; }
}