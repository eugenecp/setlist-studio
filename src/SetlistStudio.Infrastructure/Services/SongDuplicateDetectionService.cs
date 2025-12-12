using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Security;
using SetlistStudio.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Service for detecting duplicate songs in a user's library
/// Helps prevent users from adding the same song multiple times
/// </summary>
public interface ISongDuplicateDetectionService
{
    /// <summary>
    /// Checks if a song with the same title and artist already exists for the user
    /// Uses fuzzy matching to catch variations like different spacing, capitalization, etc.
    /// </summary>
    /// <param name="song">The song to check</param>
    /// <param name="userId">The user's ID</param>
    /// <param name="excludeSongId">Optional song ID to exclude from comparison (for updates)</param>
    /// <returns>The duplicate song if found, null otherwise</returns>
    Task<Song?> FindDuplicateAsync(Song song, string userId, int? excludeSongId = null);

    /// <summary>
    /// Gets all potential duplicates for a song (with similarity scoring)
    /// </summary>
    /// <param name="song">The song to check</param>
    /// <param name="userId">The user's ID</param>
    /// <param name="similarityThreshold">Similarity score threshold (0.0 - 1.0), default 0.8</param>
    /// <returns>List of similar songs with their similarity scores</returns>
    Task<List<(Song Song, double SimilarityScore)>> FindPotentialDuplicatesAsync(
        Song song,
        string userId,
        double similarityThreshold = 0.8);
}

public class SongDuplicateDetectionService : ISongDuplicateDetectionService
{
    private readonly SetlistStudioDbContext _context;
    private readonly ILogger<SongDuplicateDetectionService> _logger;

    public SongDuplicateDetectionService(
        SetlistStudioDbContext context,
        ILogger<SongDuplicateDetectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Song?> FindDuplicateAsync(Song song, string userId, int? excludeSongId = null)
    {
        try
        {
            var normalizedInputTitle = NormalizeString(song.Title);
            var normalizedInputArtist = NormalizeString(song.Artist);

            // Get all songs for the user
            var userSongs = await _context.Songs
                .Where(s => s.UserId == userId)
                .ToListAsync();

            // Find exact match after normalization
            var exactMatch = userSongs.FirstOrDefault(s =>
                (excludeSongId == null || s.Id != excludeSongId) &&
                NormalizeString(s.Title) == normalizedInputTitle &&
                NormalizeString(s.Artist) == normalizedInputArtist);

            if (exactMatch != null)
            {
                return exactMatch;
            }

            // If no exact match, check for fuzzy matches
            var fuzzyMatches = userSongs
                .Where(s => excludeSongId == null || s.Id != excludeSongId)
                .Select(s => new
                {
                    Song = s,
                    SimilarityScore = CalculateSimilarity(normalizedInputTitle, NormalizeString(s.Title)) +
                                     CalculateSimilarity(normalizedInputArtist, NormalizeString(s.Artist))
                })
                .Where(x => x.SimilarityScore >= 1.6) // High similarity threshold (0.8 for each field)
                .OrderByDescending(x => x.SimilarityScore)
                .FirstOrDefault();

            return fuzzyMatches?.Song;
        }
        catch (Exception ex)
        {
            var sanitizedTitle = SecureLoggingHelper.SanitizeMessage(song.Title);
            var sanitizedArtist = SecureLoggingHelper.SanitizeMessage(song.Artist);
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Error checking for duplicate song '{Title}' by '{Artist}' for user {UserId}",
                sanitizedTitle, sanitizedArtist, sanitizedUserId);
            throw;
        }
    }

    public async Task<List<(Song Song, double SimilarityScore)>> FindPotentialDuplicatesAsync(
        Song song,
        string userId,
        double similarityThreshold = 0.8)
    {
        try
        {
            var normalizedInputTitle = NormalizeString(song.Title);
            var normalizedInputArtist = NormalizeString(song.Artist);

            // Get all songs for the user
            var userSongs = await _context.Songs
                .Where(s => s.UserId == userId)
                .ToListAsync();

            var potentialDuplicates = userSongs
                .Where(s => s.Id != song.Id) // Exclude the song itself if it has an ID
                .Select(s => new
                {
                    Song = s,
                    TitleSimilarity = CalculateSimilarity(normalizedInputTitle, NormalizeString(s.Title)),
                    ArtistSimilarity = CalculateSimilarity(normalizedInputArtist, NormalizeString(s.Artist))
                })
                .Where(x => x.TitleSimilarity >= similarityThreshold && x.ArtistSimilarity >= similarityThreshold)
                .Select(x => (x.Song, SimilarityScore: (x.TitleSimilarity + x.ArtistSimilarity) / 2))
                .OrderByDescending(x => x.SimilarityScore)
                .ToList();

            return potentialDuplicates;
        }
        catch (Exception ex)
        {
            var sanitizedTitle = SecureLoggingHelper.SanitizeMessage(song.Title);
            var sanitizedArtist = SecureLoggingHelper.SanitizeMessage(song.Artist);
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Error finding potential duplicates for '{Title}' by '{Artist}' for user {UserId}",
                sanitizedTitle, sanitizedArtist, sanitizedUserId);
            throw;
        }
    }

    /// <summary>
    /// Normalizes a string for comparison (lowercase, trim whitespace, remove special chars)
    /// </summary>
    private static string NormalizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Convert to lowercase and trim
        var normalized = input.ToLowerInvariant().Trim();

        // Remove extra whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ");

        // Remove common variations like "the", "&", "-" that might differ between entries
        normalized = normalized.Replace(" & ", " and ");
        normalized = normalized.Replace("-", " ");

        return normalized;
    }

    /// <summary>
    /// Calculates Levenshtein similarity score between two strings (0.0 - 1.0)
    /// Higher score means more similar strings
    /// </summary>
    private static double CalculateSimilarity(string source, string target)
    {
        if (source == target)
            return 1.0;

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;

        // Calculate Levenshtein distance
        int distance = LevenshteinDistance(source, target);
        int maxLength = Math.Max(source.Length, target.Length);

        // Convert distance to similarity score (1.0 - normalized distance)
        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings
    /// This measures the minimum number of single-character edits needed to transform one string into another
    /// </summary>
    private static int LevenshteinDistance(string source, string target)
    {
        if (source.Length == 0)
            return target.Length;

        if (target.Length == 0)
            return source.Length;

        int[,] distances = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
            distances[i, 0] = i;

        for (int j = 0; j <= target.Length; j++)
            distances[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;

                distances[i, j] = Math.Min(
                    Math.Min(
                        distances[i - 1, j] + 1,      // Deletion
                        distances[i, j - 1] + 1),     // Insertion
                    distances[i - 1, j - 1] + cost);  // Substitution
            }
        }

        return distances[source.Length, target.Length];
    }
}
