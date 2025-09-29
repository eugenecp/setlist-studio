using System.ComponentModel.DataAnnotations;

namespace SetlistStudio.Core.Entities;

/// <summary>
/// Represents a song in the user's music library
/// Contains metadata like BPM, key, and duration for performance planning
/// </summary>
public class Song
{
    /// <summary>
    /// Unique identifier for the song
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The song title (e.g., "Bohemian Rhapsody")
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The artist or band name (e.g., "Queen")
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// The album name (optional)
    /// </summary>
    [StringLength(200)]
    public string? Album { get; set; }

    /// <summary>
    /// Musical genre (e.g., "Rock", "Jazz", "Blues")
    /// </summary>
    [StringLength(50)]
    public string? Genre { get; set; }

    /// <summary>
    /// Beats per minute for tempo planning (40-250 BPM range)
    /// </summary>
    [Range(40, 250)]
    public int? Bpm { get; set; }

    /// <summary>
    /// Musical key (e.g., "C", "F#m", "Bb")
    /// </summary>
    [StringLength(10)]
    public string? MusicalKey { get; set; }

    /// <summary>
    /// Song duration in seconds
    /// </summary>
    [Range(1, 3600)] // 1 second to 1 hour
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// User notes about the song (lyrics, chords, performance notes)
    /// </summary>
    [StringLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Tags for categorizing songs (e.g., "wedding", "upbeat", "slow")
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Difficulty rating (1-5 scale)
    /// </summary>
    [Range(1, 5)]
    public int? DifficultyRating { get; set; }

    /// <summary>
    /// When the song was added to the library
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the song was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Foreign key to the user who owns this song
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Navigation property to setlist entries containing this song
    /// </summary>
    public virtual ICollection<SetlistSong> SetlistSongs { get; set; } = new List<SetlistSong>();

    /// <summary>
    /// Helper property to get formatted duration (MM:SS)
    /// </summary>
    public string FormattedDuration => DurationSeconds.HasValue 
        ? TimeSpan.FromSeconds(DurationSeconds.Value).ToString(@"mm\:ss")
        : "";

    /// <summary>
    /// Helper property to check if song has complete metadata
    /// </summary>
    public bool IsComplete => !string.IsNullOrEmpty(Title) && 
                             !string.IsNullOrEmpty(Artist) && 
                             Bpm.HasValue && 
                             !string.IsNullOrEmpty(MusicalKey);
}