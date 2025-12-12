using System.ComponentModel.DataAnnotations;
using SetlistStudio.Core.Validation;

namespace SetlistStudio.Web.Models;

/// <summary>
/// Request model for creating a setlist from a template
/// </summary>
public class CreateFromTemplateRequest
{
    /// <summary>
    /// Name for the new setlist (e.g., "Smith Wedding - June 2025")
    /// </summary>
    [Required]
    [StringLength(200)]
    [SanitizedString(AllowHtml = false, AllowSpecialCharacters = true, MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Performance date and time
    /// </summary>
    public DateTime? PerformanceDate { get; set; }

    /// <summary>
    /// Performance venue or event location
    /// </summary>
    [StringLength(200)]
    [SanitizedString(AllowHtml = false, AllowSpecialCharacters = true, MaxLength = 200)]
    public string? Venue { get; set; }

    /// <summary>
    /// Optional notes for this specific performance
    /// </summary>
    [StringLength(2000)]
    [SanitizedString(AllowHtml = false, AllowLineBreaks = true, MaxLength = 2000)]
    public string? PerformanceNotes { get; set; }
}
