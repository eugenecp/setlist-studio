using FluentAssertions;
using SetlistStudio.Core.Validation;
using Xunit;

namespace SetlistStudio.Tests.Validation;

/// <summary>
/// Advanced tests for BpmRangeAttribute to improve branch coverage
/// Targets specific branches in TryConvertToInt and GetGenreRange methods
/// </summary>
public class BpmRangeAttributeAdvancedTests
{
    #region TryConvertToInt Branch Coverage Tests

    [Fact]
    public void BpmRange_ShouldHandleLongValueAtIntMinBoundary()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(int.MinValue, int.MaxValue);
        long longValue = int.MinValue;

        // Act
        var result = attribute.IsValid(longValue);

        // Assert
        result.Should().BeTrue("Long value at int.MinValue boundary should be converted and accepted");
    }

    [Fact]
    public void BpmRange_ShouldHandleLongValueAtIntMaxBoundary()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(int.MinValue, int.MaxValue);
        long longValue = int.MaxValue;

        // Act
        var result = attribute.IsValid(longValue);

        // Assert
        result.Should().BeTrue("Long value at int.MaxValue boundary should be converted and accepted");
    }

    [Fact]
    public void BpmRange_ShouldRejectLongValueBelowIntMin()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        long longValue = (long)int.MinValue - 1;

        // Act
        var result = attribute.IsValid(longValue);

        // Assert
        result.Should().BeFalse("Long value below int.MinValue should be rejected");
    }

    [Fact]
    public void BpmRange_ShouldRejectLongValueAboveIntMax()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        long longValue = (long)int.MaxValue + 1;

        // Act
        var result = attribute.IsValid(longValue);

        // Assert
        result.Should().BeFalse("Long value above int.MaxValue should be rejected");
    }

    [Fact]
    public void BpmRange_ShouldHandleFloatAtIntMinBoundary()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(int.MinValue, int.MaxValue);
        float floatValue = int.MinValue;

        // Act
        var result = attribute.IsValid(floatValue);

        // Assert
        result.Should().BeTrue("Float value at int.MinValue boundary should be converted and accepted");
    }

    [Fact]
    public void BpmRange_ShouldHandleFloatAtIntMaxBoundary()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(int.MinValue, int.MaxValue);
        float floatValue = int.MaxValue;

        // Act
        var result = attribute.IsValid(floatValue);

        // Assert
        result.Should().BeTrue("Float value at int.MaxValue boundary should be converted and accepted");
    }

    [Fact]
    public void BpmRange_ShouldRejectNonFiniteFloat()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        
        // Act & Assert
        attribute.IsValid(float.PositiveInfinity).Should().BeFalse("Positive infinity should be rejected");
        attribute.IsValid(float.NegativeInfinity).Should().BeFalse("Negative infinity should be rejected");
        attribute.IsValid(float.NaN).Should().BeFalse("NaN should be rejected");
    }

    [Fact]
    public void BpmRange_ShouldRejectFloatBelowIntMin()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        float floatValue = (float)int.MinValue - 1000000000f; // Beyond int range

        // Act
        var result = attribute.IsValid(floatValue);

        // Assert
        result.Should().BeFalse("Float value below int.MinValue should be rejected");
    }

    [Fact]
    public void BpmRange_ShouldRejectFloatAboveIntMax()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        float floatValue = (float)int.MaxValue + 1000000000f; // Beyond int range

        // Act
        var result = attribute.IsValid(floatValue);

        // Assert
        result.Should().BeFalse("Float value above int.MaxValue should be rejected");
    }

    [Fact]
    public void BpmRange_ShouldHandleDoubleAtIntMinBoundary()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(int.MinValue, int.MaxValue);
        double doubleValue = int.MinValue;

        // Act
        var result = attribute.IsValid(doubleValue);

        // Assert
        result.Should().BeTrue("Double value at int.MinValue boundary should be converted and accepted");
    }

    [Fact]
    public void BpmRange_ShouldHandleDoubleAtIntMaxBoundary()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(int.MinValue, int.MaxValue);
        double doubleValue = int.MaxValue;

        // Act
        var result = attribute.IsValid(doubleValue);

        // Assert
        result.Should().BeTrue("Double value at int.MaxValue boundary should be converted and accepted");
    }

    [Fact]
    public void BpmRange_ShouldRejectDoubleAboveIntMax()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        double doubleValue = (double)int.MaxValue + 1000000000.0;

        // Act
        var result = attribute.IsValid(doubleValue);

        // Assert
        result.Should().BeFalse("Double value above int.MaxValue should be rejected");
    }

    [Fact]
    public void BpmRange_ShouldRejectDoubleBelowIntMin()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        double doubleValue = (double)int.MinValue - 1000000000.0;

        // Act
        var result = attribute.IsValid(doubleValue);

        // Assert
        result.Should().BeFalse("Double value below int.MinValue should be rejected");
    }

    [Fact]
    public void BpmRange_ShouldHandleDecimalAtIntMinBoundary()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(int.MinValue, int.MaxValue);
        decimal decimalValue = int.MinValue;

        // Act
        var result = attribute.IsValid(decimalValue);

        // Assert
        result.Should().BeTrue("Decimal value at int.MinValue boundary should be converted and accepted");
    }

    [Fact]
    public void BpmRange_ShouldHandleDecimalAtIntMaxBoundary()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(int.MinValue, int.MaxValue);
        decimal decimalValue = int.MaxValue;

        // Act
        var result = attribute.IsValid(decimalValue);

        // Assert
        result.Should().BeTrue("Decimal value at int.MaxValue boundary should be converted and accepted");
    }

    [Fact]
    public void BpmRange_ShouldRejectDecimalAboveIntMax()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        decimal decimalValue = (decimal)int.MaxValue + 1;

        // Act
        var result = attribute.IsValid(decimalValue);

        // Assert
        result.Should().BeFalse("Decimal value above int.MaxValue should be rejected");
    }

    [Fact]
    public void BpmRange_ShouldRejectDecimalBelowIntMin()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        decimal decimalValue = (decimal)int.MinValue - 1;

        // Act
        var result = attribute.IsValid(decimalValue);

        // Assert
        result.Should().BeFalse("Decimal value below int.MinValue should be rejected");
    }

    [Fact]
    public void BpmRange_ShouldRejectStringThatCannotBeParsed()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act & Assert
        attribute.IsValid("not-a-number").Should().BeFalse("Unparseable string should be rejected");
        attribute.IsValid("120.5.3").Should().BeFalse("Invalid number format should be rejected");
        attribute.IsValid("").Should().BeFalse("Empty string should be rejected");
        attribute.IsValid("   ").Should().BeFalse("Whitespace-only string should be rejected");
    }

    [Fact]
    public void BpmRange_ShouldAcceptStringThatCanBeParsed()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act & Assert
        attribute.IsValid("120").Should().BeTrue("Valid number string should be accepted");
        attribute.IsValid(" 120 ").Should().BeTrue("Valid number string with whitespace should be accepted");
        attribute.IsValid("100").Should().BeTrue("Valid number string should be accepted");
    }

    #endregion

    #region GetGenreRange Comprehensive Branch Coverage

    [Theory]
    [InlineData("ballad", 60, 80)]
    [InlineData("BALLAD", 60, 80)]
    [InlineData("Ballad", 60, 80)]
    [InlineData("BaLlAd", 60, 80)]
    public void GetGenreRange_ShouldHandleBalladCaseVariations(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, $"{genre} should return ballad BPM range");
        max.Should().Be(expectedMax, $"{genre} should return ballad BPM range");
    }

    [Theory]
    [InlineData("electronic", 120, 140)]
    [InlineData("ELECTRONIC", 120, 140)]
    [InlineData("Electronic", 120, 140)]
    [InlineData("techno", 120, 140)]
    [InlineData("TECHNO", 120, 140)]
    [InlineData("Techno", 120, 140)]
    public void GetGenreRange_ShouldHandleElectronicTechnoCaseVariations(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, $"{genre} should return electronic/techno BPM range");
        max.Should().Be(expectedMax, $"{genre} should return electronic/techno BPM range");
    }

    [Theory]
    [InlineData("drum and bass", 160, 180)]
    [InlineData("DRUM AND BASS", 160, 180)]
    [InlineData("Drum And Bass", 160, 180)]
    public void GetGenreRange_ShouldHandleDrumAndBassCaseVariations(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, $"{genre} should return drum and bass BPM range");
        max.Should().Be(expectedMax, $"{genre} should return drum and bass BPM range");
    }

    [Theory]
    [InlineData("unknown-genre")]
    [InlineData("metal")]
    [InlineData("classical")]
    [InlineData("ambient")]
    [InlineData("folk")]
    [InlineData("punk")]
    [InlineData("")]
    [InlineData("   ")]
    public void GetGenreRange_ShouldReturnDefaultRange_ForUnknownGenres(string genre)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(40, $"Unknown genre '{genre}' should return default minimum");
        max.Should().Be(250, $"Unknown genre '{genre}' should return default maximum");
    }

    [Fact]
    public void GetGenreRange_ShouldReturnDefaultRange_ForNullGenre()
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(null);

        // Assert
        min.Should().Be(40, "Null genre should return default minimum");
        max.Should().Be(250, "Null genre should return default maximum");
    }

    #endregion

    #region GetBpmGuidance Branch Coverage

    [Fact]
    public void FormatErrorMessage_ShouldIncludeSpecificGuidance_ForCustomRange1()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(60, 180);

        // Act
        var message = attribute.FormatErrorMessage("TestField");

        // Assert
        message.Should().Contain("TestField", "Should include field name");
        message.Should().Contain("60", "Should include minimum BPM");
        message.Should().Contain("180", "Should include maximum BPM");
        message.Should().Contain("Ballads", "Should include specific guidance for 60-180 range");
        message.Should().Contain("Medium tempo", "Should include specific guidance for 60-180 range");
        message.Should().Contain("Up-tempo", "Should include specific guidance for 60-180 range");
        message.Should().Contain("Fast", "Should include specific guidance for 60-180 range");
    }

    [Fact]
    public void FormatErrorMessage_ShouldIncludeSpecificGuidance_ForCustomRange2()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(80, 140);

        // Act
        var message = attribute.FormatErrorMessage("TestField");

        // Assert
        message.Should().Contain("TestField", "Should include field name");
        message.Should().Contain("80", "Should include minimum BPM");
        message.Should().Contain("140", "Should include maximum BPM");
        message.Should().Contain("Slow to medium", "Should include specific guidance for 80-140 range");
        message.Should().Contain("Medium", "Should include specific guidance for 80-140 range");
        message.Should().Contain("Up-tempo", "Should include specific guidance for 80-140 range");
    }

    [Fact]
    public void FormatErrorMessage_ShouldIncludeGenericGuidance_ForUnknownRange()
    {
        // Arrange
        var attribute = new BpmRangeAttribute(50, 200);

        // Act
        var message = attribute.FormatErrorMessage("TestField");

        // Assert
        message.Should().Contain("TestField", "Should include field name");
        message.Should().Contain("50", "Should include minimum BPM");
        message.Should().Contain("200", "Should include maximum BPM");
        message.Should().Contain("Valid range: 50-200", "Should include generic guidance for unknown range");
    }

    #endregion

    #region Rounding Logic Coverage

    [Theory]
    [InlineData(119.4f, true)]  // Rounds to 119, should be valid
    [InlineData(119.6f, true)]  // Rounds to 120, should be valid
    [InlineData(250.4f, true)]  // Rounds to 250, should be valid
    [InlineData(250.6f, false)] // Rounds to 251, should be invalid
    [InlineData(39.4f, false)]  // Rounds to 39, should be invalid
    [InlineData(39.6f, true)]   // Rounds to 40, should be valid
    public void BpmRange_ShouldRoundFloatValues_AndValidateCorrectly(float bpm, bool expectedValid)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().Be(expectedValid, $"Float {bpm} should round and be {(expectedValid ? "valid" : "invalid")}");
    }

    [Theory]
    [InlineData(119.4, true)]   // Rounds to 119, should be valid
    [InlineData(119.6, true)]   // Rounds to 120, should be valid
    [InlineData(250.4, true)]   // Rounds to 250, should be valid
    [InlineData(250.6, false)]  // Rounds to 251, should be invalid
    [InlineData(39.4, false)]   // Rounds to 39, should be invalid
    [InlineData(39.6, true)]    // Rounds to 40, should be valid
    public void BpmRange_ShouldRoundDoubleValues_AndValidateCorrectly(double bpm, bool expectedValid)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().Be(expectedValid, $"Double {bpm} should round and be {(expectedValid ? "valid" : "invalid")}");
    }

    [Theory]
    [InlineData(119.4, true)]   // Rounds to 119, should be valid
    [InlineData(119.6, true)]   // Rounds to 120, should be valid
    [InlineData(250.4, true)]   // Rounds to 250, should be valid
    [InlineData(250.6, false)]  // Rounds to 251, should be invalid
    [InlineData(39.4, false)]   // Rounds to 39, should be invalid
    [InlineData(39.6, true)]    // Rounds to 40, should be valid
    public void BpmRange_ShouldRoundDecimalValues_AndValidateCorrectly(decimal bpm, bool expectedValid)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        result.Should().Be(expectedValid, $"Decimal {bpm} should round and be {(expectedValid ? "valid" : "invalid")}");
    }

    #endregion
}