using System.ComponentModel.DataAnnotations;
using SetlistStudio.Core.Validation;

namespace SetlistStudio.Core.Entities;

/// <summary>
/// Represents a setlist for a performance
/// Contains ordered songs with performance metadata
/// </summary>
public class Setlist
{
    /// <summary>
    /// Unique identifier for the setlist
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the setlist (e.g., "Wedding Reception Set", "Rock Concert Main Set")
    /// </summary>
    [Required]
    [StringLength(200)]
    [SanitizedString(AllowHtml = false, AllowSpecialCharacters = true, MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the setlist
    /// </summary>
    [StringLength(1000)]
    [SanitizedString(AllowHtml = false, AllowLineBreaks = true, MaxLength = 1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Performance venue or event name
    /// </summary>
    [StringLength(200)]
    [SanitizedString(AllowHtml = false, AllowSpecialCharacters = true, MaxLength = 200)]
    public string? Venue { get; set; }

    /// <summary>
    /// Date and time of the performance
    /// </summary>
    public DateTime? PerformanceDate { get; set; }

    /// <summary>
    /// Expected total duration in minutes
    /// </summary>
    public int? ExpectedDurationMinutes { get; set; }

    /// <summary>
    /// Whether this setlist is marked as template for reuse
    /// </summary>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// Whether this setlist is currently active/in use
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Performance notes (sound check info, special instructions, etc.)
    /// </summary>
    [StringLength(2000)]
    public string? PerformanceNotes { get; set; }

    /// <summary>
    /// When the setlist was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the setlist was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Foreign key to the user who owns this setlist
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the songs in this setlist (ordered)
    /// </summary>
    public virtual ICollection<SetlistSong> SetlistSongs { get; set; } = new List<SetlistSong>();

    /// <summary>
    /// Helper property to get total estimated duration
    /// </summary>
    public int CalculatedDurationMinutes => SetlistSongs
        .Where(ss => ss.Song.DurationSeconds.HasValue)
        .Sum(ss => ss.Song.DurationSeconds.GetValueOrDefault()) / 60;

    /// <summary>
    /// Helper property to get song count
    /// </summary>
    public int SongCount => SetlistSongs.Count;

    /// <summary>
    /// Helper property to check if setlist is ready for performance
    /// </summary>
    public bool IsReadyForPerformance => SongCount > 0 && 
                                        PerformanceDate.HasValue && 
                                        !string.IsNullOrEmpty(Venue);
}