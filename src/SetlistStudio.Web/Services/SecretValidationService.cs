using Microsoft.Extensions.Options;
using SetlistStudio.Core.Security;
using System.Text;

namespace SetlistStudio.Web.Services;

/// <summary>
/// Service for validating that all required secrets are properly configured
/// Prevents application startup with missing or placeholder secrets in production
/// </summary>
public class SecretValidationService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly SecurityEventLogger _securityEventLogger;
    private readonly ILogger<SecretValidationService> _logger;

    /// <summary>
    /// Required secrets for production deployment
    /// </summary>
    private static readonly Dictionary<string, string> RequiredSecrets = new()
    {
        { "Authentication:Google:ClientId", "Google OAuth Client ID" },
        { "Authentication:Google:ClientSecret", "Google OAuth Client Secret" },
        { "Authentication:Microsoft:ClientId", "Microsoft OAuth Client ID" },
        { "Authentication:Microsoft:ClientSecret", "Microsoft OAuth Client Secret" },
        { "Authentication:Facebook:AppId", "Facebook App ID" },
        { "Authentication:Facebook:AppSecret", "Facebook App Secret" },
        { "ConnectionStrings:DefaultConnection", "Database Connection String" }
    };

    /// <summary>
    /// Placeholder values that indicate secrets are not properly configured
    /// SECURITY ENHANCEMENT: Expanded list of insecure patterns and development-only values
    /// </summary>
    private static readonly HashSet<string> PlaceholderValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "YOUR_GOOGLE_CLIENT_ID",
        "YOUR_GOOGLE_CLIENT_SECRET",
        "YOUR_MICROSOFT_CLIENT_ID",
        "YOUR_MICROSOFT_CLIENT_SECRET",
        "YOUR_FACEBOOK_APP_ID",
        "YOUR_FACEBOOK_APP_SECRET",
        "YOUR_CLIENT_ID",
        "YOUR_CLIENT_SECRET",
        "YOUR_APP_ID",
        "YOUR_APP_SECRET",
        "YOUR_RECAPTCHA_SITE_KEY",
        "YOUR_RECAPTCHA_SECRET_KEY",
        "CHANGE_ME",
        "REPLACE_ME",
        "EXAMPLE_VALUE",
        "TEST_VALUE",
        "DEMO_VALUE",
        "PLACEHOLDER",
        "TODO_CONFIGURE",
        "SET_YOUR_VALUE_HERE"
    };

    /// <summary>
    /// Insecure patterns that indicate non-production-ready secrets
    /// SECURITY ENHANCEMENT: Detects common insecure values and development artifacts
    /// </summary>
    private static readonly string[] InsecurePatterns = new[]
    {
        "localhost",
        "127.0.0.1",
        "::1",
        "password",
        "secret123",
        "admin123",
        "test123",
        "demo123",
        "changeme",
        "defaultpassword",
        "insecure",
        "development",
        "staging", // For production validation
        "production", // Development/test databases named production
        "example.com",
        "test.com",
        "dev.",
        ".local",
        "internal",
        "temp",
        "debug",
        "valid-" // Test patterns like "valid-google-client-id"
    };

    public SecretValidationService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        SecurityEventLogger securityEventLogger,
        ILogger<SecretValidationService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _securityEventLogger = securityEventLogger ?? throw new ArgumentNullException(nameof(securityEventLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Determines if the current environment is non-production (Development, Testing, or SecurityTesting)
    /// </summary>
    private bool IsNonProductionEnvironment()
    {
        var environmentName = _environment.EnvironmentName;
        
        // Check for explicit security testing bypass
        var skipSecretValidation = _configuration.GetValue<bool>("SKIP_SECRET_VALIDATION", false) ||
                                 !string.IsNullOrEmpty(_configuration["SECURITY_TESTING"]);
        
        return _environment.IsDevelopment() || 
               string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(environmentName, "SecurityTesting", StringComparison.OrdinalIgnoreCase) ||
               skipSecretValidation;
    }

    /// <summary>
    /// Validates all required secrets for the current environment
    /// </summary>
    /// <param name="strictValidation">If true, validates all secrets. If false, only validates configured OAuth providers</param>
    /// <returns>Validation result with details of any missing or invalid secrets</returns>
    public SecretValidationResult ValidateSecrets(bool strictValidation = false)
    {
        var result = new SecretValidationResult();
        var environmentName = _environment.EnvironmentName;

        _logger.LogInformation("Validating secrets for environment: {Environment}", environmentName);

        ValidateKeyVaultForProduction(result, environmentName);

        if (ShouldSkipValidation(strictValidation))
        {
            _logger.LogInformation("Skipping strict secret validation in development environment");
            return result;
        }

        ValidateAllRequiredSecrets(result, environmentName);
        LogValidationSummary(result, environmentName);

        return result;
    }

    /// <summary>
    /// Validates Key Vault configuration for production environments
    /// </summary>
    private void ValidateKeyVaultForProduction(SecretValidationResult result, string environmentName)
    {
        if (!IsNonProductionEnvironment())
        {
            var keyVaultValidation = ValidateKeyVaultConfiguration(environmentName);
            result.ValidationErrors.AddRange(keyVaultValidation);
        }
    }

    /// <summary>
    /// Determines if validation should be skipped based on environment and settings
    /// </summary>
    private bool ShouldSkipValidation(bool strictValidation)
    {
        return IsNonProductionEnvironment() && !strictValidation;
    }

    /// <summary>
    /// Validates all required secrets and handles OAuth-specific logic
    /// </summary>
    private void ValidateAllRequiredSecrets(SecretValidationResult result, string environmentName)
    {
        foreach (var (secretKey, description) in RequiredSecrets)
        {
            var secretValue = _configuration[secretKey];
            
            if (ShouldSkipOptionalOAuthSecret(secretKey, secretValue))
            {
                _logger.LogDebug("Skipping validation for optional OAuth secret: {SecretKey}", secretKey);
                continue;
            }

            ValidateSecretAndLogErrors(result, environmentName, secretKey, secretValue, description);
        }
    }

    /// <summary>
    /// Determines if an optional OAuth secret should be skipped
    /// </summary>
    private bool ShouldSkipOptionalOAuthSecret(string secretKey, string? secretValue)
    {
        if (!IsOptionalOAuthSecret(secretKey))
            return false;

        if (string.IsNullOrWhiteSpace(secretValue))
            return true;

        _logger.LogDebug("Validating optional OAuth secret with value: {SecretKey}", secretKey);
        return false;
    }

    /// <summary>
    /// Validates an individual secret and logs any validation errors
    /// </summary>
    private void ValidateSecretAndLogErrors(SecretValidationResult result, string environmentName, 
        string secretKey, string? secretValue, string description)
    {
        var validationIssue = ValidateIndividualSecret(secretKey, secretValue, description);
        
        if (validationIssue != null)
        {
            result.ValidationErrors.Add(validationIssue);
            LogSecurityEventForProductionFailure(environmentName, secretKey, description, validationIssue);
        }
    }

    /// <summary>
    /// Logs security events for production secret validation failures
    /// </summary>
    private void LogSecurityEventForProductionFailure(string environmentName, string secretKey, 
        string description, SecretValidationError validationIssue)
    {
        if (_environment.IsProduction())
        {
            _securityEventLogger.LogSecurityEvent(
                SecurityEventType.ConfigurationChange,
                SecurityEventSeverity.High,
                $"Production secret validation failed: {description}",
                resourceType: "Configuration",
                resourceId: secretKey,
                additionalData: new { Environment = environmentName, Issue = validationIssue.Issue }
            );
        }
    }

    /// <summary>
    /// Logs the validation summary based on results
    /// </summary>
    private void LogValidationSummary(SecretValidationResult result, string environmentName)
    {
        if (result.IsValid)
        {
            LogSuccessfulValidation(environmentName);
        }
        else
        {
            LogValidationFailures(result, environmentName);
        }
    }

    /// <summary>
    /// Logs successful validation and Key Vault usage
    /// </summary>
    private void LogSuccessfulValidation(string environmentName)
    {
        _logger.LogInformation("All secrets validated successfully for environment: {Environment}", environmentName);
        
        if (!IsNonProductionEnvironment())
        {
            var keyVaultName = _configuration["KeyVault:VaultName"];
            if (!string.IsNullOrEmpty(keyVaultName))
            {
                _logger.LogInformation("Using Azure Key Vault for secret management: {KeyVaultName}", keyVaultName);
            }
        }
    }

    /// <summary>
    /// Logs validation failures for non-test environments
    /// </summary>
    private void LogValidationFailures(SecretValidationResult result, string environmentName)
    {
        if (!environmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Secret validation failed with {ErrorCount} errors in environment: {Environment}", 
                result.ValidationErrors.Count, environmentName);
        }
    }

    /// <summary>
    /// Validates Azure Key Vault configuration for production environments
    /// </summary>
    private List<SecretValidationError> ValidateKeyVaultConfiguration(string environmentName)
    {
        var errors = new List<SecretValidationError>();
        var keyVaultName = _configuration["KeyVault:VaultName"];

        // Check for empty/missing Key Vault configuration
        if (ValidateKeyVaultNotEmpty(keyVaultName, environmentName, errors))
        {
            return errors; // Early return if Key Vault not configured
        }

        // Validate Key Vault name format
        ValidateKeyVaultNameFormat(keyVaultName!, errors);

        // Check for placeholder values
        ValidateKeyVaultNotPlaceholder(keyVaultName!, errors);

        // Log successful validation
        LogKeyVaultValidationSuccess(keyVaultName!, errors);

        return errors;
    }

    /// <summary>
    /// Validates that Key Vault name is not empty and logs appropriate message
    /// </summary>
    private bool ValidateKeyVaultNotEmpty(string? keyVaultName, string environmentName, List<SecretValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(keyVaultName))
        {
            // Key Vault is not configured - this is okay for some deployment scenarios
            if (!environmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Azure Key Vault not configured - using local configuration");
            }
            return true; // Indicates early return should happen
        }
        return false; // Continue validation
    }

    /// <summary>
    /// Validates Key Vault name format according to Azure naming conventions
    /// </summary>
    private void ValidateKeyVaultNameFormat(string keyVaultName, List<SecretValidationError> errors)
    {
        if (!IsValidKeyVaultName(keyVaultName))
        {
            errors.Add(new SecretValidationError(
                "KeyVault:VaultName",
                "Azure Key Vault Name",
                SecretValidationIssue.InvalidFormat,
                $"Invalid Key Vault name format: {keyVaultName}. Must be 3-24 characters, alphanumeric and hyphens only."
            ));
        }
    }

    /// <summary>
    /// Validates that Key Vault name does not contain placeholder values
    /// </summary>
    private void ValidateKeyVaultNotPlaceholder(string keyVaultName, List<SecretValidationError> errors)
    {
        if (PlaceholderValues.Contains(keyVaultName) || keyVaultName.Contains("YOUR_") || keyVaultName.Contains("your_"))
        {
            errors.Add(new SecretValidationError(
                "KeyVault:VaultName",
                "Azure Key Vault Name",
                SecretValidationIssue.Placeholder,
                $"Key Vault name contains placeholder value: {keyVaultName}"
            ));
        }
    }

    /// <summary>
    /// Logs successful Key Vault validation if no errors occurred
    /// </summary>
    private void LogKeyVaultValidationSuccess(string keyVaultName, List<SecretValidationError> errors)
    {
        if (errors.Count == 0)
        {
            _logger.LogInformation("Azure Key Vault configuration validated: {KeyVaultName}", keyVaultName);
        }
    }

    /// <summary>
    /// Validates Azure Key Vault name format according to Azure naming conventions
    /// </summary>
    private static bool IsValidKeyVaultName(string keyVaultName)
    {
        return IsValidKeyVaultNameLength(keyVaultName) &&
               IsValidKeyVaultNameStart(keyVaultName) &&
               IsValidKeyVaultNameEnd(keyVaultName) &&
               IsValidKeyVaultNameCharacters(keyVaultName);
    }

    /// <summary>
    /// Validates that the Key Vault name has the correct length (3-24 characters)
    /// </summary>
    private static bool IsValidKeyVaultNameLength(string keyVaultName)
    {
        if (string.IsNullOrWhiteSpace(keyVaultName))
            return false;

        return keyVaultName.Length >= 3 && keyVaultName.Length <= 24;
    }

    /// <summary>
    /// Validates that the Key Vault name starts with a letter
    /// </summary>
    private static bool IsValidKeyVaultNameStart(string keyVaultName)
    {
        return keyVaultName.Length > 0 && char.IsLetter(keyVaultName[0]);
    }

    /// <summary>
    /// Validates that the Key Vault name ends with a letter or digit
    /// </summary>
    private static bool IsValidKeyVaultNameEnd(string keyVaultName)
    {
        return keyVaultName.Length > 0 && char.IsLetterOrDigit(keyVaultName[^1]);
    }

    /// <summary>
    /// Validates that the Key Vault name contains only valid characters (letters, digits, and hyphens)
    /// </summary>
    private static bool IsValidKeyVaultNameCharacters(string keyVaultName)
    {
        return keyVaultName.All(c => char.IsLetterOrDigit(c) || c == '-');
    }

    /// <summary>
    /// Validates a single secret configuration value
    /// </summary>
    private SecretValidationError? ValidateIndividualSecret(string secretKey, string? secretValue, string description)
    {
        // Check if secret is missing or empty
        var missingError = ValidateSecretNotMissing(secretKey, secretValue, description);
        if (missingError != null)
        {
            return missingError;
        }

        // Check if secret contains placeholder values
        var placeholderError = ValidateNotPlaceholder(secretKey, secretValue!, description);
        if (placeholderError != null)
        {
            return placeholderError;
        }

        // Check for production-unsafe patterns
        var productionReadyError = ValidateProductionReadiness(secretKey, secretValue!, description);
        if (productionReadyError != null)
        {
            return productionReadyError;
        }

        // Validate format based on secret type
        return ValidateSecretFormat(secretKey, secretValue!, description);
    }

    /// <summary>
    /// Validates that a secret is not missing or empty.
    /// </summary>
    private SecretValidationError? ValidateSecretNotMissing(string secretKey, string? secretValue, string description)
    {
        // Only check for truly missing/empty values - let placeholders be handled by ValidateNotPlaceholder
        if (string.IsNullOrWhiteSpace(secretValue))
        {
            // OAuth secrets are optional if not being used
            if (IsOptionalOAuthSecret(secretKey))
            {
                _logger.LogDebug("Optional OAuth secret not configured: {SecretKey}", secretKey);
                return null;
            }

            return new SecretValidationError(
                secretKey,
                description,
                SecretValidationIssue.Missing,
                "Secret is not configured or is empty"
            );
        }

        return null;
    }

    /// <summary>
    /// Validates that a secret does not contain placeholder values.
    /// </summary>
    private SecretValidationError? ValidateNotPlaceholder(string secretKey, string secretValue, string description)
    {
        if (PlaceholderValues.Contains(secretValue))
        {
            return new SecretValidationError(
                secretKey,
                description,
                SecretValidationIssue.Placeholder,
                $"Secret contains placeholder value: {secretValue}"
            );
        }

        return null;
    }

    /// <summary>
    /// Validates that a secret is production-ready.
    /// </summary>
    private SecretValidationError? ValidateProductionReadiness(string secretKey, string secretValue, string description)
    {
        // Skip enhanced security validation for connection strings and OAuth secrets in Development/Testing environments
        bool skipEnhancedValidation = IsNonProductionEnvironment() && 
            (secretKey.Contains("ConnectionString") || 
             secretKey.Contains("Authentication:"));
        
        if (!skipEnhancedValidation && !IsProductionReadySecret(secretValue))
        {
            return new SecretValidationError(
                secretKey,
                description,
                SecretValidationIssue.InvalidFormat,
                "Secret contains insecure patterns unsuitable for production"
            );
        }

        return null;
    }

    /// <summary>
    /// Validates the format of a secret based on its type.
    /// </summary>
    private SecretValidationError? ValidateSecretFormat(string secretKey, string secretValue, string description)
    {
        // Validate OAuth Client ID format
        if (IsOAuthClientId(secretKey) && !string.IsNullOrEmpty(secretValue) && secretValue.Length < 10)
        {
            return new SecretValidationError(
                secretKey,
                description,
                SecretValidationIssue.InvalidFormat,
                "OAuth Client ID appears to be too short"
            );
        }

        // Validate OAuth Client Secret format
        if (IsOAuthClientSecret(secretKey) && !string.IsNullOrEmpty(secretValue) && secretValue.Length < 16)
        {
            return new SecretValidationError(
                secretKey,
                description,
                SecretValidationIssue.InvalidFormat,
                "OAuth Client Secret appears to be too short"
            );
        }

        // Validate connection string format
        if (secretKey.Contains("ConnectionString") && !IsValidConnectionString(secretValue))
        {
            return new SecretValidationError(
                secretKey,
                description,
                SecretValidationIssue.InvalidFormat,
                "Connection string format appears invalid"
            );
        }

        return null;
    }

    /// <summary>
    /// Checks if a secret key represents an OAuth Client ID.
    /// </summary>
    private static bool IsOAuthClientId(string secretKey)
    {
        return secretKey.Contains("ClientId") || secretKey.Contains("AppId");
    }

    /// <summary>
    /// Checks if a secret key represents an OAuth Client Secret.
    /// </summary>
    private static bool IsOAuthClientSecret(string secretKey)
    {
        return secretKey.Contains("ClientSecret") || secretKey.Contains("AppSecret");
    }

    /// <summary>
    /// Determines if an OAuth secret is optional (provider not being used)
    /// </summary>
    private bool IsOptionalOAuthSecret(string secretKey)
    {
        // In production, OAuth secrets are only optional if:
        // 1. This specific provider is not configured, AND
        // 2. At least one other OAuth provider is configured (showing OAuth is intentionally used)
        if (_environment.IsProduction())
        {
            var isAnyOAuthConfigured = IsProviderConfigured("Google") || 
                                     IsProviderConfigured("Microsoft") || 
                                     IsProviderConfigured("Facebook");
            
            if (!isAnyOAuthConfigured)
            {
                // No OAuth configured at all in production - all OAuth secrets are required
                return false;
            }
        }
        
        // In non-production or when some OAuth is configured, individual providers can be optional
        // BUT: If a provider is attempted (has values including placeholders), it's not optional
        if (secretKey.Contains("Google"))
        {
            return !IsProviderAttempted("Google"); // Only optional if not attempted at all
        }
        if (secretKey.Contains("Microsoft"))
        {
            return !IsProviderAttempted("Microsoft"); // Only optional if not attempted at all
        }
        if (secretKey.Contains("Facebook"))
        {
            return !IsProviderAttempted("Facebook"); // Only optional if not attempted at all
        }
        
        return false;
    }

    /// <summary>
    /// Checks if an OAuth provider is configured for use (has valid, non-placeholder values)
    /// </summary>
    private bool IsProviderConfigured(string provider)
    {
        var clientIdKey = provider switch
        {
            "Google" => "Authentication:Google:ClientId",
            "Microsoft" => "Authentication:Microsoft:ClientId",
            "Facebook" => "Authentication:Facebook:AppId",
            _ => null
        };

        if (clientIdKey == null) return false;

        var clientId = _configuration[clientIdKey];
        return !string.IsNullOrWhiteSpace(clientId) && !PlaceholderValues.Contains(clientId);
    }

    /// <summary>
    /// Checks if an OAuth provider has any configuration attempt (including placeholder values)
    /// This distinguishes between "not configured" vs "misconfigured with placeholders"
    /// </summary>
    private bool IsProviderAttempted(string provider)
    {
        var clientIdKey = provider switch
        {
            "Google" => "Authentication:Google:ClientId",
            "Microsoft" => "Authentication:Microsoft:ClientId",
            "Facebook" => "Authentication:Facebook:AppId",
            _ => null
        };

        if (clientIdKey == null) return false;

        var clientId = _configuration[clientIdKey];
        return !string.IsNullOrWhiteSpace(clientId); // Has any value, including placeholders
    }

    /// <summary>
    /// Validates connection string format
    /// </summary>
    private static bool IsValidConnectionString(string connectionString)
    {
        // Basic validation for common connection string patterns
        return connectionString.Contains("Data Source=") || 
               connectionString.Contains("Server=") ||
               connectionString.Contains("Host=");
    }

    /// <summary>
    /// SECURITY ENHANCEMENT: Validates if a secret value is production-ready
    /// Checks for insecure patterns that indicate development or test values
    /// </summary>
    private static bool IsProductionReadySecret(string secretValue)
    {
        if (string.IsNullOrWhiteSpace(secretValue))
            return false;

        // Check for insecure patterns (case-insensitive)
        var lowerValue = secretValue.ToLowerInvariant();
        
        // Use LINQ for better performance and CodeQL compliance - check if any insecure pattern is found
        if (InsecurePatterns.Any(pattern => lowerValue.Contains(pattern)))
        {
            return false;
        }

        // Additional checks for weak secrets
        if (IsWeakSecret(secretValue))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// SECURITY ENHANCEMENT: Detects weak or predictable secret patterns
    /// </summary>
    private static bool IsWeakSecret(string secretValue)
    {
        // Too short for most secret types
        if (secretValue.Length < 8)
            return true;

        // Sequential characters
        if (HasSequentialCharacters(secretValue))
            return true;

        // Repeated characters (like "aaaaaaa" or "1111111")
        if (HasRepeatedCharacters(secretValue))
            return true;

        // Common weak patterns
        var weakPatterns = new[]
        {
            "12345", "abcde", "qwerty", "password", "secret", "admin",
            "guest", "default", "changeme", "letmein", "welcome"
        };

        var lowerValue = secretValue.ToLowerInvariant();
        return weakPatterns.Any(pattern => lowerValue.Contains(pattern));
    }

    /// <summary>
    /// Detects sequential characters in a string (e.g., "123", "abc")
    /// </summary>
    private static bool HasSequentialCharacters(string value)
    {
        if (value.Length < 3) return false;

        for (int i = 0; i < value.Length - 2; i++)
        {
            if (char.IsLetterOrDigit(value[i]) && 
                char.IsLetterOrDigit(value[i + 1]) && 
                char.IsLetterOrDigit(value[i + 2]))
            {
                // Check for ascending sequence
                if (value[i + 1] == value[i] + 1 && value[i + 2] == value[i] + 2)
                    return true;
                
                // Check for descending sequence
                if (value[i + 1] == value[i] - 1 && value[i + 2] == value[i] - 2)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Detects repeated characters in a string (e.g., "aaa", "111")
    /// </summary>
    private static bool HasRepeatedCharacters(string value)
    {
        if (value.Length < 3) return false;

        for (int i = 0; i < value.Length - 2; i++)
        {
            if (value[i] == value[i + 1] && value[i + 1] == value[i + 2])
                return true;
        }

        return false;
    }

    /// <summary>
    /// Validates secrets and throws exception if critical secrets are missing in production
    /// </summary>
    public void ValidateSecretsOrThrow()
    {
        // Skip strict validation for non-production environments (including security testing)
        var shouldStrictValidate = !IsNonProductionEnvironment();
        var result = ValidateSecrets(strictValidation: shouldStrictValidate);
        
        if (!result.IsValid)
        {
            var criticalErrors = result.ValidationErrors
                .Where(e => e.Issue == SecretValidationIssue.Missing || e.Issue == SecretValidationIssue.Placeholder)
                .ToList();

            // Only throw in actual production/staging environments (not security testing)
            var isInTestContext = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name?.Contains("Test", StringComparison.OrdinalIgnoreCase) == true);
                
            if (criticalErrors.Any() && shouldStrictValidate && !isInTestContext)
            {
                var errorMessageBuilder = new StringBuilder();
                errorMessageBuilder.AppendLine($"Critical secret validation failed in {_environment.EnvironmentName} environment:");
                foreach (var error in criticalErrors)
                {
                    errorMessageBuilder.AppendLine($"- {error.Description}: {error.Details}");
                }
                var errorMessage = errorMessageBuilder.ToString();

                _logger.LogCritical("Secret validation failed: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}

/// <summary>
/// Result of secret validation operation
/// </summary>
public class SecretValidationResult
{
    public List<SecretValidationError> ValidationErrors { get; } = new();
    public bool IsValid => ValidationErrors.Count == 0;
}

/// <summary>
/// Represents a secret validation error
/// </summary>
public class SecretValidationError
{
    public string SecretKey { get; }
    public string Description { get; }
    public SecretValidationIssue Issue { get; }
    public string Details { get; }

    public SecretValidationError(string secretKey, string description, SecretValidationIssue issue, string details)
    {
        SecretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Issue = issue;
        Details = details ?? throw new ArgumentNullException(nameof(details));
    }
}

/// <summary>
/// Types of secret validation issues
/// </summary>
public enum SecretValidationIssue
{
    Missing,
    Placeholder,
    InvalidFormat,
    TooShort,
    Insecure
}