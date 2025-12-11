using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SetlistStudio.Core.Configuration;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Models;
using SetlistStudio.Infrastructure.Data;

namespace SetlistStudio.Infrastructure.Services;

public class SetlistDurationService : ISetlistDurationService
{
    private readonly ReadOnlySetlistStudioDbContext _readContext;
    private readonly ITransitionPredictionService _transitionPredictionService;
    private readonly SetlistOptions _options;
    private readonly ILogger<SetlistDurationService> _logger;

    public SetlistDurationService(
        ReadOnlySetlistStudioDbContext readContext,
        ITransitionPredictionService transitionPredictionService,
        IOptions<SetlistOptions> options,
        ILogger<SetlistDurationService> logger)
    {
        _readContext = readContext;
        _transitionPredictionService = transitionPredictionService;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    public async Task<SetlistDurationResult?> CalculateDurationAsync(int setlistId, string userId)
    {
        // Load minimal projection for efficiency
        var items = await _readContext.SetlistSongs
            .Where(ss => ss.SetlistId == setlistId && ss.Setlist.UserId == userId)
            .OrderBy(ss => ss.Position)
            .Select(ss => new
            {
                SetlistSongId = ss.Id,
                ss.Position,
                ss.CustomDurationOverride,
                SongId = ss.Song.Id,
                SongTitle = ss.Song.Title,
                EstimatedDuration = ss.Song.EstimatedDuration,
                DurationSeconds = ss.Song.DurationSeconds,
                Bpm = ss.CustomBpm ?? ss.Song.Bpm,
                Key = string.IsNullOrEmpty(ss.CustomKey) ? ss.Song.MusicalKey : ss.CustomKey
            })
            .ToListAsync();

        if (items == null || items.Count == 0)
        {
            _logger.LogInformation("CalculateDurationAsync: setlist {SetlistId} has no songs or not found for user {UserId}", setlistId, userId);
            return new SetlistDurationResult();
        }

        var dtoItems = new System.Collections.Generic.List<SetlistItemDurationDto>();

        double totalSongSeconds = 0;
        double totalTransitionSeconds = 0;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            TimeSpan? resolvedDuration = item.CustomDurationOverride;
            if (!resolvedDuration.HasValue && item.EstimatedDuration.HasValue)
                resolvedDuration = item.EstimatedDuration;
            if (!resolvedDuration.HasValue && item.DurationSeconds.HasValue)
                resolvedDuration = TimeSpan.FromSeconds(item.DurationSeconds.Value);
            if (!resolvedDuration.HasValue)
                resolvedDuration = TimeSpan.FromSeconds(_options.DefaultSongDurationSeconds);

            var resolvedSeconds = resolvedDuration.Value.TotalSeconds;
            totalSongSeconds += resolvedSeconds;

            // Compute transition to next
            double transitionSeconds = 0;
            if (i < items.Count - 1)
            {
                // Create temporary Song objects to use prediction service
                var currentSong = new Song { Id = item.SongId, Bpm = item.Bpm, MusicalKey = item.Key };
                var nextRaw = items[i + 1];
                var nextSong = new Song { Id = nextRaw.SongId, Bpm = nextRaw.Bpm, MusicalKey = nextRaw.Key };

                var predicted = _transitionPredictionService.PredictTransition(currentSong, nextSong);
                transitionSeconds = predicted.TotalSeconds;
                totalTransitionSeconds += transitionSeconds;
            }

            dtoItems.Add(new SetlistItemDurationDto
            {
                SetlistSongId = item.SetlistSongId,
                SongId = item.SongId,
                SongTitle = item.SongTitle,
                ResolvedDurationSeconds = resolvedSeconds,
                EstimatedDurationSeconds = item.EstimatedDuration?.TotalSeconds,
                CustomDurationOverrideSeconds = item.CustomDurationOverride?.TotalSeconds,
                PredictedTransitionSecondsToNext = transitionSeconds,
                Position = item.Position
            });
        }

        var combined = totalSongSeconds + totalTransitionSeconds;

        _logger.LogInformation("Duration calculation for setlist {SetlistId} by user {UserId}: items={Count}, totalSongSeconds={SongSeconds}, totalTransitionSeconds={TransitionSeconds}, combined={Combined}",
            setlistId, userId, items.Count, totalSongSeconds, totalTransitionSeconds, combined);

        return new SetlistDurationResult
        {
            TotalSongSeconds = totalSongSeconds,
            TotalTransitionSeconds = totalTransitionSeconds,
            CombinedTotalSeconds = combined,
            Items = dtoItems
        };
    }
}
