using System.ComponentModel.DataAnnotations;

namespace SetlistStudio.Core.Entities;

/// <summary>
/// Junction table linking songs to setlists with ordering and performance metadata
/// Enables many-to-many relationship between songs and setlists
/// </summary>
public class SetlistSong
{
    /// <summary>
    /// Unique identifier for this setlist-song relationship
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Order position of the song in the setlist (1-based)
    /// </summary>
    [Required]
    [Range(1, 1000)]
    public int Position { get; set; }

    /// <summary>
    /// Optional transition notes between this song and the next
    /// (e.g., "Direct into...", "2 minute break", "Key change from C to F")
    /// </summary>
    [StringLength(500)]
    public string? TransitionNotes { get; set; }

    /// <summary>
    /// Performance-specific notes for this song in this setlist
    /// (e.g., "Play acoustic version", "Skip second verse", "Crowd participation")
    /// </summary>
    [StringLength(1000)]
    public string? PerformanceNotes { get; set; }

    /// <summary>
    /// Whether this song is marked as an encore song
    /// </summary>
    public bool IsEncore { get; set; }

    /// <summary>
    /// Whether this song is optional (can be skipped if running long)
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Custom tempo for this performance (overrides song's default BPM)
    /// </summary>
    [Range(40, 250)]
    public int? CustomBpm { get; set; }

    /// <summary>
    /// Custom key for this performance (overrides song's default key)
    /// </summary>
    [StringLength(10)]
    public string? CustomKey { get; set; }

    /// <summary>
    /// When this song was added to the setlist
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Foreign key to the setlist
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "SetlistId must be a positive integer")]
    public int SetlistId { get; set; }

    /// <summary>
    /// Navigation property to the setlist
    /// </summary>
    public virtual Setlist Setlist { get; set; } = null!;

    /// <summary>
    /// Foreign key to the song
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "SongId must be a positive integer")]
    public int SongId { get; set; }

    /// <summary>
    /// Navigation property to the song
    /// </summary>
    public virtual Song Song { get; set; } = null!;

    /// <summary>
    /// Helper property to get effective BPM (custom or song default)
    /// </summary>
    public int? EffectiveBpm => CustomBpm ?? Song?.Bpm;

    /// <summary>
    /// Helper property to get effective key (custom or song default)
    /// </summary>
    public string? EffectiveKey => string.IsNullOrEmpty(CustomKey) ? Song?.MusicalKey : CustomKey;

    /// <summary>
    /// Helper property to check if this song has custom performance settings
    /// </summary>
    public bool HasCustomSettings => CustomBpm.HasValue || 
                                    !string.IsNullOrEmpty(CustomKey) || 
                                    !string.IsNullOrEmpty(PerformanceNotes);
}