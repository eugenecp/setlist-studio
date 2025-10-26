using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Infrastructure.Configuration;
using SetlistStudio.Core.Interfaces;
using Xunit;

namespace SetlistStudio.Tests.Infrastructure.Configuration;

public class DatabaseConfigurationTests
{
    private readonly Mock<ILogger<DatabaseConfiguration>> _mockLogger;
    private readonly IConfiguration _configuration;

    public DatabaseConfigurationTests()
    {
        _mockLogger = new Mock<ILogger<DatabaseConfiguration>>();
        
        // Create test configuration
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "SQLite",
            ["ConnectionStrings:WriteConnection"] = "Data Source=test.db"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    [Fact]
    public void Constructor_ShouldLoadConfiguration_Successfully()
    {
        // Act
        var config = new DatabaseConfiguration(_configuration, _mockLogger.Object);

        // Assert
        config.Provider.Should().Be(DatabaseProvider.SQLite);
        config.WriteConnectionString.Should().Be("Data Source=test.db");
        config.MaxPoolSize.Should().Be(20); // SQLite adjusted default
        config.MinPoolSize.Should().Be(2); // SQLite adjusted default  
        config.ConnectionTimeout.Should().Be(30);
        config.CommandTimeout.Should().Be(120); // Default command timeout is 120, not 300
        config.EnablePooling.Should().BeFalse(); // SQLite disables pooling
    }

    [Fact]
    public void Constructor_WithCustomPoolConfiguration_ShouldLoadValues()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "PostgreSQL",
            ["ConnectionStrings:WriteConnection"] = "Data Source=test.db",
            ["Database:Pool:MaxSize"] = "75",
            ["Database:Pool:MinSize"] = "10",
            ["Database:Pool:ConnectionTimeout"] = "60",
            ["Database:Pool:CommandTimeout"] = "600",
            ["Database:Pool:Enabled"] = "false"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

        // Assert
        databaseConfig.MaxPoolSize.Should().Be(75);
        databaseConfig.MinPoolSize.Should().Be(10);
        databaseConfig.ConnectionTimeout.Should().Be(60);
        databaseConfig.CommandTimeout.Should().Be(600);
        databaseConfig.EnablePooling.Should().BeFalse();
    }

    [Fact]
    public void GenerateDefaultConnectionString_WithPostgreSQLProvider_ShouldGenerateConnectionString()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "PostgreSQL",
            ["Database:PostgreSQL:Host"] = "localhost",
            ["Database:PostgreSQL:Port"] = "5432",
            ["Database:PostgreSQL:Database"] = "testdb",
            ["Database:PostgreSQL:Username"] = "testuser",
            ["Database:PostgreSQL:Password"] = "testpass"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

        // Act & Assert
        databaseConfig.WriteConnectionString.Should().Contain("Host=localhost");
        databaseConfig.WriteConnectionString.Should().Contain("Port=5432");
        databaseConfig.WriteConnectionString.Should().Contain("Database=testdb");
        databaseConfig.WriteConnectionString.Should().Contain("Username=testuser");
        databaseConfig.WriteConnectionString.Should().Contain("Password=testpass");
    }

    [Fact]
    public void GenerateDefaultConnectionString_WithPostgreSQLInProduction_ShouldIncludeSSL()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "PostgreSQL",
            ["Database:PostgreSQL:Host"] = "prodhost",
            ["Database:PostgreSQL:Port"] = "5432",
            ["Database:PostgreSQL:Database"] = "proddb",
            ["Database:PostgreSQL:Username"] = "produser",
            ["Database:PostgreSQL:Password"] = "prodpass"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        try
        {
            var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

            // Act & Assert
            databaseConfig.WriteConnectionString.Should().Contain("SSL Mode=Require");
            databaseConfig.WriteConnectionString.Should().Contain("Trust Server Certificate=false");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
    }

    [Fact]
    public void GenerateDefaultConnectionString_WithSqlServerProvider_ShouldGenerateConnectionString()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "SqlServer",
            ["Database:SqlServer:Server"] = "localhost",
            ["Database:SqlServer:Database"] = "TestDB",
            ["Database:SqlServer:Username"] = "testuser",
            ["Database:SqlServer:Password"] = "testpass"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

        // Act & Assert
        databaseConfig.WriteConnectionString.Should().Contain("Server=localhost");
        databaseConfig.WriteConnectionString.Should().Contain("Database=TestDB");
        databaseConfig.WriteConnectionString.Should().Contain("User Id=testuser");
        databaseConfig.WriteConnectionString.Should().Contain("Password=testpass");
    }

    [Fact]
    public void GenerateDefaultConnectionString_WithSqlServerInProduction_ShouldIncludeEncryption()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "SqlServer",
            ["Database:SqlServer:Server"] = "prodserver",
            ["Database:SqlServer:Database"] = "ProdDB",
            ["Database:SqlServer:Username"] = "produser",
            ["Database:SqlServer:Password"] = "prodpass"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        try
        {
            var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

            // Act & Assert
            databaseConfig.WriteConnectionString.Should().Contain("Encrypt=true");
            databaseConfig.WriteConnectionString.Should().Contain("TrustServerCertificate=false");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
    }

    [Fact]
    public void GenerateDefaultConnectionString_WithSqlServerNoCredentials_ShouldUseIntegratedSecurity()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "SqlServer",
            ["Database:SqlServer:Server"] = "localhost",
            ["Database:SqlServer:Database"] = "TestDB"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

        // Act & Assert
        databaseConfig.WriteConnectionString.Should().Contain("Integrated Security=true");
    }

    [Fact]
    public void Constructor_WithUnknownProvider_ShouldDefaultToSQLite()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "UnknownProvider",
            ["ConnectionStrings:WriteConnection"] = "Data Source=test.db"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

        // Assert
        databaseConfig.Provider.Should().Be(DatabaseProvider.SQLite);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown database provider 'UnknownProvider', defaulting to SQLite")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithReadReplicas_ShouldConfigureReadConnections()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "SQLite",
            ["ConnectionStrings:WriteConnection"] = "Data Source=write.db",
            ["Database:ReadReplicas:0"] = "Data Source=read1.db",
            ["Database:ReadReplicas:1"] = "Data Source=read2.db"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

        // Assert
        databaseConfig.HasReadReplicas.Should().BeTrue();
        databaseConfig.ReadConnectionStrings.Should().HaveCount(2);
        databaseConfig.ReadConnectionStrings.Should().Contain("Data Source=read1.db");
        databaseConfig.ReadConnectionStrings.Should().Contain("Data Source=read2.db");
    }

    [Fact]
    public void GetReadConnectionString_WithReadReplicas_ShouldRotateConnections()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "SQLite",
            ["ConnectionStrings:WriteConnection"] = "Data Source=write.db",
            ["Database:ReadReplicas:0"] = "Data Source=read1.db",
            ["Database:ReadReplicas:1"] = "Data Source=read2.db"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

        // Act
        var firstConnection = databaseConfig.GetReadConnectionString();
        var secondConnection = databaseConfig.GetReadConnectionString();
        var thirdConnection = databaseConfig.GetReadConnectionString();

        // Assert
        firstConnection.Should().Be("Data Source=read1.db");
        secondConnection.Should().Be("Data Source=read2.db");
        thirdConnection.Should().Be("Data Source=read1.db"); // Should rotate back
    }

    [Fact]
    public void GetReadConnectionString_WithoutReadReplicas_ShouldReturnWriteConnection()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "SQLite",
            ["ConnectionStrings:WriteConnection"] = "Data Source=write.db"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

        // Act
        var readConnection = databaseConfig.GetReadConnectionString();

        // Assert
        readConnection.Should().Be("Data Source=write.db");
        databaseConfig.HasReadReplicas.Should().BeFalse();
    }

    [Fact]
    public void ReadConnectionStrings_Property_ShouldReturnReadOnlyList()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "SQLite",
            ["ConnectionStrings:WriteConnection"] = "Data Source=write.db",
            ["Database:ReadReplicas:0"] = "Data Source=read1.db"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

        // Act
        var readConnections = databaseConfig.ReadConnectionStrings;

        // Assert
        readConnections.Should().NotBeNull();
        readConnections.Should().HaveCount(1);
        readConnections.Should().Contain("Data Source=read1.db");
        
        // Verify it's read-only by attempting to cast to a mutable list
        readConnections.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void Constructor_WithDefaultConnection_ShouldUseDefaultForWrite()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "SQLite",
            ["ConnectionStrings:DefaultConnection"] = "Data Source=default.db"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

        // Assert
        databaseConfig.WriteConnectionString.Should().Be("Data Source=default.db");
    }

    [Fact]
    public void Constructor_WithTestEnvironment_ShouldUseInMemoryDatabase()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "SQLite"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        try
        {
            // Act
            var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

            // Assert
            databaseConfig.WriteConnectionString.Should().Be("Data Source=:memory:");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
    }

    [Fact]
    public void Constructor_WithContainerizedEnvironment_ShouldUseContainerDefaults()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        
        var configData = new Dictionary<string, string>
        {
            ["Database:Provider"] = "PostgreSQL"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        try
        {
            // Act
            var databaseConfig = new DatabaseConfiguration(config, _mockLogger.Object);

            // Assert
            databaseConfig.WriteConnectionString.Should().Contain("Host=postgres");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
    }
}