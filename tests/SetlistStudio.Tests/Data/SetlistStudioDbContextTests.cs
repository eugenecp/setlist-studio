using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using Xunit;

namespace SetlistStudio.Tests.Data;

/// <summary>
/// Comprehensive tests for SetlistStudioDbContext covering all remaining uncovered scenarios
/// Target: Increase DbContext coverage from 89.7% to 100%
/// </summary>
public class SetlistStudioDbContextTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;

    public SetlistStudioDbContextTests()
    {
        var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SetlistStudioDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Entity Configuration Tests

    [Fact]
    public void Song_ShouldHaveCorrectConfiguration()
    {
        // Arrange & Act
        var songEntity = _context.Model.FindEntityType(typeof(Song));

        // Assert
        songEntity.Should().NotBeNull();
        
        // Check primary key
        var primaryKey = songEntity!.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().HaveCount(1);
        primaryKey.Properties.First().Name.Should().Be("Id");

        // Check required properties
        var titleProperty = songEntity.FindProperty("Title");
        titleProperty!.IsNullable.Should().BeFalse();
        titleProperty.GetMaxLength().Should().Be(200);

        var artistProperty = songEntity.FindProperty("Artist");
        artistProperty!.IsNullable.Should().BeFalse();
        artistProperty.GetMaxLength().Should().Be(200);

        var userIdProperty = songEntity.FindProperty("UserId");
        userIdProperty!.IsNullable.Should().BeFalse();

        // Check optional properties with max lengths
        var albumProperty = songEntity.FindProperty("Album");
        albumProperty!.GetMaxLength().Should().Be(200);

        var genreProperty = songEntity.FindProperty("Genre");
        genreProperty!.GetMaxLength().Should().Be(50);

        var musicalKeyProperty = songEntity.FindProperty("MusicalKey");
        musicalKeyProperty!.GetMaxLength().Should().Be(10);

        var notesProperty = songEntity.FindProperty("Notes");
        notesProperty!.GetMaxLength().Should().Be(2000);

        var tagsProperty = songEntity.FindProperty("Tags");
        tagsProperty!.GetMaxLength().Should().Be(500);

        // Check indexes (updated count due to query optimization indexes)
        var indexes = songEntity.GetIndexes().ToList();
        indexes.Should().HaveCount(8); // Updated from 4 to 8 due to new performance indexes
        
        // UserId index
        indexes.Should().Contain(i => i.Properties.Count == 1 && i.Properties.First().Name == "UserId");
        
        // Composite indexes
        indexes.Should().Contain(i => i.Properties.Count == 2 && 
            i.Properties.Any(p => p.Name == "UserId") && i.Properties.Any(p => p.Name == "Title"));
        indexes.Should().Contain(i => i.Properties.Count == 2 && 
            i.Properties.Any(p => p.Name == "UserId") && i.Properties.Any(p => p.Name == "Artist"));
        indexes.Should().Contain(i => i.Properties.Count == 2 && 
            i.Properties.Any(p => p.Name == "UserId") && i.Properties.Any(p => p.Name == "Genre"));
    }

    [Fact]
    public void Setlist_ShouldHaveCorrectConfiguration()
    {
        // Arrange & Act
        var setlistEntity = _context.Model.FindEntityType(typeof(Setlist));

        // Assert
        setlistEntity.Should().NotBeNull();
        
        // Check primary key
        var primaryKey = setlistEntity!.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().HaveCount(1);
        primaryKey.Properties.First().Name.Should().Be("Id");

        // Check required properties
        var nameProperty = setlistEntity.FindProperty("Name");
        nameProperty!.IsNullable.Should().BeFalse();
        nameProperty.GetMaxLength().Should().Be(200);

        var userIdProperty = setlistEntity.FindProperty("UserId");
        userIdProperty!.IsNullable.Should().BeFalse();

        // Check optional properties with max lengths
        var descriptionProperty = setlistEntity.FindProperty("Description");
        descriptionProperty!.GetMaxLength().Should().Be(1000);

        var venueProperty = setlistEntity.FindProperty("Venue");
        venueProperty!.GetMaxLength().Should().Be(200);

        var performanceNotesProperty = setlistEntity.FindProperty("PerformanceNotes");
        performanceNotesProperty!.GetMaxLength().Should().Be(2000);

        // Check indexes (updated count due to query optimization indexes)
        var indexes = setlistEntity.GetIndexes().ToList();
        indexes.Should().HaveCount(8); // Updated from 4 to 8 due to new performance indexes
        
        // Single column indexes
        indexes.Should().Contain(i => i.Properties.Count == 1 && i.Properties.First().Name == "UserId");
        
        // Composite indexes
        indexes.Should().Contain(i => i.Properties.Count == 2 && 
            i.Properties.Any(p => p.Name == "UserId") && i.Properties.Any(p => p.Name == "Name"));
        indexes.Should().Contain(i => i.Properties.Count == 2 && 
            i.Properties.Any(p => p.Name == "UserId") && i.Properties.Any(p => p.Name == "IsTemplate"));
        indexes.Should().Contain(i => i.Properties.Count == 2 && 
            i.Properties.Any(p => p.Name == "UserId") && i.Properties.Any(p => p.Name == "PerformanceDate"));
    }

    [Fact]
    public void SetlistSong_ShouldHaveCorrectConfiguration()
    {
        // Arrange & Act
        var setlistSongEntity = _context.Model.FindEntityType(typeof(SetlistSong));

        // Assert
        setlistSongEntity.Should().NotBeNull();
        
        // Check primary key
        var primaryKey = setlistSongEntity!.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().HaveCount(1);
        primaryKey.Properties.First().Name.Should().Be("Id");

        // Check required properties
        var positionProperty = setlistSongEntity.FindProperty("Position");
        positionProperty!.IsNullable.Should().BeFalse();

        var setlistIdProperty = setlistSongEntity.FindProperty("SetlistId");
        setlistIdProperty!.IsNullable.Should().BeFalse();

        var songIdProperty = setlistSongEntity.FindProperty("SongId");
        songIdProperty!.IsNullable.Should().BeFalse();

        // Check optional properties with max lengths
        var transitionNotesProperty = setlistSongEntity.FindProperty("TransitionNotes");
        transitionNotesProperty!.GetMaxLength().Should().Be(500);

        var performanceNotesProperty = setlistSongEntity.FindProperty("PerformanceNotes");
        performanceNotesProperty!.GetMaxLength().Should().Be(1000);

        var customKeyProperty = setlistSongEntity.FindProperty("CustomKey");
        customKeyProperty!.GetMaxLength().Should().Be(10);

        // Check indexes (there should be 3: SongId, SetlistId+Position, SetlistId+SongId unique)
        var indexes = setlistSongEntity.GetIndexes().ToList();
        indexes.Should().HaveCount(3);
        
        // Unique composite index for SetlistId + SongId
        var uniqueIndex = indexes.FirstOrDefault(i => i.IsUnique);
        uniqueIndex.Should().NotBeNull();
        uniqueIndex!.Properties.Should().HaveCount(2);
        uniqueIndex.Properties.Should().Contain(p => p.Name == "SetlistId");
        uniqueIndex.Properties.Should().Contain(p => p.Name == "SongId");
        
        // Composite index for SetlistId + Position
        var positionIndex = indexes.FirstOrDefault(i => !i.IsUnique && 
            i.Properties.Count == 2 && 
            i.Properties.Any(p => p.Name == "SetlistId") && 
            i.Properties.Any(p => p.Name == "Position"));
        positionIndex.Should().NotBeNull();
        
        // Single column index for SongId
        var songIdIndex = indexes.FirstOrDefault(i => !i.IsUnique && 
            i.Properties.Count == 1 && 
            i.Properties.Any(p => p.Name == "SongId"));
        songIdIndex.Should().NotBeNull();
    }

    [Fact]
    public void ApplicationUser_ShouldHaveCorrectConfiguration()
    {
        // Arrange & Act
        var userEntity = _context.Model.FindEntityType(typeof(ApplicationUser));

        // Assert
        userEntity.Should().NotBeNull();
        
        // Check extended properties with max lengths
        var displayNameProperty = userEntity!.FindProperty("DisplayName");
        displayNameProperty!.GetMaxLength().Should().Be(200);

        var profilePictureUrlProperty = userEntity.FindProperty("ProfilePictureUrl");
        profilePictureUrlProperty!.GetMaxLength().Should().Be(500);

        var providerProperty = userEntity.FindProperty("Provider");
        providerProperty!.GetMaxLength().Should().Be(50);

        var providerKeyProperty = userEntity.FindProperty("ProviderKey");
        providerKeyProperty!.GetMaxLength().Should().Be(200);
    }

    #endregion

    #region Foreign Key Relationship Tests

    [Fact]
    public void Song_ShouldHaveCascadeDeleteRelationshipWithUser()
    {
        // Arrange & Act
        var songEntity = _context.Model.FindEntityType(typeof(Song));
        var userForeignKey = songEntity!.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ApplicationUser));

        // Assert
        userForeignKey.Should().NotBeNull();
        userForeignKey!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        userForeignKey.Properties.Should().HaveCount(1);
        userForeignKey.Properties.First().Name.Should().Be("UserId");
    }

    [Fact]
    public void Setlist_ShouldHaveCascadeDeleteRelationshipWithUser()
    {
        // Arrange & Act
        var setlistEntity = _context.Model.FindEntityType(typeof(Setlist));
        var userForeignKey = setlistEntity!.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ApplicationUser));

        // Assert
        userForeignKey.Should().NotBeNull();
        userForeignKey!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        userForeignKey.Properties.Should().HaveCount(1);
        userForeignKey.Properties.First().Name.Should().Be("UserId");
    }

    [Fact]
    public void SetlistSong_ShouldHaveCascadeDeleteRelationshipWithSetlist()
    {
        // Arrange & Act
        var setlistSongEntity = _context.Model.FindEntityType(typeof(SetlistSong));
        var setlistForeignKey = setlistSongEntity!.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Setlist));

        // Assert
        setlistForeignKey.Should().NotBeNull();
        setlistForeignKey!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        setlistForeignKey.Properties.Should().HaveCount(1);
        setlistForeignKey.Properties.First().Name.Should().Be("SetlistId");
    }

    [Fact]
    public void SetlistSong_ShouldHaveCascadeDeleteRelationshipWithSong()
    {
        // Arrange & Act
        var setlistSongEntity = _context.Model.FindEntityType(typeof(SetlistSong));
        var songForeignKey = setlistSongEntity!.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Song));

        // Assert
        songForeignKey.Should().NotBeNull();
        songForeignKey!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        songForeignKey.Properties.Should().HaveCount(1);
        songForeignKey.Properties.First().Name.Should().Be("SongId");
    }

    #endregion

    #region Sample Data and Database Creation Tests

    [Fact]
    public async Task EnsureCreatedWithSampleDataAsync_ShouldCreateDatabase_WhenDatabaseDoesNotExist()
    {
        // Act
        await _context.EnsureCreatedWithSampleDataAsync();

        // Assert
        var databaseExists = await _context.Database.CanConnectAsync();
        databaseExists.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureCreatedWithSampleDataAsync_ShouldNotSeedData_WhenSongsAlreadyExist()
    {
        // Arrange
        var existingSong = new Song
        {
            Title = "Existing Song",
            Artist = "Existing Artist",
            UserId = "test-user"
        };
        _context.Songs.Add(existingSong);
        await _context.SaveChangesAsync();

        var initialSongCount = await _context.Songs.CountAsync();

        // Act
        await _context.EnsureCreatedWithSampleDataAsync();

        // Assert
        var finalSongCount = await _context.Songs.CountAsync();
        finalSongCount.Should().Be(initialSongCount); // Should not add any new songs
    }

    [Fact]
    public async Task EnsureCreatedWithSampleDataAsync_ShouldCallSeedSampleData_WhenNoSongsExist()
    {
        // Arrange
        var initialSongCount = await _context.Songs.CountAsync();
        initialSongCount.Should().Be(0);

        // Act
        await _context.EnsureCreatedWithSampleDataAsync();

        // Assert
        // The SeedSampleDataAsync method currently doesn't add any data, 
        // but this test ensures the method is called and executes without error
        var finalSongCount = await _context.Songs.CountAsync();
        finalSongCount.Should().Be(0); // Current implementation doesn't add sample data
    }

    [Fact]
    public async Task SeedSampleDataAsync_ShouldExecuteWithoutError_WhenCalled()
    {
        // Arrange & Act
        // Since SeedSampleDataAsync is private, we test it indirectly through EnsureCreatedWithSampleDataAsync
        var act = async () => await _context.EnsureCreatedWithSampleDataAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region DbSet Tests

    [Fact]
    public void DbSets_ShouldBeInitialized()
    {
        // Assert
        _context.Songs.Should().NotBeNull();
        _context.Setlists.Should().NotBeNull();
        _context.SetlistSongs.Should().NotBeNull();
        _context.Users.Should().NotBeNull(); // From IdentityDbContext
    }

    [Fact]
    public async Task Songs_ShouldSaveAndRetrieve_WhenValidData()
    {
        // Arrange
        var song = new Song
        {
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            Album = "A Night at the Opera",
            Genre = "Rock",
            MusicalKey = "Bb",
            Bpm = 72,
            DurationSeconds = 355,
            Notes = "Epic operatic rock song",
            Tags = "rock,classic,opera",
            UserId = "test-user"
        };

        // Act
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Assert
        var savedSong = await _context.Songs.FirstOrDefaultAsync(s => s.Title == "Bohemian Rhapsody");
        savedSong.Should().NotBeNull();
        savedSong!.Artist.Should().Be("Queen");
        savedSong.Album.Should().Be("A Night at the Opera");
        savedSong.Genre.Should().Be("Rock");
        savedSong.MusicalKey.Should().Be("Bb");
        savedSong.Bpm.Should().Be(72);
        savedSong.UserId.Should().Be("test-user");
    }

    [Fact]
    public async Task Setlists_ShouldSaveAndRetrieve_WhenValidData()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Queen Greatest Hits",
            Description = "Best songs from Queen",
            Venue = "Wembley Stadium",
            ExpectedDurationMinutes = 120,
            IsTemplate = false,
            IsActive = true,
            PerformanceNotes = "High-energy performance",
            UserId = "test-user"
        };

        // Act
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Assert
        var savedSetlist = await _context.Setlists.FirstOrDefaultAsync(sl => sl.Name == "Queen Greatest Hits");
        savedSetlist.Should().NotBeNull();
        savedSetlist!.Description.Should().Be("Best songs from Queen");
        savedSetlist.Venue.Should().Be("Wembley Stadium");
        savedSetlist.ExpectedDurationMinutes.Should().Be(120);
        savedSetlist.IsTemplate.Should().BeFalse();
        savedSetlist.IsActive.Should().BeTrue();
        savedSetlist.UserId.Should().Be("test-user");
    }

    [Fact]
    public async Task SetlistSongs_ShouldSaveAndRetrieve_WhenValidData()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = "test-user" };
        var setlist = new Setlist { Name = "Test Setlist", UserId = "test-user" };
        
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        var setlistSong = new SetlistSong
        {
            SetlistId = setlist.Id,
            SongId = song.Id,
            Position = 1,
            TransitionNotes = "Smooth transition",
            PerformanceNotes = "Play with feeling",
            CustomBpm = 80,
            CustomKey = "C",
            IsEncore = false,
            IsOptional = false
        };

        // Act
        _context.SetlistSongs.Add(setlistSong);
        await _context.SaveChangesAsync();

        // Assert
        var savedSetlistSong = await _context.SetlistSongs
            .Include(ss => ss.Song)
            .Include(ss => ss.Setlist)
            .FirstOrDefaultAsync(ss => ss.Position == 1);
        
        savedSetlistSong.Should().NotBeNull();
        savedSetlistSong!.TransitionNotes.Should().Be("Smooth transition");
        savedSetlistSong.PerformanceNotes.Should().Be("Play with feeling");
        savedSetlistSong.CustomBpm.Should().Be(80);
        savedSetlistSong.CustomKey.Should().Be("C");
        savedSetlistSong.Song.Title.Should().Be("Test Song");
        savedSetlistSong.Setlist.Name.Should().Be("Test Setlist");
    }

    #endregion

    #region Constraint Tests

    [Fact]
    public void SetlistSongs_ShouldHaveUniqueIndex_WhenDuplicateSongInSameSetlist()
    {
        // Arrange & Act
        var setlistSongEntity = _context.Model.FindEntityType(typeof(SetlistSong));
        var uniqueIndex = setlistSongEntity!.GetIndexes().FirstOrDefault(i => i.IsUnique);

        // Assert
        // Note: In-memory database doesn't enforce unique constraints, 
        // but we can verify the index configuration is correct
        uniqueIndex.Should().NotBeNull();
        uniqueIndex!.Properties.Should().HaveCount(2);
        uniqueIndex.Properties.Should().Contain(p => p.Name == "SetlistId");
        uniqueIndex.Properties.Should().Contain(p => p.Name == "SongId");
        uniqueIndex.IsUnique.Should().BeTrue();
    }

    #endregion

    #region Enhanced Coverage Tests for 80% Target



    [Fact]
    public async Task EnsureCreatedWithSampleDataAsync_ShouldCallSeedSampleDataAsync_WhenNoSongsExist()
    {
        // Arrange - Ensure database is created but empty
        await _context.Database.EnsureCreatedAsync();
        _context.Songs.Any().Should().BeFalse();

        // Act
        await _context.EnsureCreatedWithSampleDataAsync();

        // Assert - The method should complete successfully
        // Since SeedSampleDataAsync is currently empty, we just verify it executes without error
        _context.Database.CanConnect().Should().BeTrue();
    }

    [Fact]
    public async Task EnsureCreatedWithSampleDataAsync_ShouldNotCallSeedSampleDataAsync_WhenSongsExist()
    {
        // Arrange - Add a song first
        var song = new Song
        {
            Title = "Existing Song",
            Artist = "Existing Artist",
            UserId = "user-123",
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        // Act
        await _context.EnsureCreatedWithSampleDataAsync();

        // Assert - Should not modify existing data
        _context.Songs.Should().HaveCount(1);
        _context.Songs.First().Title.Should().Be("Existing Song");
    }

    [Fact]
    public async Task DbContext_ShouldSupportCascadeDelete_WhenUserIsDeleted()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-cascade-test",
            UserName = "cascadeuser@example.com",
            Email = "cascadeuser@example.com",
            CreatedAt = DateTime.UtcNow
        };

        var song = new Song
        {
            Title = "Cascade Test Song",
            Artist = "Cascade Test Artist",
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var setlist = new Setlist
        {
            Name = "Cascade Test Setlist",
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.Songs.Add(song);
        _context.Setlists.Add(setlist);
        await _context.SaveChangesAsync();

        // Act - Delete the user
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        // Assert - Related entities should be deleted
        var remainingSongs = await _context.Songs.Where(s => s.UserId == user.Id).ToListAsync();
        var remainingSetlists = await _context.Setlists.Where(sl => sl.UserId == user.Id).ToListAsync();
        
        remainingSongs.Should().BeEmpty();
        remainingSetlists.Should().BeEmpty();
    }

    #endregion
}