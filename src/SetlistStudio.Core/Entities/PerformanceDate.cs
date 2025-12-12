using System.ComponentModel.DataAnnotations;
using SetlistStudio.Core.Validation;

namespace SetlistStudio.Core.Entities;

/// <summary>
/// Represents a scheduled performance date for a setlist
/// Allows a single setlist to be performed multiple times at different dates/venues
/// </summary>
public class PerformanceDate
{
    /// <summary>
    /// Unique identifier for the performance date
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the parent setlist
    /// </summary>
    [Required]
    public int SetlistId { get; set; }

    /// <summary>
    /// The specific date and time of the performance
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// User ID for authorization - inherited from parent setlist
    /// Ensures user-specific data isolation
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Optional venue override for this specific performance date
    /// If null, uses the venue from the parent setlist
    /// </summary>
    [StringLength(200)]
    [SanitizedString(AllowHtml = false, AllowSpecialCharacters = true, MaxLength = 200)]
    public string? Venue { get; set; }

    /// <summary>
    /// Optional notes specific to this performance
    /// (e.g., "Opening act for headliner", "Extended set with encores")
    /// </summary>
    [StringLength(1000)]
    [SanitizedString(AllowHtml = false, AllowLineBreaks = true, MaxLength = 1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// When this performance date was scheduled
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this performance date was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent setlist
    /// </summary>
    public virtual Setlist Setlist { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;
}
