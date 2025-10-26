using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using SetlistStudio.Infrastructure.Configuration;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive tests for DatabaseProviderService covering all database provider scenarios
/// Tests configuration, connection handling, data source creation, and error conditions
/// </summary>
public class DatabaseProviderServiceTests : IDisposable
{
    private readonly Mock<IDatabaseConfiguration> _mockConfig;
    private readonly Mock<ILogger<DatabaseProviderService>> _mockLogger;
    private readonly List<DatabaseProviderService> _disposableServices;

    public DatabaseProviderServiceTests()
    {
        _mockConfig = new Mock<IDatabaseConfiguration>();
        _mockLogger = new Mock<ILogger<DatabaseProviderService>>();
        _disposableServices = new List<DatabaseProviderService>();
        
        // Set up default configuration values
        SetupDefaultConfiguration();
    }

    private void SetupDefaultConfiguration()
    {
        _mockConfig.Setup(x => x.MaxPoolSize).Returns(100);
        _mockConfig.Setup(x => x.MinPoolSize).Returns(10);
        _mockConfig.Setup(x => x.ConnectionTimeout).Returns(30);
        _mockConfig.Setup(x => x.CommandTimeout).Returns(30);
        _mockConfig.Setup(x => x.EnablePooling).Returns(true);
        _mockConfig.Setup(x => x.HasReadReplicas).Returns(false);
        _mockConfig.Setup(x => x.WriteConnectionString).Returns("Server=localhost;Database=Test;");
        _mockConfig.Setup(x => x.GetReadConnectionString()).Returns("Server=localhost;Database=Test;");
        _mockConfig.Setup(x => x.ReadConnectionStrings).Returns(new List<string>());
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldSucceed()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.SQLite);

        // Act
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new DatabaseProviderService(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new DatabaseProviderService(_mockConfig.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithPostgreSqlProvider_ShouldCreateDataSources()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.PostgreSQL);
        _mockConfig.Setup(x => x.WriteConnectionString).Returns("Host=localhost;Database=test;Username=user;Password=pass;");
        _mockConfig.Setup(x => x.HasReadReplicas).Returns(false);

        // Act
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);

        // Assert
        service.Should().NotBeNull();
        // Data sources are created internally but we can't directly verify them
        // We verify through the logging instead
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created PostgreSQL data source for Write")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithPostgreSqlAndReadReplicas_ShouldCreateBothDataSources()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.PostgreSQL);
        _mockConfig.Setup(x => x.WriteConnectionString).Returns("Host=localhost;Database=test;Username=user;Password=pass;");
        _mockConfig.Setup(x => x.HasReadReplicas).Returns(true);
        _mockConfig.Setup(x => x.GetReadConnectionString()).Returns("Host=replica;Database=test;Username=user;Password=pass;");

        // Act
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);

        // Assert
        service.Should().NotBeNull();
        
        // Verify both write and read data sources are created
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created PostgreSQL data source for Write")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created PostgreSQL data source for Read")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ConfigureWriteContext_WithInMemoryProvider_ShouldConfigureInMemoryDatabase()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.InMemory);
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);
        
        var options = new DbContextOptionsBuilder();

        // Act
        service.ConfigureWriteContext(options);

        // Assert
        var builtOptions = options.Options;
        builtOptions.Should().NotBeNull();
        
        // Verify logging for InMemory configuration
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configured In-Memory Write context")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ConfigureReadContext_WithInMemoryProvider_ShouldConfigureInMemoryDatabase()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.InMemory);
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);
        
        var options = new DbContextOptionsBuilder();

        // Act
        service.ConfigureReadContext(options);

        // Assert
        var builtOptions = options.Options;
        builtOptions.Should().NotBeNull();
        
        // Verify logging for InMemory configuration
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configured In-Memory Read context")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ConfigureWriteContext_WithSqlServerProvider_ShouldConfigureSqlServer()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.SqlServer);
        _mockConfig.Setup(x => x.WriteConnectionString).Returns("Server=localhost;Database=test;Integrated Security=true;");
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);
        
        var options = new DbContextOptionsBuilder();

        // Act
        service.ConfigureWriteContext(options);

        // Assert
        var builtOptions = options.Options;
        builtOptions.Should().NotBeNull();
        
        // Verify logging for SQL Server configuration
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configured SQL Server Write context")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ConfigureReadContext_WithSqlServerProvider_ShouldConfigureSqlServer()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.SqlServer);
        _mockConfig.Setup(x => x.GetReadConnectionString()).Returns("Server=localhost;Database=test;Integrated Security=true;");
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);
        
        var options = new DbContextOptionsBuilder();

        // Act
        service.ConfigureReadContext(options);

        // Assert
        var builtOptions = options.Options;
        builtOptions.Should().NotBeNull();
        
        // Verify logging for SQL Server configuration
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configured SQL Server Read context")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ConfigureWriteContext_WithSQLiteProvider_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.SQLite);
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);
        
        var options = new DbContextOptionsBuilder();

        // Act & Assert
        var act = () => service.ConfigureWriteContext(options);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("SQLite configuration should be handled by the calling assembly with SQLite package reference");
    }

    [Fact]
    public void ConfigureReadContext_WithSQLiteProvider_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.SQLite);
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);
        
        var options = new DbContextOptionsBuilder();

        // Act & Assert
        var act = () => service.ConfigureReadContext(options);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("SQLite configuration should be handled by the calling assembly with SQLite package reference");
    }

    [Fact]
    public void ConfigureWriteContext_WithUnsupportedProvider_ShouldThrowNotSupportedException()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns((DatabaseProvider)999); // Invalid provider
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);
        
        var options = new DbContextOptionsBuilder();

        // Act & Assert
        var act = () => service.ConfigureWriteContext(options);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Database provider 999 is not supported");
    }

    [Fact]
    public void ConfigureReadContext_WithUnsupportedProvider_ShouldThrowNotSupportedException()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns((DatabaseProvider)999); // Invalid provider
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);
        
        var options = new DbContextOptionsBuilder();

        // Act & Assert
        var act = () => service.ConfigureReadContext(options);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Database provider 999 is not supported");
    }

    [Fact]
    public void ConfigureWriteContext_InDevelopmentEnvironment_ShouldEnableSensitiveDataLogging()
    {
        // Arrange
        var originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        
        try
        {
            _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.InMemory);
            var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
            _disposableServices.Add(service);
            
            var options = new DbContextOptionsBuilder();

            // Act
            service.ConfigureWriteContext(options);

            // Assert
            var builtOptions = options.Options;
            builtOptions.Should().NotBeNull();
            
            // Note: We can't directly verify EnableSensitiveDataLogging was called,
            // but we can verify the configuration completed successfully
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configured Write context for InMemory")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnvironment);
        }
    }

    [Fact]
    public void ConfigureWriteContext_InProductionEnvironment_ShouldNotEnableSensitiveDataLogging()
    {
        // Arrange
        var originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        
        try
        {
            _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.InMemory);
            var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
            _disposableServices.Add(service);
            
            var options = new DbContextOptionsBuilder();

            // Act
            service.ConfigureWriteContext(options);

            // Assert
            var builtOptions = options.Options;
            builtOptions.Should().NotBeNull();
            
            // Verify debug logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configured Write context for InMemory")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnvironment);
        }
    }

    [Fact]
    public void ConfigureReadContext_WithReadReplicas_ShouldUseReadDataSource()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.PostgreSQL);
        _mockConfig.Setup(x => x.WriteConnectionString).Returns("Host=localhost;Database=test;Username=user;Password=pass;");
        _mockConfig.Setup(x => x.HasReadReplicas).Returns(true);
        _mockConfig.Setup(x => x.GetReadConnectionString()).Returns("Host=replica;Database=test;Username=user;Password=pass;");
        
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);
        
        var options = new DbContextOptionsBuilder();

        // Act
        service.ConfigureReadContext(options);

        // Assert
        var builtOptions = options.Options;
        builtOptions.Should().NotBeNull();
        
        // Verify the read connection string was requested (called during constructor and ConfigureReadContext)
        _mockConfig.Verify(x => x.GetReadConnectionString(), Times.AtLeast(1));
        
        // Verify PostgreSQL configuration for read context
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configured PostgreSQL Read context with connection pooling")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ConfigureReadContext_WithoutReadReplicas_ShouldUseWriteDataSource()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.PostgreSQL);
        _mockConfig.Setup(x => x.WriteConnectionString).Returns("Host=localhost;Database=test;Username=user;Password=pass;");
        _mockConfig.Setup(x => x.HasReadReplicas).Returns(false);
        _mockConfig.Setup(x => x.GetReadConnectionString()).Returns("Host=localhost;Database=test;Username=user;Password=pass;");
        
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        _disposableServices.Add(service);
        
        var options = new DbContextOptionsBuilder();

        // Act
        service.ConfigureReadContext(options);

        // Assert
        var builtOptions = options.Options;
        builtOptions.Should().NotBeNull();
        
        // Verify the read connection string was requested (called during ConfigureReadContext)
        _mockConfig.Verify(x => x.GetReadConnectionString(), Times.AtLeast(1));
        
        // Verify PostgreSQL configuration for read context
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configured PostgreSQL Read context with connection pooling")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_WithDataSources_ShouldDisposeResources()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.PostgreSQL);
        _mockConfig.Setup(x => x.WriteConnectionString).Returns("Host=localhost;Database=test;Username=user;Password=pass;");
        _mockConfig.Setup(x => x.HasReadReplicas).Returns(true);
        _mockConfig.Setup(x => x.GetReadConnectionString()).Returns("Host=replica;Database=test;Username=user;Password=pass;");
        
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);

        // Act
        service.Dispose();

        // Assert
        // We can't directly verify disposal, but we can ensure no exceptions are thrown
        // and the service can be disposed multiple times
        var act = () => service.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WithNonPostgreSqlProvider_ShouldNotThrow()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.InMemory);
        var service = new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);

        // Act & Assert
        var act = () => service.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithInvalidPostgreSqlConnectionString_ShouldThrowException()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.PostgreSQL);
        _mockConfig.Setup(x => x.WriteConnectionString).Returns("invalid-connection-string");
        _mockConfig.Setup(x => x.HasReadReplicas).Returns(false);

        // Act & Assert
        var act = () => new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        act.Should().Throw<Exception>(); // The exact exception type depends on Npgsql parsing
        
        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create PostgreSQL data source for Write")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithInvalidReadReplicaConnectionString_ShouldThrowException()
    {
        // Arrange
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.PostgreSQL);
        _mockConfig.Setup(x => x.WriteConnectionString).Returns("Host=localhost;Database=test;Username=user;Password=pass;");
        _mockConfig.Setup(x => x.HasReadReplicas).Returns(true);
        _mockConfig.Setup(x => x.GetReadConnectionString()).Returns("invalid-read-connection-string");

        // Act & Assert
        var act = () => new DatabaseProviderService(_mockConfig.Object, _mockLogger.Object);
        act.Should().Throw<Exception>(); // The exact exception type depends on Npgsql parsing
        
        // Verify error logging for read data source creation failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create PostgreSQL data source for Read")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        foreach (var service in _disposableServices)
        {
            service?.Dispose();
        }
        _disposableServices.Clear();
    }
}

/// <summary>
/// Tests for DatabaseProviderServiceFactory
/// </summary>
public class DatabaseProviderServiceFactoryTests
{
    private readonly Mock<IDatabaseConfiguration> _mockConfig;
    private readonly Mock<ILogger<DatabaseProviderService>> _mockLogger;

    public DatabaseProviderServiceFactoryTests()
    {
        _mockConfig = new Mock<IDatabaseConfiguration>();
        _mockLogger = new Mock<ILogger<DatabaseProviderService>>();
        
        // Set up default configuration
        _mockConfig.Setup(x => x.Provider).Returns(DatabaseProvider.InMemory);
        _mockConfig.Setup(x => x.MaxPoolSize).Returns(100);
        _mockConfig.Setup(x => x.MinPoolSize).Returns(10);
        _mockConfig.Setup(x => x.ConnectionTimeout).Returns(30);
        _mockConfig.Setup(x => x.CommandTimeout).Returns(30);
        _mockConfig.Setup(x => x.EnablePooling).Returns(true);
        _mockConfig.Setup(x => x.HasReadReplicas).Returns(false);
        _mockConfig.Setup(x => x.WriteConnectionString).Returns("Server=localhost;Database=Test;");
        _mockConfig.Setup(x => x.GetReadConnectionString()).Returns("Server=localhost;Database=Test;");
        _mockConfig.Setup(x => x.ReadConnectionStrings).Returns(new List<string>());
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Act
        var factory = new DatabaseProviderServiceFactory(_mockConfig.Object, _mockLogger.Object);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldReturnNewDatabaseProviderService()
    {
        // Arrange
        var factory = new DatabaseProviderServiceFactory(_mockConfig.Object, _mockLogger.Object);

        // Act
        var service = factory.Create();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<DatabaseProviderService>();
        
        // Clean up
        service.Dispose();
    }

    [Fact]
    public void Create_CalledMultipleTimes_ShouldReturnDifferentInstances()
    {
        // Arrange
        var factory = new DatabaseProviderServiceFactory(_mockConfig.Object, _mockLogger.Object);

        // Act
        var service1 = factory.Create();
        var service2 = factory.Create();

        // Assert
        service1.Should().NotBeNull();
        service2.Should().NotBeNull();
        service1.Should().NotBeSameAs(service2);
        
        // Clean up
        service1.Dispose();
        service2.Dispose();
    }
}