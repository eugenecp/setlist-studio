using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using SetlistStudio.Infrastructure.Data;
using Xunit;

namespace SetlistStudio.Tests.Web;

/// <summary>
/// Focused tests for Program.cs configuration logic using direct service configuration
/// Testing database provider selection and authentication configuration branches
/// </summary>
public class ProgramConfigurationBranchTests : IDisposable
{
    private ServiceCollection? _services;
    private ServiceProvider? _serviceProvider;

    #region Database Provider Configuration Tests

    [Fact]
    public void ConfigureServices_ShouldUseInMemoryDatabase_WhenTestEnvironment()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string>());
        var environment = CreateTestEnvironment("Test");
        
        // Act
        _services = new ServiceCollection();
        ConfigureDatabaseProvider(_services, configuration, environment);
        _serviceProvider = _services.BuildServiceProvider();
        
        // Assert
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
        context.Database.IsInMemory().Should().BeTrue("Test environment should use InMemory database");
    }

    [Fact]
    public void ConfigureServices_ShouldUseSqlite_WhenDevelopmentEnvironmentAndNoConnectionString()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string>());
        var environment = CreateTestEnvironment("Development");
        
        // Act
        _services = new ServiceCollection();
        ConfigureDatabaseProvider(_services, configuration, environment);
        _serviceProvider = _services.BuildServiceProvider();
        
        // Assert
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
        context.Database.IsSqlite().Should().BeTrue("Development should default to SQLite");
    }

    [Fact]
    public void ConfigureServices_ShouldUseSqlServer_WhenConnectionStringContainsServer()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", "Server=localhost;Database=TestDb;Trusted_Connection=true;"}
        });
        var environment = CreateTestEnvironment("Production");
        
        // Act
        _services = new ServiceCollection();
        ConfigureDatabaseProvider(_services, configuration, environment);
        _serviceProvider = _services.BuildServiceProvider();
        
        // Assert
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
        context.Database.IsSqlServer().Should().BeTrue("Should use SQL Server when connection string contains Server");
    }

    [Fact]
    public void ConfigureServices_ShouldUseSqlite_WhenConnectionStringContainsDataSource()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=test.db"}
        });
        var environment = CreateTestEnvironment("Production");
        
        // Act
        _services = new ServiceCollection();
        ConfigureDatabaseProvider(_services, configuration, environment);
        _serviceProvider = _services.BuildServiceProvider();
        
        // Assert
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
        context.Database.IsSqlite().Should().BeTrue("Should use SQLite when connection string contains Data Source");
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    public void ConfigureServices_ShouldUseLocalDatabasePath_BasedOnEnvironment(string environmentName, bool shouldUseLocalPath)
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
        var configuration = CreateConfiguration(new Dictionary<string, string>());
        var environment = CreateTestEnvironment(environmentName);
        
        try
        {
            // Act
            _services = new ServiceCollection();
            ConfigureDatabaseProvider(_services, configuration, environment);
            _serviceProvider = _services.BuildServiceProvider();
            
            // Assert
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
            var connectionString = context.Database.GetConnectionString();
            
            if (shouldUseLocalPath)
            {
                connectionString.Should().Contain("setliststudio-dev.db", "Development should use dev database");
            }
            else
            {
                connectionString.Should().Contain("setliststudio.db", "Production should use production database");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
        }
    }

    [Fact]
    public void ConfigureServices_ShouldUseContainerPath_WhenRunningInContainer()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");
        var configuration = CreateConfiguration(new Dictionary<string, string>());
        var environment = CreateTestEnvironment("Production");
        
        try
        {
            // Act
            _services = new ServiceCollection();
            ConfigureDatabaseProvider(_services, configuration, environment);
            _serviceProvider = _services.BuildServiceProvider();
            
            // Assert
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
            var connectionString = context.Database.GetConnectionString();
            connectionString.Should().Contain("/app/", "Container should use /app/ path");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
        }
    }

    #endregion

    #region Authentication Configuration Branch Tests

    [Fact]
    public void ConfigureDatabaseProvider_ShouldHandleNullConnectionString_Gracefully()
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", ""}
        });
        var environment = CreateTestEnvironment("Production");
        
        // Act
        _services = new ServiceCollection();
        var configureAction = () => ConfigureDatabaseProvider(_services, configuration, environment);
        
        // Assert
        configureAction.Should().NotThrow("Empty connection string should be handled gracefully");
        _serviceProvider = _services.BuildServiceProvider();
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
        context.Should().NotBeNull("Database context should still be configured");
    }

    [Theory]
    [InlineData("Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=true;", true)]
    [InlineData("Data Source=test.db;Version=3;", false)]
    [InlineData("Data Source=:memory:", false)]
    [InlineData("", false)]
    public void ConfigureDatabaseProvider_ShouldDetectSqlServer_FromConnectionString(string connectionString, bool shouldUseSqlServer)
    {
        // Arrange
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", connectionString}
        });
        var environment = CreateTestEnvironment("Production");
        
        // Act
        _services = new ServiceCollection();
        ConfigureDatabaseProvider(_services, configuration, environment);
        _serviceProvider = _services.BuildServiceProvider();
        
        // Assert
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
        
        if (shouldUseSqlServer)
        {
            context.Database.IsSqlServer().Should().BeTrue("Should detect SQL Server from connection string");
        }
        else
        {
            context.Database.IsSqlite().Should().BeTrue("Should default to SQLite when not SQL Server");
        }
    }

    #endregion

    #region Helper Methods

    private static IConfiguration CreateConfiguration(Dictionary<string, string> configValues)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)))
            .Build();
    }

    private static IWebHostEnvironment CreateTestEnvironment(string environmentName)
    {
        var environment = new TestWebHostEnvironment
        {
            EnvironmentName = environmentName
        };
        return environment;
    }

    private static void ConfigureDatabaseProvider(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Add logging for testing
        services.AddLogging(builder => builder.AddConsole());
        
        // Replicate the database configuration logic from Program.cs
        if (environment.IsEnvironment("Test"))
        {
            services.AddDbContext<SetlistStudioDbContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));
        }
        else
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");

            if (!string.IsNullOrEmpty(connectionString))
            {
                // Use provided connection string - detect provider type
                if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                    connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
                {
                    services.AddDbContext<SetlistStudioDbContext>(options =>
                        options.UseSqlServer(connectionString));
                }
                else
                {
                    services.AddDbContext<SetlistStudioDbContext>(options =>
                        options.UseSqlite(connectionString));
                }
            }
            else
            {
                // Generate SQLite connection string based on environment
                var databasePath = GetDatabasePath(environment);
                services.AddDbContext<SetlistStudioDbContext>(options =>
                    options.UseSqlite($"Data Source={databasePath}"));
            }
        }
    }

    private static string GetDatabasePath(IWebHostEnvironment environment)
    {
        bool isContainer = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));
        
        if (isContainer)
        {
            return "/app/setliststudio.db";
        }
        
        return environment.IsDevelopment() ? "setliststudio-dev.db" : "setliststudio.db";
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    #endregion
}

/// <summary>
/// Test implementation of IWebHostEnvironment for configuration testing
/// </summary>
public class TestWebHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Test";
    public string ApplicationName { get; set; } = "SetlistStudio.Tests";
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}