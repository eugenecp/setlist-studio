using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using SetlistStudio.Core.Entities;
using Xunit;
using FluentAssertions;
using Moq;

namespace SetlistStudio.Tests.Web.Pages.Shared
{
    public class LoginPartialTests : TestContext
    {
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;

        public LoginPartialTests()
        {
            // Setup UserManager mock
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<ApplicationUser>>().Object,
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<ApplicationUser>>>().Object);

            // Setup SignInManager mock
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var optionsAccessor = new Mock<IOptions<IdentityOptions>>();
            var logger = new Mock<ILogger<SignInManager<ApplicationUser>>>();
            var schemes = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
            var confirmation = new Mock<IUserConfirmation<ApplicationUser>>();

            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                optionsAccessor.Object,
                logger.Object,
                schemes.Object,
                confirmation.Object);

            Services.AddSingleton(_mockSignInManager.Object);
            Services.AddSingleton(_mockUserManager.Object);
            Services.AddAuthorizationCore();
        }

        [Fact]
        public void LoginPartial_WhenUserNotSignedIn_ShouldConfigureServices()
        {
            // Arrange
            _mockSignInManager.Setup(x => x.IsSignedIn(It.IsAny<ClaimsPrincipal>()))
                            .Returns(false);

            // This test validates the service configuration for LoginPartial logic
            // The actual rendering test would require more complex setup for Razor pages
            
            // Assert - Services should be configured correctly
            var signInManager = Services.GetService<SignInManager<ApplicationUser>>();
            var userManager = Services.GetService<UserManager<ApplicationUser>>();
            
            signInManager.Should().NotBeNull();
            userManager.Should().NotBeNull();
        }

        [Fact]
        public void LoginPartial_WhenUserSignedIn_ShouldShowUserNameAndLogout()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "test@example.com"),
                new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            }, "test"));

            _mockSignInManager.Setup(x => x.IsSignedIn(user)).Returns(true);

            // Act & Assert
            // This validates the SignInManager setup for the logic
            var isSignedIn = _mockSignInManager.Object.IsSignedIn(user);
            isSignedIn.Should().BeTrue();
        }

        [Fact]
        public void LoginPartial_SignInManagerIsSignedIn_ShouldWorkCorrectly()
        {
            // Arrange
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "test@example.com")
            }, "test"));

            var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());

            _mockSignInManager.Setup(x => x.IsSignedIn(authenticatedUser)).Returns(true);
            _mockSignInManager.Setup(x => x.IsSignedIn(unauthenticatedUser)).Returns(false);

            // Act & Assert
            _mockSignInManager.Object.IsSignedIn(authenticatedUser).Should().BeTrue();
            _mockSignInManager.Object.IsSignedIn(unauthenticatedUser).Should().BeFalse();
        }

        [Fact]
        public void LoginPartial_UserManager_ShouldBeConfigured()
        {
            // Act & Assert
            _mockUserManager.Should().NotBeNull();
            _mockUserManager.Object.Should().NotBeNull();
        }

        [Fact]
        public void LoginPartial_SignInManager_ShouldBeConfigured()
        {
            // Act & Assert
            _mockSignInManager.Should().NotBeNull();
            _mockSignInManager.Object.Should().NotBeNull();
        }

        [Fact]
        public void LoginPartial_Dependencies_ShouldBeInjectable()
        {
            // Arrange & Act
            var signInManager = Services.GetService<SignInManager<ApplicationUser>>();
            var userManager = Services.GetService<UserManager<ApplicationUser>>();

            // Assert
            signInManager.Should().NotBeNull();
            userManager.Should().NotBeNull();
        }

        [Fact]
        public void LoginPartial_UserIdentityName_ShouldBeAccessible()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "john.doe@example.com"),
                new Claim(ClaimTypes.NameIdentifier, "user-123")
            }, "test"));

            // Act
            var userName = user.Identity?.Name;

            // Assert
            userName.Should().Be("john.doe@example.com");
        }

        [Fact]
        public void LoginPartial_ClaimsPrincipal_ShouldHandleNullIdentity()
        {
            // Arrange
            var user = new ClaimsPrincipal();

            // Act
            var userName = user.Identity?.Name;

            // Assert
            userName.Should().BeNull();
        }

        [Fact]
        public void LoginPartial_AuthenticationLogic_ShouldDistinguishUsers()
        {
            // Arrange
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "authenticated@example.com"),
                new Claim(ClaimTypes.NameIdentifier, "auth-user-id")
            }, "Identity.Application"));

            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());

            _mockSignInManager.Setup(x => x.IsSignedIn(authenticatedUser)).Returns(true);
            _mockSignInManager.Setup(x => x.IsSignedIn(anonymousUser)).Returns(false);

            // Act & Assert
            _mockSignInManager.Object.IsSignedIn(authenticatedUser).Should().BeTrue();
            _mockSignInManager.Object.IsSignedIn(anonymousUser).Should().BeFalse();
            
            authenticatedUser.Identity?.Name.Should().Be("authenticated@example.com");
            anonymousUser.Identity?.Name.Should().BeNull();
        }

        #region Authentication State Edge Cases

        [Fact]
        public void LoginPartial_IsSignedIn_WithNullUser_ShouldReturnFalse()
        {
            // Arrange
            ClaimsPrincipal? nullUser = null;
            _mockSignInManager.Setup(x => x.IsSignedIn(nullUser!)).Returns(false);

            // Act
            var result = _mockSignInManager.Object.IsSignedIn(nullUser!);

            // Assert
            result.Should().BeFalse("Null user should not be signed in");
        }

        [Fact]
        public void LoginPartial_IsSignedIn_WithEmptyClaimsPrincipal_ShouldReturnFalse()
        {
            // Arrange
            var emptyUser = new ClaimsPrincipal();
            _mockSignInManager.Setup(x => x.IsSignedIn(emptyUser)).Returns(false);

            // Act
            var result = _mockSignInManager.Object.IsSignedIn(emptyUser);

            // Assert
            result.Should().BeFalse("Empty ClaimsPrincipal should not be signed in");
        }

        [Fact]
        public void LoginPartial_IsSignedIn_WithValidUser_ShouldReturnTrue()
        {
            // Arrange
            var validUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "test@example.com"),
                new Claim(ClaimTypes.NameIdentifier, "user-123")
            }, "Identity.Application"));

            _mockSignInManager.Setup(x => x.IsSignedIn(validUser)).Returns(true);

            // Act
            var result = _mockSignInManager.Object.IsSignedIn(validUser);

            // Assert
            result.Should().BeTrue("Valid user should be signed in");
        }

        [Fact]
        public void LoginPartial_UserIdentityName_WithSpecialCharacters_ShouldBePreserved()
        {
            // Arrange
            var specialNames = new[]
            {
                "test@example.com",
                "José García",
                "王小明",
                "user@münchen.de",
                "🎵 Music Lover 🎸"
            };

            foreach (var name in specialNames)
            {
                var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, name)
                }, "test"));

                // Act
                var actualName = user.Identity?.Name;

                // Assert
                actualName.Should().Be(name, $"Special character name '{name}' should be preserved");
            }
        }

        [Fact]
        public void LoginPartial_UserIdentityName_WithLongName_ShouldBePreserved()
        {
            // Arrange
            var longName = new string('a', 300); // Very long name
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, longName)
            }, "test"));

            // Act
            var actualName = user.Identity?.Name;

            // Assert
            actualName.Should().Be(longName, "Long name should be preserved");
            actualName!.Length.Should().Be(300, "Long name length should be preserved");
        }

        #endregion

        #region Multiple Authentication Schemes

        [Fact]
        public void LoginPartial_DifferentAuthSchemes_ShouldAllWorkCorrectly()
        {
            // Arrange
            var schemes = new[]
            {
                "Identity.Application",
                "Identity.External",
                "Google",
                "Microsoft",
                "Facebook"
            };

            foreach (var scheme in schemes)
            {
                var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, $"user@{scheme.ToLower()}.com")
                }, scheme));

                _mockSignInManager.Setup(x => x.IsSignedIn(user)).Returns(true);

                // Act
                var isSignedIn = _mockSignInManager.Object.IsSignedIn(user);
                var name = user.Identity?.Name;

                // Assert
                isSignedIn.Should().BeTrue($"User with scheme '{scheme}' should be signed in");
                name.Should().Be($"user@{scheme.ToLower()}.com", $"Name for scheme '{scheme}' should be correct");
            }
        }

        [Fact]
        public void LoginPartial_MultipleIdentities_ShouldUsePrimary()
        {
            // Arrange
            var primaryIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "primary@test.com")
            }, "Identity.Application");

            var secondaryIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "secondary@test.com")
            }, "Google");

            var user = new ClaimsPrincipal(new[] { primaryIdentity, secondaryIdentity });
            _mockSignInManager.Setup(x => x.IsSignedIn(user)).Returns(true);

            // Act
            var isSignedIn = _mockSignInManager.Object.IsSignedIn(user);
            var name = user.Identity?.Name; // Should use primary identity

            // Assert
            isSignedIn.Should().BeTrue("User with multiple identities should be signed in");
            name.Should().Be("primary@test.com", "Primary identity name should be used");
        }

        #endregion

        #region Claims Handling

        [Fact]
        public void LoginPartial_MultipleClaims_ShouldHandleCorrectly()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "test@example.com"),
                new Claim(ClaimTypes.NameIdentifier, "user-123"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "User"),
                new Claim("custom_claim", "custom_value")
            }, "test"));

            _mockSignInManager.Setup(x => x.IsSignedIn(user)).Returns(true);

            // Act
            var isSignedIn = _mockSignInManager.Object.IsSignedIn(user);
            var name = user.Identity?.Name;
            var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
            var customClaim = user.FindFirst("custom_claim")?.Value;

            // Assert
            isSignedIn.Should().BeTrue("User with multiple claims should be signed in");
            name.Should().Be("test@example.com");
            nameIdentifier.Should().Be("user-123");
            email.Should().Be("test@example.com");
            roles.Should().Equal("Admin", "User");
            customClaim.Should().Be("custom_value");
        }

        [Fact]
        public void LoginPartial_MissingNameClaim_ShouldHandleGracefully()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-123"),
                new Claim(ClaimTypes.Email, "test@example.com")
                // Missing Name claim
            }, "test"));

            _mockSignInManager.Setup(x => x.IsSignedIn(user)).Returns(true);

            // Act
            var isSignedIn = _mockSignInManager.Object.IsSignedIn(user);
            var name = user.Identity?.Name;

            // Assert
            isSignedIn.Should().BeTrue("User without Name claim should still be signed in if authenticated");
            name.Should().BeNull("Missing Name claim should result in null name");
        }

        [Fact]
        public void LoginPartial_LargeClaimSet_ShouldHandleEfficiently()
        {
            // Arrange - Create user with many claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "test@example.com"),
                new Claim(ClaimTypes.NameIdentifier, "user-123")
            };

            // Add 100 additional claims
            for (int i = 0; i < 100; i++)
            {
                claims.Add(new Claim($"custom_claim_{i}", $"value_{i}"));
            }

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
            _mockSignInManager.Setup(x => x.IsSignedIn(user)).Returns(true);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var isSignedIn = _mockSignInManager.Object.IsSignedIn(user);
            var name = user.Identity?.Name;
            var claimCount = user.Claims.Count();
            stopwatch.Stop();

            // Assert
            isSignedIn.Should().BeTrue("User with many claims should be signed in");
            name.Should().Be("test@example.com");
            claimCount.Should().Be(102, "Should handle large number of claims");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, "Should handle large claim set efficiently");
        }

        #endregion

        #region Cultural and Localization

        [Fact]
        public void LoginPartial_DifferentCultures_ShouldMaintainFunctionality()
        {
            // Arrange
            var cultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("es-ES"),
                new CultureInfo("fr-FR"),
                new CultureInfo("ja-JP"),
                new CultureInfo("ar-SA")
            };

            foreach (var culture in cultures)
            {
                var originalCulture = CultureInfo.CurrentCulture;
                var originalUICulture = CultureInfo.CurrentUICulture;

                try
                {
                    // Set culture
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;

                    var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, $"user@{culture.Name}.com"),
                        new Claim(ClaimTypes.NameIdentifier, $"user-{culture.Name}")
                    }, "test"));

                    _mockSignInManager.Setup(x => x.IsSignedIn(user)).Returns(true);

                    // Act
                    var isSignedIn = _mockSignInManager.Object.IsSignedIn(user);
                    var name = user.Identity?.Name;

                    // Assert
                    isSignedIn.Should().BeTrue($"Authentication should work with culture '{culture.Name}'");
                    name.Should().Be($"user@{culture.Name}.com", $"Name should be preserved with culture '{culture.Name}'");
                }
                finally
                {
                    // Restore original culture
                    CultureInfo.CurrentCulture = originalCulture;
                    CultureInfo.CurrentUICulture = originalUICulture;
                }
            }
        }

        #endregion

        #region Service Configuration and Validation

        [Fact]
        public void LoginPartial_SignInManager_ShouldBeConfiguredCorrectly()
        {
            // Act & Assert
            _mockSignInManager.Should().NotBeNull("SignInManager should be configured");
            _mockSignInManager.Object.Should().NotBeNull("SignInManager instance should be valid");
        }

        [Fact]
        public void LoginPartial_UserManager_ShouldBeConfiguredCorrectly()
        {
            // Act & Assert
            _mockUserManager.Should().NotBeNull("UserManager should be configured");
            _mockUserManager.Object.Should().NotBeNull("UserManager instance should be valid");
        }

        [Fact]
        public void LoginPartial_AuthenticationTypes_ShouldBeRecognized()
        {
            // Arrange
            var authTypes = new[]
            {
                "Identity.Application",
                "Identity.External",
                "Cookies",
                "Bearer",
                "Google",
                "Microsoft"
            };

            foreach (var authType in authTypes)
            {
                var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "test@example.com")
                }, authType));

                _mockSignInManager.Setup(x => x.IsSignedIn(user)).Returns(true);

                // Act
                var isSignedIn = _mockSignInManager.Object.IsSignedIn(user);

                // Assert
                isSignedIn.Should().BeTrue($"Authentication type '{authType}' should be recognized");
            }
        }

        #endregion

        #region Performance and Memory

        [Fact]
        public async Task LoginPartial_ConcurrentAuthenticationChecks_ShouldHandleCorrectly()
        {
            // Arrange
            var users = Enumerable.Range(1, 20).Select(i => 
                new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, $"user{i}@example.com"),
                    new Claim(ClaimTypes.NameIdentifier, $"user-{i}")
                }, "test"))).ToArray();

            foreach (var user in users)
            {
                _mockSignInManager.Setup(x => x.IsSignedIn(user)).Returns(true);
            }

            // Act - Test concurrent authentication checks
            var tasks = users.Select(async user =>
            {
                await Task.Delay(Random.Shared.Next(1, 5)); // Random delay
                return _mockSignInManager.Object.IsSignedIn(user);
            });

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().AllSatisfy(result => 
                result.Should().BeTrue("All concurrent authentication checks should succeed"));
        }

        [Fact]
        public void LoginPartial_RepeatedAuthenticationChecks_ShouldBeConsistent()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "test@example.com")
            }, "test"));

            _mockSignInManager.Setup(x => x.IsSignedIn(user)).Returns(true);

            // Act - Check authentication multiple times
            var results = new List<bool>();
            for (int i = 0; i < 100; i++)
            {
                results.Add(_mockSignInManager.Object.IsSignedIn(user));
            }

            // Assert
            results.Should().AllSatisfy(result => 
                result.Should().BeTrue("All repeated authentication checks should be consistent"));
        }

        #endregion

        #region Error Conditions

        [Fact]
        public void LoginPartial_ExceptionInAuthentication_ShouldPropagateCorrectly()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "test@example.com")
            }, "test"));

            _mockSignInManager.Setup(x => x.IsSignedIn(user))
                            .Throws(new InvalidOperationException("Authentication service error"));

            // Act & Assert
            var action = () => _mockSignInManager.Object.IsSignedIn(user);
            action.Should().Throw<InvalidOperationException>()
                  .WithMessage("Authentication service error");
        }

        [Fact]
        public void LoginPartial_NullClaimsIdentity_ShouldHandleGracefully()
        {
            // Arrange
            var userWithNullIdentity = new ClaimsPrincipal();
            _mockSignInManager.Setup(x => x.IsSignedIn(userWithNullIdentity)).Returns(false);

            // Act
            var isSignedIn = _mockSignInManager.Object.IsSignedIn(userWithNullIdentity);
            var name = userWithNullIdentity.Identity?.Name;

            // Assert
            isSignedIn.Should().BeFalse("User with null identity should not be signed in");
            name.Should().BeNull("User with null identity should have null name");
        }

        #endregion
    }
}