using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Extensions;
using System.Collections.Concurrent;
using System.Linq;
namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Implementation of IQueryCacheService using IMemoryCache
/// Provides high-performance caching for expensive database operations
/// </summary>
public class QueryCacheService : IQueryCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<QueryCacheService> _logger;
    private readonly MemoryCacheEntryOptions _defaultOptions;
    private readonly ConcurrentDictionary<string, CacheMetrics> _metrics;
    private readonly IPerformanceMonitoringService? _performanceService;

    // Cache key prefixes for different data types
    private const string GenrePrefix = "genres";
    private const string ArtistPrefix = "artists";
    private const string SongCountPrefix = "song_count";
    private const string SetlistCountPrefix = "setlist_count";
    private const string RecentSongsPrefix = "recent_songs";

    // Cache configuration
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CountExpiry = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RecentSongsExpiry = TimeSpan.FromMinutes(10);

    public QueryCacheService(IMemoryCache cache, ILogger<QueryCacheService> logger, IPerformanceMonitoringService? performanceService = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _performanceService = performanceService;
        _metrics = new ConcurrentDictionary<string, CacheMetrics>();

        _defaultOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultExpiry,
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Size = 1,
            Priority = CacheItemPriority.Normal
        };
    }

    /// <summary>
    /// Gets cached genres for a user or retrieves and caches if not present
    /// </summary>
    public async Task<IEnumerable<string>> GetGenresAsync(string userId, Func<Task<IEnumerable<string>>> factory)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var cacheKey = GetCacheKey(GenrePrefix, userId);
        
        if (_cache.TryGetValue(cacheKey, out IEnumerable<string>? cached))
        {
            RecordCacheHit(cacheKey);
            _logger.LogDebug("Cache hit for genres: {UserId}", userId);
            return cached!;
        }

        RecordCacheMiss(cacheKey);
        _logger.LogDebug("Cache miss for genres: {UserId}", userId);

        var genres = await factory();
        var genreList = genres.ToList(); // Materialize to avoid multiple enumerations

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultExpiry,
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Size = EstimateSize(genreList),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, genreList, options);
        _logger.LogInformation("Cached {Count} genres for user {UserId}", genreList.Count, userId);

        return genreList;
    }

    /// <summary>
    /// Gets cached artists for a user or retrieves and caches if not present
    /// </summary>
    public async Task<IEnumerable<string>> GetArtistsAsync(string userId, Func<Task<IEnumerable<string>>> factory)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var cacheKey = GetCacheKey(ArtistPrefix, userId);
        
        if (_cache.TryGetValue(cacheKey, out IEnumerable<string>? cached))
        {
            RecordCacheHit(cacheKey);
            _logger.LogDebug("Cache hit for artists: {UserId}", userId);
            return cached!;
        }

        RecordCacheMiss(cacheKey);
        _logger.LogDebug("Cache miss for artists: {UserId}", userId);

        var artists = await factory();
        var artistList = artists.ToList(); // Materialize to avoid multiple enumerations

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultExpiry,
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Size = EstimateSize(artistList),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, artistList, options);
        _logger.LogInformation("Cached {Count} artists for user {UserId}", artistList.Count, userId);

        return artistList;
    }

    /// <summary>
    /// Gets cached song count for a user or retrieves and caches if not present
    /// </summary>
    public async Task<int> GetSongCountAsync(string userId, Func<Task<int>> factory)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var cacheKey = GetCacheKey(SongCountPrefix, userId);
        
        if (_cache.TryGetValue(cacheKey, out int cached))
        {
            RecordCacheHit(cacheKey);
            _logger.LogDebug("Cache hit for song count: {UserId}", userId);
            return cached;
        }

        RecordCacheMiss(cacheKey);
        _logger.LogDebug("Cache miss for song count: {UserId}", userId);

        var count = await factory();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CountExpiry,
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Size = 1,
            Priority = CacheItemPriority.High
        };

        _cache.Set(cacheKey, count, options);
        _logger.LogDebug("Cached song count {Count} for user {UserId}", count, userId);

        return count;
    }

    /// <summary>
    /// Gets cached setlist count for a user or retrieves and caches if not present
    /// </summary>
    public async Task<int> GetSetlistCountAsync(string userId, Func<Task<int>> factory)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var cacheKey = GetCacheKey(SetlistCountPrefix, userId);
        
        if (_cache.TryGetValue(cacheKey, out int cached))
        {
            RecordCacheHit(cacheKey);
            _logger.LogDebug("Cache hit for setlist count: {UserId}", userId);
            return cached;
        }

        RecordCacheMiss(cacheKey);
        _logger.LogDebug("Cache miss for setlist count: {UserId}", userId);

        var count = await factory();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CountExpiry,
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Size = 1,
            Priority = CacheItemPriority.High
        };

        _cache.Set(cacheKey, count, options);
        _logger.LogDebug("Cached setlist count {Count} for user {UserId}", count, userId);

        return count;
    }

    /// <summary>
    /// Gets or creates a cached value using the provided factory function
    /// Generic method for flexible caching of any data type with 5-minute expiration
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        if (_cache.TryGetValue(key, out T? cached))
        {
            RecordCacheHit(key);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cached!;
        }

        RecordCacheMiss(key);
        _logger.LogDebug("Cache miss for key: {Key}", key);

        var value = await factory();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Size = 1,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(key, value, options);
        _logger.LogInformation("Cached value for key: {Key}", key);

        return value;
    }

    /// <summary>
    /// Gets cached frequently accessed songs for a user
    /// </summary>
    public async Task<IEnumerable<Song>> GetRecentSongsAsync(string userId, Func<Task<IEnumerable<Song>>> factory)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var cacheKey = GetCacheKey(RecentSongsPrefix, userId);
        
        if (_cache.TryGetValue(cacheKey, out IEnumerable<Song>? cached))
        {
            RecordCacheHit(cacheKey);
            _logger.LogDebug("Cache hit for recent songs: {UserId}", userId);
            return cached!;
        }

        RecordCacheMiss(cacheKey);
        _logger.LogDebug("Cache miss for recent songs: {UserId}", userId);

        var songs = await factory();
        var songList = songs.ToList(); // Materialize to avoid multiple enumerations

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = RecentSongsExpiry,
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Size = EstimateSize(songList),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, songList, options);
        _logger.LogInformation("Cached {Count} recent songs for user {UserId}", songList.Count, userId);

        return songList;
    }

    /// <summary>
    /// Invalidates all cached data for a specific user
    /// </summary>
    public async Task InvalidateUserCacheAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var prefixes = new[] { GenrePrefix, ArtistPrefix, SongCountPrefix, SetlistCountPrefix, RecentSongsPrefix };
        var invalidatedCount = 0;

        foreach (var cacheKey in prefixes.Select(prefix => GetCacheKey(prefix, userId)))
        {
            _cache.Remove(cacheKey);
            _metrics.TryRemove(cacheKey, out _);
            invalidatedCount++;
        }

        _logger.LogInformation("Invalidated {Count} cache entries for user {UserId}", invalidatedCount, userId);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Invalidates cached genres for a specific user
    /// </summary>
    public async Task InvalidateGenreCacheAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var cacheKey = GetCacheKey(GenrePrefix, userId);
        _cache.Remove(cacheKey);
        _metrics.TryRemove(cacheKey, out _);

        _logger.LogDebug("Invalidated genre cache for user {UserId}", userId);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Invalidates cached artists for a specific user
    /// </summary>
    public async Task InvalidateArtistCacheAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        var cacheKey = GetCacheKey(ArtistPrefix, userId);
        _cache.Remove(cacheKey);
        _metrics.TryRemove(cacheKey, out _);

        _logger.LogDebug("Invalidated artist cache for user {UserId}", userId);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets cache statistics for monitoring and debugging
    /// </summary>
    public async Task<CacheStatistics> GetCacheStatisticsAsync()
    {
        var totalHits = 0L;
        var totalMisses = 0L;
        var entryCount = 0;

        foreach (var metric in _metrics.Values)
        {
            totalHits += metric.HitCount;
            totalMisses += metric.MissCount;
            entryCount++;
        }

        var statistics = new CacheStatistics
        {
            HitCount = totalHits,
            MissCount = totalMisses,
            EvictionCount = 0, // IMemoryCache doesn't provide eviction count
            CachedEntryCount = entryCount,
            EstimatedSize = EstimateTotalCacheSize()
        };

        _logger.LogDebug("Cache statistics - Hits: {Hits}, Misses: {Misses}, Hit Ratio: {HitRatio:P2}, Entries: {Entries}",
            statistics.HitCount, statistics.MissCount, statistics.HitRatio, statistics.CachedEntryCount);

        return await Task.FromResult(statistics);
    }

    private static string GetCacheKey(string prefix, string userId)
    {
        return $"{prefix}:{userId}";
    }

    private void RecordCacheHit(string cacheKey)
    {
        _metrics.AddOrUpdate(cacheKey, 
            new CacheMetrics { HitCount = 1 },
            (_, existing) => { existing.HitCount++; return existing; });
        
        // Track cache hit in performance monitoring if available
        _performanceService?.RecordCacheOperationAsync(cacheKey, true);
    }

    private void RecordCacheMiss(string cacheKey)
    {
        _metrics.AddOrUpdate(cacheKey, 
            new CacheMetrics { MissCount = 1 },
            (_, existing) => { existing.MissCount++; return existing; });
        
        // Track cache miss in performance monitoring if available
        _performanceService?.RecordCacheOperationAsync(cacheKey, false);
    }

    private static int EstimateSize<T>(ICollection<T> collection)
    {
        // Rough estimate: 1 unit per item plus overhead
        return Math.Max(1, collection.Count / 10);
    }

    private long EstimateTotalCacheSize()
    {
        // Simple estimation - in a real implementation you might use more sophisticated memory measurement
        return _metrics.Count * 1024; // Estimate 1KB per cache entry on average
    }

    private class CacheMetrics
    {
        public long HitCount { get; set; }
        public long MissCount { get; set; }
    }
}