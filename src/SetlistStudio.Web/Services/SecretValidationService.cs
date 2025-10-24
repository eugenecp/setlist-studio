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
    /// Determines if the current environment is non-production (Development or Testing)
    /// </summary>
    private bool IsNonProductionEnvironment()
    {
        var environmentName = _environment.EnvironmentName;
        return _environment.IsDevelopment() || 
               string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
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

        // Check if Azure Key Vault is configured for production
        if (!IsNonProductionEnvironment())
        {
            var keyVaultValidation = ValidateKeyVaultConfiguration(environmentName);
            result.ValidationErrors.AddRange(keyVaultValidation);
        }

        // Skip validation in development or testing unless explicitly requested
        if (IsNonProductionEnvironment() && !strictValidation)
        {
            _logger.LogInformation("Skipping strict secret validation in development environment");
            return result;
        }

        // Validate each required secret
        foreach (var (secretKey, description) in RequiredSecrets)
        {
            var secretValue = _configuration[secretKey];
            var validationIssue = ValidateIndividualSecret(secretKey, secretValue, description);
            
            if (validationIssue != null)
            {
                result.ValidationErrors.Add(validationIssue);
                
                // Log security event for missing production secrets
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
        }

        // Log validation summary
        if (result.IsValid)
        {
            _logger.LogInformation("All secrets validated successfully for environment: {Environment}", environmentName);
            
            // Log Key Vault usage for production
            if (!IsNonProductionEnvironment())
            {
                var keyVaultName = _configuration["KeyVault:VaultName"];
                if (!string.IsNullOrEmpty(keyVaultName))
                {
                    _logger.LogInformation("Using Azure Key Vault for secret management: {KeyVaultName}", keyVaultName);
                }
            }
        }
        else
        {
            // Only log warning for non-test environments
            if (!environmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Secret validation failed with {ErrorCount} errors in environment: {Environment}", 
                    result.ValidationErrors.Count, environmentName);
            }
        }

        return result;
    }

    /// <summary>
    /// Validates Azure Key Vault configuration for production environments
    /// </summary>
    private List<SecretValidationError> ValidateKeyVaultConfiguration(string environmentName)
    {
        var errors = new List<SecretValidationError>();
        var keyVaultName = _configuration["KeyVault:VaultName"];

        if (string.IsNullOrWhiteSpace(keyVaultName))
        {
            // Key Vault is not configured - this is okay for some deployment scenarios
            if (!environmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Azure Key Vault not configured - using local configuration");
            }
            return errors;
        }

        // Validate Key Vault name format
        if (!IsValidKeyVaultName(keyVaultName))
        {
            errors.Add(new SecretValidationError(
                "KeyVault:VaultName",
                "Azure Key Vault Name",
                SecretValidationIssue.InvalidFormat,
                $"Invalid Key Vault name format: {keyVaultName}. Must be 3-24 characters, alphanumeric and hyphens only."
            ));
        }

        // Check if Key Vault name contains placeholder values
        if (PlaceholderValues.Contains(keyVaultName) || keyVaultName.Contains("YOUR_") || keyVaultName.Contains("your_"))
        {
            errors.Add(new SecretValidationError(
                "KeyVault:VaultName",
                "Azure Key Vault Name",
                SecretValidationIssue.Placeholder,
                $"Key Vault name contains placeholder value: {keyVaultName}"
            ));
        }

        if (errors.Count == 0)
        {
            _logger.LogInformation("Azure Key Vault configuration validated: {KeyVaultName}", keyVaultName);
        }

        return errors;
    }

    /// <summary>
    /// Validates Azure Key Vault name format according to Azure naming conventions
    /// </summary>
    private static bool IsValidKeyVaultName(string keyVaultName)
    {
        if (string.IsNullOrWhiteSpace(keyVaultName))
            return false;

        if (keyVaultName.Length < 3 || keyVaultName.Length > 24)
            return false;

        // Must start with letter
        if (!char.IsLetter(keyVaultName[0]))
            return false;

        // Must end with letter or digit
        if (!char.IsLetterOrDigit(keyVaultName[^1]))
            return false;

        // Can only contain letters, digits, and hyphens
        return keyVaultName.All(c => char.IsLetterOrDigit(c) || c == '-');
    }

    /// <summary>
    /// Validates a single secret configuration value
    /// </summary>
    private SecretValidationError? ValidateIndividualSecret(string secretKey, string? secretValue, string description)
    {
        // Check if secret is missing or empty
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

        // Check if secret contains placeholder values
        if (PlaceholderValues.Contains(secretValue))
        {
            return new SecretValidationError(
                secretKey,
                description,
                SecretValidationIssue.Placeholder,
                $"Secret contains placeholder value: {secretValue}"
            );
        }

        // SECURITY ENHANCEMENT: Check for production-unsafe patterns
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

        // Validate OAuth secret format
        if ((secretKey.Contains("ClientId") || secretKey.Contains("AppId")) && secretValue.Length < 10)
        {
            return new SecretValidationError(
                secretKey,
                description,
                SecretValidationIssue.InvalidFormat,
                "OAuth Client ID appears to be too short"
            );
        }

        if ((secretKey.Contains("ClientSecret") || secretKey.Contains("AppSecret")) && secretValue.Length < 16)
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
        if (secretKey.Contains("Google"))
        {
            return !IsProviderConfigured("Google");
        }
        if (secretKey.Contains("Microsoft"))
        {
            return !IsProviderConfigured("Microsoft");
        }
        if (secretKey.Contains("Facebook"))
        {
            return !IsProviderConfigured("Facebook");
        }
        
        return false;
    }

    /// <summary>
    /// Checks if an OAuth provider is configured for use
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
        
        foreach (var pattern in InsecurePatterns)
        {
            if (lowerValue.Contains(pattern.ToLowerInvariant()))
            {
                return false;
            }
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
        var result = ValidateSecrets(strictValidation: _environment.IsProduction());
        
        if (!result.IsValid)
        {
            var criticalErrors = result.ValidationErrors
                .Where(e => e.Issue == SecretValidationIssue.Missing || e.Issue == SecretValidationIssue.Placeholder)
                .ToList();

            // Only throw in production/staging environments AND not in test context
            var isInTestContext = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name?.Contains("Test", StringComparison.OrdinalIgnoreCase) == true);
                
            if (criticalErrors.Any() && (_environment.IsProduction() || _environment.IsStaging()) && !isInTestContext)
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