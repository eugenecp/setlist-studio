using FluentAssertions;
using SetlistStudio.Core.Security;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive tests for the SafeBpmAttribute validation in the Security namespace.
/// Tests BPM validation ranges, edge cases, and error conditions.
/// </summary>
public class SafeBpmAttributeTests
{
    private readonly SafeBpmAttribute _attribute;
    private readonly ValidationContext _validationContext;

    public SafeBpmAttributeTests()
    {
        _attribute = new SafeBpmAttribute();
        _validationContext = new ValidationContext(new object());
    }

    #region Valid BPM Tests

    [Theory]
    [InlineData(40)]  // Minimum default value
    [InlineData(250)] // Maximum default value
    [InlineData(120)] // Common tempo
    [InlineData(60)]  // Ballad tempo
    [InlineData(180)] // Fast tempo
    [InlineData(90)]  // Moderate tempo
    public void SafeBpm_ShouldAcceptValidBpmValues_WithinDefaultRange(int bpm)
    {
        // Act
        var result = _attribute.GetValidationResult(bpm, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, $"BPM {bpm} should be valid within default range 40-250");
    }

    [Fact]
    public void SafeBpm_ShouldAcceptNull_AsValidValue()
    {
        // Act
        var result = _attribute.GetValidationResult(null, _validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success, "Null should be allowed");
    }

    #endregion

    #region Invalid BPM Tests

    [Theory]
    [InlineData(39)]  // Below minimum
    [InlineData(251)] // Above maximum
    [InlineData(0)]   // Zero
    [InlineData(-10)] // Negative
    [InlineData(500)] // Extremely high
    [InlineData(1)]   // Too low
    public void SafeBpm_ShouldRejectInvalidBpmValues_OutsideDefaultRange(int bpm)
    {
        // Act
        var result = _attribute.GetValidationResult(bpm, _validationContext);

        // Assert
        result.Should().NotBe(ValidationResult.Success, $"BPM {bpm} should be invalid outside range 40-250");
        result!.ErrorMessage.Should().Contain("BPM must be between 40 and 250");
    }

    [Theory]
    [InlineData("120")]    // String number
    [InlineData("abc")]    // Non-numeric string
    [InlineData(120.5)]    // Double
    [InlineData(120f)]     // Float
    [InlineData(true)]     // Boolean
    public void SafeBpm_ShouldRejectNonIntegerValues(object value)
    {
        // Act
        var result = _attribute.GetValidationResult(value, _validationContext);

        // Assert
        result.Should().NotBe(ValidationResult.Success, $"Value {value} of type {value.GetType()} should be invalid");
        result!.ErrorMessage.Should().Contain("BPM must be a number");
    }

    #endregion

    #region Custom Range Tests

    [Fact]
    public void SafeBpm_ShouldAcceptCustomMinMaxRange()
    {
        // Arrange
        var customAttribute = new SafeBpmAttribute { MinBpm = 60, MaxBpm = 140 };

        // Act & Assert - Valid values
        customAttribute.GetValidationResult(60, _validationContext).Should().Be(ValidationResult.Success);
        customAttribute.GetValidationResult(100, _validationContext).Should().Be(ValidationResult.Success);
        customAttribute.GetValidationResult(140, _validationContext).Should().Be(ValidationResult.Success);

        // Act & Assert - Invalid values
        var belowMinResult = customAttribute.GetValidationResult(59, _validationContext);
        belowMinResult.Should().NotBeNull();
        belowMinResult.Should().NotBe(ValidationResult.Success);
        belowMinResult!.ErrorMessage.Should().Contain("BPM must be between 60 and 140");

        var aboveMaxResult = customAttribute.GetValidationResult(141, _validationContext);
        aboveMaxResult.Should().NotBeNull();
        aboveMaxResult.Should().NotBe(ValidationResult.Success);
        aboveMaxResult!.ErrorMessage.Should().Contain("BPM must be between 60 and 140");
    }

    [Fact]
    public void SafeBpm_ShouldAllowExtremeBpmRanges_ForSpecializedUse()
    {
        // Arrange - Very restrictive range for ambient music
        var ambientAttribute = new SafeBpmAttribute { MinBpm = 20, MaxBpm = 60 };

        // Act & Assert
        ambientAttribute.GetValidationResult(30, _validationContext).Should().Be(ValidationResult.Success);
        ambientAttribute.GetValidationResult(19, _validationContext).Should().NotBe(ValidationResult.Success);
        ambientAttribute.GetValidationResult(61, _validationContext).Should().NotBe(ValidationResult.Success);
    }

    [Fact]
    public void SafeBpm_ShouldAllowHighBpmRanges_ForElectronicMusic()
    {
        // Arrange - High range for electronic/dance music
        var electronicAttribute = new SafeBpmAttribute { MinBpm = 120, MaxBpm = 200 };

        // Act & Assert
        electronicAttribute.GetValidationResult(150, _validationContext).Should().Be(ValidationResult.Success);
        electronicAttribute.GetValidationResult(119, _validationContext).Should().NotBe(ValidationResult.Success);
        electronicAttribute.GetValidationResult(201, _validationContext).Should().NotBe(ValidationResult.Success);
    }

    #endregion

    #region Boundary Testing

    [Theory]
    [InlineData(40, true)]   // Exact minimum
    [InlineData(39, false)]  // Just below minimum
    [InlineData(250, true)]  // Exact maximum
    [InlineData(251, false)] // Just above maximum
    public void SafeBpm_ShouldHandleBoundaryValues_Correctly(int bpm, bool shouldBeValid)
    {
        // Act
        var result = _attribute.GetValidationResult(bpm, _validationContext);

        // Assert
        if (shouldBeValid)
        {
            result.Should().Be(ValidationResult.Success, $"BPM {bpm} should be valid at boundary");
        }
        else
        {
            result.Should().NotBe(ValidationResult.Success, $"BPM {bpm} should be invalid at boundary");
        }
    }

    #endregion

    #region Musical Genre BPM Ranges

    [Fact]
    public void SafeBpm_ShouldAcceptTypicalBalladBpm()
    {
        // Arrange - Typical ballad range
        var balladBpms = new[] { 60, 70, 80 };

        // Act & Assert
        foreach (var bpm in balladBpms)
        {
            var result = _attribute.GetValidationResult(bpm, _validationContext);
            result.Should().Be(ValidationResult.Success, $"Ballad BPM {bpm} should be valid");
        }
    }

    [Fact]
    public void SafeBpm_ShouldAcceptTypicalRockBpm()
    {
        // Arrange - Typical rock range
        var rockBpms = new[] { 100, 120, 140, 160 };

        // Act & Assert
        foreach (var bpm in rockBpms)
        {
            var result = _attribute.GetValidationResult(bpm, _validationContext);
            result.Should().Be(ValidationResult.Success, $"Rock BPM {bpm} should be valid");
        }
    }

    [Fact]
    public void SafeBpm_ShouldAcceptTypicalJazzBpm()
    {
        // Arrange - Typical jazz range (varies widely)
        var jazzBpms = new[] { 90, 120, 160, 180, 220 };

        // Act & Assert
        foreach (var bpm in jazzBpms)
        {
            var result = _attribute.GetValidationResult(bpm, _validationContext);
            result.Should().Be(ValidationResult.Success, $"Jazz BPM {bpm} should be valid");
        }
    }

    #endregion

    #region Edge Cases and Security

    [Fact]
    public void SafeBpm_ShouldRejectExtremelyHighValues_ToPreventResourceExhaustion()
    {
        // Arrange - Extremely high values that could cause performance issues
        var extremeBpms = new[] { 1000, 10000, int.MaxValue };

        // Act & Assert
        foreach (var bpm in extremeBpms)
        {
            var result = _attribute.GetValidationResult(bpm, _validationContext);
            result.Should().NotBe(ValidationResult.Success, $"Extreme BPM {bpm} should be rejected for security");
        }
    }

    [Fact]
    public void SafeBpm_ShouldRejectNegativeValues_ToPreventLogicErrors()
    {
        // Arrange
        var negativeBpms = new[] { -1, -100, int.MinValue };

        // Act & Assert
        foreach (var bpm in negativeBpms)
        {
            var result = _attribute.GetValidationResult(bpm, _validationContext);
            result.Should().NotBe(ValidationResult.Success, $"Negative BPM {bpm} should be rejected");
        }
    }

    #endregion

    #region Property Configuration Tests

    [Fact]
    public void SafeBpm_ShouldAllowCustomMinBpm_Configuration()
    {
        // Arrange
        var attribute = new SafeBpmAttribute { MinBpm = 100 };

        // Act & Assert
        attribute.MinBpm.Should().Be(100);
        attribute.GetValidationResult(99, _validationContext).Should().NotBe(ValidationResult.Success);
        attribute.GetValidationResult(100, _validationContext).Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void SafeBpm_ShouldAllowCustomMaxBpm_Configuration()
    {
        // Arrange
        var attribute = new SafeBpmAttribute { MaxBpm = 150 };

        // Act & Assert
        attribute.MaxBpm.Should().Be(150);
        attribute.GetValidationResult(150, _validationContext).Should().Be(ValidationResult.Success);
        attribute.GetValidationResult(151, _validationContext).Should().NotBe(ValidationResult.Success);
    }

    [Fact]
    public void SafeBpm_ShouldHaveCorrectDefaultValues()
    {
        // Act & Assert
        _attribute.MinBpm.Should().Be(40, "Default minimum BPM should be 40");
        _attribute.MaxBpm.Should().Be(250, "Default maximum BPM should be 250");
    }

    #endregion
}