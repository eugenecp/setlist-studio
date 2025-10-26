using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SetlistStudio.Infrastructure.Configuration;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Service for configuring Entity Framework with different database providers
/// Supports connection pooling and read/write splitting
/// </summary>
public class DatabaseProviderService
{
    private readonly IDatabaseConfiguration _config;
    private readonly ILogger<DatabaseProviderService> _logger;
    private readonly NpgsqlDataSource? _writeDataSource;
    private readonly NpgsqlDataSource? _readDataSource;

    public DatabaseProviderService(IDatabaseConfiguration config, ILogger<DatabaseProviderService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize PostgreSQL data sources for connection pooling
        if (_config.Provider == DatabaseProvider.PostgreSQL)
        {
            _writeDataSource = CreatePostgreSqlDataSource(_config.WriteConnectionString, "Write");
            
            if (_config.HasReadReplicas)
            {
                var readConnectionString = _config.GetReadConnectionString();
                _readDataSource = CreatePostgreSqlDataSource(readConnectionString, "Read");
            }
        }
    }

    /// <summary>
    /// Configures DbContext options for write operations
    /// </summary>
    public void ConfigureWriteContext(DbContextOptionsBuilder options)
    {
        ConfigureContext(options, _config.WriteConnectionString, _writeDataSource, "Write");
    }

    /// <summary>
    /// Configures DbContext options for read operations
    /// </summary>
    public void ConfigureReadContext(DbContextOptionsBuilder options)
    {
        var connectionString = _config.GetReadConnectionString();
        var dataSource = _config.HasReadReplicas ? _readDataSource : _writeDataSource;
        ConfigureContext(options, connectionString, dataSource, "Read");
    }

    /// <summary>
    /// Configures DbContext options for the specified connection
    /// </summary>
    private void ConfigureContext(DbContextOptionsBuilder options, string connectionString, 
        NpgsqlDataSource? dataSource, string contextType)
    {
        switch (_config.Provider)
        {
            case DatabaseProvider.PostgreSQL:
                ConfigurePostgreSQL(options, connectionString, dataSource, contextType);
                break;

            case DatabaseProvider.SqlServer:
                ConfigureSqlServer(options, connectionString, contextType);
                break;

            case DatabaseProvider.SQLite:
                ConfigureSQLite(options, connectionString, contextType);
                break;

            case DatabaseProvider.InMemory:
                ConfigureInMemory(options, contextType);
                break;

            default:
                throw new NotSupportedException($"Database provider {_config.Provider} is not supported");
        }

        // Configure query behavior
        options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
        
        // Enable sensitive data logging in development only
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true)
        {
            options.EnableSensitiveDataLogging();
        }

        _logger.LogDebug("Configured {ContextType} context for {Provider}", contextType, _config.Provider);
    }

    private void ConfigurePostgreSQL(DbContextOptionsBuilder options, string connectionString, 
        NpgsqlDataSource? dataSource, string contextType)
    {
        if (dataSource != null)
        {
            // Use data source for connection pooling
            options.UseNpgsql(dataSource, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(_config.CommandTimeout);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        }
        else
        {
            // Fallback to connection string
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(_config.CommandTimeout);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        }

        _logger.LogInformation("Configured PostgreSQL {ContextType} context with connection pooling: {HasDataSource}", 
            contextType, dataSource != null);
    }

    private void ConfigureSqlServer(DbContextOptionsBuilder options, string connectionString, string contextType)
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.CommandTimeout(_config.CommandTimeout);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });

        _logger.LogInformation("Configured SQL Server {ContextType} context", contextType);
    }

    private void ConfigureSQLite(DbContextOptionsBuilder options, string connectionString, string contextType)
    {
        // SQLite configuration will be handled by the Web project since it has the SQLite package
        throw new InvalidOperationException("SQLite configuration should be handled by the calling assembly with SQLite package reference");
    }

    private void ConfigureInMemory(DbContextOptionsBuilder options, string contextType)
    {
        var databaseName = $"SetlistStudio_{contextType}_{Guid.NewGuid()}";
        options.UseInMemoryDatabase(databaseName);

        _logger.LogInformation("Configured In-Memory {ContextType} context: {DatabaseName}", contextType, databaseName);
    }

    private NpgsqlDataSource CreatePostgreSqlDataSource(string connectionString, string contextType)
    {
        try
        {
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            
            // Configure connection pool settings
            builder.ConnectionStringBuilder.MaxPoolSize = _config.MaxPoolSize;
            builder.ConnectionStringBuilder.MinPoolSize = _config.MinPoolSize;
            builder.ConnectionStringBuilder.ConnectionIdleLifetime = 300; // 5 minutes
            builder.ConnectionStringBuilder.ConnectionPruningInterval = 10; // 10 seconds
            builder.ConnectionStringBuilder.CommandTimeout = _config.CommandTimeout;
            builder.ConnectionStringBuilder.Timeout = _config.ConnectionTimeout;
            builder.ConnectionStringBuilder.Pooling = _config.EnablePooling;

            // Configure reliability settings
            builder.ConnectionStringBuilder.ApplicationName = $"SetlistStudio_{contextType}";
            builder.ConnectionStringBuilder.IncludeErrorDetail = true;

            var dataSource = builder.Build();
            
            _logger.LogInformation("Created PostgreSQL data source for {ContextType} - Pool: {MinSize}-{MaxSize}, Timeout: {Timeout}s",
                contextType, _config.MinPoolSize, _config.MaxPoolSize, _config.ConnectionTimeout);
            
            return dataSource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PostgreSQL data source for {ContextType}", contextType);
            throw;
        }
    }

    /// <summary>
    /// Disposes managed resources
    /// </summary>
    public void Dispose()
    {
        _writeDataSource?.Dispose();
        _readDataSource?.Dispose();
    }
}

/// <summary>
/// Factory for creating database provider services
/// </summary>
public class DatabaseProviderServiceFactory
{
    private readonly IDatabaseConfiguration _config;
    private readonly ILogger<DatabaseProviderService> _logger;

    public DatabaseProviderServiceFactory(IDatabaseConfiguration config, ILogger<DatabaseProviderService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new database provider service instance
    /// </summary>
    public DatabaseProviderService Create()
    {
        return new DatabaseProviderService(_config, _logger);
    }
}