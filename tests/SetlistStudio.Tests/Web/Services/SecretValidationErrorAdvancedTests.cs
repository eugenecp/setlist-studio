using FluentAssertions;
using SetlistStudio.Web.Services;
using Xunit;

namespace SetlistStudio.Tests.Web.Services;

/// <summary>
/// Advanced tests for SecretValidationError to achieve comprehensive branch coverage
/// targeting the constructor parameter validation and all edge cases.
/// </summary>
public class SecretValidationErrorAdvancedTests
{
    [Fact]
    public void Constructor_ShouldCreateValidInstance_WhenAllParametersValid()
    {
        // Arrange
        var secretKey = "Authentication:Google:ClientId";
        var description = "Google OAuth Client ID";
        var issue = SecretValidationIssue.Missing;
        var details = "Configuration value is missing or empty";

        // Act
        var error = new SecretValidationError(secretKey, description, issue, details);

        // Assert
        error.SecretKey.Should().Be(secretKey);
        error.Description.Should().Be(description);
        error.Issue.Should().Be(issue);
        error.Details.Should().Be(details);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSecretKeyIsNull()
    {
        // Arrange
        string? secretKey = null;
        var description = "Google OAuth Client ID";
        var issue = SecretValidationIssue.Missing;
        var details = "Configuration value is missing or empty";

        // Act
        var act = () => new SecretValidationError(secretKey!, description, issue, details);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("secretKey");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenDescriptionIsNull()
    {
        // Arrange
        var secretKey = "Authentication:Google:ClientId";
        string? description = null;
        var issue = SecretValidationIssue.Placeholder;
        var details = "Using placeholder value YOUR_GOOGLE_CLIENT_ID";

        // Act
        var act = () => new SecretValidationError(secretKey, description!, issue, details);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("description");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenDetailsIsNull()
    {
        // Arrange
        var secretKey = "Authentication:Microsoft:ClientSecret";
        var description = "Microsoft OAuth Client Secret";
        var issue = SecretValidationIssue.TooShort;
        string? details = null;

        // Act
        var act = () => new SecretValidationError(secretKey, description, issue, details!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("details");
    }

    [Theory]
    [InlineData(SecretValidationIssue.Missing)]
    [InlineData(SecretValidationIssue.Placeholder)]
    [InlineData(SecretValidationIssue.InvalidFormat)]
    [InlineData(SecretValidationIssue.TooShort)]
    [InlineData(SecretValidationIssue.Insecure)]
    public void Constructor_ShouldHandleAllValidationIssueTypes(SecretValidationIssue issueType)
    {
        // Arrange
        var secretKey = "Authentication:Facebook:AppSecret";
        var description = "Facebook App Secret";
        var details = $"Validation failed with issue type: {issueType}";

        // Act
        var error = new SecretValidationError(secretKey, description, issueType, details);

        // Assert
        error.Issue.Should().Be(issueType);
        error.SecretKey.Should().Be(secretKey);
        error.Description.Should().Be(description);
        error.Details.Should().Be(details);
    }

    [Fact]
    public void Constructor_ShouldHandleEmptyStringParameters_WithoutThrowingNull()
    {
        // Arrange
        var secretKey = "";
        var description = "";
        var issue = SecretValidationIssue.InvalidFormat;
        var details = "";

        // Act
        var error = new SecretValidationError(secretKey, description, issue, details);

        // Assert
        error.SecretKey.Should().Be("");
        error.Description.Should().Be("");
        error.Details.Should().Be("");
        error.Issue.Should().Be(issue);
    }

    [Fact]
    public void Constructor_ShouldHandleWhitespaceStringParameters()
    {
        // Arrange
        var secretKey = "   ";
        var description = "\t\n";
        var issue = SecretValidationIssue.Insecure;
        var details = "  \r\n  ";

        // Act
        var error = new SecretValidationError(secretKey, description, issue, details);

        // Assert
        error.SecretKey.Should().Be("   ");
        error.Description.Should().Be("\t\n");
        error.Details.Should().Be("  \r\n  ");
        error.Issue.Should().Be(issue);
    }

    [Fact]
    public void Constructor_ShouldHandleLongStringParameters()
    {
        // Arrange
        var secretKey = new string('a', 1000);
        var description = new string('b', 2000);
        var issue = SecretValidationIssue.TooShort; // Ironically testing TooShort with long strings
        var details = new string('c', 3000);

        // Act
        var error = new SecretValidationError(secretKey, description, issue, details);

        // Assert
        error.SecretKey.Should().Be(secretKey);
        error.Description.Should().Be(description);
        error.Details.Should().Be(details);
        error.Issue.Should().Be(issue);
    }

    [Fact]
    public void Constructor_ShouldHandleSpecialCharactersInParameters()
    {
        // Arrange
        var secretKey = "Auth:Google:ClientId<>&\"'";
        var description = "Special chars: 你好 🌟 ñ ü";
        var issue = SecretValidationIssue.InvalidFormat;
        var details = "Contains: \r\n\t\\\"'<>&@#$%^*(){}[]|";

        // Act
        var error = new SecretValidationError(secretKey, description, issue, details);

        // Assert
        error.SecretKey.Should().Be(secretKey);
        error.Description.Should().Be(description);
        error.Details.Should().Be(details);
        error.Issue.Should().Be(issue);
    }

    [Fact]
    public void Properties_ShouldBeReadOnly_AfterConstruction()
    {
        // Arrange
        var secretKey = "Authentication:Google:ClientSecret";
        var description = "Google OAuth Client Secret";
        var issue = SecretValidationIssue.Missing;
        var details = "Secret not configured in environment";

        // Act
        var error = new SecretValidationError(secretKey, description, issue, details);

        // Assert - Properties should be get-only
        error.SecretKey.Should().Be(secretKey);
        error.Description.Should().Be(description);
        error.Issue.Should().Be(issue);
        error.Details.Should().Be(details);

        // Verify properties are immutable by checking they can't be set
        // (This is enforced at compile time with get-only properties)
        var secretKeyProperty = typeof(SecretValidationError).GetProperty(nameof(SecretValidationError.SecretKey));
        var descriptionProperty = typeof(SecretValidationError).GetProperty(nameof(SecretValidationError.Description));
        var issueProperty = typeof(SecretValidationError).GetProperty(nameof(SecretValidationError.Issue));
        var detailsProperty = typeof(SecretValidationError).GetProperty(nameof(SecretValidationError.Details));

        secretKeyProperty!.CanWrite.Should().BeFalse();
        descriptionProperty!.CanWrite.Should().BeFalse();
        issueProperty!.CanWrite.Should().BeFalse();
        detailsProperty!.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldValidateParametersInCorrectOrder()
    {
        // Test that secretKey is validated first
        var act1 = () => new SecretValidationError(null!, "description", SecretValidationIssue.Missing, "details");
        act1.Should().Throw<ArgumentNullException>().WithParameterName("secretKey");

        // Test that description is validated second (when secretKey is valid)
        var act2 = () => new SecretValidationError("key", null!, SecretValidationIssue.Missing, "details");
        act2.Should().Throw<ArgumentNullException>().WithParameterName("description");

        // Test that details is validated last (when secretKey and description are valid)
        var act3 = () => new SecretValidationError("key", "description", SecretValidationIssue.Missing, null!);
        act3.Should().Throw<ArgumentNullException>().WithParameterName("details");
    }
}