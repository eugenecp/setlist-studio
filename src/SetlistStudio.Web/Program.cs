using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Serilog;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using SetlistStudio.Web.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/setlist-studio-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();
    
    // Enable static web assets (for MudBlazor and other package assets)
    builder.WebHost.UseStaticWebAssets();

    // Add services to the container
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddControllers(); // Add API controllers support

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

    // Configure database
    builder.Services.AddDbContext<SetlistStudioDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        
        // Use container-specific path only when running in Docker
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
                ? "Data Source=/app/data/setliststudio.db"
                : "Data Source=setliststudio.db";
        }
        
        // Auto-detect database provider based on connection string
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
    });

    // Configure Identity
    builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        // Password settings (relaxed for demo purposes)
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

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

    // Google OAuth
    if (!string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientId"]))
    {
        authBuilder.AddGoogle(googleOptions =>
        {
            googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
            googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
            googleOptions.CallbackPath = "/signin-google";
        });
    }

    // Microsoft OAuth
    if (!string.IsNullOrEmpty(builder.Configuration["Authentication:Microsoft:ClientId"]))
    {
        authBuilder.AddMicrosoftAccount(microsoftOptions =>
        {
            microsoftOptions.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]!;
            microsoftOptions.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
            microsoftOptions.CallbackPath = "/signin-microsoft";
        });
    }

    // Facebook OAuth
    if (!string.IsNullOrEmpty(builder.Configuration["Authentication:Facebook:AppId"]))
    {
        authBuilder.AddFacebook(facebookOptions =>
        {
            facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"]!;
            facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
            facebookOptions.CallbackPath = "/signin-facebook";
        });
    }

    // Register application services
    builder.Services.AddScoped<ISongService, SongService>();
    builder.Services.AddScoped<ISetlistService, SetlistService>();

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

    // Configure the HTTP request pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    // Add localization
    app.UseRequestLocalization();

    app.UseAuthentication();
    app.UseAuthorization();

    // Map API controllers before fallback routing
    app.MapControllers();
    
    app.MapRazorPages();
    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    // Initialize database
    try
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await DatabaseInitializer.InitializeAsync(app.Services, logger);
        
        // Seed sample data in development
        if (app.Environment.IsDevelopment())
        {
            var context = scope.ServiceProvider.GetRequiredService<SetlistStudioDbContext>();
            Log.Information("Seeding development data...");
            await SeedDevelopmentDataAsync(context, scope.ServiceProvider);
            Log.Information("Development data seeded successfully");
        }
    }
    catch (Exception dbEx)
    {
        Log.Error(dbEx, "Failed to initialize database");
        
        // In containers and production, continue without database to allow health checks
        if (app.Environment.IsDevelopment() && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
        {
            throw; // Re-throw in local development for debugging
        }
        else
        {
            Log.Warning("Continuing without database initialization - app will have limited functionality but will respond to health checks");
        }
    }

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
        
        // Create a demo user
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
            return;
        }

        // Add sample songs with realistic music data
        var sampleSongs = new List<Song>
        {
            new Song { Title = "Bohemian Rhapsody", Artist = "Queen", Album = "A Night at the Opera", Genre = "Rock", Bpm = 72, MusicalKey = "Bb", DurationSeconds = 355, Tags = "epic, opera, classic rock", DifficultyRating = 5, UserId = demoUser.Id },
            new Song { Title = "Billie Jean", Artist = "Michael Jackson", Album = "Thriller", Genre = "Pop", Bpm = 117, MusicalKey = "F#m", DurationSeconds = 294, Tags = "dance, pop, 80s", DifficultyRating = 3, UserId = demoUser.Id },
            new Song { Title = "Sweet Child O' Mine", Artist = "Guns N' Roses", Album = "Appetite for Destruction", Genre = "Rock", Bpm = 125, MusicalKey = "D", DurationSeconds = 356, Tags = "guitar solo, rock, 80s", DifficultyRating = 4, UserId = demoUser.Id },
            new Song { Title = "Take Five", Artist = "Dave Brubeck", Album = "Time Out", Genre = "Jazz", Bpm = 176, MusicalKey = "Bb", DurationSeconds = 324, Tags = "instrumental, jazz, 5/4 time", DifficultyRating = 4, UserId = demoUser.Id },
            new Song { Title = "The Thrill Is Gone", Artist = "B.B. King", Genre = "Blues", Bpm = 98, MusicalKey = "Bm", DurationSeconds = 311, Tags = "blues, guitar, emotional", DifficultyRating = 3, UserId = demoUser.Id },
            new Song { Title = "Hotel California", Artist = "Eagles", Album = "Hotel California", Genre = "Rock", Bpm = 75, MusicalKey = "Bm", DurationSeconds = 391, Tags = "classic rock, guitar", DifficultyRating = 4, UserId = demoUser.Id },
            new Song { Title = "Summertime", Artist = "George Gershwin", Genre = "Jazz", Bpm = 85, MusicalKey = "Am", DurationSeconds = 195, Tags = "jazz standard, ballad", DifficultyRating = 2, UserId = demoUser.Id },
            new Song { Title = "Uptown Funk", Artist = "Mark Ronson ft. Bruno Mars", Genre = "Funk", Bpm = 115, MusicalKey = "Dm", DurationSeconds = 269, Tags = "funk, dance, modern", DifficultyRating = 3, UserId = demoUser.Id }
        };

        context.Songs.AddRange(sampleSongs);
        await context.SaveChangesAsync();

        // Create sample setlists
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
            UserId = demoUser.Id
        };

        var jazzSetlist = new Setlist
        {
            Name = "Jazz Evening Template",
            Description = "Sophisticated jazz standards for intimate venues",
            IsTemplate = true,
            IsActive = false,
            ExpectedDurationMinutes = 90,
            PerformanceNotes = "Encourage improvisation, adjust tempo based on audience",
            UserId = demoUser.Id
        };

        context.Setlists.AddRange(weddingSetlist, jazzSetlist);
        await context.SaveChangesAsync();

        // Add songs to setlists
        var songByTitle = sampleSongs.ToDictionary(s => s.Title);
        var weddingSongs = new List<SetlistSong>
        {
            new SetlistSong { SetlistId = weddingSetlist.Id, SongId = songByTitle.TryGetValue("Billie Jean", out var billieJean) ? billieJean.Id : throw new Exception("Song 'Billie Jean' not found"), Position = 1, PerformanceNotes = "High energy opener" },
            new SetlistSong { SetlistId = weddingSetlist.Id, SongId = songByTitle.TryGetValue("Uptown Funk", out var uptownFunk) ? uptownFunk.Id : throw new Exception("Song 'Uptown Funk' not found"), Position = 2, PerformanceNotes = "Get everyone dancing" },
            new SetlistSong { SetlistId = weddingSetlist.Id, SongId = songByTitle.TryGetValue("Hotel California", out var hotelCalifornia) ? hotelCalifornia.Id : throw new Exception("Song 'Hotel California' not found"), Position = 3, PerformanceNotes = "Crowd sing-along" },
            new SetlistSong { SetlistId = weddingSetlist.Id, SongId = songByTitle.TryGetValue("Sweet Child O' Mine", out var sweetChildOMine) ? sweetChildOMine.Id : throw new Exception("Song 'Sweet Child O' Mine' not found"), Position = 4, PerformanceNotes = "Guitar showcase" }
        };

        var jazzSongs = new List<SetlistSong>
        {
            new SetlistSong { SetlistId = jazzSetlist.Id, SongId = songByTitle.TryGetValue("Summertime", out var summertime) ? summertime.Id : throw new Exception("Song 'Summertime' not found"), Position = 1, PerformanceNotes = "Gentle opener" },
            new SetlistSong { SetlistId = jazzSetlist.Id, SongId = songByTitle.TryGetValue("Take Five", out var takeFive) ? takeFive.Id : throw new Exception("Song 'Take Five' not found"), Position = 2, PerformanceNotes = "Feature odd time signature" },
            new SetlistSong { SetlistId = jazzSetlist.Id, SongId = songByTitle.TryGetValue("The Thrill Is Gone", out var thrillIsGone) ? thrillIsGone.Id : throw new Exception("Song 'The Thrill Is Gone' not found"), Position = 3, PerformanceNotes = "Blues influence" }
        };

        context.SetlistSongs.AddRange(weddingSongs);
        context.SetlistSongs.AddRange(jazzSongs);
        await context.SaveChangesAsync();

        Log.Information("Sample data seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to seed development data");
    }
}