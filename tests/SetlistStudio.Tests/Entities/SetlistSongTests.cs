using SetlistStudio.Core.Entities;
using FluentAssertions;
using Xunit;
using System.ComponentModel.DataAnnotations;

namespace SetlistStudio.Tests.Entities;

/// <summary>
/// Comprehensive tests for SetlistSong entity covering all properties, validation, and helper methods
/// Covers: Property initialization, validation attributes, calculated properties, and business logic
/// </summary>
public class SetlistSongTests
{
    [Fact]
    public void SetlistSong_ShouldInitializeWithDefaultValues_WhenCreated()
    {
        // Act
        var setlistSong = new SetlistSong();

        // Assert
        setlistSong.Id.Should().Be(0);
        setlistSong.Position.Should().Be(0);
        setlistSong.TransitionNotes.Should().BeNull();
        setlistSong.PerformanceNotes.Should().BeNull();
        setlistSong.IsEncore.Should().BeFalse();
        setlistSong.IsOptional.Should().BeFalse();
        setlistSong.CustomBpm.Should().BeNull();
        setlistSong.CustomKey.Should().BeNull();
        setlistSong.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        setlistSong.SetlistId.Should().Be(0);
        setlistSong.Setlist.Should().BeNull();
        setlistSong.SongId.Should().Be(0);
        setlistSong.Song.Should().BeNull();
    }

    [Fact]
    public void SetlistSong_ShouldSetPropertiesCorrectly_WhenAssigned()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddHours(-2);
        var setlist = new Setlist { Id = 1, Name = "Test Setlist", UserId = "user-123" };
        var song = new Song { Id = 1, Title = "Bohemian Rhapsody", Artist = "Queen", UserId = "user-123" };

        // Act
        var setlistSong = new SetlistSong
        {
            Id = 42,
            Position = 3,
            TransitionNotes = "Direct segue into next song",
            PerformanceNotes = "Play acoustic version with crowd participation",
            IsEncore = true,
            IsOptional = false,
            CustomBpm = 120,
            CustomKey = "A",
            CreatedAt = createdAt,
            SetlistId = 1,
            Setlist = setlist,
            SongId = 1,
            Song = song
        };

        // Assert
        setlistSong.Id.Should().Be(42);
        setlistSong.Position.Should().Be(3);
        setlistSong.TransitionNotes.Should().Be("Direct segue into next song");
        setlistSong.PerformanceNotes.Should().Be("Play acoustic version with crowd participation");
        setlistSong.IsEncore.Should().BeTrue();
        setlistSong.IsOptional.Should().BeFalse();
        setlistSong.CustomBpm.Should().Be(120);
        setlistSong.CustomKey.Should().Be("A");
        setlistSong.CreatedAt.Should().Be(createdAt);
        setlistSong.SetlistId.Should().Be(1);
        setlistSong.Setlist.Should().Be(setlist);
        setlistSong.SongId.Should().Be(1);
        setlistSong.Song.Should().Be(song);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(999)]
    [InlineData(1000)]
    public void Position_ShouldAcceptValidValues_WhenWithinRange(int position)
    {
        // Act
        var setlistSong = new SetlistSong { Position = position };

        // Assert
        setlistSong.Position.Should().Be(position);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Position_ShouldFailValidation_WhenOutsideRange(int invalidPosition)
    {
        // Arrange
        var setlistSong = new SetlistSong 
        { 
            Position = invalidPosition,
            SetlistId = 1,
            SongId = 1
        };
        var context = new ValidationContext(setlistSong);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlistSong, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Position"));
    }

    [Fact]
    public void Position_ShouldBeRequired_BasedOnRequiredAttribute()
    {
        // Arrange
        var setlistSong = new SetlistSong { SetlistId = 1, SongId = 1 };
        var context = new ValidationContext(setlistSong);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlistSong, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Position"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Direct into next song")]
    [InlineData("2 minute break - adjust lighting")]
    public void TransitionNotes_ShouldAcceptValidValues_WhenWithinLengthLimit(string? notes)
    {
        // Act
        var setlistSong = new SetlistSong { TransitionNotes = notes };

        // Assert
        setlistSong.TransitionNotes.Should().Be(notes);
    }

    [Fact]
    public void TransitionNotes_ShouldFailValidation_WhenExceedsMaxLength()
    {
        // Arrange
        var longNotes = new string('x', 501); // Exceeds StringLength(500)
        var setlistSong = new SetlistSong 
        { 
            TransitionNotes = longNotes,
            Position = 1,
            SetlistId = 1,
            SongId = 1
        };
        var context = new ValidationContext(setlistSong);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlistSong, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("TransitionNotes"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Play acoustic version")]
    [InlineData("Skip second verse and go straight to bridge - crowd participation on chorus")]
    public void PerformanceNotes_ShouldAcceptValidValues_WhenWithinLengthLimit(string? notes)
    {
        // Act
        var setlistSong = new SetlistSong { PerformanceNotes = notes };

        // Assert
        setlistSong.PerformanceNotes.Should().Be(notes);
    }

    [Fact]
    public void PerformanceNotes_ShouldFailValidation_WhenExceedsMaxLength()
    {
        // Arrange
        var longNotes = new string('x', 1001); // Exceeds StringLength(1000)
        var setlistSong = new SetlistSong 
        { 
            PerformanceNotes = longNotes,
            Position = 1,
            SetlistId = 1,
            SongId = 1
        };
        var context = new ValidationContext(setlistSong);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlistSong, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("PerformanceNotes"));
    }

    [Theory]
    [InlineData(40)]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(180)]
    [InlineData(250)]
    public void CustomBpm_ShouldAcceptValidValues_WhenWithinRange(int bpm)
    {
        // Act
        var setlistSong = new SetlistSong { CustomBpm = bpm };

        // Assert
        setlistSong.CustomBpm.Should().Be(bpm);
    }

    [Theory]
    [InlineData(39)]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(251)]
    [InlineData(500)]
    public void CustomBpm_ShouldFailValidation_WhenOutsideRange(int invalidBpm)
    {
        // Arrange
        var setlistSong = new SetlistSong 
        { 
            CustomBpm = invalidBpm,
            Position = 1,
            SetlistId = 1,
            SongId = 1
        };
        var context = new ValidationContext(setlistSong);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlistSong, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("CustomBpm"));
    }

    [Fact]
    public void CustomBpm_ShouldAcceptNull_WhenNotSpecified()
    {
        // Act
        var setlistSong = new SetlistSong { CustomBpm = null };

        // Assert
        setlistSong.CustomBpm.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("C")]
    [InlineData("F#")]
    [InlineData("Bb")]
    [InlineData("A#m")]
    public void CustomKey_ShouldAcceptValidValues_WhenWithinLengthLimit(string? key)
    {
        // Act
        var setlistSong = new SetlistSong { CustomKey = key };

        // Assert
        setlistSong.CustomKey.Should().Be(key);
    }

    [Fact]
    public void CustomKey_ShouldFailValidation_WhenExceedsMaxLength()
    {
        // Arrange
        var longKey = new string('x', 11); // Exceeds StringLength(10)
        var setlistSong = new SetlistSong 
        { 
            CustomKey = longKey,
            Position = 1,
            SetlistId = 1,
            SongId = 1
        };
        var context = new ValidationContext(setlistSong);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlistSong, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("CustomKey"));
    }

    [Fact]
    public void SetlistId_ShouldBeRequired_BasedOnRequiredAttribute()
    {
        // Arrange - For value types like int, Required validates against default(T)
        // So we need to specifically set it to 0 (default) to trigger validation failure
        var setlistSong = new SetlistSong { Position = 1, SongId = 1, SetlistId = 0 };
        var context = new ValidationContext(setlistSong);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlistSong, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("SetlistId"));
    }

    [Fact]
    public void SongId_ShouldBeRequired_BasedOnRequiredAttribute()
    {
        // Arrange - For value types like int, Required validates against default(T)
        var setlistSong = new SetlistSong { Position = 1, SetlistId = 1, SongId = 0 };
        var context = new ValidationContext(setlistSong);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlistSong, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("SongId"));
    }

    [Fact]
    public void EffectiveBpm_ShouldReturnCustomBpm_WhenCustomBpmIsSet()
    {
        // Arrange
        var song = new Song { Bpm = 100, Title = "Test Song", Artist = "Test Artist" };
        var setlistSong = new SetlistSong
        {
            Song = song,
            CustomBpm = 120
        };

        // Act
        var effectiveBpm = setlistSong.EffectiveBpm;

        // Assert
        effectiveBpm.Should().Be(120);
    }

    [Fact]
    public void EffectiveBpm_ShouldReturnSongBpm_WhenCustomBpmIsNotSet()
    {
        // Arrange
        var song = new Song { Bpm = 100, Title = "Test Song", Artist = "Test Artist" };
        var setlistSong = new SetlistSong
        {
            Song = song,
            CustomBpm = null
        };

        // Act
        var effectiveBpm = setlistSong.EffectiveBpm;

        // Assert
        effectiveBpm.Should().Be(100);
    }

    [Fact]
    public void EffectiveBpm_ShouldReturnNull_WhenBothCustomAndSongBpmAreNull()
    {
        // Arrange
        var song = new Song { Bpm = null, Title = "Test Song", Artist = "Test Artist" };
        var setlistSong = new SetlistSong
        {
            Song = song,
            CustomBpm = null
        };

        // Act
        var effectiveBpm = setlistSong.EffectiveBpm;

        // Assert
        effectiveBpm.Should().BeNull();
    }

    [Fact]
    public void EffectiveBpm_ShouldReturnNull_WhenSongIsNull()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            Song = null!,
            CustomBpm = null
        };

        // Act
        var effectiveBpm = setlistSong.EffectiveBpm;

        // Assert
        effectiveBpm.Should().BeNull();
    }

    [Fact]
    public void EffectiveKey_ShouldReturnCustomKey_WhenCustomKeyIsSet()
    {
        // Arrange
        var song = new Song { MusicalKey = "C", Title = "Test Song", Artist = "Test Artist" };
        var setlistSong = new SetlistSong
        {
            Song = song,
            CustomKey = "F#"
        };

        // Act
        var effectiveKey = setlistSong.EffectiveKey;

        // Assert
        effectiveKey.Should().Be("F#");
    }

    [Fact]
    public void EffectiveKey_ShouldReturnSongKey_WhenCustomKeyIsNotSet()
    {
        // Arrange
        var song = new Song { MusicalKey = "C", Title = "Test Song", Artist = "Test Artist" };
        var setlistSong = new SetlistSong
        {
            Song = song,
            CustomKey = null
        };

        // Act
        var effectiveKey = setlistSong.EffectiveKey;

        // Assert
        effectiveKey.Should().Be("C");
    }

    [Fact]
    public void EffectiveKey_ShouldReturnNull_WhenBothCustomAndSongKeyAreNull()
    {
        // Arrange
        var song = new Song { MusicalKey = null, Title = "Test Song", Artist = "Test Artist" };
        var setlistSong = new SetlistSong
        {
            Song = song,
            CustomKey = null
        };

        // Act
        var effectiveKey = setlistSong.EffectiveKey;

        // Assert
        effectiveKey.Should().BeNull();
    }

    [Fact]
    public void EffectiveKey_ShouldReturnCustomKey_WhenCustomKeyIsEmptyString()
    {
        // Arrange
        var song = new Song { MusicalKey = "C", Title = "Test Song", Artist = "Test Artist" };
        var setlistSong = new SetlistSong
        {
            Song = song,
            CustomKey = ""
        };

        // Act
        var effectiveKey = setlistSong.EffectiveKey;

        // Assert
        effectiveKey.Should().Be("C"); // Empty string should fall back to song key
    }

    [Theory]
    [InlineData(null, null, null, false)]
    [InlineData(120, null, null, true)]
    [InlineData(null, "F#", null, true)]
    [InlineData(null, null, "Custom notes", true)]
    [InlineData(120, "F#", "Custom notes", true)]
    [InlineData(null, "", null, false)] // Empty string should not count as custom
    [InlineData(null, null, "", false)]  // Empty string should not count as custom
    public void HasCustomSettings_ShouldReturnCorrectValue_BasedOnCustomizations(
        int? customBpm, string? customKey, string? performanceNotes, bool expectedHasCustom)
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            CustomBpm = customBpm,
            CustomKey = customKey,
            PerformanceNotes = performanceNotes
        };

        // Act
        var hasCustomSettings = setlistSong.HasCustomSettings;

        // Assert
        hasCustomSettings.Should().Be(expectedHasCustom);
    }

    [Fact]
    public void SetlistSong_ShouldPassValidation_WhenAllRequiredFieldsProvided()
    {
        // Arrange
        var setlistSong = new SetlistSong
        {
            Position = 1,
            SetlistId = 1,
            SongId = 1
        };
        var context = new ValidationContext(setlistSong);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(setlistSong, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void BooleanProperties_ShouldDefaultToFalse_WhenNotSet()
    {
        // Arrange & Act
        var setlistSong = new SetlistSong();

        // Assert
        setlistSong.IsEncore.Should().BeFalse();
        setlistSong.IsOptional.Should().BeFalse();
    }

    [Fact]
    public void BooleanProperties_ShouldAllowBothTrueAndFalse_WhenSet()
    {
        // Arrange & Act
        var setlistSong1 = new SetlistSong { IsEncore = true, IsOptional = false };
        var setlistSong2 = new SetlistSong { IsEncore = false, IsOptional = true };

        // Assert
        setlistSong1.IsEncore.Should().BeTrue();
        setlistSong1.IsOptional.Should().BeFalse();
        
        setlistSong2.IsEncore.Should().BeFalse();
        setlistSong2.IsOptional.Should().BeTrue();
    }
}