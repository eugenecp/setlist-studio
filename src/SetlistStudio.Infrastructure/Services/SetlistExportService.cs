using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Security;
using SetlistStudio.Infrastructure.Data;
using System.Text;

namespace SetlistStudio.Infrastructure.Services;

/// <summary>
/// Service implementation for exporting setlists to various formats
/// Provides CSV export functionality for sharing setlists with band members and venue coordinators
/// </summary>
public class SetlistExportService : ISetlistExportService
{
    private readonly SetlistStudioDbContext _context;
    private readonly ILogger<SetlistExportService> _logger;

    public SetlistExportService(SetlistStudioDbContext context, ILogger<SetlistExportService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<byte[]?> ExportSetlistToCsvAsync(int setlistId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentNullException(nameof(userId));
        }

        try
        {
            var setlist = await GetSetlistWithSongsAsync(setlistId, userId);

            if (setlist == null)
            {
                _logger.LogWarning("Setlist {SetlistId} not found or unauthorized for user {UserId}",
                    setlistId, SecureLoggingHelper.SanitizeUserId(userId));
                return null;
            }

            var csvContent = GenerateCsvContent(setlist);
            var csvBytes = Encoding.UTF8.GetBytes(csvContent);

            var sanitizedName = SecureLoggingHelper.SanitizeMessage(setlist.Name);
            _logger.LogInformation("Exported setlist {SetlistId} '{Name}' to CSV for user {UserId}",
                setlistId, sanitizedName, SecureLoggingHelper.SanitizeUserId(userId));

            return csvBytes;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while exporting setlist {SetlistId} for user {UserId}",
                setlistId, userId);
            throw;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while exporting setlist {SetlistId} for user {UserId}",
                setlistId, userId);
            throw;
        }
    }

    public string GenerateCsvFilename(Setlist setlist)
    {
        if (setlist == null)
        {
            throw new ArgumentNullException(nameof(setlist));
        }

        var sanitizedName = SanitizeFilename(setlist.Name);
        var dateString = setlist.PerformanceDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        return $"setlist_{sanitizedName}_{dateString}.csv";
    }

    private async Task<Setlist?> GetSetlistWithSongsAsync(int setlistId, string userId)
    {
        return await _context.Setlists
            .Include(sl => sl.SetlistSongs.OrderBy(ss => ss.Position))
            .ThenInclude(ss => ss.Song)
            .FirstOrDefaultAsync(sl => sl.Id == setlistId && sl.UserId == userId);
    }

    private string GenerateCsvContent(Setlist setlist)
    {
        var csv = new StringBuilder();

        // Add setlist header information
        csv.AppendLine("# Setlist Export");
        csv.AppendLine($"# Name: {EscapeCsvValue(setlist.Name)}");
        if (!string.IsNullOrEmpty(setlist.Description))
        {
            csv.AppendLine($"# Description: {EscapeCsvValue(setlist.Description)}");
        }
        if (!string.IsNullOrEmpty(setlist.Venue))
        {
            csv.AppendLine($"# Venue: {EscapeCsvValue(setlist.Venue)}");
        }
        if (setlist.PerformanceDate.HasValue)
        {
            csv.AppendLine($"# Performance Date: {setlist.PerformanceDate.Value:yyyy-MM-dd HH:mm}");
        }
        if (setlist.ExpectedDurationMinutes.HasValue)
        {
            csv.AppendLine($"# Expected Duration: {setlist.ExpectedDurationMinutes.Value} minutes");
        }
        csv.AppendLine($"# Total Songs: {setlist.SongCount}");
        csv.AppendLine($"# Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        csv.AppendLine();

        // Add column headers
        csv.AppendLine("Position,Title,Artist,Key,BPM,Duration (sec),Genre,Difficulty,Notes,Transition Notes,Encore,Optional");

        // Add song rows
        foreach (var setlistSong in setlist.SetlistSongs.OrderBy(ss => ss.Position))
        {
            var song = setlistSong.Song;
            var row = new[]
            {
                setlistSong.Position.ToString(),
                EscapeCsvValue(song.Title),
                EscapeCsvValue(song.Artist),
                EscapeCsvValue(setlistSong.CustomKey ?? song.MusicalKey ?? ""),
                (setlistSong.CustomBpm ?? song.Bpm)?.ToString() ?? "",
                song.DurationSeconds?.ToString() ?? "",
                EscapeCsvValue(song.Genre ?? ""),
                song.DifficultyRating?.ToString() ?? "",
                EscapeCsvValue(setlistSong.PerformanceNotes ?? ""),
                EscapeCsvValue(setlistSong.TransitionNotes ?? ""),
                setlistSong.IsEncore ? "Yes" : "No",
                setlistSong.IsOptional ? "Yes" : "No"
            };
            csv.AppendLine(string.Join(",", row));
        }

        return csv.ToString();
    }

    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        // If value contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string SanitizeFilename(string filename)
    {
        // Remove or replace invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder();

        foreach (var c in filename)
        {
            if (invalidChars.Contains(c))
            {
                sanitized.Append('_');
            }
            else
            {
                sanitized.Append(c);
            }
        }

        // Limit length and trim spaces
        var result = sanitized.ToString().Trim();
        if (result.Length > 50)
        {
            result = result.Substring(0, 50);
        }

        return result;
    }
}
