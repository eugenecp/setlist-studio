using Microsoft.EntityFrameworkCore;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Implementation of audit logging service for tracking data changes and security events
/// with HTTP context enhancement for IP address and user agent tracking
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly SetlistStudioDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        SetlistStudioDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditLogService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAuditAsync(string action, string tableName, string recordId, string userId, object? changes, string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty", nameof(action));
        
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
        
        if (string.IsNullOrWhiteSpace(recordId))
            throw new ArgumentException("Record ID cannot be null or empty", nameof(recordId));
        
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = tableName,
                EntityId = recordId,
                UserId = userId,
                NewValues = changes != null ? JsonSerializer.Serialize(changes, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) : null,
                Timestamp = DateTime.UtcNow,
                CorrelationId = correlationId ?? Guid.NewGuid().ToString()
            };

            // Enhance with HTTP context information if available
            EnhanceAuditLogWithHttpContext(auditLog);

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Audit log created: {Action} on {TableName} {RecordId} by user {UserId}", 
                action, tableName, recordId, userId);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating audit log: {Action} on {TableName} {RecordId} by user {UserId}", 
                action, tableName, recordId, userId);
            // Don't throw - audit logging failures should not break the main operation
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument creating audit log: {Action} on {TableName} {RecordId} by user {UserId}", 
                action, tableName, recordId, userId);
            // Don't throw - audit logging failures should not break the main operation
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "Context disposed error creating audit log: {Action} on {TableName} {RecordId} by user {UserId}", 
                action, tableName, recordId, userId);
            // Don't throw - audit logging failures should not break the main operation
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        string? userId = null,
        string? action = null,
        string? tableName = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(a => a.UserId == userId);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(a => a.Action == action);

            if (!string.IsNullOrWhiteSpace(tableName))
                query = query.Where(a => a.EntityType == tableName);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation retrieving audit logs");
            return Enumerable.Empty<AuditLog>();
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument retrieving audit logs");
            return Enumerable.Empty<AuditLog>();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogsByRecordAsync(string tableName, string recordId)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
        
        if (string.IsNullOrWhiteSpace(recordId))
            throw new ArgumentException("Record ID cannot be null or empty", nameof(recordId));

        try
        {
            return await _context.AuditLogs
                .Where(a => a.EntityType == tableName && a.EntityId == recordId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error retrieving audit logs for {TableName} {RecordId}", tableName, recordId);
            return Enumerable.Empty<AuditLog>();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation retrieving audit logs for {TableName} {RecordId}", tableName, recordId);
            return Enumerable.Empty<AuditLog>();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogsByCorrelationIdAsync(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

        try
        {
            return await _context.AuditLogs
                .Where(a => a.CorrelationId == correlationId)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error retrieving audit logs for correlation ID {CorrelationId}", correlationId);
            return Enumerable.Empty<AuditLog>();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation retrieving audit logs for correlation ID {CorrelationId}", correlationId);
            return Enumerable.Empty<AuditLog>();
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteAuditLogsOlderThanAsync(DateTime cutoffDate)
    {
        try
        {
            var oldLogs = await _context.AuditLogs
                .Where(a => a.Timestamp < cutoffDate)
                .ToListAsync();

            if (oldLogs.Any())
            {
                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted {Count} audit logs older than {CutoffDate}", 
                    oldLogs.Count, cutoffDate);
            }

            return oldLogs.Count;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error deleting old audit logs older than {CutoffDate}", cutoffDate);
            return 0;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting old audit logs older than {CutoffDate}", cutoffDate);
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation deleting old audit logs older than {CutoffDate}", cutoffDate);
            return 0;
        }
    }

    /// <summary>
    /// Enhances the audit log with HTTP context information if available
    /// </summary>
    /// <param name="auditLog">The audit log to enhance</param>
    private void EnhanceAuditLogWithHttpContext(AuditLog auditLog)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        try
        {
            // Extract IP address (handle proxy scenarios)
            auditLog.IpAddress = GetClientIpAddress(httpContext);

            // Extract user agent
            auditLog.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();

            // Extract session ID if available
            if (httpContext.Session != null)
            {
                auditLog.SessionId = httpContext.Session.Id;
            }

            // Enhance user ID from claims if not already set
            if (string.IsNullOrEmpty(auditLog.UserId) && httpContext.User?.Identity?.IsAuthenticated == true)
            {
                auditLog.UserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument enhancing audit log with HTTP context information");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation enhancing audit log with HTTP context information");
        }
    }

    /// <summary>
    /// Extracts the client IP address from HTTP context, handling proxy scenarios
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <returns>Client IP address</returns>
    private static string GetClientIpAddress(HttpContext httpContext)
    {
        // Check X-Forwarded-For header (common in load balancer scenarios)
        var xForwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            // Take the first IP address from the comma-separated list
            return xForwardedFor.Split(',')[0].Trim();
        }

        // Check X-Real-IP header (used by some reverse proxies)
        var xRealIp = httpContext.Request.Headers["X-Real-IP"].ToString();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        // Check CF-Connecting-IP header (Cloudflare)
        var cfConnectingIp = httpContext.Request.Headers["CF-Connecting-IP"].ToString();
        if (!string.IsNullOrEmpty(cfConnectingIp))
        {
            return cfConnectingIp;
        }

        // Fall back to remote IP address
        return httpContext.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}