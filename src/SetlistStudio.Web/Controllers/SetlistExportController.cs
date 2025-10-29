using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SetlistStudio.Core.Interfaces;
using System.Security.Claims;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// Controller for exporting setlists to various formats.
/// Provides endpoints for downloading setlist data in CSV format for sharing with band members and venue coordinators.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
[EnableRateLimiting("DefaultPolicy")] // Apply rate limiting
public class SetlistExportController : ControllerBase
{
    private readonly ISetlistExportService _exportService;
    private readonly ISetlistService _setlistService;
    private readonly ILogger<SetlistExportController> _logger;

    public SetlistExportController(
        ISetlistExportService exportService,
        ISetlistService setlistService,
        ILogger<SetlistExportController> logger)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _setlistService = setlistService ?? throw new ArgumentNullException(nameof(setlistService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports a setlist to CSV format with song details and performance metadata.
    /// </summary>
    /// <param name="id">The setlist ID to export</param>
    /// <returns>CSV file download if successful, error status otherwise</returns>
    [HttpGet("{id}/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportToCsv(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized export attempt for setlist {SetlistId} - no user ID found", id);
                return Unauthorized("User authentication required");
            }

            _logger.LogInformation("Export CSV requested for setlist {SetlistId} by user {UserId}", id, userId);

            // Get setlist for filename generation (also verifies authorization)
            var setlist = await _setlistService.GetSetlistByIdAsync(id, userId);
            if (setlist == null)
            {
                _logger.LogWarning("Setlist {SetlistId} not found or unauthorized for user {UserId}", id, userId);
                return NotFound($"Setlist with ID {id} not found or you don't have permission to access it");
            }

            // Export to CSV
            var csvBytes = await _exportService.ExportSetlistToCsvAsync(id, userId);
            if (csvBytes == null)
            {
                _logger.LogWarning("Failed to export setlist {SetlistId} to CSV for user {UserId}", id, userId);
                return NotFound($"Could not export setlist with ID {id}");
            }

            // Generate filename
            var filename = _exportService.GenerateCsvFilename(setlist);

            _logger.LogInformation("Successfully exported setlist {SetlistId} to CSV ({Size} bytes) for user {UserId}",
                id, csvBytes.Length, userId);

            // Return CSV file
            return File(csvBytes, "text/csv", filename);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Argument null error exporting setlist {SetlistId}", id);
            return BadRequest("Invalid request parameters");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempting to export setlist {SetlistId}", id);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while exporting setlist {SetlistId}", id);
            return StatusCode(503, "Export service temporarily unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error exporting setlist {SetlistId} to CSV", id);
            return StatusCode(500, "An error occurred while exporting the setlist");
        }
    }
}
