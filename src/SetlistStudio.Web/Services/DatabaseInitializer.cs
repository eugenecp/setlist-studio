using Microsoft.EntityFrameworkCore;
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
            
            // Test basic database operations
            var songCount = await context.Songs.CountAsync();
            logger.LogInformation("Current song count in database: {SongCount}", songCount);
            
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