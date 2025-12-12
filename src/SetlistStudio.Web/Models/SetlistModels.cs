using System.ComponentModel.DataAnnotations;
using SetlistStudio.Core.Security;

namespace SetlistStudio.Web.Models;

/// <summary>
/// Response model for setlist data
/// </summary>
public class SetlistResponse
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public int SongCount { get; set; }
}

/// <summary>
/// Request model for creating a new setlist
/// </summary>
public class CreateSetlistRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [SafeString(MaxLength = 100, AllowEmpty = false)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    [SafeString(MaxLength = 500, AllowEmpty = true)]
    public string? Description { get; set; }
}

/// <summary>
/// Request model for updating a setlist
/// </summary>
public class UpdateSetlistRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [SafeString(MaxLength = 100, AllowEmpty = false, AllowSpecialCharacters = true)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    [SafeString(MaxLength = 500, AllowEmpty = true, AllowSpecialCharacters = true, AllowNewlines = true)]
    public string? Description { get; set; }
}

/// <summary>
/// Complete setlist DTO with songs
/// </summary>
public class SetlistDto
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public DateTime? PerformanceDate { get; set; }
    
    public string? Venue { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public List<SetlistSongDto> Songs { get; set; } = new();
}

/// <summary>
/// Setlist song DTO with position and song details
/// </summary>
public class SetlistSongDto
{
    public int Id { get; set; }
    
    public int Position { get; set; }
    
    public string? Notes { get; set; }
    
    public SongDto Song { get; set; } = null!;
}

/// <summary>
/// Song DTO with all properties
/// </summary>
public class SongDto
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string Artist { get; set; } = string.Empty;
    
    public string? Album { get; set; }
    
    public int? DurationSeconds { get; set; }
    
    public int? Bpm { get; set; }
    
    public string? MusicalKey { get; set; }
    
    public string? Genre { get; set; }
    
    public string? Tags { get; set; }
}