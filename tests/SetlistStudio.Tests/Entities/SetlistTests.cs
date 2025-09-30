using SetlistStudio.Core.Entities;
using FluentAssertions;
using Xunit;
using System.ComponentModel.DataAnnotations;

namespace SetlistStudio.Tests.Entities;

/// <summary>
/// Comprehensive tests for Setlist entity covering all properties, validation, and helper methods
/// Covers: Property initialization, validation attributes, calculated properties, and business logic
/// </summary>
public class SetlistTests
{
    [Fact]
    public void Setlist_ShouldInitializeWithDefaultValues_WhenCreated()
    {
        // Act
        var setlist = new Setlist();

        // Assert
        setlist.Id.Should().Be(0);
        setlist.Name.Should().BeEmpty();
        setlist.Description.Should().BeNull();
        setlist.Venue.Should().BeNull();
        setlist.PerformanceDate.Should().BeNull();
        setlist.ExpectedDurationMinutes.Should().BeNull();
        setlist.IsTemplate.Should().BeFalse();
        setlist.IsActive.Should().BeFalse();
        setlist.PerformanceNotes.Should().BeNull();
        setlist.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        setlist.UpdatedAt.Should().BeNull();
        setlist.UserId.Should().BeEmpty();
        setlist.User.Should().BeNull();
        setlist.SetlistSongs.Should().NotBeNull();
        setlist.SetlistSongs.Should().BeEmpty();
    }

    [Fact]
    public void Setlist_ShouldSetPropertiesCorrectly_WhenAssigned()
    {
        // Arrange
        var performanceDate = DateTime.UtcNow.AddDays(30);
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };

        // Act
        var setlist = new Setlist
        {
            Id = 42,
            Name = "Rock Concert Main Set",
            Description = "High-energy rock setlist for main concert",
            Venue = "Madison Square Garden", 
            PerformanceDate = performanceDate,
            ExpectedDurationMinutes = 90,
            IsTemplate = true,
            IsActive = true,
            PerformanceNotes = "Sound check at 6 PM, stage lights dimmed for opening",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            UserId = "user-123",
            User = user
        };

        // Assert
        setlist.Id.Should().Be(42);
        setlist.Name.Should().Be("Rock Concert Main Set");
        setlist.Description.Should().Be("High-energy rock setlist for main concert");
        setlist.Venue.Should().Be("Madison Square Garden");
        setlist.PerformanceDate.Should().Be(performanceDate);
        setlist.ExpectedDurationMinutes.Should().Be(90);
        setlist.IsTemplate.Should().BeTrue();
        setlist.IsActive.Should().BeTrue();
        setlist.PerformanceNotes.Should().Be("Sound check at 6 PM, stage lights dimmed for opening");
        setlist.CreatedAt.Should().Be(createdAt);
        setlist.UpdatedAt.Should().Be(updatedAt);
        setlist.UserId.Should().Be("user-123");
        setlist.User.Should().Be(user);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("Short Name")]
    [InlineData("This is a very long setlist name that approaches the maximum length limit for testing")]
    public void Name_ShouldAcceptValidValues_WhenWithinLengthLimit(string name)
    {
        // Act
        var setlist = new Setlist { Name = name };

        // Assert
        setlist.Name.Should().Be(name);
    }

    [Fact]
    public void Name_ShouldBeRequired_BasedOnRequiredAttribute()
    {
        // Arrange
        var setlist = new Setlist();
        var context = new ValidationContext(setlist);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlist, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Name_ShouldFailValidation_WhenExceedsMaxLength()
    {
        // Arrange
        var longName = new string('x', 201); // Exceeds StringLength(200)
        var setlist = new Setlist { Name = longName, UserId = "test" };
        var context = new ValidationContext(setlist);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlist, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Name"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Simple description")]
    [InlineData("This is a very detailed description of the setlist with lots of information about the performance and what to expect during the show.")]
    public void Description_ShouldAcceptValidValues_WhenWithinLengthLimit(string? description)
    {
        // Act
        var setlist = new Setlist { Description = description };

        // Assert
        setlist.Description.Should().Be(description);
    }

    [Fact]
    public void Description_ShouldFailValidation_WhenExceedsMaxLength()
    {
        // Arrange
        var longDescription = new string('x', 1001); // Exceeds StringLength(1000)
        var setlist = new Setlist { Description = longDescription, Name = "Test", UserId = "test" };
        var context = new ValidationContext(setlist);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlist, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Description"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Local Bar")]
    [InlineData("Madison Square Garden")]
    public void Venue_ShouldAcceptValidValues_WhenWithinLengthLimit(string? venue)
    {
        // Act
        var setlist = new Setlist { Venue = venue };

        // Assert
        setlist.Venue.Should().Be(venue);
    }

    [Fact]
    public void Venue_ShouldFailValidation_WhenExceedsMaxLength()
    {
        // Arrange
        var longVenue = new string('x', 201); // Exceeds StringLength(200)
        var setlist = new Setlist { Venue = longVenue, Name = "Test", UserId = "test" };
        var context = new ValidationContext(setlist);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlist, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Venue"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Sound check at 6 PM")]
    [InlineData("This is a very long performance note with lots of details about the sound check, lighting, special instructions for the band, and other important information that needs to be communicated.")]
    public void PerformanceNotes_ShouldAcceptValidValues_WhenWithinLengthLimit(string? notes)
    {
        // Act
        var setlist = new Setlist { PerformanceNotes = notes };

        // Assert
        setlist.PerformanceNotes.Should().Be(notes);
    }

    [Fact]
    public void PerformanceNotes_ShouldFailValidation_WhenExceedsMaxLength()
    {
        // Arrange
        var longNotes = new string('x', 2001); // Exceeds StringLength(2000)
        var setlist = new Setlist { PerformanceNotes = longNotes, Name = "Test", UserId = "test" };
        var context = new ValidationContext(setlist);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlist, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("PerformanceNotes"));
    }

    [Fact]
    public void UserId_ShouldBeRequired_BasedOnRequiredAttribute()
    {
        // Arrange
        var setlist = new Setlist { Name = "Test Setlist" };
        var context = new ValidationContext(setlist);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlist, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("UserId"));
    }

    [Fact]
    public void CalculatedDurationMinutes_ShouldReturnZero_WhenNoSongs()
    {
        // Arrange
        var setlist = new Setlist();

        // Act
        var duration = setlist.CalculatedDurationMinutes;

        // Assert
        duration.Should().Be(0);
    }

    [Fact]
    public void CalculatedDurationMinutes_ShouldCalculateCorrectly_WhenSongsHaveDurations()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", DurationSeconds = 180 }, // 3 minutes
            new Song { Title = "Song 2", Artist = "Artist 2", DurationSeconds = 240 }, // 4 minutes
            new Song { Title = "Song 3", Artist = "Artist 3", DurationSeconds = 300 }  // 5 minutes
        };

        var setlist = new Setlist
        {
            SetlistSongs = new List<SetlistSong>
            {
                new SetlistSong { Song = songs[0], Position = 1 },
                new SetlistSong { Song = songs[1], Position = 2 },
                new SetlistSong { Song = songs[2], Position = 3 }
            }
        };

        // Act
        var duration = setlist.CalculatedDurationMinutes;

        // Assert
        duration.Should().Be(12); // (180 + 240 + 300) / 60 = 12 minutes
    }

    [Fact]
    public void CalculatedDurationMinutes_ShouldIgnoreSongsWithoutDuration_WhenCalculating()
    {
        // Arrange
        var songs = new List<Song>
        {
            new Song { Title = "Song 1", Artist = "Artist 1", DurationSeconds = 180 }, // 3 minutes
            new Song { Title = "Song 2", Artist = "Artist 2", DurationSeconds = null }, // No duration
            new Song { Title = "Song 3", Artist = "Artist 3", DurationSeconds = 240 }  // 4 minutes
        };

        var setlist = new Setlist
        {
            SetlistSongs = new List<SetlistSong>
            {
                new SetlistSong { Song = songs[0], Position = 1 },
                new SetlistSong { Song = songs[1], Position = 2 },
                new SetlistSong { Song = songs[2], Position = 3 }
            }
        };

        // Act
        var duration = setlist.CalculatedDurationMinutes;

        // Assert
        duration.Should().Be(7); // (180 + 240) / 60 = 7 minutes (ignores null duration)
    }

    [Fact]
    public void SongCount_ShouldReturnZero_WhenNoSongs()
    {
        // Arrange
        var setlist = new Setlist();

        // Act
        var count = setlist.SongCount;

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void SongCount_ShouldReturnCorrectCount_WhenSongsExist()
    {
        // Arrange
        var setlist = new Setlist
        {
            SetlistSongs = new List<SetlistSong>
            {
                new SetlistSong { Song = new Song { Title = "Song 1", Artist = "Artist 1" } },
                new SetlistSong { Song = new Song { Title = "Song 2", Artist = "Artist 2" } },
                new SetlistSong { Song = new Song { Title = "Song 3", Artist = "Artist 3" } }
            }
        };

        // Act
        var count = setlist.SongCount;

        // Assert
        count.Should().Be(3);
    }

    [Theory]
    [InlineData(0, null, null, false)] // No songs, no date, no venue
    [InlineData(1, null, null, false)] // Has songs but no date or venue
    [InlineData(1, "Madison Square Garden", null, false)] // Has songs and venue but no date
    [InlineData(0, "Madison Square Garden", "2025-12-31", false)] // Has venue and date but no songs
    [InlineData(1, "Madison Square Garden", "2025-12-31", true)] // Has all required elements
    [InlineData(3, "Local Bar", "2025-10-15", true)] // Multiple songs with all elements
    public void IsReadyForPerformance_ShouldReturnCorrectValue_BasedOnConditions(
        int songCount, string? venue, string? performanceDateString, bool expectedReady)
    {
        // Arrange
        var performanceDate = performanceDateString != null ? DateTime.Parse(performanceDateString) : (DateTime?)null;
        
        var setlist = new Setlist
        {
            Venue = venue,
            PerformanceDate = performanceDate,
            SetlistSongs = Enumerable.Range(1, songCount)
                .Select(i => new SetlistSong 
                { 
                    Song = new Song { Title = $"Song {i}", Artist = $"Artist {i}" },
                    Position = i
                })
                .ToList()
        };

        // Act
        var isReady = setlist.IsReadyForPerformance;

        // Assert
        isReady.Should().Be(expectedReady);
    }

    [Fact]
    public void IsReadyForPerformance_ShouldReturnFalse_WhenVenueIsEmptyString()
    {
        // Arrange
        var setlist = new Setlist
        {
            Venue = "", // Empty string should be treated as "no venue"
            PerformanceDate = DateTime.UtcNow.AddDays(1),
            SetlistSongs = new List<SetlistSong>
            {
                new SetlistSong { Song = new Song { Title = "Test Song", Artist = "Test Artist" } }
            }
        };

        // Act
        var isReady = setlist.IsReadyForPerformance;

        // Assert
        isReady.Should().BeFalse();
    }

    [Fact]
    public void Setlist_ShouldPassValidation_WhenAllRequiredFieldsProvided()
    {
        // Arrange
        var setlist = new Setlist
        {
            Name = "Valid Setlist Name",
            UserId = "valid-user-id"
        };
        var context = new ValidationContext(setlist);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlist, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void SetlistSongs_ShouldAllowModification_WhenInitialized()
    {
        // Arrange
        var setlist = new Setlist();
        var setlistSong = new SetlistSong 
        { 
            Song = new Song { Title = "Test Song", Artist = "Test Artist" },
            Position = 1
        };

        // Act
        setlist.SetlistSongs.Add(setlistSong);

        // Assert
        setlist.SetlistSongs.Should().HaveCount(1);
        setlist.SetlistSongs.First().Should().Be(setlistSong);
        setlist.SongCount.Should().Be(1);
    }
}