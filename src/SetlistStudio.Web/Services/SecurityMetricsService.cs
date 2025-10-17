using System.Collections.Concurrent;
using System.Text.Json;

namespace SetlistStudio.Web.Services;

/// <summary>
/// Centralized security metrics collection service for monitoring and alerting.
/// Tracks authentication failures, rate limit violations, suspicious activities, and security events.
/// Provides real-time security metrics for operational visibility and incident response.
/// </summary>
public interface ISecurityMetricsService
{
    /// <summary>
    /// Records an authentication failure event.
    /// </summary>
    void RecordAuthenticationFailure(string username, string ipAddress, string userAgent);

    /// <summary>
    /// Records a rate limit violation event.
    /// </summary>
    void RecordRateLimitViolation(string ipAddress, string endpoint, string userAgent);

    /// <summary>
    /// Records a suspicious activity event.
    /// </summary>
    void RecordSuspiciousActivity(string activityType, string ipAddress, string details);

    /// <summary>
    /// Records a security event (CSP violation, security header missing, etc.).
    /// </summary>
    void RecordSecurityEvent(string eventType, string severity, string details);

    /// <summary>
    /// Gets current security metrics snapshot.
    /// </summary>
    SecurityMetricsSnapshot GetMetricsSnapshot();

    /// <summary>
    /// Gets detailed security metrics with filtering options.
    /// </summary>
    DetailedSecurityMetrics GetDetailedMetrics(DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// Gets security metrics for a specific time period.
    /// </summary>
    SecurityMetricsPeriod GetMetricsForPeriod(TimeSpan period);

    /// <summary>
    /// Clears old metrics data (for maintenance).
    /// </summary>
    void ClearOldMetrics(TimeSpan olderThan);
}

public class SecurityMetricsService : ISecurityMetricsService
{
    private readonly ILogger<SecurityMetricsService> _logger;
    private readonly IConfiguration _configuration;
    
    // Thread-safe collections for metrics storage
    private readonly ConcurrentQueue<SecurityEvent> _recentEvents = new();
    private readonly ConcurrentDictionary<string, int> _authenticationFailures = new();
    private readonly ConcurrentDictionary<string, int> _rateLimitViolations = new();
    private readonly ConcurrentDictionary<string, int> _suspiciousActivities = new();
    private readonly ConcurrentDictionary<string, int> _securityEventCounts = new();
    private volatile int _directSecurityEvents = 0; // Direct calls to RecordSecurityEvent
    
    // Performance tracking
    private volatile int _totalEvents = 0;
    private DateTime _lastEventTime = DateTime.UtcNow;
    private readonly object _metricsLock = new object();

    public SecurityMetricsService(ILogger<SecurityMetricsService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        // Start background cleanup task
        _ = Task.Run(StartMetricsCleanupTask);
    }

    public void RecordAuthenticationFailure(string username, string ipAddress, string userAgent)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = "AUTHENTICATION_FAILURE",
            Timestamp = DateTime.UtcNow,
            Severity = "HIGH",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = new { Username = username, IpAddress = ipAddress }
        };

        RecordEvent(securityEvent);
        
        // Track by IP address for pattern analysis
        var key = $"auth_fail_{ipAddress}";
        _authenticationFailures.AddOrUpdate(key, 1, (k, v) => v + 1);

        // Log security event
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "AUTHENTICATION_FAILURE",
            ["Username"] = username,
            ["IPAddress"] = ipAddress,
            ["UserAgent"] = userAgent,
            ["Timestamp"] = DateTime.UtcNow
        });

        _logger.LogWarning("Authentication failure recorded for user {Username} from IP {IPAddress}", 
            username, ipAddress);

        // Check for brute force patterns
        CheckForBruteForcePattern(ipAddress, username);
    }

    public void RecordRateLimitViolation(string ipAddress, string endpoint, string userAgent)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = "RATE_LIMIT_VIOLATION",
            Timestamp = DateTime.UtcNow,
            Severity = "MEDIUM",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = new { IpAddress = ipAddress, Endpoint = endpoint, UserAgent = userAgent }
        };

        RecordEvent(securityEvent);
        
        // Track by IP and endpoint
        var key = $"rate_limit_{ipAddress}_{endpoint}";
        _rateLimitViolations.AddOrUpdate(key, 1, (k, v) => v + 1);

        _logger.LogWarning("Rate limit violation recorded for IP {IPAddress} on endpoint {Endpoint}", 
            ipAddress, endpoint);

        // Check for denial of service patterns
        CheckForDosPattern(ipAddress, endpoint);
    }

    public void RecordSuspiciousActivity(string activityType, string ipAddress, string details)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = "SUSPICIOUS_ACTIVITY",
            Timestamp = DateTime.UtcNow,
            Severity = "HIGH",
            IpAddress = ipAddress,
            Details = new { ActivityType = activityType, IpAddress = ipAddress, Details = details }
        };

        RecordEvent(securityEvent);
        
        // Track by activity type and IP
        var key = $"suspicious_{activityType}_{ipAddress}";
        _suspiciousActivities.AddOrUpdate(key, 1, (k, v) => v + 1);

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "SUSPICIOUS_ACTIVITY",
            ["ActivityType"] = activityType,
            ["IPAddress"] = ipAddress,
            ["Details"] = details
        });

        _logger.LogError("Suspicious activity detected: {ActivityType} from IP {IPAddress}. Details: {Details}", 
            activityType, ipAddress, details);
    }

    public void RecordSecurityEvent(string eventType, string severity, string details)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Severity = severity,
            Details = new { EventType = eventType, Severity = severity, Details = details }
        };

        RecordEvent(securityEvent);
        Interlocked.Increment(ref _directSecurityEvents);

        var logLevel = severity.ToUpperInvariant() switch
        {
            "CRITICAL" => LogLevel.Critical,
            "HIGH" => LogLevel.Error,
            "MEDIUM" => LogLevel.Warning,
            "LOW" => LogLevel.Information,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, "Security event recorded: {EventType} (Severity: {Severity}). Details: {Details}", 
            eventType, severity, details);
    }

    public SecurityMetricsSnapshot GetMetricsSnapshot()
    {
        lock (_metricsLock)
        {
            var recentEvents = _recentEvents.ToArray().Take(100).ToArray();
            
            return new SecurityMetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                TotalEvents = _totalEvents,
                LastEventTime = _lastEventTime,
                AuthenticationFailures = _authenticationFailures.Count,
                RateLimitViolations = _rateLimitViolations.Count,
                SuspiciousActivities = _suspiciousActivities.Count,
                SecurityEvents = _directSecurityEvents,
                RecentEvents = recentEvents,
                TopFailingIps = GetTopFailingIPs(10),
                TopViolatedEndpoints = GetTopViolatedEndpoints(10),
                SecurityEventsByType = _securityEventCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
    }

    public DetailedSecurityMetrics GetDetailedMetrics(DateTime? startTime = null, DateTime? endTime = null)
    {
        startTime ??= DateTime.UtcNow.AddHours(-24); // Default to last 24 hours
        endTime ??= DateTime.UtcNow;

        var filteredEvents = _recentEvents
            .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
            .ToArray();

        return new DetailedSecurityMetrics
        {
            StartTime = startTime.Value,
            EndTime = endTime.Value,
            TotalEventsInPeriod = filteredEvents.Length,
            Events = filteredEvents,
            EventsByHour = GetEventsByHour(filteredEvents, startTime.Value, endTime.Value),
            EventsBySeverity = GetEventsBySeverity(filteredEvents),
            EventsByType = GetEventsByType(filteredEvents),
            TopAttackingIPs = GetTopAttackingIPs(filteredEvents, 10),
            SecurityTrends = CalculateSecurityTrends(filteredEvents)
        };
    }

    public SecurityMetricsPeriod GetMetricsForPeriod(TimeSpan period)
    {
        var startTime = DateTime.UtcNow - period;
        var endTime = DateTime.UtcNow;

        var filteredEvents = _recentEvents
            .Where(e => e.Timestamp >= startTime)
            .ToArray();

        return new SecurityMetricsPeriod
        {
            Period = period,
            StartTime = startTime,
            EndTime = endTime,
            EventCount = filteredEvents.Length,
            AuthFailureRate = CalculateEventRate(filteredEvents, "AUTHENTICATION_FAILURE", period),
            RateLimitRate = CalculateEventRate(filteredEvents, "RATE_LIMIT_VIOLATION", period),
            SuspiciousActivityRate = CalculateEventRate(filteredEvents, "SUSPICIOUS_ACTIVITY", period),
            SecurityScore = CalculateSecurityScore(filteredEvents),
            ThreatLevel = CalculateThreatLevel(filteredEvents),
            Recommendations = GenerateSecurityRecommendations(filteredEvents)
        };
    }

    public void ClearOldMetrics(TimeSpan olderThan)
    {
        var cutoffTime = DateTime.UtcNow - olderThan;
        var eventsToKeep = new ConcurrentQueue<SecurityEvent>();
        
        while (_recentEvents.TryDequeue(out var securityEvent))
        {
            if (securityEvent.Timestamp > cutoffTime)
            {
                eventsToKeep.Enqueue(securityEvent);
            }
        }

        // Replace the queue with filtered events
        while (eventsToKeep.TryDequeue(out var eventToKeep))
        {
            _recentEvents.Enqueue(eventToKeep);
        }

        _logger.LogInformation("Cleared security metrics older than {OlderThan}", olderThan);
    }

    private void RecordEvent(SecurityEvent securityEvent)
    {
        _recentEvents.Enqueue(securityEvent);
        Interlocked.Increment(ref _totalEvents);
        _lastEventTime = securityEvent.Timestamp;

        // Update event type counts for SecurityEventsByType
        _securityEventCounts.AddOrUpdate(securityEvent.EventType, 1, (key, value) => value + 1);

        // Keep only recent events (last 1000)
        while (_recentEvents.Count > 1000)
        {
            _recentEvents.TryDequeue(out _);
        }
    }

    private void CheckForBruteForcePattern(string ipAddress, string username)
    {
        var key = $"auth_fail_{ipAddress}";
        if (_authenticationFailures.TryGetValue(key, out var failureCount))
        {
            var threshold = _configuration.GetValue<int>("Security:BruteForceThreshold", 5);
            if (failureCount >= threshold)
            {
                RecordSuspiciousActivity("BRUTE_FORCE_ATTEMPT", ipAddress, 
                    $"Multiple authentication failures ({failureCount}) for user {username}");
            }
        }
    }

    private void CheckForDosPattern(string ipAddress, string endpoint)
    {
        var key = $"rate_limit_{ipAddress}_{endpoint}";
        if (_rateLimitViolations.TryGetValue(key, out var violationCount))
        {
            var threshold = _configuration.GetValue<int>("Security:DosThreshold", 10);
            if (violationCount >= threshold)
            {
                RecordSuspiciousActivity("DOS_ATTEMPT", ipAddress, 
                    $"Multiple rate limit violations ({violationCount}) on endpoint {endpoint}");
            }
        }
    }

    private string[] GetTopFailingIPs(int count)
    {
        return _authenticationFailures
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => kvp.Key.Replace("auth_fail_", ""))
            .ToArray();
    }

    private string[] GetTopViolatedEndpoints(int count)
    {
        return _rateLimitViolations
            .GroupBy(kvp => kvp.Key.Split('_').Last()) // Extract endpoint from key
            .OrderByDescending(g => g.Sum(kvp => kvp.Value))
            .Take(count)
            .Select(g => g.Key)
            .ToArray();
    }

    private Dictionary<int, int> GetEventsByHour(SecurityEvent[] events, DateTime startTime, DateTime endTime)
    {
        return events
            .GroupBy(e => e.Timestamp.Hour)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private Dictionary<string, int> GetEventsBySeverity(SecurityEvent[] events)
    {
        return events
            .GroupBy(e => e.Severity)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private Dictionary<string, int> GetEventsByType(SecurityEvent[] events)
    {
        return events
            .GroupBy(e => e.EventType)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private string[] GetTopAttackingIPs(SecurityEvent[] events, int count)
    {
        return events
            .Where(e => !string.IsNullOrEmpty(e.IpAddress))
            .GroupBy(e => e.IpAddress)
            .OrderByDescending(g => g.Count())
            .Take(count)
            .Select(g => g.Key!)
            .ToArray();
    }

    private SecurityTrends CalculateSecurityTrends(SecurityEvent[] events)
    {
        var last24Hours = events.Where(e => e.Timestamp >= DateTime.UtcNow.AddHours(-24)).Count();
        var previous24Hours = events.Where(e => e.Timestamp >= DateTime.UtcNow.AddHours(-48) && 
                                               e.Timestamp < DateTime.UtcNow.AddHours(-24)).Count();

        var trendPercentage = previous24Hours == 0 ? 0 : 
            ((double)(last24Hours - previous24Hours) / previous24Hours) * 100;

        return new SecurityTrends
        {
            Last24Hours = last24Hours,
            Previous24Hours = previous24Hours,
            TrendPercentage = trendPercentage,
            IsIncreasing = trendPercentage > 10,
            IsDecreasing = trendPercentage < -10
        };
    }

    private double CalculateEventRate(SecurityEvent[] events, string eventType, TimeSpan period)
    {
        var eventCount = events.Count(e => e.EventType == eventType);
        return eventCount / period.TotalHours; // Events per hour
    }

    private double CalculateSecurityScore(SecurityEvent[] events)
    {
        // Calculate security score (0-100, where 100 is best)
        var baseScore = 100.0;
        var criticalEvents = events.Count(e => e.Severity == "CRITICAL");
        var highEvents = events.Count(e => e.Severity == "HIGH");
        var mediumEvents = events.Count(e => e.Severity == "MEDIUM");

        baseScore -= criticalEvents * 10; // Critical events heavily impact score
        baseScore -= highEvents * 5;     // High events moderately impact score
        baseScore -= mediumEvents * 2;   // Medium events lightly impact score

        return Math.Max(0, Math.Min(100, baseScore));
    }

    private string CalculateThreatLevel(SecurityEvent[] events)
    {
        var criticalCount = events.Count(e => e.Severity == "CRITICAL");
        var highCount = events.Count(e => e.Severity == "HIGH");

        if (criticalCount > 0 || highCount > 10) return "CRITICAL";
        if (highCount > 5) return "HIGH";
        if (highCount > 0 || events.Count(e => e.Severity == "MEDIUM") > 20) return "MEDIUM";
        return "LOW";
    }

    private string[] GenerateSecurityRecommendations(SecurityEvent[] events)
    {
        var recommendations = new List<string>();

        var authFailures = events.Count(e => e.EventType == "AUTHENTICATION_FAILURE");
        if (authFailures > 10)
        {
            recommendations.Add("Consider implementing additional authentication controls (2FA, account lockout)");
        }

        var rateLimitViolations = events.Count(e => e.EventType == "RATE_LIMIT_VIOLATION");
        if (rateLimitViolations > 50)
        {
            recommendations.Add("Review and tighten rate limiting policies");
        }

        var suspiciousActivities = events.Count(e => e.EventType == "SUSPICIOUS_ACTIVITY");
        if (suspiciousActivities > 5)
        {
            recommendations.Add("Investigate suspicious IP addresses and consider blocking repeat offenders");
        }

        var cspViolations = events.Count(e => e.EventType == "CSP_VIOLATION");
        if (cspViolations > 20)
        {
            recommendations.Add("Review Content Security Policy configuration and investigate potential XSS attempts");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Security posture is good. Continue monitoring for anomalies.");
        }

        return recommendations.ToArray();
    }

    private async Task StartMetricsCleanupTask()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1)); // Run cleanup every hour
                ClearOldMetrics(TimeSpan.FromDays(7)); // Keep 7 days of metrics
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during security metrics cleanup");
            }
        }
    }
}

#region Data Models

public class SecurityEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public object? Details { get; set; }
}

public class SecurityMetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public int TotalEvents { get; set; }
    public DateTime LastEventTime { get; set; }
    public int AuthenticationFailures { get; set; }
    public int RateLimitViolations { get; set; }
    public int SuspiciousActivities { get; set; }
    public int SecurityEvents { get; set; }
    public SecurityEvent[] RecentEvents { get; set; } = Array.Empty<SecurityEvent>();
    public string[] TopFailingIps { get; set; } = Array.Empty<string>();
    public string[] TopViolatedEndpoints { get; set; } = Array.Empty<string>();
    public Dictionary<string, int> SecurityEventsByType { get; set; } = new();
}

public class DetailedSecurityMetrics
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalEventsInPeriod { get; set; }
    public SecurityEvent[] Events { get; set; } = Array.Empty<SecurityEvent>();
    public Dictionary<int, int> EventsByHour { get; set; } = new();
    public Dictionary<string, int> EventsBySeverity { get; set; } = new();
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public string[] TopAttackingIPs { get; set; } = Array.Empty<string>();
    public SecurityTrends SecurityTrends { get; set; } = new();
}

public class SecurityMetricsPeriod
{
    public TimeSpan Period { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int EventCount { get; set; }
    public double AuthFailureRate { get; set; }
    public double RateLimitRate { get; set; }
    public double SuspiciousActivityRate { get; set; }
    public double SecurityScore { get; set; }
    public string ThreatLevel { get; set; } = string.Empty;
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

public class SecurityTrends
{
    public int Last24Hours { get; set; }
    public int Previous24Hours { get; set; }
    public double TrendPercentage { get; set; }
    public bool IsIncreasing { get; set; }
    public bool IsDecreasing { get; set; }
}

#endregion