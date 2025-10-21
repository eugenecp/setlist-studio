using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SetlistStudio.Core.Validation;

/// <summary>
/// Validates that a string represents a valid musical key
/// Supports major keys (C, C#, Db, D, D#, Eb, E, F, F#, Gb, G, G#, Ab, A, A#, Bb, B)
/// and minor keys (Cm, C#m, Dbm, Dm, D#m, Ebm, Em, Fm, F#m, Gbm, Gm, G#m, Abm, Am, A#m, Bbm, Bm)
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MusicalKeyAttribute : ValidationAttribute
{
    private static readonly string[] ValidKeys = 
    {
        // Major keys
        "C", "C#", "Db", "D", "D#", "Eb", "E", "F", "F#", "Gb", "G", "G#", "Ab", "A", "A#", "Bb", "B",
        
        // Minor keys  
        "Cm", "C#m", "Dbm", "Dm", "D#m", "Ebm", "Em", "Fm", "F#m", "Gbm", "Gm", "G#m", "Abm", "Am", "A#m", "Bbm", "Bm",
        
        // Alternative notations (some musicians prefer these)
        "c", "c#", "db", "d", "d#", "eb", "e", "f", "f#", "gb", "g", "g#", "ab", "a", "a#", "bb", "b",
        "cm", "c#m", "dbm", "dm", "d#m", "ebm", "em", "fm", "f#m", "gbm", "gm", "g#m", "abm", "am", "a#m", "bbm", "bm"
    };

    private static readonly Regex KeyPattern = new Regex(
        @"^[A-Ga-g][#b]?m?$", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    public MusicalKeyAttribute()
    {
        ErrorMessage = "Musical key must be a valid key signature (e.g., C, F#, Bb, Am, F#m)";
    }

    public override bool IsValid(object? value)
    {
        // Null values are considered valid (use [Required] for mandatory validation)
        if (value == null)
        {
            return true;
        }

        // Handle non-string values
        if (value is not string keyValue)
        {
            return false;
        }

        // Empty or whitespace-only strings are considered valid (use [Required] for mandatory validation)
        if (string.IsNullOrWhiteSpace(keyValue))
        {
            return true;
        }

        // Remove whitespace and validate format first
        keyValue = keyValue.Trim();
        
        if (!KeyPattern.IsMatch(keyValue))
        {
            return false;
        }

        // Check against valid keys list (case-insensitive)
        return ValidKeys.Contains(keyValue, StringComparer.OrdinalIgnoreCase);
    }



    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a valid musical key (examples: C, F#, Bb, Am, F#m)";
    }

    /// <summary>
    /// Gets all valid musical keys for validation reference
    /// </summary>
    public static IReadOnlyList<string> GetValidKeys()
    {
        return Array.AsReadOnly(ValidKeys);
    }

    /// <summary>
    /// Normalizes a musical key to standard notation
    /// </summary>
    public static string? NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return key;

        key = key.Trim();
        
        // Convert to standard capitalization
        if (key.Length >= 1)
        {
            // First character uppercase
            var normalized = char.ToUpper(key[0]).ToString();
            
            // Rest lowercase except 'm' which stays lowercase
            if (key.Length > 1)
            {
                normalized += key.Substring(1).ToLower();
            }
            
            return normalized;
        }
        
        return key;
    }
}