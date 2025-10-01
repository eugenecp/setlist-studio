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
            
            // Skip database initialization for test environments using in-memory databases
            // Check if using in-memory database provider
            var providerName = context.Database.ProviderName;
            if (providerName?.Contains("InMemory") == true)
            {
                logger.LogInformation("In-memory database detected - skipping initialization for test environment");
                logger.LogInformation("Database initialization completed (skipped for tests)");
                return;
            }
            
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
            
            // Test basic database operations with retry logic (only for persistent databases)
            var songCount = 0;
            var retryCount = 0;
            const int maxRetries = 3;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    songCount = await context.Songs.CountAsync();
                    logger.LogInformation("Current song count in database: {SongCount}", songCount);
                    break; // Success, exit retry loop
                }
                catch (Exception countEx) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    logger.LogWarning("Failed to query Songs table on attempt {Attempt}: {Error}. Retrying...", 
                        retryCount, countEx.Message);
                    await Task.Delay(200 * retryCount); // Exponential backoff
                }
            }
            
            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed: {ErrorMessage}", ex.Message);
            
            // Log additional details about the database file
            try
            {
                var connectionString = context.Database.GetConnectionString();
                logger.LogError("Connection string: {ConnectionString}", connectionString);
                
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
            }
            catch (Exception innerEx)
            {
                logger.LogError(innerEx, "Failed to get database file information");
            }
            
            throw;
        }
    }
}