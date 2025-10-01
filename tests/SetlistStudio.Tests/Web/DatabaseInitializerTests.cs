using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Web.Services;
using FluentAssertions;
using Xunit;

namespace SetlistStudio.Tests.Web;

public class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_ShouldSkipInitialization_WhenUsingInMemoryDatabase()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var serviceProvider = CreateServiceProvider(canConnect: true, ensureCreatedResult: false, songCount: 5);

        // Act
        await DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object);

        // Assert - Should skip initialization for in-memory databases
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Information, "In-memory database detected - skipping initialization for test environment");
        VerifyLogMessage(mockLogger, LogLevel.Information, "Database initialization completed (skipped for tests)");
    }

    [Fact]
    public async Task InitializeAsync_ShouldSkipConnectionTest_WhenUsingInMemoryDatabase()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var serviceProvider = CreateServiceProvider(canConnect: true, ensureCreatedResult: false, songCount: 0);

        // Act
        await DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object);

        // Assert - Should skip initialization for in-memory databases
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Information, "In-memory database detected - skipping initialization for test environment");
        VerifyLogMessage(mockLogger, LogLevel.Information, "Database initialization completed (skipped for tests)");
    }

    [Fact]
    public async Task InitializeAsync_ShouldSkipDatabaseCreation_WhenUsingInMemoryDatabase()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var serviceProvider = CreateServiceProvider(canConnect: false, ensureCreatedResult: true, songCount: 0);

        // Act
        await DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object);

        // Assert - Should skip initialization for in-memory databases
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Information, "In-memory database detected - skipping initialization for test environment");
        VerifyLogMessage(mockLogger, LogLevel.Information, "Database initialization completed (skipped for tests)");
    }



    [Fact]
    public async Task InitializeAsync_ShouldSkipInitialization_WhenUsingInMemoryDatabaseWithData()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var serviceProvider = CreateServiceProvider(canConnect: true, ensureCreatedResult: true, songCount: 5);

        // Act
        await DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object);

        // Assert - Should skip initialization for in-memory databases
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Information, "In-memory database detected - skipping initialization for test environment");
        VerifyLogMessage(mockLogger, LogLevel.Information, "Database initialization completed (skipped for tests)");
    }

    [Fact]
    public async Task InitializeAsync_ShouldSkipDatabaseCreationLogging_WhenUsingInMemoryDatabase()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var serviceProvider = CreateServiceProvider(canConnect: true, ensureCreatedResult: true, songCount: 0);

        // Act
        await DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object);

        // Assert - Should skip initialization for in-memory databases
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Information, "In-memory database detected - skipping initialization for test environment");
        VerifyLogMessage(mockLogger, LogLevel.Information, "Database initialization completed (skipped for tests)");
    }

    [Fact]
    public async Task InitializeAsync_ShouldSkipSongCounting_WhenUsingInMemoryDatabase()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var serviceProvider = CreateServiceProvider(canConnect: true, ensureCreatedResult: false, songCount: 42);

        // Act
        await DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object);

        // Assert - Should skip initialization for in-memory databases
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Information, "In-memory database detected - skipping initialization for test environment");
        VerifyLogMessage(mockLogger, LogLevel.Information, "Database initialization completed (skipped for tests)");
    }

    [Theory]
    [InlineData(true, false, 0)]
    [InlineData(true, false, 10)]
    [InlineData(false, true, 0)]
    [InlineData(false, true, 5)]
    public async Task InitializeAsync_ShouldSkipInitializationForAllInMemoryDatabaseStates(
        bool canConnect, bool ensureCreatedResult, int songCount)
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var serviceProvider = CreateServiceProvider(canConnect, ensureCreatedResult, songCount);

        // Act
        var exception = await Record.ExceptionAsync(
            () => DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object));

        // Assert - Should skip initialization for all in-memory database states
        exception.Should().BeNull();
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Information, "In-memory database detected - skipping initialization for test environment");
        VerifyLogMessage(mockLogger, LogLevel.Information, "Database initialization completed (skipped for tests)");
    }

    private static IServiceProvider CreateServiceProvider(bool canConnect, bool ensureCreatedResult, int songCount)
    {
        var services = new ServiceCollection();
        
        // Use in-memory database for testing
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<SetlistStudioDbContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName);
        });

        var serviceProvider = services.BuildServiceProvider();
        
        // Seed the database with test data if needed
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
        
        if (songCount > 0)
        {
            for (int i = 1; i <= songCount; i++)
            {
                context.Songs.Add(new Song 
                { 
                    Id = i,
                    Title = $"Test Song {i}",
                    Artist = $"Test Artist {i}",
                    UserId = "test-user"
                });
            }
            context.SaveChanges();
        }
        
        return serviceProvider;
    }



    [Fact]
    public async Task InitializeAsync_ShouldLogWarning_WhenCannotConnectToDatabase()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var services = new ServiceCollection();
        
        // Use a SQLite connection string that will cause an exception during EnsureCreatedAsync
        // Use a malformed connection string that SQLite cannot handle
        services.AddDbContext<SetlistStudioDbContext>(options =>
        {
            options.UseSqlite("Data Source=/dev/null/invalid.db"); // This will fail on Linux as /dev/null is a device file
        });
        
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - The invalid database path should cause an exception
        var exception = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object));
        
        // Verify that we got the expected logging
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Error, "Database initialization failed:");
        // Don't verify specific connection string details as they might vary by platform
    }

    [Fact]
    public async Task InitializeAsync_ShouldLogConnectionString_WhenNotContainingDataSource()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var services = new ServiceCollection();
        
        // Use a connection string without "Data Source=" to test the other path
        // This will cause an exception but won't trigger the file info logging
        services.AddDbContext<SetlistStudioDbContext>(options =>
        {
            options.UseSqlite("Server=invalid;Database=test");
        });
        
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var exception = await Record.ExceptionAsync(
            () => DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object));
        
        exception.Should().NotBeNull();
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Error, "Database initialization failed:");
        VerifyLogMessage(mockLogger, LogLevel.Error, "Connection string: Server=invalid;Database=test");
    }

    [Fact]
    public async Task InitializeAsync_ShouldRetryCountingOnFailure_AndLogRetryAttempts()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var services = new ServiceCollection();
        
        // Create a custom DbContext that will fail on first Songs.CountAsync() call
        var mockContext = new Mock<SetlistStudioDbContext>();
        var mockSet = new Mock<DbSet<Song>>();
        
        // Configure the Songs property to return our mock DbSet
        mockContext.Setup(c => c.Songs).Returns(mockSet.Object);
        
        // Make Database.IsInMemory() return false to avoid early return
        var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
        mockDatabase.Setup(d => d.IsInMemory()).Returns(false);
        mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
        
        // Make CanConnectAsync return true and EnsureCreatedAsync return false
        mockDatabase.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockDatabase.Setup(d => d.EnsureCreatedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        
        // Configure CountAsync to fail once then succeed
        var callCount = 0;
        mockSet.Setup(s => s.CountAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Database temporarily unavailable");
                }
                return Task.FromResult(5);
            });

        services.AddSingleton(mockContext.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        await DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object);

        // Assert
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Information, "Database connection test: True");
        VerifyLogMessage(mockLogger, LogLevel.Information, "Database creation result: False");
        VerifyLogMessage(mockLogger, LogLevel.Warning, "Failed to query Songs table on attempt 1:");
        VerifyLogMessage(mockLogger, LogLevel.Information, "Current song count in database: 5");
        VerifyLogMessage(mockLogger, LogLevel.Information, "Database initialization completed successfully");
    }

    [Fact]
    public async Task InitializeAsync_ShouldLogDatabaseFileInfo_WhenExceptionOccursWithDataSource()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var services = new ServiceCollection();
        
        // Create a temporary database file to test file info logging
        var tempDbPath = Path.Combine(Path.GetTempPath(), "test_database.db");
        
        try
        {
            // Create the directory and file
            Directory.CreateDirectory(Path.GetDirectoryName(tempDbPath)!);
            File.WriteAllText(tempDbPath, "test");
            
            services.AddDbContext<SetlistStudioDbContext>(options =>
            {
                options.UseSqlite($"Data Source={tempDbPath};");
            });
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Use the service provider to create a scope and cause an exception
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
            
            // Force an exception by trying to create a table on a file that's not a valid database
            var exception = await Record.ExceptionAsync(
                () => DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object));

            // Assert
            exception.Should().NotBeNull();
            VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
            VerifyLogMessage(mockLogger, LogLevel.Error, "Database initialization failed:");
            VerifyLogMessage(mockLogger, LogLevel.Error, $"Connection string: Data Source={tempDbPath};");
            VerifyLogMessage(mockLogger, LogLevel.Error, "Database file path:");
            VerifyLogMessage(mockLogger, LogLevel.Error, "Database file exists: True");
            VerifyLogMessage(mockLogger, LogLevel.Error, "Database directory exists: True");
            VerifyLogMessage(mockLogger, LogLevel.Error, "Database directory permissions: Readable");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempDbPath))
            {
                File.Delete(tempDbPath);
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldHandleInnerException_WhenGettingDatabaseFileInfo()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var services = new ServiceCollection();
        
        // Create a mock service provider that will throw when getting connection string
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockScope = new Mock<IServiceScope>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        
        // Setup the service provider to return our mocked scope factory
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        
        mockScopeFactory.Setup(sf => sf.CreateScope()).Returns(mockScope.Object);
        
        // Create a context that will throw during database operations
        var mockContext = new Mock<SetlistStudioDbContext>();
        var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
        
        mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.IsInMemory()).Returns(false);
        mockDatabase.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));
        
        // Make GetConnectionString throw an exception to trigger the inner catch block
        mockDatabase.Setup(d => d.GetConnectionString())
            .Throws(new InvalidOperationException("Cannot get connection string"));
        
        mockScope.Setup(s => s.ServiceProvider).Returns(Mock.Of<IServiceProvider>(sp => 
            sp.GetService(typeof(SetlistStudioDbContext)) == mockContext.Object));

        // Act
        var exception = await Record.ExceptionAsync(
            () => DatabaseInitializer.InitializeAsync(mockServiceProvider.Object, mockLogger.Object));

        // Assert
        exception.Should().NotBeNull();
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Error, "Database initialization failed:");
        VerifyLogMessage(mockLogger, LogLevel.Error, "Failed to get database file information");
    }

    [Fact]
    public async Task InitializeAsync_ShouldHandleNullConnectionString_InErrorPath()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var services = new ServiceCollection();
        
        // Create a context that will fail connection but return null connection string
        var mockContext = new Mock<SetlistStudioDbContext>();
        var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
        
        mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.IsInMemory()).Returns(false);
        mockDatabase.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));
        mockDatabase.Setup(d => d.GetConnectionString()).Returns((string?)null);
        
        services.AddSingleton(mockContext.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var exception = await Record.ExceptionAsync(
            () => DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object));

        // Assert
        exception.Should().NotBeNull();
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Error, "Database initialization failed:");
        VerifyLogMessage(mockLogger, LogLevel.Error, "Connection string:");
    }

    [Fact]
    public async Task InitializeAsync_ShouldHandleNonDataSourceConnectionString_InErrorPath()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var services = new ServiceCollection();
        
        // Create a context that will fail connection with non-SQLite connection string
        var mockContext = new Mock<SetlistStudioDbContext>();
        var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
        
        mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.IsInMemory()).Returns(false);
        mockDatabase.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));
        mockDatabase.Setup(d => d.GetConnectionString()).Returns("Server=localhost;Database=test");
        
        services.AddSingleton(mockContext.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var exception = await Record.ExceptionAsync(
            () => DatabaseInitializer.InitializeAsync(serviceProvider, mockLogger.Object));

        // Assert
        exception.Should().NotBeNull();
        VerifyLogMessage(mockLogger, LogLevel.Information, "Starting database initialization...");
        VerifyLogMessage(mockLogger, LogLevel.Error, "Database initialization failed:");
        VerifyLogMessage(mockLogger, LogLevel.Error, "Connection string: Server=localhost;Database=test");
        
        // Should NOT log file information since connection string doesn't contain "Data Source="
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Database file path:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    private static void VerifyLogMessage(Mock<ILogger> mockLogger, LogLevel logLevel, string expectedMessage)
    {
        mockLogger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}