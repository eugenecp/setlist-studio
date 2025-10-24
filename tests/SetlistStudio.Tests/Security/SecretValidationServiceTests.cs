using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using Xunit;
using SetlistStudio.Core.Security;
using SetlistStudio.Web.Services;
using System;
using System.Collections.Generic;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Comprehensive tests for SecretValidationService
/// Tests secret validation, security compliance, and environment-specific behaviors
/// </summary>
public class SecretValidationServiceTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<SecurityEventLogger> _mockSecurityEventLogger;
    private readonly Mock<ILogger<SecretValidationService>> _mockLogger;

    public SecretValidationServiceTests()
    {
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockSecurityEventLogger = new Mock<SecurityEventLogger>(Mock.Of<ILogger<SecurityEventLogger>>());
        _mockLogger = new Mock<ILogger<SecretValidationService>>();
    }

    #region Helper Methods

    private IConfiguration CreateConfiguration(Dictionary<string, string> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values!)
            .Build();
    }

    private SecretValidationService CreateService(Dictionary<string, string> configValues, string environment = "Development")
    {
        var configuration = CreateConfiguration(configValues);
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(environment);
        
        return new SecretValidationService(
            configuration,
            _mockEnvironment.Object,
            _mockSecurityEventLogger.Object,
            _mockLogger.Object);
    }

    #endregion

    #region Development Environment Tests

    [Fact]
    public void ValidateSecrets_ShouldSkipValidation_InDevelopmentEnvironment()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string>(), "Development");

        // Act
        var result = service.ValidateSecrets(strictValidation: false);

        // Assert
        result.IsValid.Should().BeTrue("Development environment should skip strict validation");
        result.ValidationErrors.Should().BeEmpty("No errors should be reported in development");
    }

    [Fact]
    public void ValidateSecrets_ShouldValidateWhenStrict_InDevelopmentEnvironment()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", "" },
            { "Authentication:Google:ClientSecret", "" }
        };
        var service = CreateService(configValues, "Development");

        // Act
        var result = service.ValidateSecrets(strictValidation: true);

        // Assert
        result.IsValid.Should().BeFalse("Strict validation should find missing secrets even in development");
        result.ValidationErrors.Should().NotBeEmpty("Should report missing secrets when strict validation is enabled");
    }

    #endregion

    #region Production Environment Tests

    [Fact]
    public void ValidateSecrets_ShouldRequireAllSecrets_InProductionEnvironment()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string>(), "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Production environment should require all secrets");
        result.ValidationErrors.Should().NotBeEmpty("Should report missing secrets in production");
        result.ValidationErrors.Should().HaveCount(7, "Should validate all required secrets");
    }

    [Fact]
    public void ValidateSecretsOrThrow_ShouldNotThrow_InTestContext()
    {
        // Arrange - Test context is detected by checking loaded assemblies
        var service = CreateService(new Dictionary<string, string>(), "Production");

        // Act & Assert - Should not throw in test context even with missing secrets
        var action = () => service.ValidateSecretsOrThrow();
        action.Should().NotThrow("Test context should suppress exceptions for missing secrets");
    }

    [Fact]
    public void ValidateSecrets_ShouldLogSecurityEvent_ForProductionSecretFailures()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string>(), "Production");

        // Act
        service.ValidateSecrets();

        // Assert
        _mockSecurityEventLogger.Verify(
            x => x.LogSecurityEvent(
                SecurityEventType.ConfigurationChange,
                SecurityEventSeverity.High,
                It.IsAny<string>(),
                null,
                "Configuration",
                It.IsAny<string>(),
                It.IsAny<object>()),
            Times.AtLeast(1),
            "Should log security events for production secret failures");
    }

    #endregion

    #region Secret Validation Tests

    [Theory]
    [InlineData("Authentication:Google:ClientId", "valid-google-client-id")]
    [InlineData("Authentication:Microsoft:ClientId", "valid-microsoft-client-id")]
    [InlineData("Authentication:Facebook:AppId", "valid-facebook-app-id")]
    public void ValidateSecrets_ShouldDetectInsecurePatterns_InProduction(string secretKey, string validValue)
    {
        // Arrange - Test production behavior where enhanced security validation detects test-like patterns
        var configValues = new Dictionary<string, string>
        {
            { secretKey, validValue },
            { secretKey.Replace("ClientId", "ClientSecret").Replace("AppId", "AppSecret"), "valid-secret-with-sufficient-length" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert - With enhanced security, these patterns should be detected as insecure
        result.IsValid.Should().BeFalse("Enhanced security validation should detect insecure patterns");
        var secretErrors = result.ValidationErrors.Where(e => e.SecretKey == secretKey);
        secretErrors.Should().NotBeEmpty($"Client ID '{secretKey}' with test-like patterns should be detected as insecure");
        secretErrors.First().Details.Should().Contain("insecure patterns", "Should detect test-like patterns as insecure");
    }

    [Theory]
    [InlineData("Authentication:Google:ClientSecret", "valid-google-secret-with-sufficient-length")]
    [InlineData("Authentication:Microsoft:ClientSecret", "valid-microsoft-secret-with-sufficient-length")]
    [InlineData("Authentication:Facebook:AppSecret", "valid-facebook-secret-with-sufficient-length")]
    public void ValidateSecrets_ShouldDetectInsecurePatterns_ForClientSecrets(string secretKey, string validValue)
    {
        // Arrange - Test production behavior where enhanced security validation detects test-like patterns
        var configValues = new Dictionary<string, string>
        {
            { secretKey, validValue },
            { secretKey.Replace("ClientSecret", "ClientId").Replace("AppSecret", "AppId"), "valid-client-id" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert - With enhanced security, these patterns should be detected as insecure
        result.IsValid.Should().BeFalse("Enhanced security validation should detect insecure patterns");
        var secretErrors = result.ValidationErrors.Where(e => e.SecretKey == secretKey);
        secretErrors.Should().NotBeEmpty($"Client secret '{secretKey}' with test-like patterns should be detected as insecure");
        secretErrors.First().Details.Should().Contain("insecure patterns", "Should detect test-like patterns as insecure");
    }

    [Theory]
    [InlineData("YOUR_GOOGLE_CLIENT_ID")]
    [InlineData("YOUR_MICROSOFT_CLIENT_SECRET")]
    [InlineData("YOUR_FACEBOOK_APP_ID")]
    [InlineData("YOUR_CLIENT_ID")]
    [InlineData("YOUR_CLIENT_SECRET")]
    public void ValidateSecrets_ShouldFail_ForPlaceholderValues(string placeholderValue)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", placeholderValue }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        var placeholderError = result.ValidationErrors
            .FirstOrDefault(e => e.Issue == SecretValidationIssue.Placeholder);
        placeholderError.Should().NotBeNull($"Placeholder value '{placeholderValue}' should be detected");
        placeholderError!.Details.Should().Contain(placeholderValue);
    }

    [Theory]
    [InlineData("XyZqWrTuP")]  // 9 characters, too short for OAuth Client ID
    [InlineData("MnBvCxZaS")]  // 9 characters, too short for OAuth Client ID
    public void ValidateSecrets_ShouldFail_ForTooShortClientId(string shortClientId)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", shortClientId }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        var formatError = result.ValidationErrors
            .FirstOrDefault(e => e.Issue == SecretValidationIssue.InvalidFormat && e.SecretKey.Contains("ClientId"));
        formatError.Should().NotBeNull($"Short client ID '{shortClientId}' should be rejected");
        formatError!.Details.Should().Contain("too short");
    }

    [Theory]
    [InlineData("AbCdEfGhI")]  // Has sequential characters A->b->C, detected as insecure
    public void ValidateSecrets_ShouldFail_ForInsecureClientId(string insecureClientId)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", insecureClientId }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        var formatError = result.ValidationErrors
            .FirstOrDefault(e => e.Issue == SecretValidationIssue.InvalidFormat && e.SecretKey.Contains("ClientId"));
        formatError.Should().NotBeNull($"Insecure client ID '{insecureClientId}' should be rejected");
        formatError!.Details.Should().Contain("insecure patterns");
    }

    [Theory]
    [InlineData("ShrtScrt123456")]  // Contains sequential "123" - detected as insecure patterns
    [InlineData("ClntScrt789012")]  // Contains sequential "789" - detected as insecure patterns  
    [InlineData("MyAppScrt12345")]  // Contains sequential "12345" - detected as insecure patterns
    public void ValidateSecrets_ShouldFail_ForInsecureClientSecret(string insecureSecret)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientSecret", insecureSecret }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        var formatError = result.ValidationErrors
            .FirstOrDefault(e => e.Issue == SecretValidationIssue.InvalidFormat && e.SecretKey.Contains("ClientSecret"));
        formatError.Should().NotBeNull($"Insecure client secret '{insecureSecret}' should be rejected");
        formatError!.Details.Should().Contain("insecure patterns");
    }

    #endregion

    #region Connection String Validation Tests

    [Theory]
    [InlineData("Data Source=production.db")]
    [InlineData("Server=localhost;Database=TestDb;Trusted_Connection=true;")]
    [InlineData("Host=prod-db-cluster.amazonaws.com;Database=setlist_studio;User Id=appuser;Authentication=SCRAM-SHA-256;")]
    public void ValidateSecrets_ShouldDetectInsecurePatterns_ForConnectionStrings(string connectionString)
    {
        // Arrange - Test production behavior where enhanced security validation detects insecure patterns
        var configValues = new Dictionary<string, string>
        {
            { "ConnectionStrings:DefaultConnection", connectionString }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert - With enhanced security, these patterns should be detected as insecure
        result.IsValid.Should().BeFalse("Enhanced security validation should detect insecure patterns");
        var connectionErrors = result.ValidationErrors
            .Where(e => e.SecretKey == "ConnectionStrings:DefaultConnection");
        connectionErrors.Should().NotBeEmpty($"Connection string with insecure patterns should be detected: {connectionString}");
        connectionErrors.First().Details.Should().Contain("insecure patterns", "Should detect insecure patterns in connection strings");
    }

    [Theory]
    [InlineData("invalid connection string")]
    [InlineData("random text")]
    [InlineData("ConnectionString=")]
    public void ValidateSecrets_ShouldFail_ForInvalidConnectionStrings(string invalidConnectionString)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "ConnectionStrings:DefaultConnection", invalidConnectionString }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        var connectionError = result.ValidationErrors
            .FirstOrDefault(e => e.SecretKey == "ConnectionStrings:DefaultConnection" 
                               && e.Issue == SecretValidationIssue.InvalidFormat);
        connectionError.Should().NotBeNull($"Invalid connection string should be rejected: {invalidConnectionString}");
    }

    #endregion

    #region Optional OAuth Provider Tests

    [Fact]
    public void ValidateSecrets_ShouldSkipOptionalOAuthSecrets_WhenProviderNotConfigured()
    {
        // Arrange - Only configure Google, leave Microsoft empty
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", "valid-google-client-id" },
            { "Authentication:Google:ClientSecret", "valid-google-secret-with-length" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        var microsoftErrors = result.ValidationErrors
            .Where(e => e.SecretKey.Contains("Microsoft"));
        microsoftErrors.Should().BeEmpty("Microsoft OAuth secrets should be optional when not configured");
        
        var facebookErrors = result.ValidationErrors
            .Where(e => e.SecretKey.Contains("Facebook"));
        facebookErrors.Should().BeEmpty("Facebook OAuth secrets should be optional when not configured");
    }

    [Fact]
    public void ValidateSecrets_ShouldRequireBothSecrets_WhenOAuthProviderPartiallyConfigured()
    {
        // Arrange - Configure only Google Client ID but not secret
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", "valid-google-client-id" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        var googleSecretError = result.ValidationErrors
            .FirstOrDefault(e => e.SecretKey == "Authentication:Google:ClientSecret");
        googleSecretError.Should().NotBeNull("Google Client Secret should be required when Client ID is configured");
    }

    #endregion

    #region Staging Environment Tests

    [Fact]
    public void ValidateSecretsOrThrow_ShouldNotThrow_InStagingTestContext()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string>(), "Staging");

        // Act & Assert - Should not throw in test context even in staging
        var action = () => service.ValidateSecretsOrThrow();
        action.Should().NotThrow("Test context should suppress exceptions for missing secrets in staging");
    }

    [Fact]
    public void ValidateSecretsOrThrow_ShouldNotThrow_InDevelopmentWithMissingSecrets()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string>(), "Development");

        // Act & Assert
        var action = () => service.ValidateSecretsOrThrow();
        action.Should().NotThrow("Development environment should not throw on missing secrets");
    }

    #endregion

    #region Complete Configuration Tests

    [Fact]
    public void ValidateSecrets_ShouldPass_InDevelopmentEnvironment()
    {
        // Arrange - Development environment bypasses enhanced security validation
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", "valid-google-client-id-123" },
            { "Authentication:Google:ClientSecret", "valid-google-secret-with-sufficient-length" },
            { "Authentication:Microsoft:ClientId", "valid-microsoft-client-id-456" },
            { "Authentication:Microsoft:ClientSecret", "valid-microsoft-secret-with-sufficient-length" },
            { "Authentication:Facebook:AppId", "valid-facebook-app-id-789" },
            { "Authentication:Facebook:AppSecret", "valid-facebook-secret-with-sufficient-length" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Development");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeTrue("Development environment should bypass enhanced security validation");
        result.ValidationErrors.Should().BeEmpty("No validation errors should be present in development");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void ValidateSecrets_ShouldHandleNullConfiguration_Gracefully()
    {
        // Arrange
        var service = new SecretValidationService(
            new ConfigurationBuilder().Build(),
            _mockEnvironment.Object,
            _mockSecurityEventLogger.Object,
            _mockLogger.Object);
        
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Should fail validation with missing configuration");
        result.ValidationErrors.Should().NotBeEmpty("Should report missing secrets");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void ValidateSecrets_ShouldLogValidationResults()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string>(), "Development");

        // Act
        service.ValidateSecrets();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating secrets for environment")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Should log validation start");
    }

    [Fact]
    public void ValidateSecrets_ShouldLogWarning_WhenValidationFails()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string>(), "Production");

        // Act
        service.ValidateSecrets();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Secret validation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log warning when validation fails");
    }

    #endregion
}