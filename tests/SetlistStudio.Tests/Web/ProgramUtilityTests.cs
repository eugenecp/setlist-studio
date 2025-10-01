using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using SetlistStudio.Infrastructure.Data;
using FluentAssertions;
using Xunit;
using System.Reflection;

namespace SetlistStudio.Tests.Web;

/// <summary>
/// Simple branch coverage tests for Program.cs utility methods.
/// Focuses on easily testable static methods to improve coverage.
/// </summary>
public class ProgramUtilityTests
{
    [Fact]
    public void GetDatabaseConnectionString_WithCustomConnection_ReturnsCustomValue()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=custom.db"
            })
            .Build();

        // Act
        var result = GetDatabaseConnectionStringViaReflection(config);

        // Assert
        result.Should().Be("Data Source=custom.db");
    }

    [Fact]
    public void GetDatabaseConnectionString_WithTestEnvironment_ReturnsMemoryConnection()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

            // Act
            var result = GetDatabaseConnectionStringViaReflection(config);

            // Assert
            result.Should().Be("Data Source=:memory:");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
        }
    }

    [Fact]
    public void GetDatabaseConnectionString_WithContainerFlag_ReturnsContainerPath()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");

            // Act
            var result = GetDatabaseConnectionStringViaReflection(config);

            // Assert
            result.Should().Be("Data Source=/app/data/setliststudio.db");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", originalContainer);
        }
    }

    [Fact]
    public void ConfigureDatabaseProvider_WithSqliteString_UsesSqlite()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>();
        var connectionString = "Data Source=test.db";

        // Act
        ConfigureDatabaseProviderViaReflection(options, connectionString);
        var context = new SetlistStudioDbContext(options.Options);

        // Assert
        context.Database.ProviderName.Should().Contain("Sqlite");
    }

    [Fact] 
    public void ConfigureDatabaseProvider_WithSqlServerString_UsesSqlServer()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>();
        var connectionString = "Server=localhost;Database=TestDb;";

        // Act
        ConfigureDatabaseProviderViaReflection(options, connectionString);
        var context = new SetlistStudioDbContext(options.Options);

        // Assert
        context.Database.ProviderName.Should().Contain("SqlServer");
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("", "", false)]
    [InlineData("   ", "   ", false)]
    [InlineData("YOUR_CLIENT_ID", "secret", false)]
    [InlineData("client", "YOUR_SECRET", false)]
    [InlineData("valid-client", "valid-secret", true)]
    [InlineData("google-client-123", "google-secret-456", true)]
    public void IsValidAuthenticationCredentials_ValidatesCorrectly(string? id, string? secret, bool expected)
    {
        // Act
        var result = IsValidAuthenticationCredentialsViaReflection(id, secret);

        // Assert
        result.Should().Be(expected);
    }

    // Helper methods using reflection to access static methods
    private static string GetDatabaseConnectionStringViaReflection(IConfiguration configuration)
    {
        var programType = typeof(Program);
        
        // Find the generated method name for GetDatabaseConnectionString
        var method = programType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name.Contains("GetDatabaseConnectionString"));
        
        if (method == null)
        {
            var allMethods = programType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.IsStatic)
                .Select(m => m.Name)
                .ToArray();
            throw new InvalidOperationException($"GetDatabaseConnectionString method not found. Available static methods: {string.Join(", ", allMethods)}");
        }
        
        return (string)method.Invoke(null, new object[] { configuration })!;
    }

    private static void ConfigureDatabaseProviderViaReflection(DbContextOptionsBuilder options, string connectionString)
    {
        var programType = typeof(Program);
        
        // Find the generated method name for ConfigureDatabaseProvider
        var method = programType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name.Contains("ConfigureDatabaseProvider"));
        
        if (method == null)
        {
            var allMethods = programType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.IsStatic)
                .Select(m => m.Name)
                .ToArray();
            throw new InvalidOperationException($"ConfigureDatabaseProvider method not found. Available static methods: {string.Join(", ", allMethods)}");
        }
        
        method.Invoke(null, new object[] { options, connectionString });
    }

    private static bool IsValidAuthenticationCredentialsViaReflection(string? id, string? secret)
    {
        var programType = typeof(Program);
        
        // Find the generated method name for IsValidAuthenticationCredentials
        var method = programType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name.Contains("IsValidAuthenticationCredentials"));
        
        if (method == null)
        {
            var allMethods = programType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.IsStatic)
                .Select(m => m.Name)
                .ToArray();
            throw new InvalidOperationException($"IsValidAuthenticationCredentials method not found. Available static methods: {string.Join(", ", allMethods)}");
        }
        
        return (bool)method.Invoke(null, new object?[] { id, secret })!;
    }
}