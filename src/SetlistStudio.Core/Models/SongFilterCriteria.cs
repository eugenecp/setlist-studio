namespace SetlistStudio.Core.Models;

/// <summary>
/// Criteria for filtering songs based on multiple attributes
/// Used by the song filter service to build dynamic queries
/// </summary>
public class SongFilterCriteria
{
    /// <summary>
    /// Search text to match against title, artist, or album
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Filter by one or more genres
    /// </summary>
    public List<string>? Genres { get; set; }

    /// <summary>
    /// Minimum BPM (beats per minute)
    /// </summary>
    public int? MinBpm { get; set; }

    /// <summary>
    /// Maximum BPM (beats per minute)
    /// </summary>
    public int? MaxBpm { get; set; }

    /// <summary>
    /// Filter by one or more musical keys (e.g., "C", "F#m", "Bb")
    /// </summary>
    public List<string>? MusicalKeys { get; set; }

    /// <summary>
    /// Minimum difficulty level (1-5)
    /// </summary>
    public int? DifficultyMin { get; set; }

    /// <summary>
    /// Maximum difficulty level (1-5)
    /// </summary>
    public int? DifficultyMax { get; set; }

    /// <summary>
    /// Filter by one or more tags
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Minimum song duration in seconds
    /// </summary>
    public int? MinDurationSeconds { get; set; }

    /// <summary>
    /// Maximum song duration in seconds
    /// </summary>
    public int? MaxDurationSeconds { get; set; }

    /// <summary>
    /// Include optional songs (default: false to exclude them)
    /// </summary>
    public bool? IncludeOptional { get; set; }

    /// <summary>
    /// Include encore songs (default: false to exclude them)
    /// </summary>
    public bool? IncludeEncore { get; set; }

    /// <summary>
    /// Sort field: "title", "artist", "bpm", "duration", "difficulty"
    /// </summary>
    public string? SortBy { get; set; } = "artist";

    /// <summary>
    /// Sort order: "asc" or "desc"
    /// </summary>
    public string? SortOrder { get; set; } = "asc";
}
