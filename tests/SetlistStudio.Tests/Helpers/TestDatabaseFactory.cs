using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SetlistStudio.Infrastructure.Configuration;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using Testcontainers.PostgreSql;

namespace SetlistStudio.Tests.Helpers;

/// <summary>
/// Factory for creating test database contexts with different providers
/// Supports in-memory, SQLite, and PostgreSQL databases for different test scenarios
/// </summary>
public class TestDatabaseFactory : IAsyncDisposable
{
    private readonly PostgreSqlContainer? _postgresContainer;
    private readonly IServiceProvider _serviceProvider;
    private readonly DatabaseProvider _provider;
    private readonly string _connectionString;

    private TestDatabaseFactory(DatabaseProvider provider, string connectionString, PostgreSqlContainer? container = null)
    {
        _provider = provider;
        _connectionString = connectionString;
        _postgresContainer = container;
        _serviceProvider = CreateServiceProvider();
    }

    /// <summary>
    /// Creates an in-memory database factory for fast unit tests
    /// </summary>
    public static TestDatabaseFactory CreateInMemory(string? databaseName = null)
    {
        var dbName = databaseName ?? $"TestDb_{Guid.NewGuid()}";
        return new TestDatabaseFactory(DatabaseProvider.InMemory, dbName);
    }

    /// <summary>
    /// Creates a SQLite database factory for integration tests
    /// </summary>
    public static TestDatabaseFactory CreateSqlite(string? filePath = null)
    {
        var path = filePath ?? ":memory:";
        var connectionString = $"Data Source={path}";
        return new TestDatabaseFactory(DatabaseProvider.SQLite, connectionString);
    }

    /// <summary>
    /// Creates a PostgreSQL database factory using Testcontainers for full integration tests
    /// </summary>
    public static async Task<TestDatabaseFactory> CreatePostgreSqlAsync()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("setliststudio_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithPortBinding(0, 5432) // Random port
            .Build();

        await container.StartAsync();

        var connectionString = container.GetConnectionString();
        return new TestDatabaseFactory(DatabaseProvider.PostgreSQL, connectionString, container);
    }

    /// <summary>
    /// Creates a new database context instance
    /// </summary>
    public SetlistStudioDbContext CreateContext()
    {
        var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
    }

    /// <summary>
    /// Creates a new read-only database context instance
    /// </summary>
    public ReadOnlySetlistStudioDbContext CreateReadOnlyContext()
    {
        var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ReadOnlySetlistStudioDbContext>();
    }

    /// <summary>
    /// Ensures the database is created and applies migrations
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        using var context = CreateContext();
        
        if (_provider == DatabaseProvider.PostgreSQL)
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }
    }

    /// <summary>
    /// Clears all data from the database
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        using var context = CreateContext();
        
        // Clear all tables in reverse dependency order
        context.SetlistSongs.RemoveRange(context.SetlistSongs);
        context.Setlists.RemoveRange(context.Setlists);
        context.Songs.RemoveRange(context.Songs);
        context.AuditLogs.RemoveRange(context.AuditLogs);
        
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the database provider being used
    /// </summary>
    public DatabaseProvider Provider => _provider;

    /// <summary>
    /// Gets the connection string being used
    /// </summary>
    public string ConnectionString => _connectionString;

    /// <summary>
    /// Gets whether PostgreSQL container is running
    /// </summary>
    public bool IsPostgreSqlContainerRunning => _postgresContainer?.State == DotNet.Testcontainers.Containers.TestcontainersStates.Running;

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = _provider.ToString(),
                ["Database:Pool:MaxSize"] = "10",
                ["Database:Pool:MinSize"] = "1",
                ["Database:Pool:ConnectionTimeout"] = "30",
                ["Database:Pool:CommandTimeout"] = "30",
                ["Database:Pool:Enabled"] = "true",
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["ConnectionStrings:WriteConnection"] = _connectionString,
                ["Database:ReadReplicas:0"] = _connectionString // Use same for read in tests
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add database configuration
        services.AddSingleton<IDatabaseConfiguration, DatabaseConfiguration>();
        services.AddSingleton<DatabaseProviderService>();
        
        // Add database contexts
        services.AddDbContext<SetlistStudioDbContext>((serviceProvider, options) =>
        {
            ConfigureDbContext(options, serviceProvider);
        });
        
        services.AddDbContext<ReadOnlySetlistStudioDbContext>((serviceProvider, options) =>
        {
            ConfigureDbContext(options, serviceProvider);
        });
        
        return services.BuildServiceProvider();
    }

    private void ConfigureDbContext(DbContextOptionsBuilder options, IServiceProvider serviceProvider)
    {
        switch (_provider)
        {
            case DatabaseProvider.InMemory:
                options.UseInMemoryDatabase(_connectionString);
                break;
                
            case DatabaseProvider.SQLite:
                options.UseSqlite(_connectionString);
                break;
                
            case DatabaseProvider.PostgreSQL:
                options.UseNpgsql(_connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(30);
                    npgsqlOptions.MigrationsAssembly("SetlistStudio.Web");
                });
                break;
                
            default:
                throw new NotSupportedException($"Database provider {_provider} is not supported in tests");
        }
        
        // Configure for testing
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
        
        // Add logging for PostgreSQL tests
        if (_provider == DatabaseProvider.PostgreSQL)
        {
            var logger = serviceProvider.GetService<ILogger<TestDatabaseFactory>>();
            if (logger != null)
            {
                options.LogTo(message => logger.LogDebug(message));
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.StopAsync();
            await _postgresContainer.DisposeAsync();
        }
        
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}