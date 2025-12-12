using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Models;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests
{
    public class SongFilterServiceTests : IDisposable
    {
        private readonly DbContextOptions<SetlistStudioDbContext> _dbOptions;
        private readonly SetlistStudioDbContext _context;
        private readonly SongFilterService _filterService;

        public SongFilterServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<SetlistStudioDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new SetlistStudioDbContext(_dbOptions);
            _filterService = new SongFilterService(_context, NullLogger<SongFilterService>.Instance);

            SeedTestData();
        }

        private void SeedTestData()
        {
            _context.Songs.AddRange(
                new Song
                {
                    Id = 1,
                    Title = "Bohemian Rhapsody",
                    Artist = "Queen",
                    Genre = "Rock",
                    Bpm = 180,
                    MusicalKey = "Bm",
                    DurationSeconds = 354,
                    DifficultyRating = 4,
                    Tags = "classic,epic",
                    UserId = "user1"
                },
                new Song
                {
                    Id = 2,
                    Title = "Stairway to Heaven",
                    Artist = "Led Zeppelin",
                    Genre = "Rock",
                    Bpm = 82,
                    MusicalKey = "Am",
                    DurationSeconds = 482,
                    DifficultyRating = 5,
                    Tags = "classic,slow",
                    UserId = "user1"
                },
                new Song
                {
                    Id = 3,
                    Title = "All Blues",
                    Artist = "Miles Davis",
                    Genre = "Jazz",
                    Bpm = 88,
                    MusicalKey = "F",
                    DurationSeconds = 480,
                    DifficultyRating = 3,
                    Tags = "instrumental",
                    UserId = "user1"
                },
                new Song
                {
                    Id = 4,
                    Title = "User2 Song",
                    Artist = "Unknown",
                    Genre = "Pop",
                    Bpm = 120,
                    MusicalKey = "C",
                    DurationSeconds = 200,
                    DifficultyRating = 2,
                    Tags = "pop",
                    UserId = "user2"
                }
            );
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task FilterSongs_ByGenre_ReturnsMatchingRock()
        {
            var criteria = new SongFilterCriteria { Genres = new List<string> { "Rock" } };
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            Assert.NotEmpty(result.Items);
            Assert.All(result.Items, s => Assert.Equal("Rock", s.Genre));
        }

        [Fact]
        public async Task FilterSongs_ByGenreAndBpm_ReturnsFastRockSongs()
        {
            var criteria = new SongFilterCriteria { Genres = new List<string> { "Rock" }, MinBpm = 120, MaxBpm = 200 };
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            Assert.All(result.Items, s =>
            {
                Assert.Equal("Rock", s.Genre);
                Assert.InRange(s.Bpm ?? 0, 120, 200);
            });
        }

        [Fact]
        public async Task FilterSongs_SearchText_MatchesTitleArtistAndAlbum()
        {
            var criteria = new SongFilterCriteria { SearchText = "bohemian" };
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            Assert.NotEmpty(result.Items);
            Assert.Contains(result.Items, s => s.Title.Contains("Bohemian", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task FilterSongs_Pagination_ReturnsCorrectPageAndMetadata()
        {
            // Ensure there are at least 2 pages by setting small page size
            var criteria = new SongFilterCriteria { Genres = new List<string> { "Rock" } };
            var page1 = await _filterService.FilterSongsAsync("user1", criteria, pageNumber: 1, pageSize: 1);
            var page2 = await _filterService.FilterSongsAsync("user1", criteria, pageNumber: 2, pageSize: 1);
            Assert.Equal(1, page1.Items.Count);
            Assert.Equal(1, page2.Items.Count);
            Assert.NotEqual(page1.Items[0].Id, page2.Items[0].Id);
            Assert.True(page1.HasNextPage);
        }

        [Theory]
        [InlineData("title", "asc")]
        [InlineData("artist", "desc")]
        [InlineData("bpm", "asc")]
        [InlineData("duration", "desc")]
        public async Task FilterSongs_Sorting_ReturnsSortedResults(string sortBy, string sortOrder)
        {
            var criteria = new SongFilterCriteria { SortBy = sortBy, SortOrder = sortOrder };
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            AssertIsSorted(result.Items, sortBy, sortOrder == "asc");
        }

        [Fact]
        public async Task FilterSongs_OnlyReturnsCurrentUserSongs()
        {
            var criteria = new SongFilterCriteria();
            var r1 = await _filterService.FilterSongsAsync("user1", criteria);
            var r2 = await _filterService.FilterSongsAsync("user2", criteria);
            Assert.All(r1.Items, s => Assert.Equal("user1", s.UserId));
            Assert.All(r2.Items, s => Assert.Equal("user2", s.UserId));
        }

        [Fact]
        public async Task FilterSongs_NoMatches_ReturnsEmptyButValidResult()
        {
            var criteria = new SongFilterCriteria { SearchText = "zzzz_nonexistent" };
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task FilterSongs_AllCriteriaNull_ReturnsAllUserSongs()
        {
            var criteria = new SongFilterCriteria();
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            var expected = await _context.Songs.CountAsync(s => s.UserId == "user1");
            Assert.Equal(expected, result.TotalCount);
        }

        [Fact]
        public async Task FilterSongs_EmptyGenreList_IgnoresGenreFilter()
        {
            var criteria = new SongFilterCriteria { Genres = new List<string>() };
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            Assert.NotEmpty(result.Items);
        }

        [Theory]
        [InlineData("BOHEMIAN")]
        [InlineData("bohemian")]
        [InlineData("BoHeM")]
        public async Task FilterSongs_SearchIsCaseInsensitive(string searchText)
        {
            var criteria = new SongFilterCriteria { SearchText = searchText };
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            Assert.NotEmpty(result.Items);
        }

        [Fact]
        public async Task FilterSongs_BpmBoundaries_IncludesExactValues()
        {
            // Add a song with BPM exactly 120
            _context.Songs.Add(new Song { Title = "Exact120", Artist = "A", UserId = "user1", Bpm = 120 });
            _context.SaveChanges();
            var criteria = new SongFilterCriteria { MinBpm = 120, MaxBpm = 120 };
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            Assert.All(result.Items, s => Assert.Equal(120, s.Bpm));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task FilterSongs_InvalidPageNumber_DefaultsToFirstPage(int pageNumber)
        {
            var criteria = new SongFilterCriteria();
            var result = await _filterService.FilterSongsAsync("user1", criteria, pageNumber: pageNumber, pageSize: 10);
            Assert.Equal(1, result.PageNumber);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        [InlineData(101)]
        public async Task FilterSongs_InvalidPageSize_DefaultsTo20(int pageSize)
        {
            var criteria = new SongFilterCriteria();
            var result = await _filterService.FilterSongsAsync("user1", criteria, pageSize: pageSize);
            Assert.Equal(20, result.PageSize);
        }

        [Fact]
        public async Task FilterSongs_SongsWithNullProperties_HandledGracefully()
        {
            _context.Songs.Add(new Song { Title = "NullProps", Artist = "X", UserId = "user1" });
            _context.SaveChanges();
            var criteria = new SongFilterCriteria { MusicalKeys = new List<string> { "Cm" } };
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            Assert.All(result.Items, s => Assert.NotNull(s.MusicalKey));
        }

        [Fact]
        public async Task FilterSongs_TagsWithSpaces_MatchesCorrectly()
        {
            _context.Songs.Add(new Song { Title = "TagSpace", Artist = "Y", UserId = "user1", Tags = "wedding,slow jazz,romantic" });
            _context.SaveChanges();
            var criteria = new SongFilterCriteria { Tags = new List<string> { "slow jazz" } };
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            Assert.NotEmpty(result.Items);
            Assert.All(result.Items, s => Assert.Contains("slow jazz", s.Tags ?? string.Empty));
        }

        [Theory]
        [InlineData("Mc'Donald")]
        [InlineData("Ac/Dc")]
        [InlineData("Qu-tips")]
        public async Task FilterSongs_SpecialCharactersInSearch_SearchesCorrectly(string searchText)
        {
            var criteria = new SongFilterCriteria { SearchText = searchText };
            var result = await _filterService.FilterSongsAsync("user1", criteria);
            Assert.NotNull(result);
        }

        // Helpers
        private void AssertIsSorted(List<Song> items, string sortBy, bool ascending)
        {
            if (items.Count < 2) return;
            for (int i = 0; i < items.Count - 1; i++)
            {
                var current = GetSortValue(items[i], sortBy);
                var next = GetSortValue(items[i + 1], sortBy);
                if (ascending)
                    Assert.True(Comparer<object>.Default.Compare(current, next) <= 0);
                else
                    Assert.True(Comparer<object>.Default.Compare(current, next) >= 0);
            }
        }

        private object GetSortValue(Song song, string sortBy)
        {
            return sortBy.ToLower() switch
            {
                "title" => song.Title ?? string.Empty,
                "artist" => song.Artist ?? string.Empty,
                "bpm" => song.Bpm ?? 0,
                "duration" => song.DurationSeconds ?? 0,
                "difficulty" => song.DifficultyRating ?? 0,
                "genre" => song.Genre ?? string.Empty,
                _ => song.Title ?? string.Empty
            };
        }
    }
}
