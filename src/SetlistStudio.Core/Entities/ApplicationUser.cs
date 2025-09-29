using Microsoft.AspNetCore.Identity;

namespace SetlistStudio.Core.Entities;

/// <summary>
/// Represents a user in the Setlist Studio application
/// Extends IdentityUser to support OAuth authentication providers
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// The user's display name from their OAuth provider
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// URL to the user's profile picture from their OAuth provider
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// OAuth provider used for authentication (Google, Microsoft, Facebook)
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Provider-specific user ID
    /// </summary>
    public string? ProviderKey { get; set; }

    /// <summary>
    /// When the user first registered
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user last updated their profile
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the user's songs
    /// </summary>
    public virtual ICollection<Song> Songs { get; set; } = new List<Song>();

    /// <summary>
    /// Navigation property to the user's setlists
    /// </summary>
    public virtual ICollection<Setlist> Setlists { get; set; } = new List<Setlist>();
}