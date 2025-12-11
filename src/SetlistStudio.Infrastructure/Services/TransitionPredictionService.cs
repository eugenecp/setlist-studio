using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SetlistStudio.Core.Configuration;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;

namespace SetlistStudio.Infrastructure.Services;

public class TransitionPredictionService : ITransitionPredictionService
{
    private readonly SetlistOptions _options;
    private readonly ILogger<TransitionPredictionService> _logger;

    public TransitionPredictionService(IOptions<SetlistOptions> options, ILogger<TransitionPredictionService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    public TimeSpan PredictTransition(Song? a, Song? b)
    {
        try
        {
            // Base transition
            double seconds = _options.BaseTransitionSeconds;

            if (a == null || b == null)
            {
                _logger?.LogDebug("PredictTransition: one or both songs null; using base {BaseSeconds}", _options.BaseTransitionSeconds);
                return TimeSpan.FromSeconds(Math.Min(seconds, _options.MaxTransitionSeconds));
            }

            // BPM difference penalty
            if (a.Bpm.HasValue && b.Bpm.HasValue)
            {
                var bpmDiff = Math.Abs(a.Bpm.Value - b.Bpm.Value);
                var bpmPenalty = bpmDiff * _options.BpmDifferencePenaltyMultiplier;
                seconds += bpmPenalty;
            }
            else
            {
                _logger?.LogDebug("PredictTransition: missing BPM for songs {AId}/{BId}", a.Id, b.Id);
            }

            // Key compatibility check (simple rule: identical base key or relative minor/major)
            var keyA = NormalizeKey(a.MusicalKey);
            var keyB = NormalizeKey(b.MusicalKey);
            if (!string.IsNullOrEmpty(keyA) && !string.IsNullOrEmpty(keyB))
            {
                if (!AreKeysCompatible(keyA, keyB))
                {
                    seconds += _options.KeyMismatchPenaltySeconds;
                }
            }
            else
            {
                _logger?.LogDebug("PredictTransition: missing key for songs {AId}/{BId}", a.Id, b.Id);
            }

            var final = Math.Min(seconds, _options.MaxTransitionSeconds);
            _logger?.LogDebug("PredictTransition: computed {Seconds}s between {AId}->{BId}", final, a.Id, b.Id);
            return TimeSpan.FromSeconds(final);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "PredictTransition: failed to compute transition, falling back to base seconds");
            return TimeSpan.FromSeconds(_options.BaseTransitionSeconds);
        }
    }

    private static string NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        return key.Trim().ToUpperInvariant().Replace("?", "#").Replace("?", "B");
    }

    private static bool AreKeysCompatible(string a, string b)
    {
        if (a == b) return true;

        // Simple compatibility: same root letter (C ~ Cm compatible), or relative major/minor pairs
        // Map relative pairs
        var relativePairs = new (string Major, string Minor)[]
        {
            ("C", "AM"), ("G", "EM"), ("D", "BM"), ("A", "FM#"), ("E", "CM#"), ("B", "GM#"), ("F#", "DM#"), ("C#", "AM#"),
            ("F", "DM"), ("Bb", "GM"), ("Eb", "CM"), ("Ab", "FM"), ("Db", "Bbm"), ("Gb", "Ebm"), ("Cb", "Abm")
        };

        a = a.Replace("M", "M");
        b = b.Replace("M", "M");

        // Simplify by checking first letter
        if (a.Length > 0 && b.Length > 0 && a[0] == b[0]) return true;

        // Check relative pairs loosely
        foreach (var pair in relativePairs)
        {
            if ((pair.Major == a && pair.Minor == b) || (pair.Major == b && pair.Minor == a)) return true;
        }

        return false;
    }
}
