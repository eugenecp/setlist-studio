using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Infrastructure.Data;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Service implementation for managing setlists and their songs
/// Provides CRUD operations with proper authorization and validation
/// </summary>
public class SetlistService : ISetlistService
{
    private readonly SetlistStudioDbContext _context;
    private readonly ILogger<SetlistService> _logger;

    public SetlistService(SetlistStudioDbContext context, ILogger<SetlistService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(IEnumerable<Setlist> Setlists, int TotalCount)> GetSetlistsAsync(
        string userId,
        string? searchTerm = null,
        bool? isTemplate = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        try
        {
            var query = _context.Setlists
                .Include(sl => sl.SetlistSongs)
                .ThenInclude(ss => ss.Song)
                .Where(sl => sl.UserId == userId);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                query = query.Where(sl => 
                    sl.Name.ToLower().Contains(lowerSearch) ||
                    (sl.Description != null && sl.Description.ToLower().Contains(lowerSearch)) ||
                    (sl.Venue != null && sl.Venue.ToLower().Contains(lowerSearch)));
            }

            // Apply filters
            if (isTemplate.HasValue)
            {
                query = query.Where(sl => sl.IsTemplate == isTemplate.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(sl => sl.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            var setlists = await query
                .OrderByDescending(sl => sl.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} setlists for user {UserId} (page {Page})", 
                setlists.Count, userId, pageNumber);

            return (setlists, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving setlists for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Setlist?> GetSetlistByIdAsync(int setlistId, string userId)
    {
        try
        {
            var setlist = await _context.Setlists
                .Include(sl => sl.SetlistSongs.OrderBy(ss => ss.Position))
                .ThenInclude(ss => ss.Song)
                .FirstOrDefaultAsync(sl => sl.Id == setlistId && sl.UserId == userId);

            if (setlist != null)
            {
                _logger.LogInformation("Retrieved setlist {SetlistId} for user {UserId}", setlistId, userId);
            }

            return setlist;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving setlist {SetlistId} for user {UserId}", setlistId, userId);
            throw;
        }
    }

    public async Task<Setlist> CreateSetlistAsync(Setlist setlist)
    {
        try
        {
            var validationErrors = ValidateSetlist(setlist);
            if (validationErrors.Any())
            {
                throw new ArgumentException($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            setlist.CreatedAt = DateTime.UtcNow;
            setlist.UpdatedAt = null;

            _context.Setlists.Add(setlist);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created setlist {SetlistId} '{Name}' for user {UserId}", 
                setlist.Id, setlist.Name, setlist.UserId);

            return setlist;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating setlist '{Name}' for user {UserId}", 
                setlist.Name, setlist.UserId);
            throw;
        }
    }

    public async Task<Setlist?> UpdateSetlistAsync(Setlist setlist, string userId)
    {
        try
        {
            var existingSetlist = await _context.Setlists
                .FirstOrDefaultAsync(sl => sl.Id == setlist.Id && sl.UserId == userId);

            if (existingSetlist == null)
            {
                _logger.LogWarning("Setlist {SetlistId} not found or unauthorized for user {UserId}", 
                    setlist.Id, userId);
                return null;
            }

            var validationErrors = ValidateSetlist(setlist);
            if (validationErrors.Any())
            {
                throw new ArgumentException($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            // Update properties
            existingSetlist.Name = setlist.Name;
            existingSetlist.Description = setlist.Description;
            existingSetlist.Venue = setlist.Venue;
            existingSetlist.PerformanceDate = setlist.PerformanceDate;
            existingSetlist.ExpectedDurationMinutes = setlist.ExpectedDurationMinutes;
            existingSetlist.IsTemplate = setlist.IsTemplate;
            existingSetlist.IsActive = setlist.IsActive;
            existingSetlist.PerformanceNotes = setlist.PerformanceNotes;
            existingSetlist.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated setlist {SetlistId} for user {UserId}", setlist.Id, userId);

            return existingSetlist;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating setlist {SetlistId} for user {UserId}", setlist.Id, userId);
            throw;
        }
    }

    public async Task<bool> DeleteSetlistAsync(int setlistId, string userId)
    {
        try
        {
            var setlist = await _context.Setlists
                .FirstOrDefaultAsync(sl => sl.Id == setlistId && sl.UserId == userId);

            if (setlist == null)
            {
                _logger.LogWarning("Setlist {SetlistId} not found or unauthorized for user {UserId}", 
                    setlistId, userId);
                return false;
            }

            _context.Setlists.Remove(setlist);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted setlist {SetlistId} '{Name}' for user {UserId}", 
                setlistId, setlist.Name, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting setlist {SetlistId} for user {UserId}", setlistId, userId);
            throw;
        }
    }

    public async Task<SetlistSong?> AddSongToSetlistAsync(int setlistId, int songId, string userId, int? position = null)
    {
        try
        {
            // Verify setlist belongs to user
            var setlist = await _context.Setlists
                .Include(sl => sl.SetlistSongs)
                .FirstOrDefaultAsync(sl => sl.Id == setlistId && sl.UserId == userId);

            if (setlist == null)
            {
                _logger.LogWarning("Setlist {SetlistId} not found or unauthorized for user {UserId}", 
                    setlistId, userId);
                return null;
            }

            // Verify song belongs to user
            var song = await _context.Songs
                .FirstOrDefaultAsync(s => s.Id == songId && s.UserId == userId);

            if (song == null)
            {
                _logger.LogWarning("Song {SongId} not found or unauthorized for user {UserId}", 
                    songId, userId);
                return null;
            }

            // Check if song is already in setlist
            var existingSetlistSong = await _context.SetlistSongs
                .FirstOrDefaultAsync(ss => ss.SetlistId == setlistId && ss.SongId == songId);

            if (existingSetlistSong != null)
            {
                _logger.LogWarning("Song {SongId} already exists in setlist {SetlistId}", songId, setlistId);
                return existingSetlistSong;
            }

            // Determine position
            var targetPosition = position ?? (setlist.SetlistSongs.Any() ? setlist.SetlistSongs.Max(ss => ss.Position) + 1 : 1);

            // Adjust positions if inserting in middle
            if (position.HasValue)
            {
                var songsToShift = await _context.SetlistSongs
                    .Where(ss => ss.SetlistId == setlistId && ss.Position >= position.Value)
                    .ToListAsync();

                foreach (var songToShift in songsToShift)
                {
                    songToShift.Position++;
                }
            }

            var setlistSong = new SetlistSong
            {
                SetlistId = setlistId,
                SongId = songId,
                Position = targetPosition,
                CreatedAt = DateTime.UtcNow
            };

            _context.SetlistSongs.Add(setlistSong);
            await _context.SaveChangesAsync();

            // Load the complete entity with navigation properties
            var result = await _context.SetlistSongs
                .Include(ss => ss.Song)
                .Include(ss => ss.Setlist)
                .FirstAsync(ss => ss.Id == setlistSong.Id);

            _logger.LogInformation("Added song {SongId} to setlist {SetlistId} at position {Position} for user {UserId}", 
                songId, setlistId, targetPosition, userId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding song {SongId} to setlist {SetlistId} for user {UserId}", 
                songId, setlistId, userId);
            throw;
        }
    }

    public async Task<bool> RemoveSongFromSetlistAsync(int setlistId, int songId, string userId)
    {
        try
        {
            var setlistSong = await _context.SetlistSongs
                .Include(ss => ss.Setlist)
                .FirstOrDefaultAsync(ss => ss.SetlistId == setlistId && 
                                         ss.SongId == songId && 
                                         ss.Setlist.UserId == userId);

            if (setlistSong == null)
            {
                _logger.LogWarning("SetlistSong not found for setlist {SetlistId}, song {SongId}, user {UserId}", 
                    setlistId, songId, userId);
                return false;
            }

            var removedPosition = setlistSong.Position;
            _context.SetlistSongs.Remove(setlistSong);

            // Adjust positions of remaining songs
            var songsToShift = await _context.SetlistSongs
                .Where(ss => ss.SetlistId == setlistId && ss.Position > removedPosition)
                .ToListAsync();

            foreach (var songToShift in songsToShift)
            {
                songToShift.Position--;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed song {SongId} from setlist {SetlistId} for user {UserId}", 
                songId, setlistId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing song {SongId} from setlist {SetlistId} for user {UserId}", 
                songId, setlistId, userId);
            throw;
        }
    }

    public async Task<bool> ReorderSetlistSongsAsync(int setlistId, int[] songOrdering, string userId)
    {
        try
        {
            var setlist = await _context.Setlists
                .Include(sl => sl.SetlistSongs)
                .FirstOrDefaultAsync(sl => sl.Id == setlistId && sl.UserId == userId);

            if (setlist == null)
            {
                _logger.LogWarning("Setlist {SetlistId} not found or unauthorized for user {UserId}", 
                    setlistId, userId);
                return false;
            }

            // Validate that all songs in ordering exist in setlist
            var setlistSongIds = setlist.SetlistSongs.Select(ss => ss.SongId).ToHashSet();
            if (!songOrdering.All(id => setlistSongIds.Contains(id)))
            {
                _logger.LogWarning("Invalid song ordering for setlist {SetlistId}: some songs not in setlist", 
                    setlistId);
                return false;
            }

            // Update positions
            for (int i = 0; i < songOrdering.Length; i++)
            {
                var setlistSong = setlist.SetlistSongs.First(ss => ss.SongId == songOrdering[i]);
                setlistSong.Position = i + 1;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Reordered songs in setlist {SetlistId} for user {UserId}", setlistId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering setlist {SetlistId} for user {UserId}", setlistId, userId);
            throw;
        }
    }

    public async Task<SetlistSong?> UpdateSetlistSongAsync(
        int setlistSongId,
        string userId,
        string? performanceNotes = null,
        string? transitionNotes = null,
        int? customBpm = null,
        string? customKey = null,
        bool? isEncore = null,
        bool? isOptional = null)
    {
        try
        {
            var setlistSong = await _context.SetlistSongs
                .Include(ss => ss.Setlist)
                .Include(ss => ss.Song)
                .FirstOrDefaultAsync(ss => ss.Id == setlistSongId && ss.Setlist.UserId == userId);

            if (setlistSong == null)
            {
                _logger.LogWarning("SetlistSong {SetlistSongId} not found or unauthorized for user {UserId}", 
                    setlistSongId, userId);
                return null;
            }

            // Update provided properties
            if (performanceNotes != null)
                setlistSong.PerformanceNotes = performanceNotes;
            if (transitionNotes != null)
                setlistSong.TransitionNotes = transitionNotes;
            if (customBpm.HasValue)
                setlistSong.CustomBpm = customBpm.Value;
            if (customKey != null)
                setlistSong.CustomKey = customKey;
            if (isEncore.HasValue)
                setlistSong.IsEncore = isEncore.Value;
            if (isOptional.HasValue)
                setlistSong.IsOptional = isOptional.Value;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated setlist song {SetlistSongId} for user {UserId}", 
                setlistSongId, userId);

            return setlistSong;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating setlist song {SetlistSongId} for user {UserId}", 
                setlistSongId, userId);
            throw;
        }
    }

    public async Task<Setlist?> CopySetlistAsync(int sourceSetlistId, string newName, string userId)
    {
        try
        {
            var sourceSetlist = await _context.Setlists
                .Include(sl => sl.SetlistSongs)
                .ThenInclude(ss => ss.Song)
                .FirstOrDefaultAsync(sl => sl.Id == sourceSetlistId && sl.UserId == userId);

            if (sourceSetlist == null)
            {
                _logger.LogWarning("Source setlist {SetlistId} not found or unauthorized for user {UserId}", 
                    sourceSetlistId, userId);
                return null;
            }

            var newSetlist = new Setlist
            {
                Name = newName,
                Description = sourceSetlist.Description,
                Venue = null, // Don't copy venue as it's performance-specific
                PerformanceDate = null, // Don't copy performance date
                ExpectedDurationMinutes = sourceSetlist.ExpectedDurationMinutes,
                IsTemplate = false, // New copies are not templates by default
                IsActive = false, // New copies are not active by default
                PerformanceNotes = sourceSetlist.PerformanceNotes,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Setlists.Add(newSetlist);
            await _context.SaveChangesAsync();

            // Copy songs
            foreach (var sourceSong in sourceSetlist.SetlistSongs.OrderBy(ss => ss.Position))
            {
                var newSetlistSong = new SetlistSong
                {
                    SetlistId = newSetlist.Id,
                    SongId = sourceSong.SongId,
                    Position = sourceSong.Position,
                    TransitionNotes = sourceSong.TransitionNotes,
                    PerformanceNotes = sourceSong.PerformanceNotes,
                    IsEncore = sourceSong.IsEncore,
                    IsOptional = sourceSong.IsOptional,
                    CustomBpm = sourceSong.CustomBpm,
                    CustomKey = sourceSong.CustomKey,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SetlistSongs.Add(newSetlistSong);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Copied setlist {SourceId} to new setlist {NewId} '{NewName}' for user {UserId}", 
                sourceSetlistId, newSetlist.Id, newName, userId);

            return newSetlist;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying setlist {SetlistId} for user {UserId}", 
                sourceSetlistId, userId);
            throw;
        }
    }

    public IEnumerable<string> ValidateSetlist(Setlist setlist)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(setlist.Name))
            errors.Add("Setlist name is required");
        else if (setlist.Name.Length > 200)
            errors.Add("Setlist name cannot exceed 200 characters");

        if (!string.IsNullOrEmpty(setlist.Description) && setlist.Description.Length > 1000)
            errors.Add("Description cannot exceed 1000 characters");

        if (!string.IsNullOrEmpty(setlist.Venue) && setlist.Venue.Length > 200)
            errors.Add("Venue cannot exceed 200 characters");

        if (setlist.ExpectedDurationMinutes.HasValue && setlist.ExpectedDurationMinutes < 1)
            errors.Add("Expected duration must be at least 1 minute");

        if (!string.IsNullOrEmpty(setlist.PerformanceNotes) && setlist.PerformanceNotes.Length > 2000)
            errors.Add("Performance notes cannot exceed 2000 characters");

        if (string.IsNullOrWhiteSpace(setlist.UserId))
            errors.Add("User ID is required");

        return errors;
    }
}