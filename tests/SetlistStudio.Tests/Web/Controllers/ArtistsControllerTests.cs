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
        public async Task SearchArtists_ShouldReturnBadRequest_WhenNameIsNull()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists(null!);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Name parameter is required" });
        }

        [Fact]
        public async Task SearchArtists_ShouldReturnBadRequest_WhenNameIsEmpty()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists("");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Name parameter is required" });
        }

        [Fact]
        public async Task SearchArtists_ShouldReturnBadRequest_WhenNameIsWhitespace()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists("   ");

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
        public async Task SearchArtists_ShouldReturnBadRequest_WhenNameContainsMaliciousXssContent(string maliciousName)
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists(maliciousName);

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
        public async Task SearchArtists_ShouldReturnBadRequest_WhenNameContainsMaliciousSqlContent(string maliciousName)
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists(maliciousName);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Invalid search query" });
        }

        [Fact]
        public async Task SearchArtists_ShouldReturnOkWithMatchingArtists_WhenValidNameProvided()
        {
            // Arrange
            var controller = new ArtistsController();
            var searchName = "Queen";

            // Act
            var result = await controller.SearchArtists(searchName);

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
        public async Task SearchArtists_ShouldReturnCaseInsensitiveResults()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act - Test different cases
            var resultLower = await controller.SearchArtists("queen");
            var resultUpper = await controller.SearchArtists("QUEEN");
            var resultMixed = await controller.SearchArtists("QuEeN");

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
        public async Task SearchArtists_ShouldReturnPartialMatches()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists("Beat");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var totalCount = okResult!.Value!.GetType().GetProperty("totalCount")!.GetValue(okResult.Value);
            totalCount.Should().Be(1); // Should find "The Beatles"
        }

        [Fact]
        public async Task SearchArtists_ShouldReturnEmptyResults_WhenNoMatchesFound()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists("NonExistentArtist");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var totalCount = okResult!.Value!.GetType().GetProperty("totalCount")!.GetValue(okResult.Value);
            totalCount.Should().Be(0);
        }

        [Fact]
        public async Task SearchArtists_ShouldReturnMultipleMatches_WhenMultipleArtistsMatch()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists("The");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var totalCount = okResult!.Value!.GetType().GetProperty("totalCount")!.GetValue(okResult.Value);
            totalCount.Should().Be(2); // Should find "The Beatles" and "The Rolling Stones"
        }

        #endregion

        #region GetArtists Endpoint Tests

        [Fact]
        public async Task GetArtists_ShouldReturnOkWithAllArtists()
        {
            // Arrange
            var controller = new ArtistsController();
            SetupAuthenticatedUser(controller);

            // Act
            var result = await controller.GetArtists();

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
        public void SearchArtists_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange & Act
            var method = typeof(ArtistsController).GetMethod("SearchArtists");
            var allowAnonymousAttributes = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), false);

            // Assert
            allowAnonymousAttributes.Should().NotBeEmpty("SearchArtists should allow anonymous access for testing");
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
        public async Task SearchArtists_ShouldHandleVariousWhitespaceInputs(string whitespaceInput)
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists(whitespaceInput);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task SearchArtists_ShouldHandleVeryLongInput()
        {
            // Arrange
            var controller = new ArtistsController();
            var longInput = new string('a', 1000);

            // Act
            var result = await controller.SearchArtists(longInput);

            // Assert
            result.Should().BeOfType<OkObjectResult>(); // Should not crash, even if no matches
        }

        [Fact]
        public async Task SearchArtists_ShouldHandleSpecialMusicCharacters()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act & Assert - These should be allowed as they're legitimate musical characters
            var resultSharp = await controller.SearchArtists("C# Major");
            var resultFlat = await controller.SearchArtists("B♭ Minor");
            var resultDegree = await controller.SearchArtists("7° Chord");

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
        public async Task SearchArtists_ShouldHandleLegitimateArtistNames(string artistName)
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists(artistName);

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
        public async Task SearchArtists_ShouldAllowSafeContent(string safeName)
        {
            // Arrange
            var controller = new ArtistsController();

            // Act & Assert
            if (string.IsNullOrWhiteSpace(safeName))
            {
                var result = await controller.SearchArtists(safeName);
                result.Should().BeOfType<BadRequestObjectResult>();
            }
            else
            {
                var result = await controller.SearchArtists(safeName);
                result.Should().BeOfType<OkObjectResult>();
            }
        }

        [Fact]
        public async Task SearchArtists_ShouldBlockMixedCaseMaliciousContent()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists("TeSt<ScRiPt>AlErT('XsS')</ScRiPt>");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region HTTP Response Format Tests

        [Fact]
        public async Task SearchArtists_ShouldReturnCorrectResponseFormat_WhenSuccessful()
        {
            // Arrange
            var controller = new ArtistsController();

            // Act
            var result = await controller.SearchArtists("Queen");

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
        public async Task GetArtists_ShouldReturnCorrectResponseFormat()
        {
            // Arrange
            var controller = new ArtistsController();
            SetupAuthenticatedUser(controller);

            // Act
            var result = await controller.GetArtists();

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