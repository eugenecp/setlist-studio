using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Web.Shared;
using Xunit;
using FluentAssertions;
using System.Reflection;

namespace SetlistStudio.Tests.Web.Shared;

/// <summary>
/// Unit tests for the DownloadForOffline component, focusing on testable component logic
/// and properties. Avoids UI rendering to prevent MudBlazor dependency issues.
/// </summary>
public class DownloadForOfflineTests : IDisposable
{
    private readonly Mock<IJSRuntime> _mockJSRuntime;

    public DownloadForOfflineTests()
    {
        _mockJSRuntime = new Mock<IJSRuntime>();
    }

    void IDisposable.Dispose()
    {
        // Cleanup resources if needed
    }

    /// <summary>
    /// Creates a DownloadForOffline component instance for testing.
    /// Uses direct instantiation with reflection to set dependencies without rendering.
    /// </summary>
    private DownloadForOffline CreateTestableComponent(IEnumerable<Song>? songs = null)
    {
        var component = new DownloadForOffline();
        
        // Set the JS runtime field using reflection
        var jsField = typeof(DownloadForOffline).GetField("JS", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        jsField?.SetValue(component, _mockJSRuntime.Object);
        
        // Set Songs parameter if provided
        if (songs != null)
        {
            var songsProperty = typeof(DownloadForOffline).GetProperty("Songs");
            songsProperty?.SetValue(component, songs);
        }
        
        return component;
    }

    /// <summary>
    /// Gets a private property value using reflection
    /// </summary>
    private T GetPrivateProperty<T>(DownloadForOffline component, string propertyName)
    {
        var property = typeof(DownloadForOffline).GetProperty(propertyName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)property!.GetValue(component)!;
    }

    /// <summary>
    /// Gets a private field value using reflection
    /// </summary>
    private T GetPrivateField<T>(DownloadForOffline component, string fieldName)
    {
        var field = typeof(DownloadForOffline).GetField($"<{fieldName}>k__BackingField", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)field!.GetValue(component)!;
    }

    [Fact]
    public void HasSongsToCache_ShouldReturnTrueWhenSongsExist()
    {
        // Arrange
        var songs = CreateTestSongs();
        var component = CreateTestableComponent(songs);

        // Act
        var result = GetPrivateProperty<bool>(component, "HasSongsToCache");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasSongsToCache_ShouldReturnFalseWhenSongsEmpty()
    {
        // Arrange
        var songs = new List<Song>();
        var component = CreateTestableComponent(songs);

        // Act
        var result = GetPrivateProperty<bool>(component, "HasSongsToCache");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasSongsToCache_ShouldReturnFalseWhenSongsNull()
    {
        // Arrange
        var component = CreateTestableComponent();

        // Act
        var result = GetPrivateProperty<bool>(component, "HasSongsToCache");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Component_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var component = CreateTestableComponent();

        // Assert
        component.Should().NotBeNull();
        var isDownloading = GetPrivateField<bool>(component, "IsDownloading");
        var isDownloaded = GetPrivateField<bool>(component, "IsDownloaded");
        var isCachingSongs = GetPrivateField<bool>(component, "IsCachingSongs");
        
        isDownloading.Should().BeFalse();
        isDownloaded.Should().BeFalse();
        isCachingSongs.Should().BeFalse();
    }

    [Fact]
    public void Component_ShouldAcceptSetlistParameter()
    {
        // Arrange
        var setlist = CreateTestSetlist();
        var component = CreateTestableComponent();

        // Act
        var setlistProperty = typeof(DownloadForOffline).GetProperty("Setlist");
        setlistProperty?.SetValue(component, setlist);

        // Assert
        var currentSetlist = setlistProperty?.GetValue(component) as Setlist;
        currentSetlist.Should().NotBeNull();
        currentSetlist!.Name.Should().Be("Test Setlist");
    }

    [Fact]
    public void Component_ShouldAcceptSongsParameter()
    {
        // Arrange
        var songs = CreateTestSongs();
        var component = CreateTestableComponent();

        // Act
        var songsProperty = typeof(DownloadForOffline).GetProperty("Songs");
        songsProperty?.SetValue(component, songs);

        // Assert
        var currentSongs = songsProperty?.GetValue(component) as IEnumerable<Song>;
        currentSongs.Should().NotBeNull();
        currentSongs!.Should().HaveCount(2);
    }

    /// <summary>
    /// Creates test songs for unit testing
    /// </summary>
    private static List<Song> CreateTestSongs()
    {
        return new List<Song>
        {
            new Song
            {
                Id = 1,
                Title = "Sweet Child O' Mine",
                Artist = "Guns N' Roses",
                Bpm = 125,
                MusicalKey = "D"
            },
            new Song
            {
                Id = 2,
                Title = "Billie Jean",
                Artist = "Michael Jackson",
                Bpm = 117,
                MusicalKey = "F#m"
            }
        };
    }

    /// <summary>
    /// Creates a test setlist for unit testing
    /// </summary>
    private static Setlist CreateTestSetlist()
    {
        return new Setlist
        {
            Id = 1,
            Name = "Test Setlist",
            Description = "Test description",
            CreatedAt = DateTime.UtcNow
        };
    }
}