using SetlistStudio.Core.Entities;

namespace SetlistStudio.Core.Interfaces;

/// <summary>
/// Service interface for exporting setlists to various formats
/// Provides functionality to export setlist data for sharing with band members or venue coordinators
/// </summary>
public interface ISetlistExportService
{
    /// <summary>
    /// Exports a setlist to CSV format with song details and performance metadata
    /// </summary>
    /// <param name="setlistId">The setlist ID to export</param>
    /// <param name="userId">The user's ID for authorization</param>
    /// <returns>CSV content as byte array if successful, null if not found or unauthorized</returns>
    Task<byte[]?> ExportSetlistToCsvAsync(int setlistId, string userId);

    /// <summary>
    /// Generates a CSV filename for a setlist based on its name and date
    /// </summary>
    /// <param name="setlist">The setlist to generate filename for</param>
    /// <returns>Sanitized filename suitable for download</returns>
    string GenerateCsvFilename(Setlist setlist);
}
