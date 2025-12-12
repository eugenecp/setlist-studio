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
/// Response model for performance date data
/// </summary>
public class PerformanceDateResponse
{
    public int Id { get; set; }
    
    public int SetlistId { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [StringLength(200)]
    public string? Venue { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public string? SetlistName { get; set; }
}

/// <summary>
/// Request model for creating a new performance date
/// </summary>
public class CreatePerformanceDateRequest
{
    [Required]
    public DateTime Date { get; set; }
    
    [StringLength(200)]
    [SafeString(MaxLength = 200, AllowEmpty = true, AllowSpecialCharacters = true)]
    public string? Venue { get; set; }
    
    [StringLength(1000)]
    [SafeString(MaxLength = 1000, AllowEmpty = true, AllowSpecialCharacters = true, AllowNewlines = true)]
    public string? Notes { get; set; }
}