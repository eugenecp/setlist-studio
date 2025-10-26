using Microsoft.EntityFrameworkCore;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Security;
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
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task LogAuditAsync(string action, string tableName, string recordId, string userId, object? changes, string? correlationId = null)
    {
        await LogAuditInternalAsync(action, tableName, recordId, userId, changes, correlationId, requireUserId: true);
    }

    /// <summary>
    /// Logs an audit event allowing empty userId for system operations (enables user enhancement from HTTP context)
    /// </summary>
    /// <param name="action">The action performed</param>
    /// <param name="tableName">The name of the table affected</param>
    /// <param name="recordId">The ID of the record affected</param>
    /// <param name="changes">The changes made (optional)</param>
    /// <param name="correlationId">Optional correlation ID for tracking related operations</param>
    public async Task LogSystemAuditAsync(string action, string tableName, string recordId, object? changes = null, string? correlationId = null)
    {
        await LogAuditInternalAsync(action, tableName, recordId, "", changes, correlationId, requireUserId: false);
    }

    /// <summary>
    /// Internal method to handle audit logging with optional user ID validation
    /// </summary>
    private async Task LogAuditInternalAsync(string action, string tableName, string recordId, string userId, object? changes, string? correlationId, bool requireUserId)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty", nameof(action));
        
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
        
        if (string.IsNullOrWhiteSpace(recordId))
            throw new ArgumentException("Record ID cannot be null or empty", nameof(recordId));
        
        if (requireUserId && string.IsNullOrWhiteSpace(userId))
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
            var query = BuildAuditLogQuery(userId, action, tableName, startDate, endDate);
            return await ExecutePagedQuery(query, pageNumber, pageSize);
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

    private IQueryable<AuditLog> BuildAuditLogQuery(
        string? userId, 
        string? action, 
        string? tableName, 
        DateTime? startDate, 
        DateTime? endDate)
    {
        var query = _context.AuditLogs.AsQueryable();

        query = ApplyUserFilter(query, userId);
        query = ApplyActionFilter(query, action);
        query = ApplyTableNameFilter(query, tableName);
        query = ApplyDateRangeFilters(query, startDate, endDate);

        return query;
    }

    private static IQueryable<AuditLog> ApplyUserFilter(IQueryable<AuditLog> query, string? userId)
    {
        return string.IsNullOrWhiteSpace(userId) 
            ? query 
            : query.Where(a => a != null && a.UserId == userId);
    }

    private static IQueryable<AuditLog> ApplyActionFilter(IQueryable<AuditLog> query, string? action)
    {
        return string.IsNullOrWhiteSpace(action) 
            ? query 
            : query.Where(a => a != null && a.Action == action);
    }

    private static IQueryable<AuditLog> ApplyTableNameFilter(IQueryable<AuditLog> query, string? tableName)
    {
        return string.IsNullOrWhiteSpace(tableName) 
            ? query 
            : query.Where(a => a != null && a.EntityType == tableName);
    }

    private static IQueryable<AuditLog> ApplyDateRangeFilters(IQueryable<AuditLog> query, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && startDate.Value != DateTime.MinValue)
            query = query.Where(a => a != null && a.Timestamp >= startDate!.Value);

        if (endDate.HasValue && endDate.Value != DateTime.MinValue)
            query = query.Where(a => a != null && a.Timestamp <= endDate!.Value);

        return query;
    }

    private static async Task<List<AuditLog>> ExecutePagedQuery(IQueryable<AuditLog> query, int pageNumber, int pageSize)
    {
        return await query
            .OrderByDescending(a => a!.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync() ?? new List<AuditLog>();
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
                .Where(a => a != null && a.EntityType == tableName && a.EntityId == recordId)
                .OrderByDescending(a => a != null ? a.Timestamp : DateTime.MinValue)
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
                .Where(a => a != null && a.CorrelationId == correlationId)
                .OrderBy(a => a != null ? a.Timestamp : DateTime.MinValue)
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
                .Where(a => a != null && a.Timestamp < cutoffDate)
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
            EnhanceWithNetworkInfo(auditLog, httpContext);
            EnhanceWithSessionInfo(auditLog, httpContext);
            EnhanceWithUserInfo(auditLog, httpContext);
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
    /// Enhances audit log with network-related information
    /// </summary>
    private static void EnhanceWithNetworkInfo(AuditLog auditLog, HttpContext httpContext)
    {
        auditLog.IpAddress = GetClientIpAddress(httpContext);
        auditLog.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();
    }

    /// <summary>
    /// Enhances audit log with session information
    /// </summary>
    private static void EnhanceWithSessionInfo(AuditLog auditLog, HttpContext httpContext)
    {
        if (httpContext.Session is not null)
        {
            auditLog.SessionId = httpContext.Session.Id;
        }
    }

    /// <summary>
    /// Enhances audit log with user information from claims
    /// </summary>
    private static void EnhanceWithUserInfo(AuditLog auditLog, HttpContext httpContext)
    {
        if (string.IsNullOrEmpty(auditLog.UserId) && httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var user = httpContext.User;
            if (user is not null)
            {
                auditLog.UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            }
        }
    }

    /// <summary>
    /// Extracts and sanitizes the client IP address from HTTP context, handling proxy scenarios
    /// and protecting user privacy by masking the last octet/segments
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <returns>Sanitized client IP address</returns>
    private static string GetClientIpAddress(HttpContext httpContext)
    {
        // Get raw IP address first
        var rawIp = GetRawClientIpAddress(httpContext);
        
        // Sanitize the IP address for privacy protection
        return SecureLoggingHelper.SanitizeIpAddress(rawIp);
    }

    /// <summary>
    /// Extracts the raw client IP address from HTTP context, handling proxy scenarios
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <returns>Raw client IP address</returns>
    private static string GetRawClientIpAddress(HttpContext httpContext)
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