using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Models;
using SetlistStudio.Core.Security;
using SetlistStudio.Infrastructure.Data;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Service for filtering songs based on multiple criteria
/// Builds optimized EF Core queries for efficient database filtering
/// </summary>
public class SongFilterService
{
    private readonly SetlistStudioDbContext _context;
    private readonly ILogger<SongFilterService> _logger;

    public SongFilterService(SetlistStudioDbContext context, ILogger<SongFilterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Filters songs based on criteria and returns paginated results
    /// </summary>
    public async Task<PaginatedResult<Song>> FilterSongsAsync(
        string userId,
        SongFilterCriteria criteria,
        int pageNumber = 1,
        int pageSize = 20)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _context.Songs.Where(s => s.UserId == userId).AsQueryable();

            // Apply all filters
            query = ApplyTextSearchFilter(query, criteria.SearchText);
            query = ApplyGenreFilter(query, criteria.Genres);
            query = ApplyBpmFilter(query, criteria.MinBpm, criteria.MaxBpm);
            query = ApplyMusicalKeyFilter(query, criteria.MusicalKeys);
            query = ApplyDifficultyFilter(query, criteria.DifficultyMin, criteria.DifficultyMax);
            query = ApplyDurationFilter(query, criteria.MinDurationSeconds, criteria.MaxDurationSeconds);
            query = ApplyTagFilter(query, criteria.Tags);
            query = ApplyOptionalFilter(query, criteria.IncludeOptional);
            query = ApplyEncoreFilter(query, criteria.IncludeEncore);

            // If tag filtering was requested, fall back to in-memory evaluation for tag matching
            // (complex string matching against a comma-separated field isn't reliably translated by all providers).
            List<Song> items;
            int totalCount;
            if (criteria.Tags != null && criteria.Tags.Count > 0)
            {
                var normalized = criteria.Tags.Select(t => t?.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                var inMemory = await query.ToListAsync();
                var filtered = inMemory.Where(s => !string.IsNullOrEmpty(s.Tags) && normalized.Any(tag => s.Tags!.Contains(tag!, StringComparison.OrdinalIgnoreCase))).ToList();
                totalCount = filtered.Count;
                // Apply sorting in-memory
                filtered = ApplySorting(filtered.AsQueryable(), criteria.SortBy, criteria.SortOrder).ToList();
                items = filtered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            }
            else
            {
                // Get total count before pagination
                totalCount = await query.CountAsync();

                // Apply sorting
                query = ApplySorting(query, criteria.SortBy, criteria.SortOrder);

                // Apply pagination
                items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }

            _logger.LogInformation(
                "Filtered songs for user {UserId}: found {Count} total, returning page {Page} with {Items} items",
                SecureLoggingHelper.SanitizeUserId(userId),
                totalCount,
                pageNumber,
                items.Count);

            return new PaginatedResult<Song>(items, pageNumber, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering songs for user {UserId}", 
                SecureLoggingHelper.SanitizeUserId(userId));
            throw;
        }
    }

    /// <summary>
    /// Get distinct genres for filter dropdowns
    /// </summary>
    public async Task<List<string>> GetAvailableGenresAsync(string userId)
    {
        return await _context.Songs
            .Where(s => s.UserId == userId && s.Genre != null)
            .Select(s => s.Genre!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();
    }

    /// <summary>
    /// Get distinct musical keys for filter dropdowns
    /// </summary>
    public async Task<List<string>> GetAvailableKeysAsync(string userId)
    {
        return await _context.Songs
            .Where(s => s.UserId == userId && s.MusicalKey != null)
            .Select(s => s.MusicalKey!)
            .Distinct()
            .OrderBy(k => k)
            .ToListAsync();
    }

    /// <summary>
    /// Get distinct tags for filter dropdowns
    /// </summary>
    public async Task<List<string>> GetAvailableTagsAsync(string userId)
    {
        var allTags = await _context.Songs
            .Where(s => s.UserId == userId && s.Tags != null)
            .Select(s => s.Tags)
            .ToListAsync();

        // Parse comma-separated tags and flatten
        var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tagString in allTags)
        {
            if (!string.IsNullOrEmpty(tagString))
            {
                var tags = tagString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in tags)
                {
                    tagSet.Add(tag.Trim());
                }
            }
        }

        return tagSet.OrderBy(t => t).ToList();
    }

    #region Filter Methods

    private IQueryable<Song> ApplyTextSearchFilter(IQueryable<Song> query, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return query;

        var searchLower = searchText.ToLower();
        return query.Where(s =>
            s.Title.ToLower().Contains(searchLower) ||
            s.Artist.ToLower().Contains(searchLower) ||
            (s.Album != null && s.Album.ToLower().Contains(searchLower)));
    }

    private IQueryable<Song> ApplyGenreFilter(IQueryable<Song> query, List<string>? genres)
    {
        if (genres == null || genres.Count == 0)
            return query;

        return query.Where(s => genres.Contains(s.Genre!));
    }

    private IQueryable<Song> ApplyBpmFilter(IQueryable<Song> query, int? minBpm, int? maxBpm)
    {
        if (minBpm.HasValue)
            query = query.Where(s => s.Bpm >= minBpm);

        if (maxBpm.HasValue)
            query = query.Where(s => s.Bpm <= maxBpm);

        return query;
    }

    private IQueryable<Song> ApplyMusicalKeyFilter(IQueryable<Song> query, List<string>? keys)
    {
        if (keys == null || keys.Count == 0)
            return query;

        return query.Where(s => keys.Contains(s.MusicalKey!));
    }

    private IQueryable<Song> ApplyDifficultyFilter(IQueryable<Song> query, int? minDifficulty, int? maxDifficulty)
    {
        if (minDifficulty.HasValue)
            query = query.Where(s => s.DifficultyRating >= minDifficulty);

        if (maxDifficulty.HasValue)
            query = query.Where(s => s.DifficultyRating <= maxDifficulty);

        return query;
    }

    private IQueryable<Song> ApplyDurationFilter(IQueryable<Song> query, int? minSeconds, int? maxSeconds)
    {
        if (minSeconds.HasValue)
            query = query.Where(s => s.DurationSeconds >= minSeconds);

        if (maxSeconds.HasValue)
            query = query.Where(s => s.DurationSeconds <= maxSeconds);

        return query;
    }

    private IQueryable<Song> ApplyTagFilter(IQueryable<Song> query, List<string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return query;

        // Filter songs where Tags contains any of the requested tags
        // Build an expression like: s => s.Tags != null && (s.Tags.Contains(tag1) || s.Tags.Contains(tag2) ...)
        var normalized = tags.Select(t => t?.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
        if (normalized.Count == 0)
            return query;

        var param = System.Linq.Expressions.Expression.Parameter(typeof(Song), "s");
        var tagsProp = System.Linq.Expressions.Expression.PropertyOrField(param, "Tags");
        var notNull = System.Linq.Expressions.Expression.NotEqual(tagsProp, System.Linq.Expressions.Expression.Constant(null, typeof(string)));

        System.Linq.Expressions.Expression? containsBody = null;
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        foreach (var tag in normalized)
        {
            var tagConst = System.Linq.Expressions.Expression.Constant(tag, typeof(string));
            var call = System.Linq.Expressions.Expression.Call(tagsProp, containsMethod!, tagConst);
            containsBody = containsBody == null ? call : System.Linq.Expressions.Expression.OrElse(containsBody, call);
        }

        var finalBody = System.Linq.Expressions.Expression.AndAlso(notNull, containsBody!);
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Song, bool>>(finalBody, param);
        return query.Where(lambda);
    }

    private IQueryable<Song> ApplyOptionalFilter(IQueryable<Song> query, bool? includeOptional)
    {
        // Song entity doesn't have IsOptional property, so return query as-is
        return query;
    }

    private IQueryable<Song> ApplyEncoreFilter(IQueryable<Song> query, bool? includeEncore)
    {
        // Song entity doesn't have IsEncore property, so return query as-is
        return query;
    }

    private IQueryable<Song> ApplySorting(IQueryable<Song> query, string? sortBy, string? sortOrder)
    {
        var ascending = sortOrder?.ToLower() != "desc";

        return (sortBy?.ToLower()) switch
        {
            "title" => ascending ? query.OrderBy(s => s.Title) : query.OrderByDescending(s => s.Title),
            "artist" => ascending ? query.OrderBy(s => s.Artist) : query.OrderByDescending(s => s.Artist),
            "bpm" => ascending ? query.OrderBy(s => s.Bpm) : query.OrderByDescending(s => s.Bpm),
            "duration" => ascending ? query.OrderBy(s => s.DurationSeconds) : query.OrderByDescending(s => s.DurationSeconds),
            "difficulty" => ascending ? query.OrderBy(s => s.DifficultyRating) : query.OrderByDescending(s => s.DifficultyRating),
            "genre" => ascending ? query.OrderBy(s => s.Genre) : query.OrderByDescending(s => s.Genre),
            _ => query.OrderBy(s => s.Artist).ThenBy(s => s.Title) // Default sort
        };
    }

    #endregion
}
