namespace SetlistStudio.Core.Models;

/// <summary>
/// Generic paginated result wrapper for API responses
/// Used to return paginated data with metadata
/// </summary>
/// <typeparam name="T">The type of items in the result set</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// The items for the current page
    /// </summary>
    public List<T> Items { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there are more pages after the current one
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Whether there are pages before the current one
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    public PaginatedResult(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public PaginatedResult()
    {
        Items = new List<T>();
        PageNumber = 1;
        PageSize = 20;
        TotalCount = 0;
    }
}
