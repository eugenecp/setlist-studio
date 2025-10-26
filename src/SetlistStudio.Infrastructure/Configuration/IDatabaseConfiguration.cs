namespace SetlistStudio.Infrastructure.Configuration;

/// <summary>
/// Interface for database configuration management
/// Supports multiple providers with connection pooling and read replica capabilities
/// </summary>
public interface IDatabaseConfiguration
{
    /// <summary>
    /// The primary database provider type
    /// </summary>
    DatabaseProvider Provider { get; }

    /// <summary>
    /// Connection string for write operations (master/primary database)
    /// </summary>
    string WriteConnectionString { get; }

    /// <summary>
    /// Connection strings for read operations (read replicas)
    /// Falls back to write connection if no read replicas configured
    /// </summary>
    IReadOnlyList<string> ReadConnectionStrings { get; }

    /// <summary>
    /// Maximum size of the connection pool
    /// </summary>
    int MaxPoolSize { get; }

    /// <summary>
    /// Minimum size of the connection pool
    /// </summary>
    int MinPoolSize { get; }

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    int ConnectionTimeout { get; }

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    int CommandTimeout { get; }

    /// <summary>
    /// Whether to enable connection pooling
    /// </summary>
    bool EnablePooling { get; }

    /// <summary>
    /// Whether read replicas are configured and should be used
    /// </summary>
    bool HasReadReplicas { get; }

    /// <summary>
    /// Gets a connection string for read operations
    /// Uses round-robin selection among read replicas
    /// </summary>
    string GetReadConnectionString();

    /// <summary>
    /// Validates the database configuration
    /// </summary>
    /// <returns>True if configuration is valid, false otherwise</returns>
    bool IsValid();
}

/// <summary>
/// Supported database providers
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    /// SQLite - for development and testing
    /// </summary>
    SQLite,

    /// <summary>
    /// PostgreSQL - for production with scalability
    /// </summary>
    PostgreSQL,

    /// <summary>
    /// SQL Server - for enterprise environments
    /// </summary>
    SqlServer,

    /// <summary>
    /// In-Memory - for unit testing
    /// </summary>
    InMemory
}