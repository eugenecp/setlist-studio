using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace SetlistStudio.Tests.Web;

/// <summary>
/// Tests for authentication configuration in Program.cs
/// These tests verify that authentication providers are properly configured
/// </summary>
public class ProgramAuthenticationTests : IDisposable
{
    private WebApplicationFactory<Program>? _factory;

    [Fact]
    public async Task Program_ShouldConfigureIdentityAuthentication()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"}
        };

        // Act
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.Sources.Clear(); // Clear all existing configuration sources
                    config.AddInMemoryCollection(configuration);
                });
            });

        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        // Identity authentication schemes should always be configured
        schemes.Should().Contain(s => s.Name == "Identity.Application");
        schemes.Should().Contain(s => s.Name == "Identity.External");
    }

    [Fact]
    public async Task Program_ShouldConfigureExternalAuth_WhenValidCredentialsProvided()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Google:ClientId", "valid-google-client-id"},
            {"Authentication:Google:ClientSecret", "valid-google-client-secret"},
            {"Authentication:Microsoft:ClientId", "valid-microsoft-client-id"},
            {"Authentication:Microsoft:ClientSecret", "valid-microsoft-client-secret"},
            {"Authentication:Facebook:AppId", "valid-facebook-app-id"},
            {"Authentication:Facebook:AppSecret", "valid-facebook-app-secret"}
        };

        // Act
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                
                // Set environment variables for authentication configuration
                foreach (var kvp in configuration)
                {
                    if (kvp.Value != null)
                    {
                        Environment.SetEnvironmentVariable(kvp.Key.Replace(":", "__"), kvp.Value);
                    }
                }
                
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.Sources.Clear(); // Clear all existing configuration sources
                    config.AddInMemoryCollection(configuration);
                    config.AddEnvironmentVariables();
                });
            });

        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        // External authentication providers should be configured when valid credentials are provided
        schemes.Should().Contain(s => s.Name == "Google");
        schemes.Should().Contain(s => s.Name == "Microsoft");
        schemes.Should().Contain(s => s.Name == "Facebook");
    }

    [Fact]
    public async Task Program_ShouldNotConfigureExternalAuth_WhenPlaceholderCredentialsProvided()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Google:ClientId", "YOUR_GOOGLE_CLIENT_ID"},
            {"Authentication:Google:ClientSecret", "YOUR_GOOGLE_CLIENT_SECRET"}
        };

        // Act
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                
                // Set environment variables for authentication configuration
                foreach (var kvp in configuration)
                {
                    if (kvp.Value != null)
                    {
                        Environment.SetEnvironmentVariable(kvp.Key.Replace(":", "__"), kvp.Value);
                    }
                }
                
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.Sources.Clear(); // Clear all existing configuration sources
                    config.AddInMemoryCollection(configuration);
                    config.AddEnvironmentVariables();
                });
            });

        var serviceProvider = _factory.Services;

        // Assert
        var authSchemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await authSchemeProvider.GetAllSchemesAsync();
        
        // Google should not be configured with placeholder credentials
        schemes.Should().NotContain(s => s.Name == "Google");
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }
}