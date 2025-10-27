using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SetlistStudio.Core.Entities;
using SetlistStudio.Web.Shared;
using Xunit;
using FluentAssertions;
using MudBlazor.Services;
using Microsoft.JSInterop;
using Moq;
using Microsoft.AspNetCore.Components;
using System.Linq;

namespace SetlistStudio.Tests.Web.Shared;

/// <summary>
/// Tests for MobileSetlistCard component to ensure proper rendering and functionality
/// </summary>
public class MobileSetlistCardTests : TestContext
{
    private readonly Mock<IJSRuntime> _mockJSRuntime;

    public MobileSetlistCardTests()
    {
        _mockJSRuntime = new Mock<IJSRuntime>();
        
        // Register MudBlazor services
        Services.AddMudServices();
        
        // Register mocked services
        Services.AddScoped<IJSRuntime>(_ => _mockJSRuntime.Object);
    }

    [Fact]
    public void MobileSetlistCard_ShouldRender_WithBasicSetlistData()
    {
        // Arrange
        var setlist = CreateTestSetlist("Test Setlist", "Test Venue");

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        component.Should().NotBeNull();
        component.Find(".setlist-title").TextContent.Should().Contain("Test Setlist");
        component.Find(".venue-name").TextContent.Should().Contain("Test Venue");
    }

    [Fact]
    public void MobileSetlistCard_ShouldDisplaySongCount_WhenSetlistHasSongs()
    {
        // Arrange
        var setlist = CreateTestSetlist("Rock Setlist", "Stadium");
        setlist.SetlistSongs = new List<SetlistSong>
        {
            CreateTestSetlistSong("Bohemian Rhapsody", 1),
            CreateTestSetlistSong("Sweet Child O' Mine", 2),
            CreateTestSetlistSong("Billie Jean", 3)
        };

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var metricsText = component.Find(".performance-metrics").TextContent;
        metricsText.Should().Contain("3");
        metricsText.Should().Contain("Songs");
    }

    [Fact]
    public void MobileSetlistCard_ShouldDisplayDuration_WhenExpectedDurationIsSet()
    {
        // Arrange
        var setlist = CreateTestSetlist("Jazz Night", "Blue Note");
        setlist.ExpectedDurationMinutes = 90;

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var metricsText = component.Find(".performance-metrics").TextContent;
        metricsText.Should().Contain("1h 30m");
        metricsText.Should().Contain("Duration");
    }

    [Fact]
    public void GetAverageBpm_ShouldCalculateCorrectly_WhenSongsHaveBpm()
    {
        // Arrange
        var setlist = CreateTestSetlist("High Energy Set", "Club");
        setlist.SetlistSongs = new List<SetlistSong>
        {
            CreateTestSetlistSongWithBpm("Fast Song", 1, 150),
            CreateTestSetlistSongWithBpm("Medium Song", 2, 120),
            CreateTestSetlistSongWithBpm("Slow Song", 3, 90)
        };

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        // Average BPM should be (150 + 120 + 90) / 3 = 120
        var metricsText = component.Find(".performance-metrics").TextContent;
        metricsText.Should().Contain("120");
        metricsText.Should().Contain("Avg BPM");
    }

    [Fact]
    public void GetAverageBpm_ShouldReturnZero_WhenNoSongsHaveBpm()
    {
        // Arrange
        var setlist = CreateTestSetlist("Acoustic Set", "Coffee Shop");
        setlist.SetlistSongs = new List<SetlistSong>
        {
            CreateTestSetlistSong("Song Without BPM", 1)
        };

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        // Should not display BPM section when average is 0
        var metricsText = component.Find(".performance-metrics").TextContent;
        metricsText.Should().NotContain("Avg BPM");
    }

    [Fact]
    public void MobileSetlistCard_ShouldDisplayPerformanceDate_WhenDateIsToday()
    {
        // Arrange
        var setlist = CreateTestSetlist("Tonight's Show", "Main Stage");
        setlist.PerformanceDate = DateTime.Today.AddHours(20); // 8 PM today

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var statusIndicator = component.Find(".performance-status-indicator");
        statusIndicator.TextContent.Should().Contain("TODAY");
        statusIndicator.TextContent.Should().Contain("8:00 PM");
    }

    [Fact]
    public void MobileSetlistCard_ShouldDisplayUpcomingPerformance_WhenDateIsFuture()
    {
        // Arrange
        var setlist = CreateTestSetlist("Future Concert", "Arena");
        setlist.PerformanceDate = DateTime.Now.AddDays(7).Date.AddHours(19); // Next week at 7 PM

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var statusIndicator = component.Find(".performance-status-indicator");
        statusIndicator.TextContent.Should().Contain("📅");
        statusIndicator.TextContent.Should().Contain("7:00 PM");
    }

    [Fact]
    public void MobileSetlistCard_ShouldDisplayPastPerformance_WhenDateIsInPast()
    {
        // Arrange
        var setlist = CreateTestSetlist("Last Week's Show", "Theater");
        setlist.PerformanceDate = DateTime.Now.AddDays(-7);

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var statusIndicator = component.Find(".performance-status-indicator");
        var statusText = statusIndicator.TextContent;
        statusText.Should().NotContain("TODAY");
        statusText.Should().NotContain("📅");
        // Should show past date without special highlighting
    }

    [Fact]
    public void MobileSetlistCard_ShouldDisplayTemplateBadge_WhenSetlistIsTemplate()
    {
        // Arrange
        var setlist = CreateTestSetlist("Wedding Template", null);
        setlist.IsTemplate = true;

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var badges = component.Find(".status-badges");
        badges.TextContent.Should().Contain("📋 Template");
    }

    [Fact]
    public void MobileSetlistCard_ShouldDisplayActiveBadge_WhenSetlistIsActive()
    {
        // Arrange
        var setlist = CreateTestSetlist("Current Rotation", "Various");
        setlist.IsActive = true;

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var badges = component.Find(".status-badges");
        badges.TextContent.Should().Contain("🎵 Active");
    }

    [Fact]
    public void MobileSetlistCard_ShouldHaveStartPerformanceButton()
    {
        // Arrange
        var setlist = CreateTestSetlist("Concert", "Hall");

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var startButton = component.Find(".performance-focus");
        startButton.TextContent.Should().Contain("Start Performance");
        startButton.GetAttribute("href").Should().Be($"/setlists/{setlist.Id}");
    }

    [Fact]
    public void MobileSetlistCard_ShouldHaveEditAndShareButtons()
    {
        // Arrange
        var setlist = CreateTestSetlist("Editable Set", "Studio");

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var editButton = component.Find("a[href*='edit']");
        editButton.TextContent.Should().Contain("Edit");
        editButton.GetAttribute("href").Should().Be($"/setlists/{setlist.Id}/edit");

        var shareButtons = component.FindAll("button").Where(b => b.TextContent.Contains("Share"));
        shareButtons.Should().NotBeEmpty();
        shareButtons.First().TextContent.Should().Contain("Share");
    }

    [Fact]
    public void FormatDuration_ShouldDisplayMinutes_WhenUnderOneHour()
    {
        // Arrange
        var setlist = CreateTestSetlist("Short Set", "Bar");
        setlist.ExpectedDurationMinutes = 45;

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var metricsText = component.Find(".performance-metrics").TextContent;
        metricsText.Should().Contain("45m");
    }

    [Fact]
    public void FormatDuration_ShouldDisplayHoursAndMinutes_WhenOverOneHour()
    {
        // Arrange
        var setlist = CreateTestSetlist("Long Concert", "Stadium");
        setlist.ExpectedDurationMinutes = 135; // 2 hours 15 minutes

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var metricsText = component.Find(".performance-metrics").TextContent;
        metricsText.Should().Contain("2h 15m");
    }

    [Fact]
    public void FormatDuration_ShouldDisplayExactHours_WhenNoMinutes()
    {
        // Arrange
        var setlist = CreateTestSetlist("Two Hour Set", "Theater");
        setlist.ExpectedDurationMinutes = 120; // Exactly 2 hours

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var metricsText = component.Find(".performance-metrics").TextContent;
        metricsText.Should().Contain("2h");
        metricsText.Should().NotContain("0m");
    }

    [Fact]
    public void MobileSetlistCard_ShouldHandleMenuCallback()
    {
        // Arrange
        var setlist = CreateTestSetlist("Menu Test", "Venue");
        // Act & Assert - Just verify the component accepts the callback parameter without error
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist)
            .Add(p => p.OnShowMenu, EventCallback.Factory.Create<int>(this, (id) =>
            {
                // Callback is configured and ready to use
            })));

        // Component should render successfully with callback configured
        component.Should().NotBeNull();
        var menuButton = component.FindAll("button").FirstOrDefault();
        menuButton.Should().NotBeNull();
        
        // The component accepts the callback parameter and is ready to use it
        // (In a real scenario, the callback would be triggered by user interaction)
    }

    [Fact]
    public void MobileSetlistCard_ShouldHandleShareCallback()
    {
        // Arrange
        var setlist = CreateTestSetlist("Share Test", "Location");
        var shareClicked = false;
        Setlist? sharedSetlist = null;

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist)
            .Add(p => p.OnShare, EventCallback.Factory.Create<Setlist>(this, (s) =>
            {
                shareClicked = true;
                sharedSetlist = s;
            })));

        var shareButton = component.FindAll("button").Where(b => b.TextContent.Contains("Share")).First();
        shareButton.Click();

        // Assert
        shareClicked.Should().BeTrue();
        sharedSetlist.Should().Be(setlist);
    }

    [Fact]
    public void MobileSetlistCard_ShouldInitializeJavaScript_OnFirstRender()
    {
        // Arrange
        var setlist = CreateTestSetlist("JS Test", "Venue");

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert - Component should render successfully, JS initialization is called internally
        component.Should().NotBeNull();
        var swipeItem = component.Find($"[data-setlist-id='{setlist.Id}']");
        swipeItem.Should().NotBeNull();
        
        // Verify that any JS calls were made (this tests the JS integration setup)
        _mockJSRuntime.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public void MobileSetlistCard_ShouldHaveCorrectSwipeContainer()
    {
        // Arrange
        var setlist = CreateTestSetlist("Swipe Test", "Touch Venue");

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var swipeContainer = component.Find(".swipe-container");
        swipeContainer.Should().NotBeNull();
        
        var swipeItem = component.Find($"[data-setlist-id='{setlist.Id}']");
        swipeItem.Should().NotBeNull();
    }

    [Fact]
    public void MobileSetlistCard_ShouldNotDisplayDescription_WhenEmpty()
    {
        // Arrange
        var setlist = CreateTestSetlist("No Description", "Venue");
        setlist.Description = null;

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var hasDescription = component.FindAll(".mb-4.performance-mode").Any();
        hasDescription.Should().BeFalse();
    }

    [Fact]
    public void MobileSetlistCard_ShouldDisplayDescription_WhenProvided()
    {
        // Arrange
        var setlist = CreateTestSetlist("With Description", "Venue");
        setlist.Description = "This is a test setlist description for the mobile card component.";

        // Act
        var component = RenderComponent<MobileSetlistCard>(parameters => parameters
            .Add(p => p.Setlist, setlist));

        // Assert
        var descriptionElement = component.Find(".mb-4.performance-mode");
        descriptionElement.TextContent.Should().Contain("This is a test setlist description");
    }

    private static Setlist CreateTestSetlist(string name, string? venue)
    {
        return new Setlist
        {
            Id = Random.Shared.Next(1, 1000),
            Name = name,
            Venue = venue,
            UserId = "test-user"
        };
    }

    private static SetlistSong CreateTestSetlistSong(string songName, int position)
    {
        return new SetlistSong
        {
            Id = Random.Shared.Next(1, 1000),
            Position = position,
            Song = new Song
            {
                Id = Random.Shared.Next(1, 1000),
                Title = songName,
                Artist = "Test Artist",
                UserId = "test-user"
            }
        };
    }

    private static SetlistSong CreateTestSetlistSongWithBpm(string songName, int position, int bpm)
    {
        var setlistSong = CreateTestSetlistSong(songName, position);
        setlistSong.Song!.Bpm = bpm;
        return setlistSong;
    }
}