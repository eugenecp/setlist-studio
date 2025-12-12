using System.ComponentModel.DataAnnotations;
using SetlistStudio.Core.Security;

namespace SetlistStudio.Core.Entities;

/// <summary>
/// Reusable setlist template for recurring performance types
/// (e.g., "Wedding Ceremony", "Rock Bar Night", "Jazz Club Set")
/// </summary>
public class SetlistTemplate
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    [SafeString(MaxLength = 200, AllowEmpty = false)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    [SafeString(MaxLength = 1000, AllowEmpty = true)]
    public string? Description { get; set; }
    
    [StringLength(100)]
    [SafeString(MaxLength = 100, AllowEmpty = true)]
    public string? Category { get; set; } // "Wedding", "Rock Bar", "Jazz Club"
    
    public int? EstimatedDurationMinutes { get; set; }
    
    public bool IsPublic { get; set; } = false; // Future: template sharing
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<SetlistTemplateSong> TemplateSongs { get; set; } = new List<SetlistTemplateSong>();
}

/// <summary>
/// Junction table for many-to-many relationship between templates and songs
/// </summary>
public class SetlistTemplateSong
{
    public int Id { get; set; }
    public int SetlistTemplateId { get; set; }
    public int SongId { get; set; }
    public int Position { get; set; } // Order in template (1-based)
    
    // Navigation properties
    public SetlistTemplate Template { get; set; } = null!;
    public Song Song { get; set; } = null!;
}
