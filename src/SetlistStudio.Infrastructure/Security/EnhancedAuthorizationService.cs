using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Security;
using SetlistStudio.Infrastructure.Data;

namespace SetlistStudio.Infrastructure.Security;

/// <summary>
/// Enhanced authorization service providing resource-based authorization with comprehensive security logging
/// Validates user ownership, handles complex authorization scenarios, and provides detailed audit trails
/// </summary>
public class EnhancedAuthorizationService
{
    private readonly SetlistStudioDbContext _context;
    private readonly ILogger<EnhancedAuthorizationService> _logger;

    public EnhancedAuthorizationService(SetlistStudioDbContext context, ILogger<EnhancedAuthorizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Authorizes access to a song resource with enhanced security logging
    /// </summary>
    /// <param name="songId">The song ID to authorize</param>
    /// <param name="userId">The requesting user's ID</param>
    /// <param name="action">The action being attempted</param>
    /// <returns>Authorization result with detailed security information</returns>
    public async Task<AuthorizationResult> AuthorizeSongAccessAsync(int songId, string userId, ResourceAuthorizationHelper.ResourceAction action)
    {
        try
        {
            // Validate user ID first
            var userValidation = ResourceAuthorizationHelper.ValidateUserId(userId, ResourceAuthorizationHelper.ResourceType.Song, songId.ToString(), action, _logger);
            if (!userValidation.IsAuthorized)
            {
                return userValidation;
            }

            // Get song with user information
            var song = await _context.Songs
                .Select(s => new { s.Id, s.UserId })
                .FirstOrDefaultAsync(s => s.Id == songId);

            // Validate resource ownership
            var authResult = ResourceAuthorizationHelper.ValidateResourceOwnership(
                song?.UserId,
                userId,
                ResourceAuthorizationHelper.ResourceType.Song,
                songId.ToString(),
                action,
                _logger);

            // Add performance metrics to security context
            authResult.SecurityContext["QueryExecutionTime"] = DateTime.UtcNow;
            authResult.SecurityContext["DatabaseChecked"] = true;

            return authResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during song authorization for user {UserId}, song {SongId}", userId, songId);
            return AuthorizationResult.NotFound(userId, "Song", songId.ToString(), action.ToString());
        }
    }

    /// <summary>
    /// Authorizes access to a setlist resource with enhanced security logging
    /// </summary>
    /// <param name="setlistId">The setlist ID to authorize</param>
    /// <param name="userId">The requesting user's ID</param>
    /// <param name="action">The action being attempted</param>
    /// <returns>Authorization result with detailed security information</returns>
    public async Task<AuthorizationResult> AuthorizeSetlistAccessAsync(int setlistId, string userId, ResourceAuthorizationHelper.ResourceAction action)
    {
        try
        {
            // Validate user ID first
            var userValidation = ResourceAuthorizationHelper.ValidateUserId(userId, ResourceAuthorizationHelper.ResourceType.Setlist, setlistId.ToString(), action, _logger);
            if (!userValidation.IsAuthorized)
            {
                return userValidation;
            }

            // Get setlist with user information
            var setlist = await _context.Setlists
                .Select(sl => new { sl.Id, sl.UserId })
                .FirstOrDefaultAsync(sl => sl.Id == setlistId);

            // Validate resource ownership
            var authResult = ResourceAuthorizationHelper.ValidateResourceOwnership(
                setlist?.UserId,
                userId,
                ResourceAuthorizationHelper.ResourceType.Setlist,
                setlistId.ToString(),
                action,
                _logger);

            // Add performance metrics to security context
            authResult.SecurityContext["QueryExecutionTime"] = DateTime.UtcNow;
            authResult.SecurityContext["DatabaseChecked"] = true;

            return authResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during setlist authorization for user {UserId}, setlist {SetlistId}", userId, setlistId);
            return AuthorizationResult.NotFound(userId, "Setlist", setlistId.ToString(), action.ToString());
        }
    }

    /// <summary>
    /// Authorizes access to a setlist song with complex ownership validation
    /// Validates both setlist and song ownership for comprehensive security
    /// </summary>
    /// <param name="setlistSongId">The setlist song ID to authorize</param>
    /// <param name="userId">The requesting user's ID</param>
    /// <param name="action">The action being attempted</param>
    /// <returns>Authorization result with detailed security information</returns>
    public async Task<AuthorizationResult> AuthorizeSetlistSongAccessAsync(int setlistSongId, string userId, ResourceAuthorizationHelper.ResourceAction action)
    {
        try
        {
            // Validate user ID first
            var userValidation = ResourceAuthorizationHelper.ValidateUserId(userId, ResourceAuthorizationHelper.ResourceType.SetlistSong, setlistSongId.ToString(), action, _logger);
            if (!userValidation.IsAuthorized)
            {
                return userValidation;
            }

            // Get setlist song with related entity information for comprehensive authorization
            var setlistSong = await _context.SetlistSongs
                .Include(ss => ss.Setlist)
                .Include(ss => ss.Song)
                .Select(ss => new { 
                    ss.Id, 
                    SetlistUserId = ss.Setlist.UserId, 
                    SongUserId = ss.Song.UserId,
                    SetlistId = ss.SetlistId,
                    SongId = ss.SongId
                })
                .FirstOrDefaultAsync(ss => ss.Id == setlistSongId);

            if (setlistSong == null)
            {
                var notFoundResult = AuthorizationResult.NotFound(userId, "SetlistSong", setlistSongId.ToString(), action.ToString());
                ResourceAuthorizationHelper.LogAuthorizationFailure(_logger, notFoundResult);
                return notFoundResult;
            }

            // Validate both setlist and song ownership
            var setlistAuth = ResourceAuthorizationHelper.ValidateResourceOwnership(
                setlistSong.SetlistUserId,
                userId,
                ResourceAuthorizationHelper.ResourceType.Setlist,
                setlistSong.SetlistId.ToString(),
                action,
                _logger);

            if (!setlistAuth.IsAuthorized)
            {
                setlistAuth.ResourceType = "SetlistSong";
                setlistAuth.ResourceId = setlistSongId.ToString();
                setlistAuth.Reason = "User does not own the setlist containing this song";
                return setlistAuth;
            }

            var songAuth = ResourceAuthorizationHelper.ValidateResourceOwnership(
                setlistSong.SongUserId,
                userId,
                ResourceAuthorizationHelper.ResourceType.Song,
                setlistSong.SongId.ToString(),
                action,
                _logger);

            if (!songAuth.IsAuthorized)
            {
                songAuth.ResourceType = "SetlistSong";
                songAuth.ResourceId = setlistSongId.ToString();
                songAuth.Reason = "User does not own the song in this setlist";
                return songAuth;
            }

            // Both checks passed - create success result
            var successResult = AuthorizationResult.Success(userId, "SetlistSong", setlistSongId.ToString(), action.ToString());
            successResult.SecurityContext["SetlistId"] = setlistSong.SetlistId;
            successResult.SecurityContext["SongId"] = setlistSong.SongId;
            successResult.SecurityContext["ComprehensiveCheck"] = true;
            
            ResourceAuthorizationHelper.LogAuthorizationSuccess(_logger, successResult);
            return successResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during setlist song authorization for user {UserId}, setlist song {SetlistSongId}", userId, setlistSongId);
            return AuthorizationResult.NotFound(userId, "SetlistSong", setlistSongId.ToString(), action.ToString());
        }
    }

    /// <summary>
    /// Authorizes bulk song access for operations affecting multiple songs
    /// Optimized for performance with single database query
    /// </summary>
    /// <param name="songIds">The song IDs to authorize</param>
    /// <param name="userId">The requesting user's ID</param>
    /// <param name="action">The action being attempted</param>
    /// <returns>Dictionary of song IDs to authorization results</returns>
    public async Task<Dictionary<int, AuthorizationResult>> AuthorizeBulkSongAccessAsync(IEnumerable<int> songIds, string userId, ResourceAuthorizationHelper.ResourceAction action)
    {
        var results = new Dictionary<int, AuthorizationResult>();
        var songIdList = songIds.ToList();

        try
        {
            // Validate user ID first
            var userValidation = ResourceAuthorizationHelper.ValidateUserId(userId, ResourceAuthorizationHelper.ResourceType.Song, "bulk", action, _logger);
            if (!userValidation.IsAuthorized)
            {
                // Return failure for all songs if user ID is invalid
                foreach (var songId in songIdList)
                {
                    results[songId] = AuthorizationResult.InvalidUser(userId, "Song", songId.ToString(), action.ToString());
                }
                return results;
            }

            // Single query to get all songs
            var songs = await _context.Songs
                .Where(s => songIdList.Contains(s.Id))
                .Select(s => new { s.Id, s.UserId })
                .ToListAsync();

            var songLookup = songs.ToDictionary(s => s.Id, s => s.UserId);

            // Create resource mapping for bulk validation
            var resourceUserIds = songIdList.ToDictionary(
                id => id.ToString(), 
                id => songLookup.TryGetValue(id, out var uid) ? uid : null);

            var bulkResults = ResourceAuthorizationHelper.ValidateBulkResourceOwnership(
                resourceUserIds,
                userId,
                ResourceAuthorizationHelper.ResourceType.Song,
                action,
                _logger);

            // Convert string keys back to integers
            foreach (var songId in songIdList)
            {
                results[songId] = bulkResults[songId.ToString()];
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during bulk song authorization for user {UserId}", userId);
            
            // Return failure for all songs on exception
            foreach (var songId in songIdList)
            {
                results[songId] = AuthorizationResult.NotFound(userId, "Song", songId.ToString(), action.ToString());
            }
            return results;
        }
    }

    /// <summary>
    /// Authorizes adding a song to a setlist with comprehensive ownership validation
    /// Validates both song and setlist ownership before allowing the operation
    /// </summary>
    /// <param name="setlistId">The setlist ID to add the song to</param>
    /// <param name="songId">The song ID to add</param>
    /// <param name="userId">The requesting user's ID</param>
    /// <returns>Authorization result with detailed security information</returns>
    public async Task<AuthorizationResult> AuthorizeAddSongToSetlistAsync(int setlistId, int songId, string userId)
    {
        try
        {
            // Validate user ID
            var userValidation = ResourceAuthorizationHelper.ValidateUserId(userId, ResourceAuthorizationHelper.ResourceType.SetlistSong, $"{setlistId}-{songId}", ResourceAuthorizationHelper.ResourceAction.Create, _logger);
            if (!userValidation.IsAuthorized)
            {
                return userValidation;
            }

            // Check both setlist and song ownership in parallel
            var setlistTask = AuthorizeSetlistAccessAsync(setlistId, userId, ResourceAuthorizationHelper.ResourceAction.Update);
            var songTask = AuthorizeSongAccessAsync(songId, userId, ResourceAuthorizationHelper.ResourceAction.Read);

            await Task.WhenAll(setlistTask, songTask);

            var setlistAuth = setlistTask.Result;
            var songAuth = songTask.Result;

            if (!setlistAuth.IsAuthorized)
            {
                setlistAuth.Reason = "Cannot add song: User does not own the target setlist";
                setlistAuth.Action = "AddSongToSetlist";
                return setlistAuth;
            }

            if (!songAuth.IsAuthorized)
            {
                songAuth.Reason = "Cannot add song: User does not own the song being added";
                songAuth.Action = "AddSongToSetlist";
                return songAuth;
            }

            // Both authorizations successful
            var successResult = AuthorizationResult.Success(userId, "SetlistSong", $"{setlistId}-{songId}", "AddSongToSetlist");
            successResult.SecurityContext["SetlistId"] = setlistId;
            successResult.SecurityContext["SongId"] = songId;
            successResult.SecurityContext["ParallelChecks"] = true;

            ResourceAuthorizationHelper.LogAuthorizationSuccess(_logger, successResult);
            return successResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during add song to setlist authorization for user {UserId}, setlist {SetlistId}, song {SongId}", userId, setlistId, songId);
            return AuthorizationResult.NotFound(userId, "SetlistSong", $"{setlistId}-{songId}", "AddSongToSetlist");
        }
    }
}