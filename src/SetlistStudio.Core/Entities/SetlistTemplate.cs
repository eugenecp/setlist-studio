using System.ComponentModel.DataAnnotations;
using SetlistStudio.Core.Validation;

namespace SetlistStudio.Core.Entities;

/// <summary>
/// Represents a reusable setlist template for common performance scenarios
/// Templates are blueprints that can be converted to actual setlists
/// Examples: "Wedding Set - Classic Rock", "Friday Night Blues", "Jazz Standards"
/// </summary>
public class SetlistTemplate
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the template (e.g., "Wedding Reception - Classic Rock")
    /// WORKS Principle: Clear, descriptive names help musicians find templates
    /// </summary>
    [Required]
    [StringLength(200)]
    [SanitizedString(AllowHtml = false, AllowSpecialCharacters = true, MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the template's purpose and content
    /// USER DELIGHT: Helps musicians understand when to use this template
    /// </summary>
    [StringLength(500)]
    [SanitizedString(AllowHtml = false, AllowLineBreaks = true, MaxLength = 500)]
    public string? Description { get; set; }

    /// <summary>
    /// Category for organizing templates (e.g., "Wedding", "Bar Gig", "Concert", "Practice")
    /// SCALE Principle: Enables efficient filtering and discovery
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Foreign key to the user who owns this template
    /// SECURE Principle: CRITICAL for authorization - templates belong to specific users
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this template is publicly visible to other users
    /// SECURE Principle: Default is false (private) - sharing is opt-in
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Estimated total duration in minutes for the template
    /// USER DELIGHT: Helps musicians plan performance timing
    /// </summary>
    public int EstimatedDurationMinutes { get; set; }

    /// <summary>
    /// Number of times this template has been used to create setlists
    /// USER DELIGHT: Analytics for popular templates, usage tracking
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// When the template was created
    /// MAINTAINABLE Principle: Audit trail for template lifecycle
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the template was last updated
    /// MAINTAINABLE Principle: Track template modifications over time
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the user who owns this template
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the songs in this template (with positions and notes)
    /// WORKS Principle: Ordered song list defines the template structure
    /// </summary>
    public virtual ICollection<SetlistTemplateSong> Songs { get; set; } = new List<SetlistTemplateSong>();

    /// <summary>
    /// Navigation property to setlists created from this template
    /// USER DELIGHT: Track which performances used this template
    /// </summary>
    public virtual ICollection<Setlist> GeneratedSetlists { get; set; } = new List<Setlist>();

    /// <summary>
    /// Helper property to get total song count
    /// USER DELIGHT: Quick metrics for template overview
    /// </summary>
    public int TotalSongs => Songs.Count;

    /// <summary>
    /// Helper property to get calculated duration from actual song durations
    /// USER DELIGHT: Accurate duration calculation from song metadata
    /// </summary>
    public int CalculatedDurationMinutes => Songs
        .Where(ts => ts.Song?.DurationSeconds != null)
        .Sum(ts => ts.Song!.DurationSeconds!.Value) / 60;
}
