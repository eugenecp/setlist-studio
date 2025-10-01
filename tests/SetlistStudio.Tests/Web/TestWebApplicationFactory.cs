using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SetlistStudio.Infrastructure.Data;

namespace SetlistStudio.Tests.Web;

/// <summary>
/// Base WebApplicationFactory for integration tests with proper configuration
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
                {"Logging:LogLevel:Default", "Warning"},
                {"Logging:LogLevel:Microsoft.AspNetCore", "Warning"},
                {"Logging:LogLevel:Microsoft.EntityFrameworkCore", "Warning"}
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SetlistStudioDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<SetlistStudioDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            });

            // Reduce logging noise for tests
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        });
    }
}