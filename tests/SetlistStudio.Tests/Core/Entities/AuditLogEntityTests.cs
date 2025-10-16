using FluentAssertions;
using Xunit;
using SetlistStudio.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SetlistStudio.Tests.Core.Entities;

/// <summary>
/// Unit tests for the AuditLog entity.
/// Tests validation, constraints, data integrity, and serialization behavior.
/// </summary>
public class AuditLogEntityTests
{
    [Fact]
    public void AuditLog_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange & Act
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "CREATE_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            NewValues = JsonSerializer.Serialize(new { Title = "Sweet Child O' Mine", Artist = "Guns N' Roses" }),
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            SessionId = "session-abc123",
            CorrelationId = 1.ToString(),
            Timestamp = DateTime.UtcNow
        };

        // Assert
        auditLog.Should().NotBeNull();
        auditLog.Id.Should().Be(1);
        auditLog.Action.Should().Be("CREATE_SONG");
        auditLog.EntityType.Should().Be("Songs");
        auditLog.EntityId.Should().Be("song-123");
        auditLog.UserId.Should().Be("user-456");
        auditLog.NewValues.Should().NotBeNullOrEmpty();
        auditLog.IpAddress.Should().Be("192.168.1.100");
        auditLog.UserAgent.Should().NotBeNullOrEmpty();
        auditLog.SessionId.Should().Be("session-abc123");
        auditLog.CorrelationId.Should().NotBeNullOrEmpty();
        auditLog.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AuditLog_WithMinimalRequiredData_ShouldBeValid()
    {
        // Arrange & Act
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "DELETE_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            Timestamp = DateTime.UtcNow
            // Changes, IpAddress, UserAgent, SessionId, CorrelationId are optional
        };

        // Assert
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AuditLog_WithInvalidAction_ShouldFailValidation(string? action)
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = action!,
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("Action");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AuditLog_WithInvalidEntityType_ShouldFailValidation(string? EntityType)
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "CREATE_SONG",
            EntityType = EntityType!,
            EntityId = "song-123",
            UserId = "user-456",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("EntityType");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AuditLog_WithInvalidEntityId_ShouldFailValidation(string? EntityId)
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "CREATE_SONG",
            EntityType = "Songs",
            EntityId = EntityId!,
            UserId = "user-456",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("EntityId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AuditLog_WithInvalidUserId_ShouldFailValidation(string? userId)
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "CREATE_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = userId!,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("UserId");
    }

    [Fact]
    public void AuditLog_WithTooLongAction_ShouldFailValidation()
    {
        // Arrange - Action longer than 100 characters
        var longAction = new string('A', 101);
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = longAction,
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("Action");
    }

    [Fact]
    public void AuditLog_WithTooLongEntityType_ShouldFailValidation()
    {
        // Arrange - EntityType longer than 100 characters
        var longEntityType = new string('T', 101);
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "CREATE_SONG",
            EntityType = longEntityType,
            EntityId = "song-123",
            UserId = "user-456",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("EntityType");
    }

    [Fact]
    public void AuditLog_WithTooLongEntityId_ShouldFailValidation()
    {
        // Arrange - EntityId longer than 255 characters
        var longEntityId = new string('R', 256);
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "CREATE_SONG",
            EntityType = "Songs",
            EntityId = longEntityId,
            UserId = "user-456",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("EntityId");
    }

    [Fact]
    public void AuditLog_WithTooLongIpAddress_ShouldFailValidation()
    {
        // Arrange - IpAddress longer than 45 characters (max for IPv6)
        var longIpAddress = new string('1', 46);
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "CREATE_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            IpAddress = longIpAddress,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("IpAddress");
    }

    [Fact]
    public void AuditLog_WithTooLongUserAgent_ShouldFailValidation()
    {
        // Arrange - UserAgent longer than 500 characters
        var longUserAgent = new string('U', 501);
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "CREATE_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            UserAgent = longUserAgent,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("UserAgent");
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("203.0.113.195")]
    [InlineData("2001:db8:85a3:8d3:1319:8a2e:370:7348")] // IPv6
    [InlineData("::1")] // IPv6 localhost
    public void AuditLog_WithValidIpAddresses_ShouldBeValid(string ipAddress)
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "CREATE_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void AuditLog_WithComplexChangesObject_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var originalNewValues = new
        {
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            BPM = 72,
            Key = "Bb",
            Metadata = new
            {
                Genre = "Rock",
                Duration = "5:55",
                Tags = new[] { "classic", "opera", "rock" },
                IsActive = true,
                Rating = 4.8
            },
            UpdatedFields = new[] { "Title", "BPM", "Metadata.Genre" }
        };

        var serializedNewValues = JsonSerializer.Serialize(originalNewValues, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "UPDATE_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            NewValues = serializedNewValues,
            Timestamp = DateTime.UtcNow
        };

        // Act - Deserialize the changes back
        var deserializedNewValues = JsonSerializer.Deserialize<JsonElement>(auditLog.OldValues);

        // Assert
        deserializedNewValues.GetProperty("title").GetString().Should().Be("Bohemian Rhapsody");
        deserializedNewValues.GetProperty("bpm").GetInt32().Should().Be(72);
        deserializedNewValues.GetProperty("metadata").GetProperty("genre").GetString().Should().Be("Rock");
        deserializedNewValues.GetProperty("metadata").GetProperty("tags")[0].GetString().Should().Be("classic");
        deserializedNewValues.GetProperty("metadata").GetProperty("isActive").GetBoolean().Should().BeTrue();
        deserializedNewValues.GetProperty("metadata").GetProperty("rating").GetDouble().Should().Be(4.8);
    }

    [Fact]
    public void AuditLog_WithNullChanges_ShouldBeValid()
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "DELETE_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            NewValues = null, // Null changes for delete operations
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
        auditLog.OldValues.Should().BeNull();
    }

    [Fact]
    public void AuditLog_WithEmptyChanges_ShouldBeValid()
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "READ_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            NewValues = "{}", // Empty JSON object
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
        auditLog.OldValues.Should().Be("{}");
    }

    [Theory]
    [InlineData("CREATE_SONG")]
    [InlineData("UPDATE_SONG")]
    [InlineData("DELETE_SONG")]
    [InlineData("READ_SONG")]
    [InlineData("ARCHIVE_SONG")]
    [InlineData("RESTORE_SONG")]
    [InlineData("CREATE_SETLIST")]
    [InlineData("UPDATE_SETLIST")]
    [InlineData("DELETE_SETLIST")]
    [InlineData("ADD_SONG_TO_SETLIST")]
    [InlineData("REMOVE_SONG_FROM_SETLIST")]
    [InlineData("REORDER_SETLIST")]
    public void AuditLog_WithCommonActions_ShouldBeValid(string action)
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = action,
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void AuditLog_EqualityComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var id = 1;
        var timestamp = DateTime.UtcNow;

        var auditLog1 = new AuditLog
        {
            Id = id,
            Action = "CREATE_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            Timestamp = timestamp
        };

        var auditLog2 = new AuditLog
        {
            Id = id,
            Action = "CREATE_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            Timestamp = timestamp
        };

        var auditLog3 = new AuditLog
        {
            Id = 1,
            Action = "CREATE_SONG",
            EntityType = "Songs",
            EntityId = "song-123",
            UserId = "user-456",
            Timestamp = timestamp
        };

        // Act & Assert
        auditLog1.Id.Should().Be(auditLog2.Id);
        auditLog1.Id.Should().NotBe(auditLog3.Id);
    }

    [Fact]
    public void AuditLog_WithSecuritySensitiveData_ShouldNotLeakInformation()
    {
        // Arrange - Simulate audit log for password change (should not store actual password)
        var sensitiveData = new
        {
            Action = "PASSWORD_CHANGED",
            // Never store actual passwords, tokens, or sensitive data
            HashedPasswordChanged = true,
            SecurityStampUpdated = true,
            Timestamp = DateTime.UtcNow
        };

        var auditLog = new AuditLog
        {
            Id = 1,
            Action = "UPDATE_USER_SECURITY",
            EntityType = "AspNetUsers",
            EntityId = "user-123",
            UserId = "user-456",
            NewValues = JsonSerializer.Serialize(sensitiveData),
            Timestamp = DateTime.UtcNow
        };

        // Act
        var deserializedNewValues = JsonSerializer.Deserialize<JsonElement>(auditLog.OldValues);

        // Assert - Should not contain sensitive data
        auditLog.OldValues.Should().NotContain("password");
        auditLog.OldValues.Should().NotContain("token");
        auditLog.OldValues.Should().NotContain("secret");
        deserializedNewValues.GetProperty("Action").GetString().Should().Be("PASSWORD_CHANGED");
        deserializedNewValues.GetProperty("HashedPasswordChanged").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void AuditLog_WithMaxLengthFields_ShouldBeValid()
    {
        // Arrange - Test maximum allowed lengths
        var auditLog = new AuditLog
        {
            Id = 1,
            Action = new string('A', 100), // Max length
            EntityType = new string('T', 100), // Max length
            EntityId = new string('R', 255), // Max length
            UserId = new string('U', 450), // Max length for AspNetUser ID
            IpAddress = "2001:0db8:85a3:0000:0000:8a2e:0370:7334", // Max IPv6 length
            UserAgent = new string('B', 500), // Max length
            SessionId = new string('S', 100), // Max length
            CorrelationId = 1.ToString(),
            Timestamp = DateTime.UtcNow
        };

        // Act
        var validationContext = new ValidationContext(auditLog);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(auditLog, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }
}
