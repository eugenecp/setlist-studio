using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Security;
using Xunit;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive tests for ResourceAuthorizationHelper covering all helper methods
/// Tests user validation, resource ownership, bulk operations, and security logging
/// </summary>
public class ResourceAuthorizationHelperTests
{
    private readonly Mock<ILogger> _mockLogger;
    private const string ValidUserId = "user123";
    private const string ResourceId = "456";
    private const string OtherUserId = "otheruser789";

    public ResourceAuthorizationHelperTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    #region ValidateUserId Tests

    [Fact]
    public void ValidateUserId_WithValidUserId_ShouldReturnSuccess()
    {
        // Act
        var result = ResourceAuthorizationHelper.ValidateUserId(
            ValidUserId, 
            ResourceAuthorizationHelper.ResourceType.Song, 
            ResourceId, 
            ResourceAuthorizationHelper.ResourceAction.Read, 
            _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.UserId.Should().Be(ValidUserId);
        result.ResourceType.Should().Be("Song");
        result.ResourceId.Should().Be(ResourceId);
        result.Action.Should().Be("Read");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUserId_WithInvalidUserId_ShouldReturnFailureAndLog(string? invalidUserId)
    {
        // Act
        var result = ResourceAuthorizationHelper.ValidateUserId(
            invalidUserId, 
            ResourceAuthorizationHelper.ResourceType.Setlist, 
            ResourceId, 
            ResourceAuthorizationHelper.ResourceAction.Update, 
            _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Invalid or missing user ID");

        // Verify security logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security violation: Invalid user ID attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ValidateResourceOwnership Tests

    [Fact]
    public void ValidateResourceOwnership_WithMatchingUserIds_ShouldReturnSuccess()
    {
        // Act
        var result = ResourceAuthorizationHelper.ValidateResourceOwnership(
            ValidUserId,
            ValidUserId,
            ResourceAuthorizationHelper.ResourceType.Song,
            ResourceId,
            ResourceAuthorizationHelper.ResourceAction.Read,
            _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.UserId.Should().Be(ValidUserId);
        result.Reason.Should().Be("User owns the requested resource");

        // Verify success logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Authorization successful")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateResourceOwnership_WithNullResourceUserId_ShouldReturnNotFound()
    {
        // Act
        var result = ResourceAuthorizationHelper.ValidateResourceOwnership(
            null,
            ValidUserId,
            ResourceAuthorizationHelper.ResourceType.Setlist,
            ResourceId,
            ResourceAuthorizationHelper.ResourceAction.Update,
            _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Requested resource does not exist or user does not have access");

        // Verify failure logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SECURITY ALERT: Authorization failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateResourceOwnership_WithDifferentUserIds_ShouldReturnForbidden()
    {
        // Act
        var result = ResourceAuthorizationHelper.ValidateResourceOwnership(
            OtherUserId,
            ValidUserId,
            ResourceAuthorizationHelper.ResourceType.Song,
            ResourceId,
            ResourceAuthorizationHelper.ResourceAction.Delete,
            _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be($"Resource belongs to user {OtherUserId}, not {ValidUserId}");
        result.SecurityContext.Should().ContainKey("ActualOwnerId").WhoseValue.Should().Be(OtherUserId);

        // Verify security alert logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SECURITY ALERT: Authorization failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateResourceOwnership_WithInvalidRequestingUserId_ShouldReturnInvalidUser()
    {
        // Act
        var result = ResourceAuthorizationHelper.ValidateResourceOwnership(
            ValidUserId,
            "", // Invalid requesting user ID
            ResourceAuthorizationHelper.ResourceType.Setlist,
            ResourceId,
            ResourceAuthorizationHelper.ResourceAction.Create,
            _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Invalid or missing user ID");
    }

    #endregion

    #region ValidateBulkResourceOwnership Tests

    [Fact]
    public void ValidateBulkResourceOwnership_WithAllValidResources_ShouldReturnAllSuccesses()
    {
        // Arrange
        var resourceUserIds = new Dictionary<string, string?>
        {
            { "1", ValidUserId },
            { "2", ValidUserId },
            { "3", ValidUserId }
        };

        // Act
        var results = ResourceAuthorizationHelper.ValidateBulkResourceOwnership(
            resourceUserIds,
            ValidUserId,
            ResourceAuthorizationHelper.ResourceType.Song,
            ResourceAuthorizationHelper.ResourceAction.Read,
            _mockLogger.Object);

        // Assert
        results.Should().HaveCount(3);
        results.Values.Should().AllSatisfy(r => r.IsAuthorized.Should().BeTrue());

        // Verify bulk operation logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulk authorization successful")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateBulkResourceOwnership_WithMixedOwnership_ShouldReturnMixedResults()
    {
        // Arrange
        var resourceUserIds = new Dictionary<string, string?>
        {
            { "1", ValidUserId },      // Success
            { "2", OtherUserId },      // Forbidden
            { "3", null }              // Not found
        };

        // Act
        var results = ResourceAuthorizationHelper.ValidateBulkResourceOwnership(
            resourceUserIds,
            ValidUserId,
            ResourceAuthorizationHelper.ResourceType.Setlist,
            ResourceAuthorizationHelper.ResourceAction.Update,
            _mockLogger.Object);

        // Assert
        results.Should().HaveCount(3);
        results["1"].IsAuthorized.Should().BeTrue();
        results["2"].IsAuthorized.Should().BeFalse();
        results["2"].Reason.Should().Contain("Resource belongs to user");
        results["3"].IsAuthorized.Should().BeFalse();
        results["3"].Reason.Should().Be("Requested resource does not exist or user does not have access");

        // Verify mixed results logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulk authorization completed with") && v.ToString()!.Contains("failures")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region CreateSecurityContext Tests

    [Fact]
    public void CreateSecurityContext_WithBasicParameters_ShouldReturnCorrectContext()
    {
        // Act
        var context = ResourceAuthorizationHelper.CreateSecurityContext(
            ValidUserId,
            ResourceAuthorizationHelper.ResourceType.Song,
            ResourceId,
            ResourceAuthorizationHelper.ResourceAction.Read);

        // Assert
        context.Should().NotBeNull();
        context["UserId"].Should().Be(ValidUserId);
        context["ResourceType"].Should().Be("Song");
        context["ResourceId"].Should().Be(ResourceId);
        context["Action"].Should().Be("Read");
        context["SecurityCheck"].Should().Be("ResourceOwnership");
        context.Should().ContainKey("Timestamp");
    }

    [Fact]
    public void CreateSecurityContext_WithAdditionalContext_ShouldMergeCorrectly()
    {
        // Arrange
        var additionalContext = new Dictionary<string, object>
        {
            { "CustomKey", "CustomValue" },
            { "RequestId", "req123" }
        };

        // Act
        var context = ResourceAuthorizationHelper.CreateSecurityContext(
            ValidUserId,
            ResourceAuthorizationHelper.ResourceType.Setlist,
            ResourceId,
            ResourceAuthorizationHelper.ResourceAction.Create,
            additionalContext);

        // Assert
        context.Should().NotBeNull();
        context["UserId"].Should().Be(ValidUserId);
        context["CustomKey"].Should().Be("CustomValue");
        context["RequestId"].Should().Be("req123");
        context.Should().HaveCountGreaterThan(additionalContext.Count);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void LogAuthorizationSuccess_ShouldLogCorrectInformation()
    {
        // Arrange
        var result = AuthorizationResult.Success(ValidUserId, "Song", ResourceId, "Read");

        // Act
        ResourceAuthorizationHelper.LogAuthorizationSuccess(_mockLogger.Object, result);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Authorization successful") && 
                                              v.ToString()!.Contains(ValidUserId) &&
                                              v.ToString()!.Contains("Song")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogAuthorizationFailure_WithContext_ShouldLogSecurityAlert()
    {
        // Arrange
        var result = AuthorizationResult.Forbidden(ValidUserId, "Setlist", ResourceId, "Delete");
        var requestContext = new Dictionary<string, object> { { "IP", "192.168.1.1" } };

        // Act
        ResourceAuthorizationHelper.LogAuthorizationFailure(_mockLogger.Object, result, requestContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SECURITY ALERT") && 
                                              v.ToString()!.Contains("Authorization failed") &&
                                              v.ToString()!.Contains("IP=192.168.1.1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogSuspiciousActivity_ShouldLogSecurityThreat()
    {
        // Arrange
        var details = new Dictionary<string, object>
        {
            { "AttemptCount", 10 },
            { "TimeSpan", "1 minute" }
        };

        // Act
        ResourceAuthorizationHelper.LogSuspiciousActivity(_mockLogger.Object, ValidUserId, "Rapid failed access attempts", details);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SECURITY THREAT") && 
                                              v.ToString()!.Contains("Suspicious activity detected") &&
                                              v.ToString()!.Contains("AttemptCount=10")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Enum Coverage Tests

    [Theory]
    [InlineData(ResourceAuthorizationHelper.ResourceAction.Read)]
    [InlineData(ResourceAuthorizationHelper.ResourceAction.Create)]
    [InlineData(ResourceAuthorizationHelper.ResourceAction.Update)]
    [InlineData(ResourceAuthorizationHelper.ResourceAction.Delete)]
    [InlineData(ResourceAuthorizationHelper.ResourceAction.List)]
    public void ResourceAction_AllValues_ShouldBeSupported(ResourceAuthorizationHelper.ResourceAction action)
    {
        // Act
        var result = ResourceAuthorizationHelper.ValidateUserId(
            ValidUserId, 
            ResourceAuthorizationHelper.ResourceType.Song, 
            ResourceId, 
            action, 
            _mockLogger.Object);

        // Assert
        result.Action.Should().Be(action.ToString());
    }

    [Theory]
    [InlineData(ResourceAuthorizationHelper.ResourceType.Song)]
    [InlineData(ResourceAuthorizationHelper.ResourceType.Setlist)]
    [InlineData(ResourceAuthorizationHelper.ResourceType.SetlistSong)]
    public void ResourceType_AllValues_ShouldBeSupported(ResourceAuthorizationHelper.ResourceType resourceType)
    {
        // Act
        var result = ResourceAuthorizationHelper.ValidateUserId(
            ValidUserId, 
            resourceType, 
            ResourceId, 
            ResourceAuthorizationHelper.ResourceAction.Read, 
            _mockLogger.Object);

        // Assert
        result.ResourceType.Should().Be(resourceType.ToString());
    }

    #endregion
}