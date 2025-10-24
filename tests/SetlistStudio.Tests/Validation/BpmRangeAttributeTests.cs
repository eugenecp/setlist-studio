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

    #region TryConvertToInt Coverage Tests

    [Theory]
    [InlineData(42)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void BpmRange_ShouldHandleIntegerValues(int bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        var expected = bpm >= 40 && bpm <= 250;
        result.Should().Be(expected, $"Integer {bpm} should be {(expected ? "valid" : "invalid")}");
    }

    [Theory]
    [InlineData(42L)]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData((long)int.MaxValue)]
    [InlineData((long)int.MinValue)]
    [InlineData((long)int.MaxValue + 1L)]
    [InlineData((long)int.MinValue - 1L)]
    public void BpmRange_ShouldHandleLongValues(long bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        if (bpm >= int.MinValue && bpm <= int.MaxValue)
        {
            var expected = bpm >= 40 && bpm <= 250;
            result.Should().Be(expected, $"Long {bpm} should be {(expected ? "valid" : "invalid")}");
        }
        else
        {
            result.Should().BeFalse($"Long {bpm} outside int range should be invalid");
        }
    }

    [Theory]
    [InlineData(42.0f)]
    [InlineData(42.7f)]
    [InlineData(42.3f)]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.NaN)]
    public void BpmRange_ShouldHandleFloatValues(float bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        if (float.IsFinite(bpm) && bpm >= int.MinValue && bpm <= int.MaxValue)
        {
            var roundedBpm = (int)Math.Round(bpm);
            var expected = roundedBpm >= 40 && roundedBpm <= 250;
            result.Should().Be(expected, $"Float {bpm} (rounded to {roundedBpm}) should be {(expected ? "valid" : "invalid")}");
        }
        else
        {
            result.Should().BeFalse($"Float {bpm} should be invalid");
        }
    }

    [Theory]
    [InlineData(42.0)]
    [InlineData(42.7)]
    [InlineData(42.3)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void BpmRange_ShouldHandleDoubleValues(double bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        if (double.IsFinite(bpm) && bpm >= int.MinValue && bpm <= int.MaxValue)
        {
            var roundedBpm = (int)Math.Round(bpm);
            var expected = roundedBpm >= 40 && roundedBpm <= 250;
            result.Should().Be(expected, $"Double {bpm} (rounded to {roundedBpm}) should be {(expected ? "valid" : "invalid")}");
        }
        else
        {
            result.Should().BeFalse($"Double {bpm} should be invalid");
        }
    }

    [Theory]
    [InlineData("42")]
    [InlineData("42.7")]
    [InlineData("42.3")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData(" 42 ")]
    [InlineData("\t120\n")]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("42.5.6")]
    [InlineData("abc123")]
    public void BpmRange_ShouldHandleStringValues(string bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        if (int.TryParse(bpm.Trim(), out int parsedBpm))
        {
            var expected = parsedBpm >= 40 && parsedBpm <= 250;
            result.Should().Be(expected, $"String '{bpm}' (parsed to {parsedBpm}) should be {(expected ? "valid" : "invalid")}");
        }
        else
        {
            result.Should().BeFalse($"String '{bpm}' that cannot be parsed should be invalid");
        }
    }

    [Theory]
    [InlineData(42.0)]
    [InlineData(42.7)]
    [InlineData(42.3)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void BpmRange_ShouldHandleDecimalValues(decimal bpm)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(bpm);

        // Assert
        if (bpm >= int.MinValue && bpm <= int.MaxValue)
        {
            var roundedBpm = (int)Math.Round(bpm);
            var expected = roundedBpm >= 40 && roundedBpm <= 250;
            result.Should().Be(expected, $"Decimal {bpm} (rounded to {roundedBpm}) should be {(expected ? "valid" : "invalid")}");
        }
        else
        {
            result.Should().BeFalse($"Decimal {bpm} outside int range should be invalid");
        }
    }

    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    public void BpmRange_ShouldRejectUnsupportedTypes(Type type)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();
        var testValue = type == typeof(object) ? new object() :
                       type == typeof(DateTime) ? DateTime.Now :
                       type == typeof(Guid) ? Guid.NewGuid() : null;

        // Act
        var result = attribute.IsValid(testValue);

        // Assert
        result.Should().BeFalse($"Type {type.Name} should be rejected");
    }

    #endregion

    #region Custom Range Constructor Coverage

    [Theory]
    [InlineData(60, 180)]
    [InlineData(80, 140)]
    [InlineData(100, 200)]
    [InlineData(1, 2)]
    [InlineData(1, 1000)]
    public void BpmRange_ShouldAcceptValidCustomRanges(int min, int max)
    {
        // Act & Assert
        var attribute = new BpmRangeAttribute(min, max);
        attribute.MinimumBpm.Should().Be(min);
        attribute.MaximumBpm.Should().Be(max);
        attribute.ErrorMessage.Should().Contain($"{min}").And.Contain($"{max}");
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(100, 99)]
    [InlineData(250, 40)]
    [InlineData(0, -1)]
    public void BpmRange_ShouldThrowArgumentException_ForInvalidRanges(int min, int max)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new BpmRangeAttribute(min, max));
        exception.Message.Should().Contain("Minimum BPM must be less than maximum BPM");
        exception.ParamName.Should().Be("minimumBpm");
    }

    #endregion

    #region FormatErrorMessage Coverage

    [Theory]
    [InlineData("BPM")]
    [InlineData("Tempo")]
    [InlineData("BeatsPerMinute")]
    [InlineData("Song.Bpm")]
    public void FormatErrorMessage_ShouldIncludeFieldName(string fieldName)
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var message = attribute.FormatErrorMessage(fieldName);

        // Assert
        message.Should().Contain(fieldName);
        message.Should().Contain("40");
        message.Should().Contain("250");
        message.Should().Contain("Very slow ballads");
    }

    [Theory]
    [InlineData(60, 180, "Ballads")]
    [InlineData(80, 140, "medium")]
    [InlineData(100, 200, "Valid range")]
    public void FormatErrorMessage_ShouldIncludeGuidanceForCustomRanges(int min, int max, string expectedGuidance)
    {
        // Arrange
        var attribute = new BpmRangeAttribute(min, max);

        // Act
        var message = attribute.FormatErrorMessage("TestField");

        // Assert
        message.Should().Contain("TestField");
        message.Should().Contain($"{min}");
        message.Should().Contain($"{max}");
        message.Should().Contain(expectedGuidance);
    }

    #endregion

    #region GetGenreRange Method Tests

    [Theory]
    [InlineData("funk", 90, 120)]
    [InlineData("FUNK", 90, 120)]
    [InlineData("Funk", 90, 120)]
    public void GetGenreRange_ShouldReturnCorrectRange_ForFunk(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, "Funk should have 90-120 BPM range");
        max.Should().Be(expectedMax, "Funk should have 90-120 BPM range");
    }

    [Theory]
    [InlineData("reggae", 60, 90)]
    [InlineData("REGGAE", 60, 90)]
    [InlineData("Reggae", 60, 90)]
    public void GetGenreRange_ShouldReturnCorrectRange_ForReggae(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, "Reggae should have 60-90 BPM range");
        max.Should().Be(expectedMax, "Reggae should have 60-90 BPM range");
    }

    [Theory]
    [InlineData("country", 80, 140)]
    [InlineData("COUNTRY", 80, 140)]
    [InlineData("Country", 80, 140)]
    public void GetGenreRange_ShouldReturnCorrectRange_ForCountry(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, "Country should have 80-140 BPM range");
        max.Should().Be(expectedMax, "Country should have 80-140 BPM range");
    }

    [Theory]
    [InlineData("electronic", 120, 140)]
    [InlineData("ELECTRONIC", 120, 140)]
    [InlineData("Electronic", 120, 140)]
    [InlineData("techno", 120, 140)]
    [InlineData("TECHNO", 120, 140)]
    [InlineData("Techno", 120, 140)]
    public void GetGenreRange_ShouldReturnCorrectRange_ForElectronicAndTechno(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, $"{genre} should have 120-140 BPM range");
        max.Should().Be(expectedMax, $"{genre} should have 120-140 BPM range");
    }

    [Theory]
    [InlineData("house", 115, 130)]
    [InlineData("HOUSE", 115, 130)]
    [InlineData("House", 115, 130)]
    public void GetGenreRange_ShouldReturnCorrectRange_ForHouse(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, "House should have 115-130 BPM range");
        max.Should().Be(expectedMax, "House should have 115-130 BPM range");
    }

    [Theory]
    [InlineData("trance", 130, 140)]
    [InlineData("TRANCE", 130, 140)]
    [InlineData("Trance", 130, 140)]
    public void GetGenreRange_ShouldReturnCorrectRange_ForTrance(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, "Trance should have 130-140 BPM range");
        max.Should().Be(expectedMax, "Trance should have 130-140 BPM range");
    }

    [Theory]
    [InlineData("drum and bass", 160, 180)]
    [InlineData("DRUM AND BASS", 160, 180)]
    [InlineData("Drum And Bass", 160, 180)]
    public void GetGenreRange_ShouldReturnCorrectRange_ForDrumAndBass(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, "Drum and Bass should have 160-180 BPM range");
        max.Should().Be(expectedMax, "Drum and Bass should have 160-180 BPM range");
    }

    [Theory]
    [InlineData("dubstep", 140, 150)]
    [InlineData("DUBSTEP", 140, 150)]
    [InlineData("Dubstep", 140, 150)]
    public void GetGenreRange_ShouldReturnCorrectRange_ForDubstep(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, "Dubstep should have 140-150 BPM range");
        max.Should().Be(expectedMax, "Dubstep should have 140-150 BPM range");
    }

    [Theory]
    [InlineData("unknown genre")]
    [InlineData("metal")]
    [InlineData("classical")]
    [InlineData("folk")]
    [InlineData("")]
    [InlineData(null)]
    public void GetGenreRange_ShouldReturnDefaultRange_ForUnknownGenres(string? genre)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(40, "Unknown genres should use default minimum BPM");
        max.Should().Be(250, "Unknown genres should use default maximum BPM");
    }

    [Fact]
    public void GetGenreRange_ShouldHandleNullInput()
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(null);

        // Assert
        min.Should().Be(40, "Null genre should use default minimum BPM");
        max.Should().Be(250, "Null genre should use default maximum BPM");
    }

    [Fact]
    public void GetGenreRange_ShouldHandleEmptyString()
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange("");

        // Assert
        min.Should().Be(40, "Empty genre should use default minimum BPM");
        max.Should().Be(250, "Empty genre should use default maximum BPM");
    }

    [Fact]
    public void GetGenreRange_ShouldHandleWhitespaceString()
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange("   ");

        // Assert
        min.Should().Be(40, "Whitespace-only genre should use default minimum BPM");
        max.Should().Be(250, "Whitespace-only genre should use default maximum BPM");
    }

    [Theory]
    [InlineData("slow", 60, 80)]
    [InlineData("SLOW", 60, 80)]
    [InlineData("Slow", 60, 80)]
    public void GetGenreRange_ShouldReturnCorrectRange_ForSlowAlias(string genre, int expectedMin, int expectedMax)
    {
        // Act
        var (min, max) = BpmRangeAttribute.GetGenreRange(genre);

        // Assert
        min.Should().Be(expectedMin, "Slow should be alias for ballad (60-80 BPM)");
        max.Should().Be(expectedMax, "Slow should be alias for ballad (60-80 BPM)");
    }

    #endregion

    #region GetTempoDescription Method Tests

    [Theory]
    [InlineData(50, "Very Slow")]
    [InlineData(70, "Slow")]
    [InlineData(90, "Moderate")]
    [InlineData(110, "Medium")]
    [InlineData(130, "Up-tempo")]
    [InlineData(150, "Fast")]
    [InlineData(170, "Very Fast")]
    [InlineData(200, "Extremely Fast")]
    public void GetTempoDescription_ShouldReturnCorrectDescription_ForAllBpmRanges(int bpm, string expectedDescription)
    {
        // Act
        var description = BpmRangeAttribute.GetTempoDescription(bpm);

        // Assert
        description.Should().Be(expectedDescription, $"BPM {bpm} should be classified as {expectedDescription}");
    }

    [Theory]
    [InlineData(59)]   // Just below 60
    [InlineData(79)]   // Just below 80
    [InlineData(99)]   // Just below 100
    [InlineData(119)]  // Just below 120
    [InlineData(139)]  // Just below 140
    [InlineData(159)]  // Just below 160
    [InlineData(179)]  // Just below 180
    public void GetTempoDescription_ShouldHandleBoundaryValues(int bpm)
    {
        // Act
        var description = BpmRangeAttribute.GetTempoDescription(bpm);

        // Assert
        description.Should().NotBeNullOrEmpty($"BPM {bpm} should have a valid tempo description");
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

    [Fact]
    public void BpmRange_ShouldHandleNullValue()
    {
        // Arrange
        var attribute = new BpmRangeAttribute();

        // Act
        var result = attribute.IsValid(null);

        // Assert
        result.Should().BeTrue("Null values should be considered valid (use [Required] for mandatory validation)");
    }

    #endregion
}