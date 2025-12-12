using System.ComponentModel.DataAnnotations;
using SetlistStudio.Core.Validation;

namespace SetlistStudio.Web.Models;

/// <summary>
/// Response DTO for SetlistTemplate entity
/// </summary>
public class TemplateDto
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? Category { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public bool IsPublic { get; set; }
    
    public int EstimatedDurationMinutes { get; set; }
    
    public int UsageCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public List<TemplateSongDto> Songs { get; set; } = new();
}

/// <summary>
/// Response DTO for SetlistTemplateSong entity
/// </summary>
public class TemplateSongDto
{
    public int Id { get; set; }
    
    public int Position { get; set; }
    
    public string? Notes { get; set; }
    
    public SongSummaryDto Song { get; set; } = null!;
}

/// <summary>
/// Lightweight song information for template responses
/// </summary>
public class SongSummaryDto
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string Artist { get; set; } = string.Empty;
    
    public string? Album { get; set; }
    
    public int? DurationSeconds { get; set; }
    
    public int? Bpm { get; set; }
    
    public string? MusicalKey { get; set; }
}

/// <summary>
/// Request DTO for creating a new template
/// </summary>
public class CreateTemplateRequest
{
    [Required(ErrorMessage = "Template name is required")]
    [MaxLength(200, ErrorMessage = "Template name cannot exceed 200 characters")]
    [SanitizedString(AllowHtml = false, MaxLength = 200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [SanitizedString(AllowHtml = false, MaxLength = 500)]
    public string? Description { get; set; }
    
    [MaxLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }
    
    [Range(0, 1440, ErrorMessage = "Estimated duration must be between 0 and 1440 minutes (24 hours)")]
    public int EstimatedDurationMinutes { get; set; }
    
    public bool IsPublic { get; set; }
    
    [Required(ErrorMessage = "At least one song is required")]
    [MinLength(1, ErrorMessage = "Template must contain at least one song")]
    public List<int> SongIds { get; set; } = new();
}

/// <summary>
/// Request DTO for updating an existing template
/// </summary>
public class UpdateTemplateRequest
{
    [Required(ErrorMessage = "Template name is required")]
    [MaxLength(200, ErrorMessage = "Template name cannot exceed 200 characters")]
    [SanitizedString(AllowHtml = false, MaxLength = 200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [SanitizedString(AllowHtml = false, MaxLength = 500)]
    public string? Description { get; set; }
    
    [MaxLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }
    
    [Range(0, 1440, ErrorMessage = "Estimated duration must be between 0 and 1440 minutes (24 hours)")]
    public int EstimatedDurationMinutes { get; set; }
}

/// <summary>
/// Request DTO for adding a song to a template
/// </summary>
public class AddSongToTemplateRequest
{
    [Required(ErrorMessage = "Song ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Song ID must be greater than 0")]
    public int SongId { get; set; }
    
    [Required(ErrorMessage = "Position is required")]
    [Range(1, 1000, ErrorMessage = "Position must be between 1 and 1000")]
    public int Position { get; set; }
    
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    [SanitizedString(AllowHtml = false, MaxLength = 500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Request DTO for reordering template songs
/// </summary>
public class ReorderTemplateSongsRequest
{
    [Required(ErrorMessage = "Song order is required")]
    [MinLength(1, ErrorMessage = "At least one song ID is required")]
    public List<int> SongIds { get; set; } = new();
}

/// <summary>
/// Request DTO for converting template to setlist
/// </summary>
public class ConvertTemplateToSetlistRequest
{
    [Required(ErrorMessage = "Performance date is required")]
    public DateTime PerformanceDate { get; set; }
    
    [MaxLength(200, ErrorMessage = "Venue cannot exceed 200 characters")]
    [SanitizedString(AllowHtml = false, MaxLength = 200)]
    public string? Venue { get; set; }
}

/// <summary>
/// Response DTO for template statistics
/// </summary>
public class TemplateStatisticsDto
{
    public int TemplateId { get; set; }
    
    public string TemplateName { get; set; } = string.Empty;
    
    public int TotalSongs { get; set; }
    
    public int EstimatedDurationMinutes { get; set; }
    
    public int UsageCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public int? AverageBpm { get; set; }
    
    public Dictionary<string, int> GenreDistribution { get; set; } = new();
    
    public Dictionary<string, int> KeyDistribution { get; set; } = new();
}

/// <summary>
/// Response DTO for paginated template list
/// </summary>
public class TemplateListResponse
{
    public List<TemplateDto> Templates { get; set; } = new();
    
    public int TotalCount { get; set; }
    
    public int PageNumber { get; set; }
    
    public int PageSize { get; set; }
    
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    public bool HasPreviousPage => PageNumber > 1;
    
    public bool HasNextPage => PageNumber < TotalPages;
}
