using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Infrastructure.Services;

/// <summary>
/// Tests for QueryCacheService - verifies caching functionality, performance tracking, and cache invalidation
/// </summary>
public class QueryCacheServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<QueryCacheService>> _mockLogger;
    private readonly Mock<IPerformanceMonitoringService> _mockPerformanceService;
    private readonly QueryCacheService _service;
    private readonly string _testUserId = "test-user-123";

    public QueryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 100
        });
        _mockLogger = new Mock<ILogger<QueryCacheService>>();
        _mockPerformanceService = new Mock<IPerformanceMonitoringService>();
        
        _service = new QueryCacheService(_memoryCache, _mockLogger.Object, _mockPerformanceService.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Act
        var service = new QueryCacheService(_memoryCache, _mockLogger.Object, _mockPerformanceService.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryCacheService(null!, _mockLogger.Object, _mockPerformanceService.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryCacheService(_memoryCache, null!, _mockPerformanceService.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullPerformanceService_ShouldInitializeCorrectly()
    {
        // Act
        var service = new QueryCacheService(_memoryCache, _mockLogger.Object, null);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region GetGenresAsync Tests

    [Fact]
    public async Task GetGenresAsync_WithValidUserId_FirstCall_ShouldCallFactoryAndCacheResult()
    {
        // Arrange
        var expectedGenres = new[] { "Rock", "Pop", "Jazz" };
        var factoryCallCount = 0;
        
        Task<IEnumerable<string>> Factory()
        {
            factoryCallCount++;
            return Task.FromResult<IEnumerable<string>>(expectedGenres);
        }

        // Act
        var result = await _service.GetGenresAsync(_testUserId, Factory);

        // Assert
        result.Should().BeEquivalentTo(expectedGenres);
        factoryCallCount.Should().Be(1);
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), false), Times.Once);
    }

    [Fact]
    public async Task GetGenresAsync_WithValidUserId_SecondCall_ShouldReturnCachedResult()
    {
        // Arrange
        var expectedGenres = new[] { "Rock", "Pop", "Jazz" };
        var factoryCallCount = 0;
        
        Task<IEnumerable<string>> Factory()
        {
            factoryCallCount++;
            return Task.FromResult<IEnumerable<string>>(expectedGenres);
        }

        // First call
        await _service.GetGenresAsync(_testUserId, Factory);

        // Act - Second call
        var result = await _service.GetGenresAsync(_testUserId, Factory);

        // Assert
        result.Should().BeEquivalentTo(expectedGenres);
        factoryCallCount.Should().Be(1); // Factory should only be called once
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task GetGenresAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _service.GetGenresAsync(null!, () => Task.FromResult<IEnumerable<string>>([]));
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public async Task GetGenresAsync_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _service.GetGenresAsync("", () => Task.FromResult<IEnumerable<string>>([]));
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public async Task GetGenresAsync_WithWhitespaceUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _service.GetGenresAsync("   ", () => Task.FromResult<IEnumerable<string>>([]));
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    #endregion

    #region GetArtistsAsync Tests

    [Fact]
    public async Task GetArtistsAsync_WithValidUserId_FirstCall_ShouldCallFactoryAndCacheResult()
    {
        // Arrange
        var expectedArtists = new[] { "The Beatles", "Queen", "Led Zeppelin" };
        var factoryCallCount = 0;
        
        Task<IEnumerable<string>> Factory()
        {
            factoryCallCount++;
            return Task.FromResult<IEnumerable<string>>(expectedArtists);
        }

        // Act
        var result = await _service.GetArtistsAsync(_testUserId, Factory);

        // Assert
        result.Should().BeEquivalentTo(expectedArtists);
        factoryCallCount.Should().Be(1);
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), false), Times.Once);
    }

    [Fact]
    public async Task GetArtistsAsync_WithValidUserId_SecondCall_ShouldReturnCachedResult()
    {
        // Arrange
        var expectedArtists = new[] { "The Beatles", "Queen", "Led Zeppelin" };
        var factoryCallCount = 0;
        
        Task<IEnumerable<string>> Factory()
        {
            factoryCallCount++;
            return Task.FromResult<IEnumerable<string>>(expectedArtists);
        }

        // First call
        await _service.GetArtistsAsync(_testUserId, Factory);

        // Act - Second call
        var result = await _service.GetArtistsAsync(_testUserId, Factory);

        // Assert
        result.Should().BeEquivalentTo(expectedArtists);
        factoryCallCount.Should().Be(1);
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task GetArtistsAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _service.GetArtistsAsync(null!, () => Task.FromResult<IEnumerable<string>>([]));
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    #endregion

    #region GetSongCountAsync Tests

    [Fact]
    public async Task GetSongCountAsync_WithValidUserId_FirstCall_ShouldCallFactoryAndCacheResult()
    {
        // Arrange
        const int expectedCount = 42;
        var factoryCallCount = 0;
        
        Task<int> Factory()
        {
            factoryCallCount++;
            return Task.FromResult(expectedCount);
        }

        // Act
        var result = await _service.GetSongCountAsync(_testUserId, Factory);

        // Assert
        result.Should().Be(expectedCount);
        factoryCallCount.Should().Be(1);
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), false), Times.Once);
    }

    [Fact]
    public async Task GetSongCountAsync_WithValidUserId_SecondCall_ShouldReturnCachedResult()
    {
        // Arrange
        const int expectedCount = 42;
        var factoryCallCount = 0;
        
        Task<int> Factory()
        {
            factoryCallCount++;
            return Task.FromResult(expectedCount);
        }

        // First call
        await _service.GetSongCountAsync(_testUserId, Factory);

        // Act - Second call
        var result = await _service.GetSongCountAsync(_testUserId, Factory);

        // Assert
        result.Should().Be(expectedCount);
        factoryCallCount.Should().Be(1);
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task GetSongCountAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _service.GetSongCountAsync(null!, () => Task.FromResult(0));
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public async Task GetSongCountAsync_WithZeroCount_ShouldCacheZeroValue()
    {
        // Arrange
        const int expectedCount = 0;
        var factoryCallCount = 0;
        
        Task<int> Factory()
        {
            factoryCallCount++;
            return Task.FromResult(expectedCount);
        }

        // Act
        var result1 = await _service.GetSongCountAsync(_testUserId, Factory);
        var result2 = await _service.GetSongCountAsync(_testUserId, Factory);

        // Assert
        result1.Should().Be(0);
        result2.Should().Be(0);
        factoryCallCount.Should().Be(1); // Should cache zero values
    }

    #endregion

    #region GetSetlistCountAsync Tests

    [Fact]
    public async Task GetSetlistCountAsync_WithValidUserId_FirstCall_ShouldCallFactoryAndCacheResult()
    {
        // Arrange
        const int expectedCount = 15;
        var factoryCallCount = 0;
        
        Task<int> Factory()
        {
            factoryCallCount++;
            return Task.FromResult(expectedCount);
        }

        // Act
        var result = await _service.GetSetlistCountAsync(_testUserId, Factory);

        // Assert
        result.Should().Be(expectedCount);
        factoryCallCount.Should().Be(1);
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), false), Times.Once);
    }

    [Fact]
    public async Task GetSetlistCountAsync_WithValidUserId_SecondCall_ShouldReturnCachedResult()
    {
        // Arrange
        const int expectedCount = 15;
        var factoryCallCount = 0;
        
        Task<int> Factory()
        {
            factoryCallCount++;
            return Task.FromResult(expectedCount);
        }

        // First call
        await _service.GetSetlistCountAsync(_testUserId, Factory);

        // Act - Second call
        var result = await _service.GetSetlistCountAsync(_testUserId, Factory);

        // Assert
        result.Should().Be(expectedCount);
        factoryCallCount.Should().Be(1);
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task GetSetlistCountAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _service.GetSetlistCountAsync(null!, () => Task.FromResult(0));
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    #endregion

    #region GetRecentSongsAsync Tests

    [Fact]
    public async Task GetRecentSongsAsync_WithValidUserId_FirstCall_ShouldCallFactoryAndCacheResult()
    {
        // Arrange
        var expectedSongs = new[]
        {
            new Song { Id = 1, Title = "Bohemian Rhapsody", Artist = "Queen", Bpm = 72 },
            new Song { Id = 2, Title = "Hotel California", Artist = "Eagles", Bpm = 75 },
            new Song { Id = 3, Title = "Stairway to Heaven", Artist = "Led Zeppelin", Bpm = 82 }
        };
        var factoryCallCount = 0;
        
        Task<IEnumerable<Song>> Factory()
        {
            factoryCallCount++;
            return Task.FromResult<IEnumerable<Song>>(expectedSongs);
        }

        // Act
        var result = await _service.GetRecentSongsAsync(_testUserId, Factory);

        // Assert
        result.Should().BeEquivalentTo(expectedSongs);
        factoryCallCount.Should().Be(1);
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), false), Times.Once);
    }

    [Fact]
    public async Task GetRecentSongsAsync_WithValidUserId_SecondCall_ShouldReturnCachedResult()
    {
        // Arrange
        var expectedSongs = new[]
        {
            new Song { Id = 1, Title = "Bohemian Rhapsody", Artist = "Queen", Bpm = 72 }
        };
        var factoryCallCount = 0;
        
        Task<IEnumerable<Song>> Factory()
        {
            factoryCallCount++;
            return Task.FromResult<IEnumerable<Song>>(expectedSongs);
        }

        // First call
        await _service.GetRecentSongsAsync(_testUserId, Factory);

        // Act - Second call
        var result = await _service.GetRecentSongsAsync(_testUserId, Factory);

        // Assert
        result.Should().BeEquivalentTo(expectedSongs);
        factoryCallCount.Should().Be(1);
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task GetRecentSongsAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _service.GetRecentSongsAsync(null!, () => Task.FromResult<IEnumerable<Song>>([]));
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public async Task GetRecentSongsAsync_WithEmptySongs_ShouldCacheEmptyResult()
    {
        // Arrange
        var expectedSongs = Array.Empty<Song>();
        var factoryCallCount = 0;
        
        Task<IEnumerable<Song>> Factory()
        {
            factoryCallCount++;
            return Task.FromResult<IEnumerable<Song>>(expectedSongs);
        }

        // Act
        var result1 = await _service.GetRecentSongsAsync(_testUserId, Factory);
        var result2 = await _service.GetRecentSongsAsync(_testUserId, Factory);

        // Assert
        result1.Should().BeEmpty();
        result2.Should().BeEmpty();
        factoryCallCount.Should().Be(1); // Should cache empty collections
    }

    #endregion

    #region Cache Invalidation Tests

    [Fact]
    public async Task InvalidateUserCacheAsync_WithValidUserId_ShouldRemoveAllUserCacheEntries()
    {
        // Arrange
        var genres = new[] { "Rock", "Pop" };
        var artists = new[] { "Queen", "Beatles" };
        const int songCount = 25;
        const int setlistCount = 5;
        var recentSongs = new[] { new Song { Id = 1, Title = "Test Song", Artist = "Test Artist" } };

        // Cache some data
        await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(genres));
        await _service.GetArtistsAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(artists));
        await _service.GetSongCountAsync(_testUserId, () => Task.FromResult(songCount));
        await _service.GetSetlistCountAsync(_testUserId, () => Task.FromResult(setlistCount));
        await _service.GetRecentSongsAsync(_testUserId, () => Task.FromResult<IEnumerable<Song>>(recentSongs));

        var factoryCalls = 0;
        Task<IEnumerable<string>> TestFactory()
        {
            factoryCalls++;
            return Task.FromResult<IEnumerable<string>>(new[] { "New Data" });
        }

        // Act
        await _service.InvalidateUserCacheAsync(_testUserId);

        // Verify cache is cleared by making new calls
        var newGenres = await _service.GetGenresAsync(_testUserId, TestFactory);

        // Assert
        newGenres.Should().BeEquivalentTo(new[] { "New Data" });
        factoryCalls.Should().Be(1); // Factory should be called since cache was cleared
    }

    [Fact]
    public async Task InvalidateUserCacheAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _service.InvalidateUserCacheAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public async Task InvalidateGenreCacheAsync_WithValidUserId_ShouldRemoveOnlyGenreCache()
    {
        // Arrange
        var genres = new[] { "Rock", "Pop" };
        var artists = new[] { "Queen", "Beatles" };

        // Cache data
        await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(genres));
        await _service.GetArtistsAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(artists));

        var genreFactoryCalls = 0;
        var artistFactoryCalls = 0;

        Task<IEnumerable<string>> GenreFactory()
        {
            genreFactoryCalls++;
            return Task.FromResult<IEnumerable<string>>(new[] { "New Genres" });
        }

        Task<IEnumerable<string>> ArtistFactory()
        {
            artistFactoryCalls++;
            return Task.FromResult<IEnumerable<string>>(new[] { "New Artists" });
        }

        // Act
        await _service.InvalidateGenreCacheAsync(_testUserId);

        // Verify only genre cache is cleared
        var newGenres = await _service.GetGenresAsync(_testUserId, GenreFactory);
        var cachedArtists = await _service.GetArtistsAsync(_testUserId, ArtistFactory);

        // Assert
        newGenres.Should().BeEquivalentTo(new[] { "New Genres" });
        cachedArtists.Should().BeEquivalentTo(artists); // Should still be cached
        genreFactoryCalls.Should().Be(1);
        artistFactoryCalls.Should().Be(0); // Artist factory should not be called
    }

    [Fact]
    public async Task InvalidateGenreCacheAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _service.InvalidateGenreCacheAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public async Task InvalidateArtistCacheAsync_WithValidUserId_ShouldRemoveOnlyArtistCache()
    {
        // Arrange
        var genres = new[] { "Rock", "Pop" };
        var artists = new[] { "Queen", "Beatles" };

        // Cache data
        await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(genres));
        await _service.GetArtistsAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(artists));

        var genreFactoryCalls = 0;
        var artistFactoryCalls = 0;

        Task<IEnumerable<string>> GenreFactory()
        {
            genreFactoryCalls++;
            return Task.FromResult<IEnumerable<string>>(new[] { "New Genres" });
        }

        Task<IEnumerable<string>> ArtistFactory()
        {
            artistFactoryCalls++;
            return Task.FromResult<IEnumerable<string>>(new[] { "New Artists" });
        }

        // Act
        await _service.InvalidateArtistCacheAsync(_testUserId);

        // Verify only artist cache is cleared
        var cachedGenres = await _service.GetGenresAsync(_testUserId, GenreFactory);
        var newArtists = await _service.GetArtistsAsync(_testUserId, ArtistFactory);

        // Assert
        cachedGenres.Should().BeEquivalentTo(genres); // Should still be cached
        newArtists.Should().BeEquivalentTo(new[] { "New Artists" });
        genreFactoryCalls.Should().Be(0); // Genre factory should not be called
        artistFactoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task InvalidateArtistCacheAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _service.InvalidateArtistCacheAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
    }

    #endregion

    #region Cache Statistics Tests

    [Fact]
    public async Task GetCacheStatisticsAsync_WithNoCacheActivity_ShouldReturnZeroStatistics()
    {
        // Act
        var statistics = await _service.GetCacheStatisticsAsync();

        // Assert
        statistics.Should().NotBeNull();
        statistics.HitCount.Should().Be(0);
        statistics.MissCount.Should().Be(0);
        statistics.HitRatio.Should().Be(0);
        statistics.CachedEntryCount.Should().Be(0);
        statistics.EvictionCount.Should().Be(0);
        statistics.EstimatedSize.Should().Be(0);
    }

    [Fact]
    public async Task GetCacheStatisticsAsync_WithCacheActivity_ShouldReturnAccurateStatistics()
    {
        // Arrange
        var genres = new[] { "Rock", "Pop" };
        var artists = new[] { "Queen", "Beatles" };

        // Generate cache activity (2 misses, 2 hits)
        await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(genres));
        await _service.GetArtistsAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(artists));
        await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(genres)); // Hit
        await _service.GetArtistsAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(artists)); // Hit

        // Act
        var statistics = await _service.GetCacheStatisticsAsync();

        // Assert
        statistics.Should().NotBeNull();
        statistics.HitCount.Should().Be(2);
        statistics.MissCount.Should().Be(2);
        statistics.HitRatio.Should().Be(0.5);
        statistics.CachedEntryCount.Should().Be(2);
        statistics.EstimatedSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCacheStatisticsAsync_HitRatioCalculation_ShouldBeAccurate()
    {
        // Arrange - Generate 1 miss, 3 hits
        var data = new[] { "Test" };
        await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(data)); // Miss
        await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(data)); // Hit
        await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(data)); // Hit
        await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(data)); // Hit

        // Act
        var statistics = await _service.GetCacheStatisticsAsync();

        // Assert
        statistics.HitCount.Should().Be(3);
        statistics.MissCount.Should().Be(1);
        statistics.HitRatio.Should().Be(0.75); // 3/4 = 0.75
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task ConcurrentAccess_MultipleCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var genres = new[] { "Rock", "Pop", "Jazz" };
        var factoryCallCount = 0;
        
        async Task<IEnumerable<string>> Factory()
        {
            Interlocked.Increment(ref factoryCallCount);
            await Task.Delay(50); // Simulate some work
            return genres;
        }

        // Act - Make multiple concurrent calls
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _service.GetGenresAsync(_testUserId, Factory))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            result.Should().BeEquivalentTo(genres);
        }
        
        // Factory might be called multiple times due to race conditions, but should be reasonable
        factoryCallCount.Should().BeLessOrEqualTo(10);
    }

    [Fact]
    public async Task DifferentUsers_SameDataType_ShouldCacheSeparately()
    {
        // Arrange
        const string user1 = "user1";
        const string user2 = "user2";
        var user1Genres = new[] { "Rock", "Pop" };
        var user2Genres = new[] { "Classical", "Jazz" };

        // Act
        var result1 = await _service.GetGenresAsync(user1, () => Task.FromResult<IEnumerable<string>>(user1Genres));
        var result2 = await _service.GetGenresAsync(user2, () => Task.FromResult<IEnumerable<string>>(user2Genres));

        // Assert
        result1.Should().BeEquivalentTo(user1Genres);
        result2.Should().BeEquivalentTo(user2Genres);
        result1.Should().NotBeEquivalentTo(result2);
    }

    #endregion

    #region Performance Monitoring Integration Tests

    [Fact]
    public async Task CacheOperations_WithPerformanceMonitoring_ShouldTrackCorrectly()
    {
        // Arrange
        var data = new[] { "Test Data" };

        // Act - Generate cache miss and hit
        await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(data));
        await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(data));

        // Assert
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), false), Times.Once);
        _mockPerformanceService.Verify(p => p.RecordCacheOperationAsync(It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task CacheOperations_WithoutPerformanceMonitoring_ShouldNotThrow()
    {
        // Arrange
        var serviceWithoutPerformanceMonitoring = new QueryCacheService(_memoryCache, _mockLogger.Object, null);
        var data = new[] { "Test Data" };

        // Act & Assert - Should not throw
        var result = await serviceWithoutPerformanceMonitoring.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(data));
        result.Should().BeEquivalentTo(data);
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task Factory_ThrowsException_ShouldPropagateException()
    {
        // Arrange
        Task<IEnumerable<string>> FailingFactory()
        {
            throw new InvalidOperationException("Test exception");
        }

        // Act & Assert
        var act = () => _service.GetGenresAsync(_testUserId, FailingFactory);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Test exception");
    }

    [Fact]
    public async Task LargeDataSets_ShouldHandleCorrectly()
    {
        // Arrange
        var largeGenreList = Enumerable.Range(1, 10000).Select(i => $"Genre{i}").ToArray();

        // Act
        var result = await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(largeGenreList));

        // Assert
        result.Should().HaveCount(10000);
        result.Should().BeEquivalentTo(largeGenreList);
    }

    [Fact]
    public async Task SpecialCharacters_InUserId_ShouldHandleCorrectly()
    {
        // Arrange
        const string specialUserId = "user@domain.com_123-456";
        var genres = new[] { "Rock", "Pop" };

        // Act
        var result = await _service.GetGenresAsync(specialUserId, () => Task.FromResult<IEnumerable<string>>(genres));

        // Assert
        result.Should().BeEquivalentTo(genres);
    }

    [Fact]
    public async Task UnicodeData_ShouldHandleCorrectly()
    {
        // Arrange
        var unicodeGenres = new[] { "ロック", "ポップ", "ジャズ", "クラシック" }; // Japanese genre names
        
        // Act
        var result = await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(unicodeGenres));
        var cachedResult = await _service.GetGenresAsync(_testUserId, () => Task.FromResult<IEnumerable<string>>(new[] { "Fallback" }));

        // Assert
        result.Should().BeEquivalentTo(unicodeGenres);
        cachedResult.Should().BeEquivalentTo(unicodeGenres); // Should return cached Unicode data
    }

    #endregion

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }
}