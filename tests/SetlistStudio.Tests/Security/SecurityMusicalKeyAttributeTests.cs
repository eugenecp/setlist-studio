using FluentAssertions;
using SetlistStudio.Core.Security;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive tests for the MusicalKeyAttribute validation in the Security namespace.
/// Tests musical key validation, edge cases, and security considerations.
/// Note: This is different from the Validation namespace MusicalKeyAttribute.
/// </summary>
public class SecurityMusicalKeyAttributeTests
{
    private readonly MusicalKeyAttribute _attribute;
    private readonly ValidationContext _validationContext;

    public SecurityMusicalKeyAttributeTests()
    {
        _attribute = new MusicalKeyAttribute();
        _validationContext = new ValidationContext(new object());
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
    public void MusicalKey_ShouldAcceptValidMajorKeys_WithoutAccidentals(string key)
    {
        // Act
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, $"{key} is a valid major key");
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
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, $"{key} is a valid major key with sharp");
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
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, $"{key} is a valid major key with flat");
    }

    [Theory]
    [InlineData("Cm")]
    [InlineData("Dm")]
    [InlineData("Em")]
    [InlineData("Fm")]
    [InlineData("Gm")]
    [InlineData("Am")]
    [InlineData("Bm")]
    public void MusicalKey_ShouldAcceptValidMinorKeys_WithoutAccidentals(string key)
    {
        // Act
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, $"{key} is a valid minor key");
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
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, $"{key} is a valid minor key with sharp");
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
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, $"{key} is a valid minor key with flat");
    }

    [Fact]
    public void MusicalKey_ShouldAcceptNull_AsValidValue()
    {
        // Act
        var result = _attribute.GetValidationResult(null, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, "Null should be allowed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MusicalKey_ShouldAcceptEmptyString_AsValidValue(string key)
    {
        // Act
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, "Empty or whitespace string should be allowed");
    }

    #endregion

    #region Invalid Musical Keys Tests

    [Theory]
    [InlineData("H")]      // Not a valid note name in English notation
    [InlineData("X")]      // Invalid note name
    [InlineData("Z")]      // Invalid note name
    [InlineData("CB")]     // Invalid combination
    [InlineData("E#")]     // Theoretically valid but not in the allowed list
    [InlineData("Fb")]     // Theoretically valid but not in the allowed list
    [InlineData("B#")]     // Theoretically valid but not in the allowed list
    public void MusicalKey_ShouldRejectInvalidKeys(string key)
    {
        // Act
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().NotBe(ValidationResult.Success, $"{key} should be rejected as invalid");
        result?.ErrorMessage.Should().Contain($"'{key}' is not a valid musical key");
        result?.ErrorMessage.Should().Contain("Valid keys are:");
    }

    [Theory]
    [InlineData("c")]      // Lowercase
    [InlineData("d#")]     // Lowercase with sharp
    [InlineData("ebm")]    // Lowercase minor
    [InlineData("f#M")]    // Uppercase M for major
    [InlineData("CM")]     // Uppercase M for major
    public void MusicalKey_ShouldRejectCaseVariations(string key)
    {
        // Act
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().NotBe(ValidationResult.Success, $"{key} should be rejected due to incorrect case");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("C1")]
    [InlineData("D-flat")]
    [InlineData("F sharp")]
    [InlineData("A minor")]
    [InlineData("C major")]
    public void MusicalKey_ShouldRejectNonStandardFormats(string key)
    {
        // Act
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().NotBe(ValidationResult.Success, $"{key} should be rejected as non-standard format");
    }

    #endregion

    #region Security and Edge Cases

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("'; DROP TABLE Songs; --")]
    [InlineData("../../../etc/passwd")]
    [InlineData("javascript:alert('xss')")]
    public void MusicalKey_ShouldRejectMaliciousInput_ForSecurity(string maliciousInput)
    {
        // Act
        var result = _attribute.GetValidationResult(maliciousInput, _validationContext);

        // Assert
        result.Should().NotBe(ValidationResult.Success, "Malicious input should be rejected");
        result?.ErrorMessage.Should().Contain("is not a valid musical key");
    }

    [Theory]
    [InlineData("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC")]
    public void MusicalKey_ShouldRejectExcessivelyLongStrings_ForSecurity(string longInput)
    {
        // Act
        var result = _attribute.GetValidationResult(longInput, _validationContext);

        // Assert
        result.Should().NotBe(ValidationResult.Success, "Excessively long input should be rejected");
    }

    [Fact]
    public void MusicalKey_ShouldRejectDynamicallyGeneratedLongStrings_ForSecurity()
    {
        // Arrange
        var longInput = "A" + new string('b', 1000);

        // Act
        var result = _attribute.GetValidationResult(longInput, _validationContext);

        // Assert
        result.Should().NotBe(ValidationResult.Success, "Dynamically generated long input should be rejected");
    }

    [Fact]
    public void MusicalKey_ShouldRejectNonStringValues()
    {
        // Arrange
        var testValues = new object[] { 123, 45.6, true, new object() };

        // Act & Assert
        foreach (var value in testValues)
        {
            var result = _attribute.GetValidationResult(value, _validationContext);
            result.Should().NotBe(ValidationResult.Success, $"Non-string value of type {value.GetType()} should be rejected");
        }
    }

    #endregion

    #region Comprehensive Key Coverage Tests

    [Fact]
    public void MusicalKey_ShouldAcceptAllValidMajorKeys()
    {
        // Arrange
        var validMajorKeys = new[]
        {
            "C", "C#", "Db", "D", "D#", "Eb", "E", "F", "F#", "Gb", "G", "G#", "Ab", "A", "A#", "Bb", "B"
        };

        // Act & Assert
        foreach (var key in validMajorKeys)
        {
            var result = _attribute.GetValidationResult(key, _validationContext);
            result.Should().Be(ValidationResult.Success, $"Major key {key} should be valid");
        }
    }

    [Fact]
    public void MusicalKey_ShouldAcceptAllValidMinorKeys()
    {
        // Arrange
        var validMinorKeys = new[]
        {
            "Cm", "C#m", "Dbm", "Dm", "D#m", "Ebm", "Em", "Fm", "F#m", "Gbm", "Gm", "G#m", "Abm", "Am", "A#m", "Bbm", "Bm"
        };

        // Act & Assert
        foreach (var key in validMinorKeys)
        {
            var result = _attribute.GetValidationResult(key, _validationContext);
            result.Should().Be(ValidationResult.Success, $"Minor key {key} should be valid");
        }
    }

    #endregion

    #region Enharmonic Equivalents Tests

    [Theory]
    [InlineData("C#", "Db")]
    [InlineData("D#", "Eb")]
    [InlineData("F#", "Gb")]
    [InlineData("G#", "Ab")]
    [InlineData("A#", "Bb")]
    public void MusicalKey_ShouldAcceptBothEnharmonicEquivalents_ForMajorKeys(string sharp, string flat)
    {
        // Act & Assert
        _attribute.GetValidationResult(sharp, _validationContext).Should().Be(ValidationResult.Success, $"Sharp key {sharp} should be valid");
        _attribute.GetValidationResult(flat, _validationContext).Should().Be(ValidationResult.Success, $"Flat key {flat} should be valid");
    }

    [Theory]
    [InlineData("C#m", "Dbm")]
    [InlineData("D#m", "Ebm")]
    [InlineData("F#m", "Gbm")]
    [InlineData("G#m", "Abm")]
    [InlineData("A#m", "Bbm")]
    public void MusicalKey_ShouldAcceptBothEnharmonicEquivalents_ForMinorKeys(string sharp, string flat)
    {
        // Act & Assert
        _attribute.GetValidationResult(sharp, _validationContext).Should().Be(ValidationResult.Success, $"Sharp minor key {sharp} should be valid");
        _attribute.GetValidationResult(flat, _validationContext).Should().Be(ValidationResult.Success, $"Flat minor key {flat} should be valid");
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void MusicalKey_ShouldProvideHelpfulErrorMessage_WithValidKeysList()
    {
        // Act
        var result = _attribute.GetValidationResult("InvalidKey", _validationContext);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result?.ErrorMessage.Should().Contain("'InvalidKey' is not a valid musical key");
        result?.ErrorMessage.Should().Contain("Valid keys are:");
        result?.ErrorMessage.Should().Contain("C, C#, Db, D");  // Should contain some valid keys
        result?.ErrorMessage.Should().Contain("Am, A#m, Bbm, Bm"); // Should contain some minor keys
    }

    [Fact]
    public void MusicalKey_ErrorMessage_ShouldContainAllValidKeys()
    {
        // Act
        var result = _attribute.GetValidationResult("InvalidKey", _validationContext);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        var errorMessage = result?.ErrorMessage ?? "";
        
        // Check that all major keys are listed
        var majorKeys = new[] { "C", "C#", "Db", "D", "D#", "Eb", "E", "F", "F#", "Gb", "G", "G#", "Ab", "A", "A#", "Bb", "B" };
        foreach (var key in majorKeys)
        {
            errorMessage.Should().Contain(key, $"Error message should list major key {key}");
        }
        
        // Check that all minor keys are listed
        var minorKeys = new[] { "Cm", "C#m", "Dbm", "Dm", "D#m", "Ebm", "Em", "Fm", "F#m", "Gbm", "Gm", "G#m", "Abm", "Am", "A#m", "Bbm", "Bm" };
        foreach (var key in minorKeys)
        {
            errorMessage.Should().Contain(key, $"Error message should list minor key {key}");
        }
    }

    #endregion

    #region Practical Usage Tests

    [Theory]
    [InlineData("G")]     // Common guitar key
    [InlineData("C")]     // Common piano key
    [InlineData("Em")]    // Common guitar minor key
    [InlineData("Am")]    // Common guitar minor key
    [InlineData("F#")]    // Common for certain instruments
    [InlineData("Bb")]    // Common for wind instruments
    public void MusicalKey_ShouldAcceptCommonlyUsedKeys(string key)
    {
        // Act
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, $"Common key {key} should be valid");
    }

    [Theory]
    [InlineData("C#")]    // 7 sharps - theoretical but valid
    [InlineData("Gb")]    // 6 flats - less common but valid
    [InlineData("F#m")]   // 3 sharps - common in some genres
    [InlineData("Bbm")]   // 5 flats - jazz standard key
    public void MusicalKey_ShouldAcceptComplexKeys_UsedInAdvancedMusic(string key)
    {
        // Act
        var result = _attribute.GetValidationResult(key, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, $"Complex key {key} should be valid for advanced music");
    }

    #endregion
}