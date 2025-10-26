using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Interface for monitoring query performance and caching metrics
/// Provides insights into database query times and cache effectiveness
/// </summary>
public interface IPerformanceMonitoringService
{
    /// <summary>
    /// Records a database query execution time
    /// </summary>
    /// <param name="queryType">Type of query (e.g., "GetSongs", "GetSetlists")</param>
    /// <param name="executionTime">Time taken to execute the query</param>
    /// <param name="recordCount">Number of records returned</param>
    Task RecordQueryPerformanceAsync(string queryType, TimeSpan executionTime, int recordCount);

    /// <summary>
    /// Records a cache hit or miss
    /// </summary>
    /// <param name="cacheKey">The cache key</param>
    /// <param name="isHit">Whether it was a cache hit or miss</param>
    Task RecordCacheOperationAsync(string cacheKey, bool isHit);

    /// <summary>
    /// Gets query performance statistics
    /// </summary>
    /// <returns>Performance metrics for all tracked queries</returns>
    Task<QueryPerformanceStatistics> GetQueryPerformanceAsync();

    /// <summary>
    /// Gets cache performance statistics
    /// </summary>
    /// <returns>Cache performance metrics</returns>
    Task<CachePerformanceStatistics> GetCachePerformanceAsync();

    /// <summary>
    /// Clears all performance metrics (useful for testing)
    /// </summary>
    Task ClearMetricsAsync();
}

/// <summary>
/// Query performance statistics
/// </summary>
public class QueryPerformanceStatistics
{
    /// <summary>
    /// Performance metrics by query type
    /// </summary>
    public Dictionary<string, QueryMetrics> Queries { get; set; } = new();

    /// <summary>
    /// Overall average query time across all queries
    /// </summary>
    public double OverallAverageMs => Queries.Values.Any() 
        ? Queries.Values.Average(q => q.AverageExecutionTimeMs) 
        : 0;

    /// <summary>
    /// Total number of queries executed
    /// </summary>
    public long TotalQueries => Queries.Values.Sum(q => q.ExecutionCount);

    /// <summary>
    /// Slowest query type and its average time
    /// </summary>
    public (string QueryType, double AverageMs) SlowestQuery => Queries.Any() 
        ? Queries.OrderByDescending(q => q.Value.AverageExecutionTimeMs).First().Value.QueryType != null
            ? (Queries.OrderByDescending(q => q.Value.AverageExecutionTimeMs).First().Key, 
               Queries.OrderByDescending(q => q.Value.AverageExecutionTimeMs).First().Value.AverageExecutionTimeMs)
            : ("None", 0)
        : ("None", 0);
}

/// <summary>
/// Cache performance statistics
/// </summary>
public class CachePerformanceStatistics
{
    /// <summary>
    /// Total number of cache operations
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Number of cache hits
    /// </summary>
    public long HitCount { get; set; }

    /// <summary>
    /// Number of cache misses
    /// </summary>
    public long MissCount { get; set; }

    /// <summary>
    /// Cache hit ratio (0.0 to 1.0)
    /// </summary>
    public double HitRatio => TotalOperations > 0 ? (double)HitCount / TotalOperations : 0.0;

    /// <summary>
    /// Most frequently accessed cache keys
    /// </summary>
    public Dictionary<string, long> TopCacheKeys { get; set; } = new();
}

/// <summary>
/// Performance metrics for a specific query type
/// </summary>
public class QueryMetrics
{
    /// <summary>
    /// Query type identifier
    /// </summary>
    public string? QueryType { get; set; }

    /// <summary>
    /// Number of times this query was executed
    /// </summary>
    public long ExecutionCount { get; set; }

    /// <summary>
    /// Total execution time for all runs
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double AverageExecutionTimeMs => ExecutionCount > 0 
        ? TotalExecutionTime.TotalMilliseconds / ExecutionCount 
        : 0;

    /// <summary>
    /// Minimum execution time recorded
    /// </summary>
    public TimeSpan MinExecutionTime { get; set; } = TimeSpan.MaxValue;

    /// <summary>
    /// Maximum execution time recorded
    /// </summary>
    public TimeSpan MaxExecutionTime { get; set; }

    /// <summary>
    /// Total number of records returned across all executions
    /// </summary>
    public long TotalRecords { get; set; }

    /// <summary>
    /// Average number of records returned
    /// </summary>
    public double AverageRecords => ExecutionCount > 0 ? (double)TotalRecords / ExecutionCount : 0;
}

/// <summary>
/// Implementation of IPerformanceMonitoringService using concurrent collections for thread safety
/// </summary>
public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly ConcurrentDictionary<string, QueryMetrics> _queryMetrics;
    private readonly ConcurrentDictionary<string, long> _cacheOperations;
    private long _totalCacheHits;
    private long _totalCacheMisses;

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _queryMetrics = new ConcurrentDictionary<string, QueryMetrics>();
        _cacheOperations = new ConcurrentDictionary<string, long>();
    }

    /// <summary>
    /// Records a database query execution time
    /// </summary>
    public async Task RecordQueryPerformanceAsync(string queryType, TimeSpan executionTime, int recordCount)
    {
        if (string.IsNullOrWhiteSpace(queryType))
            throw new ArgumentException("Query type cannot be null or empty", nameof(queryType));

        _queryMetrics.AddOrUpdate(queryType,
            new QueryMetrics
            {
                QueryType = queryType,
                ExecutionCount = 1,
                TotalExecutionTime = executionTime,
                MinExecutionTime = executionTime,
                MaxExecutionTime = executionTime,
                TotalRecords = recordCount
            },
            (_, existing) =>
            {
                existing.ExecutionCount++;
                existing.TotalExecutionTime += executionTime;
                existing.MinExecutionTime = TimeSpan.FromTicks(Math.Min(existing.MinExecutionTime.Ticks, executionTime.Ticks));
                existing.MaxExecutionTime = TimeSpan.FromTicks(Math.Max(existing.MaxExecutionTime.Ticks, executionTime.Ticks));
                existing.TotalRecords += recordCount;
                return existing;
            });

        // Log slow queries (> 500ms as per scalability requirements)
        if (executionTime.TotalMilliseconds > 500)
        {
            _logger.LogWarning("Slow query detected: {QueryType} took {ExecutionTimeMs}ms for {RecordCount} records",
                queryType, executionTime.TotalMilliseconds, recordCount);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Records a cache hit or miss
    /// </summary>
    public async Task RecordCacheOperationAsync(string cacheKey, bool isHit)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(cacheKey));

        _cacheOperations.AddOrUpdate(cacheKey, 1, (_, count) => count + 1);

        if (isHit)
        {
            Interlocked.Increment(ref _totalCacheHits);
        }
        else
        {
            Interlocked.Increment(ref _totalCacheMisses);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets query performance statistics
    /// </summary>
    public async Task<QueryPerformanceStatistics> GetQueryPerformanceAsync()
    {
        var statistics = new QueryPerformanceStatistics
        {
            Queries = _queryMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        _logger.LogDebug("Retrieved query performance statistics: {TotalQueries} queries, {AverageMs}ms average",
            statistics.TotalQueries, statistics.OverallAverageMs);

        return await Task.FromResult(statistics);
    }

    /// <summary>
    /// Gets cache performance statistics
    /// </summary>
    public async Task<CachePerformanceStatistics> GetCachePerformanceAsync()
    {
        var statistics = new CachePerformanceStatistics
        {
            HitCount = _totalCacheHits,
            MissCount = _totalCacheMisses,
            TotalOperations = _totalCacheHits + _totalCacheMisses,
            TopCacheKeys = _cacheOperations.OrderByDescending(kvp => kvp.Value)
                                          .Take(10)
                                          .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        _logger.LogDebug("Retrieved cache performance statistics: {HitRatio:P2} hit ratio, {TotalOperations} operations",
            statistics.HitRatio, statistics.TotalOperations);

        return await Task.FromResult(statistics);
    }

    /// <summary>
    /// Clears all performance metrics (useful for testing)
    /// </summary>
    public async Task ClearMetricsAsync()
    {
        _queryMetrics.Clear();
        _cacheOperations.Clear();
        Interlocked.Exchange(ref _totalCacheHits, 0);
        Interlocked.Exchange(ref _totalCacheMisses, 0);

        _logger.LogInformation("Performance metrics cleared");
        await Task.CompletedTask;
    }
}