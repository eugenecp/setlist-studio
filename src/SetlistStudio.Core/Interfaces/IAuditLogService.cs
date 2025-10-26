using SetlistStudio.Core.Entities;

namespace SetlistStudio.Core.Interfaces;

/// <summary>
/// Interface for comprehensive audit logging service with HTTP context enhancement
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Log an audit entry with comprehensive details
    /// </summary>
    /// <param name="action">The action performed (CREATE, UPDATE, DELETE, etc.)</param>
    /// <param name="tableName">Name of the table/entity affected</param>
    /// <param name="recordId">ID of the record affected</param>
    /// <param name="userId">User who performed the action</param>
    /// <param name="changes">The changes made (will be JSON serialized)</param>
    /// <param name="correlationId">Optional correlation ID for grouping related operations</param>
    /// <returns>Task representing the async operation</returns>
    Task LogAuditAsync(string action, string tableName, string recordId, string userId, object? changes, string? correlationId = null);

    /// <summary>
    /// Log a system audit entry allowing empty userId (enables user enhancement from HTTP context)
    /// </summary>
    /// <param name="action">The action performed (CREATE, UPDATE, DELETE, etc.)</param>
    /// <param name="tableName">Name of the table/entity affected</param>
    /// <param name="recordId">ID of the record affected</param>
    /// <param name="changes">The changes made (will be JSON serialized)</param>
    /// <param name="correlationId">Optional correlation ID for grouping related operations</param>
    /// <returns>Task representing the async operation</returns>
    Task LogSystemAuditAsync(string action, string tableName, string recordId, object? changes = null, string? correlationId = null);

    /// <summary>
    /// Retrieve audit logs with filtering and pagination
    /// </summary>
    /// <param name="userId">Filter by user ID (optional)</param>
    /// <param name="action">Filter by action (optional)</param>
    /// <param name="tableName">Filter by table name (optional)</param>
    /// <param name="startDate">Filter by start date (optional)</param>
    /// <param name="endDate">Filter by end date (optional)</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <returns>Filtered audit logs</returns>
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        string? userId = null,
        string? action = null,
        string? tableName = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50);

    /// <summary>
    /// Get audit logs for a specific record
    /// </summary>
    /// <param name="tableName">Name of the table/entity</param>
    /// <param name="recordId">ID of the record</param>
    /// <returns>Audit history for the specified record</returns>
    Task<IEnumerable<AuditLog>> GetAuditLogsByRecordAsync(string tableName, string recordId);

    /// <summary>
    /// Get audit logs by correlation ID (for tracking related operations)
    /// </summary>
    /// <param name="correlationId">The correlation ID</param>
    /// <returns>Related audit logs</returns>
    Task<IEnumerable<AuditLog>> GetAuditLogsByCorrelationIdAsync(string correlationId);

    /// <summary>
    /// Delete audit logs older than the specified date (for cleanup)
    /// </summary>
    /// <param name="cutoffDate">Date before which logs should be deleted</param>
    /// <returns>Number of logs deleted</returns>
    Task<int> DeleteAuditLogsOlderThanAsync(DateTime cutoffDate);
}