using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Configuration;
using SetlistStudio.Tests.Integration;
using Xunit;

namespace SetlistStudio.Tests.Integration.Database;

/// <summary>
/// Integration tests for PostgreSQL-specific functionality
/// Tests connection pooling, read replicas, and PostgreSQL-specific features
/// </summary>
public class PostgreSqlIntegrationTests : PostgreSqlIntegrationTestBase
{
    public PostgreSqlIntegrationTests(PostgreSqlTestFixture fixture) : base(fixture)
    {
        // Constructor will throw SkipException if PostgreSQL is not available
        // This is handled by xUnit and tests will be skipped
    }

    [Fact(Skip = "Requires PostgreSQL container - run manually when needed")]
    public async Task PostgreSql_ShouldSupportBasicCrudOperations()
    {
        // Arrange
        await ClearDatabaseAsync();
        
        var userId = "test-user-1";
        var song = new Song
        {
            Title = "Sweet Child O' Mine",
            Artist = "Guns N' Roses",
            Album = "Appetite for Destruction",
            Genre = "Rock",
            MusicalKey = "D",
            Bpm = 125,
            DurationSeconds = 356,
            UserId = userId
        };

        // Act - Create
        Context.Songs.Add(song);
        await Context.SaveChangesAsync();

        // Act - Read
        var retrievedSong = await ReadOnlyContext.Songs
            .FirstOrDefaultAsync(s => s.Title == "Sweet Child O' Mine");

        // Act - Update
        song.Bpm = 130;
        Context.Songs.Update(song);
        await Context.SaveChangesAsync();

        var updatedSong = await Context.Songs.FindAsync(song.Id);

        // Act - Delete
        Context.Songs.Remove(song);
        await Context.SaveChangesAsync();

        var deletedSong = await Context.Songs.FindAsync(song.Id);

        // Assert
        retrievedSong.Should().NotBeNull();
        retrievedSong!.Title.Should().Be("Sweet Child O' Mine");
        retrievedSong.Artist.Should().Be("Guns N' Roses");
        retrievedSong.Bpm.Should().Be(125);

        updatedSong.Should().NotBeNull();
        updatedSong!.Bpm.Should().Be(130);

        deletedSong.Should().BeNull();
    }

    [Fact(Skip = "Requires PostgreSQL container - run manually when needed")]
    public async Task PostgreSql_ShouldSupportComplexQueries()
    {
        // Arrange
        await ClearDatabaseAsync();
        
        var userId = "test-user-complex";
        var songs = new[]
        {
            new Song { Title = "Hotel California", Artist = "Eagles", Genre = "Rock", Bpm = 75, MusicalKey = "Bm", UserId = userId },
            new Song { Title = "Stairway to Heaven", Artist = "Led Zeppelin", Genre = "Rock", Bpm = 82, MusicalKey = "Am", UserId = userId },
            new Song { Title = "Bohemian Rhapsody", Artist = "Queen", Genre = "Rock", Bpm = 72, MusicalKey = "Bb", UserId = userId },
            new Song { Title = "Take Five", Artist = "Dave Brubeck", Genre = "Jazz", Bpm = 176, MusicalKey = "Bb", UserId = userId },
            new Song { Title = "Blue in Green", Artist = "Miles Davis", Genre = "Jazz", Bpm = 120, MusicalKey = "Bb", UserId = userId }
        };

        Context.Songs.AddRange(songs);
        await Context.SaveChangesAsync();

        // Act - Complex query with grouping and filtering
        var genreStats = await ReadOnlyContext.Songs
            .Where(s => s.UserId == userId)
            .GroupBy(s => s.Genre)
            .Select(g => new
            {
                Genre = g.Key,
                Count = g.Count(),
                AverageBpm = g.Average(s => s.Bpm),
                MinBpm = g.Min(s => s.Bpm),
                MaxBpm = g.Max(s => s.Bpm)
            })
            .OrderBy(g => g.Genre)
            .ToListAsync();

        // Act - Query with multiple conditions
        var rockSongsInBb = await ReadOnlyContext.Songs
            .Where(s => s.UserId == userId && s.Genre == "Rock" && s.MusicalKey == "Bb")
            .OrderBy(s => s.Title)
            .ToListAsync();

        // Assert
        genreStats.Should().HaveCount(2);
        
        var rockStats = genreStats.First(g => g.Genre == "Rock");
        rockStats.Count.Should().Be(3);
        rockStats.AverageBpm.Should().BeApproximately(76.33, 0.1);
        
        var jazzStats = genreStats.First(g => g.Genre == "Jazz");
        jazzStats.Count.Should().Be(2);
        jazzStats.AverageBpm.Should().Be(148);

        rockSongsInBb.Should().HaveCount(1);
        rockSongsInBb[0].Title.Should().Be("Bohemian Rhapsody");
    }

    [Fact(Skip = "Requires PostgreSQL container - run manually when needed")]
    public async Task PostgreSql_ShouldSupportTransactions()
    {
        // Arrange
        await ClearDatabaseAsync();
        
        var userId = "test-user-transaction";
        var song1 = new Song { Title = "Song 1", Artist = "Artist 1", UserId = userId };
        var song2 = new Song { Title = "Song 2", Artist = "Artist 2", UserId = userId };

        // Act - Successful transaction
        using (var transaction = await Context.Database.BeginTransactionAsync())
        {
            Context.Songs.Add(song1);
            await Context.SaveChangesAsync();
            
            Context.Songs.Add(song2);
            await Context.SaveChangesAsync();
            
            await transaction.CommitAsync();
        }

        var committedSongs = await ReadOnlyContext.Songs
            .Where(s => s.UserId == userId)
            .CountAsync();

        // Act - Failed transaction
        var song3 = new Song { Title = "Song 3", Artist = "Artist 3", UserId = userId };
        
        try
        {
            using var transaction = await Context.Database.BeginTransactionAsync();
            Context.Songs.Add(song3);
            await Context.SaveChangesAsync();
            
            // Simulate an error
            throw new InvalidOperationException("Simulated error");
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }

        var finalSongCount = await ReadOnlyContext.Songs
            .Where(s => s.UserId == userId)
            .CountAsync();

        // Assert
        committedSongs.Should().Be(2);
        finalSongCount.Should().Be(2); // Song3 should not be committed
    }

    [Fact(Skip = "Requires PostgreSQL container - run manually when needed")]
    public async Task PostgreSql_ShouldSupportConcurrentOperations()
    {
        // Arrange
        await ClearDatabaseAsync();
        
        var userId = "test-user-concurrent";
        
        // Act - Create multiple songs concurrently
        var tasks = Enumerable.Range(1, 10).Select(async i =>
        {
            using var context = DatabaseFactory.CreateContext();
            var song = new Song 
            { 
                Title = $"Song {i}", 
                Artist = $"Artist {i}", 
                UserId = userId,
                Bpm = 100 + i
            };
            
            context.Songs.Add(song);
            return await context.SaveChangesAsync();
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result => result.Should().Be(1));
        
        var totalSongs = await ReadOnlyContext.Songs
            .Where(s => s.UserId == userId)
            .CountAsync();
        
        totalSongs.Should().Be(10);
    }

    [Fact(Skip = "Requires PostgreSQL container - run manually when needed")]
    public async Task PostgreSql_ReadOnlyContext_ShouldPreventWrites()
    {
        // Arrange
        var song = new Song { Title = "Test Song", Artist = "Test Artist", UserId = "test-user" };

        // Act & Assert
        var action = () => ReadOnlyContext.Songs.Add(song);
        action.Should().NotThrow(); // Adding to context doesn't throw

        var saveAction = async () => await ReadOnlyContext.SaveChangesAsync();
        await saveAction.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This is a read-only context. Use SetlistStudioDbContext for write operations.");
    }

    [Fact(Skip = "Requires PostgreSQL container - run manually when needed")]
    public async Task PostgreSql_ShouldSupportIndexesForPerformance()
    {
        // Arrange
        await ClearDatabaseAsync();
        
        var userId = "test-user-indexes";
        var songs = Enumerable.Range(1, 100).Select(i => new Song
        {
            Title = $"Song {i:D3}",
            Artist = $"Artist {i % 10}",
            Genre = i % 2 == 0 ? "Rock" : "Jazz",
            MusicalKey = new[] { "C", "D", "E", "F", "G", "A", "B" }[i % 7],
            UserId = userId,
            Bpm = 60 + (i % 100)
        }).ToArray();

        Context.Songs.AddRange(songs);
        await Context.SaveChangesAsync();

        // Act - Query using indexed columns (should be fast)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        var userSongs = await ReadOnlyContext.Songs
            .Where(s => s.UserId == userId) // Uses IX_Songs_UserId
            .CountAsync();
        
        var userSongsByArtist = await ReadOnlyContext.Songs
            .Where(s => s.UserId == userId && s.Artist == "Artist 5") // Uses IX_Songs_UserId_Artist
            .CountAsync();
        
        var userSongsByGenre = await ReadOnlyContext.Songs
            .Where(s => s.UserId == userId && s.Genre == "Rock") // Uses IX_Songs_UserId_Genre
            .CountAsync();
        
        sw.Stop();

        // Assert - Verify results and performance
        userSongs.Should().Be(100);
        userSongsByArtist.Should().Be(10);
        userSongsByGenre.Should().Be(50);
        
        // Performance assertion - should complete quickly with indexes
        sw.ElapsedMilliseconds.Should().BeLessThan(1000);
    }
}