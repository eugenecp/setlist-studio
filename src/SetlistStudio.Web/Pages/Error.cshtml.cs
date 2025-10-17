using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace SetlistStudio.Web.Pages;

/// <summary>
/// Error page model that provides secure error handling without leaking sensitive information
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet()
    {
        // Only show a generic request ID, never expose actual exception details
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}