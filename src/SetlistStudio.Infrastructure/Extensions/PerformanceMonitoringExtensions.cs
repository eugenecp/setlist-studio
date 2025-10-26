using System.Diagnostics;
using SetlistStudio.Infrastructure.Services;

namespace SetlistStudio.Infrastructure.Extensions;

/// <summary>
/// Extension methods for performance monitoring in services
/// Provides easy integration with IPerformanceMonitoringService
/// </summary>
public static class PerformanceMonitoringExtensions
{
    /// <summary>
    /// Executes a function and tracks its performance
    /// </summary>
    /// <typeparam name="T">Return type of the function</typeparam>
    /// <param name="performanceService">The performance monitoring service</param>
    /// <param name="queryType">Type/name of the query for tracking</param>
    /// <param name="function">The function to execute and monitor</param>
    /// <param name="recordCount">Number of records returned (optional)</param>
    /// <returns>The result from the function</returns>
    public static async Task<T> TrackQueryAsync<T>(
        this IPerformanceMonitoringService performanceService,
        string queryType,
        Func<Task<T>> function,
        int recordCount = 0)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await function();
            
            // Try to get record count from result if not provided
            if (recordCount == 0)
            {
                recordCount = GetRecordCount(result);
            }
            
            return result;
        }
        finally
        {
            stopwatch.Stop();
            await performanceService.RecordQueryPerformanceAsync(queryType, stopwatch.Elapsed, recordCount);
        }
    }

    /// <summary>
    /// Executes a function that returns a tuple with count and tracks its performance
    /// </summary>
    /// <typeparam name="TData">Type of the data collection</typeparam>
    /// <param name="performanceService">The performance monitoring service</param>
    /// <param name="queryType">Type/name of the query for tracking</param>
    /// <param name="function">The function to execute and monitor</param>
    /// <returns>The result from the function</returns>
    public static async Task<(TData Data, int TotalCount)> TrackPaginatedQueryAsync<TData>(
        this IPerformanceMonitoringService performanceService,
        string queryType,
        Func<Task<(TData Data, int TotalCount)>> function)
        where TData : IEnumerable<object>
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await function();
            return result;
        }
        finally
        {
            stopwatch.Stop();
            var (data, totalCount) = await function();
            var recordCount = data?.Count() ?? 0;
            await performanceService.RecordQueryPerformanceAsync(queryType, stopwatch.Elapsed, recordCount);
        }
    }

    /// <summary>
    /// Tracks cache operations
    /// </summary>
    /// <param name="performanceService">The performance monitoring service</param>
    /// <param name="cacheKey">Cache key being accessed</param>
    /// <param name="isHit">Whether it was a cache hit</param>
    public static async Task TrackCacheOperationAsync(
        this IPerformanceMonitoringService performanceService,
        string cacheKey,
        bool isHit)
    {
        await performanceService.RecordCacheOperationAsync(cacheKey, isHit);
    }

    private static int GetRecordCount<T>(T result)
    {
        return result switch
        {
            System.Collections.ICollection collection => collection.Count,
            System.Collections.IEnumerable enumerable => enumerable.Cast<object>().Count(),
            _ => result != null ? 1 : 0
        };
    }
}