using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Service implementation for managing performance dates for setlists
/// Provides CRUD operations with user authorization checks
/// </summary>
public class PerformanceDateService : IPerformanceDateService
{
    private readonly SetlistStudioDbContext _context;
    private readonly ILogger<PerformanceDateService> _logger;

    public PerformanceDateService(SetlistStudioDbContext context, ILogger<PerformanceDateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<PerformanceDate>?> GetPerformanceDatesAsync(int setlistId, string userId)
    {
        try
        {
            // First verify that the setlist belongs to the user
            var setlistExists = await _context.Setlists
                .AnyAsync(sl => sl.Id == setlistId && sl.UserId == userId);

            if (!setlistExists)
            {
                _logger.LogWarning("Unauthorized access attempt: User {UserId} tried to access performance dates for setlist {SetlistId}", 
                    userId, setlistId);
                return null;
            }

            var performanceDates = await _context.PerformanceDates
                .Include(pd => pd.Setlist)
                .Where(pd => pd.SetlistId == setlistId && pd.UserId == userId)
                .OrderBy(pd => pd.Date)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} performance dates for setlist {SetlistId}", 
                performanceDates.Count, setlistId);

            return performanceDates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance dates for setlist {SetlistId}", setlistId);
            throw;
        }
    }

    public async Task<PerformanceDate?> GetPerformanceDateByIdAsync(int performanceDateId, string userId)
    {
        try
        {
            var performanceDate = await _context.PerformanceDates
                .Include(pd => pd.Setlist)
                .FirstOrDefaultAsync(pd => pd.Id == performanceDateId && pd.UserId == userId);

            if (performanceDate == null)
            {
                _logger.LogWarning("Performance date {PerformanceDateId} not found or unauthorized for user {UserId}", 
                    performanceDateId, userId);
            }

            return performanceDate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance date {PerformanceDateId}", performanceDateId);
            throw;
        }
    }

    public async Task<PerformanceDate?> CreatePerformanceDateAsync(PerformanceDate performanceDate)
    {
        try
        {
            // Verify that the setlist exists and belongs to the user
            var setlist = await _context.Setlists
                .FirstOrDefaultAsync(sl => sl.Id == performanceDate.SetlistId && sl.UserId == performanceDate.UserId);

            if (setlist == null)
            {
                _logger.LogWarning("Cannot create performance date: Setlist {SetlistId} not found or unauthorized for user {UserId}", 
                    performanceDate.SetlistId, performanceDate.UserId);
                return null;
            }

            // Set timestamps
            performanceDate.CreatedAt = DateTime.UtcNow;
            performanceDate.UpdatedAt = null;

            _context.PerformanceDates.Add(performanceDate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created performance date {PerformanceDateId} for setlist {SetlistId} on {Date}", 
                performanceDate.Id, performanceDate.SetlistId, performanceDate.Date);

            // Reload with navigation properties
            return await GetPerformanceDateByIdAsync(performanceDate.Id, performanceDate.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating performance date for setlist {SetlistId}", performanceDate.SetlistId);
            throw;
        }
    }

    public async Task<bool> DeletePerformanceDateAsync(int performanceDateId, string userId)
    {
        try
        {
            var performanceDate = await _context.PerformanceDates
                .FirstOrDefaultAsync(pd => pd.Id == performanceDateId && pd.UserId == userId);

            if (performanceDate == null)
            {
                _logger.LogWarning("Cannot delete performance date {PerformanceDateId}: Not found or unauthorized for user {UserId}", 
                    performanceDateId, userId);
                return false;
            }

            _context.PerformanceDates.Remove(performanceDate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted performance date {PerformanceDateId} for setlist {SetlistId}", 
                performanceDateId, performanceDate.SetlistId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting performance date {PerformanceDateId}", performanceDateId);
            throw;
        }
    }

    public async Task<(IEnumerable<PerformanceDate> PerformanceDates, int TotalCount)> GetUpcomingPerformanceDatesAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 20)
    {
        try
        {
            var currentDate = DateTime.UtcNow.Date;

            var query = _context.PerformanceDates
                .Include(pd => pd.Setlist)
                .Where(pd => pd.UserId == userId && pd.Date >= currentDate);

            var totalCount = await query.CountAsync();

            var performanceDates = await query
                .OrderBy(pd => pd.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} upcoming performance dates for user {UserId} (page {Page})", 
                performanceDates.Count, userId, pageNumber);

            return (performanceDates, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming performance dates for user {UserId}", userId);
            throw;
        }
    }
}
