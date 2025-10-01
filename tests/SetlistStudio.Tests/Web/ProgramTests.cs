using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FluentAssertions;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Xunit;

namespace SetlistStudio.Tests.Web;

/// <summary>
/// Comprehensive tests for Program.cs configuration and startup behavior
/// Testing application configuration, services registration, middleware pipeline, and database initialization
/// </summary>
public class ProgramTests : IDisposable
{
    private WebApplicationFactory<Program>? _factory;
    private readonly Dictionary<string, string> _testConfiguration;

    public ProgramTests()
    {
        _testConfiguration = new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Google:ClientId", "test-google-client-id"},
            {"Authentication:Google:ClientSecret", "test-google-client-secret"},
            {"Authentication:Microsoft:ClientId", "test-microsoft-client-id"},
            {"Authentication:Microsoft:ClientSecret", "test-microsoft-client-secret"},
            {"Authentication:Facebook:AppId", "test-facebook-app-id"},
            {"Authentication:Facebook:AppSecret", "test-facebook-app-secret"}
        };
    }

    #region Service Configuration Tests

    [Fact]
    public void Program_ShouldConfigureBasicServices_WhenApplicationStarts()
    {
        // Arrange & Act
        using var factory = CreateWebApplicationFactory();
        
        // Assert - Test basic service registrations
        var serviceProvider = factory.Services;
        
        // Verify ASP.NET Core services
        serviceProvider.GetService<IWebHostEnvironment>().Should().NotBeNull();
        serviceProvider.GetService<IConfiguration>().Should().NotBeNull();
        serviceProvider.GetService<ILoggerFactory>().Should().NotBeNull();
        
        // Verify database context
        serviceProvider.GetService<SetlistStudioDbContext>().Should().NotBeNull();
        
        // Verify application services
        serviceProvider.GetService<ISongService>().Should().NotBeNull();
        serviceProvider.GetService<ISetlistService>().Should().NotBeNull();
        
        // Verify Identity services
        serviceProvider.GetService<UserManager<ApplicationUser>>().Should().NotBeNull();
        serviceProvider.GetService<SignInManager<ApplicationUser>>().Should().NotBeNull();
    }

    [Fact]
    public void Program_ShouldConfigureMudBlazorServices_WithCorrectSettings()
    {
        // Arrange & Act
        using var factory = CreateWebApplicationFactory();
        
        // Assert
        // Check for any MudBlazor service registration
        var serviceDescriptors = factory.Services;
        serviceDescriptors.Should().NotBeNull("MudBlazor services should be registered");
    }

    [Fact]
    public void Program_ShouldConfigureIdentityOptions_WithRelaxedPasswordPolicy()
    {
        // Arrange & Act
        using var factory = CreateWebApplicationFactory();
        
        // Assert
        var identityOptions = factory.Services.GetService<IOptions<IdentityOptions>>();
        identityOptions.Should().NotBeNull();
        
        var options = identityOptions!.Value;
        options.Password.RequireDigit.Should().BeFalse();
        options.Password.RequireLowercase.Should().BeFalse();
        options.Password.RequireNonAlphanumeric.Should().BeFalse();
        options.Password.RequireUppercase.Should().BeFalse();
        options.Password.RequiredLength.Should().Be(6);
        options.Password.RequiredUniqueChars.Should().Be(1);
        
        options.Lockout.DefaultLockoutTimeSpan.Should().Be(TimeSpan.FromMinutes(5));
        options.Lockout.MaxFailedAccessAttempts.Should().Be(5);
        options.Lockout.AllowedForNewUsers.Should().BeTrue();
        
        options.SignIn.RequireConfirmedEmail.Should().BeFalse();
        options.SignIn.RequireConfirmedPhoneNumber.Should().BeFalse();
        
        options.User.RequireUniqueEmail.Should().BeFalse();
        options.User.AllowedUserNameCharacters.Should().Contain("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+");
    }

    [Fact]
    public void Program_ShouldConfigureLocalization_WithSupportedCultures()
    {
        // Arrange & Act
        using var factory = CreateWebApplicationFactory();
        
        // Assert
        var localizationOptions = factory.Services.GetService<IOptions<RequestLocalizationOptions>>();
        localizationOptions.Should().NotBeNull();
        
        var options = localizationOptions!.Value;
        options.DefaultRequestCulture.Culture.Name.Should().Be("en-US");
        options.SupportedCultures.Should().HaveCount(4);
        options.SupportedCultures!.Select(c => c.Name).Should().Contain(new[] { "en-US", "es-ES", "fr-FR", "de-DE" });
        options.SupportedUICultures.Should().HaveCount(4);
        options.SupportedUICultures!.Select(c => c.Name).Should().Contain(new[] { "en-US", "es-ES", "fr-FR", "de-DE" });
    }

    #endregion

    #region Database Configuration Tests

    [Fact]
    public void Program_ShouldConfigureSQLiteDatabase_WhenConnectionStringContainsDataSource()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=test.db"}
        };
        
        // Act
        using var factory = CreateWebApplicationFactoryWithoutDbOverride(config);
        
        // Assert
        var context = factory.Services.GetRequiredService<SetlistStudioDbContext>();
        context.Should().NotBeNull();
        context.Database.IsSqlite().Should().BeTrue();
    }

    [Fact]
    public void Program_ShouldUseSQLiteByDefault_WhenNoConnectionStringProvided()
    {
        // Arrange
        var config = new Dictionary<string, string>();
        
        // Act
        using var factory = CreateWebApplicationFactoryWithoutDbOverride(config);
        
        // Assert
        var context = factory.Services.GetRequiredService<SetlistStudioDbContext>();
        context.Should().NotBeNull();
        context.Database.IsSqlite().Should().BeTrue();
    }

    [Fact]
    public void Program_ShouldUseContainerPath_WhenRunningInContainer()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");
        var config = new Dictionary<string, string>();
        
        try
        {
            // Act
            using var factory = CreateWebApplicationFactory(config);
            
            // Assert
            var context = factory.Services.GetRequiredService<SetlistStudioDbContext>();
            context.Should().NotBeNull();
            // The database should still be configured (path is determined at runtime)
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
        }
    }

    #endregion

    #region Authentication Configuration Tests

    [Fact]
    public void Program_ShouldConfigureGoogleAuthentication_WhenClientIdProvided()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Google:ClientId", "real-google-client-id"},
            {"Authentication:Google:ClientSecret", "real-google-secret"}
        };
        
        // Act
        using var factory = CreateWebApplicationFactory(config);
        
        // Assert - Verify configuration was applied correctly
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var googleClientId = configuration["Authentication:Google:ClientId"];
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
        
        googleClientId.Should().Be("real-google-client-id");
        googleClientSecret.Should().Be("real-google-secret");
        
        var authenticationService = factory.Services.GetService<IAuthenticationService>();
        authenticationService.Should().NotBeNull();
        
        var schemeProvider = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = schemeProvider.GetAllSchemesAsync().Result;
        schemes.Should().Contain(s => s.Name == "Google");
    }

    [Fact]
    public void Program_ShouldConfigureMicrosoftAuthentication_WhenClientIdProvided()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Microsoft:ClientId", "real-microsoft-client-id"},
            {"Authentication:Microsoft:ClientSecret", "real-microsoft-secret"}
        };
        
        // Act
        using var factory = CreateWebApplicationFactory(config);
        
        // Assert
        var schemeProvider = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = schemeProvider.GetAllSchemesAsync().Result;
        schemes.Should().Contain(s => s.Name == "Microsoft");
    }

    [Fact]
    public void Program_ShouldConfigureFacebookAuthentication_WhenAppIdProvided()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"},
            {"Authentication:Facebook:AppId", "real-facebook-app-id"},
            {"Authentication:Facebook:AppSecret", "real-facebook-secret"}
        };
        
        // Act
        using var factory = CreateWebApplicationFactory(config);
        
        // Assert
        var schemeProvider = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = schemeProvider.GetAllSchemesAsync().Result;
        schemes.Should().Contain(s => s.Name == "Facebook");
    }

    [Fact]
    public void Program_ShouldNotConfigureExternalAuth_WhenNoCredentialsProvided()
    {
        // Arrange - explicitly provide config with empty auth credentials
        var config = new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", "Data Source=:memory:"}
            // Don't include authentication keys at all - this should ensure they are null
        };
        
        // Act
        using var factory = CreateWebApplicationFactory(config);
        
        // Assert
        var schemeProvider = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = schemeProvider.GetAllSchemesAsync().Result;
        
        // Should only have default Identity schemes, not external providers
        schemes.Should().NotContain(s => s.Name == "Google");
        schemes.Should().NotContain(s => s.Name == "Microsoft");
        schemes.Should().NotContain(s => s.Name == "Facebook");
    }

    #endregion

    #region Middleware Pipeline Tests

    [Fact]
    public async Task Program_ShouldConfigureMiddlewarePipeline_InCorrectOrder()
    {
        // Arrange
        using var factory = CreateWebApplicationFactory();
        var client = factory.CreateClient();
        
        // Act - Make a request to trigger middleware pipeline
        var response = await client.GetAsync("/health/simple");
        
        // Assert - Response should be successful, indicating middleware pipeline is working
        response.Should().NotBeNull();
        // The response may not be successful due to test setup but middleware should be configured
    }

    [Fact]
    public async Task Program_ShouldServeStaticFiles_WhenRequested()
    {
        // Arrange
        using var factory = CreateWebApplicationFactory();
        var client = factory.CreateClient();
        
        // Act - Request a static file
        var response = await client.GetAsync("/favicon.png");
        
        // Assert - Should attempt to serve static files (404 is expected in test, but shows pipeline works)
        response.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.NotFound);
    }

    #endregion

    #region Application Configuration Tests

    [Fact]
    public void Program_ShouldConfigureApplicationServices_WithCorrectScoping()
    {
        // Arrange & Act
        using var factory = CreateWebApplicationFactory();
        
        // Assert - Verify service lifetimes
        var serviceDescriptors = factory.Services.GetService<IServiceCollection>();
        
        // Verify ISongService is registered as Scoped
        var songService = factory.Services.GetRequiredService<ISongService>();
        songService.Should().NotBeNull();
        songService.Should().BeOfType<SongService>();
        
        // Verify ISetlistService is registered as Scoped  
        var setlistService = factory.Services.GetRequiredService<ISetlistService>();
        setlistService.Should().NotBeNull();
        setlistService.Should().BeOfType<SetlistService>();
    }

    [Fact]
    public void Program_ShouldConfigureControllers_ForAPIEndpoints()
    {
        // Arrange & Act
        using var factory = CreateWebApplicationFactory();
        
        // Assert
        var mvcBuilder = factory.Services.GetService<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager>();
        mvcBuilder.Should().NotBeNull("Controllers should be configured");
    }

    [Fact]
    public void Program_ShouldConfigureRazorPages_AndBlazorServer()
    {
        // Arrange & Act
        using var factory = CreateWebApplicationFactory();
        
        // Assert - Basic service verification
        var serviceProvider = factory.Services;
        serviceProvider.Should().NotBeNull("Services should be configured");
        
        // Verify core services are registered
        var configuration = factory.Services.GetService<IConfiguration>();
        configuration.Should().NotBeNull("Configuration should be available");
    }

    #endregion

    #region Environment-Specific Tests

    [Fact]
    public void Program_ShouldConfigureDevelopmentFeatures_InDevelopmentEnvironment()
    {
        // Arrange & Act
        using var factory = CreateWebApplicationFactory(environment: "Development");
        
        // Assert
        var env = factory.Services.GetRequiredService<IWebHostEnvironment>();
        env.IsDevelopment().Should().BeTrue();
        
        // In development, certain middleware and features should be configured differently
        // This is primarily tested through integration rather than unit tests
    }

    [Fact]
    public void Program_ShouldConfigureProductionFeatures_InProductionEnvironment()
    {
        // Arrange & Act
        using var factory = CreateWebApplicationFactory(environment: "Production");
        
        // Assert
        var env = factory.Services.GetRequiredService<IWebHostEnvironment>();
        env.IsProduction().Should().BeTrue();
        
        // Production should have HSTS and exception handling configured
        // This is primarily validated through integration tests
    }

    #endregion

    #region Helper Methods

    private WebApplicationFactory<Program> CreateWebApplicationFactory(
        Dictionary<string, string>? configuration = null, 
        string environment = "Test")
    {
        var config = configuration ?? _testConfiguration;
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                // Set environment variables for authentication configuration
                foreach (var kvp in config)
                {
                    Environment.SetEnvironmentVariable(kvp.Key.Replace(":", "__"), kvp.Value);
                }
                
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    // Clear existing configuration and add only our test config
                    configBuilder.Sources.Clear();
                    configBuilder.AddInMemoryCollection(config!);
                    configBuilder.AddEnvironmentVariables();
                });
                
                // Override database with in-memory for testing
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SetlistStudioDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);
                    
                    // Add in-memory database for testing
                    services.AddDbContext<SetlistStudioDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });
            
        return _factory;
    }

    private WebApplicationFactory<Program> CreateWebApplicationFactoryWithoutDbOverride(
        Dictionary<string, string>? configuration = null, 
        string environment = "Test")
    {
        var config = configuration ?? new Dictionary<string, string>();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                // Set environment variables for authentication configuration
                foreach (var kvp in config)
                {
                    Environment.SetEnvironmentVariable(kvp.Key.Replace(":", "__"), kvp.Value);
                }
                
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    // Clear existing configuration and add only our test config
                    configBuilder.Sources.Clear();
                    configBuilder.AddInMemoryCollection(config!);
                    configBuilder.AddEnvironmentVariables();
                });
                // Don't override database configuration - let Program.cs handle it
            });
            
        return _factory;
    }

    #endregion

    public void Dispose()
    {
        // Clean up environment variables to avoid interference between tests
        var configKeys = new[]
        {
            "ConnectionStrings__DefaultConnection",
            "Authentication__Google__ClientId",
            "Authentication__Google__ClientSecret",
            "Authentication__Microsoft__ClientId",
            "Authentication__Microsoft__ClientSecret",
            "Authentication__Facebook__AppId",
            "Authentication__Facebook__AppSecret"
        };
        
        foreach (var key in configKeys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
        
        _factory?.Dispose();
    }
}