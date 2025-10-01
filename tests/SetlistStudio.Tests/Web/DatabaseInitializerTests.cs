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