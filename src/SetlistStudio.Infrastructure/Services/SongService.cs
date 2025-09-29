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

    public SongService(SetlistStudioDbContext context, ILogger<SongService> logger)
    {
        _context = context;
        _logger = logger;
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

            _logger.LogInformation("Created song {SongId} '{Title}' by '{Artist}' for user {UserId}", 
                song.Id, song.Title, song.Artist, song.UserId);

            return song;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating song '{Title}' by '{Artist}' for user {UserId}", 
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

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated song {SongId} for user {UserId}", song.Id, userId);

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

            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted song {SongId} '{Title}' for user {UserId}", 
                songId, song.Title, userId);

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
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(song.Title))
            errors.Add("Song title is required");
        else if (song.Title.Length > 200)
            errors.Add("Song title cannot exceed 200 characters");

        if (string.IsNullOrWhiteSpace(song.Artist))
            errors.Add("Artist name is required");
        else if (song.Artist.Length > 200)
            errors.Add("Artist name cannot exceed 200 characters");

        if (!string.IsNullOrEmpty(song.Album) && song.Album.Length > 200)
            errors.Add("Album name cannot exceed 200 characters");

        if (!string.IsNullOrEmpty(song.Genre) && song.Genre.Length > 50)
            errors.Add("Genre cannot exceed 50 characters");

        if (song.Bpm.HasValue && (song.Bpm < 40 || song.Bpm > 250))
            errors.Add("BPM must be between 40 and 250");

        if (!string.IsNullOrEmpty(song.MusicalKey) && song.MusicalKey.Length > 10)
            errors.Add("Musical key cannot exceed 10 characters");

        if (song.DurationSeconds.HasValue && (song.DurationSeconds < 1 || song.DurationSeconds > 3600))
            errors.Add("Duration must be between 1 second and 1 hour");

        if (!string.IsNullOrEmpty(song.Notes) && song.Notes.Length > 2000)
            errors.Add("Notes cannot exceed 2000 characters");

        if (!string.IsNullOrEmpty(song.Tags) && song.Tags.Length > 500)
            errors.Add("Tags cannot exceed 500 characters");

        if (song.DifficultyRating.HasValue && (song.DifficultyRating < 1 || song.DifficultyRating > 5))
            errors.Add("Difficulty rating must be between 1 and 5");

        if (string.IsNullOrWhiteSpace(song.UserId))
            errors.Add("User ID is required");

        return errors;
    }
}