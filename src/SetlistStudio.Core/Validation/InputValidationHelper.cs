using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SetlistStudio.Core.Validation;

/// <summary>
/// Utility class for common input validation and sanitization operations
/// Provides reusable validation logic for the Setlist Studio application
/// </summary>
public static class InputValidationHelper
{
    private static readonly Regex EmailPattern = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex AlphanumericPattern = new Regex(
        @"^[a-zA-Z0-9\s\-_\.]+$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex MusicalNotationPattern = new Regex(
        @"^[A-Ga-g][#♯♭b]?m?(\/[A-Ga-g][#♯♭b]?)?(\s*\([^)]+\))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Validates an email address format
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return EmailPattern.IsMatch(email.Trim());
    }

    /// <summary>
    /// Validates that a string contains only safe alphanumeric characters
    /// </summary>
    public static bool IsAlphanumericSafe(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return true;

        return AlphanumericPattern.IsMatch(input);
    }

    /// <summary>
    /// Validates a song title for safety and musical appropriateness
    /// </summary>
    public static ValidationResult ValidateSongTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return new ValidationResult("Song title is required");
        }

        title = title.Trim();

        if (title.Length < 1 || title.Length > 200)
        {
            return new ValidationResult("Song title must be between 1 and 200 characters");
        }

        if (!SanitizedStringAttribute.IsSafeForMusicalContent(title))
        {
            return new ValidationResult("Song title contains potentially unsafe content");
        }

        return ValidationResult.Success!;
    }

    /// <summary>
    /// Validates an artist name for safety and musical appropriateness
    /// </summary>
    public static ValidationResult ValidateArtistName(string artist)
    {
        if (string.IsNullOrWhiteSpace(artist))
        {
            return new ValidationResult("Artist name is required");
        }

        artist = artist.Trim();

        if (artist.Length < 1 || artist.Length > 200)
        {
            return new ValidationResult("Artist name must be between 1 and 200 characters");
        }

        if (!SanitizedStringAttribute.IsSafeForMusicalContent(artist))
        {
            return new ValidationResult("Artist name contains potentially unsafe content");
        }

        return ValidationResult.Success!;
    }

    /// <summary>
    /// Validates a musical key with comprehensive rules
    /// </summary>
    public static ValidationResult ValidateMusicalKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return ValidationResult.Success!; // Optional field
        }

        key = key.Trim();

        if (!MusicalNotationPattern.IsMatch(key))
        {
            return new ValidationResult("Musical key format is invalid");
        }

        var keyAttribute = new MusicalKeyAttribute();
        if (!keyAttribute.IsValid(key))
        {
            return new ValidationResult(keyAttribute.ErrorMessage);
        }

        return ValidationResult.Success!;
    }

    /// <summary>
    /// Validates BPM value with musical context
    /// </summary>
    public static ValidationResult ValidateBpm(int? bpm)
    {
        if (bpm == null)
        {
            return ValidationResult.Success!; // Optional field
        }

        var bpmAttribute = new BpmRangeAttribute();
        if (!bpmAttribute.IsValid(bpm))
        {
            return new ValidationResult($"BPM must be between 40 and 250. Current value: {bpm}");
        }

        return ValidationResult.Success!;
    }

    /// <summary>
    /// Validates setlist name for safety and appropriateness
    /// </summary>
    public static ValidationResult ValidateSetlistName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new ValidationResult("Setlist name is required");
        }

        name = name.Trim();

        if (name.Length < 1 || name.Length > 200)
        {
            return new ValidationResult("Setlist name must be between 1 and 200 characters");
        }

        if (!SanitizedStringAttribute.IsSafeForMusicalContent(name))
        {
            return new ValidationResult("Setlist name contains potentially unsafe content");
        }

        return ValidationResult.Success!;
    }

    /// <summary>
    /// Validates user notes with appropriate length and content restrictions
    /// </summary>
    public static ValidationResult ValidateUserNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return ValidationResult.Success!; // Optional field
        }

        notes = notes.Trim();

        if (notes.Length > 2000)
        {
            return new ValidationResult("Notes must be 2000 characters or less");
        }

        if (!SanitizedStringAttribute.IsSafeForMusicalContent(notes))
        {
            return new ValidationResult("Notes contain potentially unsafe content");
        }

        return ValidationResult.Success!;
    }

    /// <summary>
    /// Validates song duration in seconds
    /// </summary>
    public static ValidationResult ValidateDuration(int? durationSeconds)
    {
        if (durationSeconds == null)
        {
            return ValidationResult.Success!; // Optional field
        }

        if (durationSeconds < 1 || durationSeconds > 3600)
        {
            return new ValidationResult("Song duration must be between 1 second and 1 hour (3600 seconds)");
        }

        return ValidationResult.Success!;
    }

    /// <summary>
    /// Validates difficulty rating
    /// </summary>
    public static ValidationResult ValidateDifficultyRating(int? rating)
    {
        if (rating == null)
        {
            return ValidationResult.Success!; // Optional field
        }

        if (rating < 1 || rating > 5)
        {
            return new ValidationResult("Difficulty rating must be between 1 and 5");
        }

        return ValidationResult.Success!;
    }

    /// <summary>
    /// Sanitizes input and returns cleaned value
    /// </summary>
    public static string SanitizeInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return SanitizedStringAttribute.SanitizeMusicalContent(input);
    }

    /// <summary>
    /// Performs comprehensive validation on a song entity
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateSong(string title, string artist, string? musicalKey, int? bpm, int? durationSeconds, int? difficultyRating, string? notes)
    {
        var results = new List<ValidationResult>();

        var titleResult = ValidateSongTitle(title);
        if (titleResult != ValidationResult.Success)
            results.Add(titleResult);

        var artistResult = ValidateArtistName(artist);
        if (artistResult != ValidationResult.Success)
            results.Add(artistResult);

        var keyResult = ValidateMusicalKey(musicalKey);
        if (keyResult != ValidationResult.Success)
            results.Add(keyResult);

        var bpmResult = ValidateBpm(bpm);
        if (bpmResult != ValidationResult.Success)
            results.Add(bpmResult);

        var durationResult = ValidateDuration(durationSeconds);
        if (durationResult != ValidationResult.Success)
            results.Add(durationResult);

        var difficultyResult = ValidateDifficultyRating(difficultyRating);
        if (difficultyResult != ValidationResult.Success)
            results.Add(difficultyResult);

        var notesResult = ValidateUserNotes(notes);
        if (notesResult != ValidationResult.Success)
            results.Add(notesResult);

        return results;
    }

    /// <summary>
    /// Performs comprehensive validation on a setlist entity
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateSetlist(string name, string? description, string? venue)
    {
        var results = new List<ValidationResult>();

        var nameResult = ValidateSetlistName(name);
        if (nameResult != ValidationResult.Success)
            results.Add(nameResult);

        if (!string.IsNullOrWhiteSpace(description))
        {
            var descResult = ValidateUserNotes(description);
            if (descResult != ValidationResult.Success)
                results.Add(descResult);
        }

        if (!string.IsNullOrWhiteSpace(venue))
        {
            var venueResult = ValidateSetlistName(venue); // Same rules as setlist name
            if (venueResult != ValidationResult.Success)
                results.Add(venueResult);
        }

        return results;
    }
}