using System.ComponentModel.DataAnnotations;

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
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Request model for updating a setlist
/// </summary>
public class UpdateSetlistRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
}