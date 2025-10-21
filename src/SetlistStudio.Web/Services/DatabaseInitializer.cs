using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.Logging;
using SetlistStudio.Infrastructure.Data;

namespace SetlistStudio.Web.Services;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
        
        try
        {
            logger.LogInformation("Starting database initialization...");
            
            if (await IsInMemoryDatabaseAsync(context, logger))
            {
                return;
            }
            
            await EnsureDatabaseExistsAsync(context, logger);
            await ValidateDatabaseConnectionAsync(context, logger);
            
            logger.LogInformation("Database initialization completed successfully");
        }
        catch (InvalidOperationException ex)
        {
            await LogDatabaseErrorDetailsAsync(context, logger, ex);
            logger.LogError(ex, "Database configuration or state is invalid");
            throw;
        }
        catch (TimeoutException ex)
        {
            await LogDatabaseErrorDetailsAsync(context, logger, ex);
            logger.LogError(ex, "Database operation timed out during initialization");
            throw;
        }
        catch (Exception ex)
        {
            await LogDatabaseErrorDetailsAsync(context, logger, ex);
            logger.LogError(ex, "Unexpected error during database initialization");
            throw;
        }
    }

    private static async Task<bool> IsInMemoryDatabaseAsync(SetlistStudioDbContext context, ILogger logger)
    {
        var providerName = context.Database.ProviderName;
        
        // Try to get connection string, but handle cases where it might throw for non-relational providers
        string? connectionString = await GetConnectionStringAsync(context);
        
        // Check if using EF InMemory provider or SQLite in-memory database
        if (providerName?.Contains("InMemory") == true || 
            connectionString?.Contains(":memory:") == true)
        {
            logger.LogInformation("In-memory database detected - skipping initialization for test environment");
            logger.LogInformation("Database initialization completed (skipped for tests)");
            return true;
        }
        
        return false;
    }

    private static Task<string?> GetConnectionStringAsync(SetlistStudioDbContext context)
    {
        try
        {
            return Task.FromResult(context.Database.GetConnectionString());
        }
        catch (InvalidOperationException)
        {
            // GetConnectionString() throws for non-relational providers like InMemory
            // This is expected and we can continue with providerName check
            return Task.FromResult<string?>(null);
        }
    }

    private static async Task EnsureDatabaseExistsAsync(SetlistStudioDbContext context, ILogger logger)
    {
        // Check if database can be accessed
        var canConnect = await context.Database.CanConnectAsync();
        logger.LogInformation("Database connection test: {CanConnect}", canConnect);
        
        if (!canConnect)
        {
            logger.LogWarning("Cannot connect to database, attempting to create...");
        }
        
        // Ensure database is created
        var created = await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Database creation result: {Created} (true = created, false = already existed)", created);
        
        // If database was just created, wait a moment for all connections to sync
        if (created)
        {
            await Task.Delay(100); // Brief delay to ensure schema is fully available
            logger.LogInformation("Database was created, allowing schema to settle");
        }
    }

    private static async Task ValidateDatabaseConnectionAsync(SetlistStudioDbContext context, ILogger logger)
    {
        // Test basic database operations with retry logic (only for persistent databases)
        const int maxRetries = 3;
        
        for (int retryCount = 0; retryCount < maxRetries; retryCount++)
        {
            try
            {
                var songCount = await context.Songs.CountAsync();
                logger.LogInformation("Current song count in database: {SongCount}", songCount);
                return; // Success, exit retry loop
            }
            catch (Exception countEx) when (retryCount < maxRetries - 1)
            {
                logger.LogWarning("Failed to query Songs table on attempt {Attempt}: {Error}. Retrying...", 
                    retryCount + 1, countEx.Message);
                await Task.Delay(200 * (retryCount + 1)); // Exponential backoff
            }
        }
    }

    private static async Task LogDatabaseErrorDetailsAsync(SetlistStudioDbContext context, ILogger logger, Exception ex)
    {
        logger.LogError(ex, "Database initialization failed: {ErrorMessage}", ex.Message);
        
        // Log additional details about the database file
        try
        {
            var connectionString = context.Database.GetConnectionString();
            logger.LogError("Connection string: {ConnectionString}", connectionString);
            
            await LogDatabaseFileDetailsAsync(connectionString, logger);
        }
        catch (InvalidOperationException innerEx)
        {
            logger.LogError(innerEx, "Database connection configuration invalid");
        }
        catch (UnauthorizedAccessException innerEx)
        {
            logger.LogError(innerEx, "Insufficient permissions to access database file information");
        }
        catch (Exception innerEx)
        {
            logger.LogError(innerEx, "Unexpected error getting database file information");
        }
    }

    private static Task LogDatabaseFileDetailsAsync(string? connectionString, ILogger logger)
    {
        if (connectionString?.Contains("Data Source=") == true)
        {
            var dataSource = connectionString.Split("Data Source=")[1].Split(';')[0];
            var fileInfo = new FileInfo(dataSource);
            
            logger.LogError("Database file path: {FilePath}", fileInfo.FullName);
            logger.LogError("Database file exists: {FileExists}", fileInfo.Exists);
            logger.LogError("Database directory exists: {DirectoryExists}", fileInfo.Directory?.Exists);
            logger.LogError("Database directory permissions: {DirectoryPermissions}", 
                fileInfo.Directory?.Exists == true ? "Readable" : "Unknown");
        }
        
        return Task.CompletedTask;
    }
}