using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Core.Entities;
using SetlistStudio.Web.Models;
using System.Security.Claims;

namespace SetlistStudio.Web.Controllers;

/// <summary>
/// API controller for managing setlist templates
/// Provides endpoints for creating performance setlists from templates
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TemplateController : ControllerBase
{
    private readonly ISetlistService _setlistService;
    private readonly ILogger<TemplateController> _logger;

    public TemplateController(ISetlistService setlistService, ILogger<TemplateController> logger)
    {
        _setlistService = setlistService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new active setlist from a template
    /// </summary>
    /// <param name="templateId">The template ID to create from</param>
    /// <param name="request">The setlist creation request</param>
    /// <returns>The created setlist</returns>
    [HttpPost("{templateId}/create")]
    [ProducesResponseType(typeof(Setlist), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Setlist>> CreateFromTemplate(
        int templateId,
        [FromBody] CreateFromTemplateRequest request)
    {
        // 1. Get userId from authenticated user (security pattern)
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Unauthorized template access attempt - no user ID");
            return Unauthorized();
        }

        try
        {
            // 2. Validate request (follows documented validation pattern)
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request creating from template {TemplateId} for user {UserId}", 
                    templateId, userId);
                return BadRequest(ModelState);
            }

            // 3. Call service following documented CreateFromTemplateAsync pattern
            var setlist = await _setlistService.CreateFromTemplateAsync(
                templateId,
                userId,
                request.Name,
                request.PerformanceDate,
                request.Venue,
                request.PerformanceNotes);

            // 4. Handle authorization/not found (documented pattern: service returns null)
            if (setlist == null)
            {
                _logger.LogWarning(
                    "Template {TemplateId} not found or unauthorized for user {UserId}", 
                    templateId, userId);
                return NotFound(new { message = "Template not found or access denied" });
            }

            // 5. Success response with 201 Created (RESTful pattern)
            _logger.LogInformation(
                "Created setlist {SetlistId} from template {TemplateId} for user {UserId}", 
                setlist.Id, templateId, userId);

            return CreatedAtAction(
                nameof(GetSetlist), 
                new { id = setlist.Id }, 
                setlist);
        }
        catch (ArgumentException ex)
        {
            // 6. Validation errors return 400 Bad Request
            _logger.LogWarning(ex, 
                "Validation error creating from template {TemplateId} for user {UserId}", 
                templateId, userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // 7. Unexpected errors return 500 with sanitized message
            _logger.LogError(ex, 
                "Error creating setlist from template {TemplateId} for user {UserId}", 
                templateId, userId);
            return StatusCode(500, new { message = "An error occurred creating the setlist" });
        }
    }

    // Placeholder for GetSetlist endpoint (referenced in CreatedAtAction)
    [HttpGet("{id}")]
    public async Task<ActionResult<Setlist>> GetSetlist(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var setlist = await _setlistService.GetSetlistByIdAsync(id, userId);
        if (setlist == null)
            return NotFound();

        return Ok(setlist);
    }
}
