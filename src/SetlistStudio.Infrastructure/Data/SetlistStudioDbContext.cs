using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Services;

namespace SetlistStudio.Infrastructure.Data;

/// <summary>
/// Entity Framework database context for Setlist Studio
/// Manages user authentication and application data with write operations support
/// </summary>
public class SetlistStudioDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly DatabaseProviderService? _providerService;

    public SetlistStudioDbContext(DbContextOptions<SetlistStudioDbContext> options)
        : base(options)
    {
    }

    public SetlistStudioDbContext(
        DbContextOptions<SetlistStudioDbContext> options,
        DatabaseProviderService providerService)
        : base(options)
    {
        _providerService = providerService;
    }

    /// <summary>
    /// Songs in the user's music library
    /// </summary>
    public DbSet<Song> Songs { get; set; } = null!;

    /// <summary>
    /// User-created setlists
    /// </summary>
    public DbSet<Setlist> Setlists { get; set; } = null!;

    /// <summary>
    /// Junction table linking songs to setlists with ordering
    /// </summary>
    public DbSet<SetlistSong> SetlistSongs { get; set; } = null!;

    /// <summary>
    /// Audit logs for tracking data changes
    /// </summary>
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    /// <summary>
    /// Setlist templates for reusable performance blueprints
    /// </summary>
    public DbSet<SetlistTemplate> SetlistTemplates { get; set; } = null!;

    /// <summary>
    /// Junction table linking songs to templates with ordering
    /// </summary>
    public DbSet<SetlistTemplateSong> SetlistTemplateSongs { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured && _providerService != null)
        {
            _providerService.ConfigureWriteContext(optionsBuilder);
        }
        
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Song entity
        builder.Entity<Song>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Title).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Artist).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Album).HasMaxLength(200);
            entity.Property(s => s.Genre).HasMaxLength(50);
            entity.Property(s => s.MusicalKey).HasMaxLength(10);
            entity.Property(s => s.Notes).HasMaxLength(2000);
            entity.Property(s => s.Tags).HasMaxLength(500);
            entity.Property(s => s.UserId).IsRequired();

            // Foreign key relationship to User
            entity.HasOne(s => s.User)
                  .WithMany(u => u.Songs)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(s => s.UserId);
            entity.HasIndex(s => new { s.UserId, s.Title });
            entity.HasIndex(s => new { s.UserId, s.Artist });
            entity.HasIndex(s => new { s.UserId, s.Genre });
            entity.HasIndex(s => new { s.UserId, s.MusicalKey });
            entity.HasIndex(s => new { s.UserId, s.Bpm });
            entity.HasIndex(s => new { s.UserId, s.Album });
            entity.HasIndex(s => new { s.UserId, s.CreatedAt });
        });

        // Configure Setlist entity
        builder.Entity<Setlist>(entity =>
        {
            entity.HasKey(sl => sl.Id);
            entity.Property(sl => sl.Name).IsRequired().HasMaxLength(200);
            entity.Property(sl => sl.Description).HasMaxLength(1000);
            entity.Property(sl => sl.Venue).HasMaxLength(200);
            entity.Property(sl => sl.PerformanceNotes).HasMaxLength(2000);
            entity.Property(sl => sl.UserId).IsRequired();

            // Foreign key relationship to User
            entity.HasOne(sl => sl.User)
                  .WithMany(u => u.Setlists)
                  .HasForeignKey(sl => sl.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(sl => sl.UserId);
            entity.HasIndex(sl => new { sl.UserId, sl.Name });
            entity.HasIndex(sl => new { sl.UserId, sl.IsTemplate });
            entity.HasIndex(sl => new { sl.UserId, sl.PerformanceDate });
            entity.HasIndex(sl => new { sl.UserId, sl.CreatedAt });
            entity.HasIndex(sl => new { sl.UserId, sl.IsActive });
            entity.HasIndex(sl => new { sl.UserId, sl.Venue });
            entity.HasIndex(sl => new { sl.UserId, sl.IsTemplate, sl.IsActive });
        });

        // Configure SetlistSong junction entity
        builder.Entity<SetlistSong>(entity =>
        {
            entity.HasKey(ss => ss.Id);
            entity.Property(ss => ss.Position).IsRequired();
            entity.Property(ss => ss.TransitionNotes).HasMaxLength(500);
            entity.Property(ss => ss.PerformanceNotes).HasMaxLength(1000);
            entity.Property(ss => ss.CustomKey).HasMaxLength(10);
            entity.Property(ss => ss.SetlistId).IsRequired();
            entity.Property(ss => ss.SongId).IsRequired();

            // Foreign key relationships
            entity.HasOne(ss => ss.Setlist)
                  .WithMany(sl => sl.SetlistSongs)
                  .HasForeignKey(ss => ss.SetlistId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ss => ss.Song)
                  .WithMany(s => s.SetlistSongs)
                  .HasForeignKey(ss => ss.SongId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint to prevent duplicate songs in same setlist
            entity.HasIndex(ss => new { ss.SetlistId, ss.SongId }).IsUnique();
            
            // Index for ordering
            entity.HasIndex(ss => new { ss.SetlistId, ss.Position });
        });

        // Configure AuditLog entity
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.UserId).IsRequired().HasMaxLength(450);
            entity.Property(a => a.UserName).HasMaxLength(256);
            entity.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityId).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(20);
            entity.Property(a => a.Timestamp).IsRequired();
            entity.Property(a => a.IpAddress).HasMaxLength(45);
            entity.Property(a => a.UserAgent).HasMaxLength(500);
            entity.Property(a => a.AdditionalContext).HasMaxLength(1000);
            entity.Property(a => a.SessionId).HasMaxLength(100);
            entity.Property(a => a.CorrelationId).HasMaxLength(100);
            
            // Indexes for common queries
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.Timestamp);
        });

        // Configure ApplicationUser extensions
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName).HasMaxLength(200);
            entity.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(u => u.Provider).HasMaxLength(50);
            entity.Property(u => u.ProviderKey).HasMaxLength(200);
        });

        // Configure SetlistTemplate
        builder.Entity<SetlistTemplate>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).HasMaxLength(1000);
            entity.Property(t => t.Category).HasMaxLength(100);
            entity.Property(t => t.UserId).IsRequired();

            // Indexes for filtering and pagination
            entity.HasIndex(t => t.UserId);
            entity.HasIndex(t => new { t.UserId, t.Category });
            entity.HasIndex(t => new { t.UserId, t.CreatedAt });

            // Navigation to template songs
            entity.HasMany(t => t.TemplateSongs)
                .WithOne(ts => ts.Template)
                .HasForeignKey(ts => ts.SetlistTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SetlistTemplateSong junction table
        builder.Entity<SetlistTemplateSong>(entity =>
        {
            entity.HasKey(ts => ts.Id);

            // Composite index for template + position ordering
            entity.HasIndex(ts => new { ts.SetlistTemplateId, ts.Position });

            // Foreign key to template
            entity.HasOne(ts => ts.Template)
                .WithMany(t => t.TemplateSongs)
                .HasForeignKey(ts => ts.SetlistTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to song
            entity.HasOne(ts => ts.Song)
                .WithMany()
                .HasForeignKey(ts => ts.SongId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Ensures database is created and seeded with sample data for development
    /// </summary>
    public async Task EnsureCreatedWithSampleDataAsync()
    {
        await Database.EnsureCreatedAsync();
        
        if (!Songs.Any())
        {
            await SeedSampleDataAsync();
        }
    }

    /// <summary>
    /// Seeds the database with realistic sample music data for development and testing
    /// </summary>
    private async Task SeedSampleDataAsync()
    {
        // This would be populated when users actually use the app
        // Sample data can be added here for development/demo purposes
        await SaveChangesAsync();
    }
}