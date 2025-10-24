using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Security;
using Xunit;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive tests for AuthorizationResult covering all scenarios
/// Tests successful and failed authorization results with detailed security context
/// </summary>
public class AuthorizationResultTests
{
    private const string TestUserId = "user123";
    private const string TestResourceType = "Song";
    private const string TestResourceId = "456";
    private const string TestAction = "Read";

    [Fact]
    public void Success_ShouldCreateAuthorizedResult_WithCorrectProperties()
    {
        // Act
        var result = AuthorizationResult.Success(TestUserId, TestResourceType, TestResourceId, TestAction);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.UserId.Should().Be(TestUserId);
        result.ResourceType.Should().Be(TestResourceType);
        result.ResourceId.Should().Be(TestResourceId);
        result.Action.Should().Be(TestAction);
        result.Reason.Should().Be("User owns the requested resource");
        result.SecurityContext.Should().NotBeNull().And.BeEmpty();
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void NotFound_ShouldCreateUnauthorizedResult_WithNotFoundReason()
    {
        // Act
        var result = AuthorizationResult.NotFound(TestUserId, TestResourceType, TestResourceId, TestAction);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.UserId.Should().Be(TestUserId);
        result.ResourceType.Should().Be(TestResourceType);
        result.ResourceId.Should().Be(TestResourceId);
        result.Action.Should().Be(TestAction);
        result.Reason.Should().Be("Requested resource does not exist or user does not have access");
        result.SecurityContext.Should().NotBeNull().And.BeEmpty();
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Forbidden_WithoutActualOwner_ShouldCreateUnauthorizedResult_WithGenericReason()
    {
        // Act
        var result = AuthorizationResult.Forbidden(TestUserId, TestResourceType, TestResourceId, TestAction);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.UserId.Should().Be(TestUserId);
        result.ResourceType.Should().Be(TestResourceType);
        result.ResourceId.Should().Be(TestResourceId);
        result.Action.Should().Be(TestAction);
        result.Reason.Should().Be("User does not own the requested resource");
        result.SecurityContext.Should().NotBeNull().And.BeEmpty();
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Forbidden_WithActualOwner_ShouldCreateUnauthorizedResult_WithDetailedReason()
    {
        // Arrange
        var actualOwnerId = "owner789";

        // Act
        var result = AuthorizationResult.Forbidden(TestUserId, TestResourceType, TestResourceId, TestAction, actualOwnerId);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.UserId.Should().Be(TestUserId);
        result.ResourceType.Should().Be(TestResourceType);
        result.ResourceId.Should().Be(TestResourceId);
        result.Action.Should().Be(TestAction);
        result.Reason.Should().Be($"Resource belongs to user {actualOwnerId}, not {TestUserId}");
        result.SecurityContext.Should().NotBeNull().And.ContainKey("ActualOwnerId").WhoseValue.Should().Be(actualOwnerId);
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void InvalidUser_ShouldCreateUnauthorizedResult_WithInvalidUserReason()
    {
        // Act
        var result = AuthorizationResult.InvalidUser(TestUserId, TestResourceType, TestResourceId, TestAction);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.UserId.Should().Be(TestUserId);
        result.ResourceType.Should().Be(TestResourceType);
        result.ResourceId.Should().Be(TestResourceId);
        result.Action.Should().Be(TestAction);
        result.Reason.Should().Be("Invalid or missing user ID");
        result.SecurityContext.Should().NotBeNull().And.BeEmpty();
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidUser_WithVariousInvalidUserIds_ShouldHandleCorrectly(string? invalidUserId)
    {
        // Act
        var result = AuthorizationResult.InvalidUser(invalidUserId ?? "null", TestResourceType, TestResourceId, TestAction);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.Reason.Should().Be("Invalid or missing user ID");
    }

    [Fact]
    public void SecurityContext_ShouldBeMutable_AndAllowCustomData()
    {
        // Arrange
        var result = AuthorizationResult.Success(TestUserId, TestResourceType, TestResourceId, TestAction);

        // Act
        result.SecurityContext["CustomKey"] = "CustomValue";
        result.SecurityContext["Timestamp"] = DateTime.UtcNow;
        result.SecurityContext["RequestId"] = Guid.NewGuid().ToString();

        // Assert
        result.SecurityContext.Should().HaveCount(3);
        result.SecurityContext["CustomKey"].Should().Be("CustomValue");
        result.SecurityContext.Should().ContainKey("Timestamp");
        result.SecurityContext.Should().ContainKey("RequestId");
    }

    [Theory]
    [InlineData("Read")]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Delete")]
    [InlineData("List")]
    public void AuthorizationResult_ShouldSupportAllActions(string action)
    {
        // Act
        var result = AuthorizationResult.Success(TestUserId, TestResourceType, TestResourceId, action);

        // Assert
        result.Action.Should().Be(action);
        result.IsAuthorized.Should().BeTrue();
    }

    [Theory]
    [InlineData("Song")]
    [InlineData("Setlist")]
    [InlineData("SetlistSong")]
    [InlineData("User")]
    public void AuthorizationResult_ShouldSupportAllResourceTypes(string resourceType)
    {
        // Act
        var result = AuthorizationResult.Success(TestUserId, resourceType, TestResourceId, TestAction);

        // Assert
        result.ResourceType.Should().Be(resourceType);
        result.IsAuthorized.Should().BeTrue();
    }
}