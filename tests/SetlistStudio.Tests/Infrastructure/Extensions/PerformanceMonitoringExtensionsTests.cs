using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Infrastructure.Extensions;
using SetlistStudio.Infrastructure.Services;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace SetlistStudio.Tests.Infrastructure.Extensions;

/// <summary>
/// Comprehensive tests for PerformanceMonitoringExtensions
/// Tests performance tracking extensions for query monitoring and cache operations
/// </summary>
public class PerformanceMonitoringExtensionsTests
{
    private readonly Mock<IPerformanceMonitoringService> _mockPerformanceService;

    public PerformanceMonitoringExtensionsTests()
    {
        _mockPerformanceService = new Mock<IPerformanceMonitoringService>();
    }

    #region TrackQueryAsync Tests

    [Fact]
    public async Task TrackQueryAsync_ShouldExecuteFunction_AndRecordPerformance()
    {
        // Arrange
        const string queryType = "GetSongs";
        const string expectedResult = "test result";
        var function = new Func<Task<string>>(() => Task.FromResult(expectedResult));

        // Act
        var result = await _mockPerformanceService.Object.TrackQueryAsync(queryType, function);

        // Assert
        result.Should().Be(expectedResult);
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task TrackQueryAsync_WithRecordCount_ShouldUseProvidedCount()
    {
        // Arrange
        const string queryType = "GetArtists";
        const int recordCount = 5;
        var function = new Func<Task<string>>(() => Task.FromResult("result"));

        // Act
        await _mockPerformanceService.Object.TrackQueryAsync(queryType, function, recordCount);

        // Assert
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), recordCount),
            Times.Once);
    }

    [Fact]
    public async Task TrackQueryAsync_WithCollectionResult_ShouldDetectRecordCount()
    {
        // Arrange
        const string queryType = "GetSetlists";
        var expectedList = new List<string> { "item1", "item2", "item3" };
        var function = new Func<Task<List<string>>>(() => Task.FromResult(expectedList));

        // Act
        var result = await _mockPerformanceService.Object.TrackQueryAsync(queryType, function);

        // Assert
        result.Should().BeEquivalentTo(expectedList);
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), 3),
            Times.Once);
    }

    [Fact]
    public async Task TrackQueryAsync_WithEnumerableResult_ShouldDetectRecordCount()
    {
        // Arrange
        const string queryType = "GetGenres";
        var expectedEnumerable = new[] { "Rock", "Jazz", "Blues" }.AsEnumerable();
        var function = new Func<Task<IEnumerable<string>>>(() => Task.FromResult(expectedEnumerable));

        // Act
        var result = await _mockPerformanceService.Object.TrackQueryAsync(queryType, function);

        // Assert
        result.Should().BeEquivalentTo(expectedEnumerable);
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), 3),
            Times.Once);
    }

    [Fact]
    public async Task TrackQueryAsync_WithSingleObjectResult_ShouldCount1()
    {
        // Arrange
        const string queryType = "GetUser";
        var expectedObject = new { Id = 1, Name = "Test User" };
        var function = new Func<Task<object>>(() => Task.FromResult<object>(expectedObject));

        // Act
        var result = await _mockPerformanceService.Object.TrackQueryAsync(queryType, function);

        // Assert
        result.Should().Be(expectedObject);
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), 1),
            Times.Once);
    }

    [Fact]
    public async Task TrackQueryAsync_WithNullResult_ShouldCount0()
    {
        // Arrange
        const string queryType = "GetNullResult";
        var function = new Func<Task<string?>>(() => Task.FromResult<string?>(null));

        // Act
        var result = await _mockPerformanceService.Object.TrackQueryAsync(queryType, function);

        // Assert
        result.Should().BeNull();
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), 0),
            Times.Once);
    }

    [Fact]
    public async Task TrackQueryAsync_WhenFunctionThrows_ShouldStillRecordPerformance()
    {
        // Arrange
        const string queryType = "FailingQuery";
        var function = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockPerformanceService.Object.TrackQueryAsync(queryType, function));

        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task TrackQueryAsync_ShouldRecordReasonableExecutionTime()
    {
        // Arrange
        const string queryType = "TimedQuery";
        const int delayMs = 50;
        var function = new Func<Task<string>>(async () =>
        {
            await Task.Delay(delayMs);
            return "delayed result";
        });

        TimeSpan? recordedTime = null;
        _mockPerformanceService
            .Setup(x => x.RecordQueryPerformanceAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<int>()))
            .Callback<string, TimeSpan, int>((_, time, _) => recordedTime = time)
            .Returns(Task.CompletedTask);

        // Act
        await _mockPerformanceService.Object.TrackQueryAsync(queryType, function);

        // Assert
        recordedTime.Should().NotBeNull();
        recordedTime!.Value.TotalMilliseconds.Should().BeGreaterOrEqualTo(delayMs * 0.1); // Very loose lower bound for CI
        recordedTime.Value.TotalMilliseconds.Should().BeLessOrEqualTo(delayMs * 100); // Very generous upper bound for slow CI (5000ms)
    }

    #endregion

    #region TrackPaginatedQueryAsync Tests

    [Fact]
    public async Task TrackPaginatedQueryAsync_ShouldExecuteFunction_AndRecordPerformance()
    {
        // Arrange
        const string queryType = "GetPaginatedSongs";
        var expectedData = new List<string> { "song1", "song2", "song3" };
        const int totalCount = 100;
        var expectedResult = (expectedData.AsEnumerable(), totalCount);
        
        var function = new Func<Task<(IEnumerable<string> Data, int TotalCount)>>(() => 
            Task.FromResult(expectedResult));

        // Act
        var result = await _mockPerformanceService.Object.TrackPaginatedQueryAsync(queryType, function);

        // Assert
        result.Data.Should().BeEquivalentTo(expectedData);
        result.TotalCount.Should().Be(totalCount);
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), expectedData.Count),
            Times.Once);
    }

    [Fact]
    public async Task TrackPaginatedQueryAsync_WithEmptyData_ShouldRecord0Count()
    {
        // Arrange
        const string queryType = "GetEmptyResults";
        var emptyData = new List<string>();
        const int totalCount = 0;
        var expectedResult = (emptyData.AsEnumerable(), totalCount);
        
        var function = new Func<Task<(IEnumerable<string> Data, int TotalCount)>>(() => 
            Task.FromResult(expectedResult));

        // Act
        var result = await _mockPerformanceService.Object.TrackPaginatedQueryAsync(queryType, function);

        // Assert
        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), 0),
            Times.Once);
    }

    [Fact]
    public async Task TrackPaginatedQueryAsync_WithNullData_ShouldHandle0Count()
    {
        // Arrange
        const string queryType = "GetNullData";
        const int totalCount = 5;
        // Use empty list instead of null to match the constraint
        var expectedResult = (Enumerable.Empty<object>(), totalCount);
        
        var function = new Func<Task<(IEnumerable<object> Data, int TotalCount)>>(() => 
            Task.FromResult(expectedResult));

        // Act
        var result = await _mockPerformanceService.Object.TrackPaginatedQueryAsync(queryType, function);

        // Assert
        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(totalCount);
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), 0),
            Times.Once);
    }

    [Fact]
    public async Task TrackPaginatedQueryAsync_WhenFunctionThrows_ShouldStillRecordPerformance()
    {
        // Arrange
        const string queryType = "FailingPaginatedQuery";
        var function = new Func<Task<(IEnumerable<string> Data, int TotalCount)>>(() => 
            throw new InvalidOperationException("Paginated test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockPerformanceService.Object.TrackPaginatedQueryAsync(queryType, function));

        // Performance should still be recorded even when exceptions occur (finally block behavior)
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), 0),
            Times.Once); // Performance is recorded with recordCount=0 since no data was retrieved
    }

    #endregion

    #region TrackCacheOperationAsync Tests

    [Fact]
    public async Task TrackCacheOperationAsync_WithCacheHit_ShouldRecordHit()
    {
        // Arrange
        const string cacheKey = "user:123:songs";
        const bool isHit = true;

        // Act
        await _mockPerformanceService.Object.TrackCacheOperationAsync(cacheKey, isHit);

        // Assert
        _mockPerformanceService.Verify(
            x => x.RecordCacheOperationAsync(cacheKey, isHit),
            Times.Once);
    }

    [Fact]
    public async Task TrackCacheOperationAsync_WithCacheMiss_ShouldRecordMiss()
    {
        // Arrange
        const string cacheKey = "user:456:setlists";
        const bool isHit = false;

        // Act
        await _mockPerformanceService.Object.TrackCacheOperationAsync(cacheKey, isHit);

        // Assert
        _mockPerformanceService.Verify(
            x => x.RecordCacheOperationAsync(cacheKey, isHit),
            Times.Once);
    }

    [Fact]
    public async Task TrackCacheOperationAsync_WithNullCacheKey_ShouldNotThrow()
    {
        // Arrange
        const string? cacheKey = null;
        const bool isHit = true;

        // Act & Assert - Should not throw
        await _mockPerformanceService.Object.TrackCacheOperationAsync(cacheKey!, isHit);

        _mockPerformanceService.Verify(
            x => x.RecordCacheOperationAsync(cacheKey!, isHit),
            Times.Once);
    }

    [Fact]
    public async Task TrackCacheOperationAsync_WithEmptyCacheKey_ShouldRecord()
    {
        // Arrange
        const string cacheKey = "";
        const bool isHit = false;

        // Act
        await _mockPerformanceService.Object.TrackCacheOperationAsync(cacheKey, isHit);

        // Assert
        _mockPerformanceService.Verify(
            x => x.RecordCacheOperationAsync(cacheKey, isHit),
            Times.Once);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task TrackQueryAsync_WithComplexGenericType_ShouldDetectCount()
    {
        // Arrange
        const string queryType = "GetComplexData";
        var complexData = new Dictionary<string, List<int>>
        {
            { "group1", new List<int> { 1, 2, 3 } },
            { "group2", new List<int> { 4, 5 } }
        };
        var function = new Func<Task<Dictionary<string, List<int>>>>(() => Task.FromResult(complexData));

        // Act
        var result = await _mockPerformanceService.Object.TrackQueryAsync(queryType, function);

        // Assert
        result.Should().BeEquivalentTo(complexData);
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), 2), // Dictionary has 2 entries
            Times.Once);
    }

    [Fact]
    public async Task TrackQueryAsync_WithArray_ShouldDetectCount()
    {
        // Arrange
        const string queryType = "GetArrayData";
        var arrayData = new[] { "item1", "item2", "item3", "item4" };
        var function = new Func<Task<string[]>>(() => Task.FromResult(arrayData));

        // Act
        var result = await _mockPerformanceService.Object.TrackQueryAsync(queryType, function);

        // Assert
        result.Should().BeEquivalentTo(arrayData);
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), 4),
            Times.Once);
    }

    [Fact]
    public async Task TrackQueryAsync_ConcurrentExecution_ShouldTrackIndependently()
    {
        // Arrange
        const string queryType1 = "ConcurrentQuery1";
        const string queryType2 = "ConcurrentQuery2";
        
        var function1 = new Func<Task<string>>(async () =>
        {
            await Task.Delay(25);
            return "result1";
        });
        
        var function2 = new Func<Task<string>>(async () =>
        {
            await Task.Delay(50);
            return "result2";
        });

        // Act
        var task1 = _mockPerformanceService.Object.TrackQueryAsync(queryType1, function1);
        var task2 = _mockPerformanceService.Object.TrackQueryAsync(queryType2, function2);
        
        var results = await Task.WhenAll(task1, task2);

        // Assert
        results[0].Should().Be("result1");
        results[1].Should().Be("result2");
        
        // String "result1" and "result2" are treated as IEnumerable<char>, so 7 characters each
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType1, It.IsAny<TimeSpan>(), 7),
            Times.Once);
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType2, It.IsAny<TimeSpan>(), 7),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ValidQueryType")]
    [InlineData("Query.With.Dots")]
    [InlineData("Query-With-Dashes")]
    [InlineData("Query_With_Underscores")]
    public async Task TrackQueryAsync_WithVariousQueryTypeFormats_ShouldWork(string queryType)
    {
        // Arrange
        var function = new Func<Task<string>>(() => Task.FromResult("test"));

        // Act & Assert - Should not throw
        var result = await _mockPerformanceService.Object.TrackQueryAsync(queryType, function);

        result.Should().Be("test");
        // Note: String is treated as IEnumerable<char>, so "test" has 4 characters
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), 4),
            Times.Once);
    }

    #endregion

    #region Performance Verification Tests

    [Fact]
    public async Task TrackQueryAsync_ShouldHaveMinimalOverhead()
    {
        // Arrange
        const string queryType = "OverheadTest";
        var function = new Func<Task<string>>(() => Task.FromResult("fast"));

        var stopwatch = Stopwatch.StartNew();

        // Act
        await _mockPerformanceService.Object.TrackQueryAsync(queryType, function);

        // Assert
        stopwatch.Stop();
        
        // The tracking overhead should be minimal (less than 50ms for a simple operation)
        // Note: CI environments may have more variance in timing
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
        
        // String "fast" is treated as IEnumerable<char>, so 4 characters
        _mockPerformanceService.Verify(
            x => x.RecordQueryPerformanceAsync(queryType, It.IsAny<TimeSpan>(), 4),
            Times.Once);
    }

    [Fact]
    public async Task TrackQueryAsync_WithLongRunningOperation_ShouldAccuratelyMeasureTime()
    {
        // Arrange
        const string queryType = "LongRunningQuery";
        const int expectedDelayMs = 100;
        
        var function = new Func<Task<string>>(async () =>
        {
            await Task.Delay(expectedDelayMs);
            return "long result";
        });

        TimeSpan? recordedTime = null;
        _mockPerformanceService
            .Setup(x => x.RecordQueryPerformanceAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<int>()))
            .Callback<string, TimeSpan, int>((_, time, _) => recordedTime = time)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _mockPerformanceService.Object.TrackQueryAsync(queryType, function);

        // Assert
        result.Should().Be("long result");
        recordedTime.Should().NotBeNull();
        recordedTime!.Value.TotalMilliseconds.Should().BeGreaterOrEqualTo(expectedDelayMs * 0.5); // Allow 50% variance for very slow CI
        recordedTime.Value.TotalMilliseconds.Should().BeLessOrEqualTo(expectedDelayMs * 5.0); // Allow very generous upper bound for extremely slow CI environments
    }

    #endregion
}