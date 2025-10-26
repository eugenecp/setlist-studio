using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace SetlistStudio.Infrastructure.Configuration;

/// <summary>
/// Database configuration implementation with support for multiple providers,
/// connection pooling, and read replica distribution
/// </summary>
public class DatabaseConfiguration : IDatabaseConfiguration
{
    private readonly ILogger<DatabaseConfiguration> _logger;
    private readonly List<string> _readConnectionStrings;
    private int _currentReadIndex = 0;
    private readonly object _readIndexLock = new();

    public DatabaseProvider Provider { get; private set; }
    public string WriteConnectionString { get; private set; } = string.Empty;
    public IReadOnlyList<string> ReadConnectionStrings => _readConnectionStrings.AsReadOnly();
    public int MaxPoolSize { get; private set; }
    public int MinPoolSize { get; private set; }
    public int ConnectionTimeout { get; private set; }
    public int CommandTimeout { get; private set; }
    public bool EnablePooling { get; private set; }
    public bool HasReadReplicas => _readConnectionStrings.Count > 0 && 
                                   !_readConnectionStrings.All(cs => cs == WriteConnectionString);

    public DatabaseConfiguration(IConfiguration configuration, ILogger<DatabaseConfiguration> logger)
    {
        _logger = logger;
        _readConnectionStrings = new List<string>();
        LoadConfiguration(configuration);
    }

    /// <summary>
    /// Gets a connection string for read operations using round-robin selection
    /// </summary>
    public string GetReadConnectionString()
    {
        if (!HasReadReplicas)
        {
            return WriteConnectionString;
        }

        lock (_readIndexLock)
        {
            var connectionString = _readConnectionStrings[_currentReadIndex];
            _currentReadIndex = (_currentReadIndex + 1) % _readConnectionStrings.Count;
            return connectionString;
        }
    }

    /// <summary>
    /// Validates the database configuration
    /// </summary>
    public bool IsValid()
    {
        return IsConnectionStringValid() && 
               IsPoolConfigurationValid() && 
               IsTimeoutConfigurationValid();
    }

    /// <summary>
    /// Validates the connection string configuration
    /// </summary>
    private bool IsConnectionStringValid()
    {
        if (string.IsNullOrWhiteSpace(WriteConnectionString))
        {
            _logger.LogError("Write connection string is missing or empty");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates the connection pool configuration
    /// </summary>
    private bool IsPoolConfigurationValid()
    {
        return ValidateMaxPoolSizeRange() && ValidateMinPoolSizeRange();
    }

    /// <summary>
    /// Validates that MaxPoolSize is within acceptable bounds
    /// </summary>
    private bool ValidateMaxPoolSizeRange()
    {
        if (MaxPoolSize <= 0 || MaxPoolSize > 1000)
        {
            _logger.LogError("MaxPoolSize must be between 1 and 1000, got: {MaxPoolSize}", MaxPoolSize);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Validates that MinPoolSize is within acceptable bounds relative to MaxPoolSize
    /// </summary>
    private bool ValidateMinPoolSizeRange()
    {
        if (MinPoolSize < 0 || MinPoolSize > MaxPoolSize)
        {
            _logger.LogError("MinPoolSize must be between 0 and MaxPoolSize ({MaxPoolSize}), got: {MinPoolSize}", 
                MaxPoolSize, MinPoolSize);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Validates the timeout configuration
    /// </summary>
    private bool IsTimeoutConfigurationValid()
    {
        return ValidateConnectionTimeoutRange() && ValidateCommandTimeoutRange();
    }

    /// <summary>
    /// Validates that ConnectionTimeout is within acceptable bounds
    /// </summary>
    private bool ValidateConnectionTimeoutRange()
    {
        if (ConnectionTimeout <= 0 || ConnectionTimeout > 300)
        {
            _logger.LogError("ConnectionTimeout must be between 1 and 300 seconds, got: {ConnectionTimeout}", ConnectionTimeout);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Validates that CommandTimeout is within acceptable bounds
    /// </summary>
    private bool ValidateCommandTimeoutRange()
    {
        if (CommandTimeout <= 0 || CommandTimeout > 3600)
        {
            _logger.LogError("CommandTimeout must be between 1 and 3600 seconds, got: {CommandTimeout}", CommandTimeout);
            return false;
        }
        return true;
    }

    private void LoadConfiguration(IConfiguration configuration)
    {
        // Determine database provider
        var providerName = configuration["Database:Provider"] ?? 
                          Environment.GetEnvironmentVariable("DB_PROVIDER") ?? 
                          "SQLite";
        
        if (!Enum.TryParse<DatabaseProvider>(providerName, true, out var provider))
        {
            _logger.LogWarning("Unknown database provider '{Provider}', defaulting to SQLite", providerName);
            provider = DatabaseProvider.SQLite;
        }
        Provider = provider;

        // Load connection strings
        LoadConnectionStrings(configuration);

        // Load pool configuration
        LoadPoolConfiguration(configuration);

        _logger.LogInformation("Database configuration loaded - Provider: {Provider}, HasReadReplicas: {HasReadReplicas}, MaxPoolSize: {MaxPoolSize}",
            Provider, HasReadReplicas, MaxPoolSize);
    }

    private void LoadConnectionStrings(IConfiguration configuration)
    {
        // Primary write connection
        WriteConnectionString = GetWriteConnectionString(configuration);

        // Read replica connections
        var readConnectionsSection = configuration.GetSection("Database:ReadReplicas");
        if (readConnectionsSection.Exists())
        {
            foreach (var connectionSection in readConnectionsSection.GetChildren())
            {
                var connectionString = connectionSection.Value;
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    _readConnectionStrings.Add(connectionString);
                }
            }
        }

        // If no read replicas configured, use write connection for reads
        if (_readConnectionStrings.Count == 0)
        {
            _readConnectionStrings.Add(WriteConnectionString);
        }

        _logger.LogInformation("Configured {ReadReplicaCount} read connection(s)", _readConnectionStrings.Count);
    }

    private string GetWriteConnectionString(IConfiguration configuration)
    {
        // Try explicit write connection first
        var writeConnection = configuration.GetConnectionString("WriteConnection");
        if (!string.IsNullOrWhiteSpace(writeConnection))
        {
            return writeConnection;
        }

        // Fall back to default connection
        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(defaultConnection))
        {
            return defaultConnection;
        }

        // Generate default based on provider and environment
        return GenerateDefaultConnectionString(configuration);
    }

    private string GenerateDefaultConnectionString(IConfiguration configuration)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var isContainerized = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        return Provider switch
        {
            DatabaseProvider.PostgreSQL => GeneratePostgreSqlConnectionString(configuration, environment, isContainerized),
            DatabaseProvider.SqlServer => GenerateSqlServerConnectionString(configuration, environment, isContainerized),
            DatabaseProvider.InMemory => "InMemory",
            _ => GenerateSqliteConnectionString(configuration, environment, isContainerized)
        };
    }

    private string GeneratePostgreSqlConnectionString(IConfiguration configuration, string environment, bool isContainerized)
    {
        var connectionParams = ExtractPostgreSqlConnectionParameters(configuration, isContainerized);
        var baseConnectionString = BuildPostgreSqlBaseConnectionString(connectionParams);
        var sslSettings = GetPostgreSqlSslSettings(environment);
        
        return baseConnectionString + sslSettings;
    }

    /// <summary>
    /// Extracts PostgreSQL connection parameters from configuration and environment variables
    /// </summary>
    private static (string host, string port, string database, string username, string password) ExtractPostgreSqlConnectionParameters(
        IConfiguration configuration, bool isContainerized)
    {
        var host = configuration["Database:PostgreSQL:Host"] ?? 
                   Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? 
                   (isContainerized ? "postgres" : "localhost");
        
        var port = configuration["Database:PostgreSQL:Port"] ?? 
                   Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? 
                   "5432";
        
        var database = configuration["Database:PostgreSQL:Database"] ?? 
                       Environment.GetEnvironmentVariable("POSTGRES_DB") ?? 
                       "setliststudio";
        
        var username = configuration["Database:PostgreSQL:Username"] ?? 
                       Environment.GetEnvironmentVariable("POSTGRES_USER") ?? 
                       "setliststudio";
        
        var password = configuration["Database:PostgreSQL:Password"] ?? 
                       Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? 
                       "setliststudio";

        return (host, port, database, username, password);
    }

    /// <summary>
    /// Builds the base PostgreSQL connection string from connection parameters
    /// </summary>
    private static string BuildPostgreSqlBaseConnectionString(
        (string host, string port, string database, string username, string password) parameters)
    {
        return $"Host={parameters.host};Port={parameters.port};Database={parameters.database};Username={parameters.username};Password={parameters.password};";
    }

    /// <summary>
    /// Gets appropriate SSL settings based on environment
    /// </summary>
    private static string GetPostgreSqlSslSettings(string environment)
    {
        return environment.Equals("Development", StringComparison.OrdinalIgnoreCase) 
            ? "SSL Mode=Prefer;" 
            : "SSL Mode=Require;Trust Server Certificate=false;";
    }

    private string GenerateSqlServerConnectionString(IConfiguration configuration, string environment, bool isContainerized)
    {
        var server = configuration["Database:SqlServer:Server"] ?? 
                     Environment.GetEnvironmentVariable("SQL_SERVER") ?? 
                     (isContainerized ? "sqlserver" : "localhost");
        
        var database = configuration["Database:SqlServer:Database"] ?? 
                       Environment.GetEnvironmentVariable("SQL_DATABASE") ?? 
                       "SetlistStudio";
        
        var username = configuration["Database:SqlServer:Username"] ?? 
                       Environment.GetEnvironmentVariable("SQL_USER");
        
        var password = configuration["Database:SqlServer:Password"] ?? 
                       Environment.GetEnvironmentVariable("SQL_PASSWORD");

        string connectionString;
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            connectionString = $"Server={server};Database={database};User Id={username};Password={password};";
        }
        else
        {
            connectionString = $"Server={server};Database={database};Integrated Security=true;";
        }

        // Add encryption for production
        if (!environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += "Encrypt=true;TrustServerCertificate=false;";
        }
        else
        {
            connectionString += "Encrypt=false;";
        }

        return connectionString;
    }

    private string GenerateSqliteConnectionString(IConfiguration configuration, string environment, bool isContainerized)
    {
        if (environment.Equals("Test", StringComparison.OrdinalIgnoreCase))
        {
            return "Data Source=:memory:";
        }

        var dataDirectory = isContainerized ? "/app/data" : Path.Combine(Directory.GetCurrentDirectory(), "data");
        var databaseFile = Path.Combine(dataDirectory, "setliststudio.db");
        
        // Normalize path separators for consistent behavior across platforms
        databaseFile = databaseFile.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        
        // Ensure directory exists - skip during testing to avoid permission issues
        if (!IsRunningInTests())
        {
            Directory.CreateDirectory(dataDirectory);
        }
        
        return $"Data Source={databaseFile}";
    }

    /// <summary>
    /// Detects if the application is running in a test environment
    /// </summary>
    private static bool IsRunningInTests()
    {
        // Check if any test framework assemblies are loaded
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        return loadedAssemblies.Any(assembly => 
            assembly.FullName?.Contains("xunit") == true ||
            assembly.FullName?.Contains("nunit") == true ||
            assembly.FullName?.Contains("mstest") == true ||
            assembly.FullName?.Contains("SetlistStudio.Tests") == true);
    }

    private void LoadPoolConfiguration(IConfiguration configuration)
    {
        MaxPoolSize = GetConfigurationValue(configuration, "Database:Pool:MaxSize", Provider == DatabaseProvider.PostgreSQL ? 100 : 50);
        MinPoolSize = GetConfigurationValue(configuration, "Database:Pool:MinSize", Provider == DatabaseProvider.PostgreSQL ? 5 : 2);
        ConnectionTimeout = GetConfigurationValue(configuration, "Database:Pool:ConnectionTimeout", 30);
        CommandTimeout = GetConfigurationValue(configuration, "Database:Pool:CommandTimeout", 120);
        EnablePooling = GetConfigurationValue(configuration, "Database:Pool:Enabled", true);

        // Adjust defaults based on provider
        if (Provider == DatabaseProvider.SQLite)
        {
            MaxPoolSize = Math.Min(MaxPoolSize, 20); // SQLite has lower concurrency limits
            EnablePooling = false; // SQLite doesn't benefit from connection pooling
        }
        else if (Provider == DatabaseProvider.PostgreSQL)
        {
            MaxPoolSize = Math.Max(MaxPoolSize, 50); // PostgreSQL scales better with larger pools
        }
    }

    private static T GetConfigurationValue<T>(IConfiguration configuration, string key, T defaultValue)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
}