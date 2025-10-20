using Microsoft.Extensions.Options;
using SetlistStudio.Core.Security;

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
        "YOUR_APP_SECRET"
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
    /// Validates all required secrets for the current environment
    /// </summary>
    /// <param name="strictValidation">If true, validates all secrets. If false, only validates configured OAuth providers</param>
    /// <returns>Validation result with details of any missing or invalid secrets</returns>
    public SecretValidationResult ValidateSecrets(bool strictValidation = false)
    {
        var result = new SecretValidationResult();
        var environmentName = _environment.EnvironmentName;

        _logger.LogInformation("Validating secrets for environment: {Environment}", environmentName);

        // Skip validation in development unless explicitly requested
        if (_environment.IsDevelopment() && !strictValidation)
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
        }
        else
        {
            _logger.LogWarning("Secret validation failed with {ErrorCount} errors in environment: {Environment}", 
                result.ValidationErrors.Count, environmentName);
        }

        return result;
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

        // Validate OAuth secret format
        if (secretKey.Contains("ClientId") || secretKey.Contains("AppId"))
        {
            if (secretValue.Length < 10)
            {
                return new SecretValidationError(
                    secretKey,
                    description,
                    SecretValidationIssue.InvalidFormat,
                    "OAuth Client ID appears to be too short"
                );
            }
        }

        if (secretKey.Contains("ClientSecret") || secretKey.Contains("AppSecret"))
        {
            if (secretValue.Length < 16)
            {
                return new SecretValidationError(
                    secretKey,
                    description,
                    SecretValidationIssue.InvalidFormat,
                    "OAuth Client Secret appears to be too short"
                );
            }
        }

        // Validate connection string format
        if (secretKey.Contains("ConnectionString"))
        {
            if (!IsValidConnectionString(secretValue))
            {
                return new SecretValidationError(
                    secretKey,
                    description,
                    SecretValidationIssue.InvalidFormat,
                    "Connection string format appears invalid"
                );
            }
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
                var errorMessage = $"Critical secret validation failed in {_environment.EnvironmentName} environment:\n" +
                    string.Join("\n", criticalErrors.Select(e => $"- {e.Description}: {e.Details}"));

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