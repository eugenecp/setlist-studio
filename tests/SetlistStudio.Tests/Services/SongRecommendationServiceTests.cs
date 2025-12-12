using Xunit;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Services;
using SetlistStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SetlistStudio.Tests.Services
{
    public class SongRecommendationServiceTests
    {
        private readonly SongRecommendationService _recommendationService;
        private readonly IDbContextFactory<SetlistStudioDbContext> _contextFactory;

        public SongRecommendationServiceTests()
        {
            var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _contextFactory = new InMemoryDbContextFactory(options);
            _recommendationService = new SongRecommendationService(_contextFactory);
        }

        private void SeedTestData(string userId)
        {
            using var context = _contextFactory.CreateDbContext();

            // Create test songs with varied attributes
            var songs = new List<Song>
            {
                new Song
                {
                    Id = 1,
                    Title = "Billie Jean",
                    Artist = "Michael Jackson",
                    Genre = "Pop",
                    Bpm = 117,
                    MusicalKey = "F#m",
                    DurationSeconds = 294,
                    DifficultyRating = 3,
                    UserId = userId
                },
                new Song
                {
                    Id = 2,
                    Title = "Smooth Criminal",
                    Artist = "Michael Jackson",
                    Genre = "Pop",
                    Bpm = 125,
                    MusicalKey = "G",
                    DurationSeconds = 272,
                    DifficultyRating = 4,
                    UserId = userId
                },
                new Song
                {
                    Id = 3,
                    Title = "Sweet Child O' Mine",
                    Artist = "Guns N' Roses",
                    Genre = "Rock",
                    Bpm = 125,
                    MusicalKey = "D",
                    DurationSeconds = 356,
                    DifficultyRating = 4,
                    UserId = userId
                },
                new Song
                {
                    Id = 4,
                    Title = "Take Five",
                    Artist = "Dave Brubeck",
                    Genre = "Jazz",
                    Bpm = 176,
                    MusicalKey = "Bb",
                    DurationSeconds = 324,
                    DifficultyRating = 4,
                    UserId = userId
                },
                new Song
                {
                    Id = 5,
                    Title = "Summertime",
                    Artist = "George Gershwin",
                    Genre = "Jazz",
                    Bpm = 85,
                    MusicalKey = "Am",
                    DurationSeconds = 195,
                    DifficultyRating = 2,
                    UserId = userId
                },
                new Song
                {
                    Id = 6,
                    Title = "Bohemian Rhapsody",
                    Artist = "Queen",
                    Genre = "Rock",
                    Bpm = 72,
                    MusicalKey = "Bb",
                    DurationSeconds = 355,
                    DifficultyRating = 5,
                    UserId = userId
                },
                new Song
                {
                    Id = 7,
                    Title = "Hey Jude",
                    Artist = "The Beatles",
                    Genre = "Rock",
                    Bpm = 126,
                    MusicalKey = "C",
                    DurationSeconds = 431,
                    DifficultyRating = 3,
                    UserId = userId
                }
            };

            context.Songs.AddRange(songs);
            context.SaveChanges();
        }

        [Fact]
        public async Task GetNextSongRecommendations_WithValidSong_ReturnsRecommendations()
        {
            // Arrange
            var userId = "test-user";
            SeedTestData(userId);

            // Act
            var recommendations = await _recommendationService.GetNextSongRecommendationsAsync(
                currentSongId: 1, // Billie Jean (Pop, 117 BPM, F#m)
                userId: userId,
                maxResults: 5);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.All(recommendations, r =>
            {
                Assert.True(r.CompatibilityScore >= 0 && r.CompatibilityScore <= 100);
                Assert.NotNull(r.CompatibilityDetails);
                Assert.NotEmpty(r.CompatibilityDetails);
            });
        }

        [Fact]
        public async Task GetNextSongRecommendations_SameGenreHigherScore()
        {
            // Arrange
            var userId = "test-user";
            SeedTestData(userId);

            // Act
            var recommendations = await _recommendationService.GetNextSongRecommendationsAsync(
                currentSongId: 1, // Billie Jean (Pop)
                userId: userId,
                maxResults: 7);

            // Assert
            var smoothCriminal = recommendations.FirstOrDefault(r => r.SongId == 2); // Same genre (Pop)
            var jazz = recommendations.FirstOrDefault(r => r.SongId == 4); // Different genre (Jazz)

            Assert.NotNull(smoothCriminal);
            Assert.NotNull(jazz);
            Assert.True(smoothCriminal.CompatibilityScore > jazz.CompatibilityScore,
                "Same genre should score higher than different genre");
        }

        [Fact]
        public async Task GetNextSongRecommendations_ExcludesSongIds()
        {
            // Arrange
            var userId = "test-user";
            SeedTestData(userId);

            // Act
            var excludeIds = new List<int> { 2, 3, 4 };
            var recommendations = await _recommendationService.GetNextSongRecommendationsAsync(
                currentSongId: 1,
                userId: userId,
                excludeSongIds: excludeIds,
                maxResults: 5);

            // Assert
            var recommendedIds = recommendations.Select(r => r.SongId).ToList();
            Assert.DoesNotContain(2, recommendedIds);
            Assert.DoesNotContain(3, recommendedIds);
            Assert.DoesNotContain(4, recommendedIds);
            Assert.DoesNotContain(1, recommendedIds); // Current song also excluded
        }

        [Fact]
        public async Task GetNextSongRecommendations_SimilarBpmScoresHigher()
        {
            // Arrange
            var userId = "test-user";
            SeedTestData(userId);

            // Act
            var recommendations = await _recommendationService.GetNextSongRecommendationsAsync(
                currentSongId: 1, // Billie Jean (117 BPM)
                userId: userId,
                maxResults: 7);

            // Assert
            // Smooth Criminal (125 BPM - diff of 8) should score higher than Take Five (176 BPM - diff of 59)
            var smoothCriminal = recommendations.FirstOrDefault(r => r.SongId == 2);
            var takeFive = recommendations.FirstOrDefault(r => r.SongId == 4);

            Assert.NotNull(smoothCriminal);
            Assert.NotNull(takeFive);
            Assert.True(smoothCriminal.CompatibilityScore > takeFive.CompatibilityScore,
                "Similar BPM should score higher than very different BPM");
        }

        [Fact]
        public async Task GetNextSongRecommendations_RespectsDifficultyBalance()
        {
            // Arrange
            var userId = "test-user";
            SeedTestData(userId);

            // Act
            var recommendations = await _recommendationService.GetNextSongRecommendationsAsync(
                currentSongId: 1, // Billie Jean (Difficulty 3)
                userId: userId,
                maxResults: 7);

            // Assert
            // Sweet Child O' Mine (Difficulty 4, diff of 1) should score higher than Bohemian Rhapsody (Difficulty 5, diff of 2)
            var sweetChild = recommendations.FirstOrDefault(r => r.SongId == 3);
            var bohemian = recommendations.FirstOrDefault(r => r.SongId == 6);

            Assert.NotNull(sweetChild);
            Assert.NotNull(bohemian);
            Assert.True(sweetChild.CompatibilityScore > bohemian.CompatibilityScore,
                "Smaller difficulty jump should score higher");
        }

        [Fact]
        public async Task GetNextSongRecommendations_ReturnsMaxResults()
        {
            // Arrange
            var userId = "test-user";
            SeedTestData(userId);

            // Act
            var recommendations = await _recommendationService.GetNextSongRecommendationsAsync(
                currentSongId: 1,
                userId: userId,
                maxResults: 3);

            // Assert
            Assert.True(recommendations.Count <= 3);
        }

        [Fact]
        public async Task GetNextSongRecommendations_InvalidSongId_ReturnsEmpty()
        {
            // Arrange
            var userId = "test-user";
            SeedTestData(userId);

            // Act
            var recommendations = await _recommendationService.GetNextSongRecommendationsAsync(
                currentSongId: 999,
                userId: userId);

            // Assert
            Assert.Empty(recommendations);
        }

        [Fact]
        public async Task GetNextSongRecommendations_IncludesCompatibilityDetails()
        {
            // Arrange
            var userId = "test-user";
            SeedTestData(userId);

            // Act
            var recommendations = await _recommendationService.GetNextSongRecommendationsAsync(
                currentSongId: 1,
                userId: userId,
                maxResults: 5);

            // Assert
            Assert.NotEmpty(recommendations);
            var firstRec = recommendations.First();
            Assert.NotEmpty(firstRec.CompatibilityDetails);
            
            // Should have details about BPM, Genre, Key, and Difficulty
            Assert.True(firstRec.CompatibilityDetails.Count >= 3,
                "Should have at least 3 compatibility detail items");

            var details = firstRec.CompatibilityDetails.Select(d => d.ToLower()).ToList();
            Assert.Contains(details, d => d.Contains("tempo") || d.Contains("bpm"));
            Assert.Contains(details, d => d.Contains("genre"));
            Assert.Contains(details, d => d.Contains("difficulty"));
        }

        [Fact]
        public async Task GetNextSongRecommendations_SameKeyHigherScore()
        {
            // Arrange
            var userId = "test-user";

            using (var context = _contextFactory.CreateDbContext())
            {
                var currentSong = new Song
                {
                    Title = "Current",
                    Artist = "Test",
                    Genre = "Rock",
                    Bpm = 100,
                    MusicalKey = "C",
                    DurationSeconds = 200,
                    DifficultyRating = 3,
                    UserId = userId
                };

                var sameKey = new Song
                {
                    Title = "SameKey",
                    Artist = "Test",
                    Genre = "Rock",
                    Bpm = 100,
                    MusicalKey = "C",
                    DurationSeconds = 200,
                    DifficultyRating = 3,
                    UserId = userId
                };

                var differentKey = new Song
                {
                    Title = "DifferentKey",
                    Artist = "Test",
                    Genre = "Rock",
                    Bpm = 100,
                    MusicalKey = "F#",
                    DurationSeconds = 200,
                    DifficultyRating = 3,
                    UserId = userId
                };

                context.Songs.AddRange(currentSong, sameKey, differentKey);
                context.SaveChanges();
            }

            // Act
            var recommendations = await _recommendationService.GetNextSongRecommendationsAsync(
                currentSongId: 1,
                userId: userId,
                maxResults: 5);

            // Assert
            var sameKeyRec = recommendations.FirstOrDefault(r => r.SongId == 2);
            var diffKeyRec = recommendations.FirstOrDefault(r => r.SongId == 3);

            if (sameKeyRec != null && diffKeyRec != null)
            {
                Assert.True(sameKeyRec.CompatibilityScore > diffKeyRec.CompatibilityScore,
                    "Same key should score higher than different key");
            }
        }
    }

    /// <summary>
    /// Helper class for in-memory DbContext factory
    /// </summary>
    internal class InMemoryDbContextFactory : IDbContextFactory<SetlistStudioDbContext>
    {
        private readonly DbContextOptions<SetlistStudioDbContext> _options;

        public InMemoryDbContextFactory(DbContextOptions<SetlistStudioDbContext> options)
        {
            _options = options;
        }

        public SetlistStudioDbContext CreateDbContext()
        {
            return new SetlistStudioDbContext(_options);
        }
    }
}
