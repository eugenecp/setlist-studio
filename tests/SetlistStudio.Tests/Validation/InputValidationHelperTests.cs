using FluentAssertions;
using SetlistStudio.Core.Validation;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace SetlistStudio.Tests.Validation;

/// <summary>
/// Tests for the InputValidationHelper utility class
/// Tests individual validation methods
/// </summary>
public class InputValidationHelperTests
{
    #region Song Title Validation Tests

    [Fact]
    public void ValidateSongTitle_ShouldReturnSuccess_ForValidTitle()
    {
        // Act
        var result = InputValidationHelper.ValidateSongTitle("Sweet Child O' Mine");

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateSongTitle_ShouldReturnError_ForMaliciousTitle()
    {
        // Act
        var result = InputValidationHelper.ValidateSongTitle("<script>alert('xss')</script>");

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result?.ErrorMessage.Should().Contain("unsafe");
    }

    #endregion

    #region Artist Name Validation Tests

    [Fact]
    public void ValidateArtistName_ShouldReturnSuccess_ForValidArtist()
    {
        // Act
        var result = InputValidationHelper.ValidateArtistName("Guns N' Roses");

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateArtistName_ShouldReturnError_ForMaliciousArtist()
    {
        // Act
        var result = InputValidationHelper.ValidateArtistName("'; DROP TABLE Artists; --");

        // Assert
        result.Should().NotBe(ValidationResult.Success);
    }

    #endregion

    #region Musical Key Validation Tests

    [Theory]
    [InlineData("C")]
    [InlineData("F#")]
    [InlineData("Bb")]
    [InlineData("Am")]
    [InlineData("F#m")]
    public void ValidateMusicalKey_ShouldReturnSuccess_ForValidKeys(string key)
    {
        // Act
        var result = InputValidationHelper.ValidateMusicalKey(key);

        // Assert
        result.Should().Be(ValidationResult.Success, $"Musical key '{key}' should be valid");
    }

    [Theory]
    [InlineData("H")]
    [InlineData("Cmaj")]
    [InlineData("123")]
    [InlineData("invalid")]
    public void ValidateMusicalKey_ShouldReturnError_ForInvalidKeys(string key)
    {
        // Act
        var result = InputValidationHelper.ValidateMusicalKey(key);

        // Assert
        result.Should().NotBe(ValidationResult.Success, $"Musical key '{key}' should be invalid");
    }

    [Fact]
    public void ValidateMusicalKey_ShouldReturnSuccess_ForNullKey()
    {
        // Act
        var result = InputValidationHelper.ValidateMusicalKey(null);

        // Assert
        result.Should().Be(ValidationResult.Success, "Null musical key should be valid (optional field)");
    }

    #endregion

    #region BPM Validation Tests

    [Theory]
    [InlineData(40)]    // Minimum valid
    [InlineData(120)]   // Common tempo
    [InlineData(250)]   // Maximum valid
    public void ValidateBpm_ShouldReturnSuccess_ForValidBpm(int bpm)
    {
        // Act
        var result = InputValidationHelper.ValidateBpm(bpm);

        // Assert
        result.Should().Be(ValidationResult.Success, $"BPM {bpm} should be valid");
    }

    [Theory]
    [InlineData(39)]    // Below minimum
    [InlineData(251)]   // Above maximum
    [InlineData(-10)]   // Negative
    [InlineData(0)]     // Zero
    public void ValidateBpm_ShouldReturnError_ForInvalidBpm(int bpm)
    {
        // Act
        var result = InputValidationHelper.ValidateBpm(bpm);

        // Assert
        result.Should().NotBe(ValidationResult.Success, $"BPM {bpm} should be invalid");
    }

    [Fact]
    public void ValidateBpm_ShouldReturnSuccess_ForNullBpm()
    {
        // Act
        var result = InputValidationHelper.ValidateBpm(null);

        // Assert
        result.Should().Be(ValidationResult.Success, "Null BPM should be valid (optional field)");
    }

    #endregion

    #region Setlist Name Validation Tests

    [Fact]
    public void ValidateSetlistName_ShouldReturnSuccess_ForValidName()
    {
        // Act
        var result = InputValidationHelper.ValidateSetlistName("Wedding Reception");

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateSetlistName_ShouldReturnError_ForMaliciousName()
    {
        // Act
        var result = InputValidationHelper.ValidateSetlistName("<script>alert('xss')</script>");

        // Assert
        result.Should().NotBe(ValidationResult.Success);
    }

    #endregion

    #region User Notes Validation Tests

    [Fact]
    public void ValidateUserNotes_ShouldReturnSuccess_ForValidNotes()
    {
        // Act
        var result = InputValidationHelper.ValidateUserNotes("Great guitar solo in the middle");

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateUserNotes_ShouldReturnSuccess_ForNullNotes()
    {
        // Act
        var result = InputValidationHelper.ValidateUserNotes(null);

        // Assert
        result.Should().Be(ValidationResult.Success, "Null notes should be valid (optional field)");
    }

    #endregion

    #region Duration Validation Tests

    [Theory]
    [InlineData(30)]      // 30 seconds
    [InlineData(180)]     // 3 minutes
    [InlineData(600)]     // 10 minutes
    public void ValidateDuration_ShouldReturnSuccess_ForValidDuration(int duration)
    {
        // Act
        var result = InputValidationHelper.ValidateDuration(duration);

        // Assert
        result.Should().Be(ValidationResult.Success, $"Duration {duration} seconds should be valid");
    }

    [Theory]
    [InlineData(-10)]     // Negative
    [InlineData(0)]       // Zero
    [InlineData(3601)]    // Over 1 hour
    public void ValidateDuration_ShouldReturnError_ForInvalidDuration(int duration)
    {
        // Act
        var result = InputValidationHelper.ValidateDuration(duration);

        // Assert
        result.Should().NotBe(ValidationResult.Success, $"Duration {duration} seconds should be invalid");
    }

    #endregion

    #region Difficulty Rating Validation Tests

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void ValidateDifficultyRating_ShouldReturnSuccess_ForValidRating(int rating)
    {
        // Act
        var result = InputValidationHelper.ValidateDifficultyRating(rating);

        // Assert
        result.Should().Be(ValidationResult.Success, $"Difficulty rating {rating} should be valid");
    }

    [Theory]
    [InlineData(0)]       // Below minimum
    [InlineData(11)]      // Above maximum
    [InlineData(-5)]      // Negative
    public void ValidateDifficultyRating_ShouldReturnError_ForInvalidRating(int rating)
    {
        // Act
        var result = InputValidationHelper.ValidateDifficultyRating(rating);

        // Assert
        result.Should().NotBe(ValidationResult.Success, $"Difficulty rating {rating} should be invalid");
    }

    #endregion

    #region Comprehensive Validation Tests

    [Fact]
    public void ValidateSong_ShouldReturnNoErrors_ForValidSong()
    {
        // Act
        var results = InputValidationHelper.ValidateSong(
            title: "Sweet Child O' Mine",
            artist: "Guns N' Roses",
            musicalKey: "D",
            bpm: 125,
            durationSeconds: 356,
            difficultyRating: 4,
            notes: "Great guitar solo"
        );

        // Assert
        results.Should().BeEmpty("Valid song should pass all validation");
    }

    [Fact]
    public void ValidateSong_ShouldReturnErrors_ForInvalidSong()
    {
        // Act
        var results = InputValidationHelper.ValidateSong(
            title: "<script>alert('xss')</script>",
            artist: "",
            musicalKey: "H",
            bpm: 300,
            durationSeconds: -10,
            difficultyRating: 15,
            notes: "'; DROP TABLE Songs; --"
        );

        // Assert
        results.Should().NotBeEmpty("Invalid song should fail validation");
        results.Count().Should().BeGreaterThan(1, "Should have multiple validation errors");
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnNoErrors_ForValidSetlist()
    {
        // Act
        var results = InputValidationHelper.ValidateSetlist(
            name: "Wedding Reception",
            description: "Songs for a romantic evening",
            venue: "Grand Hotel Ballroom"
        );

        // Assert
        results.Should().BeEmpty("Valid setlist should pass all validation");
    }

    [Fact]
    public void ValidateSetlist_ShouldReturnErrors_ForInvalidSetlist()
    {
        // Act
        var results = InputValidationHelper.ValidateSetlist(
            name: "<script>alert('xss')</script>",
            description: "'; DROP TABLE Setlists; --",
            venue: "javascript:alert('xss')"
        );

        // Assert
        results.Should().NotBeEmpty("Invalid setlist should fail validation");
    }

    #endregion

    #region Helper Method Tests

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEmail_ShouldReturnCorrectResult(string email, bool expected)
    {
        // Act
        var result = InputValidationHelper.IsValidEmail(email);

        // Assert
        result.Should().Be(expected, $"Email '{email}' should be {(expected ? "valid" : "invalid")}");
    }

    [Theory]
    [InlineData("Safe123", true)]
    [InlineData("Safe Text 123", true)]
    [InlineData("<script>", false)]
    [InlineData("test@domain", false)]
    public void IsAlphanumericSafe_ShouldReturnCorrectResult(string input, bool expected)
    {
        // Act
        var result = InputValidationHelper.IsAlphanumericSafe(input);

        // Assert
        result.Should().Be(expected, $"Input '{input}' should be {(expected ? "safe" : "unsafe")}");
    }

    #endregion

    #region Edge Cases and Performance Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    public void IsValidEmail_ShouldHandleWhitespaceStrings(string email)
    {
        // Act
        var result = InputValidationHelper.IsValidEmail(email);

        // Assert
        result.Should().BeFalse($"Whitespace-only email '{email}' should be invalid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    public void IsAlphanumericSafe_ShouldAllowWhitespaceStrings(string input)
    {
        // Act
        var result = InputValidationHelper.IsAlphanumericSafe(input);

        // Assert
        result.Should().BeTrue($"Whitespace-only input '{input}' should be considered safe");
    }

    [Fact]
    public void IsValidEmail_ShouldHandleVeryLongEmail()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@" + new string('b', 250) + ".com";

        // Act
        var result = InputValidationHelper.IsValidEmail(longEmail);

        // Assert
        result.Should().BeTrue("Very long but valid email should be accepted");
    }

    [Fact]
    public void IsValidEmail_ShouldHandleEmailWithSpaces()
    {
        // Arrange
        var emailWithSpaces = "  valid@example.com  ";

        // Act
        var result = InputValidationHelper.IsValidEmail(emailWithSpaces);

        // Assert
        result.Should().BeTrue("Email with leading/trailing spaces should be valid after trimming");
    }

    [Theory]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test@.com")]
    [InlineData("test@com")]
    [InlineData("test.example.com")]
    [InlineData("test@example.")]
    [InlineData("test@example")]
    public void IsValidEmail_ShouldRejectMalformedEmails(string email)
    {
        // Act
        var result = InputValidationHelper.IsValidEmail(email);

        // Assert
        result.Should().BeFalse($"Malformed email '{email}' should be invalid");
    }

    [Theory]
    [InlineData("hello world 123")]
    [InlineData("Test-Song_Name.mp3")]
    [InlineData("UPPERCASE lowercase 123")]
    [InlineData("123456")]
    [InlineData("a")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("multi word test")]
    public void IsAlphanumericSafe_ShouldAcceptSafeInput(string input)
    {
        // Act
        var result = InputValidationHelper.IsAlphanumericSafe(input);

        // Assert
        result.Should().BeTrue($"Safe input '{input}' should be accepted");
    }

    [Theory]
    [InlineData("test<script>")]
    [InlineData("test&amp;")]
    [InlineData("test%20")]
    [InlineData("test@#$")]
    [InlineData("test(brackets)")]
    [InlineData("test[brackets]")]
    [InlineData("test{brackets}")]
    [InlineData("test|pipe")]
    [InlineData("test\\backslash")]
    [InlineData("test/slash")]
    [InlineData("test+plus")]
    [InlineData("test=equals")]
    [InlineData("test?question")]
    [InlineData("test:colon")]
    [InlineData("test;semicolon")]
    [InlineData("test\"quote")]
    [InlineData("test'apostrophe")]
    [InlineData("test`backtick")]
    [InlineData("test~tilde")]
    [InlineData("test!exclamation")]
    [InlineData("test*asterisk")]
    [InlineData("test^caret")]
    public void IsAlphanumericSafe_ShouldRejectUnsafeInput(string input)
    {
        // Act
        var result = InputValidationHelper.IsAlphanumericSafe(input);

        // Assert
        result.Should().BeFalse($"Unsafe input '{input}' should be rejected");
    }

    [Theory]
    [InlineData("Simple Song Title")]
    [InlineData("Song with (Parentheses)")]
    public void ValidateSongTitle_ShouldAcceptValidTitles(string title)
    {
        // Act
        var result = InputValidationHelper.ValidateSongTitle(title);

        // Assert
        result.Should().Be(ValidationResult.Success, $"Valid song title '{title}' should be accepted");
    }

    [Theory]
    [InlineData("C")]
    [InlineData("C#")]
    [InlineData("Am")]
    [InlineData("F#m")]
    public void ValidateMusicalKey_ShouldAcceptValidKeys(string key)
    {
        // Act
        var result = InputValidationHelper.ValidateMusicalKey(key);

        // Assert
        result.Should().Be(ValidationResult.Success, $"Valid musical key '{key}' should be accepted");
    }

    [Theory]
    [InlineData("H")]
    [InlineData("123")]
    public void ValidateMusicalKey_ShouldRejectInvalidKeys(string key)
    {
        // Act
        var result = InputValidationHelper.ValidateMusicalKey(key);

        // Assert
        result.Should().NotBe(ValidationResult.Success, $"Invalid key '{key}' should be rejected");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(100)]
    public void ValidateDifficultyRating_ShouldRejectInvalidRatings(int rating)
    {
        // Act
        var result = InputValidationHelper.ValidateDifficultyRating(rating);

        // Assert
        result.Should().NotBe(ValidationResult.Success, $"Invalid difficulty rating {rating} should be rejected");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void ValidateDifficultyRating_ShouldAcceptValidRatings(int rating)
    {
        // Act
        var result = InputValidationHelper.ValidateDifficultyRating(rating);

        // Assert
        result.Should().Be(ValidationResult.Success, $"Valid difficulty rating {rating} should be accepted");
    }

    #endregion

    #region Edge Cases and Coverage Tests

    [Fact]
    public void ValidateSongTitle_WithNullOrWhiteSpace_ReturnsValidationError()
    {
        // Arrange & Act & Assert
        var result1 = InputValidationHelper.ValidateSongTitle(null!);
        result1.Should().NotBe(ValidationResult.Success);
        result1.ErrorMessage.Should().Be("Song title is required");

        var result2 = InputValidationHelper.ValidateSongTitle("");
        result2.Should().NotBe(ValidationResult.Success);
        result2.ErrorMessage.Should().Be("Song title is required");

        var result3 = InputValidationHelper.ValidateSongTitle("   ");
        result3.Should().NotBe(ValidationResult.Success);
        result3.ErrorMessage.Should().Be("Song title is required");
    }

    [Fact]
    public void ValidateSongTitle_WithTooLongTitle_ReturnsValidationError()
    {
        // Arrange
        var title = new string('A', 201); // 201 characters

        // Act
        var result = InputValidationHelper.ValidateSongTitle(title);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result.ErrorMessage.Should().Be("Song title must be between 1 and 200 characters");
    }

    [Fact]
    public void ValidateSetlistName_WithNullOrWhiteSpace_ReturnsValidationError()
    {
        // Arrange & Act & Assert
        var result1 = InputValidationHelper.ValidateSetlistName(null!);
        result1.Should().NotBe(ValidationResult.Success);
        result1.ErrorMessage.Should().Be("Setlist name is required");

        var result2 = InputValidationHelper.ValidateSetlistName("");
        result2.Should().NotBe(ValidationResult.Success);
        result2.ErrorMessage.Should().Be("Setlist name is required");

        var result3 = InputValidationHelper.ValidateSetlistName("   ");
        result3.Should().NotBe(ValidationResult.Success);
        result3.ErrorMessage.Should().Be("Setlist name is required");
    }

    [Fact]
    public void ValidateSetlistName_WithTooLongName_ReturnsValidationError()
    {
        // Arrange
        var name = new string('A', 201); // 201 characters

        // Act
        var result = InputValidationHelper.ValidateSetlistName(name);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result.ErrorMessage.Should().Be("Setlist name must be between 1 and 200 characters");
    }

    [Fact]
    public void ValidateMusicalKey_WithInvalidKeyAttribute_ReturnsValidationError()
    {
        // Arrange - use a key that passes regex but fails MusicalKeyAttribute validation
        var invalidKey = "H"; // H is not a valid musical key

        // Act
        var result = InputValidationHelper.ValidateMusicalKey(invalidKey);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public void ValidateUserNotes_WithTooLongNotes_ReturnsValidationError()
    {
        // Arrange
        var notes = new string('A', 2001); // 2001 characters

        // Act
        var result = InputValidationHelper.ValidateUserNotes(notes);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result.ErrorMessage.Should().Be("Notes must be 2000 characters or less");
    }

    [Fact]
    public void ValidateDuration_WithNullValue_ReturnsSuccess()
    {
        // Arrange
        int? duration = null;

        // Act
        var result = InputValidationHelper.ValidateDuration(duration);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateDifficultyRating_WithNullValue_ReturnsSuccess()
    {
        // Arrange
        int? rating = null;

        // Act
        var result = InputValidationHelper.ValidateDifficultyRating(rating);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void SanitizeInput_WithNullOrWhiteSpace_ReturnsEmptyString()
    {
        // Arrange & Act & Assert
        var result1 = InputValidationHelper.SanitizeInput(null);
        result1.Should().Be(string.Empty);

        var result2 = InputValidationHelper.SanitizeInput("");
        result2.Should().Be(string.Empty);

        var result3 = InputValidationHelper.SanitizeInput("   ");
        result3.Should().Be(string.Empty);
    }

    [Fact]
    public void SanitizeInput_WithValidInput_ReturnsSanitizedString()
    {
        // Arrange
        var input = "Test Song Title";

        // Act
        var result = InputValidationHelper.SanitizeInput(input);

        // Assert
        result.Should().NotBeNull();
        // The actual sanitization logic is handled by SanitizedStringAttribute.SanitizeMusicalContent
        // We just verify the method calls through and returns a non-null result
    }

    #endregion

    #region Coverage Gap Tests - Additional Edge Cases

    [Fact]
    public void ValidateSongTitle_WithTooLongTitle_ReturnsValidationError_AdditionalCase()
    {
        // Arrange - Test the title.Length > 200 branch for additional coverage
        var title = new string('A', 201); // 201 characters for length check

        // Act
        var result = InputValidationHelper.ValidateSongTitle(title);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result.ErrorMessage.Should().Be("Song title must be between 1 and 200 characters");
    }

    [Fact]
    public void ValidateArtistName_WithTooLongName_ReturnsValidationError()
    {
        // Arrange - Test the artist.Length > 200 branch that was missing
        var artist = new string('A', 201); // 201 characters

        // Act
        var result = InputValidationHelper.ValidateArtistName(artist);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result.ErrorMessage.Should().Be("Artist name must be between 1 and 200 characters");
    }

    [Theory]
    [InlineData("H")] // Invalid musical key that passes regex but fails MusicalKeyAttribute
    [InlineData("Cmaj7")] // Complex chord that might pass regex but fail attribute validation
    public void ValidateMusicalKey_WithInvalidKeyThatPassesRegex_ReturnsValidationError(string key)
    {
        // Arrange - Test keys that pass MusicalNotationPattern but fail MusicalKeyAttribute.IsValid()
        // This covers the missing branch in ValidateMusicalKey

        // Act  
        var result = InputValidationHelper.ValidateMusicalKey(key);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result.ErrorMessage.Should().NotBeNull();
        // The error message comes from MusicalKeyAttribute, so we just verify it's not null/empty
    }

    #endregion
}