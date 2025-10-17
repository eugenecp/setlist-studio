using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SetlistStudio.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive unit and integration tests for the AuditLogService.
/// Tests HTTP context enhancement, IP detection, audit operations, and data integrity.
/// </summary>
public class AuditLogServiceTests : IDisposable
{
    private readonly DbContextOptions<SetlistStudioDbContext> _dbOptions;
    private readonly SetlistStudioDbContext _context;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<AuditLogService>> _mockLogger;
    private readonly AuditLogService _service;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpRequest> _mockHttpRequest;
    private readonly Mock<IHeaderDictionary> _mockHeaders;

    public AuditLogServiceTests()
    {
        // Setup in-memory database
        _dbOptions = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new SetlistStudioDbContext(_dbOptions);

        // Setup mocks
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<AuditLogService>>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpRequest = new Mock<HttpRequest>();
        _mockHeaders = new Mock<IHeaderDictionary>();

        // Setup HTTP context mocks
        _mockHttpContext.Setup(x => x.Request).Returns(_mockHttpRequest.Object);
        _mockHttpRequest.Setup(x => x.Headers).Returns(_mockHeaders.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);

        _service = new AuditLogService(_context, _mockHttpContextAccessor.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task LogAuditAsync_WithValidData_ShouldCreateAuditLogEntry()
    {
        // Arrange
        var action = "CREATE_SONG";
        var EntityType = "Songs";
        var EntityId = "song-123";
        var userId = "user-456";
        var changes = new { Title = "Sweet Child O' Mine", Artist = "Guns N' Roses" };
        var correlationId = 1.ToString();

        SetupHttpContext("192.168.1.100", "Mozilla/5.0 Test Browser", userId);

        // Act
        await _service.LogAuditAsync(action, EntityType, EntityId, userId, changes, correlationId);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(action);
        auditLog.EntityType.Should().Be(EntityType);
        auditLog.EntityId.Should().Be(EntityId);
        auditLog.UserId.Should().Be(userId);
        auditLog.IpAddress.Should().Be("192.168.1.100");
        auditLog.UserAgent.Should().Be("Mozilla/5.0 Test Browser");
        auditLog.CorrelationId.Should().Be(correlationId);
        auditLog.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        
        var deserializedChanges = JsonSerializer.Deserialize<JsonElement>(auditLog.NewValues!);
        deserializedChanges.GetProperty("title").GetString().Should().Be("Sweet Child O' Mine");
        deserializedChanges.GetProperty("artist").GetString().Should().Be("Guns N' Roses");
    }

    [Fact]
    public async Task LogAuditAsync_WithNullChanges_ShouldCreateAuditLogWithNullChanges()
    {
        // Arrange
        var action = "DELETE_SONG";
        var EntityType = "Songs";
        var EntityId = "song-123";
        var userId = "user-456";
        object? changes = null;

        SetupHttpContext("192.168.1.100", "Mozilla/5.0 Test Browser", userId);

        // Act
        await _service.LogAuditAsync(action, EntityType, EntityId, userId, changes);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.OldValues.Should().BeNull();
    }

    [Fact]
    public async Task LogAuditAsync_WithoutHttpContext_ShouldCreateAuditLogWithNullHttpData()
    {
        // Arrange
        var action = "CREATE_SONG";
        var EntityType = "Songs";
        var EntityId = "song-123";
        var userId = "user-456";
        var changes = new { Title = "Billie Jean" };

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        await _service.LogAuditAsync(action, EntityType, EntityId, userId, changes);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.IpAddress.Should().BeNull();
        auditLog.UserAgent.Should().BeNull();
        auditLog.SessionId.Should().BeNull();
    }

    [Theory]
    [InlineData("X-Forwarded-For", "203.0.113.195, 70.41.3.18, 150.172.238.178")]
    [InlineData("X-Real-IP", "203.0.113.195")]
    [InlineData("CF-Connecting-IP", "203.0.113.195")]
    public async Task LogAuditAsync_WithProxyHeaders_ShouldExtractCorrectIP(string headerName, string headerValue)
    {
        // Arrange
        var action = "CREATE_SONG";
        var EntityType = "Songs";
        var EntityId = "song-123";
        var userId = "user-456";
        var changes = new { Title = "Take Five" };

        // Setup proxy headers
        var headers = new HeaderDictionary
        {
            { headerName, headerValue }
        };
        _mockHttpRequest.Setup(x => x.Headers).Returns(headers);
        _mockHttpContext.Setup(x => x.Connection.RemoteIpAddress)
            .Returns(System.Net.IPAddress.Parse("127.0.0.1")); // Local proxy

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        await _service.LogAuditAsync(action, EntityType, EntityId, userId, changes);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        
        // Should extract the first IP from X-Forwarded-For or the direct value from other headers
        var expectedIP = headerName == "X-Forwarded-For" ? "203.0.113.195" : "203.0.113.195";
        auditLog!.IpAddress.Should().Be(expectedIP);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithValidFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var userId = "user-456";
        var correlationId = 1.ToString();
        
        // Create test data
        var auditLogs = new[]
        {
            new AuditLog
            {
                Id = 1,
                Action = "CREATE_SONG",
                EntityType = "Songs",
                EntityId = "song-1",
                UserId = userId,
                NewValues = JsonSerializer.Serialize(new { Title = "Song 1" }),
                Timestamp = DateTime.UtcNow.AddHours(-1),
                CorrelationId = correlationId
            },
            new AuditLog
            {
                Id = 2,
                Action = "UPDATE_SONG",
                EntityType = "Songs",
                EntityId = "song-1",
                UserId = userId,
                NewValues = JsonSerializer.Serialize(new { Title = "Song 1 Updated" }),
                Timestamp = DateTime.UtcNow.AddMinutes(-30),
                CorrelationId = correlationId
            },
            new AuditLog
            {
                Id = 3,
                Action = "CREATE_SONG",
                EntityType = "Songs",
                EntityId = "song-2",
                UserId = "other-user",
                NewValues = JsonSerializer.Serialize(new { Title = "Other User Song" }),
                Timestamp = DateTime.UtcNow.AddMinutes(-15)
            }
        };

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAuditLogsAsync(
            userId: userId,
            action: "CREATE_SONG",
            tableName: "Songs",
            startDate: DateTime.UtcNow.AddHours(-2),
            endDate: DateTime.UtcNow,
            pageNumber: 1,
            pageSize: 10);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(1);
        result.First().Action.Should().Be("CREATE_SONG");
        result.First().UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetAuditLogsByRecordAsync_ShouldReturnRecordHistory()
    {
        // Arrange
        var EntityId = "song-123";
        var EntityType = "Songs";
        
        var auditLogs = new[]
        {
            new AuditLog
            {
                Id = 4,
                Action = "CREATE_SONG",
                EntityType = EntityType,
                EntityId = EntityId,
                UserId = "user-1",
                NewValues = JsonSerializer.Serialize(new { Title = "Original Title" }),
                Timestamp = DateTime.UtcNow.AddHours(-2)
            },
            new AuditLog
            {
                Id = 5,
                Action = "UPDATE_SONG",
                EntityType = EntityType,
                EntityId = EntityId,
                UserId = "user-2",
                NewValues = JsonSerializer.Serialize(new { Title = "Updated Title" }),
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        };

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAuditLogsByRecordAsync(EntityType, EntityId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(x => x.Timestamp);
        result.First().Action.Should().Be("UPDATE_SONG");
        result.Last().Action.Should().Be("CREATE_SONG");
    }

    [Fact]
    public async Task GetAuditLogsByCorrelationIdAsync_ShouldReturnRelatedOperations()
    {
        // Arrange
        var correlationId = 1.ToString();
        
        var auditLogs = new[]
        {
            new AuditLog
            {
                Id = 6,
                Action = "CREATE_SETLIST",
                EntityType = "Setlists",
                EntityId = "setlist-1",
                UserId = "user-1",
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow.AddMinutes(-5)
            },
            new AuditLog
            {
                Id = 7,
                Action = "ADD_SONG_TO_SETLIST",
                EntityType = "SetlistSongs",
                EntityId = "setlist-song-1",
                UserId = "user-1",
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow.AddMinutes(-4)
            },
            new AuditLog
            {
                Id = 8,
                Action = "ADD_SONG_TO_SETLIST",
                EntityType = "SetlistSongs",
                EntityId = "setlist-song-2",
                UserId = "user-1",
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow.AddMinutes(-3)
            }
        };

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAuditLogsByCorrelationIdAsync(correlationId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInAscendingOrder(x => x.Timestamp);
        result.All(x => x.CorrelationId == correlationId).Should().BeTrue();
    }

    [Fact]
    public async Task LogAuditAsync_WithComplexChanges_ShouldSerializeCorrectly()
    {
        // Arrange
        var action = "UPDATE_SONG";
        var EntityType = "Songs";
        var EntityId = "song-123";
        var userId = "user-456";
        var changes = new
        {
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            BPM = 72,
            Key = "Bb",
            Metadata = new
            {
                Genre = "Rock",
                Duration = "5:55",
                Tags = new[] { "classic", "opera", "rock" }
            }
        };

        SetupHttpContext("192.168.1.100", "Mozilla/5.0 Test Browser", userId);

        // Act
        await _service.LogAuditAsync(action, EntityType, EntityId, userId, changes);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        
        var deserializedChanges = JsonSerializer.Deserialize<JsonElement>(auditLog!.NewValues!);
        deserializedChanges.GetProperty("title").GetString().Should().Be("Bohemian Rhapsody");
        deserializedChanges.GetProperty("bpm").GetInt32().Should().Be(72);
        deserializedChanges.GetProperty("metadata").GetProperty("genre").GetString().Should().Be("Rock");
        deserializedChanges.GetProperty("metadata").GetProperty("tags")[0].GetString().Should().Be("classic");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task LogAuditAsync_WithInvalidAction_ShouldThrowArgumentException(string? action)
    {
        // Arrange
        var EntityType = "Songs";
        var EntityId = "song-123";
        var userId = "user-456";
        var changes = new { Title = "Test Song" };

        // Act & Assert
        var act = async () => await _service.LogAuditAsync(action!, EntityType, EntityId, userId, changes);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task LogAuditAsync_WithInvalidEntityType_ShouldThrowArgumentException(string? EntityType)
    {
        // Arrange
        var action = "CREATE_SONG";
        var EntityId = "song-123";
        var userId = "user-456";
        var changes = new { Title = "Test Song" };

        // Act & Assert
        var act = async () => await _service.LogAuditAsync(action, EntityType!, EntityId, userId, changes);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var userId = "user-456";
        var auditLogs = Enumerable.Range(1, 25).Select(i => new AuditLog
        {
            Id = i + 10, // Start from 11 to avoid conflicts with other tests
            Action = $"ACTION_{i}",
            EntityType = "Songs",
            EntityId = $"song-{i}",
            UserId = userId,
            Timestamp = DateTime.UtcNow.AddMinutes(-i)
        }).ToArray();

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();

        // Act - Get page 2 with 10 items per page
        var result = await _service.GetAuditLogsAsync(
            userId: userId,
            pageNumber: 2,
            pageSize: 10);

        // Assert
        result.Should().HaveCount(10);
        result.Should().BeInDescendingOrder(x => x.Timestamp);
    }

    [Fact]
    public async Task DeleteAuditLogsOlderThanAsync_ShouldRemoveOldEntries()
    {
        // Arrange
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        
        var auditLogs = new[]
        {
            new AuditLog
            {
                Id = 36,
                Action = "OLD_ACTION",
                EntityType = "Songs",
                EntityId = "song-1",
                UserId = "user-1",
                Timestamp = DateTime.UtcNow.AddDays(-45) // Older than cutoff
            },
            new AuditLog
            {
                Id = 37,
                Action = "RECENT_ACTION",
                EntityType = "Songs",
                EntityId = "song-2",
                UserId = "user-1",
                Timestamp = DateTime.UtcNow.AddDays(-15) // Newer than cutoff
            }
        };

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();

        // Act
        var deletedCount = await _service.DeleteAuditLogsOlderThanAsync(cutoffDate);

        // Assert
        deletedCount.Should().Be(1);
        var remainingLogs = await _context.AuditLogs.ToListAsync();
        remainingLogs.Should().HaveCount(1);
        remainingLogs.First().Action.Should().Be("RECENT_ACTION");
    }

    [Fact]
    public async Task LogAuditAsync_ShouldGenerateCorrelationIdWhenNotProvided()
    {
        // Arrange
        var action = "CREATE_SONG";
        var EntityType = "Songs";
        var EntityId = "song-123";
        var userId = "user-456";
        var changes = new { Title = "Test Song" };

        SetupHttpContext("192.168.1.100", "Mozilla/5.0 Test Browser", userId);

        // Act
        await _service.LogAuditAsync(action, EntityType, EntityId, userId, changes);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.CorrelationId.Should().NotBeNullOrEmpty();
        Guid.TryParse(auditLog.CorrelationId, out _).Should().BeTrue();
    }

    private void SetupHttpContext(string ipAddress, string userAgent, string userId)
    {
        _mockHttpContext.Setup(x => x.Connection.RemoteIpAddress)
            .Returns(System.Net.IPAddress.Parse(ipAddress));
        
        var headers = new HeaderDictionary
        {
            { "User-Agent", userAgent }
        };
        _mockHttpRequest.Setup(x => x.Headers).Returns(headers);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _mockHttpContext.Setup(x => x.User).Returns(user);

        _mockHttpContext.Setup(x => x.Session.Id).Returns($"session-{userId}");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
