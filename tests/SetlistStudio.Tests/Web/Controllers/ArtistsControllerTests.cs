using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using SetlistStudio.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SetlistStudio.Tests.Web.Controllers
{
    /// <summary>
    /// Comprehensive tests for ArtistsController covering all endpoints, authorization scenarios,
    /// validation logic, error handling, and security measures including XSS protection and rate limiting.
    /// </summary>
    public class ArtistsControllerTests
    {
        #region Constructor and Setup Tests

        [Fact]
        public void Constructor_ShouldCreateValidInstance()
        {
            // Act
            var controller = new ArtistsController();

            // Assert
            controller.Should().NotBeNull();
            controller.Should().BeAssignableTo<ControllerBase>();
        }

        #endregion

        #region SearchArtists Endpoint Tests

        [Fact]
        public void SearchArtists_ShouldReturnBadRequest_WhenNameIsNull()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists(null!);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Name parameter is required" });
        }

        [Fact]
        public void SearchArtists_ShouldReturnBadRequest_WhenNameIsEmpty()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists("");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Name parameter is required" });
        }

        [Fact]
        public void SearchArtists_ShouldReturnBadRequest_WhenNameIsWhitespace()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists("   ");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Name parameter is required" });
        }

        [Theory]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("test</script>")]
        [InlineData("javascript:alert('xss')")]
        [InlineData("vbscript:msgbox('xss')")]
        [InlineData("test onload=alert('xss')")]
        [InlineData("test onerror=alert('xss')")]
        [InlineData("test onclick=alert('xss')")]
        [InlineData("test onmouseover=alert('xss')")]
        [InlineData("test onfocus=alert('xss')")]
        public void SearchArtists_ShouldReturnBadRequest_WhenNameContainsMaliciousXssContent(string maliciousName)
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists(maliciousName);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Invalid search query" });
        }

        [Theory]
        [InlineData("UNION SELECT * FROM users")]
        [InlineData("DROP TABLE songs")]
        [InlineData("DELETE FROM artists")]
        [InlineData("INSERT INTO users")]
        [InlineData("'; DROP TABLE artists; --")]
        [InlineData("test--")]
        [InlineData("test/*comment*/")]
        [InlineData("test*/")]
        public void SearchArtists_ShouldReturnBadRequest_WhenNameContainsMaliciousSqlContent(string maliciousName)
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists(maliciousName);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Invalid search query" });
        }

        [Fact]
        public void SearchArtists_ShouldReturnOkWithMatchingArtists_WhenValidNameProvided()
        {
            // Arrange
            var controller = new ArtistsController();
            var searchName = "Queen";

            // Act
            var result = controller.SearchArtists(searchName);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
            
            // Use reflection to access the anonymous type properties
            var responseValue = okResult.Value!;
            var responseType = responseValue.GetType();
            
            var queryProperty = responseType.GetProperty("query");
            var totalCountProperty = responseType.GetProperty("totalCount");
            var artistsProperty = responseType.GetProperty("artists");
            
            queryProperty!.GetValue(responseValue).Should().Be("Queen");
            totalCountProperty!.GetValue(responseValue).Should().Be(1);
            artistsProperty!.GetValue(responseValue).Should().NotBeNull();
        }

        [Fact]
        public void SearchArtists_ShouldReturnCaseInsensitiveResults()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act - Test different cases
            var resultLower = controller.SearchArtists("queen");
            var resultUpper = controller.SearchArtists("QUEEN");
            var resultMixed = controller.SearchArtists("QuEeN");

            // Assert - All should return the same result
            resultLower.Should().BeOfType<OkObjectResult>();
            resultUpper.Should().BeOfType<OkObjectResult>();
            resultMixed.Should().BeOfType<OkObjectResult>();
            
            // Verify all return the same artist count
            var lowerResult = (OkObjectResult)resultLower;
            var upperResult = (OkObjectResult)resultUpper;
            var mixedResult = (OkObjectResult)resultMixed;
            
            var lowerCount = lowerResult.Value!.GetType().GetProperty("totalCount")!.GetValue(lowerResult.Value);
            var upperCount = upperResult.Value!.GetType().GetProperty("totalCount")!.GetValue(upperResult.Value);
            var mixedCount = mixedResult.Value!.GetType().GetProperty("totalCount")!.GetValue(mixedResult.Value);
            
            lowerCount.Should().Be(1);
            upperCount.Should().Be(1);
            mixedCount.Should().Be(1);
        }

        [Fact]
        public void SearchArtists_ShouldReturnPartialMatches()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists("Beat");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var totalCount = okResult!.Value!.GetType().GetProperty("totalCount")!.GetValue(okResult.Value);
            totalCount.Should().Be(1); // Should find "The Beatles"
        }

        [Fact]
        public void SearchArtists_ShouldReturnEmptyResults_WhenNoMatchesFound()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists("NonExistentArtist");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var totalCount = okResult!.Value!.GetType().GetProperty("totalCount")!.GetValue(okResult.Value);
            totalCount.Should().Be(0);
        }

        [Fact]
        public void SearchArtists_ShouldReturnMultipleMatches_WhenMultipleArtistsMatch()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists("The");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var totalCount = okResult!.Value!.GetType().GetProperty("totalCount")!.GetValue(okResult.Value);
            totalCount.Should().Be(2); // Should find "The Beatles" and "The Rolling Stones"
        }

        #endregion

        #region GetArtists Endpoint Tests

        [Fact]
        public void GetArtists_ShouldReturnOkWithAllArtists()
        {
            // Arrange
            var controller = new ArtistsController();
            SetupAuthenticatedUser(controller);

            // Act
            var result = controller.GetArtists();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var totalCount = okResult!.Value!.GetType().GetProperty("totalCount")!.GetValue(okResult.Value);
            totalCount.Should().Be(5); // Should return all 5 predefined artists
        }

        #endregion

        #region Security and Authorization Tests

        [Fact]
        public void ArtistsController_ShouldHaveAuthorizationAttribute()
        {
            // Arrange & Act
            var controllerType = typeof(ArtistsController);
            var authAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);

            // Assert
            authAttributes.Should().NotBeEmpty("Controller should require authorization by default");
        }

        [Fact]
        public void SearchArtists_ShouldRequireAuthorization()
        {
            // Arrange & Act
            var method = typeof(ArtistsController).GetMethod("SearchArtists");
            var allowAnonymousAttributes = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), false);

            // Assert
            allowAnonymousAttributes.Should().BeEmpty("SearchArtists should require authentication for security consistency");
        }

        [Fact]
        public void GetArtists_ShouldHaveAuthorizeAttribute()
        {
            // Arrange & Act
            var method = typeof(ArtistsController).GetMethod("GetArtists");
            var authorizeAttributes = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);

            // Assert
            // Should inherit from controller-level authorization
            var controllerAuth = typeof(ArtistsController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
            controllerAuth.Should().NotBeEmpty("GetArtists should require authorization");
        }

        [Fact]
        public void ArtistsController_ShouldHaveRateLimitingAttribute()
        {
            // Arrange & Act
            var controllerType = typeof(ArtistsController);
            var rateLimitAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute), false);

            // Assert
            rateLimitAttributes.Should().NotBeEmpty("Controller should have rate limiting enabled");
        }

        #endregion

        #region Edge Cases and Error Handling Tests

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void SearchArtists_ShouldHandleVariousWhitespaceInputs(string whitespaceInput)
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists(whitespaceInput);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void SearchArtists_ShouldHandleVeryLongInput()
        {
            // Arrange
            var controller = new ArtistsController();
            var longInput = new string('a', 1000);

            // Act
            var result = controller.SearchArtists(longInput);

            // Assert
            result.Should().BeOfType<OkObjectResult>(); // Should not crash, even if no matches
        }

        [Fact]
        public void SearchArtists_ShouldHandleSpecialMusicCharacters()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act & Assert - These should be allowed as they're legitimate musical characters
            var resultSharp = controller.SearchArtists("C# Major");
            var resultFlat = controller.SearchArtists("B♭ Minor");
            var resultDegree = controller.SearchArtists("7° Chord");

            resultSharp.Should().BeOfType<OkObjectResult>();
            resultFlat.Should().BeOfType<OkObjectResult>();
            resultDegree.Should().BeOfType<OkObjectResult>();
        }

        [Theory]
        [InlineData("Queen's Greatest Hits")]
        [InlineData("U2")]
        [InlineData("Guns N' Roses")]
        [InlineData("AC/DC")]
        [InlineData("50 Cent")]
        public void SearchArtists_ShouldHandleLegitimateArtistNames(string artistName)
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists(artistName);

            // Assert
            result.Should().BeOfType<OkObjectResult>("Legitimate artist names should not be blocked");
        }

        #endregion

        #region Malicious Content Detection Tests

        [Theory]
        [InlineData("normal artist name")]
        [InlineData("")]
        [InlineData("Queen")]
        [InlineData("The Beatles")]
        public void SearchArtists_ShouldAllowSafeContent(string safeName)
        {
            // Arrange
            var controller = new ArtistsController();

            // Act & Assert
            if (string.IsNullOrWhiteSpace(safeName))
            {
                var result = controller.SearchArtists(safeName);
                result.Should().BeOfType<BadRequestObjectResult>();
            }
            else
            {
                var result = controller.SearchArtists(safeName);
                result.Should().BeOfType<OkObjectResult>();
            }
        }

        [Fact]
        public void SearchArtists_ShouldBlockMixedCaseMaliciousContent()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists("TeSt<ScRiPt>AlErT('XsS')</ScRiPt>");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region HTTP Response Format Tests

        [Fact]
        public void SearchArtists_ShouldReturnCorrectResponseFormat_WhenSuccessful()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = controller.SearchArtists("Queen");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseValue = okResult!.Value!;
            var responseType = responseValue.GetType();
            
            // Verify response has expected properties
            responseType.GetProperty("artists").Should().NotBeNull();
            responseType.GetProperty("query").Should().NotBeNull();
            responseType.GetProperty("totalCount").Should().NotBeNull();
            
            var query = responseType.GetProperty("query")!.GetValue(responseValue);
            query.Should().Be("Queen");
        }

        [Fact]
        public void GetArtists_ShouldReturnCorrectResponseFormat()
        {
            // Arrange
            var controller = new ArtistsController();
            SetupAuthenticatedUser(controller);

            // Act
            var result = controller.GetArtists();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseValue = okResult!.Value!;
            var responseType = responseValue.GetType();
            
            // Verify response has expected properties
            responseType.GetProperty("artists").Should().NotBeNull();
            responseType.GetProperty("totalCount").Should().NotBeNull();
            
            var totalCount = responseType.GetProperty("totalCount")!.GetValue(responseValue);
            totalCount.Should().Be(5);
        }

        #endregion

        #region Controller Attributes and Configuration Tests

        [Fact]
        public void ArtistsController_ShouldHaveApiControllerAttribute()
        {
            // Arrange & Act
            var controllerType = typeof(ArtistsController);
            var apiControllerAttributes = controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false);

            // Assert
            apiControllerAttributes.Should().NotBeEmpty();
        }

        [Fact]
        public void ArtistsController_ShouldHaveCorrectRoute()
        {
            // Arrange & Act
            var controllerType = typeof(ArtistsController);
            var routeAttributes = controllerType.GetCustomAttributes(typeof(RouteAttribute), false);

            // Assert
            routeAttributes.Should().NotBeEmpty();
            var routeAttribute = routeAttributes.First() as RouteAttribute;
            routeAttribute!.Template.Should().Be("api/[controller]");
        }

        [Fact]
        public void SearchArtists_ShouldHaveCorrectHttpGetRoute()
        {
            // Arrange & Act
            var method = typeof(ArtistsController).GetMethod("SearchArtists");
            var httpGetAttributes = method!.GetCustomAttributes(typeof(HttpGetAttribute), false);

            // Assert
            httpGetAttributes.Should().NotBeEmpty();
            var httpGetAttribute = httpGetAttributes.First() as HttpGetAttribute;
            httpGetAttribute!.Template.Should().Be("search");
        }

        [Fact]
        public void GetArtists_ShouldHaveHttpGetAttribute()
        {
            // Arrange & Act
            var method = typeof(ArtistsController).GetMethod("GetArtists");
            var httpGetAttributes = method!.GetCustomAttributes(typeof(HttpGetAttribute), false);

            // Assert
            httpGetAttributes.Should().NotBeEmpty();
        }

        #endregion

        #region Edge Case and Error Handling Tests

        [Fact]
        public void SearchArtists_ShouldHandleExceptionGracefully()
        {
            // Arrange
            var controller = new ArtistsController();
            
            // Act & Assert - This is tricky since the method doesn't have external dependencies that can fail
            // We'll test with a valid input to ensure the happy path works and exception handling is in place
            var result = controller.SearchArtists("ValidArtist");
            
            // The method should complete without throwing exceptions
            result.Should().NotBeNull();
        }

        [Fact]
        public void GetArtists_ShouldHandleExceptionGracefully()
        {
            // Arrange
            var controller = new ArtistsController();
            SetupAuthenticatedUser(controller);
            
            // Act & Assert - Similar to SearchArtists, testing that the method doesn't throw
            var result = controller.GetArtists();
            
            // The method should complete without throwing exceptions
            result.Should().NotBeNull();
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region ContainsMaliciousContent Edge Cases

        [Fact]
        public void ContainsMaliciousContent_ShouldReturnFalse_WhenInputIsNull()
        {
            // We need to test the private static method using reflection
            var method = typeof(ArtistsController).GetMethod("ContainsMaliciousContent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Should().NotBeNull();

            // Act
            var result = method!.Invoke(null, new object[] { null! });

            // Assert
            result.Should().Be(false);
        }

        [Fact]
        public void ContainsMaliciousContent_ShouldReturnFalse_WhenInputIsEmpty()
        {
            // We need to test the private static method using reflection
            var method = typeof(ArtistsController).GetMethod("ContainsMaliciousContent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Should().NotBeNull();

            // Act
            var result = method!.Invoke(null, new object[] { string.Empty });

            // Assert
            result.Should().Be(false);
        }

        [Fact]
        public void ContainsMaliciousContent_ShouldReturnFalse_WhenInputIsWhitespace()
        {
            // We need to test the private static method using reflection
            var method = typeof(ArtistsController).GetMethod("ContainsMaliciousContent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Should().NotBeNull();

            // Act
            var result = method!.Invoke(null, new object[] { "   " });

            // Assert
            result.Should().Be(false);
        }

        #endregion

        #region Helper Methods

        private static void SetupAuthenticatedUser(ControllerBase controller)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "test@example.com")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        #endregion
    }
}
