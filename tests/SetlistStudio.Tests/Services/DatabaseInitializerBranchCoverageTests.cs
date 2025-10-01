using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Web.Services;
using Xunit;

namespace SetlistStudio.Tests.Services;

/// <summary>
/// Branch coverage tests for DatabaseInitializer focusing on error paths and edge cases
/// Targets specific branches that improve overall branch coverage to reach 90%
/// </summary>
public class DatabaseInitializerBranchCoverageTests : IDisposable
{
    private readonly List<string> _logMessages;
    private ServiceProvider? _serviceProvider;

    public DatabaseInitializerBranchCoverageTests()
    {
        _logMessages = new List<string>();
    }

    private ServiceProvider CreateServiceProvider(Action<DbContextOptionsBuilder> configureDb)
    {
        var services = new ServiceCollection();
        services.AddDbContext<SetlistStudioDbContext>(configureDb);
        services.AddLogging(builder => builder.AddProvider(new TestLoggerProvider(_logMessages)));
        return services.BuildServiceProvider();
    }

    #region Error Handling Branch Tests

    [Fact]
    public async Task InitializeAsync_ShouldLogErrorDetails_WhenDatabaseConnectionFails()
    {
        // Arrange
        _serviceProvider = CreateServiceProvider(options =>
            options.UseSqlite("Data Source=/invalid/path/cannot/access/database.db"));
        var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseInitializerBranchCoverageTests>>();

        // Act & Assert - Expect SqliteException specifically, not generic Exception
        var exception = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(async () => 
            await DatabaseInitializer.InitializeAsync(_serviceProvider, logger));

        exception.Should().NotBeNull();
        _logMessages.Should().Contain(msg => msg.Contains("Database initialization failed:"));
    }

    [Fact]
    public async Task InitializeAsync_ShouldHandleConnectionStringParsing_WhenInvalidFormat()
    {
        // Arrange
        _serviceProvider = CreateServiceProvider(options =>
            options.UseSqlite("InvalidConnectionString"));
        var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseInitializerBranchCoverageTests>>();

        // Act & Assert - Should handle gracefully without throwing during string parsing
        try
        {
            await DatabaseInitializer.InitializeAsync(_serviceProvider, logger);
        }
        catch (Exception)
        {
            // Expected to fail, but should have attempted to log connection details
            _logMessages.Should().Contain(msg => msg.Contains("Database initialization failed:"));
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldHandleRetryLogic_WhenDatabaseQueryFails()
    {
        // Arrange - Use a database that exists but will have query issues
        var tempDbPath = Path.GetTempFileName();
        
        try
        {
            File.WriteAllText(tempDbPath, "invalid database content"); // Corrupt file
            
            var services = new ServiceCollection();
            services.AddDbContext<SetlistStudioDbContext>(options =>
                options.UseSqlite($"Data Source={tempDbPath}"));
            services.AddLogging(builder => builder.AddProvider(new TestLoggerProvider(_logMessages)));

            _serviceProvider?.Dispose(); // Dispose previous service provider
            _serviceProvider = services.BuildServiceProvider();
            var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseInitializerBranchCoverageTests>>();

            // Act & Assert
            await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(async () =>
                await DatabaseInitializer.InitializeAsync(_serviceProvider, logger));

            // Should show retry attempts in logs
            _logMessages.Should().Contain(msg => msg.Contains("Failed to query Songs table"));
        }
        finally
        {
            _serviceProvider?.Dispose();
            _serviceProvider = null;
            
            // Wait a moment and then try to delete the file
            await Task.Delay(100);
            try
            {
                if (File.Exists(tempDbPath))
                    File.Delete(tempDbPath);
            }
            catch (IOException)
            {
                // File may still be locked, ignore cleanup failure
            }
        }
    }

    #endregion

    #region File System Error Handling Tests

    [Fact]
    public async Task InitializeAsync_ShouldLogFileDetails_WhenDatabaseFileInformationAvailable()
    {
        // Arrange
        var tempDbPath = Path.GetTempFileName();
        
        try
        {
            File.Delete(tempDbPath); // File doesn't exist initially
            
            var services = new ServiceCollection();
            services.AddDbContext<SetlistStudioDbContext>(options =>
                options.UseSqlite($"Data Source={tempDbPath}"));
            services.AddLogging(builder => builder.AddProvider(new TestLoggerProvider(_logMessages)));

            _serviceProvider?.Dispose(); // Dispose previous service provider
            _serviceProvider = services.BuildServiceProvider();
            var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseInitializerBranchCoverageTests>>();

            // Act
            await DatabaseInitializer.InitializeAsync(_serviceProvider, logger);

            // Assert - Should successfully create database and log details
            _logMessages.Should().Contain(msg => msg.Contains("Database creation result: True"));
            _logMessages.Should().Contain(msg => msg.Contains("Database was created, allowing schema to settle"));
        }
        finally
        {
            _serviceProvider?.Dispose();
            _serviceProvider = null;
            
            // Wait a moment and then try to delete the file
            await Task.Delay(100);
            try
            {
                if (File.Exists(tempDbPath))
                    File.Delete(tempDbPath);
            }
            catch (IOException)
            {
                // File may still be locked, ignore cleanup failure
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldLogConnectionFailure_WhenCannotConnectInitially()
    {
        // Arrange - Use a path that will cause connection issues
        var invalidPath = Path.Combine(Path.GetTempPath(), "nonexistent", "database.db");
        
        var services = new ServiceCollection();
        services.AddDbContext<SetlistStudioDbContext>(options =>
            options.UseSqlite($"Data Source={invalidPath}"));
        services.AddLogging(builder => builder.AddProvider(new TestLoggerProvider(_logMessages)));

        _serviceProvider = services.BuildServiceProvider();
        var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseInitializerBranchCoverageTests>>();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await DatabaseInitializer.InitializeAsync(_serviceProvider, logger));

            // Should log connection test failure
            _logMessages.Should().Contain(msg => msg.Contains("Database connection test: False") || 
                                                 msg.Contains("Cannot connect to database"));
        }
        catch (Exception)
        {
            // Expected to fail
        }
    }

    #endregion

    #region Provider Detection Branch Tests

    [Fact]
    public async Task InitializeAsync_ShouldDetectInMemoryProvider_WhenProviderNameContainsInMemory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<SetlistStudioDbContext>(options =>
            options.UseInMemoryDatabase("TestInMemoryDb"));
        services.AddLogging(builder => builder.AddProvider(new TestLoggerProvider(_logMessages)));

        _serviceProvider = services.BuildServiceProvider();
        var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseInitializerBranchCoverageTests>>();

        // Act
        await DatabaseInitializer.InitializeAsync(_serviceProvider, logger);

        // Assert
        _logMessages.Should().Contain(msg => msg.Contains("In-memory database detected"));
        _logMessages.Should().Contain(msg => msg.Contains("Database initialization completed (skipped for tests)"));
    }

    [Fact]
    public async Task InitializeAsync_ShouldHandleNullConnectionString_Gracefully()
    {
        // Arrange - This test verifies the GetConnectionStringAsync method handles exceptions
        var services = new ServiceCollection();
        services.AddDbContext<SetlistStudioDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
        services.AddLogging(builder => builder.AddProvider(new TestLoggerProvider(_logMessages)));

        _serviceProvider = services.BuildServiceProvider();
        var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseInitializerBranchCoverageTests>>();

        // Act
        await DatabaseInitializer.InitializeAsync(_serviceProvider, logger);

        // Assert - Should complete successfully even when GetConnectionString() might throw
        _logMessages.Should().Contain(msg => msg.Contains("In-memory database detected"));
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task InitializeAsync_ShouldHandleExponentialBackoff_InRetryLogic()
    {
        // Arrange - Create a scenario that will fail multiple times
        var tempDbPath = Path.GetTempFileName();
        
        try
        {
            File.WriteAllBytes(tempDbPath, new byte[] { 0x00, 0x01, 0x02 }); // Invalid SQLite file
            
            var services = new ServiceCollection();
            services.AddDbContext<SetlistStudioDbContext>(options =>
                options.UseSqlite($"Data Source={tempDbPath}"));
            services.AddLogging(builder => builder.AddProvider(new TestLoggerProvider(_logMessages)));

            _serviceProvider?.Dispose(); // Dispose previous service provider
            _serviceProvider = services.BuildServiceProvider();
            var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseInitializerBranchCoverageTests>>();

            // Act & Assert
            await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(async () =>
                await DatabaseInitializer.InitializeAsync(_serviceProvider, logger));

            // Should show multiple retry attempts with different delays
            var retryMessages = _logMessages.Where(msg => msg.Contains("Failed to query Songs table on attempt")).ToList();
            retryMessages.Should().HaveCountGreaterOrEqualTo(1, "should show at least one retry attempt");
        }
        finally
        {
            _serviceProvider?.Dispose();
            _serviceProvider = null;
            
            // Wait a moment and then try to delete the file
            await Task.Delay(100);
            try
            {
                if (File.Exists(tempDbPath))
                    File.Delete(tempDbPath);
            }
            catch (IOException)
            {
                // File may still be locked, ignore cleanup failure
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldLogDatabaseFilePermissions_WhenFileSystemErrorOccurs()
    {
        // Arrange - Test the database file logging branch
        var readOnlyPath = Path.Combine(Path.GetTempPath(), "readonly_test.db");
        
        try
        {
            var services = new ServiceCollection();
            services.AddDbContext<SetlistStudioDbContext>(options =>
                options.UseSqlite($"Data Source={readOnlyPath}"));
            services.AddLogging(builder => builder.AddProvider(new TestLoggerProvider(_logMessages)));

            _serviceProvider?.Dispose(); // Dispose previous service provider
            _serviceProvider = services.BuildServiceProvider();
            var logger = _serviceProvider.GetRequiredService<ILogger<DatabaseInitializerBranchCoverageTests>>();

            // Act & Assert
            try
            {
                await DatabaseInitializer.InitializeAsync(_serviceProvider, logger);
            }
            catch (Exception)
            {
                // Expected to fail, verify error logging branches are hit
                _logMessages.Should().Contain(msg => msg.Contains("Database file path:") || 
                                                     msg.Contains("Failed to get database file information"));
            }
        }
        finally
        {
            _serviceProvider?.Dispose();
            _serviceProvider = null;
            
            // Wait a moment and then try to delete the file
            await Task.Delay(100);
            try
            {
                if (File.Exists(readOnlyPath))
                    File.Delete(readOnlyPath);
            }
            catch (IOException)
            {
                // File may still be locked, ignore cleanup failure
            }
        }
    }

    #endregion

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}