using SetlistStudio.Core.Entities;

namespace SetlistStudio.Core.Interfaces;

/// <summary>
/// Service interface for managing performance dates for setlists
/// Provides CRUD operations with user authorization checks
/// </summary>
public interface IPerformanceDateService
{
    /// <summary>
    /// Gets all performance dates for a specific setlist, ensuring it belongs to the user
    /// </summary>
    /// <param name="setlistId">The setlist ID</param>
    /// <param name="userId">The user's ID for authorization</param>
    /// <returns>List of performance dates if authorized, null if unauthorized</returns>
    Task<IEnumerable<PerformanceDate>?> GetPerformanceDatesAsync(int setlistId, string userId);

    /// <summary>
    /// Gets a specific performance date by ID, ensuring it belongs to the user
    /// </summary>
    /// <param name="performanceDateId">The performance date ID</param>
    /// <param name="userId">The user's ID for authorization</param>
    /// <returns>The performance date if found and authorized, null otherwise</returns>
    Task<PerformanceDate?> GetPerformanceDateByIdAsync(int performanceDateId, string userId);

    /// <summary>
    /// Creates a new performance date for a setlist
    /// Validates that the setlist belongs to the user before creating
    /// </summary>
    /// <param name="performanceDate">The performance date to create</param>
    /// <returns>The created performance date with assigned ID if authorized, null if unauthorized</returns>
    Task<PerformanceDate?> CreatePerformanceDateAsync(PerformanceDate performanceDate);

    /// <summary>
    /// Deletes a performance date, ensuring it belongs to the user
    /// </summary>
    /// <param name="performanceDateId">The performance date ID</param>
    /// <param name="userId">The user's ID for authorization</param>
    /// <returns>True if deleted, false if not found or unauthorized</returns>
    Task<bool> DeletePerformanceDateAsync(int performanceDateId, string userId);

    /// <summary>
    /// Gets all upcoming performance dates for a user (dates after current date)
    /// Ordered by date ascending
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="pageNumber">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of upcoming performance dates</returns>
    Task<(IEnumerable<PerformanceDate> PerformanceDates, int TotalCount)> GetUpcomingPerformanceDatesAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 20);
}
