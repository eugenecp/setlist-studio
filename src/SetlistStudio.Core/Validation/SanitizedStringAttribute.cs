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
        TimeSpan.FromMilliseconds(500));

    private static readonly Regex SqlInjectionPattern = new Regex(
        @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|UNION)\b)|(--)|(\bOR\b\s+\b\d+\s*=\s*\d+)|(\bAND\b\s+\b\d+\s*=\s*\d+)|(';)|('\s+(OR|AND|UNION|SELECT))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(500));

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

        // Apply security sanitization
        sanitized = ApplySecuritySanitization(sanitized);

        // Apply content formatting rules
        sanitized = ApplyContentFormatting(sanitized);

        // Apply length constraints
        sanitized = ApplyLengthConstraints(sanitized);

        return sanitized.Trim();
    }

    /// <summary>
    /// Applies security-focused sanitization to remove dangerous content
    /// </summary>
    private string ApplySecuritySanitization(string input)
    {
        var sanitized = input;

        // Remove script tags and JavaScript protocols
        sanitized = RemoveScriptingThreats(sanitized);

        // Remove SQL injection patterns
        sanitized = RemoveSqlInjectionPatterns(sanitized);

        // Remove other dangerous patterns
        sanitized = RemoveDangerousPatterns(sanitized);

        // Handle HTML content based on settings
        sanitized = ProcessHtmlContent(sanitized);

        return sanitized;
    }

    /// <summary>
    /// Removes scripting threats like script tags and JavaScript protocols
    /// </summary>
    private string RemoveScriptingThreats(string input)
    {
        var sanitized = input;
        
        // Remove script tags completely
        sanitized = ScriptTagPattern.Replace(sanitized, string.Empty);

        // Remove javascript: and similar dangerous protocols
        sanitized = JavascriptPattern.Replace(sanitized, string.Empty);

        return sanitized;
    }

    /// <summary>
    /// Removes SQL injection attack patterns
    /// </summary>
    private string RemoveSqlInjectionPatterns(string input)
    {
        // For musical application, we're strict about SQL-like patterns
        return SqlInjectionPattern.IsMatch(input) 
            ? SqlInjectionPattern.Replace(input, "***") 
            : input;
    }

    /// <summary>
    /// Removes other dangerous patterns from the input
    /// </summary>
    private string RemoveDangerousPatterns(string input)
    {
        return DangerousPatterns.Aggregate(input, (current, pattern) => 
            current.Replace(pattern, string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Processes HTML content based on AllowHtml setting
    /// </summary>
    private string ProcessHtmlContent(string input)
    {
        return !AllowHtml && ContainsHtml(input) 
            ? HttpUtility.HtmlEncode(input) 
            : input;
    }

    /// <summary>
    /// Applies content formatting rules for line breaks and special characters
    /// </summary>
    private string ApplyContentFormatting(string input)
    {
        var sanitized = input;

        // Handle line breaks
        sanitized = ProcessLineBreaks(sanitized);

        // Handle special characters for musical content
        sanitized = ProcessSpecialCharacters(sanitized);

        return sanitized;
    }

    /// <summary>
    /// Processes line breaks based on AllowLineBreaks setting
    /// </summary>
    private string ProcessLineBreaks(string input)
    {
        return !AllowLineBreaks 
            ? input.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ")
            : input;
    }

    /// <summary>
    /// Processes special characters, keeping musical notation while removing others
    /// </summary>
    private string ProcessSpecialCharacters(string input)
    {
        return !AllowSpecialCharacters
            ? Regex.Replace(input, @"[^\w\s\-#♭♯°øØ\(\)\[\]\.\,\;\:\!\?\'\""""]", string.Empty,
                RegexOptions.None, TimeSpan.FromMilliseconds(100))
            : input;
    }

    /// <summary>
    /// Applies length constraints based on MaxLength setting
    /// </summary>
    private string ApplyLengthConstraints(string input)
    {
        return MaxLength > 0 && input.Length > MaxLength 
            ? input.Substring(0, MaxLength).Trim()
            : input;
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