using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Security;
using SetlistStudio.Infrastructure.Data;
using System.Text;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Service implementation for managing setlists and their songs
/// Provides CRUD operations with proper authorization and validation
/// </summary>
public class SetlistService : ISetlistService
{
    private readonly SetlistStudioDbContext _context;
    private readonly ILogger<SetlistService> _logger;
    private readonly IQueryCacheService _cacheService;

    public SetlistService(SetlistStudioDbContext context, ILogger<SetlistService> logger, IQueryCacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
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
                query = query.Where(sl => sl.IsTemplate == isTemplate!.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(sl => sl.IsActive == isActive!.Value);
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
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while retrieving setlists for user {UserId}", userId);
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument while retrieving setlists for user {UserId}", userId);
            throw;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while retrieving setlists for user {UserId}", userId);
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
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while retrieving setlist {SetlistId} for user {UserId}", setlistId, userId);
            throw;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while retrieving setlist {SetlistId} for user {UserId}", setlistId, userId);
            throw;
        }
    }

    public async Task<Setlist> CreateSetlistAsync(Setlist setlist)
    {
        if (setlist == null)
            throw new ArgumentNullException(nameof(setlist));

        try
        {
            var validationErrors = ValidateSetlist(setlist);
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

            setlist.CreatedAt = DateTime.UtcNow;
            setlist.UpdatedAt = DateTime.UtcNow;

            _context.Setlists.Add(setlist);
            await _context.SaveChangesAsync();

            var sanitizedName = SecureLoggingHelper.SanitizeMessage(setlist.Name);
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(setlist.UserId);
            _logger.LogInformation("Created setlist {SetlistId} '{Name}' for user {UserId}", 
                setlist.Id, sanitizedName, sanitizedUserId);

            return setlist;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var sanitizedName = SecureLoggingHelper.SanitizeMessage(setlist.Name);
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(setlist.UserId);
            _logger.LogError(ex, "Database error creating setlist '{Name}' for user {UserId}", 
                sanitizedName, sanitizedUserId);
            throw;
        }
        catch (ArgumentException ex)
        {
            var sanitizedName = SecureLoggingHelper.SanitizeMessage(setlist.Name);
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(setlist.UserId);
            _logger.LogError(ex, "Invalid argument creating setlist '{Name}' for user {UserId}", 
                sanitizedName, sanitizedUserId);
            throw;
        }
    }

    public async Task<Setlist?> UpdateSetlistAsync(Setlist setlist, string userId)
    {
        if (setlist == null)
            throw new ArgumentNullException(nameof(setlist));

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
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating setlist {SetlistId} for user {UserId}", setlist.Id, userId);
            throw;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating setlist {SetlistId} for user {UserId}", setlist.Id, userId);
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

            var sanitizedName = SecureLoggingHelper.SanitizeMessage(setlist.Name);
            var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);
            _logger.LogInformation("Deleted setlist {SetlistId} '{Name}' for user {UserId}", 
                setlistId, sanitizedName, sanitizedUserId);

            return true;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error deleting setlist {SetlistId} for user {UserId}", setlistId, userId);
            throw;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting setlist {SetlistId} for user {UserId}", setlistId, userId);
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "Database error deleting setlist {SetlistId} for user {UserId}", setlistId, userId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Database error deleting setlist {SetlistId} for user {UserId}", setlistId, userId);
            throw;
        }
    }

    public async Task<SetlistSong?> AddSongToSetlistAsync(int setlistId, int songId, string userId, int? position = null)
    {
        try
        {
            var validationResult = await ValidateAddSongRequestAsync(setlistId, songId, userId);
            if (!validationResult.IsValid)
            {
                return null;
            }

            var targetPosition = await DetermineTargetPositionAsync(setlistId, position);
            await AdjustPositionsForInsertionAsync(setlistId, position);

            var setlistSong = await CreateAndSaveSetlistSongAsync(setlistId, songId, targetPosition);
            var result = await LoadSetlistSongWithNavigationPropertiesAsync(setlistSong.Id);

            _logger.LogInformation("Added song {SongId} to setlist {SetlistId} at position {Position} for user {UserId}", 
                songId, setlistId, targetPosition, userId);

            return result;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error adding song {SongId} to setlist {SetlistId} for user {UserId}", 
                songId, setlistId, userId);
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument adding song {SongId} to setlist {SetlistId} for user {UserId}", 
                songId, setlistId, userId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation adding song {SongId} to setlist {SetlistId} for user {UserId}", 
                songId, setlistId, userId);
            throw;
        }
    }

    private async Task<(bool IsValid, Setlist? Setlist, Song? Song)> ValidateAddSongRequestAsync(int setlistId, int songId, string userId)
    {
        // Verify setlist belongs to user
        var setlist = await _context.Setlists
            .Include(sl => sl.SetlistSongs)
            .FirstOrDefaultAsync(sl => sl.Id == setlistId && sl.UserId == userId);

        if (setlist == null)
        {
            _logger.LogWarning("Setlist {SetlistId} not found or unauthorized for user {UserId}", 
                setlistId, userId);
            return (false, null, null);
        }

        // Verify song belongs to user
        var song = await _context.Songs
            .FirstOrDefaultAsync(s => s.Id == songId && s.UserId == userId);

        if (song == null)
        {
            _logger.LogWarning("Song {SongId} not found or unauthorized for user {UserId}", 
                songId, userId);
            return (false, setlist, null);
        }

        // Check if song is already in setlist
        var existingSetlistSong = await _context.SetlistSongs
            .FirstOrDefaultAsync(ss => ss.SetlistId == setlistId && ss.SongId == songId);

        if (existingSetlistSong != null)
        {
            _logger.LogWarning("Song {SongId} already exists in setlist {SetlistId}", songId, setlistId);
            return (false, setlist, song);
        }

        return (true, setlist, song);
    }

    private async Task<int> DetermineTargetPositionAsync(int setlistId, int? position)
    {
        if (!position.HasValue)
        {
            var currentSetlistSongs = await _context.SetlistSongs
                .Where(ss => ss.SetlistId == setlistId)
                .ToListAsync();
            return currentSetlistSongs.Any() ? currentSetlistSongs.Max(ss => ss.Position) + 1 : 1;
        }

        return position.Value;
    }

    private async Task AdjustPositionsForInsertionAsync(int setlistId, int? position)
    {
        if (!position.HasValue) return;

        var songsToShift = await _context.SetlistSongs
            .Where(ss => ss.SetlistId == setlistId && ss.Position >= position!.Value)
            .ToListAsync();

        foreach (var songToShift in songsToShift)
        {
            songToShift.Position++;
        }
        
        if (songsToShift.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    private async Task<SetlistSong> CreateAndSaveSetlistSongAsync(int setlistId, int songId, int targetPosition)
    {
        var setlistSong = new SetlistSong
        {
            SetlistId = setlistId,
            SongId = songId,
            Position = targetPosition,
            CreatedAt = DateTime.UtcNow
        };

        _context.SetlistSongs.Add(setlistSong);
        await _context.SaveChangesAsync();

        return setlistSong;
    }

    private async Task<SetlistSong> LoadSetlistSongWithNavigationPropertiesAsync(int setlistSongId)
    {
        return await _context.SetlistSongs
            .Include(ss => ss.Song)
            .Include(ss => ss.Setlist)
            .FirstAsync(ss => ss.Id == setlistSongId);
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
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error removing song {SongId} from setlist {SetlistId} for user {UserId}", 
                songId, setlistId, userId);
            throw;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error removing song {SongId} from setlist {SetlistId} for user {UserId}", 
                songId, setlistId, userId);
            throw;
        }
    }

    public async Task<bool> ReorderSetlistSongsAsync(int setlistId, int[] songOrdering, string userId)
    {
        try
        {
            // Always query fresh data from the database to avoid change tracking issues
            var setlistSongs = await _context.SetlistSongs
                .Where(ss => ss.SetlistId == setlistId && ss.Setlist.UserId == userId)
                .ToListAsync();

            if (!setlistSongs.Any())
            {
                _logger.LogWarning("Setlist {SetlistId} not found or unauthorized for user {UserId}", 
                    setlistId, userId);
                return false;
            }

            // Validate that all songs in ordering exist in setlist
            var setlistSongIds = setlistSongs.Select(ss => ss.SongId).ToHashSet();
            if (!songOrdering.All(id => setlistSongIds.Contains(id)))
            {
                _logger.LogWarning("Invalid song ordering for setlist {SetlistId}: some songs not in setlist", 
                    setlistId);
                return false;
            }

            // Update positions
            for (int i = 0; i < songOrdering.Length; i++)
            {
                var setlistSong = setlistSongs.First(ss => ss.SongId == songOrdering[i]);
                setlistSong.Position = i + 1;
                _logger.LogDebug("Setting song {SongId} to position {Position}", songOrdering[i], i + 1);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Reordered songs in setlist {SetlistId} for user {UserId}", setlistId, userId);

            return true;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error reordering setlist {SetlistId} for user {UserId}", setlistId, userId);
            throw;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error reordering setlist {SetlistId} for user {UserId}", setlistId, userId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation reordering setlist {SetlistId} for user {UserId}", setlistId, userId);
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
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating setlist song {SetlistSongId} for user {UserId}", 
                setlistSongId, userId);
            throw;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating setlist song {SetlistSongId} for user {UserId}", 
                setlistSongId, userId);
            throw;
        }
    }

    public async Task<Setlist?> CopySetlistAsync(int sourceSetlistId, string newName, string userId)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Setlist name cannot be null or empty", nameof(newName));
        }

        try
        {
            var sourceSetlist = await GetSourceSetlistForCopyingAsync(sourceSetlistId, userId);
            if (sourceSetlist == null) return null;

            var newSetlist = await CreateCopiedSetlistAsync(sourceSetlist, newName, userId);
            await CopySetlistSongsAsync(sourceSetlist, newSetlist);

            _logger.LogInformation("Copied setlist {SourceId} to new setlist {NewId} '{NewName}' for user {UserId}", 
                sourceSetlistId, newSetlist.Id, newName, userId);

            return newSetlist;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error copying setlist {SetlistId} for user {UserId}", 
                sourceSetlistId, userId);
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument copying setlist {SetlistId} for user {UserId}", 
                sourceSetlistId, userId);
            throw;
        }
    }

    private async Task<Setlist?> GetSourceSetlistForCopyingAsync(int sourceSetlistId, string userId)
    {
        var sourceSetlist = await _context.Setlists
            .Include(sl => sl.SetlistSongs)
            .ThenInclude(ss => ss.Song)
            .FirstOrDefaultAsync(sl => sl.Id == sourceSetlistId && sl.UserId == userId);

        if (sourceSetlist == null)
        {
            _logger.LogWarning("Source setlist {SetlistId} not found or unauthorized for user {UserId}", 
                sourceSetlistId, userId);
        }

        return sourceSetlist;
    }

    private async Task<Setlist> CreateCopiedSetlistAsync(Setlist sourceSetlist, string newName, string userId)
    {
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

        return newSetlist;
    }

    private async Task CopySetlistSongsAsync(Setlist sourceSetlist, Setlist newSetlist)
    {
        var newSetlistSongs = sourceSetlist.SetlistSongs
            .OrderBy(ss => ss.Position)
            .Select(sourceSong => new SetlistSong
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
            });

        _context.SetlistSongs.AddRange(newSetlistSongs);
        await _context.SaveChangesAsync();
    }

    public async Task<Setlist?> CreateFromTemplateAsync(
        int templateId,
        string userId,
        string name,
        DateTime? performanceDate = null,
        string? venue = null,
        string? performanceNotes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Setlist name cannot be null or empty", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        try
        {
            // Get the template and verify ownership and template status
            var template = await _context.Setlists
                .Include(sl => sl.SetlistSongs)
                .ThenInclude(ss => ss.Song)
                .FirstOrDefaultAsync(sl => sl.Id == templateId && sl.UserId == userId && sl.IsTemplate);

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found, not a template, or unauthorized for user {UserId}", 
                    templateId, userId);
                return null;
            }

            // Validate the new setlist name
            if (name.Length > 200)
            {
                throw new ArgumentException("Setlist name cannot exceed 200 characters", nameof(name));
            }

            // Create new active setlist from template
            var newSetlist = new Setlist
            {
                Name = name,
                Description = template.Description,
                Venue = venue,
                PerformanceDate = performanceDate,
                ExpectedDurationMinutes = template.ExpectedDurationMinutes,
                IsTemplate = false,  // New setlist is NOT a template
                IsActive = true,     // New setlist is ACTIVE for performance
                PerformanceNotes = performanceNotes ?? template.PerformanceNotes,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Validate the new setlist
            var validationErrors = ValidateSetlist(newSetlist);
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

            _context.Setlists.Add(newSetlist);
            await _context.SaveChangesAsync();

            // Copy all songs from template to new setlist
            await CopySetlistSongsAsync(template, newSetlist);

            // Invalidate cache for user
            await _cacheService.InvalidateUserCacheAsync(userId);

            _logger.LogInformation("Created setlist {SetlistId} '{Name}' from template {TemplateId} for user {UserId}", 
                newSetlist.Id, name, templateId, userId);

            return newSetlist;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating setlist from template {TemplateId} for user {UserId}", 
                templateId, userId);
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument creating setlist from template {TemplateId} for user {UserId}", 
                templateId, userId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation creating setlist from template {TemplateId} for user {UserId}", 
                templateId, userId);
            throw;
        }
    }

    public IEnumerable<string> ValidateSetlist(Setlist setlist)
    {
        var errors = new List<string>();

        ValidateSetlistName(setlist.Name, errors);
        ValidateSetlistDescription(setlist.Description, errors);
        ValidateSetlistVenue(setlist.Venue, errors);
        ValidateSetlistDuration(setlist.ExpectedDurationMinutes, errors);
        ValidateSetlistPerformanceNotes(setlist.PerformanceNotes, errors);
        ValidateSetlistUserId(setlist.UserId, errors);

        return errors;
    }

    private static void ValidateSetlistName(string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Setlist name is required");
        }
        else if (name.Length > 200)
        {
            errors.Add("Setlist name cannot exceed 200 characters");
        }
    }

    private static void ValidateSetlistDescription(string? description, List<string> errors)
    {
        if (!string.IsNullOrEmpty(description) && description.Length > 1000)
        {
            errors.Add("Description cannot exceed 1000 characters");
        }
    }

    private static void ValidateSetlistVenue(string? venue, List<string> errors)
    {
        if (!string.IsNullOrEmpty(venue) && venue.Length > 200)
        {
            errors.Add("Venue cannot exceed 200 characters");
        }
    }

    private static void ValidateSetlistDuration(int? durationMinutes, List<string> errors)
    {
        if (durationMinutes.HasValue && durationMinutes < 1)
        {
            errors.Add("Expected duration must be at least 1 minute");
        }
    }

    private static void ValidateSetlistPerformanceNotes(string? performanceNotes, List<string> errors)
    {
        if (!string.IsNullOrEmpty(performanceNotes) && performanceNotes.Length > 2000)
        {
            errors.Add("Performance notes cannot exceed 2000 characters");
        }
    }

    private static void ValidateSetlistUserId(string userId, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            errors.Add("User ID is required");
        }
    }
}