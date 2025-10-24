using SetlistStudio.Infrastructure.Configuration;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Tests.Helpers;
using Xunit;

namespace SetlistStudio.Tests.Integration;

/// <summary>
/// Base class for integration tests that need database access
/// Provides support for multiple database providers with proper cleanup
/// </summary>
public abstract class DatabaseIntegrationTestBase : IAsyncDisposable
{
    protected readonly TestDatabaseFactory DatabaseFactory;
    protected readonly SetlistStudioDbContext Context;
    protected readonly ReadOnlySetlistStudioDbContext ReadOnlyContext;

    protected DatabaseIntegrationTestBase(DatabaseProvider provider = DatabaseProvider.InMemory)
    {
        DatabaseFactory = provider switch
        {
            DatabaseProvider.InMemory => TestDatabaseFactory.CreateInMemory(),
            DatabaseProvider.SQLite => TestDatabaseFactory.CreateSqlite(),
            _ => throw new ArgumentException($"Provider {provider} not supported in constructor. Use CreateAsync for PostgreSQL.")
        };
        
        Context = DatabaseFactory.CreateContext();
        ReadOnlyContext = DatabaseFactory.CreateReadOnlyContext();
        
        // Initialize database synchronously for simple providers
        DatabaseFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates a test instance with PostgreSQL using Testcontainers
    /// </summary>
    protected static async Task<T> CreateWithPostgreSqlAsync<T>() where T : DatabaseIntegrationTestBase
    {
        var factory = await TestDatabaseFactory.CreatePostgreSqlAsync();
        await factory.InitializeDatabaseAsync();
        
        // Use reflection to create the test instance with the factory
        var instance = (T)Activator.CreateInstance(typeof(T), factory)!;
        return instance;
    }

    /// <summary>
    /// Constructor for PostgreSQL tests (called via reflection)
    /// </summary>
    protected DatabaseIntegrationTestBase(TestDatabaseFactory factory)
    {
        DatabaseFactory = factory;
        Context = DatabaseFactory.CreateContext();
        ReadOnlyContext = DatabaseFactory.CreateReadOnlyContext();
    }

    /// <summary>
    /// Clears all data from the database for test isolation
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        await DatabaseFactory.ClearDatabaseAsync();
    }

    /// <summary>
    /// Gets the database provider being used for the test
    /// </summary>
    protected DatabaseProvider DatabaseProvider => DatabaseFactory.Provider;

    public virtual async ValueTask DisposeAsync()
    {
        Context?.Dispose();
        ReadOnlyContext?.Dispose();
        await DatabaseFactory.DisposeAsync();
    }
}

/// <summary>
/// Collection definition for PostgreSQL integration tests
/// Ensures PostgreSQL container is shared across tests in the same collection
/// </summary>
[CollectionDefinition("PostgreSQL Integration")]
public class PostgreSqlIntegrationTestCollection : ICollectionFixture<PostgreSqlTestFixture>
{
}

/// <summary>
/// Shared fixture for PostgreSQL integration tests
/// </summary>
public class PostgreSqlTestFixture : IAsyncLifetime
{
    public TestDatabaseFactory DatabaseFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        try
        {
            DatabaseFactory = await TestDatabaseFactory.CreatePostgreSqlAsync();
            await DatabaseFactory.InitializeDatabaseAsync();
        }
        catch (Exception ex)
        {
            // If PostgreSQL setup fails, skip these tests
            // This happens when Docker isn't available or PostgreSQL can't start
            throw new SkipException($"PostgreSQL integration tests skipped: {ex.Message}");
        }
    }

    public async Task DisposeAsync()
    {
        if (DatabaseFactory != null)
        {
            await DatabaseFactory.DisposeAsync();
        }
    }
}

/// <summary>
/// Base class for PostgreSQL-specific integration tests
/// </summary>
[Collection("PostgreSQL Integration")]
public abstract class PostgreSqlIntegrationTestBase : IAsyncDisposable
{
    protected readonly TestDatabaseFactory DatabaseFactory;
    protected readonly SetlistStudioDbContext Context;
    protected readonly ReadOnlySetlistStudioDbContext ReadOnlyContext;

    protected PostgreSqlIntegrationTestBase(PostgreSqlTestFixture fixture)
    {
        if (fixture.DatabaseFactory == null)
        {
            throw new SkipException("PostgreSQL is not available for testing");
        }
        
        DatabaseFactory = fixture.DatabaseFactory;
        Context = DatabaseFactory.CreateContext();
        ReadOnlyContext = DatabaseFactory.CreateReadOnlyContext();
    }

    /// <summary>
    /// Clears all data from the database for test isolation
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        await DatabaseFactory.ClearDatabaseAsync();
    }

    public virtual async ValueTask DisposeAsync()
    {
        Context?.Dispose();
        ReadOnlyContext?.Dispose();
        
        // Don't dispose the factory - it's shared across tests
        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }
}

/// <summary>
/// Exception thrown when tests should be skipped due to unavailable resources
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
    public SkipException(string message, Exception innerException) : base(message, innerException) { }
}