using System.ComponentModel.DataAnnotations;

namespace SetlistStudio.Core.Validation;

/// <summary>
/// Validates that a BPM (Beats Per Minute) value is within realistic musical ranges
/// Supports typical musical tempos from 40 BPM (very slow ballads) to 250 BPM (very fast genres)
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class BpmRangeAttribute : ValidationAttribute
{
    public int MinimumBpm { get; }
    public int MaximumBpm { get; }

    /// <summary>
    /// Creates a BPM validation attribute with default range (40-250 BPM)
    /// </summary>
    public BpmRangeAttribute() : this(40, 250)
    {
    }

    /// <summary>
    /// Creates a BPM validation attribute with custom range
    /// </summary>
    /// <param name="minimumBpm">Minimum valid BPM value</param>
    /// <param name="maximumBpm">Maximum valid BPM value</param>
    public BpmRangeAttribute(int minimumBpm, int maximumBpm)
    {
        MinimumBpm = minimumBpm;
        MaximumBpm = maximumBpm;
        
        if (minimumBpm >= maximumBpm)
        {
            throw new ArgumentException("Minimum BPM must be less than maximum BPM", nameof(minimumBpm));
        }
        
        ErrorMessage = $"BPM must be between {MinimumBpm} and {MaximumBpm} (typical musical range)";
    }

    public override bool IsValid(object? value)
    {
        // Null values are considered valid (use [Required] for mandatory validation)
        if (value == null)
        {
            return true;
        }

        // Handle different numeric types
        if (!TryConvertToInt(value, out int bpmValue))
        {
            return false;
        }

        return bpmValue >= MinimumBpm && bpmValue <= MaximumBpm;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be between {MinimumBpm} and {MaximumBpm} BPM. " +
               GetBpmGuidance(MinimumBpm, MaximumBpm);
    }

    private static bool TryConvertToInt(object value, out int result)
    {
        result = 0;
        
        // Handle direct integer
        if (value is int intValue)
        {
            result = intValue;
            return true;
        }

        // Handle string conversion
        if (value is string stringValue)
        {
            return int.TryParse(stringValue.Trim(), out result);
        }

        // Handle numeric types
        return TryConvertNumericToInt(value, out result);
    }

    private static bool TryConvertNumericToInt(object value, out int result)
    {
        result = 0;

        return value switch
        {
            long longValue => TryConvertLong(longValue, out result),
            float floatValue => TryConvertFloat(floatValue, out result),
            double doubleValue => TryConvertDouble(doubleValue, out result),
            decimal decimalValue => TryConvertDecimal(decimalValue, out result),
            _ => false
        };
    }

    private static bool TryConvertLong(long value, out int result)
    {
        result = 0;
        if (value >= int.MinValue && value <= int.MaxValue)
        {
            result = (int)value;
            return true;
        }
        return false;
    }

    private static bool TryConvertFloat(float value, out int result)
    {
        result = 0;
        if (float.IsFinite(value) && value >= int.MinValue && value <= int.MaxValue)
        {
            result = (int)Math.Round(value);
            return true;
        }
        return false;
    }

    private static bool TryConvertDouble(double value, out int result)
    {
        result = 0;
        if (double.IsFinite(value) && value >= int.MinValue && value <= int.MaxValue)
        {
            result = (int)Math.Round(value);
            return true;
        }
        return false;
    }

    private static bool TryConvertDecimal(decimal value, out int result)
    {
        result = 0;
        if (value >= int.MinValue && value <= int.MaxValue)
        {
            result = (int)Math.Round(value);
            return true;
        }
        return false;
    }

    private static string GetBpmGuidance(int min, int max)
    {
        return (min, max) switch
        {
            (40, 250) => "(40-60: Very slow ballads, 60-80: Ballads, 90-120: Medium tempo, 130-160: Up-tempo, 170+: Fast songs)",
            (60, 180) => "(60-80: Ballads, 90-120: Medium tempo, 130-160: Up-tempo, 170-180: Fast)",
            (80, 140) => "(80-100: Slow to medium, 100-120: Medium, 120-140: Up-tempo)",
            _ => $"(Valid range: {min}-{max})"
        };
    }

    /// <summary>
    /// Categorizes a BPM value into a tempo description
    /// </summary>
    public static string GetTempoDescription(int bpm)
    {
        return bpm switch
        {
            < 60 => "Very Slow",
            < 80 => "Slow",
            < 100 => "Moderate",
            < 120 => "Medium",
            < 140 => "Up-tempo",
            < 160 => "Fast",
            < 180 => "Very Fast",
            _ => "Extremely Fast"
        };
    }

    /// <summary>
    /// Gets the typical BPM range for a given genre
    /// </summary>
    public static (int Min, int Max) GetGenreRange(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            return DefaultRange;
        }

        var normalizedGenre = genre.ToLower();
        
        // Check common genre categories to reduce complexity
        if (IsSlowGenre(normalizedGenre))
        {
            return GetSlowGenreRange(normalizedGenre);
        }
        
        if (IsMediumGenre(normalizedGenre))
        {
            return GetMediumGenreRange(normalizedGenre);
        }
        
        if (IsElectronicGenre(normalizedGenre))
        {
            return GetElectronicGenreRange(normalizedGenre);
        }
        
        if (IsFastGenre(normalizedGenre))
        {
            return GetFastGenreRange(normalizedGenre);
        }

        return DefaultRange;
    }

    private static readonly (int Min, int Max) DefaultRange = (40, 250);

    private static bool IsSlowGenre(string genre) =>
        genre is "ballad" or "slow" or "reggae";

    private static bool IsMediumGenre(string genre) =>
        genre is "blues" or "jazz" or "rock" or "pop" or "funk" or "country";

    private static bool IsElectronicGenre(string genre) =>
        genre is "electronic" or "techno" or "house" or "trance";

    private static bool IsFastGenre(string genre) =>
        genre is "drum and bass" or "dubstep";

    private static (int Min, int Max) GetSlowGenreRange(string genre) =>
        genre switch
        {
            "ballad" or "slow" => (60, 80),
            "reggae" => (60, 90),
            _ => DefaultRange
        };

    private static (int Min, int Max) GetMediumGenreRange(string genre) =>
        genre switch
        {
            "blues" => (80, 120),
            "jazz" => (90, 200),
            "rock" => (110, 140),
            "pop" => (100, 130),
            "funk" => (90, 120),
            "country" => (80, 140),
            _ => DefaultRange
        };

    private static (int Min, int Max) GetElectronicGenreRange(string genre) =>
        genre switch
        {
            "electronic" or "techno" => (120, 140),
            "house" => (115, 130),
            "trance" => (130, 140),
            _ => DefaultRange
        };

    private static (int Min, int Max) GetFastGenreRange(string genre) =>
        genre switch
        {
            "drum and bass" => (160, 180),
            "dubstep" => (140, 150),
            _ => DefaultRange
        };
}