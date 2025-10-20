using FluentAssertions;
using SetlistStudio.Core.Validation;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace SetlistStudio.Tests.Core.Validation
{
    /// <summary>
    /// Advanced tests for SanitizedStringAttribute focusing on edge cases, 
    /// error conditions, and validation boundaries to achieve 90%+ branch coverage.
    /// 
    /// These tests complement the base SanitizedStringAttributeTests.cs by targeting
    /// specific uncovered branches and complex validation scenarios.
    /// </summary>
    public class SanitizedStringAttributeAdvancedTests
    {
        [Fact]
        public void IsValid_ShouldReturnTrue_ForComplexValidMusic()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var validMusic = "Don't Stop Believin' - Journey (Key: E, BPM: 119)";

            // Act
            var result = attribute.IsValid(validMusic);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForUnicodeCharacters()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var unicodeInput = "Café del Mar - José Padilla";

            // Act
            var result = attribute.IsValid(unicodeInput);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForScriptTagWithAttributes()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var maliciousInput = "<script type=\"text/javascript\">alert('xss')</script>";

            // Act
            var result = attribute.IsValid(maliciousInput);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForJavascriptProtocol()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var maliciousInput = "javascript:alert('xss')";

            // Act
            var result = attribute.IsValid(maliciousInput);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForSqlInjectionPattern()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var maliciousInput = "'; DROP TABLE Songs; --";

            // Act
            var result = attribute.IsValid(maliciousInput);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForControlCharacters()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var inputWithControlChars = "Song Title\u000B"; // Vertical tab

            // Act
            var result = attribute.IsValid(inputWithControlChars);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForFormFeedCharacter()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var inputWithFormFeed = "Song Title\f"; // Form feed

            // Act
            var result = attribute.IsValid(inputWithFormFeed);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("<iframe>")]
        [InlineData("<object>")]
        [InlineData("<embed>")]
        [InlineData("<applet>")]
        public void IsValid_ShouldReturnFalse_ForDangerousHtmlTags(string dangerousTag)
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var maliciousInput = $"Song Title {dangerousTag}";

            // Act
            var result = attribute.IsValid(maliciousInput);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("onload")]
        [InlineData("onerror")]
        [InlineData("onmouseover")]
        [InlineData("onfocus")]
        public void IsValid_ShouldReturnFalse_ForEventHandlers(string eventHandler)
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var maliciousInput = $"Song Title {eventHandler}=alert(1)";

            // Act
            var result = attribute.IsValid(maliciousInput);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForSafeHtmlEntities()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var safeInput = "Rock &amp; Roll by AC/DC";

            // Act
            var result = attribute.IsValid(safeInput);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForMathematicalSymbols()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var mathInput = "BPM = 120 ± 5";

            // Act
            var result = attribute.IsValid(mathInput);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForSpecialMusicNotation()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var musicInput = "Time Signature: 4/4, Key: C♯ major";

            // Act
            var result = attribute.IsValid(musicInput);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForMixedDangerousContent()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var maliciousInput = "<script>alert('xss')</script> AND 1=1 --";

            // Act
            var result = attribute.IsValid(maliciousInput);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForLongValidSongTitle()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var longTitle = new string('A', 500) + " - Artist Name";

            // Act
            var result = attribute.IsValid(longTitle);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForEncodedScriptTag()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var encodedScript = "%3Cscript%3Ealert('xss')%3C/script%3E";

            // Act
            var result = attribute.IsValid(encodedScript);

            // Assert
            result.Should().BeTrue("URL-encoded content should be considered safe as it doesn't match literal patterns");
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForNumbersAndPunctuation()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var input = "Track #1: Song Title (2023) - 3:45 duration";

            // Act
            var result = attribute.IsValid(input);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForDataUri()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var dataUri = "data:text/html,<script>alert('xss')</script>";

            // Act
            var result = attribute.IsValid(dataUri);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForVbscriptProtocol()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var vbscriptInput = "vbscript:msgbox('xss')";

            // Act
            var result = attribute.IsValid(vbscriptInput);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForRegularWebUrl()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var url = "https://www.music-site.com/song/bohemian-rhapsody";

            // Act
            var result = attribute.IsValid(url);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForHexEncodedScript()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var hexScript = "\\x3cscript\\x3ealert('xss')\\x3c/script\\x3e";

            // Act
            var result = attribute.IsValid(hexScript);

            // Assert
            result.Should().BeTrue("Hex-encoded content should be considered safe as it doesn't match literal patterns");
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForQuotedSongLyrics()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var lyrics = "\"Don't stop me now\" - Queen";

            // Act
            var result = attribute.IsValid(lyrics);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForComplexSqlInjection()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var sqlInjection = "' UNION SELECT password FROM users WHERE '1'='1";

            // Act
            var result = attribute.IsValid(sqlInjection);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForEmoticons()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var emoticonInput = "Happy Song 😊🎵";

            // Act
            var result = attribute.IsValid(emoticonInput);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForXssWithEventAttribute()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var xssInput = "<img src=x onerror=alert('xss')>";

            // Act
            var result = attribute.IsValid(xssInput);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForValidJsonString()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var jsonInput = "{\"title\": \"Song Name\", \"artist\": \"Artist Name\"}";

            // Act
            var result = attribute.IsValid(jsonInput);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForMultilineScriptInjection()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var multilineScript = "Song\n<script>\nalert('xss')\n</script>";

            // Act
            var result = attribute.IsValid(multilineScript);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForRegularNewlines()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var multilineInput = "Song Title\nArtist Name\nAlbum: Greatest Hits";

            // Act
            var result = attribute.IsValid(multilineInput);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForCaseSensitiveScriptTag()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var scriptVariant = "<SCRIPT>alert('xss')</SCRIPT>";

            // Act
            var result = attribute.IsValid(scriptVariant);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForFilePathWithoutDangerousContent()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var filePath = "C:\\Music\\Queen\\Bohemian Rhapsody.mp3";

            // Act
            var result = attribute.IsValid(filePath);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForNullByteInjection()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var nullByteInput = "Song Title\0<script>alert('xss')</script>";

            // Act
            var result = attribute.IsValid(nullByteInput);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForMusicTheoryNotation()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var theoryInput = "Chord progression: I-V-vi-IV in C major";

            // Act
            var result = attribute.IsValid(theoryInput);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidationContext_ShouldProvidePropertyName()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var testObject = new { Title = "<script>alert('xss')</script>" };
            var context = new ValidationContext(testObject) 
            { 
                MemberName = "Title" 
            };

            // Act
            var result = attribute.GetValidationResult("<script>alert('xss')</script>", context);

            // Assert
            result.Should().NotBeNull();
            result!.ErrorMessage.Should().Contain("Title");
        }

        [Fact]
        public void ValidationContext_ShouldHandleMissingPropertyName()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var testObject = new { };
            var context = new ValidationContext(testObject);

            // Act
            var result = attribute.GetValidationResult("<script>alert('xss')</script>", context);

            // Assert
            result.Should().NotBeNull();
            result!.ErrorMessage.Should().NotBeNull();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_ForComplexMixedInjection()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var complexInjection = "Song' OR 1=1; <script>alert('xss')</script> --";

            // Act
            var result = attribute.IsValid(complexInjection);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("\u0001")] // Start of heading
        [InlineData("\u0002")] // Start of text
        [InlineData("\u0003")] // End of text
        [InlineData("\u0004")] // End of transmission
        [InlineData("\u0005")] // Enquiry
        [InlineData("\u0006")] // Acknowledge
        [InlineData("\u0007")] // Bell
        [InlineData("\u0008")] // Backspace
        [InlineData("\u000E")] // Shift out
        [InlineData("\u000F")] // Shift in
        [InlineData("\u0010")] // Data link escape
        [InlineData("\u0011")] // Device control 1
        [InlineData("\u0012")] // Device control 2
        [InlineData("\u0013")] // Device control 3
        [InlineData("\u0014")] // Device control 4
        [InlineData("\u0015")] // Negative acknowledge
        [InlineData("\u0016")] // Synchronous idle
        [InlineData("\u0017")] // End of transmission block
        [InlineData("\u0018")] // Cancel
        [InlineData("\u0019")] // End of medium
        [InlineData("\u001A")] // Substitute
        [InlineData("\u001B")] // Escape
        [InlineData("\u001C")] // File separator
        [InlineData("\u001D")] // Group separator
        [InlineData("\u001E")] // Record separator
        [InlineData("\u001F")] // Unit separator
        [InlineData("\u007F")] // Delete
        public void IsValid_ShouldReturnFalse_ForAllControlCharacters(string controlChar)
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var inputWithControlChar = $"Song Title{controlChar}";

            // Act
            var result = attribute.IsValid(inputWithControlChar);

            // Assert
            result.Should().BeFalse($"control character \\u{((int)controlChar[0]):X4} should be detected as dangerous");
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_ForAllowedWhitespaceCharacters()
        {
            // Arrange
            var attribute = new SanitizedStringAttribute();
            var inputWithWhitespace = "Song Title\t\r\n Artist Name"; // Tab, CR, LF

            // Act
            var result = attribute.IsValid(inputWithWhitespace);

            // Assert
            result.Should().BeTrue("allowed whitespace characters should be valid");
        }
    }
}