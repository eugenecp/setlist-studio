using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SetlistStudio.Infrastructure.Data;

namespace SetlistStudio.Tests.Web;

/// <summary>
/// Test web application factory for integration testing
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Configure logging for tests
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
        });

        builder.UseEnvironment("Test");
    }
}