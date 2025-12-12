using SetlistStudio.Core.Entities;
using SetlistStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SetlistStudio.Infrastructure.Services
{
    /// <summary>
    /// Service for personalizing song recommendations based on user listening history.
    /// Analyzes user interaction patterns to suggest songs they're likely to enjoy.
    /// </summary>
    public class PersonalizedRecommendationService
    {
        private readonly IDbContextFactory<SetlistStudioDbContext> _contextFactory;

        public PersonalizedRecommendationService(IDbContextFactory<SetlistStudioDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Gets personalized song recommendations based on user's listening patterns.
        /// Analyzes:
        /// - Most frequently used genres
        /// - Preferred BPM ranges
        /// - Musical keys the user works with
        /// - Difficulty levels they typically perform
        /// - Similar songs to those in their setlists
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="maxResults">Maximum recommendations to return</param>
        /// <returns>List of personalized song recommendations</returns>
        public async Task<List<PersonalizedSongRecommendationDto>> GetPersonalizedRecommendationsAsync(
            string userId,
            int maxResults = 10)
        {
            using var context = _contextFactory.CreateDbContext();

            // Get user's songs
            var userSongs = await context.Songs
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (!userSongs.Any())
                return new List<PersonalizedSongRecommendationDto>();

            // Analyze user patterns
            var userPatterns = AnalyzeUserPatterns(userSongs);

            // Get setlist usage patterns
            var setlistPatterns = await AnalyzeSetlistPatternsAsync(context, userId, userSongs);

            // Score songs based on patterns
            var scoredSongs = userSongs
                .Where(s => s != null)
                .Select(song => new
                {
                    Song = song,
                    Score = CalculatePersonalizationScore(
                        song,
                        userPatterns,
                        setlistPatterns,
                        userSongs.Count)
                })
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => new PersonalizedSongRecommendationDto
                {
                    SongId = x.Song.Id,
                    Title = x.Song.Title,
                    Artist = x.Song.Artist,
                    Genre = x.Song.Genre,
                    Bpm = x.Song.Bpm ?? 0,
                    MusicalKey = x.Song.MusicalKey,
                    DurationSeconds = x.Song.DurationSeconds ?? 0,
                    DifficultyRating = x.Song.DifficultyRating ?? 0,
                    RecommendationScore = x.Score,
                    RecommendationReason = GetRecommendationReason(x.Song, userPatterns, setlistPatterns)
                })
                .ToList();

            return scoredSongs;
        }

        /// <summary>
        /// Analyzes user's song library to identify patterns.
        /// </summary>
        private UserPattern AnalyzeUserPatterns(List<Song> userSongs)
        {
            var pattern = new UserPattern();

            // Genre analysis
            var genreCounts = userSongs
                .Where(s => !string.IsNullOrWhiteSpace(s.Genre))
                .GroupBy(s => s.Genre)
                .OrderByDescending(g => g.Count())
                .Select(g => new { Genre = g.Key, Count = g.Count() })
                .ToList();

            pattern.FavoriteGenres = genreCounts.Take(3).Select(g => g.Genre!).ToList();

            // BPM analysis
            var validBpms = userSongs
                .Where(s => s.Bpm.HasValue && s.Bpm.Value > 0)
                .Select(s => s.Bpm ?? 0)
                .ToList();

            if (validBpms.Any())
            {
                pattern.AverageBpm = (int)validBpms.Average();
                pattern.BpmRange = (validBpms.Min(), validBpms.Max());
            }

            // Musical key analysis
            var keyCounts = userSongs
                .Where(s => !string.IsNullOrWhiteSpace(s.MusicalKey))
                .GroupBy(s => s.MusicalKey)
                .OrderByDescending(k => k.Count())
                .Select(k => k.Key)
                .Take(3)
                .ToList();

            pattern.FavoriteKeys = keyCounts;

            // Difficulty analysis
            var validDifficulties = userSongs
                .Where(s => s.DifficultyRating.HasValue && s.DifficultyRating.Value > 0)
                .Select(s => s.DifficultyRating.Value)
                .ToList();

            if (validDifficulties.Any())
            {
                pattern.AverageDifficulty = (decimal)validDifficulties.Average();
            }

            return pattern;
        }

        /// <summary>
        /// Analyzes how songs are used in setlists to understand user preferences.
        /// </summary>
        private async Task<SetlistPattern> AnalyzeSetlistPatternsAsync(
            SetlistStudioDbContext context,
            string userId,
            List<Song> userSongs)
        {
            var pattern = new SetlistPattern();

            var userSetlists = await context.Setlists
                .Where(sl => sl.UserId == userId)
                .Include(sl => sl.SetlistSongs)
                .ToListAsync();

            if (!userSetlists.Any())
                return pattern;

            // Find most frequently used songs
            var songUsageCount = new Dictionary<int, int>();
            foreach (var setlist in userSetlists)
            {
                foreach (var setlistSong in setlist.SetlistSongs ?? new List<SetlistSong>())
                {
                    if (songUsageCount.ContainsKey(setlistSong.SongId))
                        songUsageCount[setlistSong.SongId]++;
                    else
                        songUsageCount[setlistSong.SongId] = 1;
                }
            }

            if (songUsageCount.Any())
            {
                pattern.MostUsedSongIds = songUsageCount
                    .OrderByDescending(x => x.Value)
                    .Take(5)
                    .Select(x => x.Key)
                    .ToList();

                pattern.AverageSetlistLength = (int)userSetlists.Average(sl => sl.SetlistSongs?.Count ?? 0);
            }

            return pattern;
        }

        /// <summary>
        /// Calculates personalization score for a song based on user patterns.
        /// </summary>
        private decimal CalculatePersonalizationScore(
            Song song,
            UserPattern userPatterns,
            SetlistPattern setlistPatterns,
            int totalSongCount)
        {
            decimal score = 50m; // Base score

            // Genre preference (weight: 35%)
            if (!string.IsNullOrWhiteSpace(song.Genre) && userPatterns.FavoriteGenres.Any())
            {
                if (userPatterns.FavoriteGenres.Contains(song.Genre, StringComparer.OrdinalIgnoreCase))
                    score += 35m;
                else if (userPatterns.FavoriteGenres.Count > 0)
                    score += 10m; // Small bonus for being in library
            }

            // BPM preference (weight: 25%)
            if (song.Bpm.HasValue && userPatterns.BpmRange != (0, 0))
            {
                var (minBpm, maxBpm) = userPatterns.BpmRange;
                var bpmDiff = Math.Abs(song.Bpm.Value - userPatterns.AverageBpm);

                if (bpmDiff <= 20)
                    score += 25m;
                else if (bpmDiff <= 40)
                    score += 15m;
                else if (song.Bpm.Value >= minBpm && song.Bpm.Value <= maxBpm)
                    score += 10m;
            }

            // Musical key preference (weight: 15%)
            if (!string.IsNullOrWhiteSpace(song.MusicalKey) && userPatterns.FavoriteKeys.Any())
            {
                if (userPatterns.FavoriteKeys.Contains(song.MusicalKey, StringComparer.OrdinalIgnoreCase))
                    score += 15m;
            }

            // Difficulty preference (weight: 15%)
            if (song.DifficultyRating.HasValue && userPatterns.AverageDifficulty > 0)
            {
                var diffDiff = Math.Abs(song.DifficultyRating.Value - (int)userPatterns.AverageDifficulty);
                if (diffDiff == 0)
                    score += 15m;
                else if (diffDiff <= 1)
                    score += 10m;
                else if (diffDiff <= 2)
                    score += 5m;
            }

            // Popularity in setlists (weight: 10%)
            if (setlistPatterns.MostUsedSongIds.Contains(song.Id))
            {
                var position = setlistPatterns.MostUsedSongIds.IndexOf(song.Id);
                score += (10m - position); // Higher score for more frequently used songs
            }

            return Math.Min(100m, Math.Max(0m, score));
        }

        /// <summary>
        /// Generates a human-readable reason for the recommendation.
        /// </summary>
        private string GetRecommendationReason(
            Song song,
            UserPattern userPatterns,
            SetlistPattern setlistPatterns)
        {
            var reasons = new List<string>();

            // Check what made this song recommended
            if (!string.IsNullOrWhiteSpace(song.Genre) && 
                userPatterns.FavoriteGenres.Contains(song.Genre, StringComparer.OrdinalIgnoreCase))
            {
                reasons.Add($"Matches your favorite genre: {song.Genre}");
            }

            if (song.Bpm.HasValue && userPatterns.AverageBpm > 0)
            {
                var bpmDiff = Math.Abs(song.Bpm.Value - userPatterns.AverageBpm);
                if (bpmDiff <= 20)
                    reasons.Add($"Similar tempo to your preferences ({song.Bpm} BPM)");
            }

            if (setlistPatterns.MostUsedSongIds.Contains(song.Id))
            {
                reasons.Add("Frequently used in your setlists");
            }

            if (song.DifficultyRating.HasValue && userPatterns.AverageDifficulty > 0)
            {
                var diffDiff = Math.Abs(song.DifficultyRating.Value - (int)userPatterns.AverageDifficulty);
                if (diffDiff == 0)
                    reasons.Add("Matches your skill level");
            }

            return reasons.Any() ? string.Join(" • ", reasons) : "Recommended for you";
        }
    }

    /// <summary>
    /// User listening pattern analysis.
    /// </summary>
    internal class UserPattern
    {
        public List<string> FavoriteGenres { get; set; } = new();
        public (int Min, int Max) BpmRange { get; set; }
        public int AverageBpm { get; set; }
        public List<string> FavoriteKeys { get; set; } = new();
        public decimal AverageDifficulty { get; set; }
    }

    /// <summary>
    /// Setlist usage pattern analysis.
    /// </summary>
    internal class SetlistPattern
    {
        public List<int> MostUsedSongIds { get; set; } = new();
        public int AverageSetlistLength { get; set; }
    }

    /// <summary>
    /// DTO for personalized song recommendation.
    /// </summary>
    public class PersonalizedSongRecommendationDto
    {
        public int SongId { get; set; }
        public string? Title { get; set; }
        public string? Artist { get; set; }
        public string? Genre { get; set; }
        public int Bpm { get; set; }
        public string? MusicalKey { get; set; }
        public int DurationSeconds { get; set; }
        public int DifficultyRating { get; set; }

        /// <summary>
        /// Personalization score (0-100) based on user patterns.
        /// </summary>
        public decimal RecommendationScore { get; set; }

        /// <summary>
        /// Human-readable reason why this song is recommended.
        /// </summary>
        public string? RecommendationReason { get; set; }
    }
}
