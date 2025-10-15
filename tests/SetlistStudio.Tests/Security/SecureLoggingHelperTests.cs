using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Security;
using Xunit;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Tests for SecureLoggingHelper utility class to ensure proper sanitization of sensitive data.
/// Validates that passwords, tokens, and other sensitive information are properly redacted from logs.
/// </summary>
public class SecureLoggingHelperTests
{
    /// <summary>
    /// Tests that password patterns in messages are properly sanitized.
    /// </summary>
    [Theory]
    [InlineData("User login with password: secret123", "User login with password: [REDACTED]")]
    [InlineData("Password=mypassword", "Password=[REDACTED]")]
    [InlineData("password: \"test123\"", "password: \"[REDACTED]")]
    [InlineData("PASSWORD: test", "PASSWORD: [REDACTED]")]
    public void SanitizeMessage_ShouldRedactPasswordPatterns(string input, string expected)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeMessage(input);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests that token patterns in messages are properly sanitized.
    /// </summary>
    [Theory]
    [InlineData("Authorization token: abc123def456")]
    [InlineData("Token=jwt.token.here")]
    [InlineData("api_token: \"bearer xyz\"")]
    [InlineData("Bearer eyJ0eXAiOiJKV1QiLCJhbGc")]
    public void SanitizeMessage_ShouldRedactTokenPatterns(string input)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeMessage(input);

        // Assert
        result.Should().Contain("[REDACTED]");
    }

    /// <summary>
    /// Tests that API key patterns in messages are properly sanitized.
    /// </summary>
    [Theory]
    [InlineData("API_KEY: sk_test_1234567890")]
    [InlineData("apikey=abcdef123456")]
    [InlineData("client_secret: very_secret_key")]
    public void SanitizeMessage_ShouldRedactApiKeyPatterns(string input)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeMessage(input);

        // Assert
        result.Should().Contain("[REDACTED]");
    }

    /// <summary>
    /// Tests that email addresses are detected and masked in messages.
    /// </summary>
    [Theory]
    [InlineData("User email: user@example.com", true)]
    [InlineData("Contact support at hello@setliststudio.com", true)]
    [InlineData("No email in this message", false)]
    [InlineData("Invalid email format: user@", false)]
    public void SanitizeMessage_ShouldDetectEmailAddresses(string input, bool shouldContainRedacted)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeMessage(input);

        // Assert
        if (shouldContainRedacted)
        {
            result.Should().Contain("[REDACTED]");
        }
        else
        {
            result.Should().NotContain("[REDACTED]");
        }
    }

    /// <summary>
    /// Tests that credit card patterns are detected and sanitized.
    /// </summary>
    [Theory]
    [InlineData("Card number: 1234 5678 9012 3456", true)]
    [InlineData("Credit card: 1234-5678-9012-3456", true)]
    [InlineData("Card: 1234567890123456", true)]
    [InlineData("Short number: 123456", false)]
    public void SanitizeMessage_ShouldDetectCreditCardPatterns(string input, bool shouldContainRedacted)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeMessage(input);

        // Assert
        if (shouldContainRedacted)
        {
            result.Should().Contain("[REDACTED]");
        }
        else
        {
            result.Should().NotContain("[REDACTED]");
        }
    }

    /// <summary>
    /// Tests that SSN patterns are detected and sanitized.
    /// </summary>
    [Theory]
    [InlineData("SSN: 123-45-6789", true)]
    [InlineData("Social Security Number: 987-65-4321", true)]
    [InlineData("Random number: 123-45", false)]
    public void SanitizeMessage_ShouldDetectSsnPatterns(string input, bool shouldContainRedacted)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeMessage(input);

        // Assert
        if (shouldContainRedacted)
        {
            result.Should().Contain("[REDACTED]");
        }
        else
        {
            result.Should().NotContain("[REDACTED]");
        }
    }

    /// <summary>
    /// Tests that null or empty messages are handled gracefully.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeMessage_ShouldHandleEmptyInput(string input)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeMessage(input);

        // Assert
        result.Should().Be(input);
    }

    /// <summary>
    /// Tests that null messages are handled gracefully.
    /// </summary>
    [Fact]
    public void SanitizeMessage_ShouldHandleNullInput()
    {
        // Act
        var result = SecureLoggingHelper.SanitizeMessage(null!);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that non-sensitive messages are left unchanged.
    /// </summary>
    [Theory]
    [InlineData("User created new song: Bohemian Rhapsody")]
    [InlineData("Song updated with BPM: 72 and key: Bb")]
    [InlineData("Setlist 'Wedding Reception' created successfully")]
    public void SanitizeMessage_ShouldLeaveNonSensitiveMessagesUnchanged(string input)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeMessage(input);

        // Assert
        result.Should().Be(input);
    }

    /// <summary>
    /// Tests sanitization of objects with sensitive properties.
    /// </summary>
    [Fact]
    public void SanitizeObject_ShouldRedactSensitiveProperties()
    {
        // Arrange
        var testObject = new
        {
            Username = "testuser",
            Password = "secret123",
            Token = "jwt.token.here",
            ApiKey = "sk_test_123",
            Email = "user@example.com",
            NonSensitive = "This is safe"
        };

        // Act
        var result = SecureLoggingHelper.SanitizeObject(testObject);

        // Assert
        result["Username"].Should().Be("testuser");
        result["Password"].Should().Be("[REDACTED]");
        result["Token"].Should().Be("[REDACTED]");
        result["ApiKey"].Should().Be("[REDACTED]");
        result["Email"].Should().Be("[REDACTED]@example.com"); // Email should preserve domain
        result["NonSensitive"].Should().Be("This is safe");
    }

    /// <summary>
    /// Tests that null objects are handled gracefully.
    /// </summary>
    [Fact]
    public void SanitizeObject_ShouldHandleNullObject()
    {
        // Act
        var result = SecureLoggingHelper.SanitizeObject(null!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that complex nested objects are handled safely.
    /// </summary>
    [Fact]
    public void SanitizeObject_ShouldHandleComplexObjects()
    {
        // Arrange
        var testObject = new
        {
            SimpleProperty = "test",
            DateProperty = DateTime.UtcNow,
            NumberProperty = 42,
            ComplexProperty = new { NestedPassword = "secret" },
            NullProperty = (string?)null
        };

        // Act
        var result = SecureLoggingHelper.SanitizeObject(testObject);

        // Assert
        result["SimpleProperty"].Should().Be("test");
        result["DateProperty"].Should().Be(testObject.DateProperty);
        result["NumberProperty"].Should().Be(42);
        result["ComplexProperty"].Should().Match(x => x.ToString()!.StartsWith("[<>f__AnonymousType") && x.ToString()!.EndsWith("`1]")); // Complex objects show anonymous type name
        result["NullProperty"].Should().BeNull();
    }

    /// <summary>
    /// Tests field name sensitivity detection.
    /// </summary>
    [Theory]
    [InlineData("password", true)]
    [InlineData("Password", true)]
    [InlineData("USER_PASSWORD", true)]
    [InlineData("token", true)]
    [InlineData("authToken", true)]
    [InlineData("apikey", true)]
    [InlineData("api_key", true)]
    [InlineData("secret", true)]
    [InlineData("clientSecret", true)]
    [InlineData("authorization", true)]
    [InlineData("username", false)]
    [InlineData("email", false)]
    [InlineData("name", false)]
    [InlineData("title", false)]
    public void IsSensitiveField_ShouldDetectSensitiveFields(string fieldName, bool expectedSensitive)
    {
        // Act
        var result = SecureLoggingHelper.IsSensitiveField(fieldName);

        // Assert
        result.Should().Be(expectedSensitive);
    }

    /// <summary>
    /// Tests creation of secure log entries with proper sanitization.
    /// </summary>
    [Fact]
    public void CreateSecureLogEntry_ShouldCreateProperlyStructuredEntry()
    {
        // Arrange
        var action = "Login Attempt";
        var userId = "user123";
        var resourceType = "Authentication";
        var resourceId = "login-session-1";
        var additionalData = new { Password = "secret", Username = "testuser" };

        // Act
        var result = SecureLoggingHelper.CreateSecureLogEntry(action, userId, resourceType, resourceId, additionalData);

        // Assert
        result.Should().ContainKey("Action");
        result.Should().ContainKey("UserId");
        result.Should().ContainKey("ResourceType");
        result.Should().ContainKey("ResourceId");
        result.Should().ContainKey("Timestamp");
        result.Should().ContainKey("CorrelationId");
        result.Should().ContainKey("AdditionalData");

        result["Action"].Should().Be(action);
        result["UserId"].Should().Be(userId);
        result["ResourceType"].Should().Be(resourceType);
        result["ResourceId"].Should().Be(resourceId);
        result["Timestamp"].Should().BeOfType<DateTimeOffset>();
        result["CorrelationId"].Should().BeOfType<string>();

        var sanitizedData = result["AdditionalData"] as Dictionary<string, object?>;
        sanitizedData.Should().NotBeNull();
        sanitizedData!["Password"].Should().Be("[REDACTED]");
        sanitizedData["Username"].Should().Be("testuser");
    }

    /// <summary>
    /// Tests user ID sanitization for logging (email masking).
    /// </summary>
    [Theory]
    [InlineData("user@example.com", "user@[DOMAIN]")]
    [InlineData("test.user@setliststudio.com", "test.user@[DOMAIN]")]
    [InlineData("user123", "user123")]
    [InlineData("guid-like-id-12345", "guid-like-id-12345")]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void CreateSecureLogEntry_ShouldSanitizeUserIdProperly(string? input, string? expected)
    {
        // Act
        var result = SecureLoggingHelper.CreateSecureLogEntry("Test", input, "TestResource", null);

        // Assert
        result["UserId"].Should().Be(expected);
    }

    /// <summary>
    /// Tests that correlation IDs are unique for each log entry.
    /// </summary>
    [Fact]
    public void CreateSecureLogEntry_ShouldGenerateUniqueCorrelationIds()
    {
        // Act
        var entry1 = SecureLoggingHelper.CreateSecureLogEntry("Test1", "user1", "Resource", "1");
        var entry2 = SecureLoggingHelper.CreateSecureLogEntry("Test2", "user2", "Resource", "2");

        // Assert
        entry1["CorrelationId"].Should().NotBe(entry2["CorrelationId"]);
        entry1["CorrelationId"].Should().BeOfType<string>();
        entry2["CorrelationId"].Should().BeOfType<string>();
    }

    /// <summary>
    /// Tests handling of edge cases in sanitization.
    /// Even edge cases with minimal content are redacted for security.
    /// </summary>
    [Theory]
    [InlineData("password:", "password:")]  // No value after colon
    [InlineData("password=", "password=")]  // No value after equals
    [InlineData("password: ", "password:[REDACTED]")]  // Space is treated as content
    [InlineData("password=\"\"", "password=\"[REDACTED]")]  // Empty quotes are redacted
    [InlineData("Empty password: ''", "Empty password: '[REDACTED]")]  // Empty single quotes are redacted
    public void SanitizeMessage_ShouldHandleEdgeCasesInPasswords(string input, string expected)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeMessage(input);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests that multiple sensitive patterns in one message are all redacted.
    /// </summary>
    [Fact]
    public void SanitizeMessage_ShouldRedactMultipleSensitivePatternsInOneMessage()
    {
        // Arrange
        var input = "Login failed for user@example.com with password: secret123 and token: jwt.abc.def";

        // Act
        var result = SecureLoggingHelper.SanitizeMessage(input);

        // Assert
        var redactedCount = result.Split("[REDACTED]").Length - 1;
        redactedCount.Should().BeGreaterThan(1, "Multiple sensitive patterns should be redacted");
    }

    /// <summary>
    /// Tests performance with large messages containing multiple patterns.
    /// </summary>
    [Fact]
    public void SanitizeMessage_ShouldPerformWellWithLargeMessages()
    {
        // Arrange
        var largeMessage = string.Join(" ", Enumerable.Range(1, 1000).Select(i => 
            $"User {i} attempted login with password: secret{i} and token: jwt.token.{i}"));

        // Act & Assert (should complete within reasonable time)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = SecureLoggingHelper.SanitizeMessage(largeMessage);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Sanitization should be performant");
        result.Should().Contain("[REDACTED]");
    }
}