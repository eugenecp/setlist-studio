using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;

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

    public SongService(SetlistStudioDbContext context, ILogger<SongService> logger, IAuditLogService auditLogService)
    {
        _context = context;
        _logger = logger;
        _auditLogService = auditLogService;
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

            _logger.LogInformation("Retrieved {Count} songs for user {UserId} (page {Page})", 
                songs.Count, userId, pageNumber);

            return (songs, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving songs for user {UserId}", userId);
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
                _logger.LogInformation("Retrieved song {SongId} for user {UserId}", songId, userId);
            }

            return song;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving song {SongId} for user {UserId}", songId, userId);
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
                throw new ArgumentException($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            song.CreatedAt = DateTime.UtcNow;
            song.UpdatedAt = null;

            _context.Songs.Add(song);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Created song {SongId} '{Title}' by '{Artist}' for user {UserId}", 
                song.Id, song.Title, song.Artist, song.UserId);

            // Log audit trail for song creation
            await _auditLogService.LogAuditAsync(
                "CREATE",
                nameof(Song),
                song.Id.ToString(),
                song.UserId,
                new { song.Title, song.Artist, song.Album, song.Genre, song.Bpm, song.MusicalKey }
            );

            return song;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating song '{Title}' by '{Artist}' for user {UserId}", 
                song.Title, song.Artist, song.UserId);
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
                _logger.LogWarning("Song {SongId} not found or unauthorized for user {UserId}", song.Id, userId);
                return null;
            }

            var validationErrors = ValidateSong(song);
            if (validationErrors.Any())
            {
                throw new ArgumentException($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            // Capture old values for audit logging
            var oldValues = new 
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

            _logger.LogInformation("Updated song {SongId} for user {UserId}", song.Id, userId);

            // Log audit trail for song update
            await _auditLogService.LogAuditAsync(
                "UPDATE",
                nameof(Song),
                song.Id.ToString(),
                userId,
                newValues
            );

            return existingSong;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating song {SongId} for user {UserId}", song.Id, userId);
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
                _logger.LogWarning("Song {SongId} not found or unauthorized for user {UserId}", songId, userId);
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

            _logger.LogInformation("Deleted song {SongId} '{Title}' for user {UserId}", 
                songId, song.Title, userId);

            // Log audit trail for song deletion
            await _auditLogService.LogAuditAsync(
                "DELETE",
                nameof(Song),
                songId.ToString(),
                userId,
                deletedValues
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting song {SongId} for user {UserId}", songId, userId);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetGenresAsync(string userId)
    {
        try
        {
            var genres = await _context.Songs
                .Where(s => s.UserId == userId && !string.IsNullOrEmpty(s.Genre))
                .Select(s => s.Genre!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return genres;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving genres for user {UserId}", userId);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tags for user {UserId}", userId);
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
    }

    private static void ValidateOptionalProperties(Song song, List<string> errors)
    {
        ValidateOptionalStringProperty(song.Album, "Album name", 200, errors);
        ValidateOptionalStringProperty(song.Genre, "Genre", 50, errors);
        ValidateOptionalStringProperty(song.MusicalKey, "Musical key", 10, errors);
        ValidateOptionalStringProperty(song.Notes, "Notes", 2000, errors);
        ValidateOptionalStringProperty(song.Tags, "Tags", 500, errors);
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
}