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
using Serilog;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Security;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using SetlistStudio.Web.Services;
using System.Threading.RateLimiting;

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

    // SECURITY: Configure Azure Key Vault for production secrets management
    if (!builder.Environment.IsDevelopment())
    {
        var keyVaultName = builder.Configuration["KeyVault:VaultName"];
        if (!string.IsNullOrEmpty(keyVaultName))
        {
            try
            {
                var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
                var secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());
                builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                Log.Information("Azure Key Vault configured: {KeyVaultUri}", keyVaultUri);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to configure Azure Key Vault: {KeyVaultName}", keyVaultName);
            }
        }
        else
        {
            Log.Warning("Azure Key Vault name not configured for non-development environment");
        }
    }

    // Add Serilog
    builder.Host.UseSerilog();
    
    // Enable static web assets (for MudBlazor and other package assets)
    builder.WebHost.UseStaticWebAssets();

    // Add services to the container
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddControllers(); // Add API controllers support
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
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("RestrictivePolicy", policy =>
        {
            var environment = builder.Environment.EnvironmentName;
            
            if (environment == "Production")
            {
                // Production: Only allow specific trusted domains
                policy.WithOrigins(
                    "https://setliststudio.com",
                    "https://www.setliststudio.com"
                )
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-CSRF-TOKEN")
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            }
            else if (environment == "Development")
            {
                // Development: Allow localhost with specific ports
                policy.WithOrigins(
                    "https://localhost:5001",
                    "http://localhost:5000",
                    "https://localhost:7000",
                    "http://localhost:8000"
                )
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-CSRF-TOKEN")
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(5));
            }
            else
            {
                // Test/Staging: More restrictive than development but not production
                policy.WithOrigins(
                    "https://staging.setliststudio.com",
                    "https://test.setliststudio.com"
                )
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-CSRF-TOKEN")
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            }
        });

        // Add a more restrictive policy for API endpoints
        options.AddPolicy("ApiPolicy", policy =>
        {
            var environment = builder.Environment.EnvironmentName;
            
            if (environment == "Production")
            {
                policy.WithOrigins(
                    "https://setliststudio.com",
                    "https://www.setliststudio.com"
                )
                .WithMethods("GET", "POST", "PUT", "DELETE")
                .WithHeaders("Content-Type", "Authorization")
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            }
            else
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
        });
    });

    // Configure database
    builder.Services.AddDbContext<SetlistStudioDbContext>(options =>
    {
        var connectionString = GetDatabaseConnectionString(builder.Configuration);
        ConfigureDatabaseProvider(options, connectionString);
    });

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

    // Register application services
    builder.Services.AddScoped<ISongService, SongService>();
    builder.Services.AddScoped<ISetlistService, SetlistService>();
    
    // Register security services
    builder.Services.AddScoped<SecretValidationService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    
    // Register enhanced authorization services for comprehensive resource-based security
    builder.Services.AddScoped<SetlistStudio.Infrastructure.Security.EnhancedAuthorizationService>();
    
    // Register security logging services for centralized security event management
    builder.Services.AddScoped<SecurityEventLogger>();
    builder.Services.AddScoped<SetlistStudio.Web.Security.SecurityEventHandler>();
    builder.Services.AddScoped<SetlistStudio.Web.Security.EnhancedAccountLockoutService>();

    // Configure Anti-Forgery Tokens - CRITICAL CSRF PROTECTION
    builder.Services.AddAntiforgery(options =>
    {
        // Use secure cookie names with __Host- prefix for enhanced security in production
        if (builder.Environment.IsDevelopment())
        {
            options.Cookie.Name = "SetlistStudio-CSRF";
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Allow HTTP in development
        }
        else
        {
            options.Cookie.Name = "__Host-SetlistStudio-CSRF";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Require HTTPS in production
        }
        
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict; // Strict SameSite for CSRF protection
        
        // Custom header name for AJAX requests
        options.HeaderName = "X-CSRF-TOKEN";
        
        // Suppress xframe options (already handled by security headers middleware)
        options.SuppressXFrameOptionsHeader = true;
    });

    // SECURITY: Configure secure session and cookie settings
    builder.Services.ConfigureApplicationCookie(options =>
    {
        // Session timeout and expiration settings
        options.ExpireTimeSpan = TimeSpan.FromHours(2); // 2 hour absolute timeout
        options.SlidingExpiration = true; // Reset timeout on user activity
        options.Cookie.Name = "__Host-SetlistStudio-Identity";
        
        // Secure cookie configuration
        options.Cookie.HttpOnly = true; // Prevent JavaScript access
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
        options.Cookie.SameSite = SameSiteMode.Strict; // Strict SameSite for security
        options.Cookie.IsEssential = true; // Required for GDPR compliance
        
        // Session invalidation settings
        options.Events.OnValidatePrincipal = async context =>
        {
            // Validate security stamp to invalidate sessions on password change
            var securityStampValidator = context.HttpContext.RequestServices
                .GetRequiredService<ISecurityStampValidator>();
            await securityStampValidator.ValidateAsync(context);
        };
        
        // Login/logout paths
        options.LoginPath = "/Identity/Account/Login";
        options.LogoutPath = "/Identity/Account/Logout";
        options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    });

    // Configure session state with secure settings
    builder.Services.AddSession(options =>
    {
        options.Cookie.Name = "__Host-SetlistStudio-Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.IsEssential = true;
        options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout after inactivity
    });

    // Configure data protection with secure settings
    builder.Services.AddDataProtection(options =>
    {
        options.ApplicationDiscriminator = "SetlistStudio";
    })
    .SetApplicationName("SetlistStudio")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(
        builder.Environment.ContentRootPath, "DataProtection-Keys")))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Rotate keys every 90 days

    // Configure Rate Limiting - CRITICAL SECURITY ENHANCEMENT
    builder.Services.AddRateLimiter(options =>
    {
        // Global rate limiter - applies to all endpoints unless overridden
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 1000, // 1000 requests per window
                    Window = TimeSpan.FromMinutes(1)
                }));

        // API endpoints - 100 requests per minute
        options.AddFixedWindowLimiter("ApiPolicy", options =>
        {
            options.PermitLimit = 100;
            options.Window = TimeSpan.FromMinutes(1);
            options.AutoReplenishment = true;
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 10; // Allow 10 requests to queue
        });

        // Authentication endpoints - 5 attempts per minute (prevent brute force)
        options.AddFixedWindowLimiter("AuthPolicy", options =>
        {
            options.PermitLimit = 5;
            options.Window = TimeSpan.FromMinutes(1);
            options.AutoReplenishment = true;
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 2; // Very limited queue for auth attempts
        });

        // Strict policy for sensitive operations - 10 requests per minute
        options.AddFixedWindowLimiter("StrictPolicy", options =>
        {
            options.PermitLimit = 10;
            options.Window = TimeSpan.FromMinutes(1);
            options.AutoReplenishment = true;
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 3;
        });

        // Configure rejection response
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            
            // Log rate limit violation for security monitoring
            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
            var userIdentifier = context.HttpContext.User.Identity?.Name ?? 
                               context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            
            logger?.LogWarning("Rate limit exceeded for user/IP: {UserIdentifier} on endpoint: {Endpoint}", 
                userIdentifier, context.HttpContext.Request.Path);

            await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken: token);
        };
    });

    // SECURITY: Configure production security settings
    if (!builder.Environment.IsDevelopment())
    {
        // Configure forwarded headers for reverse proxy scenarios
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor 
                                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost;
            
            // Clear default known networks and proxies for security
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Configure data protection for production
        builder.Services.AddDataProtection();

        // Configure HSTS for production
        builder.Services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });
    }

    // Configure localization
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        var supportedCultures = new[] { "en-US", "es-ES", "fr-FR", "de-DE" };
        options.SetDefaultCulture(supportedCultures[0])
               .AddSupportedCultures(supportedCultures)
               .AddSupportedUICultures(supportedCultures);
    });

    var app = builder.Build();

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

    app.UseHttpsRedirection();
    
    // Security Headers Middleware - CRITICAL SECURITY ENHANCEMENT
    app.Use(async (context, next) =>
    {
        // Prevent MIME type sniffing attacks
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        
        // Prevent clickjacking attacks
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        
        // Enable XSS protection (legacy browsers)
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        
        // Referrer policy for privacy protection
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Content Security Policy - restrictive default
        var cspPolicy = "default-src 'self'; " +
                       "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +  // Blazor requires unsafe-inline/eval
                       "style-src 'self' 'unsafe-inline'; " +                  // MudBlazor requires unsafe-inline
                       "img-src 'self' data: https:; " +
                       "font-src 'self'; " +
                       "connect-src 'self'; " +
                       "frame-ancestors 'none'; " +
                       "base-uri 'self'; " +
                       "form-action 'self'";
        context.Response.Headers.Append("Content-Security-Policy", cspPolicy);
        
        // Permissions Policy - disable unnecessary browser features
        context.Response.Headers.Append("Permissions-Policy", 
            "camera=(), microphone=(), geolocation=(), payment=(), usb=()");
        
        // HSTS - only in production and when using HTTPS
        if (!context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() && 
            context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security", 
                "max-age=31536000; includeSubDomains; preload");
        }
        
        await next();
    });
    
    // Rate Limiting Middleware - CRITICAL SECURITY ENHANCEMENT
    app.UseRateLimiter();
    
    // Security Event Logging Middleware - Monitor and log security events (disabled in test environment)
    if (!app.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase))
    {
        app.UseMiddleware<SetlistStudio.Web.Middleware.SecurityEventMiddleware>();
    }
    
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

    // SECURITY: Validate production secrets before starting application
    await ValidateSecretsAsync(app);

    // Initialize database
    await InitializeDatabaseAsync(app);

    Log.Information("Setlist Studio application starting");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("URLs: {Urls}", Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
    Log.Information("Container: {IsContainer}", Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));
    
    app.Run();
}
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
/// Gets the database connection string, using environment-specific defaults if none configured
/// </summary>
static string GetDatabaseConnectionString(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        // Use in-memory database for test environments to avoid file locking
        if (string.Equals(environment, "Test", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = "Data Source=:memory:";
        }
        else
        {
            connectionString = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
                ? "Data Source=/app/data/setliststudio.db"
                : "Data Source=setliststudio.db";
        }
    }
    
    return connectionString;
}

/// <summary>
/// Configures the database provider based on the connection string format
/// </summary>
static void ConfigureDatabaseProvider(DbContextOptionsBuilder options, string connectionString)
{
    if (connectionString.Contains("Data Source=") && !connectionString.Contains("Server="))
    {
        // SQLite connection string
        options.UseSqlite(connectionString);
    }
    else
    {
        // SQL Server connection string
        options.UseSqlServer(connectionString);
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

    var result = await userManager.CreateAsync(demoUser, "Demo123!");
    if (!result.Succeeded)
    {
        Log.Warning("Failed to create demo user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
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
static async Task ValidateSecretsAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var secretValidationService = scope.ServiceProvider.GetRequiredService<SecretValidationService>();
        
        // Validate secrets and throw exception if critical secrets are missing
        secretValidationService.ValidateSecretsOrThrow();
        
        Log.Information("Secret validation completed successfully for environment: {Environment}", 
            app.Environment.EnvironmentName);
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Secret validation failed for environment: {Environment}", app.Environment.EnvironmentName);
        throw;
    }
}

// Make Program class accessible for testing
public partial class Program { }