using System.ComponentModel.DataAnnotations;

namespace SetlistStudio.Core.Entities;

/// <summary>
/// Represents an audit log entry for tracking data changes
/// Provides full audit trail for compliance and security monitoring
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The user ID who performed the action
    /// </summary>
    [Required]
    [StringLength(450)] // ASP.NET Identity default user ID length
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The username for display purposes
    /// </summary>
    [StringLength(256)]
    public string? UserName { get; set; }

    /// <summary>
    /// The type of entity that was modified (e.g., "Song", "Setlist")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the entity that was modified
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// The action performed (Create, Update, Delete)
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The old values before the change (JSON format)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// The new values after the change (JSON format)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// The primary key values (JSON format for composite keys)
    /// </summary>
    public string? PrimaryKeyValues { get; set; }

    /// <summary>
    /// IP address of the user who made the change
    /// </summary>
    [StringLength(45)] // IPv6 maximum length
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string for additional context
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional context or metadata about the change
    /// </summary>
    [StringLength(1000)]
    public string? AdditionalContext { get; set; }

    /// <summary>
    /// Session ID for tracking related actions
    /// </summary>
    [StringLength(100)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Correlation ID for tracking across services
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }
}