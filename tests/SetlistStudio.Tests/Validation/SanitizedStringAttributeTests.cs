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

    #region Branch Coverage Enhancement Tests

    [Fact]
    public void ContainsDangerousContent_ShouldReturnFalse_ForNullOrWhitespaceInput()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert - Test null input
        var resultNull = attribute.IsValid(null);
        resultNull.Should().BeTrue("Null input should be valid");

        // Act & Assert - Test whitespace input
        var resultWhitespace = attribute.IsValid("   ");
        resultWhitespace.Should().BeTrue("Whitespace-only input should be valid");

        // Act & Assert - Test empty string
        var resultEmpty = attribute.IsValid("");
        resultEmpty.Should().BeTrue("Empty string should be valid");
    }

    [Fact]
    public void SanitizedString_ShouldRejectDangerousPatterns_InnerHTML()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var input = "test innerHTML manipulation";

        // Act
        var result = attribute.IsValid(input);

        // Assert
        result.Should().BeFalse("String with innerHTML pattern should be rejected");
    }

    [Fact]
    public void SanitizedString_ShouldRejectDangerousPatterns_OuterHTML()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var input = "test outerHTML manipulation";

        // Act
        var result = attribute.IsValid(input);

        // Assert
        result.Should().BeFalse("String with outerHTML pattern should be rejected");
    }

    [Fact]
    public void SanitizedString_ShouldRejectDangerousPatterns_DocumentWrite()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var input = "test document.write attack";

        // Act
        var result = attribute.IsValid(input);

        // Assert
        result.Should().BeFalse("String with document.write pattern should be rejected");
    }

    [Fact]
    public void SanitizedString_ShouldRejectDangerousPatterns_DocumentCookie()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var input = "steal document.cookie data";

        // Act
        var result = attribute.IsValid(input);

        // Assert
        result.Should().BeFalse("String with document.cookie pattern should be rejected");
    }

    [Fact]
    public void SanitizedString_ShouldRejectDangerousPatterns_EvalFunction()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();
        var input = "malicious eval( code execution";

        // Act
        var result = attribute.IsValid(input);

        // Assert
        result.Should().BeFalse("String with eval( pattern should be rejected");
    }

    [Fact]
    public void IsHtmlEncoded_ShouldDetectAllHtmlEntities()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Test different HTML entities
        var inputs = new[]
        {
            "Test &lt;script&gt; encoded",      // Should be detected as HTML encoded
            "Test &gt;tag&lt; encoded",         // Should be detected as HTML encoded  
            "Test &amp; encoded",               // Should be detected as HTML encoded
            "Test &quot;quoted&quot; encoded",  // Should be detected as HTML encoded
            "Test &#39;apostrophe&#39; encoded", // Should be detected as HTML encoded
            "Test &apos;apostrophe&apos; encoded", // Should be detected as HTML encoded
            "Plain text without entities"       // Should NOT be detected as HTML encoded
        };

        // Act & Assert
        for (int i = 0; i < inputs.Length - 1; i++)
        {
            var result = attribute.IsValid(inputs[i]);
            result.Should().BeTrue($"HTML encoded input should be valid: {inputs[i]}");
        }

        // Last input should be plain text and trigger different validation path
        var plainResult = attribute.IsValid(inputs[inputs.Length - 1]);
        plainResult.Should().BeTrue("Plain text should be valid");
    }

    [Fact]
    public void IsSafeForMusicalContent_ShouldRejectScriptTags()
    {
        // Arrange
        var maliciousInput = "Song Title <script>alert('xss')</script>";

        // Act
        var result = SanitizedStringAttribute.IsSafeForMusicalContent(maliciousInput);

        // Assert
        result.Should().BeFalse("Musical content with script tags should be rejected");
    }

    [Fact]
    public void IsSafeForMusicalContent_ShouldRejectJavascriptProtocols()
    {
        // Arrange
        var maliciousInput = "Song javascript:alert('xss') Title";

        // Act
        var result = SanitizedStringAttribute.IsSafeForMusicalContent(maliciousInput);

        // Assert
        result.Should().BeFalse("Musical content with javascript protocol should be rejected");
    }

    [Fact]
    public void IsSafeForMusicalContent_ShouldRejectSqlInjection()
    {
        // Arrange
        var maliciousInput = "Song'; DROP TABLE Songs; --";

        // Act
        var result = SanitizedStringAttribute.IsSafeForMusicalContent(maliciousInput);

        // Assert
        result.Should().BeFalse("Musical content with SQL injection should be rejected");
    }

    [Fact]
    public void IsSafeForMusicalContent_ShouldAcceptValidMusicalContent()
    {
        // Arrange
        var validInputs = new[]
        {
            "Sweet Child O' Mine",
            "Bohemian Rhapsody (Live Version)",
            "Song in C# Major",
            "The Song with \"Quotes\" and 'Apostrophes'",
            "",
            "   "
        };

        // Act & Assert
        foreach (var input in validInputs)
        {
            var result = SanitizedStringAttribute.IsSafeForMusicalContent(input);
            result.Should().BeTrue($"Valid musical content should be accepted: {input}");
        }
    }

    [Fact]
    public void IsSafeForMusicalContent_ShouldAcceptNullInput()
    {
        // Act
        var result = SanitizedStringAttribute.IsSafeForMusicalContent(null!);

        // Assert
        result.Should().BeTrue("Null input should be considered safe for musical content");
    }

    [Fact]
    public void SanitizeInput_ShouldHandleMaxLengthProperty()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute { MaxLength = 10 };
        var longInput = "This is a very long song title that exceeds the maximum length";

        // Act
        var sanitized = attribute.SanitizeInput(longInput);

        // Assert
        sanitized.Length.Should().BeLessOrEqualTo(10, "Sanitized input should respect MaxLength property");
        sanitized.Should().NotBeNullOrWhiteSpace("Sanitized input should not be empty");
    }

    [Fact]
    public void SanitizeInput_ShouldHandleSpecialCharacterFiltering()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute { AllowSpecialCharacters = false };
        var inputWithSpecialChars = "Song@Title#With$Special%Characters";

        // Act
        var sanitized = attribute.SanitizeInput(inputWithSpecialChars);

        // Assert
        sanitized.Should().NotContain("@", "Special characters should be removed");
        sanitized.Should().NotContain("$", "Special characters should be removed");
        sanitized.Should().NotContain("%", "Special characters should be removed");
        sanitized.Should().Contain("Song", "Valid characters should be preserved");
        sanitized.Should().Contain("Title", "Valid characters should be preserved");
    }

    [Fact]
    public void SanitizeInput_ShouldHandleLineBreakFiltering()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute { AllowLineBreaks = false };
        var inputWithLineBreaks = "Song Title\r\nWith Line\rBreaks\nIncluded";

        // Act
        var sanitized = attribute.SanitizeInput(inputWithLineBreaks);

        // Assert
        sanitized.Should().NotContain("\r\n", "Line breaks should be removed");
        sanitized.Should().NotContain("\r", "Line breaks should be removed");
        sanitized.Should().NotContain("\n", "Line breaks should be removed");
        sanitized.Should().Contain("Song Title", "Valid text should be preserved");
        sanitized.Should().Contain("With Line", "Valid text should be preserved");
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

    #region ContainsDangerousContent Branch Coverage Tests

    [Theory]
    [InlineData("<script>", true)]
    [InlineData("<SCRIPT>", true)]
    [InlineData("<Script>", true)]
    [InlineData("<sCrIpT>", true)]
    [InlineData("</script>", true)]
    [InlineData("</SCRIPT>", true)]
    [InlineData("</Script>", true)]
    [InlineData("</sCrIpT>", true)]
    public void ContainsDangerousContent_ShouldDetectScript_AllCaseVariations(string input, bool expected)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().Be(!expected, $"Input '{input}' should be detected as dangerous");
    }

    [Theory]
    [InlineData("javascript:", true)]
    [InlineData("JAVASCRIPT:", true)]
    [InlineData("JavaScript:", true)]
    [InlineData("jAvAsCrIpT:", true)]
    [InlineData("vbscript:", true)]
    [InlineData("VBSCRIPT:", true)]
    [InlineData("VbScript:", true)]
    [InlineData("vBsCrIpT:", true)]
    public void ContainsDangerousContent_ShouldDetectScriptProtocols_AllCaseVariations(string input, bool expected)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().Be(!expected, $"Input '{input}' should be detected as dangerous");
    }

    [Theory]
    [InlineData("onload=", true)]
    [InlineData("ONLOAD=", true)]
    [InlineData("OnLoad=", true)]
    [InlineData("oNlOaD=", true)]
    [InlineData("onclick=", true)]
    [InlineData("ONCLICK=", true)]
    [InlineData("OnClick=", true)]
    [InlineData("oNcLiCk=", true)]
    [InlineData("onerror=", true)]
    [InlineData("ONERROR=", true)]
    [InlineData("OnError=", true)]
    [InlineData("oNeRrOr=", true)]
    public void ContainsDangerousContent_ShouldDetectEventHandlers_AllCaseVariations(string input, bool expected)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().Be(!expected, $"Input '{input}' should be detected as dangerous");
    }

    [Theory]
    [InlineData("eval(", true)]
    [InlineData("EVAL(", true)]
    [InlineData("Eval(", true)]
    [InlineData("eVaL(", true)]
    public void ContainsDangerousContent_ShouldDetectEvalFunction_AllCaseVariations(string input, bool expected)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().Be(!expected, $"Input '{input}' should be detected as dangerous");
    }

    [Theory]
    [InlineData("document.cookie", true)]
    [InlineData("DOCUMENT.COOKIE", true)]
    [InlineData("Document.Cookie", true)]
    [InlineData("document.write", true)]
    [InlineData("DOCUMENT.WRITE", true)]
    [InlineData("Document.Write", true)]
    [InlineData("innerHTML", true)]
    [InlineData("INNERHTML", true)]
    [InlineData("InnerHTML", true)]
    [InlineData("outerHTML", true)]
    [InlineData("OUTERHTML", true)]
    [InlineData("OuterHTML", true)]
    [InlineData("window.location", true)]
    [InlineData("WINDOW.LOCATION", true)]
    [InlineData("Window.Location", true)]
    public void ContainsDangerousContent_ShouldDetectJavaScriptPatterns_AllCaseVariations(string input, bool expected)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().Be(!expected, $"Input '{input}' should be detected as dangerous");
    }

    [Fact] 
    public void ContainsDangerousContent_ShouldDetectScriptTagsWithRegex()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert - Test various script tag patterns
        attribute.IsValid("<script>alert('xss')</script>").Should().BeFalse("Complete script tag should be detected");
        attribute.IsValid("<SCRIPT>alert('xss')</SCRIPT>").Should().BeFalse("Uppercase script tag should be detected");
        attribute.IsValid("<script type='text/javascript'>alert('xss')</script>").Should().BeFalse("Script with attributes should be detected");
        attribute.IsValid("< script >alert('xss')< /script >").Should().BeFalse("Script with extra spaces should be detected");
    }

    [Fact]
    public void ContainsDangerousContent_ShouldDetectJavaScriptProtocolsWithRegex()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert - Test regex patterns that detect on[event]= patterns
        attribute.IsValid("onmouseover=alert()").Should().BeFalse("onmouseover event should be detected");
        attribute.IsValid("onclick=malicious()").Should().BeFalse("onclick event should be detected");
        attribute.IsValid("onfocus=bad()").Should().BeFalse("onfocus event should be detected");
        attribute.IsValid("onblur=evil()").Should().BeFalse("onblur event should be detected");
    }

    [Fact]
    public void ContainsDangerousContent_ShouldDetectSqlInjectionPatternsWithRegex()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert - Test SQL injection patterns
        attribute.IsValid("SELECT * FROM users").Should().BeFalse("SELECT statement should be detected");
        attribute.IsValid("DROP TABLE songs").Should().BeFalse("DROP statement should be detected");
        attribute.IsValid("INSERT INTO users VALUES").Should().BeFalse("INSERT statement should be detected");
        attribute.IsValid("UPDATE users SET").Should().BeFalse("UPDATE statement should be detected");
        attribute.IsValid("DELETE FROM songs").Should().BeFalse("DELETE statement should be detected");
        attribute.IsValid("OR 1=1").Should().BeFalse("Classic SQL injection should be detected");
        attribute.IsValid("AND 1=1").Should().BeFalse("Classic SQL injection should be detected");
        attribute.IsValid("'; DROP TABLE").Should().BeFalse("SQL injection with semicolon should be detected");
        attribute.IsValid("' OR 'a'='a").Should().BeFalse("String SQL injection should be detected");
        attribute.IsValid("UNION SELECT password").Should().BeFalse("UNION SELECT should be detected");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t\t\t")]
    [InlineData("\n\n\n")]
    [InlineData("\r\r\r")]
    [InlineData(" \t\n\r ")]
    public void ContainsDangerousContent_ShouldHandleWhitespaceOnlyInput(string input)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().BeTrue($"Whitespace-only input '{input}' should be considered safe");
    }

    [Theory]
    [InlineData("This is a safe string")]
    [InlineData("Sweet Child O' Mine by Guns N' Roses")]
    [InlineData("BPM: 125, Key: D Major")]
    [InlineData("Normal text with numbers 123 and symbols !@#$%^&*()")]
    [InlineData("Email: user@domain.com")]
    [InlineData("URL: https://example.com/path")]
    public void ContainsDangerousContent_ShouldAllowSafeContent(string input)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().BeTrue($"Safe content '{input}' should be allowed");
    }

    [Fact]
    public void ContainsDangerousContent_ShouldDetectHtmlTags_WhenHtmlNotAllowed()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute { AllowHtml = false };

        // Act & Assert
        attribute.IsValid("<div>content</div>").Should().BeFalse("HTML div tags should be rejected when HTML not allowed");
        attribute.IsValid("<p>paragraph</p>").Should().BeFalse("HTML p tags should be rejected when HTML not allowed");
        attribute.IsValid("<span class='test'>text</span>").Should().BeFalse("HTML span with attributes should be rejected");
        attribute.IsValid("<img src='test.jpg' />").Should().BeFalse("Self-closing HTML tags should be rejected");
    }

    [Fact]
    public void ContainsDangerousContent_ShouldAllowHtmlTags_WhenHtmlAllowed()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute { AllowHtml = true };

        // Act & Assert
        attribute.IsValid("<div>content</div>").Should().BeTrue("HTML div tags should be allowed when HTML allowed");
        attribute.IsValid("<p>paragraph</p>").Should().BeTrue("HTML p tags should be allowed when HTML allowed");
        attribute.IsValid("<span class='test'>text</span>").Should().BeTrue("HTML span with attributes should be allowed");
    }

    [Fact]
    public void ContainsDangerousContent_ShouldAllowHtmlEntities()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute { AllowHtml = false };

        // Act & Assert
        attribute.IsValid("&lt;div&gt;content&lt;/div&gt;").Should().BeTrue("HTML entities should be allowed");
        attribute.IsValid("Rock &amp; Roll").Should().BeTrue("Ampersand entity should be allowed");
        attribute.IsValid("&quot;Song Title&quot;").Should().BeTrue("Quote entity should be allowed");
        attribute.IsValid("Temperature &gt; 100&deg;F").Should().BeTrue("Numeric entity should be allowed");
    }

    [Fact]
    public void ContainsDangerousContent_ShouldRespectMaxLength()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute { MaxLength = 10 };
        var longString = new string('a', 15);

        // Act & Assert
        attribute.IsValid(longString).Should().BeFalse("String exceeding MaxLength should be rejected");
        attribute.IsValid("short").Should().BeTrue("String within MaxLength should be accepted");
    }

    #endregion

    #region ContainsControlCharacters Branch Coverage Tests

    [Theory]
    [InlineData("\x00", true)] // NULL
    [InlineData("\x01", true)] // SOH
    [InlineData("\x02", true)] // STX
    [InlineData("\x03", true)] // ETX
    [InlineData("\x04", true)] // EOT
    [InlineData("\x05", true)] // ENQ
    [InlineData("\x06", true)] // ACK
    [InlineData("\x07", true)] // BEL
    [InlineData("\x08", true)] // BS
    [InlineData("\x09", false)] // TAB (allowed)
    [InlineData("\x0A", false)] // LF (allowed)
    [InlineData("\x0B", true)] // VT
    [InlineData("\x0C", true)] // FF
    [InlineData("\x0D", false)] // CR (allowed)
    [InlineData("\x0E", true)] // SO
    [InlineData("\x0F", true)] // SI
    public void ContainsControlCharacters_ShouldDetectLowAsciiControlChars(string input, bool shouldBeRejected)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().Be(!shouldBeRejected, 
            $"Control character \\x{(int)input[0]:X2} should be {(shouldBeRejected ? "rejected" : "allowed")}");
    }

    [Theory]
    [InlineData("\x10", true)] // DLE
    [InlineData("\x11", true)] // DC1
    [InlineData("\x12", true)] // DC2
    [InlineData("\x13", true)] // DC3
    [InlineData("\x14", true)] // DC4
    [InlineData("\x15", true)] // NAK
    [InlineData("\x16", true)] // SYN
    [InlineData("\x17", true)] // ETB
    [InlineData("\x18", true)] // CAN
    [InlineData("\x19", true)] // EM
    [InlineData("\x1A", true)] // SUB
    [InlineData("\x1B", true)] // ESC
    [InlineData("\x1C", true)] // FS
    [InlineData("\x1D", true)] // GS
    [InlineData("\x1E", true)] // RS
    [InlineData("\x1F", true)] // US
    public void ContainsControlCharacters_ShouldDetectHighAsciiControlChars(string input, bool shouldBeRejected)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().Be(!shouldBeRejected, 
            $"Control character \\x{(int)input[0]:X2} should be {(shouldBeRejected ? "rejected" : "allowed")}");
    }

    [Theory]
    [InlineData("\x7F", true)] // DEL
    [InlineData("\x80", true)] // C1 control start
    [InlineData("\x81", true)]
    [InlineData("\x82", true)]
    [InlineData("\x83", true)]
    [InlineData("\x84", true)]
    [InlineData("\x85", true)]
    [InlineData("\x86", true)]
    [InlineData("\x87", true)]
    [InlineData("\x88", true)]
    [InlineData("\x89", true)]
    [InlineData("\x8A", true)]
    [InlineData("\x8B", true)]
    [InlineData("\x8C", true)]
    [InlineData("\x8D", true)]
    [InlineData("\x8E", true)]
    [InlineData("\x8F", true)]
    public void ContainsControlCharacters_ShouldDetectExtendedControlChars1(string input, bool shouldBeRejected)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().Be(!shouldBeRejected, 
            $"Extended control character \\x{(int)input[0]:X2} should be {(shouldBeRejected ? "rejected" : "allowed")}");
    }

    [Theory]
    [InlineData("\x90", true)]
    [InlineData("\x91", true)]
    [InlineData("\x92", true)]
    [InlineData("\x93", true)]
    [InlineData("\x94", true)]
    [InlineData("\x95", true)]
    [InlineData("\x96", true)]
    [InlineData("\x97", true)]
    [InlineData("\x98", true)]
    [InlineData("\x99", true)]
    [InlineData("\x9A", true)]
    [InlineData("\x9B", true)]
    [InlineData("\x9C", true)]
    [InlineData("\x9D", true)]
    [InlineData("\x9E", true)]
    [InlineData("\x9F", true)] // C1 control end
    public void ContainsControlCharacters_ShouldDetectExtendedControlChars2(string input, bool shouldBeRejected)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().Be(!shouldBeRejected, 
            $"Extended control character \\x{(int)input[0]:X2} should be {(shouldBeRejected ? "rejected" : "allowed")}");
    }

    [Theory]
    [InlineData("Normal text with\ttab")]
    [InlineData("Line 1\nLine 2")]
    [InlineData("Windows line\r\nending")]
    [InlineData("Just carriage\rreturn")]
    [InlineData("Mixed\twhitespace\n\rand text")]
    public void ContainsControlCharacters_ShouldAllowValidWhitespace(string input)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().BeTrue($"Valid whitespace in '{input}' should be allowed");
    }

    [Fact]
    public void ContainsControlCharacters_ShouldRejectMixedContent()
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert - Test only the characters we know should work
        // Test null character first - this should definitely be rejected
        attribute.IsValid("\x00").Should().BeFalse("Pure null character should be rejected");
        
        // Test some safe content first to make sure the attribute works normally
        attribute.IsValid("Normal text").Should().BeTrue("Normal text should be valid");
        
        // Now test control characters in isolation
        attribute.IsValid("\x01").Should().BeFalse("SOH character (0x01) should be rejected");
        attribute.IsValid("\x02").Should().BeFalse("STX character (0x02) should be rejected");
        attribute.IsValid("\x1B").Should().BeFalse("Escape character (0x1B) should be rejected");
        
        // Test DEL character (0x7F)
        attribute.IsValid("\x7F").Should().BeFalse("Delete character (0x7F) should be rejected");
        
        // Test C1 control characters
        attribute.IsValid("\x80").Should().BeFalse("C1 control character (0x80) should be rejected");
        attribute.IsValid("\x9F").Should().BeFalse("C1 control character (0x9F) should be rejected");
    }

    [Theory]
    [InlineData(" ")]    // Space (ASCII 32)
    [InlineData("!")]    // Exclamation (ASCII 33)
    [InlineData("A")]    // Letter A (ASCII 65)
    [InlineData("~")]    // Tilde (ASCII 126)
    [InlineData("€")]    // Euro symbol
    [InlineData("中")]   // Chinese character
    [InlineData("🎵")]   // Musical note emoji
    public void ContainsControlCharacters_ShouldAllowPrintableCharacters(string input)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().BeTrue($"Printable character '{input}' should be allowed");
    }

    #endregion

    #region Combined Validation Coverage

    [Theory]
    [InlineData("<script>\x00alert('xss')</script>", false)] // Both dangerous content and control chars
    [InlineData("javascript:\x07payload", false)] // Protocol with bell character
    [InlineData("onload=\x1Bmalicious", false)] // Event handler with escape
    [InlineData("eval(\x7Fcode)", false)] // Function with delete character
    public void SanitizedString_ShouldRejectCombinedThreats(string input, bool expectedValid)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().Be(expectedValid, 
            $"Combined threat input '{input}' should be {(expectedValid ? "accepted" : "rejected")}");
    }

    [Theory]
    [InlineData("Sweet Child O' Mine", true)]
    [InlineData("BPM: 125\nKey: D Major", true)]
    [InlineData("Artist: Guns N' Roses\tGenre: Rock", true)]
    [InlineData("Setlist notes:\r\n- Start with ballad\r\n- End with rocker", true)]
    public void SanitizedString_ShouldAcceptMusicalContent(string input, bool expectedValid)
    {
        // Arrange
        var attribute = new SanitizedStringAttribute();

        // Act & Assert
        attribute.IsValid(input).Should().Be(expectedValid, 
            $"Musical content '{input}' should be {(expectedValid ? "accepted" : "rejected")}");
    }

    #endregion

    #region Static Method Coverage

    [Fact]
    public void IsSafeForMusicalContent_ShouldDetectDangerousPatterns()
    {
        // Act & Assert
        SanitizedStringAttribute.IsSafeForMusicalContent("<script>alert('xss')</script>").Should().BeFalse("Script tags should be detected");
        SanitizedStringAttribute.IsSafeForMusicalContent("javascript:alert()").Should().BeFalse("JavaScript protocol should be detected");
        SanitizedStringAttribute.IsSafeForMusicalContent("SELECT * FROM users").Should().BeFalse("SQL injection should be detected");
        SanitizedStringAttribute.IsSafeForMusicalContent("Sweet Child O' Mine").Should().BeTrue("Safe musical content should be allowed");
    }

    [Fact]
    public void SanitizeMusicalContent_ShouldReturnSanitizedString()
    {
        // Act & Assert
        var result = SanitizedStringAttribute.SanitizeMusicalContent("Sweet Child O' Mine <script>alert()</script>");
        result.Should().NotContain("<script>", "Script tags should be removed");
        result.Should().Contain("Sweet Child O' Mine", "Safe content should be preserved");
    }

    [Fact]
    public void SanitizeUserNotes_ShouldHandleLongContent()
    {
        // Arrange
        var longContent = new string('a', 3000); // Longer than 2000 char limit

        // Act
        var result = SanitizedStringAttribute.SanitizeUserNotes(longContent);

        // Assert
        result.Length.Should().BeLessOrEqualTo(2000, "Content should be truncated to MaxLength");
    }

    #endregion
}