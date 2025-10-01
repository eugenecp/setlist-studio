using FluentAssertions;
using SetlistStudio.Core.Entities;
using Xunit;

namespace SetlistStudio.Tests.Entities;

/// <summary>
/// Comprehensive tests for the Song entity covering all properties, validation, and business logic
/// </summary>
public class SongTests
{
    [Fact]
    public void Song_ShouldInitializeWithDefaultValues()
    {
        // Act
        var song = new Song();

        // Assert
        song.Id.Should().Be(0);
        song.Title.Should().Be(string.Empty);
        song.Artist.Should().Be(string.Empty);
        song.Album.Should().BeNull();
        song.Genre.Should().BeNull();
        song.Bpm.Should().BeNull();
        song.MusicalKey.Should().BeNull();
        song.DurationSeconds.Should().BeNull();
        song.Notes.Should().BeNull();
        song.Tags.Should().BeNull();
        song.DifficultyRating.Should().BeNull();
        song.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        song.UpdatedAt.Should().BeNull();
        song.UserId.Should().Be(string.Empty);
        song.User.Should().BeNull();
        song.SetlistSongs.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Song_ShouldSetProperties_Correctly()
    {
        // Arrange
        var userId = "test-user-123";
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;

        // Act
        var song = new Song
        {
            Id = 42,
            Title = "Sweet Child O' Mine",
            Artist = "Guns N' Roses",
            Album = "Appetite for Destruction",
            Genre = "Rock",
            Bpm = 125,
            MusicalKey = "D",
            DurationSeconds = 356,
            Notes = "Great guitar solo in the middle",
            Tags = "classic rock, guitar solo, 80s",
            DifficultyRating = 4,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            UserId = userId
        };

        // Assert
        song.Id.Should().Be(42);
        song.Title.Should().Be("Sweet Child O' Mine");
        song.Artist.Should().Be("Guns N' Roses");
        song.Album.Should().Be("Appetite for Destruction");
        song.Genre.Should().Be("Rock");
        song.Bpm.Should().Be(125);
        song.MusicalKey.Should().Be("D");
        song.DurationSeconds.Should().Be(356);
        song.Notes.Should().Be("Great guitar solo in the middle");
        song.Tags.Should().Be("classic rock, guitar solo, 80s");
        song.DifficultyRating.Should().Be(4);
        song.CreatedAt.Should().Be(createdAt);
        song.UpdatedAt.Should().Be(updatedAt);
        song.UserId.Should().Be(userId);
    }

    [Theory]
    [InlineData(null, "")] // null duration
    [InlineData(0, "00:00")] // zero seconds
    [InlineData(30, "00:30")] // 30 seconds
    [InlineData(60, "01:00")] // 1 minute
    [InlineData(90, "01:30")] // 1 minute 30 seconds
    [InlineData(125, "02:05")] // 2 minutes 5 seconds
    [InlineData(356, "05:56")] // Sweet Child O' Mine duration
    [InlineData(3600, "60:00")] // 1 hour
    public void FormattedDuration_ShouldReturnCorrectFormat(int? durationSeconds, string expected)
    {
        // Arrange
        var song = new Song { DurationSeconds = durationSeconds };

        // Act
        var result = song.FormattedDuration;

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "", null, "", false)] // Empty title and artist
    [InlineData("Title", "", null, "", false)] // Missing artist
    [InlineData("", "Artist", null, "", false)] // Missing title
    [InlineData("Title", "Artist", null, "", false)] // Missing BPM
    [InlineData("Title", "Artist", 120, "", false)] // Missing key
    [InlineData("Title", "Artist", 120, "C", true)] // Complete
    [InlineData("Sweet Child O' Mine", "Guns N' Roses", 125, "D", true)] // Complete realistic example
    public void IsComplete_ShouldReturnCorrectValue(string title, string artist, int? bpm, string musicalKey, bool expected)
    {
        // Arrange
        var song = new Song
        {
            Title = title,
            Artist = artist,
            Bpm = bpm,
            MusicalKey = musicalKey
        };

        // Act
        var result = song.IsComplete;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsComplete_ShouldReturnTrue_WhenAllRequiredFieldsAreProvided()
    {
        // Arrange
        var song = new Song
        {
            Title = "Billie Jean",
            Artist = "Michael Jackson",
            Bpm = 117,
            MusicalKey = "F#m"
        };

        // Act
        var result = song.IsComplete;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsComplete_ShouldReturnFalse_WhenTitleIsEmpty()
    {
        // Arrange
        var song = new Song
        {
            Title = "",
            Artist = "Artist",
            Bpm = 120,
            MusicalKey = "C"
        };

        // Act
        var result = song.IsComplete;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsComplete_ShouldReturnFalse_WhenArtistIsEmpty()
    {
        // Arrange
        var song = new Song
        {
            Title = "Title",
            Artist = "",
            Bpm = 120,
            MusicalKey = "C"
        };

        // Act
        var result = song.IsComplete;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsComplete_ShouldReturnFalse_WhenBpmIsNull()
    {
        // Arrange
        var song = new Song
        {
            Title = "Title",
            Artist = "Artist",
            Bpm = null,
            MusicalKey = "C"
        };

        // Act
        var result = song.IsComplete;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsComplete_ShouldReturnFalse_WhenMusicalKeyIsEmpty()
    {
        // Arrange
        var song = new Song
        {
            Title = "Title",
            Artist = "Artist",
            Bpm = 120,
            MusicalKey = ""
        };

        // Act
        var result = song.IsComplete;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsComplete_ShouldReturnFalse_WhenMusicalKeyIsNull()
    {
        // Arrange
        var song = new Song
        {
            Title = "Title",
            Artist = "Artist",
            Bpm = 120,
            MusicalKey = null
        };

        // Act
        var result = song.IsComplete;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Song_ShouldHandleMixedCaseKeys_InIsComplete()
    {
        // Arrange
        var song = new Song
        {
            Title = "Take Five",
            Artist = "Dave Brubeck",
            Bpm = 176,
            MusicalKey = "Bb" // Mixed case key
        };

        // Act
        var result = song.IsComplete;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Song_ShouldHandleWhitespaceInProperties_InIsComplete()
    {
        // Arrange
        var song = new Song
        {
            Title = "   ",
            Artist = "Artist",
            Bpm = 120,
            MusicalKey = "C"
        };

        // Act
        var result = song.IsComplete;

        // Assert - Should handle whitespace as empty
        result.Should().BeFalse();
    }

    [Fact]
    public void SetlistSongs_ShouldInitializeAsEmptyCollection()
    {
        // Arrange & Act
        var song = new Song();

        // Assert
        song.SetlistSongs.Should().NotBeNull();
        song.SetlistSongs.Should().BeEmpty();
        song.SetlistSongs.Should().BeAssignableTo<ICollection<SetlistSong>>();
    }

    [Fact]
    public void Song_ShouldSupportRealisticMusicData()
    {
        // Arrange - Various realistic examples
        var rockSong = new Song
        {
            Title = "Stairway to Heaven",
            Artist = "Led Zeppelin",
            Album = "Led Zeppelin IV",
            Genre = "Rock",
            Bpm = 82,
            MusicalKey = "Am",
            DurationSeconds = 482,
            DifficultyRating = 5,
            Tags = "epic, guitar solo, classic rock",
            UserId = "user1"
        };

        var jazzSong = new Song
        {
            Title = "All of Me",
            Artist = "John Legend",
            Album = "Love in the Future",
            Genre = "Jazz",
            Bpm = 120,
            MusicalKey = "Ab",
            DurationSeconds = 269,
            DifficultyRating = 3,
            Tags = "wedding, jazz, romantic",
            UserId = "user1"
        };

        // Act & Assert
        rockSong.IsComplete.Should().BeTrue();
        rockSong.FormattedDuration.Should().Be("08:02");
        
        jazzSong.IsComplete.Should().BeTrue();
        jazzSong.FormattedDuration.Should().Be("04:29");
    }
}