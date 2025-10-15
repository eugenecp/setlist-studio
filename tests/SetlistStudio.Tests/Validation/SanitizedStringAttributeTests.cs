using FluentAssertions;
using SetlistStudio.Core.Validation;
using Xunit;

namespace SetlistStudio.Tests.Validation;

/// <summary>
/// Comprehensive tests for the SanitizedStringAttribute validation
/// Tests XSS prevention, input sanitization, and security measures
/// </summary>
public class SanitizedStringAttributeTests
{
    #region Basic Validation Tests

    [Fact]
    public void SanitizedString_ShouldAcceptCleanString_AsValid()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var cleanString = "Sweet Child O' Mine";

        // Act
        var result = attribute.IsValid(cleanString);

        // Assert
        result.Should().BeTrue("Clean string should be valid after sanitization");
    }

    [Fact]
    public void SanitizedString_ShouldAcceptNull_AsValid()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(null);

        // Assert
        result.Should().BeTrue("Null values should be valid (optional field)");
    }

    [Fact]
    public void SanitizedString_ShouldAcceptEmptyString_AsValid()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid("");

        // Assert
        result.Should().BeTrue("Empty string should be valid after sanitization");
    }

    [Fact]
    public void SanitizedString_ShouldAcceptWhitespace_AsValid()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid("   ");

        // Assert
        result.Should().BeTrue("Whitespace should be valid after sanitization");
    }

    #endregion

    #region XSS Prevention Tests

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<SCRIPT>alert('xss')</SCRIPT>")]
    [InlineData("<Script>alert('xss')</Script>")]
    [InlineData("< script >alert('xss')< / script >")]
    [InlineData("<script type=\"text/javascript\">alert('xss')</script>")]
    [InlineData("<script src=\"evil.js\"></script>")]
    public void SanitizedString_ShouldRejectScriptTags(string maliciousInput)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(maliciousInput);

        // Assert
        result.Should().BeFalse($"Script tag '{maliciousInput}' should be rejected");
    }

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("JAVASCRIPT:alert('xss')")]
    [InlineData("JavaScript:alert('xss')")]
    [InlineData("  javascript:  alert('xss')")]
    [InlineData("vbscript:msgbox('xss')")]
    [InlineData("data:text/html,<script>alert('xss')</script>")]
    public void SanitizedString_ShouldRejectJavaScriptProtocols(string maliciousInput)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(maliciousInput);

        // Assert
        result.Should().BeFalse($"JavaScript protocol '{maliciousInput}' should be rejected");
    }

    [Theory]
    [InlineData("<img src=\"x\" onerror=\"alert('xss')\">")]
    [InlineData("<div onclick=\"alert('xss')\">")]
    [InlineData("<a href=\"#\" onmouseover=\"alert('xss')\">")]
    [InlineData("<input onfocus=\"alert('xss')\">")]
    [InlineData("<body onload=\"alert('xss')\">")]
    public void SanitizedString_ShouldRejectEventHandlers(string maliciousInput)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(maliciousInput);

        // Assert
        result.Should().BeFalse($"Event handler '{maliciousInput}' should be rejected");
    }

    [Theory]
    [InlineData("<iframe src=\"javascript:alert('xss')\">")]
    [InlineData("<object data=\"javascript:alert('xss')\">")]
    [InlineData("<embed src=\"javascript:alert('xss')\">")]
    [InlineData("<form action=\"javascript:alert('xss')\">")]
    [InlineData("<link href=\"javascript:alert('xss')\">")]
    public void SanitizedString_ShouldRejectDangerousTags(string maliciousInput)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(maliciousInput);

        // Assert
        result.Should().BeFalse($"Dangerous tag '{maliciousInput}' should be rejected");
    }

    #endregion

    #region SQL Injection Prevention Tests

    [Theory]
    [InlineData("'; DROP TABLE Songs; --")]
    [InlineData("' OR '1'='1")]
    [InlineData("1; DELETE FROM Songs")]
    [InlineData("UNION SELECT * FROM Users")]
    [InlineData("'; INSERT INTO Songs")]
    [InlineData("' UNION ALL SELECT")]
    public void SanitizedString_ShouldRejectSqlInjectionPatterns(string maliciousInput)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(maliciousInput);

        // Assert
        result.Should().BeFalse($"SQL injection pattern '{maliciousInput}' should be rejected");
    }

    [Theory]
    [InlineData("It's a beautiful day")]    // Apostrophe in normal text
    [InlineData("Rock & Roll")]             // Ampersand in normal text
    [InlineData("Song #1")]                 // Hash in normal text
    [InlineData("BPM: 120-130")]           // Dash in normal text
    public void SanitizedString_ShouldAcceptNormalPunctuation(string normalInput)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(normalInput);

        // Assert
        result.Should().BeTrue($"Normal punctuation '{normalInput}' should be accepted");
    }

    #endregion

    #region HTML Encoding Tests

    [Theory]
    [InlineData("<b>Bold Text</b>")]
    [InlineData("<i>Italic Text</i>")]
    [InlineData("<u>Underlined Text</u>")]
    [InlineData("<em>Emphasized Text</em>")]
    [InlineData("<strong>Strong Text</strong>")]
    public void SanitizedString_ShouldRejectHtmlTags_EvenSafe(string htmlInput)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(htmlInput);

        // Assert
        result.Should().BeFalse($"HTML tag '{htmlInput}' should be rejected for consistency");
    }

    [Theory]
    [InlineData("&lt;script&gt;")]    // Encoded script
    [InlineData("&amp;")]             // Encoded ampersand
    [InlineData("&quot;")]            // Encoded quote
    [InlineData("&#39;")]             // Encoded apostrophe
    public void SanitizedString_ShouldAcceptHtmlEncodedContent(string encodedInput)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(encodedInput);

        // Assert
        result.Should().BeTrue($"HTML encoded content '{encodedInput}' should be accepted");
    }

    #endregion

    #region Musical Content Tests

    [Theory]
    [InlineData("Bohemian Rhapsody")]
    [InlineData("Sweet Child O' Mine")]
    [InlineData("Stairway to Heaven")]
    [InlineData("Hotel California")]
    [InlineData("Born to Run")]
    public void SanitizedString_ShouldAcceptCommonSongTitles(string songTitle)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(songTitle);

        // Assert
        result.Should().BeTrue($"Song title '{songTitle}' should be accepted");
    }

    [Theory]
    [InlineData("The Beatles")]
    [InlineData("Led Zeppelin")]
    [InlineData("Queen")]
    [InlineData("Pink Floyd")]
    [InlineData("The Rolling Stones")]
    public void SanitizedString_ShouldAcceptCommonArtistNames(string artistName)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(artistName);

        // Assert
        result.Should().BeTrue($"Artist name '{artistName}' should be accepted");
    }

    [Theory]
    [InlineData("Rock")]
    [InlineData("Pop")]
    [InlineData("Jazz")]
    [InlineData("Blues")]
    [InlineData("Classical")]
    [InlineData("Electronic")]
    [InlineData("Country")]
    [InlineData("Hip-Hop")]
    [InlineData("R&B")]
    public void SanitizedString_ShouldAcceptMusicGenres(string genre)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(genre);

        // Assert
        result.Should().BeTrue($"Music genre '{genre}' should be accepted");
    }

    #endregion

    #region Unicode and Special Characters Tests

    [Theory]
    [InlineData("Café")]                    // Accented characters
    [InlineData("Naïve")]                   // Diaeresis
    [InlineData("Résumé")]                  // Multiple accents
    [InlineData("Piñata")]                  // Tilde
    [InlineData("Señor")]                   // Ñ character
    [InlineData("Мир")]                     // Cyrillic
    [InlineData("平和")]                    // Japanese
    [InlineData("שלום")]                    // Hebrew
    [InlineData("السلام")]                  // Arabic
    public void SanitizedString_ShouldAcceptUnicodeCharacters(string unicodeInput)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(unicodeInput);

        // Assert
        result.Should().BeTrue($"Unicode text '{unicodeInput}' should be accepted");
    }

    [Theory]
    [InlineData("♪ Musical Note")]          // Musical symbols
    [InlineData("♫ ♬ ♩")]                  // Multiple musical symbols
    [InlineData("© 2023")]                  // Copyright symbol
    [InlineData("™ Trademark")]            // Trademark symbol
    [InlineData("® Registered")]           // Registered symbol
    public void SanitizedString_ShouldAcceptSpecialSymbols(string symbolInput)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(symbolInput);

        // Assert
        result.Should().BeTrue($"Special symbols '{symbolInput}' should be accepted");
    }

    #endregion

    #region Length Validation Tests

    [Fact]
    public void SanitizedString_ShouldUseDefaultMaxLength_WhenNotSpecified()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Assert
        attribute.MaxLength.Should().Be(-1, "Default max length should be -1 (unlimited)");
    }

    [Fact]
    public void SanitizedString_ShouldAcceptLongString_WhenNoMaxLengthSet()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var longString = new string('A', 2000);

        // Act
        var result = attribute.IsValid(longString);

        // Assert
        result.Should().BeTrue("Long string should be accepted when no max length is set");
    }

    #endregion

    #region Type Validation Tests

    [Fact]
    public void SanitizedString_ShouldRejectNonStringType()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(123);

        // Assert
        result.Should().BeFalse("Non-string type should be rejected");
    }

    [Fact]
    public void SanitizedString_ShouldRejectBooleanType()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(true);

        // Assert
        result.Should().BeFalse("Boolean type should be rejected");
    }

    [Fact]
    public void SanitizedString_ShouldRejectArrayType()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var array = new[] { "test" };

        // Act
        var result = attribute.IsValid(array);

        // Assert
        result.Should().BeFalse("Array type should be rejected");
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void FormatErrorMessage_ShouldIncludeFieldName()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var message = attribute.FormatErrorMessage("SongTitle");

        // Assert
        message.Should().Contain("SongTitle", "Error message should include field name");
    }

    [Fact]
    public void FormatErrorMessage_ShouldMentionSecurity()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var message = attribute.FormatErrorMessage("Input");

        // Assert
        message.Should().Contain("security", "Error message should mention security");
    }

    [Fact]
    public void FormatErrorMessage_ShouldProvideSecurityGuidance()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var message = attribute.FormatErrorMessage("Description");

        // Assert
        message.Should().Contain("dangerous", "Error message should mention dangerous content");
    }

    [Fact]
    public void FormatErrorMessage_ShouldProvideHelpfulGuidance()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var message = attribute.FormatErrorMessage("Notes");

        // Assert
        message.Should().Contain("HTML", "Should mention HTML restriction");
        message.Should().Contain("script", "Should mention script restriction");
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("\r\n")]           // CRLF
    [InlineData("\n")]             // LF
    [InlineData("\r")]             // CR  
    [InlineData("\t")]             // Tab
    public void SanitizedString_ShouldAcceptWhitespaceCharacters(string whitespace)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act
        var result = attribute.IsValid(whitespace);

        // Assert
        result.Should().BeTrue($"Whitespace character should be accepted");
    }

    [Fact]
    public void SanitizedString_ShouldHandleVeryLongMaliciousString()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var longMaliciousString = string.Concat(Enumerable.Repeat("<script>alert('xss')</script>", 100));

        // Act
        var result = attribute.IsValid(longMaliciousString);

        // Assert
        result.Should().BeFalse("Long malicious string should be rejected");
    }

    [Fact]
    public void SanitizedString_ShouldRejectControlCharacters_NullByte()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var input = "test" + (char)0 + "null";

        // Act
        var result = attribute.IsValid(input);

        // Assert
        result.Should().BeFalse("String with null byte should be rejected");
    }

    [Fact]
    public void SanitizedString_ShouldRejectControlCharacters_StartOfHeading()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var input = "test" + (char)1 + "control";

        // Act
        var result = attribute.IsValid(input);

        // Assert
        result.Should().BeFalse("String with control character should be rejected");
    }

    [Fact]
    public void SanitizedString_ShouldRejectControlCharacters_UnitSeparator()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var input = "test" + (char)31 + "control";

        // Act
        var result = attribute.IsValid(input);

        // Assert
        result.Should().BeFalse("String with unit separator should be rejected");
    }

    #endregion

    #region Performance Edge Cases

    [Fact]
    public void SanitizedString_ShouldHandleRepeatedValidation_Efficiently()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var input = "Sweet Child O' Mine by Guns N' Roses";

        // Act & Assert - Should not throw or timeout
        for (int i = 0; i < 1000; i++)
        {
            var result = attribute.IsValid(input);
            result.Should().BeTrue();
        }
    }

    [Fact]
    public void SanitizedString_ShouldHandleComplexPattern_Efficiently()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var complexInput = string.Concat(Enumerable.Repeat("Valid Text ", 100));

        // Act
        var result = attribute.IsValid(complexInput);

        // Assert
        result.Should().BeTrue("Complex valid input should be processed efficiently");
    }

    #endregion
}