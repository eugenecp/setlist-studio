using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace SetlistStudio.Core.Validation;

/// <summary>
/// Validates and sanitizes string input to prevent XSS attacks and malicious content
/// Removes or encodes potentially dangerous characters and patterns
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class SanitizedStringAttribute : ValidationAttribute
{
    public bool AllowHtml { get; set; } = false;
    public bool AllowLineBreaks { get; set; } = true;
    public bool AllowSpecialCharacters { get; set; } = true;
    public int MaxLength { get; set; } = -1;

    private static readonly Regex ScriptTagPattern = new Regex(
        @"<\s*script[^>]*>.*?<\s*/\s*script\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex HtmlTagPattern = new Regex(
        @"<[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex JavascriptPattern = new Regex(
        @"(javascript:|vbscript:|on\w+\s*=)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex SqlInjectionPattern = new Regex(
        @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|UNION)\b)|(--)|(\bOR\b\s+\b\d+\s*=\s*\d+)|(\bAND\b\s+\b\d+\s*=\s*\d+)|(';)|('\s+(OR|AND|UNION|SELECT))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly string[] DangerousPatterns = 
    {
        "<script", "</script>", "javascript:", "vbscript:", "onload=", "onclick=", "onerror=",
        "eval(", "document.cookie", "document.write", "innerHTML", "outerHTML", "window.location"
    };

    public SanitizedStringAttribute()
    {
        ErrorMessage = "{0} contains potentially dangerous content. Please remove HTML tags, scripts, and unsafe characters for security reasons.";
    }

    public override bool IsValid(object? value)
    {
        if (value == null || (value is string str && (string.IsNullOrEmpty(str) || IsOnlyAllowedWhitespace(str))))
        {
            return true;
        }

        if (value is not string stringValue)
        {
            return false;
        }

        // Check for dangerous content and reject it
        if (ContainsDangerousContent(stringValue))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if input contains potentially dangerous content
    /// </summary>
    private bool ContainsDangerousContent(string input)
    {
        if (string.IsNullOrEmpty(input) || IsOnlyAllowedWhitespace(input))
        {
            return false;
        }

        return ContainsScriptingThreats(input) ||
               ContainsSqlInjectionPatterns(input) ||
               ContainsUnauthorizedHtml(input) ||
               ContainsDangerousPatterns(input) ||
               ContainsControlCharacters(input) ||
               ExceedsMaximumLength(input);
    }

    /// <summary>
    /// Checks for script tags and JavaScript protocol threats
    /// </summary>
    private bool ContainsScriptingThreats(string input)
    {
        return ScriptTagPattern.IsMatch(input) || JavascriptPattern.IsMatch(input);
    }

    /// <summary>
    /// Checks for SQL injection attack patterns
    /// </summary>
    private bool ContainsSqlInjectionPatterns(string input)
    {
        return SqlInjectionPattern.IsMatch(input);
    }

    /// <summary>
    /// Checks for unauthorized HTML content when HTML is not allowed
    /// </summary>
    private bool ContainsUnauthorizedHtml(string input)
    {
        return !AllowHtml && ContainsHtml(input) && !IsHtmlEncoded(input);
    }

    /// <summary>
    /// Checks for dangerous string patterns that could be used in attacks
    /// </summary>
    private bool ContainsDangerousPatterns(string input)
    {
        return DangerousPatterns.Any(pattern => input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if input exceeds the maximum allowed length
    /// </summary>
    private bool ExceedsMaximumLength(string input)
    {
        return MaxLength > 0 && input.Length > MaxLength;
    }

    /// <summary>
    /// Checks if input contains control characters
    /// </summary>
    private static bool ContainsControlCharacters(string input)
    {
        return input.Any(c => IsDangerousControlCharacter(c));
    }

    /// <summary>
    /// Determines if a character is a dangerous control character
    /// </summary>
    private static bool IsDangerousControlCharacter(char c)
    {
        return IsC0ControlCharacter(c) || IsC1ControlCharacter(c) || IsOtherControlCharacter(c);
    }

    /// <summary>
    /// Checks for dangerous C0 control characters (0-31) excluding safe whitespace
    /// </summary>
    private static bool IsC0ControlCharacter(char c)
    {
        int charCode = (int)c;
        return charCode >= 0 && charCode <= 31 && charCode != 9 && charCode != 10 && charCode != 13;
    }

    /// <summary>
    /// Checks for C1 control characters (128-159) which are always dangerous
    /// </summary>
    private static bool IsC1ControlCharacter(char c)
    {
        int charCode = (int)c;
        return charCode >= 128 && charCode <= 159;
    }

    /// <summary>
    /// Checks for other dangerous control characters using built-in detection
    /// </summary>
    private static bool IsOtherControlCharacter(char c)
    {
        return char.IsControl(c) && c != '\r' && c != '\n' && c != '\t';
    }

    /// <summary>
    /// Checks if string contains only allowed whitespace characters (space, tab, newline, carriage return)
    /// </summary>
    private static bool IsOnlyAllowedWhitespace(string input)
    {
        return input.All(c => c == ' ' || c == '\t' || c == '\n' || c == '\r');
    }

    /// <summary>
    /// Sanitizes input string to remove potentially dangerous content
    /// </summary>
    public string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input ?? string.Empty;
        }

        var sanitized = input;

        // Remove script tags completely
        sanitized = ScriptTagPattern.Replace(sanitized, string.Empty);

        // Remove javascript: and similar dangerous protocols
        sanitized = JavascriptPattern.Replace(sanitized, string.Empty);

        // Check for SQL injection patterns and remove them
        if (SqlInjectionPattern.IsMatch(sanitized))
        {
            // For musical application, we're strict about SQL-like patterns
            sanitized = SqlInjectionPattern.Replace(sanitized, "***");
        }

        // Remove dangerous patterns
        foreach (var pattern in DangerousPatterns)
        {
            sanitized = sanitized.Replace(pattern, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        // Handle HTML content based on AllowHtml setting
        if (!AllowHtml && ContainsHtml(sanitized))
        {
            // HTML encode the content
            sanitized = HttpUtility.HtmlEncode(sanitized);
        }

        // Handle line breaks
        if (!AllowLineBreaks)
        {
            sanitized = sanitized.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
        }

        // Handle special characters for musical content
        if (!AllowSpecialCharacters)
        {
            // Keep musical notation characters but remove others
            sanitized = Regex.Replace(sanitized, @"[^\w\s\-#♭♯°øØ\(\)\[\]\.\,\;\:\!\?\'\""""]", string.Empty,
                RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }

        // Apply length limit
        if (MaxLength > 0 && sanitized.Length > MaxLength)
        {
            sanitized = sanitized.Substring(0, MaxLength).Trim();
        }

        // Final trim
        sanitized = sanitized.Trim();

        return sanitized;
    }

    private static bool ContainsHtml(string input)
    {
        return HtmlTagPattern.IsMatch(input);
    }

    /// <summary>
    /// Checks if the input contains HTML entities (which are safe)
    /// </summary>
    private static bool IsHtmlEncoded(string input)
    {
        // Check for common HTML entities
        return input.Contains("&lt;") || input.Contains("&gt;") || input.Contains("&amp;") || 
               input.Contains("&quot;") || input.Contains("&#") || input.Contains("&apos;");
    }

    /// <summary>
    /// Validates that a string is safe for musical content (song titles, artist names, etc.)
    /// </summary>
    public static bool IsSafeForMusicalContent(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        // Check for obviously dangerous patterns
        if (DangerousPatterns.Any(pattern => input.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Check for script tags
        if (ScriptTagPattern.IsMatch(input))
        {
            return false;
        }

        // Check for javascript protocols
        if (JavascriptPattern.IsMatch(input))
        {
            return false;
        }

        // Check for SQL injection patterns
        if (SqlInjectionPattern.IsMatch(input))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Sanitizes musical content (song titles, artist names, notes) with appropriate rules
    /// </summary>
    public static string SanitizeMusicalContent(string input)
    {
        var attribute = new SanitizedStringAttribute
        {
            AllowHtml = false,
            AllowLineBreaks = true,
            AllowSpecialCharacters = true,
            MaxLength = -1
        };

        return attribute.SanitizeInput(input);
    }

    /// <summary>
    /// Sanitizes user notes with more permissive rules for rich content
    /// </summary>
    public static string SanitizeUserNotes(string input)
    {
        var attribute = new SanitizedStringAttribute
        {
            AllowHtml = false, // Still no HTML for security
            AllowLineBreaks = true,
            AllowSpecialCharacters = true,
            MaxLength = 2000
        };

        return attribute.SanitizeInput(input);
    }
}