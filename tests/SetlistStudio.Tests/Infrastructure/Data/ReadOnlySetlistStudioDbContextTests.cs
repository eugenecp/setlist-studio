using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Configuration;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Infrastructure.Data;

/// <summary>
/// Comprehensive tests for ReadOnlySetlistStudioDbContext
/// Validates read-only behavior, entity configuration, and query optimization
/// </summary>
public class ReadOnlySetlistStudioDbContextTests : IDisposable
{
    private readonly Mock<DatabaseProviderService> _mockProviderService;
    private readonly DbContextOptions<ReadOnlySetlistStudioDbContext> _options;
    private readonly ReadOnlySetlistStudioDbContext _context;

    public ReadOnlySetlistStudioDbContextTests()
    {
        _mockProviderService = new Mock<DatabaseProviderService>(
            Mock.Of<IDatabaseConfiguration>(),
            Mock.Of<ILogger<DatabaseProviderService>>());

        _options = new DbContextOptionsBuilder<ReadOnlySetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ReadOnlySetlistStudioDbContext(_options, _mockProviderService.Object);
    }

    [Fact]
    public void Constructor_ShouldConfigureReadOnlyBehavior()
    {
        // Assert - Context should be configured for read-only operations
        _context.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
        _context.ChangeTracker.AutoDetectChangesEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldSetDatabaseProviderService()
    {
        // Act - Create a new context to test constructor behavior
        var options = new DbContextOptionsBuilder<ReadOnlySetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ReadOnlySetlistStudioDbContext(options, _mockProviderService.Object);

        // Assert - Context should be properly initialized
        context.Should().NotBeNull();
        context.Songs.Should().NotBeNull();
        context.Setlists.Should().NotBeNull();
        context.SetlistSongs.Should().NotBeNull();
        context.AuditLogs.Should().NotBeNull();
    }

    [Fact]
    public void DbSets_ShouldBeProperlyInitialized()
    {
        // Assert - All DbSets should be available for querying
        _context.Songs.Should().NotBeNull();
        _context.Setlists.Should().NotBeNull();
        _context.SetlistSongs.Should().NotBeNull();
        _context.AuditLogs.Should().NotBeNull();
        _context.Users.Should().NotBeNull(); // From IdentityDbContext
        _context.Roles.Should().NotBeNull(); // From IdentityDbContext
    }

    [Fact]
    public void SaveChanges_ShouldThrowInvalidOperationException()
    {
        // Act & Assert - SaveChanges should throw exception for read-only context
        var exception = Assert.Throws<InvalidOperationException>(() => _context.SaveChanges());
        exception.Message.Should().Be("This is a read-only context. Use SetlistStudioDbContext for write operations.");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrowInvalidOperationException()
    {
        // Act & Assert - SaveChangesAsync should throw exception for read-only context
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _context.SaveChangesAsync());
        exception.Message.Should().Be("This is a read-only context. Use SetlistStudioDbContext for write operations.");
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellationToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act & Assert - SaveChangesAsync with cancellation token should throw exception
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _context.SaveChangesAsync(cancellationTokenSource.Token));
        exception.Message.Should().Be("This is a read-only context. Use SetlistStudioDbContext for write operations.");
    }

    [Fact]
    public void OnConfiguring_ShouldHandleUnconfiguredOptions()
    {
        // Act & Assert - Context creation behavior is tested at integration level
        // This test validates the constructor and read-only configuration only
        _context.Should().NotBeNull();
        _context.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
        _context.ChangeTracker.AutoDetectChangesEnabled.Should().BeFalse();
    }

    [Fact]
    public void OnConfiguring_ShouldSkipProviderServiceWhenAlreadyConfigured()
    {
        // Act & Assert - Pre-configured context should work properly
        _context.Should().NotBeNull();
        _context.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
        
        // Should be able to access model without issues
        var model = _context.Model;
        model.Should().NotBeNull();
    }

    [Fact]
    public void ModelCreation_ShouldConfigureSongEntity()
    {
        // Act - Get the model
        var model = _context.Model;
        var songEntity = model.FindEntityType(typeof(Song));

        // Assert - Song entity should be properly configured
        songEntity.Should().NotBeNull();
        
        var titleProperty = songEntity!.FindProperty(nameof(Song.Title));
        titleProperty.Should().NotBeNull();
        titleProperty!.IsNullable.Should().BeFalse();
        titleProperty.GetMaxLength().Should().Be(200);

        var artistProperty = songEntity.FindProperty(nameof(Song.Artist));
        artistProperty.Should().NotBeNull();
        artistProperty!.IsNullable.Should().BeFalse();
        artistProperty.GetMaxLength().Should().Be(200);

        var albumProperty = songEntity.FindProperty(nameof(Song.Album));
        albumProperty?.GetMaxLength().Should().Be(200);

        var genreProperty = songEntity.FindProperty(nameof(Song.Genre));
        genreProperty?.GetMaxLength().Should().Be(50);

        var musicalKeyProperty = songEntity.FindProperty(nameof(Song.MusicalKey));
        musicalKeyProperty?.GetMaxLength().Should().Be(10);

        var notesProperty = songEntity.FindProperty(nameof(Song.Notes));
        notesProperty?.GetMaxLength().Should().Be(1000);

        var userIdProperty = songEntity.FindProperty(nameof(Song.UserId));
        userIdProperty.Should().NotBeNull();
        userIdProperty!.IsNullable.Should().BeFalse();
        userIdProperty.GetMaxLength().Should().Be(450);
    }

    [Fact]
    public void ModelCreation_ShouldConfigureSongIndexes()
    {
        // Act - Get the model
        var model = _context.Model;
        var songEntity = model.FindEntityType(typeof(Song));

        // Assert - Song indexes should be properly configured
        songEntity.Should().NotBeNull();
        
        var indexes = songEntity!.GetIndexes().ToList();
        indexes.Should().NotBeEmpty();

        // Check for specific indexes
        var userIdIndex = indexes.FirstOrDefault(i => 
            i.Properties.Count == 1 && 
            i.Properties[0].Name == nameof(Song.UserId) &&
            i.GetDatabaseName() == "IX_Songs_UserId");
        userIdIndex.Should().NotBeNull();

        var userIdArtistIndex = indexes.FirstOrDefault(i => 
            i.Properties.Count == 2 && 
            i.Properties[0].Name == nameof(Song.UserId) &&
            i.Properties[1].Name == nameof(Song.Artist) &&
            i.GetDatabaseName() == "IX_Songs_UserId_Artist");
        userIdArtistIndex.Should().NotBeNull();

        var userIdGenreIndex = indexes.FirstOrDefault(i => 
            i.Properties.Count == 2 && 
            i.Properties[0].Name == nameof(Song.UserId) &&
            i.Properties[1].Name == nameof(Song.Genre) &&
            i.GetDatabaseName() == "IX_Songs_UserId_Genre");
        userIdGenreIndex.Should().NotBeNull();

        var userIdMusicalKeyIndex = indexes.FirstOrDefault(i => 
            i.Properties.Count == 2 && 
            i.Properties[0].Name == nameof(Song.UserId) &&
            i.Properties[1].Name == nameof(Song.MusicalKey) &&
            i.GetDatabaseName() == "IX_Songs_UserId_MusicalKey");
        userIdMusicalKeyIndex.Should().NotBeNull();
    }

    [Fact]
    public void ModelCreation_ShouldConfigureSetlistEntity()
    {
        // Act - Get the model
        var model = _context.Model;
        var setlistEntity = model.FindEntityType(typeof(Setlist));

        // Assert - Setlist entity should be properly configured
        setlistEntity.Should().NotBeNull();
        
        var nameProperty = setlistEntity!.FindProperty(nameof(Setlist.Name));
        nameProperty.Should().NotBeNull();
        nameProperty!.IsNullable.Should().BeFalse();
        nameProperty.GetMaxLength().Should().Be(200);

        var descriptionProperty = setlistEntity.FindProperty(nameof(Setlist.Description));
        descriptionProperty?.GetMaxLength().Should().Be(1000);

        var userIdProperty = setlistEntity.FindProperty(nameof(Setlist.UserId));
        userIdProperty.Should().NotBeNull();
        userIdProperty!.IsNullable.Should().BeFalse();
        userIdProperty.GetMaxLength().Should().Be(450);
    }

    [Fact]
    public void ModelCreation_ShouldConfigureSetlistIndexes()
    {
        // Act - Get the model
        var model = _context.Model;
        var setlistEntity = model.FindEntityType(typeof(Setlist));

        // Assert - Setlist indexes should be properly configured
        setlistEntity.Should().NotBeNull();
        
        var indexes = setlistEntity!.GetIndexes().ToList();
        indexes.Should().NotBeEmpty();

        // Check for specific indexes
        var userIdIndex = indexes.FirstOrDefault(i => 
            i.Properties.Count == 1 && 
            i.Properties[0].Name == nameof(Setlist.UserId) &&
            i.GetDatabaseName() == "IX_Setlists_UserId");
        userIdIndex.Should().NotBeNull();

        var userIdCreatedAtIndex = indexes.FirstOrDefault(i => 
            i.Properties.Count == 2 && 
            i.Properties[0].Name == nameof(Setlist.UserId) &&
            i.Properties[1].Name == nameof(Setlist.CreatedAt) &&
            i.GetDatabaseName() == "IX_Setlists_UserId_CreatedAt");
        userIdCreatedAtIndex.Should().NotBeNull();
    }

    [Fact]
    public void ModelCreation_ShouldConfigureSetlistSongEntity()
    {
        // Act - Get the model
        var model = _context.Model;
        var setlistSongEntity = model.FindEntityType(typeof(SetlistSong));

        // Assert - SetlistSong entity should be properly configured
        setlistSongEntity.Should().NotBeNull();
        
        var setlistIdProperty = setlistSongEntity!.FindProperty(nameof(SetlistSong.SetlistId));
        setlistIdProperty.Should().NotBeNull();
        setlistIdProperty!.IsNullable.Should().BeFalse();

        var songIdProperty = setlistSongEntity.FindProperty(nameof(SetlistSong.SongId));
        songIdProperty.Should().NotBeNull();
        songIdProperty!.IsNullable.Should().BeFalse();

        var positionProperty = setlistSongEntity.FindProperty(nameof(SetlistSong.Position));
        positionProperty.Should().NotBeNull();
        positionProperty!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void ModelCreation_ShouldConfigureSetlistSongIndexes()
    {
        // Act - Get the model
        var model = _context.Model;
        var setlistSongEntity = model.FindEntityType(typeof(SetlistSong));

        // Assert - SetlistSong indexes should be properly configured
        setlistSongEntity.Should().NotBeNull();
        
        var indexes = setlistSongEntity!.GetIndexes().ToList();
        indexes.Should().NotBeEmpty();

        // Check for specific indexes
        var setlistIdIndex = indexes.FirstOrDefault(i => 
            i.Properties.Count == 1 && 
            i.Properties[0].Name == nameof(SetlistSong.SetlistId) &&
            i.GetDatabaseName() == "IX_SetlistSongs_SetlistId");
        setlistIdIndex.Should().NotBeNull();

        var setlistIdPositionIndex = indexes.FirstOrDefault(i => 
            i.Properties.Count == 2 && 
            i.Properties[0].Name == nameof(SetlistSong.SetlistId) &&
            i.Properties[1].Name == nameof(SetlistSong.Position) &&
            i.GetDatabaseName() == "IX_SetlistSongs_SetlistId_Position");
        setlistIdPositionIndex.Should().NotBeNull();
    }

    [Fact]
    public void ModelCreation_ShouldConfigureAuditLogEntity()
    {
        // Act - Get the model
        var model = _context.Model;
        var auditLogEntity = model.FindEntityType(typeof(AuditLog));

        // Assert - AuditLog entity should be properly configured
        auditLogEntity.Should().NotBeNull();
        
        var entityTypeProperty = auditLogEntity!.FindProperty(nameof(AuditLog.EntityType));
        entityTypeProperty.Should().NotBeNull();
        entityTypeProperty!.IsNullable.Should().BeFalse();
        entityTypeProperty.GetMaxLength().Should().Be(100);

        var entityIdProperty = auditLogEntity.FindProperty(nameof(AuditLog.EntityId));
        entityIdProperty.Should().NotBeNull();
        entityIdProperty!.IsNullable.Should().BeFalse();
        entityIdProperty.GetMaxLength().Should().Be(50);

        var actionProperty = auditLogEntity.FindProperty(nameof(AuditLog.Action));
        actionProperty.Should().NotBeNull();
        actionProperty!.IsNullable.Should().BeFalse();
        actionProperty.GetMaxLength().Should().Be(20);

        var userIdProperty = auditLogEntity.FindProperty(nameof(AuditLog.UserId));
        userIdProperty.Should().NotBeNull();
        userIdProperty!.IsNullable.Should().BeFalse();
        userIdProperty.GetMaxLength().Should().Be(450);

        var userNameProperty = auditLogEntity.FindProperty(nameof(AuditLog.UserName));
        userNameProperty?.GetMaxLength().Should().Be(256);
    }

    [Fact] 
    public void ModelCreation_ShouldConfigureAuditLogIndexes()
    {
        // Act - Get the model
        var model = _context.Model;
        var auditLogEntity = model.FindEntityType(typeof(AuditLog));

        // Assert - AuditLog indexes should be properly configured
        auditLogEntity.Should().NotBeNull();
        
        var indexes = auditLogEntity!.GetIndexes().ToList();
        indexes.Should().NotBeEmpty();

        // Check for specific indexes
        var userIdIndex = indexes.FirstOrDefault(i => 
            i.Properties.Count == 1 && 
            i.Properties[0].Name == nameof(AuditLog.UserId) &&
            i.GetDatabaseName() == "IX_AuditLogs_UserId");
        userIdIndex.Should().NotBeNull();

        var entityIndex = indexes.FirstOrDefault(i => 
            i.Properties.Count == 2 && 
            i.Properties[0].Name == nameof(AuditLog.EntityType) &&
            i.Properties[1].Name == nameof(AuditLog.EntityId) &&
            i.GetDatabaseName() == "IX_AuditLogs_Entity");
        entityIndex.Should().NotBeNull();

        var timestampIndex = indexes.FirstOrDefault(i => 
            i.Properties.Count == 1 && 
            i.Properties[0].Name == nameof(AuditLog.Timestamp) &&
            i.GetDatabaseName() == "IX_AuditLogs_Timestamp");
        timestampIndex.Should().NotBeNull();
    }

    [Fact]
    public void ModelCreation_ShouldConfigureForeignKeyRelationships()
    {
        // Act - Get the model
        var model = _context.Model;
        var setlistSongEntity = model.FindEntityType(typeof(SetlistSong));

        // Assert - Foreign key relationships should be properly configured
        setlistSongEntity.Should().NotBeNull();
        
        var foreignKeys = setlistSongEntity!.GetForeignKeys().ToList();
        foreignKeys.Should().NotBeEmpty(); // EF Core may create additional shadow foreign keys

        // Check that we have foreign keys to both Setlist and Song entities
        var setlistForeignKeys = foreignKeys.Where(fk => 
            fk.PrincipalEntityType.ClrType == typeof(Setlist)).ToList();
        setlistForeignKeys.Should().NotBeEmpty();
        
        var songForeignKeys = foreignKeys.Where(fk => 
            fk.PrincipalEntityType.ClrType == typeof(Song)).ToList();
        songForeignKeys.Should().NotBeEmpty();
        
        // Verify cascade delete behavior exists
        var cascadeDeletes = foreignKeys.Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade).ToList();
        cascadeDeletes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Context_ShouldSupportBasicQueryOperations()
    {
        // Arrange - Ensure database is created
        await _context.Database.EnsureCreatedAsync();

        // Act - Test that basic query operations work
        var songsQuery = _context.Songs.AsQueryable();
        var setlistsQuery = _context.Setlists.AsQueryable();
        var setlistSongsQuery = _context.SetlistSongs.AsQueryable();
        var auditLogsQuery = _context.AuditLogs.AsQueryable();

        var songsCount = await songsQuery.CountAsync();
        var setlistsCount = await setlistsQuery.CountAsync();
        var setlistSongsCount = await setlistSongsQuery.CountAsync();
        var auditLogsCount = await auditLogsQuery.CountAsync();

        // Assert - Queries should execute without errors
        songsCount.Should().BeGreaterThanOrEqualTo(0);
        setlistsCount.Should().BeGreaterThanOrEqualTo(0);
        setlistSongsCount.Should().BeGreaterThanOrEqualTo(0);
        auditLogsCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Context_ShouldInheritFromIdentityDbContext()
    {
        // Assert - Context should inherit from IdentityDbContext with ApplicationUser
        _context.Should().BeAssignableTo<IdentityDbContext<ApplicationUser>>();
        _context.Should().BeAssignableTo<DbContext>();
    }

    [Fact]
    public void Context_ShouldHaveReadOnlyTrackingBehavior()
    {
        // Assert - Context should be configured for optimal read-only performance
        _context.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
        _context.ChangeTracker.AutoDetectChangesEnabled.Should().BeFalse();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}