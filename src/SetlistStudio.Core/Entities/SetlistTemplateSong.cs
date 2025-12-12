using System.ComponentModel.DataAnnotations;
using SetlistStudio.Core.Validation;

namespace SetlistStudio.Core.Entities;

/// <summary>
/// Junction table linking songs to setlist templates with ordering and notes
/// Enables many-to-many relationship between songs and templates
/// WORKS Principle: Defines the structure and order of songs in a template
/// </summary>
public class SetlistTemplateSong
{
    /// <summary>
    /// Unique identifier for this template-song relationship
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the template this song belongs to
    /// SECURE Principle: Links to template for authorization checks
    /// </summary>
    [Required]
    public int SetlistTemplateId { get; set; }

    /// <summary>
    /// Navigation property to the template
    /// </summary>
    public virtual SetlistTemplate SetlistTemplate { get; set; } = null!;

    /// <summary>
    /// Foreign key to the song in this template
    /// SECURE Principle: Song must belong to template owner
    /// </summary>
    [Required]
    public int SongId { get; set; }

    /// <summary>
    /// Navigation property to the song
    /// </summary>
    public virtual Song Song { get; set; } = null!;

    /// <summary>
    /// Order position of the song in the template (1-based)
    /// WORKS Principle: Defines song order in template blueprint
    /// </summary>
    [Required]
    [Range(1, 1000)]
    public int Position { get; set; }

    /// <summary>
    /// Template-specific notes for this song
    /// USER DELIGHT: Guidance for using this song in the template context
    /// Examples: "Acoustic version", "Extended solo", "Crowd favorite", "Energy builder"
    /// </summary>
    [StringLength(500)]
    [SanitizedString(AllowHtml = false, AllowLineBreaks = true, MaxLength = 500)]
    public string? Notes { get; set; }
}
