using SetlistStudio.Core.Entities;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Identity;

namespace SetlistStudio.Tests.Entities;

/// <summary>
/// Comprehensive tests for ApplicationUser entity covering all properties, inheritance, and navigation properties
/// Covers: Property initialization, inheritance from IdentityUser, collections, and business logic
/// </summary>
public class ApplicationUserTests
{
    [Fact]
    public void ApplicationUser_ShouldInitializeWithDefaultValues_WhenCreated()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        // IdentityUser inherited properties
        user.Id.Should().NotBeNull(); // IdentityUser auto-generates Id
        user.UserName.Should().BeNull();
        user.Email.Should().BeNull();
        user.EmailConfirmed.Should().BeFalse();
        user.PhoneNumber.Should().BeNull();
        user.PhoneNumberConfirmed.Should().BeFalse();
        user.TwoFactorEnabled.Should().BeFalse();
        user.LockoutEnabled.Should().BeFalse(); // Default for IdentityUser
        user.LockoutEnd.Should().BeNull();
        user.AccessFailedCount.Should().Be(0);

        // ApplicationUser custom properties
        user.DisplayName.Should().BeNull();
        user.ProfilePictureUrl.Should().BeNull();
        user.Provider.Should().BeNull();
        user.ProviderKey.Should().BeNull();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.UpdatedAt.Should().BeNull();

        // Navigation properties
        user.Songs.Should().NotBeNull();
        user.Songs.Should().BeEmpty();
        user.Setlists.Should().NotBeNull();
        user.Setlists.Should().BeEmpty();
    }

    [Fact]
    public void ApplicationUser_ShouldInheritFromIdentityUser_WhenCreated()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        user.Should().BeAssignableTo<IdentityUser>();
        user.Should().BeOfType<ApplicationUser>();
    }

    [Fact]
    public void ApplicationUser_ShouldSetPropertiesCorrectly_WhenAssigned()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var updatedAt = DateTime.UtcNow.AddDays(-1);
        var songs = new List<Song>
        {
            new Song { Title = "Bohemian Rhapsody", Artist = "Queen" },
            new Song { Title = "Billie Jean", Artist = "Michael Jackson" }
        };
        var setlists = new List<Setlist>
        {
            new Setlist { Name = "Rock Concert", UserId = "test-user" },
            new Setlist { Name = "Wedding Reception", UserId = "test-user" }
        };

        // Act
        var user = new ApplicationUser
        {
            Id = "test-user-123",
            UserName = "rockstar@example.com",
            Email = "rockstar@example.com",
            EmailConfirmed = true,
            DisplayName = "Rock Star",
            ProfilePictureUrl = "https://example.com/profile.jpg",
            Provider = "Google",
            ProviderKey = "google-12345",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Songs = songs,
            Setlists = setlists,
            PhoneNumber = "+1-555-0123",
            PhoneNumberConfirmed = true,
            TwoFactorEnabled = true,
            LockoutEnabled = false,
            AccessFailedCount = 2
        };

        // Assert
        // IdentityUser inherited properties
        user.Id.Should().Be("test-user-123");
        user.UserName.Should().Be("rockstar@example.com");
        user.Email.Should().Be("rockstar@example.com");
        user.EmailConfirmed.Should().BeTrue();
        user.PhoneNumber.Should().Be("+1-555-0123");
        user.PhoneNumberConfirmed.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeTrue();
        user.LockoutEnabled.Should().BeFalse();
        user.AccessFailedCount.Should().Be(2);

        // ApplicationUser custom properties
        user.DisplayName.Should().Be("Rock Star");
        user.ProfilePictureUrl.Should().Be("https://example.com/profile.jpg");
        user.Provider.Should().Be("Google");
        user.ProviderKey.Should().Be("google-12345");
        user.CreatedAt.Should().Be(createdAt);
        user.UpdatedAt.Should().Be(updatedAt);

        // Navigation properties
        user.Songs.Should().HaveCount(2);
        user.Songs.Should().Contain(s => s.Title == "Bohemian Rhapsody");
        user.Songs.Should().Contain(s => s.Title == "Billie Jean");
        user.Setlists.Should().HaveCount(2);
        user.Setlists.Should().Contain(s => s.Name == "Rock Concert");
        user.Setlists.Should().Contain(s => s.Name == "Wedding Reception");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("John Doe")]
    [InlineData("María González")]
    [InlineData("李小明")]
    [InlineData("A very long display name that someone might use for their profile")]
    public void DisplayName_ShouldAcceptValidValues_WhenAssigned(string? displayName)
    {
        // Act
        var user = new ApplicationUser { DisplayName = displayName };

        // Assert
        user.DisplayName.Should().Be(displayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://example.com/profile.jpg")]
    [InlineData("https://googleusercontent.com/a/default-user=s96-c")]
    [InlineData("https://graph.microsoft.com/v1.0/me/photo/$value")]
    [InlineData("data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD//2Q==")]
    public void ProfilePictureUrl_ShouldAcceptValidValues_WhenAssigned(string? profilePictureUrl)
    {
        // Act
        var user = new ApplicationUser { ProfilePictureUrl = profilePictureUrl };

        // Assert
        user.ProfilePictureUrl.Should().Be(profilePictureUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Google")]
    [InlineData("Microsoft")]
    [InlineData("Facebook")]
    [InlineData("GitHub")]
    [InlineData("Apple")]
    [InlineData("CustomProvider")]
    public void Provider_ShouldAcceptValidValues_WhenAssigned(string? provider)
    {
        // Act
        var user = new ApplicationUser { Provider = provider };

        // Assert
        user.Provider.Should().Be(provider);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("google-oauth2|1234567890")]
    [InlineData("facebook|10157234567890123")]
    [InlineData("github|87654321")]
    public void ProviderKey_ShouldAcceptValidValues_WhenAssigned(string? providerKey)
    {
        // Act
        var user = new ApplicationUser { ProviderKey = providerKey };

        // Assert
        user.ProviderKey.Should().Be(providerKey);
    }

    [Fact]
    public void CreatedAt_ShouldDefaultToCurrentTime_WhenCreated()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var user = new ApplicationUser();

        // Assert
        var afterCreation = DateTime.UtcNow;
        user.CreatedAt.Should().BeAfter(beforeCreation.AddSeconds(-1));
        user.CreatedAt.Should().BeBefore(afterCreation.AddSeconds(1));
    }

    [Fact]
    public void CreatedAt_ShouldAcceptCustomValue_WhenAssigned()
    {
        // Arrange
        var customCreatedAt = DateTime.UtcNow.AddYears(-1);

        // Act
        var user = new ApplicationUser { CreatedAt = customCreatedAt };

        // Assert
        user.CreatedAt.Should().Be(customCreatedAt);
    }

    [Fact]
    public void UpdatedAt_ShouldDefaultToNull_WhenCreated()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        user.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void UpdatedAt_ShouldAcceptDateTime_WhenAssigned()
    {
        // Arrange
        var updatedAt = DateTime.UtcNow;

        // Act
        var user = new ApplicationUser { UpdatedAt = updatedAt };

        // Assert
        user.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Songs_ShouldAllowAddingAndRemoving_WhenModified()
    {
        // Arrange
        var user = new ApplicationUser();
        var song1 = new Song { Title = "Test Song 1", Artist = "Test Artist 1" };
        var song2 = new Song { Title = "Test Song 2", Artist = "Test Artist 2" };

        // Act
        user.Songs.Add(song1);
        user.Songs.Add(song2);

        // Assert
        user.Songs.Should().HaveCount(2);
        user.Songs.Should().Contain(song1);
        user.Songs.Should().Contain(song2);

        // Act - Remove one song
        user.Songs.Remove(song1);

        // Assert
        user.Songs.Should().HaveCount(1);
        user.Songs.Should().Contain(song2);
        user.Songs.Should().NotContain(song1);
    }

    [Fact]
    public void Setlists_ShouldAllowAddingAndRemoving_WhenModified()
    {
        // Arrange
        var user = new ApplicationUser { Id = "test-user" };
        var setlist1 = new Setlist { Name = "Test Setlist 1", UserId = "test-user" };
        var setlist2 = new Setlist { Name = "Test Setlist 2", UserId = "test-user" };

        // Act
        user.Setlists.Add(setlist1);
        user.Setlists.Add(setlist2);

        // Assert
        user.Setlists.Should().HaveCount(2);
        user.Setlists.Should().Contain(setlist1);
        user.Setlists.Should().Contain(setlist2);

        // Act - Remove one setlist
        user.Setlists.Remove(setlist1);

        // Assert
        user.Setlists.Should().HaveCount(1);
        user.Setlists.Should().Contain(setlist2);
        user.Setlists.Should().NotContain(setlist1);
    }

    [Fact]
    public void ApplicationUser_ShouldSupportCompleteOAuthProfile_WhenAllPropertiesSet()
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            Id = "oauth-user-123",
            UserName = "musician@gmail.com",
            Email = "musician@gmail.com",
            EmailConfirmed = true,
            DisplayName = "Jazz Musician",
            ProfilePictureUrl = "https://lh3.googleusercontent.com/a/profile-pic",
            Provider = "Google",
            ProviderKey = "google-oauth2|123456789012345678901",
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Assert - Complete OAuth profile
        user.Should().NotBeNull();
        user.Id.Should().NotBeNullOrEmpty();
        user.UserName.Should().Be(user.Email);
        user.EmailConfirmed.Should().BeTrue();
        user.DisplayName.Should().NotBeNullOrEmpty();
        user.ProfilePictureUrl.Should().StartWith("https://");
        user.Provider.Should().NotBeNullOrEmpty();
        user.ProviderKey.Should().NotBeNullOrEmpty();
        user.CreatedAt.Should().BeBefore(DateTime.UtcNow);
        user.UpdatedAt.Should().BeBefore(DateTime.UtcNow);
        user.UpdatedAt.Should().BeAfter(user.CreatedAt);
    }

    [Fact]
    public void ApplicationUser_ShouldSupportMultipleProviders_WithDifferentProviderKeys()
    {
        // Arrange - Simulate user who has used different OAuth providers
        var googleUser = new ApplicationUser
        {
            Provider = "Google",
            ProviderKey = "google-oauth2|123456789",
            DisplayName = "From Google"
        };

        var microsoftUser = new ApplicationUser
        {
            Provider = "Microsoft",
            ProviderKey = "microsoft|abcdef123456",
            DisplayName = "From Microsoft"
        };

        var facebookUser = new ApplicationUser
        {
            Provider = "Facebook",
            ProviderKey = "facebook|987654321",
            DisplayName = "From Facebook"
        };

        // Assert
        googleUser.Provider.Should().Be("Google");
        googleUser.ProviderKey.Should().Contain("google-oauth2");

        microsoftUser.Provider.Should().Be("Microsoft");
        microsoftUser.ProviderKey.Should().Contain("microsoft");

        facebookUser.Provider.Should().Be("Facebook");
        facebookUser.ProviderKey.Should().Contain("facebook");

        // All should be different instances
        googleUser.Should().NotBe(microsoftUser);
        microsoftUser.Should().NotBe(facebookUser);
        facebookUser.Should().NotBe(googleUser);
    }

    [Fact]
    public void ApplicationUser_ShouldInheritIdentityUserSecurity_WhenCreated()
    {
        // Act
        var user = new ApplicationUser
        {
            UserName = "security@test.com",
            Email = "security@test.com",
            LockoutEnabled = true,
            TwoFactorEnabled = true,
            AccessFailedCount = 0
        };

        // Assert - Security-related inherited properties work
        user.LockoutEnabled.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeTrue();
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull(); // Not locked out
        
        // Test lockout functionality
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);
        user.LockoutEnd.Should().BeAfter(DateTimeOffset.UtcNow);
        
        // Test access failed count
        user.AccessFailedCount = 3;
        user.AccessFailedCount.Should().Be(3);
    }

    [Fact]
    public void ApplicationUser_ShouldAllowNullCollections_ButInitializeAsEmpty()
    {
        // Act
        var user = new ApplicationUser();

        // Assert - Collections are initialized but can be reassigned
        user.Songs.Should().NotBeNull();
        user.Songs.Should().BeEmpty();
        user.Setlists.Should().NotBeNull();
        user.Setlists.Should().BeEmpty();

        // Act - Can reassign collections
        user.Songs = new List<Song> { new Song { Title = "Test", Artist = "Test" } };
        user.Setlists = new List<Setlist> { new Setlist { Name = "Test", UserId = "test" } };

        // Assert
        user.Songs.Should().HaveCount(1);
        user.Setlists.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("user@example.com", "user@example.com")] // Same email and username
    [InlineData("different@email.com", "username123")] // Different email and username
    [InlineData(null, "usernameonly")] // No email, username only
    [InlineData("emailonly@test.com", null)] // Email only, no username
    public void ApplicationUser_ShouldSupportDifferentEmailUsernameCombinations(string? email, string? username)
    {
        // Act
        var user = new ApplicationUser
        {
            Email = email,
            UserName = username
        };

        // Assert
        user.Email.Should().Be(email);
        user.UserName.Should().Be(username);
    }

    [Fact]
    public void ApplicationUser_CreatedAtAndUpdatedAt_ShouldFollowTimestampPattern()
    {
        // Arrange
        var initialTime = DateTime.UtcNow.AddDays(-1);
        
        // Act - Simulate user creation
        var user = new ApplicationUser
        {
            CreatedAt = initialTime,
            UpdatedAt = null // First creation, no update yet
        };

        // Assert - Initial state
        user.CreatedAt.Should().Be(initialTime);
        user.UpdatedAt.Should().BeNull();

        // Act - Simulate profile update
        var updateTime = DateTime.UtcNow;
        user.DisplayName = "Updated Name";
        user.UpdatedAt = updateTime;

        // Assert - After update
        user.UpdatedAt.Should().Be(updateTime);
        user.UpdatedAt.Should().BeAfter(user.CreatedAt);
    }
}