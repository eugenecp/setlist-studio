using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SetlistStudio.Core.Security
{
    /// <summary>
    /// Validates that string properties contain only safe content for logging and storage.
    /// This attribute prevents log injection, XSS attacks, and SQL injection at the model level.
    /// Addresses CWE-117 (Log Injection), CWE-79 (XSS), and CWE-89 (SQL Injection).
    /// </summary>
    public class SafeStringAttribute : ValidationAttribute
    {
        public bool AllowEmpty { get; set; } = true;
        public int MaxLength { get; set; } = 1000;
        public bool AllowMusicalKeys { get; set; } = false;
        public bool AllowNewlines { get; set; } = false;
        public bool AllowSpecialCharacters { get; set; } = false;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (!(value is string stringValue))
            {
                return new ValidationResult("Value must be a string");
            }

            if (string.IsNullOrEmpty(stringValue))
            {
                return AllowEmpty ? ValidationResult.Success : 
                    new ValidationResult("Value cannot be empty");
            }

            if (stringValue.Length > MaxLength)
            {
                return new ValidationResult($"Value cannot exceed {MaxLength} characters");
            }

            // Check for malicious patterns
            if (ContainsMaliciousContent(stringValue))
            {
                return new ValidationResult("Value contains potentially malicious content");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// Checks if the input contains potentially malicious content.
        /// </summary>
        /// <param name="input">The input to check</param>
        /// <returns>True if malicious content is detected</returns>
        private bool ContainsMaliciousContent(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // XSS patterns
            var xssPatterns = new[]
            {
                @"<script[^>]*>.*?</script>",
                @"javascript:",
                @"vbscript:",
                @"onload\s*=",
                @"onerror\s*=",
                @"onclick\s*=",
                @"onmouseover\s*=",
                @"<iframe[^>]*>",
                @"<object[^>]*>",
                @"<embed[^>]*>",
                @"<form[^>]*>",
                @"<input[^>]*>"
            };

            // SQL injection patterns  
            var sqlPatterns = new[]
            {
                @"';\s*(drop|delete|insert|update|select|union|exec|execute)",
                @"union\s+select",
                @"exec\s*\(",
                @"sp_executesql",
                @"xp_cmdshell",
                @"--\s*$",
                @"/\*.*?\*/"
            };

            // Log injection patterns
            var logPatterns = new List<string>
            {
                @"[0-9]{4}-[0-9]{2}-[0-9]{2}.*?(ERROR|WARN|INFO|DEBUG|FATAL)",
                @"\x00|\x01|\x02|\x03|\x04|\x05|\x06|\x07|\x08|\x0B|\x0C|\x0E|\x0F",
                @"\x10|\x11|\x12|\x13|\x14|\x15|\x16|\x17|\x18|\x19|\x1A|\x1B|\x1C|\x1D|\x1E|\x1F"
            };

            // Only add newline restriction if not explicitly allowed
            if (!AllowNewlines)
            {
                logPatterns.Add(@"\r\n|\n\r|\n|\r");
            }

            // Command injection patterns - be more selective for musical content
            var commandPatterns = new[]
            {
                @"[;`$(){}\[\]\\]", // Removed & and | as they're common in music names
                @"\.\.[\\/]",
                @"(cmd|powershell|bash|sh|zsh|fish)(\.|\.exe)?\s"
            };

            var allPatterns = xssPatterns.Concat(sqlPatterns).Concat(logPatterns);

            // Don't apply command injection patterns if we're allowing special characters
            // because musical content can contain characters like & ' ! - 
            if (!AllowSpecialCharacters && !AllowMusicalKeys)
            {
                allPatterns = allPatterns.Concat(commandPatterns);
            }

            return allPatterns.Any(pattern => 
                Regex.IsMatch(input, pattern, 
                    RegexOptions.IgnoreCase | RegexOptions.Singleline));
        }
    }

    /// <summary>
    /// Validates that a string represents a valid musical key.
    /// Allows standard musical notation while preventing malicious content.
    /// </summary>
    public class MusicalKeyAttribute : ValidationAttribute
    {
        private static readonly string[] ValidKeys = 
        {
            "C", "C#", "Db", "D", "D#", "Eb", "E", "F", "F#", "Gb", "G", "G#", "Ab", "A", "A#", "Bb", "B",
            "Cm", "C#m", "Dbm", "Dm", "D#m", "Ebm", "Em", "Fm", "F#m", "Gbm", "Gm", "G#m", "Abm", "Am", "A#m", "Bbm", "Bm"
        };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value as string))
            {
                return ValidationResult.Success; // Allow null/empty
            }

            var keyValue = (string)value;

            if (ValidKeys.Contains(keyValue))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult($"'{keyValue}' is not a valid musical key. Valid keys are: {string.Join(", ", ValidKeys)}");
        }
    }

    /// <summary>
    /// Validates that a numeric value is within safe BPM ranges for music.
    /// Prevents unrealistic values that could cause issues.
    /// </summary>
    public class SafeBpmAttribute : ValidationAttribute
    {
        public int MinBpm { get; set; } = 40;
        public int MaxBpm { get; set; } = 250;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success; // Allow null
            }

            if (!(value is int bpmValue))
            {
                return new ValidationResult("BPM must be a number");
            }

            if (bpmValue < MinBpm || bpmValue > MaxBpm)
            {
                return new ValidationResult($"BPM must be between {MinBpm} and {MaxBpm}");
            }

            return ValidationResult.Success;
        }
    }
}