
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using FluentAssertions;
using Xunit;
using System.Data.Common;

namespace SetlistStudio.Tests.Services
{
    /// <summary>
    /// Advanced tests for SongService covering genre filtering, pagination, and edge cases for GetSongsByGenreAsync
    /// </summary>
    public class SongServiceAdvancedTests : IDisposable
    {
        private readonly SetlistStudioDbContext _context;
        private readonly Mock<ILogger<SongService>> _mockLogger;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IQueryCacheService> _mockCacheService;
        private readonly SongService _songService;
        private readonly string _testUserId = "test-user-123";
        private readonly string _otherUserId = "other-user-456";

        public SongServiceAdvancedTests()
        {
            var options = new DbContextOptionsBuilder<SetlistStudioDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SetlistStudioDbContext(options);
            _mockLogger = new Mock<ILogger<SongService>>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockCacheService = new Mock<IQueryCacheService>();
            _mockCacheService.Setup(x => x.GetGenresAsync(It.IsAny<string>(), It.IsAny<Func<Task<IEnumerable<string>>>>()))
                .Returns<string, Func<Task<IEnumerable<string>>>>((userId, callback) => callback());
            _mockCacheService.Setup(x => x.GetArtistsAsync(It.IsAny<string>(), It.IsAny<Func<Task<IEnumerable<string>>>>()))
                .Returns<string, Func<Task<IEnumerable<string>>>>((userId, callback) => callback());
            _mockCacheService.Setup(x => x.GetSongCountAsync(It.IsAny<string>(), It.IsAny<Func<Task<int>>>()))
                .Returns<string, Func<Task<int>>>((userId, callback) => callback());
            _songService = new SongService(_context, _mockLogger.Object, _mockAuditLogService.Object, _mockCacheService.Object);
        }

        #region GetSongsByGenreAsync Advanced Tests

        [Fact]
        public async Task GetSongsByGenreAsync_ShouldReturnPagedGenreFilteredSongs()
        {
            var songs = new List<Song>
            {
                new Song { Title = "Take Five", Artist = "Dave Brubeck", Genre = "Jazz", UserId = _testUserId },
                new Song { Title = "So What", Artist = "Miles Davis", Genre = "Jazz", UserId = _testUserId },
                new Song { Title = "Blue in Green", Artist = "Bill Evans", Genre = "Jazz", UserId = _testUserId },
                new Song { Title = "Sweet Child O' Mine", Artist = "Guns N' Roses", Genre = "Rock", UserId = _testUserId },
                new Song { Title = "Billie Jean", Artist = "Michael Jackson", Genre = "Pop", UserId = _testUserId },
                new Song { Title = "The Thrill Is Gone", Artist = "B.B. King", Genre = "Blues", UserId = _testUserId }
            };
            _context.Songs.AddRange(songs);
            await _context.SaveChangesAsync();
            var (page1, total1) = await _songService.GetSongsByGenreAsync(_testUserId, "Jazz", pageNumber: 1, pageSize: 2);
            var (page2, total2) = await _songService.GetSongsByGenreAsync(_testUserId, "Jazz", pageNumber: 2, pageSize: 2);
            var (rockSongs, rockTotal) = await _songService.GetSongsByGenreAsync(_testUserId, "Rock", pageNumber: 1, pageSize: 10);
            total1.Should().Be(3);
            total2.Should().Be(3);
            page1.Should().HaveCount(2);
            page2.Should().HaveCount(1);
            page1.Should().OnlyContain(s => s.Genre == "Jazz");
            page2.Should().OnlyContain(s => s.Genre == "Jazz");
            rockSongs.Should().HaveCount(1);
            rockTotal.Should().Be(1);
            rockSongs.First().Title.Should().Be("Sweet Child O' Mine");
        }
        #endregion
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
