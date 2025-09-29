using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SetlistStudio.Core.Entities;

namespace SetlistStudio.Infrastructure.Data;

/// <summary>
/// Entity Framework database context for Setlist Studio
/// Manages user authentication and application data
/// </summary>
public class SetlistStudioDbContext : IdentityDbContext<ApplicationUser>
{
    public SetlistStudioDbContext(DbContextOptions<SetlistStudioDbContext> options)
        : base(options)
    {
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

        // Configure ApplicationUser extensions
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName).HasMaxLength(200);
            entity.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(u => u.Provider).HasMaxLength(50);
            entity.Property(u => u.ProviderKey).HasMaxLength(200);
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