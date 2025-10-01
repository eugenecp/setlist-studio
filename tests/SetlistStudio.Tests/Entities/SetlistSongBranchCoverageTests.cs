using SetlistStudio.Core.Entities;
using FluentAssertions;
using Xunit;

namespace SetlistStudio.Tests.Entities;

/// <summary>
/// Additional tests for SetlistSong entity focusing on improving branch coverage
/// Specifically targeting edge cases in helper methods for better branch coverage
/// </summary>
public class SetlistSongBranchCoverageTests
{
    #region EffectiveKey Branch Coverage Tests

    [Fact]
    public void EffectiveKey_ShouldReturnSongKey_WhenCustomKeyIsEmptyString()
    {
        // Arrange
        var song = new Song { MusicalKey = "C", UserId = "user-123" };
        var setlistSong = new SetlistSong
        {
            CustomKey = "", // Empty string (not null)
            Song = song
        };

        // Act
        var effectiveKey = setlistSong.EffectiveKey;

        // Assert
        effectiveKey.Should().Be("C", "should return song key when custom key is empty string");
    }

    [Fact]
    public void EffectiveKey_ShouldReturnCustomKey_WhenCustomKeyIsNotEmpty()
    {
        // Arrange
        var song = new Song { MusicalKey = "C", UserId = "user-123" };
        var setlistSong = new SetlistSong
        {
            CustomKey = "F#",
            Song = song
        };

        // Act
        var effectiveKey = setlistSong.EffectiveKey;

        // Assert
        effectiveKey.Should().Be("F#", "should return custom key when it's not empty");
    }

    [Fact]
    public void EffectiveKey_ShouldReturnNull_WhenCustomKeyIsNullAndSongIsNull()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomKey = null,
            Song = null
        };

        // Act
        var effectiveKey = setlistSong.EffectiveKey;

        // Assert
        effectiveKey.Should().BeNull("should return null when both custom key and song are null");
    }

    [Fact]
    public void EffectiveKey_ShouldReturnNull_WhenCustomKeyIsEmptyAndSongKeyIsNull()
    {
        // Arrange
        var song = new Song { MusicalKey = null, UserId = "user-123" };
        var setlistSong = new SetlistSong
        {
            CustomKey = "",
            Song = song
        };

        // Act
        var effectiveKey = setlistSong.EffectiveKey;

        // Assert
        effectiveKey.Should().BeNull("should return null when custom key is empty and song key is null");
    }

    #endregion

    #region HasCustomSettings Branch Coverage Tests

    [Fact]
    public void HasCustomSettings_ShouldReturnTrue_WhenOnlyCustomBpmIsSet()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomBpm = 120,
            CustomKey = null,
            PerformanceNotes = null
        };

        // Act
        var hasCustomSettings = setlistSong.HasCustomSettings;

        // Assert
        hasCustomSettings.Should().BeTrue("should return true when only custom BPM is set");
    }

    [Fact]
    public void HasCustomSettings_ShouldReturnTrue_WhenOnlyCustomKeyIsSet()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomBpm = null,
            CustomKey = "A",
            PerformanceNotes = null
        };

        // Act
        var hasCustomSettings = setlistSong.HasCustomSettings;

        // Assert
        hasCustomSettings.Should().BeTrue("should return true when only custom key is set");
    }

    [Fact]
    public void HasCustomSettings_ShouldReturnTrue_WhenOnlyPerformanceNotesIsSet()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomBpm = null,
            CustomKey = null,
            PerformanceNotes = "Play acoustic version"
        };

        // Act
        var hasCustomSettings = setlistSong.HasCustomSettings;

        // Assert
        hasCustomSettings.Should().BeTrue("should return true when only performance notes is set");
    }

    [Fact]
    public void HasCustomSettings_ShouldReturnFalse_WhenCustomKeyIsEmptyString()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomBpm = null,
            CustomKey = "", // Empty string
            PerformanceNotes = null
        };

        // Act
        var hasCustomSettings = setlistSong.HasCustomSettings;

        // Assert
        hasCustomSettings.Should().BeFalse("should return false when custom key is empty string");
    }

    [Fact]
    public void HasCustomSettings_ShouldReturnFalse_WhenPerformanceNotesIsEmptyString()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomBpm = null,
            CustomKey = null,
            PerformanceNotes = "" // Empty string
        };

        // Act
        var hasCustomSettings = setlistSong.HasCustomSettings;

        // Assert
        hasCustomSettings.Should().BeFalse("should return false when performance notes is empty string");
    }

    [Fact]
    public void HasCustomSettings_ShouldReturnTrue_WhenMultipleSettingsAreSet()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomBpm = 140,
            CustomKey = "Bb",
            PerformanceNotes = "Extended solo"
        };

        // Act
        var hasCustomSettings = setlistSong.HasCustomSettings;

        // Assert
        hasCustomSettings.Should().BeTrue("should return true when multiple custom settings are set");
    }

    [Fact]
    public void HasCustomSettings_ShouldReturnFalse_WhenAllSettingsAreNullOrEmpty()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomBpm = null,
            CustomKey = null,
            PerformanceNotes = null
        };

        // Act
        var hasCustomSettings = setlistSong.HasCustomSettings;

        // Assert
        hasCustomSettings.Should().BeFalse("should return false when all custom settings are null or empty");
    }

    [Fact]
    public void HasCustomSettings_ShouldReturnFalse_WhenAllSettingsAreEmpty()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomBpm = null,
            CustomKey = "",
            PerformanceNotes = ""
        };

        // Act
        var hasCustomSettings = setlistSong.HasCustomSettings;

        // Assert
        hasCustomSettings.Should().BeFalse("should return false when all custom settings are empty");
    }

    #endregion

    #region EffectiveBpm Null Handling Branch Coverage

    [Fact]
    public void EffectiveBpm_ShouldReturnNull_WhenSongIsNullAndCustomBpmIsNull()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomBpm = null,
            Song = null
        };

        // Act
        var effectiveBpm = setlistSong.EffectiveBpm;

        // Assert
        effectiveBpm.Should().BeNull("should return null when both song and custom BPM are null");
    }

    [Fact]
    public void EffectiveBpm_ShouldReturnCustomBpm_WhenSongIsNullButCustomBpmIsSet()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomBpm = 125,
            Song = null
        };

        // Act
        var effectiveBpm = setlistSong.EffectiveBpm;

        // Assert
        effectiveBpm.Should().Be(125, "should return custom BPM even when song is null");
    }

    #endregion

    #region Edge Cases for Complex Scenarios

    [Fact]
    public void SetlistSong_ShouldHandleAllPropertiesCorrectly_InComplexScenario()
    {
        // Arrange
        var song = new Song 
        { 
            Id = 1, 
            Title = "Bohemian Rhapsody", 
            Artist = "Queen", 
            Bpm = 72, 
            MusicalKey = "Bb",
            UserId = "user-123"
        };
        
        var setlist = new Setlist 
        { 
            Id = 1, 
            Name = "Queen Tribute Show", 
            UserId = "user-123" 
        };
        
        var setlistSong = new SetlistSong
        {
            Id = 10,
            Position = 5,
            TransitionNotes = "Build up the crowd energy",
            PerformanceNotes = "Full orchestral arrangement with gospel choir",
            IsEncore = true,
            IsOptional = false,
            CustomBpm = 80,
            CustomKey = "C",
            SetlistId = 1,
            Setlist = setlist,
            SongId = 1,
            Song = song
        };

        // Act & Assert - Test all calculated properties
        setlistSong.EffectiveBpm.Should().Be(80, "should use custom BPM over song BPM");
        setlistSong.EffectiveKey.Should().Be("C", "should use custom key over song key");
        setlistSong.HasCustomSettings.Should().BeTrue("should have custom settings when BPM, key, and notes are set");
        
        // Test that original song properties are not affected
        song.Bpm.Should().Be(72, "original song BPM should remain unchanged");
        song.MusicalKey.Should().Be("Bb", "original song key should remain unchanged");
    }

    #endregion
}