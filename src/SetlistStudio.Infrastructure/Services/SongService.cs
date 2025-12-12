using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Security;
using SetlistStudio.Infrastructure.Data;
using System.Text;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Service implementation for managing songs in the user's library
/// Provides CRUD operations with proper authorization and validation
/// </summary>
public class SongService : ISongService
{
    private readonly SetlistStudioDbContext _context;
    private readonly ILogger<SongService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IQueryCacheService _cacheService;

    public SongService(SetlistStudioDbContext context, ILogger<SongService> logger, IAuditLogService auditLogService, IQueryCacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _auditLogService = auditLogService;
        _cacheService = cacheService;
    }

    public async Task<(IEnumerable<Song> Songs, int TotalCount)> GetSongsAsync(
        string userId,
        string? searchTerm = null,
        string? genre = null,
        string? tags = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        try
        {
            var query = _context.Songs.Where(s => s.UserId == userId);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                query = query.Where(s => 
                    s.Title.ToLower().Contains(lowerSearch) ||
                    s.Artist.ToLower().Contains(lowerSearch) ||
                    (s.Album != null && s.Album.ToLower().Contains(lowerSearch)));
            }

            // Apply genre filter
            if (!string.IsNullOrWhiteSpace(genre))
            {
                query = query.Where(s => s.Genre == genre);
            }

            // Apply tags filter
            if (!string.IsNullOrWhiteSpace(tags))
            {
                query = query.Where(s => s.Tags != null && s.Tags.Contains(tags));
            }

            var totalCount = await query.CountAsync();

            var songs = await query
                .OrderBy(s => s.Artist)
                .ThenBy(s => s.Title)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogInformation("Retrieved {Count} songs for user {UserId} (page {Page})", 
                songs.Count, sanitizedUserId, pageNumber);

            return (songs, totalCount);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Database error retrieving songs for user {UserId}", sanitizedUserId);
            throw;
        }
        catch (ArgumentException ex)
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Invalid argument retrieving songs for user {UserId}", sanitizedUserId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Invalid operation retrieving songs for user {UserId}", sanitizedUserId);
            throw;
        }
    }

    public async Task<Song?> GetSongByIdAsync(int songId, string userId)
    {
        try
        {
            var song = await _context.Songs
                .FirstOrDefaultAsync(s => s.Id == songId && s.UserId == userId);

            if (song != null)
            {
                var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
                _logger.LogInformation("Retrieved song {SongId} for user {UserId}", songId, sanitizedUserId);
            }

            return song;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Database error retrieving song {SongId} for user {UserId}", songId, sanitizedUserId);
            throw;
        }
        catch (ArgumentException ex)
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Invalid argument retrieving song {SongId} for user {UserId}", songId, sanitizedUserId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Invalid operation retrieving song {SongId} for user {UserId}", songId, sanitizedUserId);
            throw;
        }
    }

    public async Task<Song> CreateSongAsync(Song song)
    {
        try
        {
            var validationErrors = ValidateSong(song);
            if (validationErrors.Any())
            {
                var errorBuilder = new StringBuilder();
                errorBuilder.Append("Validation failed: ");
                bool first = true;
                foreach (var error in validationErrors)
                {
                    if (!first) errorBuilder.Append(", ");
                    errorBuilder.Append(error);
                    first = false;
                }
                throw new ArgumentException(errorBuilder.ToString());
            }

            song.CreatedAt = DateTime.UtcNow;
            song.UpdatedAt = null;

            _context.Songs.Add(song);
            await _context.SaveChangesAsync();

            var sanitizedTitle = SecureLoggingHelper.SanitizeMessage(song.Title);
            var sanitizedArtist = SecureLoggingHelper.SanitizeMessage(song.Artist);
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(song.UserId);
            _logger?.LogInformation("Created song {SongId} '{Title}' by '{Artist}' for user {UserId}", 
                song.Id, sanitizedTitle, sanitizedArtist, sanitizedUserId);

            // Log audit trail for song creation
            await _auditLogService.LogAuditAsync(
                "CREATE",
                nameof(Song),
                song.Id.ToString(),
                song.UserId,
                new { song.Title, song.Artist, song.Album, song.Genre, song.Bpm, song.MusicalKey }
            );

            // Invalidate cached data for this user
            await _cacheService.InvalidateUserCacheAsync(song.UserId);

            return song;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var sanitizedTitle = SecureLoggingHelper.SanitizeMessage(song.Title);
            var sanitizedArtist = SecureLoggingHelper.SanitizeMessage(song.Artist);
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(song.UserId);
            _logger?.LogError(ex, "Database error creating song '{Title}' by '{Artist}' for user {UserId}", 
                sanitizedTitle, sanitizedArtist, sanitizedUserId);
            throw;
        }
        catch (ArgumentException ex)
        {
            var sanitizedTitle = SecureLoggingHelper.SanitizeMessage(song.Title);
            var sanitizedArtist = SecureLoggingHelper.SanitizeMessage(song.Artist);
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(song.UserId);
            _logger?.LogError(ex, "Invalid argument creating song '{Title}' by '{Artist}' for user {UserId}", 
                sanitizedTitle, sanitizedArtist, sanitizedUserId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            var sanitizedTitle = SecureLoggingHelper.SanitizeMessage(song.Title);
            var sanitizedArtist = SecureLoggingHelper.SanitizeMessage(song.Artist);
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(song.UserId);
            _logger?.LogError(ex, "Invalid operation creating song '{Title}' by '{Artist}' for user {UserId}", 
                sanitizedTitle, sanitizedArtist, sanitizedUserId);
            throw;
        }
    }

    public async Task<Song?> UpdateSongAsync(Song song, string userId)
    {
        try
        {
            var existingSong = await _context.Songs
                .FirstOrDefaultAsync(s => s.Id == song.Id && s.UserId == userId);

            if (existingSong == null)
            {
                var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
                _logger.LogWarning("Song {SongId} not found or unauthorized for user {UserId}", song.Id, sanitizedUserId);
                return null;
            }

            var validationErrors = ValidateSong(song);
            if (validationErrors.Any())
            {
                var errorBuilder = new StringBuilder();
                errorBuilder.Append("Validation failed: ");
                bool first = true;
                foreach (var error in validationErrors)
                {
                    if (!first) errorBuilder.Append(", ");
                    errorBuilder.Append(error);
                    first = false;
                }
                throw new ArgumentException(errorBuilder.ToString());
            }

            // Update properties
            existingSong.Title = song.Title;
            existingSong.Artist = song.Artist;
            existingSong.Album = song.Album;
            existingSong.Genre = song.Genre;
            existingSong.Bpm = song.Bpm;
            existingSong.MusicalKey = song.MusicalKey;
            existingSong.DurationSeconds = song.DurationSeconds;
            existingSong.Notes = song.Notes;
            existingSong.Tags = song.Tags;
            existingSong.DifficultyRating = song.DifficultyRating;
            existingSong.UpdatedAt = DateTime.UtcNow;

            // Capture new values for audit logging
            var newValues = new 
            { 
                existingSong.Title, 
                existingSong.Artist, 
                existingSong.Album, 
                existingSong.Genre, 
                existingSong.Bpm, 
                existingSong.MusicalKey,
                existingSong.DurationSeconds,
                existingSong.Notes,
                existingSong.Tags,
                existingSong.DifficultyRating
            };

            await _context.SaveChangesAsync();

            var sanitizedUserIdForLog = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogInformation("Updated song {SongId} for user {UserId}", song.Id, sanitizedUserIdForLog);

            // Log audit trail for song update
            await _auditLogService.LogAuditAsync(
                "UPDATE",
                nameof(Song),
                song.Id.ToString(),
                userId,
                newValues
            );

            // Invalidate cached data for this user
            await _cacheService.InvalidateUserCacheAsync(userId);

            return existingSong;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            var sanitizedUserIdForLog2 = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Concurrency error updating song {SongId} for user {UserId}", song.Id, sanitizedUserIdForLog2);
            throw;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var sanitizedUserIdForLog2 = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Database error updating song {SongId} for user {UserId}", song.Id, sanitizedUserIdForLog2);
            throw;
        }
        catch (ArgumentException ex)
        {
            var sanitizedUserIdForLog2 = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Invalid argument updating song {SongId} for user {UserId}", song.Id, sanitizedUserIdForLog2);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            var sanitizedUserIdForLog2 = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Invalid operation updating song {SongId} for user {UserId}", song.Id, sanitizedUserIdForLog2);
            throw;
        }
    }

    public async Task<bool> DeleteSongAsync(int songId, string userId)
    {
        try
        {
            var song = await _context.Songs
                .FirstOrDefaultAsync(s => s.Id == songId && s.UserId == userId);

            if (song == null)
            {
                var sanitizedUserIdForDeleteLog = SecureLoggingHelper.SanitizeUserId(userId);
                _logger.LogWarning("Song {SongId} not found or unauthorized for user {UserId}", songId, sanitizedUserIdForDeleteLog);
                return false;
            }

            // Capture values for audit logging before deletion
            var deletedValues = new 
            { 
                song.Title, 
                song.Artist, 
                song.Album, 
                song.Genre, 
                song.Bpm, 
                song.MusicalKey,
                song.DurationSeconds,
                song.Notes,
                song.Tags,
                song.DifficultyRating,
                song.CreatedAt
            };

            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();

            var sanitizedUserIdForDeleteLog2 = SecureLoggingHelper.SanitizeUserId(userId);
            var sanitizedTitle = SecureLoggingHelper.SanitizeMessage(song.Title);
            _logger.LogInformation("Deleted song {SongId} '{Title}' for user {UserId}", 
                songId, sanitizedTitle, sanitizedUserIdForDeleteLog2);

            // Log audit trail for song deletion
            await _auditLogService.LogAuditAsync(
                "DELETE",
                nameof(Song),
                songId.ToString(),
                userId,
                deletedValues
            );

            // Invalidate cached data for this user
            await _cacheService.InvalidateUserCacheAsync(userId);

            return true;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            var sanitizedUserIdForDeleteLog3 = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Concurrency error deleting song {SongId} for user {UserId}", songId, sanitizedUserIdForDeleteLog3);
            throw;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var sanitizedUserIdForDeleteLog3 = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Database error deleting song {SongId} for user {UserId}", songId, sanitizedUserIdForDeleteLog3);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            var sanitizedUserIdForDeleteLog3 = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Invalid operation deleting song {SongId} for user {UserId}", songId, sanitizedUserIdForDeleteLog3);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetGenresAsync(string userId)
    {
        try
        {
            return await _cacheService.GetGenresAsync(userId, async () =>
            {
                var genres = await _context.Songs
                    .Where(s => s.UserId == userId && !string.IsNullOrEmpty(s.Genre))
                    .Select(s => s.Genre!)
                    .Distinct()
                    .OrderBy(g => g)
                    .ToListAsync();

                return genres;
            });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var sanitizedUserIdForGenreLog = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Database error retrieving genres for user {UserId}", sanitizedUserIdForGenreLog);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            var sanitizedUserIdForGenreLog = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Invalid operation retrieving genres for user {UserId}", sanitizedUserIdForGenreLog);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetArtistsAsync(string userId)
    {
        try
        {
            return await _cacheService.GetArtistsAsync(userId, async () =>
            {
                var artists = await _context.Songs
                    .Where(s => s.UserId == userId && !string.IsNullOrEmpty(s.Artist))
                    .Select(s => s.Artist)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToListAsync();

                return artists;
            });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var sanitizedUserIdForArtistLog = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Database error retrieving artists for user {UserId}", sanitizedUserIdForArtistLog);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            var sanitizedUserIdForArtistLog = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Invalid operation retrieving artists for user {UserId}", sanitizedUserIdForArtistLog);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetTagsAsync(string userId)
    {
        try
        {
            var allTags = await _context.Songs
                .Where(s => s.UserId == userId && !string.IsNullOrEmpty(s.Tags))
                .Select(s => s.Tags!)
                .ToListAsync();

            // Split comma-separated tags and flatten
            var tags = allTags
                .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return tags;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var sanitizedUserIdForTagLog = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Database error retrieving tags for user {UserId}", sanitizedUserIdForTagLog);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            var sanitizedUserIdForTagLog = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogError(ex, "Invalid operation retrieving tags for user {UserId}", sanitizedUserIdForTagLog);
            throw;
        }
    }

    public IEnumerable<string> ValidateSong(Song song)
    {
        if (song == null)
        {
            return new[] { "Song cannot be null" };
        }

        var errors = new List<string>();
        
        ValidateBasicProperties(song, errors);
        ValidateOptionalProperties(song, errors);
        ValidateNumericProperties(song, errors);
        ValidateUserAssociation(song, errors);

        return errors;
    }

    private static void ValidateBasicProperties(Song song, List<string> errors)
    {
        ValidateRequiredStringProperty(song.Title, "Song title", 200, errors);
        ValidateRequiredStringProperty(song.Artist, "Artist name", 200, errors);
        
        // Security validation for malicious input
        ValidateMaliciousInput(song.Title, "Song title", errors);
        ValidateMaliciousInput(song.Artist, "Artist name", errors);
    }

    private static void ValidateOptionalProperties(Song song, List<string> errors)
    {
        ValidateOptionalStringProperty(song.Album, "Album name", 200, errors);
        ValidateOptionalStringProperty(song.Genre, "Genre", 50, errors);
        ValidateOptionalStringProperty(song.MusicalKey, "Musical key", 10, errors);
        ValidateOptionalStringProperty(song.Notes, "Notes", 2000, errors);
        ValidateOptionalStringProperty(song.Tags, "Tags", 500, errors);
        
        // Security validation for malicious input in optional fields
        if (!string.IsNullOrEmpty(song.Album))
            ValidatePathTraversal(song.Album, "Album name", errors);
        
        if (!string.IsNullOrEmpty(song.Notes))
            ValidateMaliciousInput(song.Notes, "Notes", errors);
    }

    private static void ValidateNumericProperties(Song song, List<string> errors)
    {
        ValidateOptionalNumericRange(song.Bpm, "BPM", 40, 250, errors);
        ValidateOptionalNumericRange(song.DurationSeconds, "Duration", 1, 3600, "Duration must be between 1 second and 1 hour", errors);
        ValidateOptionalNumericRange(song.DifficultyRating, "Difficulty rating", 1, 5, errors);
    }

    private static void ValidateUserAssociation(Song song, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(song.UserId))
        {
            errors.Add("User ID is required");
        }
    }

    private static void ValidateRequiredStringProperty(string? value, string propertyName, int maxLength, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{propertyName} is required");
        }
        else if (value.Length > maxLength)
        {
            errors.Add($"{propertyName} cannot exceed {maxLength} characters");
        }
    }

    private static void ValidateOptionalStringProperty(string? value, string propertyName, int maxLength, List<string> errors)
    {
        if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
        {
            errors.Add($"{propertyName} cannot exceed {maxLength} characters");
        }
    }

    private static void ValidateOptionalNumericRange(int? value, string propertyName, int min, int max, List<string> errors)
    {
        if (value.HasValue && (value < min || value > max))
        {
            errors.Add($"{propertyName} must be between {min} and {max}");
        }
    }

    private static void ValidateOptionalNumericRange(int? value, string propertyName, int min, int max, string customMessage, List<string> errors)
    {
        if (value.HasValue && (value < min || value > max))
        {
            errors.Add(customMessage);
        }
    }

    /// <summary>
    /// Validates input for malicious patterns including XSS, SQL injection, and code injection
    /// </summary>
    private static void ValidateMaliciousInput(string? value, string propertyName, List<string> errors)
    {
        if (string.IsNullOrEmpty(value))
            return;

        // XSS patterns
        var xssPatterns = new[]
        {
            "<script",
            "</script>",
            "<img",
            "onerror=",
            "javascript:",
            "<iframe",
            "<svg",
            "onload=",
            "<embed",
            "<object"
        };

        // SQL injection patterns
        var sqlPatterns = new[]
        {
            "'; DROP",
            "' OR '1'='1",
            "'; DELETE",
            "'; UPDATE",
            "EXEC(",
            "EXECUTE(",
            "xp_cmdshell"
        };

        // Code injection patterns
        var codeInjectionPatterns = new[]
        {
            "${jndi:",
            "${jndi:ldap",
            "#{",
            "{{",
            "${{"
        };

        var lowerValue = value.ToLower();

        // Check XSS patterns
        foreach (var pattern in xssPatterns)
        {
            if (lowerValue.Contains(pattern.ToLower()))
            {
                errors.Add($"{propertyName} contains potentially malicious content (XSS attempt detected)");
                return;
            }
        }

        // Check SQL injection patterns
        foreach (var pattern in sqlPatterns)
        {
            if (lowerValue.Contains(pattern.ToLower()))
            {
                errors.Add($"{propertyName} contains potentially malicious content (SQL injection attempt detected)");
                return;
            }
        }

        // Check code injection patterns
        foreach (var pattern in codeInjectionPatterns)
        {
            if (value.Contains(pattern))
            {
                errors.Add($"{propertyName} contains potentially malicious content (Code injection attempt detected)");
                return;
            }
        }

        // Check for null byte injection
        if (value.Contains('\0'))
        {
            errors.Add($"{propertyName} contains null byte characters");
            return;
        }

        // Check for suspicious Unicode characters (homoglyph attacks)
        if (ContainsSuspiciousUnicode(value))
        {
            errors.Add($"{propertyName} contains suspicious Unicode characters");
            return;
        }
    }

    /// <summary>
    /// Validates input for path traversal attacks
    /// </summary>
    private static void ValidatePathTraversal(string? value, string propertyName, List<string> errors)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var pathTraversalPatterns = new[]
        {
            "../",
            "..\\",
            "%2e%2e%2f",
            "%2e%2e/",
            "..%2f",
            "%2e%2e\\",
            "....//",
            "....\\\\",
            "....//"
        };

        var lowerValue = value.ToLower();

        foreach (var pattern in pathTraversalPatterns)
        {
            if (lowerValue.Contains(pattern.ToLower()))
            {
                errors.Add($"{propertyName} contains path traversal patterns");
                return;
            }
        }
    }

    /// <summary>
    /// Checks for suspicious Unicode characters that might be used in homoglyph attacks
    /// </summary>
    private static bool ContainsSuspiciousUnicode(string value)
    {
        // Check for Cyrillic characters that look like Latin (homoglyph attacks)
        foreach (char c in value)
        {
            // Cyrillic range that looks like Latin
            if (c >= '\u0400' && c <= '\u04FF')
            {
                // Check if it's a Cyrillic character that looks like Latin
                if (c == '\u0430' || c == '\u0435' || c == '\u043E' || c == '\u0440' || 
                    c == '\u0441' || c == '\u0445' || c == '\u0410' || c == '\u0412' ||
                    c == '\u0415' || c == '\u041A' || c == '\u041C' || c == '\u041D' ||
                    c == '\u041E' || c == '\u0420' || c == '\u0421' || c == '\u0422' ||
                    c == '\u0425')
                {
                    return true;
                }
            }
        }
        return false;
    }
}