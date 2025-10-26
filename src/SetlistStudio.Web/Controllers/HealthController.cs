using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SetlistStudio.Infrastructure.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SetlistStudio.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[EnableRateLimiting("ApiPolicy")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly SetlistStudioDbContext? _context;

    public HealthController(ILogger<HealthController> logger, SetlistStudioDbContext? context = null)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var healthStatus = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "Setlist Studio",
            Version = "1.0.0",
            Database = await CheckDatabaseHealth()
        };

        _logger.LogInformation("Health check requested - Status: {Status}, Database: {Database}", 
            healthStatus.Status, healthStatus.Database);
        
        return Ok(healthStatus);
    }

    [HttpGet("simple")]
    public IActionResult GetSimple()
    {
        // Simple health check without database dependency
        var healthStatus = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "Setlist Studio",
            Version = "1.0.0"
        };

        _logger.LogInformation("Simple health check requested - Status: Healthy");
        
        return Ok(healthStatus);
    }

    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var process = Process.GetCurrentProcess();
        var startTime = DateTime.UtcNow;
        
        var healthStatus = new
        {
            Status = "Healthy",
            Timestamp = startTime,
            Service = "Setlist Studio",
            Version = "1.0.0",
            Instance = new
            {
                Id = Environment.GetEnvironmentVariable("LoadBalancing__InstanceId") ?? Environment.MachineName,
                Name = Environment.GetEnvironmentVariable("LoadBalancing__InstanceName") ?? "Unknown Instance",
                IsLoadBalanced = bool.TryParse(Environment.GetEnvironmentVariable("LoadBalancing__IsLoadBalanced"), out var lb) && lb,
                ProcessId = process.Id,
                StartTime = process.StartTime,
                Uptime = DateTime.UtcNow - process.StartTime
            },
            System = new
            {
                Platform = RuntimeInformation.OSDescription,
                Framework = RuntimeInformation.FrameworkDescription,
                Architecture = RuntimeInformation.OSArchitecture.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = GC.GetTotalMemory(false),
                TotalMemory = GC.GetTotalMemory(true)
            },
            Performance = new
            {
                CpuUsage = await GetCpuUsageAsync(),
                MemoryUsage = new
                {
                    WorkingSetMB = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2),
                    PrivateMemoryMB = Math.Round(process.PrivateMemorySize64 / 1024.0 / 1024.0, 2),
                    VirtualMemoryMB = Math.Round(process.VirtualMemorySize64 / 1024.0 / 1024.0, 2),
                    GCMemoryMB = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2)
                },
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount
            },
            Database = await CheckDatabaseHealth(),
            LoadBalancer = new
            {
                Ready = true,
                CanAcceptTraffic = await CanAcceptTrafficAsync(),
                ResponseTime = (DateTime.UtcNow - startTime).TotalMilliseconds
            }
        };

        var overallStatus = healthStatus.LoadBalancer.CanAcceptTraffic ? "Healthy" : "Degraded";
        
        _logger.LogInformation("Detailed health check - Status: {Status}, Instance: {Instance}, CPU: {CPU}%, Memory: {Memory}MB", 
            overallStatus, 
            healthStatus.Instance.Id, 
            healthStatus.Performance.CpuUsage,
            healthStatus.Performance.MemoryUsage.WorkingSetMB);
        
        if (!healthStatus.LoadBalancer.CanAcceptTraffic)
        {
            return StatusCode(503, healthStatus); // Service Unavailable
        }
        
        return Ok(healthStatus);
    }

    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        // Readiness probe for load balancer
        var canAcceptTraffic = await CanAcceptTrafficAsync();
        var databaseStatus = await CheckDatabaseHealth();
        
        var readinessStatus = new
        {
            Ready = canAcceptTraffic && databaseStatus == "Connected",
            Timestamp = DateTime.UtcNow,
            Instance = Environment.GetEnvironmentVariable("LoadBalancing__InstanceId") ?? Environment.MachineName,
            Database = databaseStatus,
            Checks = new
            {
                Database = databaseStatus == "Connected",
                Memory = await CheckMemoryHealthAsync(),
                Traffic = canAcceptTraffic
            }
        };

        _logger.LogInformation("Readiness check - Ready: {Ready}, Instance: {Instance}, Database: {Database}", 
            readinessStatus.Ready, readinessStatus.Instance, readinessStatus.Database);

        if (!readinessStatus.Ready)
        {
            return StatusCode(503, readinessStatus); // Service Unavailable
        }

        return Ok(readinessStatus);
    }

    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        // Liveness probe for basic application health
        var livenessStatus = new
        {
            Alive = true,
            Timestamp = DateTime.UtcNow,
            Instance = Environment.GetEnvironmentVariable("LoadBalancing__InstanceId") ?? Environment.MachineName,
            Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime
        };

        return Ok(livenessStatus);
    }

    private async Task<string> CheckDatabaseHealth()
    {
        try
        {
            if (_context is null)
            {
                return "Database context not available";
            }
            
            await _context.Database.CanConnectAsync();
            return "Connected";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Database configuration invalid during health check");
            return "Database configuration error";
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Database connection timeout during health check");
            return "Database connection timeout";
        }
        // CodeQL[cs/catch-of-all-exceptions] - Final safety net for controller boundary
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error during database health check");
            return "Database connection failed";
        }
    }

    private async Task<double> GetCpuUsageAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            
            // Wait a short time to measure CPU usage
            await Task.Delay(100);
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return Math.Round(cpuUsageTotal * 100, 2);
        }
        catch
        {
            return -1; // Unable to measure
        }
    }

    private async Task<bool> CanAcceptTrafficAsync()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            
            // Check memory usage (reject traffic if > 80% of 1GB limit)
            var memoryUsageMB = process.WorkingSet64 / 1024.0 / 1024.0;
            var memoryThreshold = 800; // 80% of 1GB in MB
            
            if (memoryUsageMB > memoryThreshold)
            {
                _logger.LogWarning("High memory usage detected: {MemoryUsage}MB > {Threshold}MB", 
                    Math.Round(memoryUsageMB, 2), memoryThreshold);
                return false;
            }

            // Check CPU usage
            var cpuUsage = await GetCpuUsageAsync();
            if (cpuUsage > 90) // Reject traffic if CPU > 90%
            {
                _logger.LogWarning("High CPU usage detected: {CpuUsage}% > 90%", cpuUsage);
                return false;
            }

            // Check thread count
            if (process.Threads.Count > 200) // Blazor Server threshold
            {
                _logger.LogWarning("High thread count detected: {ThreadCount} > 200", process.Threads.Count);
                return false;
            }

            // Check database connectivity if context is available
            if (_context != null)
            {
                var dbStatus = await CheckDatabaseHealth();
                if (dbStatus != "Connected")
                {
                    _logger.LogWarning("Database not available: {Status}", dbStatus);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking traffic acceptance capability");
            return false;
        }
    }

    private async Task<bool> CheckMemoryHealthAsync()
    {
        try
        {
            await Task.CompletedTask; // Placeholder for async signature
            
            var process = Process.GetCurrentProcess();
            var memoryUsageMB = process.WorkingSet64 / 1024.0 / 1024.0;
            var memoryThreshold = 800; // 80% of 1GB
            
            return memoryUsageMB <= memoryThreshold;
        }
        catch
        {
            return false;
        }
    }
}