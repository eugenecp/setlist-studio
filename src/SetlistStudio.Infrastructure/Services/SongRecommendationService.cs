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
    /// Provides intelligent song recommendation engine based on musical compatibility.
    /// Suggests next songs for setlist building using BPM flow, genre, key compatibility, and difficulty balance.
    /// </summary>
    public class SongRecommendationService
    {
        private readonly IDbContextFactory<SetlistStudioDbContext> _contextFactory;

        public SongRecommendationService(IDbContextFactory<SetlistStudioDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Gets recommended songs to follow the current song in a setlist.
        /// Scores songs based on:
        /// - BPM progression (avoid jarring tempo changes)
        /// - Genre compatibility (genre similarity)
        /// - Musical key transitions (harmonic compatibility)
        /// - Difficulty balance (avoid extreme jumps)
        /// </summary>
        /// <param name="currentSongId">The ID of the current song in the setlist</param>
        /// <param name="userId">The user ID to filter songs</param>
        /// <param name="excludeSongIds">Song IDs to exclude from recommendations (already in setlist)</param>
        /// <param name="maxResults">Maximum number of recommendations to return</param>
        /// <returns>List of recommended songs with compatibility scores</returns>
        public async Task<List<SongRecommendationDto>> GetNextSongRecommendationsAsync(
            int currentSongId, 
            string userId, 
            List<int>? excludeSongIds = null,
            int maxResults = 5)
        {
            using var context = _contextFactory.CreateDbContext();

            // Get current song
            var currentSong = await context.Songs
                .FirstOrDefaultAsync(s => s.Id == currentSongId && s.UserId == userId);

            if (currentSong == null)
                return new List<SongRecommendationDto>();

            // Get all available songs (excluding current and already-selected songs)
            var excludeIds = excludeSongIds ?? new List<int>();
            excludeIds.Add(currentSongId);

            var candidateSongs = await context.Songs
                .Where(s => s.UserId == userId && !excludeIds.Contains(s.Id))
                .ToListAsync();

            if (!candidateSongs.Any())
                return new List<SongRecommendationDto>();

            // Score each candidate song
            var scoredSongs = candidateSongs
                .Select(song => new
                {
                    Song = song,
                    Score = CalculateCompatibilityScore(currentSong, song)
                })
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => new SongRecommendationDto
                {
                    SongId = x.Song.Id,
                    Title = x.Song.Title,
                    Artist = x.Song.Artist,
                    Genre = x.Song.Genre,
                    Bpm = x.Song.Bpm ?? 0,
                    MusicalKey = x.Song.MusicalKey,
                    DurationSeconds = x.Song.DurationSeconds ?? 0,
                    DifficultyRating = x.Song.DifficultyRating ?? 0,
                    CompatibilityScore = x.Score,
                    CompatibilityDetails = GetCompatibilityDetails(currentSong, x.Song)
                })
                .ToList();

            return scoredSongs;
        }

        /// <summary>
        /// Calculates overall compatibility score between two songs (0-100).
        /// Higher score = better compatibility for segueing from current to next song.
        /// </summary>
        private decimal CalculateCompatibilityScore(Song currentSong, Song nextSong)
        {
            decimal totalScore = 0;

            // BPM compatibility (weight: 30%) - smooth tempo transition
            decimal bpmScore = CalculateBpmScore(currentSong.Bpm ?? 0, nextSong.Bpm ?? 0);
            totalScore += bpmScore * 0.30m;

            // Genre compatibility (weight: 25%) - genre similarity
            decimal genreScore = CalculateGenreScore(currentSong.Genre, nextSong.Genre);
            totalScore += genreScore * 0.25m;

            // Key compatibility (weight: 25%) - harmonic transition
            decimal keyScore = CalculateKeyScore(currentSong.MusicalKey, nextSong.MusicalKey);
            totalScore += keyScore * 0.25m;

            // Difficulty balance (weight: 20%) - avoid extreme difficulty jumps
            decimal difficultyScore = CalculateDifficultyScore(currentSong.DifficultyRating ?? 0, nextSong.DifficultyRating ?? 0);
            totalScore += difficultyScore * 0.20m;

            return Math.Round(totalScore, 1);
        }

        /// <summary>
        /// Scores BPM compatibility (0-100).
        /// Sweet spot is ±15 BPM from current song (smooth flow).
        /// Beyond ±30 BPM is jarring and scores low.
        /// </summary>
        private decimal CalculateBpmScore(int currentBpm, int nextBpm)
        {
            if (currentBpm <= 0 || nextBpm <= 0)
                return 50m; // Neutral score if BPM data missing

            var bpmDifference = Math.Abs(currentBpm - nextBpm);

            if (bpmDifference <= 15)
                return 100m; // Perfect smooth transition

            if (bpmDifference <= 30)
                return 80m - (decimal)(bpmDifference - 15) * 1.33m; // Good transition, gradually decreases

            if (bpmDifference <= 50)
                return 50m - (decimal)(bpmDifference - 30) * 1m; // Noticeable jump

            return Math.Max(0m, 20m - (decimal)(bpmDifference - 50) * 0.5m); // Very jarring
        }

        /// <summary>
        /// Scores genre compatibility (0-100).
        /// Same genre = 100. Related genres = 60-80. Different genres = 30-50.
        /// </summary>
        private decimal CalculateGenreScore(string? currentGenre, string? nextGenre)
        {
            if (string.IsNullOrWhiteSpace(currentGenre) || string.IsNullOrWhiteSpace(nextGenre))
                return 50m; // Neutral if genre missing

            if (currentGenre.Equals(nextGenre, StringComparison.OrdinalIgnoreCase))
                return 100m; // Same genre - perfect

            var relatedGenrePairs = new[]
            {
                ("Rock", "Alternative"),
                ("Jazz", "Blues"),
                ("Pop", "Funk"),
                ("Electronic", "Dance"),
                ("Soul", "R&B"),
                ("Country", "Americana"),
                ("Classical", "Chamber"),
                ("Reggae", "Ska")
            };

            var isRelated = relatedGenrePairs.Any(pair =>
                (currentGenre.Contains(pair.Item1, StringComparison.OrdinalIgnoreCase) &&
                 nextGenre.Contains(pair.Item2, StringComparison.OrdinalIgnoreCase)) ||
                (currentGenre.Contains(pair.Item2, StringComparison.OrdinalIgnoreCase) &&
                 nextGenre.Contains(pair.Item1, StringComparison.OrdinalIgnoreCase)));

            return isRelated ? 70m : 40m;
        }

        /// <summary>
        /// Scores musical key compatibility (0-100) based on harmonic theory.
        /// Adjacent keys or relative minors/majors = high score.
        /// Direct key changes = medium. Distant keys = low.
        /// </summary>
        private decimal CalculateKeyScore(string? currentKey, string? nextKey)
        {
            if (string.IsNullOrWhiteSpace(currentKey) || string.IsNullOrWhiteSpace(nextKey))
                return 50m; // Neutral if key data missing

            if (currentKey.Equals(nextKey, StringComparison.OrdinalIgnoreCase))
                return 100m; // Same key - perfect

            // Harmonic wheel - keys close together
            var harmonicCompatibility = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "C", new() { "G", "F", "Am", "Em" } },
                { "G", new() { "D", "C", "Em", "Bm" } },
                { "D", new() { "A", "G", "Bm", "F#m" } },
                { "A", new() { "E", "D", "F#m", "C#m" } },
                { "E", new() { "B", "A", "C#m", "G#m" } },
                { "B", new() { "F#", "E", "G#m", "D#m" } },
                { "F#", new() { "C#", "B", "D#m", "A#m" } },
                { "F", new() { "C", "Bb", "Dm", "Am" } },
                { "Bb", new() { "F", "Eb", "Gm", "Dm" } },
                { "Eb", new() { "Bb", "Ab", "Cm", "Gm" } },
                { "Ab", new() { "Eb", "Db", "Fm", "Cm" } },
                { "Db", new() { "Ab", "Gb", "Bbm", "Fm" } },
                { "Gb", new() { "Db", "Cb", "Ebm", "Bbm" } },
                { "Cb", new() { "Gb", "Fb", "Abm", "Ebm" } }
            };

            if (harmonicCompatibility.TryGetValue(currentKey, out var compatibleKeys))
            {
                if (compatibleKeys.Contains(nextKey, StringComparer.OrdinalIgnoreCase))
                    return 90m; // Harmonically compatible

                // Check if it's just an enharmonic equivalent (e.g., B vs Cb)
                if (IsEnharmonicEquivalent(currentKey, nextKey))
                    return 85m;
            }

            // Semitone distance for distant keys
            return 40m;
        }

        /// <summary>
        /// Scores difficulty balance (0-100).
        /// Avoids extreme difficulty jumps. Small progressions OK.
        /// </summary>
        private decimal CalculateDifficultyScore(int currentDifficulty, int nextDifficulty)
        {
            var difficultyJump = Math.Abs(currentDifficulty - nextDifficulty);

            if (difficultyJump == 0)
                return 90m; // Same difficulty - good balance

            if (difficultyJump == 1)
                return 85m; // Slight progression - acceptable

            if (difficultyJump == 2)
                return 70m; // Moderate jump - noticeable but manageable

            if (difficultyJump == 3)
                return 50m; // Large jump - risky

            return Math.Max(20m, 50m - (decimal)difficultyJump * 5m); // Extreme jump - penalize heavily
        }

        /// <summary>
        /// Checks if two keys are enharmonic equivalents (same pitch, different names).
        /// E.g., B and Cb, F# and Gb, etc.
        /// </summary>
        private bool IsEnharmonicEquivalent(string key1, string key2)
        {
            var enharmonics = new[]
            {
                new[] { "B", "Cb" },
                new[] { "E", "Fb" },
                new[] { "F#", "Gb" },
                new[] { "C#", "Db" },
                new[] { "G#", "Ab" },
                new[] { "D#", "Eb" },
                new[] { "A#", "Bb" }
            };

            return enharmonics.Any(pair =>
                (pair[0].Equals(key1, StringComparison.OrdinalIgnoreCase) &&
                 pair[1].Equals(key2, StringComparison.OrdinalIgnoreCase)) ||
                (pair[1].Equals(key1, StringComparison.OrdinalIgnoreCase) &&
                 pair[0].Equals(key2, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Builds human-readable compatibility details explaining why songs are compatible.
        /// </summary>
        private List<string> GetCompatibilityDetails(Song currentSong, Song nextSong)
        {
            var details = new List<string>();

            // BPM feedback
            var bpmDiff = Math.Abs((currentSong.Bpm ?? 0) - (nextSong.Bpm ?? 0));
            if (bpmDiff <= 15)
                details.Add($"Smooth tempo flow: {currentSong.Bpm ?? 0} → {nextSong.Bpm ?? 0} BPM");
            else if (bpmDiff <= 30)
                details.Add($"Moderate tempo change: {currentSong.Bpm ?? 0} → {nextSong.Bpm ?? 0} BPM");
            else
                details.Add($"Noticeable tempo jump: {currentSong.Bpm ?? 0} → {nextSong.Bpm ?? 0} BPM");

            // Genre feedback
            if (currentSong.Genre?.Equals(nextSong.Genre, StringComparison.OrdinalIgnoreCase) == true)
                details.Add($"Same genre: {nextSong.Genre}");
            else
                details.Add($"Genre shift: {currentSong.Genre} → {nextSong.Genre}");

            // Key feedback
            if (currentSong.MusicalKey?.Equals(nextSong.MusicalKey, StringComparison.OrdinalIgnoreCase) == true)
                details.Add($"Same key: {nextSong.MusicalKey}");
            else
                details.Add($"Key transition: {currentSong.MusicalKey} → {nextSong.MusicalKey}");

            // Difficulty feedback
            var diffDiff = (nextSong.DifficultyRating ?? 0) - (currentSong.DifficultyRating ?? 0);
            if (diffDiff == 0)
                details.Add($"Consistent difficulty: {nextSong.DifficultyRating ?? 0}/5");
            else if (diffDiff > 0)
                details.Add($"Difficulty increase: {currentSong.DifficultyRating ?? 0} → {nextSong.DifficultyRating ?? 0}/5");
            else
                details.Add($"Difficulty decrease: {currentSong.DifficultyRating ?? 0} → {nextSong.DifficultyRating ?? 0}/5");

            return details;
        }
    }

    /// <summary>
    /// DTO for recommended song with compatibility scoring and details.
    /// </summary>
    public class SongRecommendationDto
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
        /// Overall compatibility score (0-100).
        /// Higher = more compatible to follow current song.
        /// </summary>
        public decimal CompatibilityScore { get; set; }

        /// <summary>
        /// Human-readable explanation of why this song is recommended.
        /// </summary>
        public List<string> CompatibilityDetails { get; set; } = new();
    }
}
