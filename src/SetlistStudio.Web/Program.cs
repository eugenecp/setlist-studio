using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using System.Text;
using Serilog;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Security;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using SetlistStudio.Infrastructure.Configuration;
using SetlistStudio.Web.Services;
using SetlistStudio.Web.Middleware;

using SetlistStudio.Web.Security;
using System.Threading.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

// Configure Serilog with secure logging and data filtering
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SetlistStudio")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
    .Filter.ByExcluding(logEvent =>
    {
        // Filter out potentially sensitive log messages
        var message = logEvent.RenderMessage();
        return SecureLoggingHelper.SensitivePatterns.Any(pattern => pattern.IsMatch(message));
    })
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/setlist-studio-.txt", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
    .WriteTo.File("logs/security/security-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 90,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // SECURITY: Configure Docker secrets for containerized deployments
    var useDockerSecrets = builder.Configuration.GetValue<bool>("USE_DOCKER_SECRETS", false);
    if (useDockerSecrets)
    {
        ConfigureDockerSecrets(builder.Configuration);
        Log.Information("Docker secrets configuration enabled");
    }

    // SECURITY: Configure Azure Key Vault for production secrets management
    ConfigureAzureKeyVault(builder.Configuration, builder.Environment);

    // Add Serilog
    builder.Host.UseSerilog();
    
    // Enable static web assets (for MudBlazor and other package assets)
    builder.WebHost.UseStaticWebAssets();

    // Add services to the container
    builder.Services.AddRazorPages();
    
    // Configure Blazor Server with load balancing support
    ConfigureBlazorServerLoadBalancing(builder.Services, builder.Configuration);
    
    builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization for API controllers
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.WriteIndented = false;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }); // Add API controllers support
    
    // Configure custom input formatters for CSP reports
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
    {
        // Add support for application/csp-report content type
        var jsonInputFormatter = options.InputFormatters
            .OfType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>()
            .FirstOrDefault();
        if (jsonInputFormatter != null)
        {
            jsonInputFormatter.SupportedMediaTypes.Add("application/csp-report");
        }
    });
    
    builder.Services.AddHttpContextAccessor(); // Required for audit logging

    // Add MudBlazor services for Material Design components
    builder.Services.AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomLeft;
        config.SnackbarConfiguration.PreventDuplicates = false;
        config.SnackbarConfiguration.NewestOnTop = false;
        config.SnackbarConfiguration.ShowCloseIcon = true;
        config.SnackbarConfiguration.VisibleStateDuration = 5000;
        config.SnackbarConfiguration.HideTransitionDuration = 500;
        config.SnackbarConfiguration.ShowTransitionDuration = 500;
        config.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
    });

    // SECURITY: Configure CORS with restrictive policy for production security
    ConfigureCors(builder.Services, builder.Environment);

    // Configure database with connection pooling and read replica support
    ConfigureDatabase(builder.Services);

    // Configure Identity
    builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        // SECURITY: Strong password requirements for production security
        options.Password.RequireDigit = true;              // Require at least one digit (0-9)
        options.Password.RequireLowercase = true;          // Require at least one lowercase letter (a-z)
        options.Password.RequireNonAlphanumeric = true;    // Require at least one special character (!@#$%^&* etc.)
        options.Password.RequireUppercase = true;          // Require at least one uppercase letter (A-Z)
        options.Password.RequiredLength = 12;              // Minimum 12 characters for strong security
        options.Password.RequiredUniqueChars = 4;          // Require at least 4 unique characters

        // SECURITY: Account lockout protection against brute force attacks
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);  // Lock for 5 minutes
        options.Lockout.MaxFailedAccessAttempts = 5;                       // Lock after 5 failed attempts
        options.Lockout.AllowedForNewUsers = true;                         // Apply lockout to new users

        // User settings
        options.User.AllowedUserNameCharacters = 
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = false;

        // Sign-in settings
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<SetlistStudioDbContext>();

    // Configure external authentication
    var authBuilder = builder.Services.AddAuthentication();
    ConfigureExternalAuthentication(authBuilder, builder.Configuration);

    // Register health checks (simple readiness/liveness endpoints)
    builder.Services.AddHealthChecks();

    // Configure services and security
    ConfigureServices(builder.Services, builder.Environment, builder.Configuration);

    var app = builder.Build();

    // Configure HTTP pipeline and security middleware
    ConfigureBasicHttpPipeline(app);
    ConfigureSecurityHeaders(app, builder.Configuration);
    ConfigureSecurityMiddleware(app);
    ConfigureCoreMiddlewarePipeline(app);

    // SECURITY: Validate production secrets before starting application
    await ValidateSecretsAsync(app);

    // Initialize database
    await InitializeDatabaseAsync(app);

    // Log application startup information
    LogApplicationStartupInfo(app);
    
    // Add a simple health endpoint for readiness/liveness probes
    try
    {
        // Simple liveness endpoint
        app.MapHealthChecks("/health");

        // Readiness endpoint with JSON details
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description
                    })
                };
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(result));
            }
        });
    }
    catch
    {
        // If health checks mapping fails, continue without readiness endpoint
    }

    app.Run();
}
// CodeQL[cs/catch-of-all-exceptions] - Top-level application exception handler
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Initializes the database and seeds development data if appropriate
/// </summary>
static async Task InitializeDatabaseAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await DatabaseInitializer.InitializeAsync(app.Services, logger);
        
        await SeedDevelopmentDataIfNeededAsync(app, scope.ServiceProvider);
    }
    catch (Exception dbEx)
    {
        HandleDatabaseInitializationError(app.Environment, dbEx);
    }
}

/// <summary>
/// Seeds development data if running in development environment
/// </summary>
static async Task SeedDevelopmentDataIfNeededAsync(WebApplication app, IServiceProvider serviceProvider)
{
    if (app.Environment.IsDevelopment())
    {
        var context = serviceProvider.GetRequiredService<SetlistStudioDbContext>();
        Log.Information("Seeding development data...");
        await SeedDevelopmentDataAsync(context, serviceProvider);
        Log.Information("Development data seeded successfully");
    }
}

/// <summary>
/// Handles database initialization errors based on environment
/// </summary>
static void HandleDatabaseInitializationError(IWebHostEnvironment environment, Exception dbEx)
{
    Log.Error(dbEx, "Failed to initialize database");
    
    // In containers and production, continue without database to allow health checks
    if (environment.IsDevelopment() && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
    {
        throw new InvalidOperationException("Database initialization failed in development environment", dbEx);
    }
    else
    {
        Log.Warning("Continuing without database initialization - app will have limited functionality but will respond to health checks");
    }
}



/// <summary>
/// Configures external authentication providers (Google, Microsoft, Facebook)
/// </summary>
static void ConfigureExternalAuthentication(AuthenticationBuilder authBuilder, IConfiguration configuration)
{
    ConfigureGoogleAuthentication(authBuilder, configuration);
    ConfigureMicrosoftAuthentication(authBuilder, configuration);
    ConfigureFacebookAuthentication(authBuilder, configuration);
}

/// <summary>
/// Configures Google OAuth authentication if credentials are available
/// </summary>
static void ConfigureGoogleAuthentication(AuthenticationBuilder authBuilder, IConfiguration configuration)
{
    var clientId = configuration["Authentication:Google:ClientId"];
    var clientSecret = configuration["Authentication:Google:ClientSecret"];
    

    
    if (IsValidAuthenticationCredentials(clientId, clientSecret))
    {
        try
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = clientId!;
                options.ClientSecret = clientSecret!;
                options.CallbackPath = "/signin-google";
            });
            Log.Information("Google authentication configured successfully");
        }
        // CodeQL[cs/catch-of-all-exceptions] - OAuth provider configuration handling
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to configure Google authentication");
        }
    }
    else
    {

    }
}

/// <summary>
/// Configures Microsoft OAuth authentication if credentials are available
/// </summary>
static void ConfigureMicrosoftAuthentication(AuthenticationBuilder authBuilder, IConfiguration configuration)
{
    var clientId = configuration["Authentication:Microsoft:ClientId"];
    var clientSecret = configuration["Authentication:Microsoft:ClientSecret"];
    

    
    if (IsValidAuthenticationCredentials(clientId, clientSecret))
    {
        try
        {
            authBuilder.AddMicrosoftAccount(options =>
            {
                options.ClientId = clientId!;
                options.ClientSecret = clientSecret!;
                options.CallbackPath = "/signin-microsoft";
            });
            Log.Information("Microsoft authentication configured successfully");
        }
        // CodeQL[cs/catch-of-all-exceptions] - OAuth provider configuration handling
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to configure Microsoft authentication");
        }
    }
    else
    {

    }
}

/// <summary>
/// Configures Facebook OAuth authentication if credentials are available
/// </summary>
static void ConfigureFacebookAuthentication(AuthenticationBuilder authBuilder, IConfiguration configuration)
{
    var appId = configuration["Authentication:Facebook:AppId"];
    var appSecret = configuration["Authentication:Facebook:AppSecret"];
    

    
    if (IsValidAuthenticationCredentials(appId, appSecret))
    {
        try
        {
            authBuilder.AddFacebook(options =>
            {
                options.AppId = appId!;
                options.AppSecret = appSecret!;
                options.CallbackPath = "/signin-facebook";
            });
            Log.Information("Facebook authentication configured successfully");
        }
        // CodeQL[cs/catch-of-all-exceptions] - OAuth provider configuration handling
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to configure Facebook authentication");
        }
    }
    else
    {

    }
}

/// <summary>
/// Validates that authentication credentials are present and not placeholder values
/// </summary>
static bool IsValidAuthenticationCredentials(string? id, string? secret)
{
    return !string.IsNullOrWhiteSpace(id) && 
           !string.IsNullOrWhiteSpace(secret) &&
           !id.StartsWith("YOUR_") &&
           !secret.StartsWith("YOUR_");
}

/// <summary>
/// Seeds the database with sample data for development and testing
/// </summary>
static async Task SeedDevelopmentDataAsync(SetlistStudioDbContext context, IServiceProvider services)
{
    try
    {
        // Only seed if no songs exist
        if (await context.Songs.AnyAsync())
            return;

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var demoUser = await CreateDemoUserAsync(userManager);
        if (demoUser == null) return;

        var sampleSongs = await CreateSampleSongsAsync(context, demoUser.Id);
        var setlists = await CreateSampleSetlistsAsync(context, demoUser.Id);
        await CreateSetlistSongsAsync(context, setlists, sampleSongs);

        Log.Information("Sample data seeded successfully");
    }
    // CodeQL[cs/catch-of-all-exceptions] - Development data seeding error handling
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to seed development data");
    }
}

/// <summary>
/// Creates a demo user for development and testing
/// </summary>
static async Task<ApplicationUser?> CreateDemoUserAsync(UserManager<ApplicationUser> userManager)
{
    var demoUser = new ApplicationUser
    {
        UserName = "demo@setliststudio.com",
        Email = "demo@setliststudio.com",
        DisplayName = "Demo User",
        EmailConfirmed = true,
        Provider = "Demo",
        CreatedAt = DateTime.UtcNow
    };

    var result = await userManager.CreateAsync(demoUser, "DemoUser123456!");
    if (!result.Succeeded)
    {
        var errorsBuilder = new StringBuilder();
        bool first = true;
        foreach (var error in result.Errors)
        {
            if (!first) errorsBuilder.Append(", ");
            errorsBuilder.Append(error.Description);
            first = false;
        }
        Log.Warning("Failed to create demo user: {Errors}", errorsBuilder.ToString());
        return null;
    }

    return demoUser;
}

/// <summary>
/// Creates sample songs with realistic music data
/// </summary>
static async Task<List<Song>> CreateSampleSongsAsync(SetlistStudioDbContext context, string userId)
{
    var sampleSongs = new List<Song>
    {
        new Song { Title = "Bohemian Rhapsody", Artist = "Queen", Album = "A Night at the Opera", Genre = "Rock", Bpm = 72, MusicalKey = "Bb", DurationSeconds = 355, Tags = "epic, opera, classic rock", DifficultyRating = 5, UserId = userId },
        new Song { Title = "Billie Jean", Artist = "Michael Jackson", Album = "Thriller", Genre = "Pop", Bpm = 117, MusicalKey = "F#m", DurationSeconds = 294, Tags = "dance, pop, 80s", DifficultyRating = 3, UserId = userId },
        new Song { Title = "Sweet Child O' Mine", Artist = "Guns N' Roses", Album = "Appetite for Destruction", Genre = "Rock", Bpm = 125, MusicalKey = "D", DurationSeconds = 356, Tags = "guitar solo, rock, 80s", DifficultyRating = 4, UserId = userId },
        new Song { Title = "Take Five", Artist = "Dave Brubeck", Album = "Time Out", Genre = "Jazz", Bpm = 176, MusicalKey = "Bb", DurationSeconds = 324, Tags = "instrumental, jazz, 5/4 time", DifficultyRating = 4, UserId = userId },
        new Song { Title = "The Thrill Is Gone", Artist = "B.B. King", Genre = "Blues", Bpm = 98, MusicalKey = "Bm", DurationSeconds = 311, Tags = "blues, guitar, emotional", DifficultyRating = 3, UserId = userId },
        new Song { Title = "Hotel California", Artist = "Eagles", Album = "Hotel California", Genre = "Rock", Bpm = 75, MusicalKey = "Bm", DurationSeconds = 391, Tags = "classic rock, guitar", DifficultyRating = 4, UserId = userId },
        new Song { Title = "Summertime", Artist = "George Gershwin", Genre = "Jazz", Bpm = 85, MusicalKey = "Am", DurationSeconds = 195, Tags = "jazz standard, ballad", DifficultyRating = 2, UserId = userId },
        new Song { Title = "Uptown Funk", Artist = "Mark Ronson ft. Bruno Mars", Genre = "Funk", Bpm = 115, MusicalKey = "Dm", DurationSeconds = 269, Tags = "funk, dance, modern", DifficultyRating = 3, UserId = userId }
    };

    context.Songs.AddRange(sampleSongs);
    await context.SaveChangesAsync();

    return sampleSongs;
}

/// <summary>
/// Creates sample setlists for development and testing
/// </summary>
static async Task<(Setlist WeddingSetlist, Setlist JazzSetlist)> CreateSampleSetlistsAsync(SetlistStudioDbContext context, string userId)
{
    var weddingSetlist = new Setlist
    {
        Name = "Wedding Reception Set",
        Description = "Perfect mix for wedding celebration",
        Venue = "Grand Ballroom",
        PerformanceDate = DateTime.Now.AddDays(30),
        ExpectedDurationMinutes = 120,
        IsTemplate = false,
        IsActive = true,
        PerformanceNotes = "Keep energy up, take requests for slow dances",
        UserId = userId
    };

    var jazzSetlist = new Setlist
    {
        Name = "Jazz Evening Template",
        Description = "Sophisticated jazz standards for intimate venues",
        IsTemplate = true,
        IsActive = false,
        ExpectedDurationMinutes = 90,
        PerformanceNotes = "Encourage improvisation, adjust tempo based on audience",
        UserId = userId
    };

    context.Setlists.AddRange(weddingSetlist, jazzSetlist);
    await context.SaveChangesAsync();

    return (weddingSetlist, jazzSetlist);
}

/// <summary>
/// Creates setlist songs by linking songs to setlists
/// </summary>
static async Task CreateSetlistSongsAsync(SetlistStudioDbContext context, (Setlist WeddingSetlist, Setlist JazzSetlist) setlists, List<Song> sampleSongs)
{
    var songByTitle = sampleSongs.ToDictionary(s => s.Title);
    
    var weddingSongs = CreateWeddingSetlistSongs(setlists.WeddingSetlist.Id, songByTitle);
    var jazzSongs = CreateJazzSetlistSongs(setlists.JazzSetlist.Id, songByTitle);

    context.SetlistSongs.AddRange(weddingSongs);
    context.SetlistSongs.AddRange(jazzSongs);
    await context.SaveChangesAsync();
}

/// <summary>
/// Creates wedding setlist songs with performance notes
/// </summary>
static List<SetlistSong> CreateWeddingSetlistSongs(int setlistId, Dictionary<string, Song> songByTitle)
{
    return new List<SetlistSong>
    {
        new SetlistSong { SetlistId = setlistId, SongId = GetSongId(songByTitle, "Billie Jean"), Position = 1, PerformanceNotes = "High energy opener" },
        new SetlistSong { SetlistId = setlistId, SongId = GetSongId(songByTitle, "Uptown Funk"), Position = 2, PerformanceNotes = "Get everyone dancing" },
        new SetlistSong { SetlistId = setlistId, SongId = GetSongId(songByTitle, "Hotel California"), Position = 3, PerformanceNotes = "Crowd sing-along" },
        new SetlistSong { SetlistId = setlistId, SongId = GetSongId(songByTitle, "Sweet Child O' Mine"), Position = 4, PerformanceNotes = "Guitar showcase" }
    };
}

/// <summary>
/// Creates jazz setlist songs with performance notes
/// </summary>
static List<SetlistSong> CreateJazzSetlistSongs(int setlistId, Dictionary<string, Song> songByTitle)
{
    return new List<SetlistSong>
    {
        new SetlistSong { SetlistId = setlistId, SongId = GetSongId(songByTitle, "Summertime"), Position = 1, PerformanceNotes = "Gentle opener" },
        new SetlistSong { SetlistId = setlistId, SongId = GetSongId(songByTitle, "Take Five"), Position = 2, PerformanceNotes = "Feature odd time signature" },
        new SetlistSong { SetlistId = setlistId, SongId = GetSongId(songByTitle, "The Thrill Is Gone"), Position = 3, PerformanceNotes = "Blues influence" }
    };
}

/// <summary>
/// Configures application services and security settings
/// </summary>
static void ConfigureServices(IServiceCollection services, IWebHostEnvironment environment, IConfiguration configuration)
{
    ConfigureApplicationServices(services);
    ConfigureSecurityServices(services);
    ConfigureAntiForgeryTokens(services, environment);
    ConfigureCookiesAndSessions(services, environment);
    ConfigureDataProtection(services, environment);
    ConfigureRateLimiting(services, environment);
    ConfigureProductionSecurity(services, environment);
    ConfigureLocalization(services);
}

/// <summary>
/// Configures core application services
/// </summary>
static void ConfigureApplicationServices(IServiceCollection services)
{
    // Register caching services
    services.AddMemoryCache();
    services.AddScoped<IQueryCacheService, QueryCacheService>();
    
    // Register performance monitoring services
    services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();
    
    // Register application services
    services.AddScoped<ISongService, SongService>();
    services.AddScoped<SongFilterService>();
    services.AddScoped<SongRecommendationService>();
    services.AddScoped<ISetlistService, SetlistService>();
    
    // Register CSP nonce service for enhanced Content Security Policy
    services.AddCspNonce();
}

/// <summary>
/// Configures security services and logging
/// </summary>
static void ConfigureSecurityServices(IServiceCollection services)
{
    // Register security services
    services.AddScoped<SecretValidationService>();
    services.AddScoped<IAuditLogService, AuditLogService>();
    
    // Register enhanced authorization services for comprehensive resource-based security
    services.AddScoped<SetlistStudio.Infrastructure.Security.EnhancedAuthorizationService>();
    
    // Register security logging services for centralized security event management
    services.AddScoped<SecurityEventLogger>();
    services.AddScoped<ISecurityEventHandler, SecurityEventHandler>();
    services.AddScoped<SetlistStudio.Web.Security.EnhancedAccountLockoutService>();
    
    // Register security metrics service as singleton for centralized metrics collection
    services.AddSingleton<ISecurityMetricsService, SecurityMetricsService>();
}

/// <summary>
/// Configures anti-forgery tokens for CSRF protection
/// </summary>
static void ConfigureAntiForgeryTokens(IServiceCollection services, IWebHostEnvironment environment)
{
    // Configure Anti-Forgery Tokens - CRITICAL CSRF PROTECTION
    services.AddAntiforgery(options =>
    {
        var isTestEnvironment = IsTestingEnvironment(environment);
        ConfigureAntiForgerySecuritySettings(options, isTestEnvironment);
        ConfigureAntiForgeryHeaders(options);
    });
}

/// <summary>
/// Detects if the application is running in a test environment
/// </summary>
/// <param name="environment">The hosting environment</param>
/// <returns>True if in test environment</returns>
static bool IsTestingEnvironment(IWebHostEnvironment environment)
{
    return IsKnownTestEnvironment(environment) || HasTestingAssemblies();
}

/// <summary>
/// Checks if the environment is explicitly configured for testing
/// </summary>
/// <param name="environment">The hosting environment</param>
/// <returns>True if environment is development or testing</returns>
static bool IsKnownTestEnvironment(IWebHostEnvironment environment)
{
    return environment.IsDevelopment() || environment.EnvironmentName == "Testing";
}

/// <summary>
/// Detects if testing assemblies are loaded (for integration tests)
/// </summary>
/// <returns>True if testing assemblies are found</returns>
static bool HasTestingAssemblies()
{
    return AppDomain.CurrentDomain.GetAssemblies()
        .Any(assembly => IsTestingAssembly(assembly));
}

/// <summary>
/// Checks if an assembly is related to testing frameworks
/// </summary>
/// <param name="assembly">The assembly to check</param>
/// <returns>True if it's a testing assembly</returns>
static bool IsTestingAssembly(System.Reflection.Assembly assembly)
{
    var fullName = assembly.FullName;
    return fullName?.Contains("Microsoft.AspNetCore.Mvc.Testing") == true ||
           fullName?.Contains("xunit") == true;
}

/// <summary>
/// Configures anti-forgery cookie security settings based on environment
/// </summary>
/// <param name="options">Anti-forgery options</param>
/// <param name="isTestEnvironment">Whether running in test environment</param>
static void ConfigureAntiForgerySecuritySettings(Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions options, bool isTestEnvironment)
{
    if (isTestEnvironment)
    {
        options.Cookie.Name = "SetlistStudio-CSRF";
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Allow HTTP in development/testing
    }
    else
    {
        options.Cookie.Name = "__Host-SetlistStudio-CSRF";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Require HTTPS in production
    }
    
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict; // Strict SameSite for CSRF protection
}

/// <summary>
/// Configures anti-forgery header settings
/// </summary>
/// <param name="options">Anti-forgery options</param>
static void ConfigureAntiForgeryHeaders(Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions options)
{
    // Custom header name for AJAX requests
    options.HeaderName = "X-CSRF-TOKEN";
    
    // Suppress xframe options (already handled by security headers middleware)
    options.SuppressXFrameOptionsHeader = true;
}

/// <summary>
/// Configures cookies, sessions, and authentication behavior
/// </summary>
static void ConfigureCookiesAndSessions(IServiceCollection services, IWebHostEnvironment environment)
{
    var isProduction = environment.IsProduction() || environment.IsStaging();
    
    // SECURITY: Configure secure session and cookie settings
    services.ConfigureApplicationCookie(options =>
    {
        // Session timeout and expiration settings
        options.ExpireTimeSpan = TimeSpan.FromHours(2); // 2 hour absolute timeout
        options.SlidingExpiration = true; // Reset timeout on user activity
        
        // Use __Host- prefix in production/staging, regular name in test environments
        options.Cookie.Name = isProduction ? "__Host-SetlistStudio-Identity" : "SetlistStudio-Identity";
        
        ConfigureCookieSecuritySettings(options.Cookie, isProduction);
        ConfigureAuthenticationEventHandlers(options);
        SetAuthenticationPaths(options);
    });

    // Configure session state with secure settings
    services.AddSession(options =>
    {
        options.Cookie.Name = isProduction ? "__Host-SetlistStudio-Session" : "SetlistStudio-Session";
        ConfigureCookieSecuritySettings(options.Cookie, isProduction);
        options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout after inactivity
    });
}

/// <summary>
/// Configures cookie security settings based on environment
/// </summary>
/// <param name="cookieBuilder">Cookie builder to configure</param>
/// <param name="isProduction">Whether in production environment</param>
static void ConfigureCookieSecuritySettings(CookieBuilder cookieBuilder, bool isProduction)
{
    cookieBuilder.HttpOnly = true; // Prevent JavaScript access
    cookieBuilder.SecurePolicy = isProduction 
        ? CookieSecurePolicy.Always // HTTPS only in production
        : CookieSecurePolicy.SameAsRequest; // Allow HTTP in test environments
    cookieBuilder.SameSite = SameSiteMode.Strict; // Strict SameSite for security
    cookieBuilder.IsEssential = true; // Required for GDPR compliance
}

/// <summary>
/// Configures authentication event handlers
/// </summary>
/// <param name="options">Cookie authentication options</param>
static void ConfigureAuthenticationEventHandlers(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions options)
{
    // Session invalidation settings
    options.Events.OnValidatePrincipal = async context =>
    {
        // Validate security stamp to invalidate sessions on password change
        var securityStampValidator = context.HttpContext.RequestServices
            .GetRequiredService<ISecurityStampValidator>();
        await securityStampValidator.ValidateAsync(context);
    };
    
    // SECURITY: Configure proper API authentication behavior
    options.Events.OnRedirectToLogin = context => HandleAuthenticationRedirect(context, StatusCodes.Status401Unauthorized);
    options.Events.OnRedirectToAccessDenied = context => HandleAuthenticationRedirect(context, StatusCodes.Status403Forbidden);
}

/// <summary>
/// Handles authentication redirects for API vs web requests
/// </summary>
/// <param name="context">Redirect context</param>
/// <param name="apiStatusCode">Status code to return for API requests</param>
/// <returns>Completed task</returns>
static Task HandleAuthenticationRedirect<T>(Microsoft.AspNetCore.Authentication.RedirectContext<T> context, int apiStatusCode) where T : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
{
    // For API requests, return status code instead of redirecting
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = apiStatusCode;
        return Task.CompletedTask;
    }
    
    // For regular web requests, redirect
    context.Response.Redirect(context.RedirectUri);
    return Task.CompletedTask;
}

/// <summary>
/// Configures authentication path settings
/// </summary>
/// <param name="options">Cookie authentication options</param>
static void SetAuthenticationPaths(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions options)
{
    // Login/logout paths
    options.LoginPath = "/login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
}

/// <summary>
/// Configures data protection for key management and security
/// </summary>
static void ConfigureDataProtection(IServiceCollection services, IWebHostEnvironment environment)
{
    // Configure data protection with secure settings
    services.AddDataProtection(options =>
    {
        options.ApplicationDiscriminator = "SetlistStudio";
    })
    .SetApplicationName("SetlistStudio")
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.GetFullPath(Path.Join(environment.ContentRootPath, "DataProtection-Keys"))))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Rotate keys every 90 days
}

/// <summary>
/// Configures rate limiting policies for enhanced security
/// </summary>
static void ConfigureRateLimiting(IServiceCollection services, IWebHostEnvironment environment)
{
    // Configure Enhanced Rate Limiting - CRITICAL SECURITY ENHANCEMENT
    services.AddSingleton<IEnhancedRateLimitingService, EnhancedRateLimitingService>();
    
    services.AddRateLimiter(options =>
    {
        // Environment-specific rate limiting configuration
        var rateLimitConfig = Program.GetRateLimitConfiguration(environment);
        
        // Global rate limiter - applies to all endpoints unless overridden
        // Uses enhanced partitioning for better security with environment-specific limits
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var rateLimitingService = httpContext.RequestServices.GetService<IEnhancedRateLimitingService>();
            var partitionKey = rateLimitingService?.GetCompositePartitionKeyAsync(httpContext).Result ?? GetSafePartitionKey(httpContext);
            
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey,
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = rateLimitConfig.GlobalLimit,
                    Window = rateLimitConfig.Window
                });
        });

        ConfigureRateLimitPolicies(options, rateLimitConfig);
        ConfigureRateLimitRejection(options);
    });
}

/// <summary>
/// Configures individual rate limit policies
/// </summary>
static void ConfigureRateLimitPolicies(RateLimiterOptions options, RateLimitConfiguration rateLimitConfig)
{
    // API endpoints - environment-specific limits
    options.AddFixedWindowLimiter("ApiPolicy", options =>
    {
        options.PermitLimit = rateLimitConfig.ApiLimit;
        options.Window = rateLimitConfig.Window;
        options.AutoReplenishment = true;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = rateLimitConfig.ApiQueueLimit;
    });

    // Enhanced API policy for authenticated users
    options.AddFixedWindowLimiter("AuthenticatedApiPolicy", options =>
    {
        options.PermitLimit = rateLimitConfig.AuthenticatedApiLimit;
        options.Window = rateLimitConfig.Window;
        options.AutoReplenishment = true;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = rateLimitConfig.AuthenticatedApiQueueLimit;
    });

    // Authentication endpoints - strict limits to prevent brute force
    options.AddFixedWindowLimiter("AuthPolicy", options =>
    {
        options.PermitLimit = rateLimitConfig.AuthLimit;
        options.Window = rateLimitConfig.Window;
        options.AutoReplenishment = true;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = rateLimitConfig.AuthQueueLimit;
    });

    // Authenticated users - higher limits
    options.AddFixedWindowLimiter("AuthenticatedPolicy", options =>
    {
        options.PermitLimit = rateLimitConfig.AuthenticatedLimit;
        options.Window = rateLimitConfig.Window;
        options.AutoReplenishment = true;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = rateLimitConfig.AuthenticatedQueueLimit;
    });

    // Strict policy for sensitive operations
    options.AddFixedWindowLimiter("StrictPolicy", options =>
    {
        options.PermitLimit = rateLimitConfig.StrictLimit;
        options.Window = rateLimitConfig.Window;
        options.AutoReplenishment = true;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = rateLimitConfig.StrictQueueLimit;
    });

    // Sensitive operations - enhanced security
    options.AddFixedWindowLimiter("SensitivePolicy", options =>
    {
        options.PermitLimit = rateLimitConfig.SensitiveLimit;
        options.Window = rateLimitConfig.Window;
        options.AutoReplenishment = true;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = rateLimitConfig.SensitiveQueueLimit;
    });
}

/// <summary>
/// Configures rate limit rejection handling
/// </summary>
static void ConfigureRateLimitRejection(RateLimiterOptions options)
{
    // Configure rejection response with enhanced logging
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        
        // Enhanced logging with security context
        var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
        var rateLimitingService = context.HttpContext.RequestServices.GetService<IEnhancedRateLimitingService>();
        var partitionKey = rateLimitingService?.GetCompositePartitionKeyAsync(context.HttpContext).Result ?? GetSafePartitionKey(context.HttpContext);
        
        var sanitizedPartitionKey = SecureLoggingHelper.SanitizeMessage(partitionKey);
        var sanitizedClientIp = SecureLoggingHelper.SanitizeIpAddress(GetClientIpAddress(context.HttpContext));
        logger?.LogWarning("Rate limit exceeded for partition: {PartitionKey} on endpoint: {Endpoint} from IP: {ClientIP}", 
            sanitizedPartitionKey, context.HttpContext.Request.Path, sanitizedClientIp);

        // Record the violation for enhanced monitoring
        if (rateLimitingService != null)
        {
            await rateLimitingService.RecordRateLimitViolationAsync(context.HttpContext, partitionKey);
        }

        // Return appropriate response based on request type
        if (IsApiRequest(context.HttpContext))
        {
            context.HttpContext.Response.ContentType = "application/json";
            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "rate_limit_exceeded",
                message = "Rate limit exceeded. Please try again later.",
                retry_after = 60
            });
            await context.HttpContext.Response.WriteAsync(jsonResponse, cancellationToken: token);
        }
        else
        {
            await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken: token);
        }
    };
}

/// <summary>
/// Configures production security settings
/// </summary>
static void ConfigureProductionSecurity(IServiceCollection services, IWebHostEnvironment environment)
{
    // SECURITY: Configure production security settings
    if (!environment.IsDevelopment())
    {
        // Configure forwarded headers for reverse proxy scenarios
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor 
                                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost;
            
            // Clear default known networks and proxies for security
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Configure data protection for production
        services.AddDataProtection();

        // Configure HSTS for production
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });
    }
}

/// <summary>
/// Configures localization settings
/// </summary>
static void ConfigureLocalization(IServiceCollection services)
{
    // Configure localization
    services.AddLocalization(options => options.ResourcesPath = "Resources");
    services.Configure<RequestLocalizationOptions>(options =>
    {
        var supportedCultures = new[] { "en-US", "es-ES", "fr-FR", "de-DE" };
        options.SetDefaultCulture(supportedCultures[0])
               .AddSupportedCultures(supportedCultures)
               .AddSupportedUICultures(supportedCultures);
    });
}

/// <summary>
/// Configures the database provider based on the connection string format.
/// This method is used by unit tests to validate database provider selection logic.
/// </summary>
/// <param name="options">The DbContextOptionsBuilder to configure</param>
/// <param name="connectionString">The connection string to analyze</param>
/// <returns>The configured DbContextOptionsBuilder for method chaining</returns>


/// <summary>
/// Gets a song ID by title, throwing an exception if not found
/// </summary>
static int GetSongId(Dictionary<string, Song> songByTitle, string title)
{
    return songByTitle.TryGetValue(title, out var song) 
        ? song.Id 
        : throw new InvalidOperationException($"Song '{title}' not found in sample data");
}

/// <summary>
/// Validates that all required secrets are properly configured for the current environment
/// </summary>
static Task ValidateSecretsAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var secretValidationService = scope.ServiceProvider.GetRequiredService<SecretValidationService>();
        
        // Validate secrets and throw exception if critical secrets are missing
        secretValidationService.ValidateSecretsOrThrow();
        
        Log.Information("Secret validation completed successfully for environment: {Environment}", 
            app.Environment.EnvironmentName);
            
        return Task.CompletedTask;
    }
    // CodeQL[cs/catch-of-all-exceptions] - Application startup secret validation
    catch (Exception ex)
    {
        Log.Fatal(ex, "Secret validation failed for environment: {Environment}", app.Environment.EnvironmentName);
        throw;
    }
}

/// <summary>
/// Gets a safe partition key for rate limiting, avoiding null reference issues
/// </summary>
/// <param name="httpContext">The HTTP context</param>
/// <returns>A safe partition key string</returns>
static string GetSafePartitionKey(HttpContext httpContext)
{
    var authenticatedUser = GetAuthenticatedUser(httpContext);
    return authenticatedUser ?? GetClientIpAddress(httpContext);
}

/// <summary>
/// Gets the authenticated user name if available
/// </summary>
/// <param name="httpContext">The HTTP context</param>
/// <returns>User name if authenticated, null otherwise</returns>
static string? GetAuthenticatedUser(HttpContext httpContext)
{
    var userIdentity = GetUserIdentity(httpContext);
    return IsValidAuthenticatedUser(userIdentity) ? userIdentity?.Name : null;
}

/// <summary>
/// Extracts user identity from HTTP context
/// </summary>
/// <param name="httpContext">The HTTP context</param>
/// <returns>User identity if available, null otherwise</returns>
static System.Security.Principal.IIdentity? GetUserIdentity(HttpContext httpContext)
{
    return httpContext.User?.Identity;
}

/// <summary>
/// Validates if user identity represents a valid authenticated user
/// </summary>
/// <param name="userIdentity">The user identity to validate</param>
/// <returns>True if user is authenticated with a valid name</returns>
static bool IsValidAuthenticatedUser(System.Security.Principal.IIdentity? userIdentity)
{
    return userIdentity?.IsAuthenticated == true && !string.IsNullOrEmpty(userIdentity.Name);
}

static string GetClientIpAddress(HttpContext httpContext)
{
    // Try forwarded headers first (for load balancers/proxies)
    var forwardedIp = GetForwardedIpAddress(httpContext);
    if (forwardedIp != null) return forwardedIp;

    // Try X-Real-IP header
    var realIp = GetRealIpAddress(httpContext);
    if (realIp != null) return realIp;

    // Fallback to connection remote IP
    return GetConnectionIpAddress(httpContext);
}

/// <summary>
/// Extracts IP address from X-Forwarded-For header
/// </summary>
/// <param name="httpContext">The HTTP context</param>
/// <returns>IP address or null if not found</returns>
static string? GetForwardedIpAddress(HttpContext httpContext)
{
    var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (string.IsNullOrEmpty(forwardedFor)) return null;

    return forwardedFor.Split(',').FirstOrDefault()?.Trim();
}

/// <summary>
/// Extracts IP address from X-Real-IP header
/// </summary>
/// <param name="httpContext">The HTTP context</param>
/// <returns>IP address or null if not found</returns>
static string? GetRealIpAddress(HttpContext httpContext)
{
    var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
    return string.IsNullOrEmpty(realIp) ? null : realIp.Trim();
}

/// <summary>
/// Gets IP address from direct connection
/// </summary>
/// <param name="httpContext">The HTTP context</param>
/// <returns>IP address or "Unknown" if not available</returns>
static string GetConnectionIpAddress(HttpContext httpContext)
{
    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
}



static bool IsApiRequest(HttpContext httpContext)
{
    return IsApiPath(httpContext) || 
           IsJsonRequest(httpContext) || 
           IsAjaxRequest(httpContext);
}

/// <summary>
/// Checks if the request path indicates an API endpoint
/// </summary>
/// <param name="httpContext">The HTTP context</param>
/// <returns>True if path starts with /api/</returns>
static bool IsApiPath(HttpContext httpContext)
{
    var path = httpContext.Request.Path.Value?.ToLowerInvariant();
    return path?.StartsWith("/api/") == true;
}

/// <summary>
/// Checks if the request accepts JSON content
/// </summary>
/// <param name="httpContext">The HTTP context</param>
/// <returns>True if Accept header contains application/json</returns>
static bool IsJsonRequest(HttpContext httpContext)
{
    var acceptHeader = httpContext.Request.Headers.Accept.ToString();
    return acceptHeader.Contains("application/json");
}

/// <summary>
/// Checks if the request is an AJAX request
/// </summary>
/// <param name="httpContext">The HTTP context</param>
/// <returns>True if X-Requested-With header is present</returns>
static bool IsAjaxRequest(HttpContext httpContext)
{
    return httpContext.Request.Headers.ContainsKey("X-Requested-With");
}

/// <summary>
/// Configures OAuth secrets from Docker secrets files when running in containerized environment
/// </summary>
/// <param name="configuration">The configuration builder to add secrets to</param>
static void ConfigureDockerSecrets(IConfiguration configuration)
{
    const string secretsPath = "/run/secrets";
    
    if (!Directory.Exists(secretsPath))
    {
        Log.Warning("Docker secrets directory not found: {SecretsPath}", secretsPath);
        return;
    }

    var secretMappings = GetSecretMappings();
    var secretValues = LoadSecretValues(secretsPath, secretMappings);

    ApplySecretsToConfiguration(configuration, secretValues);
}

/// <summary>
/// Gets the mapping of Docker secret files to configuration keys
/// </summary>
/// <returns>Dictionary mapping secret file names to configuration keys</returns>
static Dictionary<string, string> GetSecretMappings()
{
    return new Dictionary<string, string>
    {
        { "setliststudio_google_client_id", "Authentication:Google:ClientId" },
        { "setliststudio_google_client_secret", "Authentication:Google:ClientSecret" },
        { "setliststudio_microsoft_client_id", "Authentication:Microsoft:ClientId" },
        { "setliststudio_microsoft_client_secret", "Authentication:Microsoft:ClientSecret" },
        { "setliststudio_facebook_app_id", "Authentication:Facebook:AppId" },
        { "setliststudio_facebook_app_secret", "Authentication:Facebook:AppSecret" }
    };
}

/// <summary>
/// Loads secret values from Docker secret files
/// </summary>
/// <param name="secretsPath">Path to the secrets directory</param>
/// <param name="secretMappings">Mapping of secret files to configuration keys</param>
/// <returns>Dictionary of configuration keys and their values</returns>
static Dictionary<string, string?> LoadSecretValues(string secretsPath, Dictionary<string, string> secretMappings)
{
    var secretValues = new Dictionary<string, string?>();

    foreach (var (secretFile, configKey) in secretMappings)
    {
        var secretValue = ReadSecretFile(secretsPath, secretFile);
        if (secretValue != null)
        {
            secretValues[configKey] = secretValue;
            Log.Information("Loaded Docker secret: {ConfigKey}", configKey);
        }
    }

    return secretValues;
}

/// <summary>
/// Reads a single secret file and validates its content
/// </summary>
/// <param name="secretsPath">Path to the secrets directory</param>
/// <param name="secretFile">Name of the secret file</param>
/// <returns>Secret value if valid, null otherwise</returns>
static string? ReadSecretFile(string secretsPath, string secretFile)
{
    var secretFilePath = GetSecretFilePath(secretsPath, secretFile);
    
    if (!ValidateSecretFileExists(secretFilePath, secretFile))
    {
        return null;
    }

    return ReadAndValidateSecretContent(secretFilePath, secretFile);
}

/// <summary>
/// Constructs the full path to a secret file
/// </summary>
/// <param name="secretsPath">Base secrets directory path</param>
/// <param name="secretFile">Secret file name</param>
/// <returns>Complete path to secret file</returns>
static string GetSecretFilePath(string secretsPath, string secretFile)
{
    return Path.Combine(secretsPath, secretFile);
}

/// <summary>
/// Validates that a secret file exists and logs appropriate message
/// </summary>
/// <param name="secretFilePath">Path to the secret file</param>
/// <param name="secretFile">Secret file name for logging</param>
/// <returns>True if file exists, false otherwise</returns>
static bool ValidateSecretFileExists(string secretFilePath, string secretFile)
{
    if (!File.Exists(secretFilePath))
    {
        Log.Debug("Docker secret file not found: {SecretFile}", secretFile);
        return false;
    }
    return true;
}

/// <summary>
/// Reads secret file content and validates it's not a placeholder
/// </summary>
/// <param name="secretFilePath">Path to the secret file</param>
/// <param name="secretFile">Secret file name for logging</param>
/// <returns>Valid secret value or null if invalid</returns>
static string? ReadAndValidateSecretContent(string secretFilePath, string secretFile)
{
    try
    {
        var secretValue = File.ReadAllText(secretFilePath).Trim();
        
        if (IsPlaceholderSecretValue(secretValue))
        {
            Log.Warning("Docker secret contains placeholder value: {SecretFile}", secretFile);
            return null;
        }

        return secretValue;
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to read Docker secret: {SecretFile}", secretFile);
        return null;
    }
}

/// <summary>
/// Checks if a secret value is a placeholder that should be ignored
/// </summary>
/// <param name="secretValue">The secret value to validate</param>
/// <returns>True if the value is a placeholder</returns>
static bool IsPlaceholderSecretValue(string secretValue)
{
    return string.IsNullOrEmpty(secretValue) || secretValue.StartsWith("PLACEHOLDER_");
}

/// <summary>
/// Applies loaded secrets to the configuration
/// </summary>
/// <param name="configuration">Configuration to update</param>
/// <param name="secretValues">Secret values to apply</param>
static void ApplySecretsToConfiguration(IConfiguration configuration, Dictionary<string, string?> secretValues)
{
    if (secretValues.Count > 0)
    {
        ((IConfigurationBuilder)configuration).AddInMemoryCollection(secretValues);
        Log.Information("Added {Count} Docker secrets to configuration", secretValues.Count);
    }
}

/// <summary>
/// Rate limiting configuration for different environments
/// </summary>
public record RateLimitConfiguration
{
    public int GlobalLimit { get; init; }
    public int ApiLimit { get; init; }
    public int AuthenticatedApiLimit { get; init; }
    public int AuthLimit { get; init; }
    public int AuthenticatedLimit { get; init; }
    public int StrictLimit { get; init; }
    public int SensitiveLimit { get; init; }
    public int ApiQueueLimit { get; init; }
    public int AuthenticatedApiQueueLimit { get; init; }
    public int AuthQueueLimit { get; init; }
    public int AuthenticatedQueueLimit { get; init; }
    public int StrictQueueLimit { get; init; }
    public int SensitiveQueueLimit { get; init; }
    public TimeSpan Window { get; init; }
}

// Make Program class accessible for testing
public partial class Program 
{
    /// <summary>
    /// Gets environment-specific rate limiting configuration
    /// </summary>
    /// <param name="environment">The hosting environment</param>
    /// <returns>Rate limiting configuration for the environment</returns>
    public static RateLimitConfiguration GetRateLimitConfiguration(IWebHostEnvironment environment)
    {
        return environment.EnvironmentName switch
        {
            "Production" => new RateLimitConfiguration
            {
                GlobalLimit = 1000,
                ApiLimit = 100,
                AuthenticatedApiLimit = 200,
                AuthLimit = 5,
                AuthenticatedLimit = 2000,
                StrictLimit = 10,
                SensitiveLimit = 25,
                ApiQueueLimit = 10,
                AuthenticatedApiQueueLimit = 20,
                AuthQueueLimit = 2,
                AuthenticatedQueueLimit = 50,
                StrictQueueLimit = 3,
                SensitiveQueueLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            },
            "Staging" => new RateLimitConfiguration
            {
                GlobalLimit = 2000,
                ApiLimit = 200,
                AuthenticatedApiLimit = 400,
                AuthLimit = 10,
                AuthenticatedLimit = 4000,
                StrictLimit = 20,
                SensitiveLimit = 50,
                ApiQueueLimit = 20,
                AuthenticatedApiQueueLimit = 40,
                AuthQueueLimit = 5,
                AuthenticatedQueueLimit = 100,
                StrictQueueLimit = 6,
                SensitiveQueueLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            },
            "Development" => new RateLimitConfiguration
            {
                GlobalLimit = 10000,
                ApiLimit = 1000,
                AuthenticatedApiLimit = 2000,
                AuthLimit = 50,
                AuthenticatedLimit = 10000,
                StrictLimit = 100,
                SensitiveLimit = 250,
                ApiQueueLimit = 100,
                AuthenticatedApiQueueLimit = 200,
                AuthQueueLimit = 20,
                AuthenticatedQueueLimit = 500,
                StrictQueueLimit = 30,
                SensitiveQueueLimit = 50,
                Window = TimeSpan.FromMinutes(1)
            },
            _ => new RateLimitConfiguration // Default for Test and other environments
            {
                GlobalLimit = 50000,
                ApiLimit = 5000,
                AuthenticatedApiLimit = 10000,
                AuthLimit = 500,
                AuthenticatedLimit = 50000,
                StrictLimit = 1000,
                SensitiveLimit = 2500,
                ApiQueueLimit = 500,
                AuthenticatedApiQueueLimit = 1000,
                AuthQueueLimit = 100,
                AuthenticatedQueueLimit = 2500,
                StrictQueueLimit = 150,
                SensitiveQueueLimit = 250,
                Window = TimeSpan.FromMinutes(1)
            }
        };
    }

    /// <summary>
    /// Determines if rate limiting should be enabled for the given environment
    /// </summary>
    /// <param name="environment">The hosting environment</param>
    /// <returns>True if rate limiting should be enabled</returns>
    public static bool ShouldEnableRateLimiting(IWebHostEnvironment environment)
    {
        // Enable rate limiting in all environments except strict test environments
        return !environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase) &&
               !environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Configures Azure Key Vault for production secrets management
    /// </summary>
    /// <param name="configuration">The configuration builder</param>
    /// <param name="environment">The hosting environment</param>
    public static void ConfigureAzureKeyVault(IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment()) return;

        var keyVaultName = GetKeyVaultName(configuration, environment);
        if (keyVaultName == null) return;

        ValidateKeyVaultName(keyVaultName);
        SetupKeyVaultClient(configuration, keyVaultName);
    }

    /// <summary>
    /// Retrieves and validates Key Vault name from configuration
    /// </summary>
    /// <param name="configuration">The configuration</param>
    /// <param name="environment">The hosting environment</param>
    /// <returns>Key Vault name or null if not configured</returns>
    private static string? GetKeyVaultName(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var keyVaultName = configuration["KeyVault:VaultName"];
        if (string.IsNullOrEmpty(keyVaultName))
        {
            if (!environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("Azure Key Vault name not configured for non-development environment");
            }
            return null;
        }
        return keyVaultName;
    }

    /// <summary>
    /// Validates Key Vault name format
    /// </summary>
    /// <param name="keyVaultName">The Key Vault name to validate</param>
    private static void ValidateKeyVaultName(string keyVaultName)
    {
        if (!IsValidKeyVaultName(keyVaultName))
        {
            Log.Warning("Invalid Azure Key Vault name format: {KeyVaultName}", keyVaultName);
            throw new InvalidOperationException("Invalid Azure Key Vault name format");
        }
    }

    /// <summary>
    /// Sets up the Key Vault client and adds to configuration
    /// </summary>
    /// <param name="configuration">The configuration builder</param>
    /// <param name="keyVaultName">The Key Vault name</param>
    private static void SetupKeyVaultClient(IConfiguration configuration, string keyVaultName)
    {
        try
        {
            var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
            var secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());
            ((IConfigurationBuilder)configuration).AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
            Log.Information("Azure Key Vault configured: {KeyVaultUri}", keyVaultUri);
        }
        // CodeQL[cs/catch-of-all-exceptions] - Application startup configuration handling
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to configure Azure Key Vault: {KeyVaultName}", keyVaultName);
        }
    }

    /// <summary>
    /// Validates Azure Key Vault name format according to Azure naming conventions
    /// </summary>
    /// <param name="keyVaultName">The Key Vault name to validate</param>
    /// <returns>True if the name is valid</returns>
    private static bool IsValidKeyVaultName(string keyVaultName)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(keyVaultName, @"^[a-zA-Z0-9][a-zA-Z0-9-]{1,22}[a-zA-Z0-9]$");
    }

    /// <summary>
    /// Configures CORS policies with environment-specific security settings
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="environment">The hosting environment</param>
    public static void ConfigureCors(IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("RestrictivePolicy", policy => ConfigureRestrictivePolicy(policy, environment));
            options.AddPolicy("ApiPolicy", policy => ConfigureApiPolicy(policy, environment));
        });
    }

    /// <summary>
    /// Configures the restrictive CORS policy based on environment
    /// </summary>
    /// <param name="policy">The CORS policy builder</param>
    /// <param name="environment">The hosting environment</param>
    private static void ConfigureRestrictivePolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy, IWebHostEnvironment environment)
    {
        var environmentName = environment.EnvironmentName;
        
        if (environmentName == "Production")
        {
            ConfigureProductionPolicy(policy);
        }
        else if (environmentName == "Development")
        {
            ConfigureDevelopmentPolicy(policy);
        }
        else
        {
            ConfigureStagingPolicy(policy);
        }
    }

    /// <summary>
    /// Configures the API-specific CORS policy based on environment
    /// </summary>
    /// <param name="policy">The CORS policy builder</param>
    /// <param name="environment">The hosting environment</param>
    private static void ConfigureApiPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy, IWebHostEnvironment environment)
    {
        var environmentName = environment.EnvironmentName;
        
        if (environmentName == "Production")
        {
            ConfigureProductionApiPolicy(policy);
        }
        else if (environmentName == "Development")
        {
            ConfigureDevelopmentApiPolicy(policy);
        }
        else
        {
            ConfigureTestingApiPolicy(policy);
        }
    }

    /// <summary>
    /// Configures production CORS policy with trusted domains only
    /// </summary>
    /// <param name="policy">The CORS policy builder</param>
    private static void ConfigureProductionPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy)
    {
        policy.WithOrigins(
            "https://setliststudio.com",
            "https://www.setliststudio.com",
            "https://api.setliststudio.com"
        )
        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
        .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-CSRF-TOKEN")
        .AllowCredentials()
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    }

    /// <summary>
    /// Configures development CORS policy with relaxed restrictions
    /// </summary>
    /// <param name="policy">The CORS policy builder</param>
    private static void ConfigureDevelopmentPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy)
    {
        policy.AllowAnyOrigin()
        .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
        .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-CSRF-TOKEN", "Origin", "Accept")
        .SetPreflightMaxAge(TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Configures staging/test CORS policy with moderate restrictions
    /// </summary>
    /// <param name="policy">The CORS policy builder</param>
    private static void ConfigureStagingPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy)
    {
        policy.WithOrigins(
            "https://staging.setliststudio.com",
            "https://test.setliststudio.com",
            "https://preview.setliststudio.com"
        )
        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
        .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-CSRF-TOKEN")
        .AllowCredentials()
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    }

    /// <summary>
    /// Configures production API CORS policy with restricted access
    /// </summary>
    /// <param name="policy">The CORS policy builder</param>
    private static void ConfigureProductionApiPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy)
    {
        policy.WithOrigins(
            "https://setliststudio.com",
            "https://www.setliststudio.com",
            "https://api.setliststudio.com"
        )
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Content-Type", "Authorization")
        .AllowCredentials()
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    }

    /// <summary>
    /// Configures development API CORS policy with relaxed restrictions
    /// </summary>
    /// <param name="policy">The CORS policy builder</param>
    private static void ConfigureDevelopmentApiPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy)
    {
        policy.AllowAnyOrigin()
        .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
        .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-CSRF-TOKEN", "Origin", "Accept")
        .SetPreflightMaxAge(TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Configures testing API CORS policy with localhost access
    /// </summary>
    /// <param name="policy">The CORS policy builder</param>
    private static void ConfigureTestingApiPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy)
    {
        policy.WithOrigins(
            "https://localhost:5001",
            "http://localhost:5000",
            "https://localhost:7000",
            "http://localhost:8000"
        )
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Content-Type", "Authorization")
        .AllowCredentials()
        .SetPreflightMaxAge(TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Configures database contexts and connection pooling with read replica support
    /// </summary>
    /// <param name="services">The service collection</param>
    public static void ConfigureDatabase(IServiceCollection services)
    {
        RegisterDatabaseServices(services);
        ConfigureWriteDbContext(services);
        ConfigureReadDbContext(services);
    }

    private static void RegisterDatabaseServices(IServiceCollection services)
    {
        // Register database configuration services
        services.AddSingleton<IDatabaseConfiguration>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var logger = provider.GetRequiredService<ILogger<DatabaseConfiguration>>();
            return new DatabaseConfiguration(config, logger);
        });

        services.AddSingleton<DatabaseProviderService>(provider =>
        {
            var config = provider.GetRequiredService<IDatabaseConfiguration>();
            var logger = provider.GetRequiredService<ILogger<DatabaseProviderService>>();
            return new DatabaseProviderService(config, logger);
        });
    }

    private static void ConfigureWriteDbContext(IServiceCollection services)
    {
        // Configure write context (primary database)
        services.AddDbContext<SetlistStudioDbContext>((serviceProvider, options) =>
        {
            var databaseConfig = serviceProvider.GetRequiredService<IDatabaseConfiguration>();
            ConfigureDatabaseProvider(options, databaseConfig, true);
        });
    }

    private static void ConfigureReadDbContext(IServiceCollection services)
    {
        // Configure read-only context (read replicas)
        services.AddDbContext<ReadOnlySetlistStudioDbContext>((serviceProvider, options) =>
        {
            var databaseConfig = serviceProvider.GetRequiredService<IDatabaseConfiguration>();
            var providerService = serviceProvider.GetRequiredService<DatabaseProviderService>();
            
            if (databaseConfig.Provider == DatabaseProvider.SQLite || databaseConfig.Provider == DatabaseProvider.InMemory)
            {
                // For SQLite/InMemory, use same connection as write (no read replicas)
                ConfigureDatabaseProvider(options, databaseConfig, false);
            }
            else
            {
                // For PostgreSQL/SQL Server, use read replica configuration
                providerService.ConfigureReadContext(options);
            }
        });
    }

    private static void ConfigureDatabaseProvider(DbContextOptionsBuilder options, IDatabaseConfiguration databaseConfig, bool isWriteContext)
    {
        // Handle different providers appropriately
        if (databaseConfig.Provider == DatabaseProvider.SQLite || databaseConfig.Provider == DatabaseProvider.InMemory)
        {
            ConfigureSqliteProvider(options, databaseConfig);
        }
        else if (isWriteContext)
        {
            ConfigureServerDatabaseProvider(options, databaseConfig);
        }
    }

    private static void ConfigureSqliteProvider(DbContextOptionsBuilder options, IDatabaseConfiguration databaseConfig)
    {
        // For SQLite/InMemory, use traditional configuration since Web project has these packages
        var connectionString = databaseConfig.WriteConnectionString;
        if (connectionString.Contains("Data Source=") && !connectionString.Contains("Server="))
        {
            options.UseSqlite(connectionString);
        }
        else
        {
            options.UseInMemoryDatabase("InMemoryTestDatabase");
        }
    }

    private static void ConfigureServerDatabaseProvider(DbContextOptionsBuilder options, IDatabaseConfiguration databaseConfig)
    {
        // For PostgreSQL/SQL Server, configure with migrations assembly
        if (databaseConfig.Provider == DatabaseProvider.PostgreSQL)
        {
            options.UseNpgsql(databaseConfig.WriteConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(databaseConfig.CommandTimeout);
                npgsqlOptions.MigrationsAssembly("SetlistStudio.Web");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        }
        else if (databaseConfig.Provider == DatabaseProvider.SqlServer)
        {
            options.UseSqlServer(databaseConfig.WriteConnectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(databaseConfig.CommandTimeout);
                sqlOptions.MigrationsAssembly("SetlistStudio.Web");
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });
        }
    }

    /// <summary>
    /// Configures basic HTTP pipeline settings for error handling and HTTPS
    /// </summary>
    /// <param name="app">The web application instance</param>
    static void ConfigureBasicHttpPipeline(WebApplication app)
    {
        // SECURITY: Configure forwarded headers for production reverse proxy scenarios
        if (!app.Environment.IsDevelopment())
        {
            app.UseForwardedHeaders();
        }

        // Configure the HTTP request pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        // Only use HTTPS redirection in Production or Staging environments
        // (disable in Development and Testing so localhost runs HTTP-only)
        if (app.Environment.IsProduction() || app.Environment.IsStaging())
        {
            app.UseHttpsRedirection();
        }
    }

    /// <summary>
    /// Configures security headers middleware for comprehensive security protection
    /// </summary>
    /// <param name="app">The web application instance</param>
    /// <param name="configuration">The application configuration</param>
    static void ConfigureSecurityHeaders(WebApplication app, IConfiguration configuration)
    {
        // Security Headers Middleware - CRITICAL SECURITY ENHANCEMENT
        app.Use(async (context, next) =>
        {
            ApplyBasicSecurityHeaders(context);
            ApplyCspHeaders(context, configuration);
            ApplyPermissionsPolicyHeaders(context);
            ApplyHstsHeaders(context);
            
            await next();
        });
    }

    /// <summary>
    /// Applies basic security headers to prevent common attacks
    /// </summary>
    /// <param name="context">The HTTP context</param>
    static void ApplyBasicSecurityHeaders(HttpContext context)
    {
        // Prevent MIME type sniffing attacks
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        
        // Prevent clickjacking attacks
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        
        // Enable XSS protection (legacy browsers)
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        
        // Referrer policy for privacy protection
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    }

    /// <summary>
    /// Applies Content Security Policy headers with nonce support
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="configuration">The application configuration</param>
    static void ApplyCspHeaders(HttpContext context, IConfiguration configuration)
    {
        // Content Security Policy - enhanced with nonce-based security
        var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var cspReportingEnabled = configuration.GetValue<bool>("Security:CspReporting:Enabled", true);
        var cspPolicyBuilder = new StringBuilder();
        cspPolicyBuilder.Append("default-src 'self'; ");
        
        // Get nonces from the current request context
        var scriptNonce = context.Items["ScriptNonce"]?.ToString();
        var styleNonce = context.Items["StyleNonce"]?.ToString();
        
        AppendScriptCspPolicy(cspPolicyBuilder, scriptNonce, environment);
        AppendStyleCspPolicy(cspPolicyBuilder, styleNonce, environment);
        AppendResourceCspPolicies(cspPolicyBuilder, environment);
        AppendCspReporting(cspPolicyBuilder, cspReportingEnabled);
        
        var cspPolicy = cspPolicyBuilder.ToString();
        context.Response.Headers.Append("Content-Security-Policy", cspPolicy);
    }

    /// <summary>
    /// Appends script-src CSP policy with nonce fallback
    /// </summary>
    /// <param name="cspPolicyBuilder">The CSP policy builder</param>
    /// <param name="scriptNonce">The script nonce if available</param>
    /// <param name="environment">The web host environment</param>
    static void AppendScriptCspPolicy(StringBuilder cspPolicyBuilder, string? scriptNonce, IWebHostEnvironment environment)
    {
        // Enhanced script-src with nonce fallback
        if (!string.IsNullOrEmpty(scriptNonce))
        {
            // Allow 'unsafe-eval' in development for Blazor debugging
            if (environment.IsDevelopment())
            {
                cspPolicyBuilder.Append($"script-src 'self' 'nonce-{scriptNonce}' 'unsafe-eval'; ");
            }
            else
            {
                cspPolicyBuilder.Append($"script-src 'self' 'nonce-{scriptNonce}'; ");
            }
        }
        else
        {
            // Fallback for development/testing or when nonces aren't available
            if (environment.IsDevelopment())
            {
                cspPolicyBuilder.Append("script-src 'self' 'unsafe-inline' 'unsafe-eval'; ");
            }
            else
            {
                cspPolicyBuilder.Append("script-src 'self' 'unsafe-inline'; ");
            }
        }
    }

    /// <summary>
    /// Appends style-src CSP policy with nonce fallback
    /// </summary>
    /// <param name="cspPolicyBuilder">The CSP policy builder</param>
    /// <param name="styleNonce">The style nonce if available</param>
    /// <param name="environment">The web host environment</param>
    static void AppendStyleCspPolicy(StringBuilder cspPolicyBuilder, string? styleNonce, IWebHostEnvironment environment)
    {
        // Enhanced style-src with nonce fallback
        if (!string.IsNullOrEmpty(styleNonce))
        {
            // Allow Google Fonts in development
            if (environment.IsDevelopment())
            {
                cspPolicyBuilder.Append($"style-src 'self' 'nonce-{styleNonce}' https://fonts.googleapis.com; ");
            }
            else
            {
                cspPolicyBuilder.Append($"style-src 'self' 'nonce-{styleNonce}'; ");
            }
        }
        else
        {
            // Fallback for development/testing or when nonces aren't available
            if (environment.IsDevelopment())
            {
                cspPolicyBuilder.Append("style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; ");
            }
            else
            {
                cspPolicyBuilder.Append("style-src 'self' 'unsafe-inline'; ");
            }
        }
    }

    /// <summary>
    /// Appends resource-related CSP policies for images, fonts, connections, etc.
    /// </summary>
    /// <param name="cspPolicyBuilder">The CSP policy builder</param>
    /// <param name="environment">The web host environment</param>
    static void AppendResourceCspPolicies(StringBuilder cspPolicyBuilder, IWebHostEnvironment environment)
    {
        cspPolicyBuilder.Append("img-src 'self' data: https:; ");
        
        // Allow Google Fonts in development
        if (environment.IsDevelopment())
        {
            cspPolicyBuilder.Append("font-src 'self' https://fonts.gstatic.com; ");
            cspPolicyBuilder.Append("connect-src 'self' wss: ws:; ");
        }
        else
        {
            cspPolicyBuilder.Append("font-src 'self'; ");
            cspPolicyBuilder.Append("connect-src 'self'; ");
        }
        
        cspPolicyBuilder.Append("frame-ancestors 'none'; ");
        cspPolicyBuilder.Append("base-uri 'self'; ");
        cspPolicyBuilder.Append("form-action 'self'");
    }

    /// <summary>
    /// Appends CSP violation reporting if enabled
    /// </summary>
    /// <param name="cspPolicyBuilder">The CSP policy builder</param>
    /// <param name="cspReportingEnabled">Whether CSP reporting is enabled</param>
    static void AppendCspReporting(StringBuilder cspPolicyBuilder, bool cspReportingEnabled)
    {
        // Add CSP violation reporting if enabled
        if (cspReportingEnabled)
        {
            cspPolicyBuilder.Append("; report-uri /api/cspreport/report");
        }
    }

    /// <summary>
    /// Applies Permissions Policy headers to disable unnecessary browser features
    /// </summary>
    /// <param name="context">The HTTP context</param>
    static void ApplyPermissionsPolicyHeaders(HttpContext context)
    {
        // Permissions Policy - disable unnecessary browser features
        context.Response.Headers.Append("Permissions-Policy", 
            "camera=(), microphone=(), geolocation=(), payment=(), usb=()");
    }

    /// <summary>
    /// Applies HSTS headers for production HTTPS environments
    /// </summary>
    /// <param name="context">The HTTP context</param>
    static void ApplyHstsHeaders(HttpContext context)
    {
        // HSTS - only in production and when using HTTPS
        if (!context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() && 
            context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security", 
                "max-age=31536000; includeSubDomains; preload");
        }
    }

    /// <summary>
    /// Configures security-specific middleware for protection and monitoring
    /// </summary>
    /// <param name="app">The web application instance</param>
    static void ConfigureSecurityMiddleware(WebApplication app)
    {
        // CSP Nonce Middleware - Generate nonces for enhanced Content Security Policy
        app.UseCspNonce();
        
        ConfigureRateLimitingMiddleware(app);
        ConfigureAttackPreventionMiddleware(app);
        ConfigureSecurityMonitoringMiddleware(app);
    }

    /// <summary>
    /// Configures rate limiting middleware with environment-specific settings
    /// </summary>
    /// <param name="app">The web application instance</param>
    static void ConfigureRateLimitingMiddleware(WebApplication app)
    {
        // Rate Limiting Headers Middleware - Environment-specific configuration
        var enableRateLimiting = Program.ShouldEnableRateLimiting(app.Environment);
        if (enableRateLimiting)
        {
            app.UseRateLimitHeaders();
            
            // Rate Limiting Middleware - CRITICAL SECURITY ENHANCEMENT with environment-specific limits
            app.UseRateLimiter();
            
            var rateLimitConfig = Program.GetRateLimitConfiguration(app.Environment);
            Log.Information("Rate limiting enabled with {Environment} configuration: Global={GlobalLimit}, API={ApiLimit}, Auth={AuthLimit}", 
                app.Environment.EnvironmentName, rateLimitConfig.GlobalLimit, rateLimitConfig.ApiLimit, rateLimitConfig.AuthLimit);
        }
        else
        {
            Log.Information("Rate limiting disabled for {Environment} environment", app.Environment.EnvironmentName);
        }
    }

    /// <summary>
    /// Configures middleware to prevent automated attacks and rate limit bypass
    /// </summary>
    /// <param name="app">The web application instance</param>
    static void ConfigureAttackPreventionMiddleware(WebApplication app)
    {
        // CAPTCHA Middleware - Prevent automated attacks and rate limit bypass
        if (!app.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase) &&
            !app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            app.UseCaptchaMiddleware();
        }
    }

    /// <summary>
    /// Configures security event monitoring middleware for threat detection
    /// </summary>
    /// <param name="app">The web application instance</param>
    static void ConfigureSecurityMonitoringMiddleware(WebApplication app)
    {
        // Security Event Logging Middleware - Monitor and log security events (disabled in test environment)
        if (!app.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase) &&
            !app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            app.UseMiddleware<SetlistStudio.Web.Middleware.SecurityEventMiddleware>();
        }
    }

    /// <summary>
    /// Configures the core middleware pipeline for routing, authentication, and authorization
    /// </summary>
    /// <param name="app">The web application instance</param>
    static void ConfigureCoreMiddlewarePipeline(WebApplication app)
    {
        app.UseStaticFiles();
        app.UseRouting();

        // SECURITY: Enable CORS with restrictive policy
        app.UseCors("RestrictivePolicy");

        // SECURITY: Enable secure session management
        app.UseSession();

        // Add localization
        app.UseRequestLocalization();

        app.UseAuthentication();
        app.UseAuthorization();

        // Map API controllers before fallback routing
        app.MapControllers();
        
        app.MapRazorPages();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");
    }

    /// <summary>
    /// Logs application startup information
    /// </summary>
    /// <param name="app">The web application instance</param>
    static void LogApplicationStartupInfo(WebApplication app)
    {
        Log.Information("Setlist Studio application starting");
        Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
        Log.Information("URLs: {Urls}", Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
        Log.Information("Container: {IsContainer}", Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));
    }

    /// <summary>
    /// Configures Blazor Server with load balancing support using Redis for session storage
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    static void ConfigureBlazorServerLoadBalancing(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");
        var isLoadBalanced = configuration.GetValue<bool>("LoadBalancing__IsLoadBalanced", false);
        
        Log.Information("Configuring Blazor Server - Load Balanced: {IsLoadBalanced}, Redis: {HasRedis}", 
            isLoadBalanced, !string.IsNullOrEmpty(redisConnection));

        if (isLoadBalanced && !string.IsNullOrEmpty(redisConnection))
        {
            // Configure Redis for distributed session storage
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "SetlistStudio";
            });

            // Configure session with Redis backing
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.Name = ".SetlistStudio.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            // Configure SignalR with Redis backplane for load balancing
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = false;
                options.MaximumReceiveMessageSize = 32 * 1024; // 32KB max message size
                options.StreamBufferCapacity = 10;
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.HandshakeTimeout = TimeSpan.FromSeconds(15);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            })
            .AddStackExchangeRedis(redisConnection, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("SetlistStudio");
            });

            // Configure Blazor Server with sticky sessions
            services.AddServerSideBlazor(options =>
            {
                options.DetailedErrors = false;
                options.DisconnectedCircuitMaxRetained = 100;
                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
                options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
                options.MaxBufferedUnacknowledgedRenderBatches = 10;
            });

            Log.Information("Blazor Server configured with Redis backplane for load balancing");
        }
        else
        {
            // Standard single-instance configuration
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = false;
                options.MaximumReceiveMessageSize = 32 * 1024;
                options.StreamBufferCapacity = 10;
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.HandshakeTimeout = TimeSpan.FromSeconds(15);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

            services.AddServerSideBlazor(options =>
            {
                options.DetailedErrors = false;
                options.DisconnectedCircuitMaxRetained = 100;
                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
                options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
                options.MaxBufferedUnacknowledgedRenderBatches = 10;
            });

            // Use in-memory session for single instance
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.Name = ".SetlistStudio.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            Log.Information("Blazor Server configured for single instance operation");
        }
    }
}