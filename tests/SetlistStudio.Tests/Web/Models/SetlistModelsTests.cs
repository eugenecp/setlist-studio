using FluentAssertions;
using SetlistStudio.Web.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace SetlistStudio.Tests.Web.Models;

/// <summary>
/// Tests for setlist request models validation and behavior
/// </summary>
public class SetlistModelsTests
{
    #region UpdateSetlistRequest Tests

    [Fact]
    public void UpdateSetlistRequest_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var request = new UpdateSetlistRequest();

        // Assert
        request.Name.Should().Be(string.Empty);
        request.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateSetlistRequest_ShouldSetProperties()
    {
        // Arrange
        var request = new UpdateSetlistRequest();
        var expectedName = "My Awesome Setlist";
        var expectedDescription = "A collection of my favorite songs";

        // Act
        request.Name = expectedName;
        request.Description = expectedDescription;

        // Assert
        request.Name.Should().Be(expectedName);
        request.Description.Should().Be(expectedDescription);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void UpdateSetlistRequest_ShouldFailValidation_WhenNameIsNullOrEmpty(string? invalidName)
    {
        // Arrange
        var request = new UpdateSetlistRequest { Name = invalidName! };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.MemberNames.Contains("Name"));
        results.First().ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public void UpdateSetlistRequest_ShouldFailValidation_WhenNameExceedsMaxLength()
    {
        // Arrange
        var longName = new string('A', 101); // Exceeds 100 character limit
        var request = new UpdateSetlistRequest { Name = longName };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.MemberNames.Contains("Name"));
        results.First().ErrorMessage.Should().Contain("100");
    }

    [Fact]
    public void UpdateSetlistRequest_ShouldFailValidation_WhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var longDescription = new string('A', 501); // Exceeds 500 character limit
        var request = new UpdateSetlistRequest 
        { 
            Name = "Valid Name",
            Description = longDescription 
        };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.MemberNames.Contains("Description"));
        results.First().ErrorMessage.Should().Contain("500");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Valid Setlist Name")]
    [InlineData("Concert at Madison Square Garden")]
    public void UpdateSetlistRequest_ShouldPassValidation_WithValidName(string validName)
    {
        // Arrange
        var request = new UpdateSetlistRequest { Name = validName };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Valid description")]
    [InlineData("A longer description that provides context about the setlist")]
    public void UpdateSetlistRequest_ShouldPassValidation_WithValidDescription(string? description)
    {
        // Arrange
        var request = new UpdateSetlistRequest 
        { 
            Name = "Valid Name",
            Description = description 
        };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void UpdateSetlistRequest_ShouldPassValidation_WithMaximumValidLengths()
    {
        // Arrange
        var maxName = new string('A', 100); // Exactly 100 characters
        var maxDescription = new string('B', 500); // Exactly 500 characters
        var request = new UpdateSetlistRequest 
        { 
            Name = maxName,
            Description = maxDescription 
        };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void UpdateSetlistRequest_ShouldAllowSpecialCharactersInName()
    {
        // Arrange
        var nameWithSpecialChars = "Rock & Roll Classics - '80s Edition!";
        var request = new UpdateSetlistRequest { Name = nameWithSpecialChars };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
        request.Name.Should().Be(nameWithSpecialChars);
    }

    [Fact]
    public void UpdateSetlistRequest_ShouldAllowSpecialCharactersInDescription()
    {
        // Arrange
        var descriptionWithSpecialChars = "A mix of rock & roll classics from the '80s! Including hits like \"Don't Stop Believin'\" and more...";
        var request = new UpdateSetlistRequest 
        { 
            Name = "Valid Name",
            Description = descriptionWithSpecialChars 
        };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
        request.Description.Should().Be(descriptionWithSpecialChars);
    }

    [Fact]
    public void UpdateSetlistRequest_ShouldSupportUnicodeCharacters()
    {
        // Arrange
        var unicodeName = "Música Clásica - Ñoño's Favorites";
        var unicodeDescription = "Songs with émotions: café, naïve, résumé";
        var request = new UpdateSetlistRequest 
        { 
            Name = unicodeName,
            Description = unicodeDescription 
        };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
        request.Name.Should().Be(unicodeName);
        request.Description.Should().Be(unicodeDescription);
    }

    #endregion

    #region Edge Case and Security Tests

    [Fact]
    public void UpdateSetlistRequest_ShouldHandleLeadingAndTrailingWhitespace()
    {
        // Arrange
        var nameWithWhitespace = "  Valid Name  ";
        var request = new UpdateSetlistRequest { Name = nameWithWhitespace };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
        // Note: Validation doesn't trim whitespace, that's typically done in controllers
        request.Name.Should().Be(nameWithWhitespace);
    }

    [Fact]
    public void UpdateSetlistRequest_ShouldAllowNewlinesInDescription()
    {
        // Arrange
        var descriptionWithNewlines = "Line 1\nLine 2\r\nLine 3";
        var request = new UpdateSetlistRequest 
        { 
            Name = "Valid Name",
            Description = descriptionWithNewlines 
        };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
        request.Description.Should().Be(descriptionWithNewlines);
    }

    [Fact]
    public void UpdateSetlistRequest_ShouldPreserveEmptyStringDescription()
    {
        // Arrange
        var request = new UpdateSetlistRequest 
        { 
            Name = "Valid Name",
            Description = string.Empty 
        };

        // Act & Assert
        request.Description.Should().Be(string.Empty);
        request.Description.Should().NotBeNull();
    }

    #endregion
}