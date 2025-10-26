using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Configuration;
using SetlistStudio.Infrastructure.Services;

namespace SetlistStudio.Infrastructure.Data;

/// <summary>
/// Read-only database context for query operations
/// Uses read replicas when available for better performance
/// </summary>
public class ReadOnlySetlistStudioDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly DatabaseProviderService _providerService;

    public ReadOnlySetlistStudioDbContext(
        DbContextOptions<ReadOnlySetlistStudioDbContext> options,
        DatabaseProviderService providerService)
        : base(options)
    {
        _providerService = providerService;
        
        // Configure read-only behavior
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    /// <summary>
    /// Songs in the user's music library (read-only)
    /// </summary>
    public DbSet<Song> Songs { get; set; } = null!;

    /// <summary>
    /// User-created setlists (read-only)
    /// </summary>
    public DbSet<Setlist> Setlists { get; set; } = null!;

    /// <summary>
    /// Junction table linking songs to setlists with ordering (read-only)
    /// </summary>
    public DbSet<SetlistSong> SetlistSongs { get; set; } = null!;

    /// <summary>
    /// Audit logs for tracking data changes (read-only)
    /// </summary>
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            _providerService.ConfigureReadContext(optionsBuilder);
        }
        
        base.OnConfiguring(optionsBuilder);
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureEntities(builder);
    }

    /// <summary>
    /// Override SaveChanges to prevent modifications on read-only context
    /// </summary>
    public override int SaveChanges()
    {
        throw new InvalidOperationException("This is a read-only context. Use SetlistStudioDbContext for write operations.");
    }

    /// <summary>
    /// Override SaveChangesAsync to prevent modifications on read-only context
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("This is a read-only context. Use SetlistStudioDbContext for write operations.");
    }

    private static void ConfigureEntities(ModelBuilder builder)
    {
        // Configure Song entity
        builder.Entity<Song>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Title).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Artist).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Album).HasMaxLength(200);
            entity.Property(s => s.Genre).HasMaxLength(50);
            entity.Property(s => s.MusicalKey).HasMaxLength(10);
            entity.Property(s => s.Notes).HasMaxLength(1000);
            entity.Property(s => s.UserId).IsRequired().HasMaxLength(450);

            // Performance indexes for common queries
            entity.HasIndex(s => s.UserId).HasDatabaseName("IX_Songs_UserId");
            entity.HasIndex(s => new { s.UserId, s.Artist }).HasDatabaseName("IX_Songs_UserId_Artist");
            entity.HasIndex(s => new { s.UserId, s.Genre }).HasDatabaseName("IX_Songs_UserId_Genre");
            entity.HasIndex(s => new { s.UserId, s.MusicalKey }).HasDatabaseName("IX_Songs_UserId_MusicalKey");
        });

        // Configure Setlist entity
        builder.Entity<Setlist>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Description).HasMaxLength(1000);
            entity.Property(s => s.UserId).IsRequired().HasMaxLength(450);

            // Performance indexes
            entity.HasIndex(s => s.UserId).HasDatabaseName("IX_Setlists_UserId");
            entity.HasIndex(s => new { s.UserId, s.CreatedAt }).HasDatabaseName("IX_Setlists_UserId_CreatedAt");
        });

        // Configure SetlistSong junction entity
        builder.Entity<SetlistSong>(entity =>
        {
            entity.HasKey(ss => ss.Id);
            entity.Property(ss => ss.SetlistId).IsRequired();
            entity.Property(ss => ss.SongId).IsRequired();
            entity.Property(ss => ss.Position).IsRequired();

            // Foreign key relationships
            entity.HasOne<Setlist>()
                  .WithMany()
                  .HasForeignKey(ss => ss.SetlistId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Song>()
                  .WithMany()
                  .HasForeignKey(ss => ss.SongId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Performance indexes
            entity.HasIndex(ss => ss.SetlistId).HasDatabaseName("IX_SetlistSongs_SetlistId");
            entity.HasIndex(ss => new { ss.SetlistId, ss.Position }).HasDatabaseName("IX_SetlistSongs_SetlistId_Position");
        });

        // Configure AuditLog entity
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityId).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(20);
            entity.Property(a => a.UserId).IsRequired().HasMaxLength(450);
            entity.Property(a => a.UserName).HasMaxLength(256);
            entity.Property(a => a.OldValues).HasColumnType("text");
            entity.Property(a => a.NewValues).HasColumnType("text");

            // Performance indexes
            entity.HasIndex(a => a.UserId).HasDatabaseName("IX_AuditLogs_UserId");
            entity.HasIndex(a => new { a.EntityType, a.EntityId }).HasDatabaseName("IX_AuditLogs_Entity");
            entity.HasIndex(a => a.Timestamp).HasDatabaseName("IX_AuditLogs_Timestamp");
        });
    }
}