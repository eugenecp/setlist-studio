using FluentAssertions;
using SetlistStudio.Core.Validation;
using Xunit;
using System.ComponentModel.DataAnnotations;

namespace SetlistStudio.Tests.Validation;

/// <summary>
/// Comprehensive tests for the MusicalKeyAttribute validation
/// Tests valid and invalid musical key formats and edge cases
/// </summary>
public class MusicalKeyAttributeTests
{
    private readonly MusicalKeyAttribute _attribute;

    public MusicalKeyAttributeTests()
    {
        _attribute = new MusicalKeyAttribute();
    }

    #region Valid Musical Keys Tests

    [Theory]
    [InlineData("C")]
    [InlineData("D")]
    [InlineData("E")]
    [InlineData("F")]
    [InlineData("G")]
    [InlineData("A")]
    [InlineData("B")]
    public void MusicalKey_ShouldAcceptValidMajorKeys_WithoutSharpsOrFlats(string key)
    {
        // Act
        var result = _attribute.IsValid(key);

        // Assert
        result.Should().BeTrue($"{key} is a valid major key");
    }

    [Theory]
    [InlineData("C#")]
    [InlineData("D#")]
    [InlineData("F#")]
    [InlineData("G#")]
    [InlineData("A#")]
    public void MusicalKey_ShouldAcceptValidMajorKeys_WithSharps(string key)
    {
        // Act
        var result = _attribute.IsValid(key);

        // Assert
        result.Should().BeTrue($"{key} is a valid major key with sharp");
    }

    [Theory]
    [InlineData("Db")]
    [InlineData("Eb")]
    [InlineData("Gb")]
    [InlineData("Ab")]
    [InlineData("Bb")]
    public void MusicalKey_ShouldAcceptValidMajorKeys_WithFlats(string key)
    {
        // Act
        var result = _attribute.IsValid(key);

        // Assert
        result.Should().BeTrue($"{key} is a valid major key with flat");
    }

    [Theory]
    [InlineData("Am")]
    [InlineData("Bm")]
    [InlineData("Cm")]
    [InlineData("Dm")]
    [InlineData("Em")]
    [InlineData("Fm")]
    [InlineData("Gm")]
    public void MusicalKey_ShouldAcceptValidMinorKeys_Natural(string key)
    {
        // Act
        var result = _attribute.IsValid(key);

        // Assert
        result.Should().BeTrue($"{key} is a valid minor key");
    }

    [Theory]
    [InlineData("C#m")]
    [InlineData("D#m")]
    [InlineData("F#m")]
    [InlineData("G#m")]
    [InlineData("A#m")]
    public void MusicalKey_ShouldAcceptValidMinorKeys_WithSharps(string key)
    {
        // Act
        var result = _attribute.IsValid(key);

        // Assert
        result.Should().BeTrue($"{key} is a valid minor key with sharp");
    }

    [Theory]
    [InlineData("Dbm")]
    [InlineData("Ebm")]
    [InlineData("Gbm")]
    [InlineData("Abm")]
    [InlineData("Bbm")]
    public void MusicalKey_ShouldAcceptValidMinorKeys_WithFlats(string key)
    {
        // Act
        var result = _attribute.IsValid(key);

        // Assert
        result.Should().BeTrue($"{key} is a valid minor key with flat");
    }

    [Theory]
    [InlineData("c")]
    [InlineData("f#")]
    [InlineData("bb")]
    [InlineData("am")]
    [InlineData("f#m")]
    public void MusicalKey_ShouldAcceptValidKeys_CaseInsensitive(string key)
    {
        // Act
        var result = _attribute.IsValid(key);

        // Assert
        result.Should().BeTrue($"{key} should be valid regardless of case");
    }

    #endregion

    #region Invalid Musical Keys Tests

    [Theory]
    [InlineData("H")]     // Not a valid note in English notation
    [InlineData("I")]     // Not a valid note
    [InlineData("J")]     // Not a valid note
    [InlineData("X")]     // Not a valid note
    [InlineData("Z")]     // Not a valid note
    public void MusicalKey_ShouldRejectInvalidNotes(string key)
    {
        // Act
        var result = _attribute.IsValid(key);

        // Assert
        result.Should().BeFalse($"{key} is not a valid musical note");
    }

    [Theory]
    [InlineData("C##")]   // Double sharp not supported
    [InlineData("Dbb")]   // Double flat not supported
    [InlineData("E#")]    // E# is F, but should be rejected for clarity
    [InlineData("Fb")]    // Fb is E, but should be rejected for clarity
    [InlineData("B#")]    // B# is C, but should be rejected for clarity
    [InlineData("Cb")]    // Cb is B, but should be rejected for clarity
    public void MusicalKey_ShouldRejectInvalidAccidentals(string key)
    {
        // Act
        var result = _attribute.IsValid(key);

        // Assert
        result.Should().BeFalse($"{key} uses invalid or confusing accidental notation");
    }

    [Theory]
    [InlineData("CC")]    // Double letter
    [InlineData("C minor")] // Word instead of abbreviation
    [InlineData("C maj")] // Word instead of standard notation
    [InlineData("1")]     // Number
    [InlineData("!")]     // Special character
    public void MusicalKey_ShouldRejectInvalidFormats(string key)
    {
        // Act
        var result = _attribute.IsValid(key);

        // Assert
        result.Should().BeFalse($"'{key}' is not a valid musical key format");
    }

    #endregion

    #region Null and Empty Tests

    [Fact]
    public void MusicalKey_ShouldAcceptNull_AsValid()
    {
        // Act
        var result = _attribute.IsValid(null);

        // Assert
        result.Should().BeTrue("Null values should be considered valid (optional field)");
    }

    [Fact]
    public void MusicalKey_ShouldAcceptEmptyString_AsValid()
    {
        // Act
        var result = _attribute.IsValid("");

        // Assert
        result.Should().BeTrue("Empty string should be considered valid (optional field)");
    }

    [Fact]
    public void MusicalKey_ShouldAcceptWhitespace_AsValid()
    {
        // Act
        var result = _attribute.IsValid("   ");

        // Assert
        result.Should().BeTrue("Whitespace-only string should be considered valid (optional field)");
    }

    #endregion

    #region Helper Methods Tests

    [Fact]
    public void GetValidKeys_ShouldReturnComprehensiveList()
    {
        // Act
        var validKeys = MusicalKeyAttribute.GetValidKeys();

        // Assert
        validKeys.Should().NotBeEmpty("Should return a list of valid keys");
        validKeys.Should().Contain("C", "Major keys should be included");
        validKeys.Should().Contain("Am", "Minor keys should be included");
        validKeys.Should().Contain("F#", "Sharp keys should be included");
        validKeys.Should().Contain("Bb", "Flat keys should be included");
        validKeys.Count.Should().BeGreaterThan(40, "Should include major, minor, and case variations");
    }

    [Theory]
    [InlineData("c", "C")]
    [InlineData("f#m", "F#m")]
    [InlineData("BB", "Bb")]
    [InlineData("am", "Am")]
    public void NormalizeKey_ShouldStandardizeCapitalization(string input, string expected)
    {
        // Act
        var result = MusicalKeyAttribute.NormalizeKey(input);

        // Assert
        result.Should().Be(expected, $"'{input}' should be normalized to '{expected}'");
    }

    [Fact]
    public void NormalizeKey_ShouldHandleNullAndEmpty()
    {
        // Act & Assert
        MusicalKeyAttribute.NormalizeKey(null).Should().BeNull();
        MusicalKeyAttribute.NormalizeKey("").Should().Be("");
        MusicalKeyAttribute.NormalizeKey("   ").Should().Be("   ");
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void FormatErrorMessage_ShouldProvideHelpfulMessage()
    {
        // Act
        var message = _attribute.FormatErrorMessage("MusicalKey");

        // Assert
        message.Should().Contain("MusicalKey", "Should include the field name");
        message.Should().Contain("musical key", "Should mention musical key");
        message.Should().Contain("C", "Should provide example");
        message.Should().Contain("F#", "Should provide sharp example");
        message.Should().Contain("Am", "Should provide minor example");
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(123)]     // Integer
    [InlineData(45.6)]    // Double
    [InlineData(true)]    // Boolean
    public void MusicalKey_ShouldRejectNonStringTypes(object value)
    {
        // Act
        var result = _attribute.IsValid(value);

        // Assert
        result.Should().BeFalse($"Non-string value {value} should be rejected");
    }

    [Theory]
    [InlineData(" C ")]   // Whitespace around valid key
    [InlineData("  F#m  ")] // Whitespace around valid minor key
    public void MusicalKey_ShouldHandleWhitespaceAroundValidKeys(string key)
    {
        // Act
        var result = _attribute.IsValid(key);

        // Assert
        result.Should().BeTrue($"'{key}' should be valid after trimming whitespace");
    }

    #endregion
}