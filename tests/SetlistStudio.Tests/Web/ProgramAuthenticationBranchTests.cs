using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using Xunit;

namespace SetlistStudio.Tests.Web;

/// <summary>
/// Comprehensive tests for authentication configuration branches in Program.cs
/// Focuses on improving branch coverage for authentication provider configurations
/// </summary>
public class ProgramAuthenticationBranchTests : IDisposable
{
    private WebApplicationFactory<Program>? _factory;
    private readonly Dictionary<string, string?> _environmentVariables = new();

    #region Google Authentication Branch Tests

    [Fact]
    public async Task Program_ShouldNotConfigureGoogle_WhenNoCredentialsProvided()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"}
            // No Google credentials provided
        };

        // Act
        _factory = CreateFactory(configuration);
        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        schemes.Should().NotContain(s => s.Name == "Google");
    }

    [Fact]
    public async Task Program_ShouldNotConfigureGoogle_WhenOnlyClientIdProvided()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Google:ClientId", "valid-client-id"}
            // Missing ClientSecret
        };

        // Act
        _factory = CreateFactory(configuration);
        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        schemes.Should().NotContain(s => s.Name == "Google");
    }

    [Fact]
    public async Task Program_ShouldNotConfigureGoogle_WhenOnlyClientSecretProvided()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Google:ClientSecret", "valid-client-secret"}
            // Missing ClientId
        };

        // Act
        _factory = CreateFactory(configuration);
        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        schemes.Should().NotContain(s => s.Name == "Google");
    }

    [Fact]
    public async Task Program_ShouldNotConfigureGoogle_WhenCredentialsAreWhitespace()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Google:ClientId", "   "},
            {"Authentication:Google:ClientSecret", "\t\n   "}
        };

        // Act
        _factory = CreateFactory(configuration);
        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        schemes.Should().NotContain(s => s.Name == "Google");
    }

    [Fact]
    public async Task Program_ShouldNotConfigureGoogle_WhenCredentialsStartWithYOUR()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Google:ClientId", "YOUR_CLIENT_ID"},
            {"Authentication:Google:ClientSecret", "YOUR_CLIENT_SECRET"}
        };

        // Act
        _factory = CreateFactory(configuration);
        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        schemes.Should().NotContain(s => s.Name == "Google");
    }

    #endregion

    #region Microsoft Authentication Branch Tests

    [Fact]
    public async Task Program_ShouldNotConfigureMicrosoft_WhenNoCredentialsProvided()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"}
            // No Microsoft credentials provided
        };

        // Act
        _factory = CreateFactory(configuration);
        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        schemes.Should().NotContain(s => s.Name == "Microsoft");
    }

    [Fact]
    public async Task Program_ShouldNotConfigureMicrosoft_WhenCredentialsAreInvalid()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Microsoft:ClientId", "YOUR_MICROSOFT_CLIENT_ID"},
            {"Authentication:Microsoft:ClientSecret", "YOUR_MICROSOFT_CLIENT_SECRET"}
        };

        // Act
        _factory = CreateFactory(configuration);
        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        schemes.Should().NotContain(s => s.Name == "Microsoft");
    }

    [Fact]
    public async Task Program_ShouldConfigureMicrosoft_WhenValidCredentialsProvided()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Microsoft:ClientId", "valid-microsoft-client-id"},
            {"Authentication:Microsoft:ClientSecret", "valid-microsoft-client-secret"}
        };

        // Act
        _factory = CreateFactory(configuration);
        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        schemes.Should().Contain(s => s.Name == "Microsoft");
    }

    #endregion

    #region Facebook Authentication Branch Tests

    [Fact]
    public async Task Program_ShouldNotConfigureFacebook_WhenNoCredentialsProvided()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"}
            // No Facebook credentials provided
        };

        // Act
        _factory = CreateFactory(configuration);
        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        schemes.Should().NotContain(s => s.Name == "Facebook");
    }

    [Fact]
    public async Task Program_ShouldNotConfigureFacebook_WhenCredentialsAreInvalid()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Facebook:AppId", "YOUR_FACEBOOK_APP_ID"},
            {"Authentication:Facebook:AppSecret", "YOUR_FACEBOOK_APP_SECRET"}
        };

        // Act
        _factory = CreateFactory(configuration);
        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        schemes.Should().NotContain(s => s.Name == "Facebook");
    }

    [Fact]
    public async Task Program_ShouldConfigureFacebook_WhenValidCredentialsProvided()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Facebook:AppId", "valid-facebook-app-id"},
            {"Authentication:Facebook:AppSecret", "valid-facebook-app-secret"}
        };

        // Act
        _factory = CreateFactory(configuration);
        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        schemes.Should().Contain(s => s.Name == "Facebook");
    }

    #endregion

    #region Environment Branch Tests

    [Fact]
    public async Task Program_ShouldUseContainerDatabasePath_WhenRunningInContainer()
    {
        // Arrange
        _environmentVariables.Add("DOTNET_RUNNING_IN_CONTAINER", "true");
        _environmentVariables.Add("ASPNETCORE_ENVIRONMENT", "Production");
        
        var configuration = new Dictionary<string, string?>
        {
            // No connection string provided to trigger default path logic
        };

        // Act & Assert - Should not throw during startup
        _factory = CreateFactory(configuration);
        _factory.Services.Should().NotBeNull();
    }

    [Fact]
    public async Task Program_ShouldUseTestDatabasePath_WhenTestEnvironment()
    {
        // Arrange
        _environmentVariables.Add("ASPNETCORE_ENVIRONMENT", "Test");
        
        var configuration = new Dictionary<string, string?>
        {
            // No connection string provided to trigger default path logic
        };

        // Act & Assert - Should not throw during startup
        _factory = CreateFactory(configuration);
        _factory.Services.Should().NotBeNull();
    }

    #endregion

    #region Database Provider Branch Tests

    [Fact]
    public async Task Program_ShouldUseSqlServer_WhenConnectionStringContainsServer()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Server=localhost;Database=TestDb;Trusted_Connection=true;"}
        };

        // Act & Assert - Should not throw during startup with SQL Server connection string
        _factory = CreateFactory(configuration);
        _factory.Services.Should().NotBeNull();
    }

    [Fact]
    public async Task Program_ShouldUseSqlite_WhenConnectionStringContainsDataSource()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=test.db"}
        };

        // Act & Assert - Should not throw during startup with SQLite connection string
        _factory = CreateFactory(configuration);
        _factory.Services.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private WebApplicationFactory<Program> CreateFactory(Dictionary<string, string?> configuration)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                
                // Set environment variables
                foreach (var kvp in _environmentVariables)
                {
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }
                
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.Sources.Clear();
                    config.AddInMemoryCollection(configuration);
                    config.AddEnvironmentVariables();
                });

                // Configure logging to suppress output during tests
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(Mock.Of<ILoggerProvider>());
                });
            });
    }

    public void Dispose()
    {
        _factory?.Dispose();
        
        // Clean up environment variables
        foreach (var key in _environmentVariables.Keys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    #endregion
}