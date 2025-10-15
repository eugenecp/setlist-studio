using FluentAssertions;
using SetlistStudio.Core.Validation;
using Xunit;

namespace SetlistStudio.Tests.Validation;

/// <summary>
/// Comprehensive tests for the BpmRangeAttribute validation
/// Tests BPM validation with musical context and edge cases
/// </summary>
public class BpmRangeAttributeTests
{
    #region Default Range Tests (40-250 BPM)

    [Fact]
    public void BpmRange_ShouldUseDefaultRange_WhenNoParametersProvided()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Assert
        attribute.MinimumBpm.Should().Be(40, "Default minimum should be 40 BPM");
        attribute.MaximumBpm.Should().Be(250, "Default maximum should be 250 BPM");
    }

    [Theory]
    [InlineData(40)]    // Minimum valid
    [InlineData(60)]    // Slow ballad
    [InlineData(80)]    // Ballad
    [InlineData(100)]   // Medium tempo
    [InlineData(120)]   // Common tempo
    [InlineData(140)]   // Up-tempo
    [InlineData(160)]   // Fast
    [InlineData(180)]   // Very fast
    [InlineData(200)]   // Extremely fast
    [InlineData(250)]   // Maximum valid
    public void BpmRange_ShouldAcceptValidBpmValues_InDefaultRange(int bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeTrue($"{bpm} BPM should be valid in default range");
    }

    [Theory]
    [InlineData(39)]    // Below minimum
    [InlineData(0)]     // Zero
    [InlineData(-10)]   // Negative
    [InlineData(251)]   // Above maximum
    [InlineData(300)]   // Way above maximum
    [InlineData(1000)]  // Unrealistic
    public void BpmRange_ShouldRejectInvalidBpmValues_OutsideDefaultRange(int bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeFalse($"{bpm} BPM should be invalid in default range");
    }

    #endregion

    #region Custom Range Tests

    [Fact]
    public void BpmRange_ShouldAcceptCustomRange_WhenProvided()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(60, 180);

        // Assert
        attribute.MinimumBpm.Should().Be(60, "Custom minimum should be set");
        attribute.MaximumBpm.Should().Be(180, "Custom maximum should be set");
    }

    [Theory]
    [InlineData(60)]    // Custom minimum
    [InlineData(100)]   // Within custom range
    [InlineData(140)]   // Within custom range
    [InlineData(180)]   // Custom maximum
    public void BpmRange_ShouldAcceptValidBpmValues_InCustomRange(int bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute(60, 180);

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeTrue($"{bpm} BPM should be valid in custom range 60-180");
    }

    [Theory]
    [InlineData(59)]    // Below custom minimum
    [InlineData(40)]    // Below custom minimum (but valid in default)
    [InlineData(181)]   // Above custom maximum
    [InlineData(250)]   // Above custom maximum (but valid in default)
    public void BpmRange_ShouldRejectInvalidBpmValues_OutsideCustomRange(int bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute(60, 180);

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeFalse($"{bpm} BPM should be invalid in custom range 60-180");
    }

    #endregion

    #region Constructor Validation Tests

    [Fact]
    public void BpmRange_ShouldThrowException_WhenMinimumEqualsMaximum()
    {
        // Act & Assert
        Action act = () => new BpmRangeAttribute(120, 120);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Minimum BPM must be less than maximum BPM*");
    }

    [Fact]
    public void BpmRange_ShouldThrowException_WhenMinimumGreaterThanMaximum()
    {
        // Act & Assert
        Action act = () => new BpmRangeAttribute(180, 120);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Minimum BPM must be less than maximum BPM*");
    }

    #endregion

    #region Null and Optional Value Tests

    [Fact]
    public void BpmRange_ShouldAcceptNull_AsValid()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(null);

        // Assert
        result.Should().BeTrue("Null values should be considered valid (optional field)");
    }

    [Fact]
    public void BpmRange_ShouldAcceptNullableInt_WithValidValue()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        int? bpm = 120;

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeTrue("Nullable int with valid value should be accepted");
    }

    [Fact]
    public void BpmRange_ShouldRejectNullableInt_WithInvalidValue()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        int? bpm = 300;

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeFalse("Nullable int with invalid value should be rejected");
    }

    #endregion

    #region Type Conversion Tests

    [Theory]
    [InlineData(120L)]      // Long
    [InlineData(120f)]      // Float
    [InlineData(120.0)]     // Double
    public void BpmRange_ShouldAcceptNumericTypes_WithValidValues(object bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeTrue($"Valid BPM value {bpm} of type {bpm.GetType().Name} should be accepted");
    }

    [Theory]
    [InlineData("120")]     // String representation
    [InlineData(" 120 ")]  // String with whitespace
    public void BpmRange_ShouldAcceptStringNumbers_WithValidValues(string bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeTrue($"Valid BPM string '{bpm}' should be accepted");
    }

    [Theory]
    [InlineData("abc")]     // Non-numeric string
    [InlineData("120.5.3")] // Invalid number format
    [InlineData("")]        // Empty string
    [InlineData("   ")]     // Whitespace only
    public void BpmRange_ShouldRejectInvalidStringNumbers(string bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeFalse($"Invalid BPM string '{bpm}' should be rejected");
    }

    [Theory]
    [InlineData(120.4f)]    // Float with decimals - should round
    [InlineData(120.6)]     // Double with decimals - should round
    [InlineData(119.5)]     // Double that rounds to valid value
    public void BpmRange_ShouldRoundDecimalValues_ToNearestInteger(object bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeTrue($"BPM value {bpm} should be rounded and validated");
    }

    [Theory]
    [InlineData(true)]      // Boolean
    [InlineData(new int[] { 120 })] // Array
    public void BpmRange_ShouldRejectIncompatibleTypes(object bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeFalse($"Incompatible type {bpm.GetType().Name} should be rejected");
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void FormatErrorMessage_ShouldIncludeFieldName_AndRange()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var message = attribute.FormatErrorMessage("Bpm");

        // Assert
        message.Should().Contain("Bpm", "Should include the field name");
        message.Should().Contain("40", "Should include minimum value");
        message.Should().Contain("250", "Should include maximum value");
        message.Should().Contain("BPM", "Should mention BPM unit");
    }

    [Fact]
    public void FormatErrorMessage_ShouldIncludeGuidance_ForDefaultRange()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var message = attribute.FormatErrorMessage("Bpm");

        // Assert
        message.Should().Contain("ballad", "Should include tempo guidance");
        message.Should().Contain("Medium", "Should include medium tempo guidance");
        message.Should().Contain("Fast", "Should include fast tempo guidance");
    }

    [Fact]
    public void FormatErrorMessage_ShouldHandleCustomRange()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(80, 140);

        // Act
        var message = attribute.FormatErrorMessage("CustomBpm");

        // Assert
        message.Should().Contain("CustomBpm", "Should include custom field name");
        message.Should().Contain("80", "Should include custom minimum");
        message.Should().Contain("140", "Should include custom maximum");
    }

    #endregion

    #region Helper Method Tests

    [Theory]
    [InlineData(50, "Very Slow")]
    [InlineData(70, "Slow")]
    [InlineData(90, "Moderate")]
    [InlineData(110, "Medium")]
    [InlineData(130, "Up-tempo")]
    [InlineData(150, "Fast")]
    [InlineData(170, "Very Fast")]
    [InlineData(200, "Extremely Fast")]
    public void GetTempoDescription_ShouldReturnCorrectDescription(int bpm, string expectedDescription)
    {
        // Act
        var description = BpmRangeAttribute.GetTempoDescription(bpm);

        // Assert
        description.Should().Be(expectedDescription, $"{bpm} BPM should be described as {expectedDescription}");
    }

    [Theory]
    [InlineData("ballad", 60, 80)]
    [InlineData("blues", 80, 120)]
    [InlineData("jazz", 90, 200)]
    [InlineData("rock", 110, 140)]
    [InlineData("pop", 100, 130)]
    [InlineData("electronic", 120, 140)]
    [InlineData("unknown", 40, 250)] // Default range
    public void GetGenreRange_ShouldReturnCorrectRange_ForGenre(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, $"{genre} genre should have minimum BPM of {expectedMin}");
        max.Should().Be(expectedMax, $"{genre} genre should have maximum BPM of {expectedMax}");
    }

    [Fact]
    public void GetGenreRange_ShouldHandleNullGenre()
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(null);

        // Assert
        min.Should().Be(40, "Null genre should return default minimum");
        max.Should().Be(250, "Null genre should return default maximum");
    }

    [Fact]
    public void GetGenreRange_ShouldBeCaseInsensitive()
    {
        // Act
        var (minLower, maxLower) = BpmRangeAttribute.GetGenreRange("rock");
        var (minUpper, maxUpper) = BpmRangeAttribute.GetGenreRange("ROCK");
        var (minMixed, maxMixed) = BpmRangeAttribute.GetGenreRange("Rock");

        // Assert
        minLower.Should().Be(minUpper).And.Be(minMixed, "Genre matching should be case insensitive");
        maxLower.Should().Be(maxUpper).And.Be(maxMixed, "Genre matching should be case insensitive");
    }

    #endregion

    #region Edge Cases and Extreme Values

    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]  
    public void BpmRange_ShouldHandleExtremeIntegerValues(int bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeFalse($"Extreme value {bpm} should be rejected");
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void BpmRange_ShouldHandleSpecialFloatingPointValues(double bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().BeFalse($"Special floating point value {bpm} should be rejected");
    }

    #endregion
}