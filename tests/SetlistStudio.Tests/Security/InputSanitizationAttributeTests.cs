using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using SetlistStudio.Web.Security;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace SetlistStudio.Tests.Security
{
    /// <summary>
    /// Comprehensive unit tests for InputSanitizationAttribute.
    /// Tests input sanitization, XSS prevention, and log injection protection.
    /// Ensures proper handling of various input types and edge cases.
    /// </summary>
    public class InputSanitizationAttributeTests
    {
        private readonly InputSanitizationAttribute _attribute;

        public InputSanitizationAttributeTests()
        {
            _attribute = new InputSanitizationAttribute();
        }

        #region OnActionExecuting Tests

        [Fact]
        public void OnActionExecuting_ShouldSanitizeStringParameters()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            context.ActionArguments["title"] = "Sweet Child O' Mine <script>alert('xss')</script>";
            context.ActionArguments["artist"] = "Guns N' Roses";

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            context.ActionArguments["title"].Should().BeOfType<string>();
            var sanitizedTitle = (string)context.ActionArguments["title"]!;
            sanitizedTitle.Should().NotContain("<script>", "Script tags should be sanitized");
            sanitizedTitle.Should().Contain("Sweet Child O' Mine", "Safe content should be preserved");
            
            context.ActionArguments["artist"].Should().Be("Guns N' Roses");
        }

        [Fact]
        public void OnActionExecuting_ShouldHandleNullParameters()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            context.ActionArguments["nullValue"] = null;
            context.ActionArguments["validValue"] = "Test";

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            context.ActionArguments["nullValue"].Should().BeNull();
            context.ActionArguments["validValue"].Should().Be("Test");
        }

        [Fact]
        public void OnActionExecuting_ShouldSanitizeComplexObjects()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            var song = new TestSong
            {
                Title = "Bohemian Rhapsody <script>alert()</script>",
                Artist = "Queen",
                Bpm = 72
            };
            context.ActionArguments["song"] = song;

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            var sanitizedSong = (TestSong)context.ActionArguments["song"]!;
            sanitizedSong.Title.Should().NotContain("<script>", "Script tags should be sanitized from complex objects");
            sanitizedSong.Title.Should().Contain("Bohemian Rhapsody", "Safe content should be preserved");
            sanitizedSong.Artist.Should().Be("Queen");
            sanitizedSong.Bpm.Should().Be(72);
        }

        [Fact]
        public void OnActionExecuting_ShouldSanitizeCollections()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            var songs = new List<string>
            {
                "Billie Jean",
                "Take Five <script>alert('xss')</script>",
                "The Thrill Is Gone"
            };
            context.ActionArguments["songs"] = songs;

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            var sanitizedSongs = (List<object>)context.ActionArguments["songs"]!;
            sanitizedSongs.Should().HaveCount(3);
            sanitizedSongs[0].Should().Be("Billie Jean");
            var maliciousSong = (string)sanitizedSongs[1];
            maliciousSong.Should().NotContain("<script>", "Script tags should be sanitized from collections");
            maliciousSong.Should().Contain("Take Five", "Safe content should be preserved");
            sanitizedSongs[2].Should().Be("The Thrill Is Gone");
        }

        [Fact]
        public void OnActionExecuting_ShouldPreserveValueTypes()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            context.ActionArguments["bpm"] = 125;
            context.ActionArguments["isActive"] = true;
            context.ActionArguments["rating"] = 4.5f;
            context.ActionArguments["timestamp"] = DateTime.Now;

            var originalBpm = context.ActionArguments["bpm"];
            var originalIsActive = context.ActionArguments["isActive"];
            var originalRating = context.ActionArguments["rating"];
            var originalTimestamp = context.ActionArguments["timestamp"];

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            context.ActionArguments["bpm"].Should().Be(originalBpm);
            context.ActionArguments["isActive"].Should().Be(originalIsActive);
            context.ActionArguments["rating"].Should().Be(originalRating);
            context.ActionArguments["timestamp"].Should().Be(originalTimestamp);
        }

        [Fact]
        public void OnActionExecuting_ShouldReturnBadRequest_WhenValidationFails()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            context.ActionArguments["song"] = new TestSong { Title = "Test" };
            
            // Add model state error to simulate validation failure
            context.ModelState.AddModelError("Title", "Title is required");

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            context.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)context.Result!;
            badRequestResult.Value.Should().NotBeNull();
        }

        [Fact]
        public void OnActionExecuting_ShouldContinueProcessing_WhenValidationPasses()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            context.ActionArguments["song"] = new TestSong 
            { 
                Title = "Sweet Child O' Mine", 
                Artist = "Guns N' Roses" 
            };

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            context.Result.Should().BeNull("Should continue processing when validation passes");
        }

        #endregion

        #region SanitizeObject Edge Cases

        [Fact]
        public void OnActionExecuting_ShouldHandleEmptyCollections()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            context.ActionArguments["emptyList"] = new List<string>();
            context.ActionArguments["emptyArray"] = new string[0];

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            var sanitizedList = (List<object>)context.ActionArguments["emptyList"]!;
            sanitizedList.Should().BeEmpty();
            
            var sanitizedArray = (List<object>)context.ActionArguments["emptyArray"]!;
            sanitizedArray.Should().BeEmpty();
        }

        [Fact]
        public void OnActionExecuting_ShouldHandleNestedObjects()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            var setlist = new TestSetlist
            {
                Name = "Rock Concert <script>alert()</script>",
                Songs = new List<TestSong>
                {
                    new TestSong 
                    { 
                        Title = "Sweet Child O' Mine <img src=x onerror=alert()>", 
                        Artist = "Guns N' Roses" 
                    }
                }
            };
            context.ActionArguments["setlist"] = setlist;

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            var sanitizedSetlist = (TestSetlist)context.ActionArguments["setlist"]!;
            sanitizedSetlist.Name.Should().NotContain("<script>", "Nested object properties should be sanitized");
            sanitizedSetlist.Name.Should().Contain("Rock Concert", "Safe content should be preserved");
            
            sanitizedSetlist.Songs.Should().HaveCount(1);
            
            var song = sanitizedSetlist.Songs.First();
            song.Title.Should().NotContain("<img", "XSS vectors should be sanitized from nested collections");
            song.Title.Should().Contain("Sweet Child O", "Safe content should be preserved");
            song.Title.Should().Contain("Mine", "Safe content should be preserved");
        }

        [Fact]
        public void OnActionExecuting_ShouldHandleComplexMusicalData()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            var musicalData = new TestMusicalData
            {
                SongTitle = "Stairway to Heaven",
                Key = "A minor",
                ChordProgression = "Am-C-D-F-G",
                Lyrics = "There's a lady who's sure\nAll that glitters is gold",
                Notes = "Start slow, build to powerful climax\r\nUse Gibson Les Paul",
                BpmRange = "70-80 BPM ballad tempo"
            };
            context.ActionArguments["musicalData"] = musicalData;

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            var sanitized = (TestMusicalData)context.ActionArguments["musicalData"]!;
            sanitized.SongTitle.Should().Be("Stairway to Heaven");
            sanitized.Key.Should().Be("A minor");
            sanitized.ChordProgression.Should().Be("Am-C-D-F-G");
            sanitized.Lyrics.Should().Contain("There's a lady who's sure");
            sanitized.Notes.Should().Contain("Start slow, build to powerful climax");
            sanitized.BpmRange.Should().Be("70-80 BPM ballad tempo");
        }

        [Fact]
        public void OnActionExecuting_ShouldSanitizeXssVectors()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            var maliciousInputs = new Dictionary<string, string>
            {
                ["script"] = "<script>alert('xss')</script>",
                ["img"] = "<img src=x onerror=alert()>",
                ["svg"] = "<svg onload=alert()>",
                ["javascript"] = "javascript:alert('xss')",
                ["vbscript"] = "vbscript:msgbox('xss')",
                ["onmouseover"] = "<div onmouseover=alert()>test</div>",
                ["iframe"] = "<iframe src=javascript:alert()></iframe>"
            };
            context.ActionArguments["maliciousData"] = maliciousInputs;

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            var sanitized = (Dictionary<string, string>)context.ActionArguments["maliciousData"]!;
            foreach (var kvp in sanitized)
            {
                var value = kvp.Value;
                value.Should().NotContain("<script>", $"Script tags should be sanitized from {kvp.Key}");
                value.Should().NotContain("javascript:", $"JavaScript protocol should be sanitized from {kvp.Key}");
                value.Should().NotContain("vbscript:", $"VBScript protocol should be sanitized from {kvp.Key}");
                value.Should().NotContain("onload=", $"Event handlers should be sanitized from {kvp.Key}");
                value.Should().NotContain("onerror=", $"Error handlers should be sanitized from {kvp.Key}");
                value.Should().NotContain("onmouseover=", $"Mouse event handlers should be sanitized from {kvp.Key}");
            }
        }

        [Fact]
        public void OnActionExecuting_ShouldHandleReadOnlyProperties()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            var objWithReadOnly = new TestObjectWithReadOnlyProperty("Initial Value");
            context.ActionArguments["readOnlyObject"] = objWithReadOnly;

            // Act & Assert - Should not throw exception
            Action act = () => _attribute.OnActionExecuting(context);
            act.Should().NotThrow("Should handle read-only properties gracefully");
            
            // The read-only property should remain unchanged
            var result = (TestObjectWithReadOnlyProperty)context.ActionArguments["readOnlyObject"]!;
            result.ReadOnlyValue.Should().Be("Initial Value");
        }

        [Fact]
        public void OnActionExecuting_ShouldHandleUnsanitizableObjects()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            var stream = new MemoryStream(); // An object that can't be easily sanitized
            context.ActionArguments["stream"] = stream;
            context.ActionArguments["title"] = "Sweet Child O' Mine";

            // Act & Assert - Should not throw exception even with unsanitizable objects
            Action act = () => _attribute.OnActionExecuting(context);
            act.Should().NotThrow("Should handle unsanitizable objects gracefully");
            
            // The string should still be processed
            context.ActionArguments["title"].Should().Be("Sweet Child O' Mine");
        }

        #endregion

        #region SQL Injection Protection Tests

        [Fact]
        public void OnActionExecuting_ShouldSanitizeSqlInjectionAttempts()
        {
            // Arrange
            var context = CreateActionExecutingContext();
            var maliciousSql = new Dictionary<string, string>
            {
                ["union"] = "1' UNION SELECT * FROM users--",
                ["drop"] = "'; DROP TABLE songs; --",
                ["insert"] = "1'; INSERT INTO users (admin) VALUES (1); --",
                ["update"] = "1'; UPDATE users SET admin=1; --",
                ["delete"] = "1'; DELETE FROM songs; --"
            };
            context.ActionArguments["sqlData"] = maliciousSql;

            // Act
            _attribute.OnActionExecuting(context);

            // Assert
            var sanitized = (Dictionary<string, string>)context.ActionArguments["sqlData"]!;
            foreach (var kvp in sanitized)
            {
                var value = kvp.Value;
                // The secure logging helper should sanitize SQL patterns
                value.Should().NotContain("UNION SELECT", $"SQL injection patterns should be sanitized from {kvp.Key}");
                value.Should().NotContain("DROP TABLE", $"SQL injection patterns should be sanitized from {kvp.Key}");
                value.Should().NotContain("INSERT INTO", $"SQL injection patterns should be sanitized from {kvp.Key}");
                value.Should().NotContain("UPDATE ", $"SQL injection patterns should be sanitized from {kvp.Key}");
                value.Should().NotContain("DELETE FROM", $"SQL injection patterns should be sanitized from {kvp.Key}");
            }
        }

        #endregion

        #region Helper Methods

        private static ActionExecutingContext CreateActionExecutingContext()
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary()
            );

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller: null!
            );
        }

        #endregion

        #region Test Data Classes

        public class TestSong
        {
            public string Title { get; set; } = string.Empty;
            public string Artist { get; set; } = string.Empty;
            public int Bpm { get; set; }
        }

        public class TestSetlist
        {
            public string Name { get; set; } = string.Empty;
            public ICollection<TestSong> Songs { get; set; } = new List<TestSong>();
        }

        public class TestMusicalData
        {
            public string SongTitle { get; set; } = string.Empty;
            public string Key { get; set; } = string.Empty;
            public string ChordProgression { get; set; } = string.Empty;
            public string Lyrics { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
            public string BpmRange { get; set; } = string.Empty;
        }

        public class TestObjectWithReadOnlyProperty
        {
            public TestObjectWithReadOnlyProperty(string readOnlyValue)
            {
                ReadOnlyValue = readOnlyValue;
            }

            public string ReadOnlyValue { get; }
            public string WritableValue { get; set; } = string.Empty;
        }

        public class TestParent
        {
            public string Name { get; set; } = string.Empty;
            public TestChild? Child { get; set; }
        }

        public class TestChild
        {
            public string Name { get; set; } = string.Empty;
            public TestParent? Parent { get; set; }
        }

        #endregion
    }
}

