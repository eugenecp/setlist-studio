using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SetlistStudio.Core.Configuration;
using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Configuration;
using SetlistStudio.Infrastructure.Data;
using SetlistStudio.Infrastructure.Services;
using Xunit;

namespace SetlistStudio.Tests.Infrastructure.Services;

public class SetlistDurationServiceTests : IDisposable
{
    private readonly SetlistStudioDbContext _context;
    private readonly SetlistDurationService _service;
    private readonly ReadOnlySetlistStudioDbContext _readContext;

    public SetlistDurationServiceTests()
    {
        var dbName = "TestDb_" + Guid.NewGuid();

        var writeOptions = new DbContextOptionsBuilder<SetlistStudioDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var readOptions = new DbContextOptionsBuilder<ReadOnlySetlistStudioDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        _context = new SetlistStudioDbContext(writeOptions);

        // Seed data
        var userId = "test-user";
        var song1 = new Song { Title = "Song1", UserId = userId, DurationSeconds = 120, Bpm = 100, MusicalKey = "C" };
        var song2 = new Song { Title = "Song2", UserId = userId, DurationSeconds = 180, Bpm = 120, MusicalKey = "D" };
        _context.Songs.AddRange(song1, song2);
        _context.SaveChanges();

        var setlist = new Setlist { Name = "TestSet", UserId = userId };
        _context.Setlists.Add(setlist);
        _context.SaveChanges();

        var ss1 = new SetlistSong { SetlistId = setlist.Id, SongId = song1.Id, Position = 1 };
        var ss2 = new SetlistSong { SetlistId = setlist.Id, SongId = song2.Id, Position = 2 };
        _context.SetlistSongs.AddRange(ss1, ss2);
        _context.SaveChanges();

        var setlistOptions = Options.Create(new SetlistOptions { DefaultSongDurationSeconds = 180 });
        var transitionService = new TransitionPredictionService(setlistOptions, new NullLogger<TransitionPredictionService>());

        // Mock IDatabaseConfiguration for DatabaseProviderService
        var mockConfig = new Mock<IDatabaseConfiguration>();
        mockConfig.Setup(c => c.Provider).Returns(DatabaseProvider.InMemory);
        mockConfig.Setup(c => c.WriteConnectionString).Returns(string.Empty);
        mockConfig.Setup(c => c.ReadConnectionStrings).Returns(new List<string>());
        mockConfig.Setup(c => c.MaxPoolSize).Returns(100);
        mockConfig.Setup(c => c.MinPoolSize).Returns(10);
        mockConfig.Setup(c => c.ConnectionTimeout).Returns(30);
        mockConfig.Setup(c => c.CommandTimeout).Returns(30);
        mockConfig.Setup(c => c.EnablePooling).Returns(false);
        mockConfig.Setup(c => c.HasReadReplicas).Returns(false);
        mockConfig.Setup(c => c.GetReadConnectionString()).Returns(string.Empty);

        var dbProvider = new DatabaseProviderService(mockConfig.Object, new NullLogger<DatabaseProviderService>());

        _readContext = new ReadOnlySetlistStudioDbContext(readOptions, dbProvider);

        _service = new SetlistDurationService(_readContext, transitionService, setlistOptions, new NullLogger<SetlistDurationService>());
    }

    [Fact]
    public async Task CalculateDurationAsync_MultipleSongs_ReturnsCombinedTotal()
    {
        var result = await _service.CalculateDurationAsync(1, "test-user");
        result.Should().NotBeNull();
        result!.TotalSongSeconds.Should().BeGreaterThan(0);
        result.TotalTransitionSeconds.Should().BeGreaterThanOrEqualTo(0);
        result.CombinedTotalSeconds.Should().BeApproximately(result.TotalSongSeconds + result.TotalTransitionSeconds, 0.001);
        result.Items.Count().Should().Be(2);
    }

    public void Dispose()
    {
        _context.Dispose();
        _readContext.Dispose();
    }
}
