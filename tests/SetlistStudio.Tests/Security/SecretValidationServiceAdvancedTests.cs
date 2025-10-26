using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using Xunit;
using SetlistStudio.Core.Security;
using SetlistStudio.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Advanced tests for SecretValidationService focusing on branch coverage
/// Tests conditional logic, OAuth provider scenarios, and edge cases
/// </summary>
public class SecretValidationServiceAdvancedTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<SecurityEventLogger> _mockSecurityEventLogger;
    private readonly Mock<ILogger<SecretValidationService>> _mockLogger;

    public SecretValidationServiceAdvancedTests()
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
        SetupEnvironmentChecks(environment);
        
        return new SecretValidationService(
            configuration,
            _mockEnvironment.Object,
            _mockSecurityEventLogger.Object,
            _mockLogger.Object);
    }

    private void SetupEnvironmentChecks(string environment)
    {
        // The extension methods work on the EnvironmentName property
        // No need to mock the extension methods directly
    }

    #endregion

    #region OAuth Provider Configuration Tests

    [Fact]
    public void ValidateSecrets_ShouldAllowOptionalOAuth_WhenOneProviderConfigured()
    {
        // Arrange - Only Google configured in Development environment to bypass enhanced security validation
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", "valid-google-client-id" },
            { "Authentication:Google:ClientSecret", "valid-google-client-secret-123456" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Development");

        // Act
        var result = service.ValidateSecrets(strictValidation: true);

        // Assert
        result.IsValid.Should().BeTrue("Should allow missing OAuth providers when at least one is configured");
        result.ValidationErrors.Should().BeEmpty("Should not report errors for missing optional OAuth providers");
    }

    [Fact]
    public void ValidateSecrets_ShouldAllowOptionalOAuth_WhenMicrosoftProviderConfigured()
    {
        // Arrange - Only Microsoft configured in Development environment to bypass enhanced security validation
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Microsoft:ClientId", "valid-microsoft-client-id" },
            { "Authentication:Microsoft:ClientSecret", "valid-microsoft-client-secret-123456" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Development");

        // Act
        var result = service.ValidateSecrets(strictValidation: true);

        // Assert
        result.IsValid.Should().BeTrue("Should allow missing OAuth providers when Microsoft is configured");
        result.ValidationErrors.Should().BeEmpty("Should not report errors for missing optional OAuth providers");
    }

    [Fact]
    public void ValidateSecrets_ShouldAllowOptionalOAuth_WhenFacebookProviderConfigured()
    {
        // Arrange - Only Facebook configured in Development environment to bypass enhanced security validation
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Facebook:AppId", "valid-facebook-app-id" },
            { "Authentication:Facebook:AppSecret", "valid-facebook-app-secret-123456" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Development");

        // Act
        var result = service.ValidateSecrets(strictValidation: true);

        // Assert
        result.IsValid.Should().BeTrue("Should allow missing OAuth providers when Facebook is configured");
        result.ValidationErrors.Should().BeEmpty("Should not report errors for missing optional OAuth providers");
    }

    [Fact]
    public void ValidateSecrets_ShouldRequireAllOAuth_WhenNoProvidersConfiguredInProduction()
    {
        // Arrange - No OAuth providers configured in production
        var configValues = new Dictionary<string, string>
        {
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Should require OAuth providers when none are configured in production");
        result.ValidationErrors.Should().HaveCountGreaterThan(0, "Should report missing OAuth secrets");
        result.ValidationErrors.Where(e => e.SecretKey.Contains("Google")).Should().HaveCount(2, "Should require Google OAuth secrets");
        result.ValidationErrors.Where(e => e.SecretKey.Contains("Microsoft")).Should().HaveCount(2, "Should require Microsoft OAuth secrets");
        result.ValidationErrors.Where(e => e.SecretKey.Contains("Facebook")).Should().HaveCount(2, "Should require Facebook OAuth secrets");
    }

    #endregion

    #region Secret Format Validation Tests

    [Theory]
    [InlineData("Authentication:Microsoft:ClientId", "XyZqWrTuP", "too short")]
    [InlineData("Authentication:Facebook:AppId", "MnBvCxZaS", "too short")]
    public void ValidateSecrets_ShouldDetectShortClientIds(string secretKey, string secretValue, string expectedError)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { secretKey, secretValue },
            { "ConnectionStrings:DefaultConnection", "Server=prodserver.database.windows.net;Database=SetlistStudio;Authentication=Active Directory Default;" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Should detect short client IDs");
        var error = result.ValidationErrors.FirstOrDefault(e => e.SecretKey == secretKey);
        error.Should().NotBeNull("Should have validation error for short client ID");
        error!.Details.Should().Contain(expectedError, "Should report specific format error");
        error.Issue.Should().Be(SecretValidationIssue.InvalidFormat, "Should classify as format issue");
    }

    [Theory]
    [InlineData("Authentication:Google:ClientId", "AbCdEfGhI", "insecure patterns")]
    public void ValidateSecrets_ShouldDetectInsecureClientIds(string secretKey, string secretValue, string expectedError)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { secretKey, secretValue },
            { "ConnectionStrings:DefaultConnection", "Server=prodserver.database.windows.net;Database=SetlistStudio;Authentication=Active Directory Default;" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Should detect insecure client IDs");
        var error = result.ValidationErrors.FirstOrDefault(e => e.SecretKey == secretKey);
        error.Should().NotBeNull("Should have validation error for insecure client ID");
        error!.Details.Should().Contain(expectedError, "Should report specific format error");
        error.Issue.Should().Be(SecretValidationIssue.InvalidFormat, "Should classify as format issue");
    }

    [Theory]
    [InlineData("Authentication:Google:ClientSecret", "ShrtScrt123456")]
    [InlineData("Authentication:Microsoft:ClientSecret", "ClntScrt789012")]
    [InlineData("Authentication:Facebook:AppSecret", "MyAppScrt12345")]
    public void ValidateSecrets_ShouldDetectInsecureClientSecrets(string secretKey, string secretValue)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { secretKey, secretValue },
            { "ConnectionStrings:DefaultConnection", "Server=prodserver.database.windows.net;Database=SetlistStudio;Authentication=Active Directory Default;" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Should detect insecure client secrets");
        var error = result.ValidationErrors.FirstOrDefault(e => e.SecretKey == secretKey);
        error.Should().NotBeNull("Should have validation error for insecure client secret");
        error!.Details.Should().Contain("insecure patterns", "Should report specific format error");
        error.Issue.Should().Be(SecretValidationIssue.InvalidFormat, "Should classify as format issue");
    }

    [Theory]
    [InlineData("NotAConnectionString")]
    [InlineData("Database=MyDb;")]
    [InlineData("Provider=SqlServer")]
    public void ValidateSecrets_ShouldDetectInvalidConnectionStrings(string connectionString)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "ConnectionStrings:DefaultConnection", connectionString },
            { "Authentication:Google:ClientId", "valid-google-client-id" },
            { "Authentication:Google:ClientSecret", "valid-google-client-secret-123456" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Should detect invalid connection strings");
        var error = result.ValidationErrors.FirstOrDefault(e => e.SecretKey.Contains("ConnectionString"));
        error.Should().NotBeNull("Should have validation error for invalid connection string");
        error!.Details.Should().Contain("Connection string format appears invalid", "Should report specific format error");
        error.Issue.Should().Be(SecretValidationIssue.InvalidFormat, "Should classify as format issue");
    }

    [Theory]
    [InlineData("Data Source=app.db")]
    [InlineData("Server=appserver.company.com;Database=AppDatabase")]
    [InlineData("Host=db-cluster.company.net;Port=5432")]
    public void ValidateSecrets_ShouldAcceptValidConnectionStrings_InDevelopment(string connectionString)
    {
        // Arrange - Test in Development environment to bypass enhanced security validation
        var configValues = new Dictionary<string, string>
        {
            { "ConnectionStrings:DefaultConnection", connectionString },
            { "Authentication:Google:ClientId", "valid-google-client-id" },
            { "Authentication:Google:ClientSecret", "valid-google-client-secret-123456" }
        };
        var service = CreateService(configValues, "Development");

        // Act
        var result = service.ValidateSecrets(strictValidation: true);

        // Assert
        result.IsValid.Should().BeTrue("Should accept valid connection strings in development");
        result.ValidationErrors.Should().BeEmpty("Should not report errors for valid connection strings in development");
    }

    #endregion

    #region Placeholder Detection Tests

    [Theory]
    [InlineData("Authentication:Google:ClientId", "YOUR_GOOGLE_CLIENT_ID")]
    [InlineData("Authentication:Microsoft:ClientSecret", "YOUR_MICROSOFT_CLIENT_SECRET")]
    [InlineData("Authentication:Facebook:AppId", "YOUR_FACEBOOK_APP_ID")]
    [InlineData("Authentication:Facebook:AppSecret", "YOUR_FACEBOOK_APP_SECRET")]
    [InlineData("Authentication:Google:ClientSecret", "YOUR_CLIENT_SECRET")]
    [InlineData("Authentication:Microsoft:ClientId", "YOUR_CLIENT_ID")]
    public void ValidateSecrets_ShouldDetectPlaceholderValues(string secretKey, string placeholderValue)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { secretKey, placeholderValue },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Should detect placeholder values");
        var error = result.ValidationErrors.FirstOrDefault(e => e.SecretKey == secretKey);
        error.Should().NotBeNull("Should have validation error for placeholder value");
        error!.Issue.Should().Be(SecretValidationIssue.Placeholder, "Should classify as placeholder issue");
        error.Details.Should().Contain($"Secret contains placeholder value: {placeholderValue}", "Should report specific placeholder");
    }

    [Theory]
    [InlineData("YOUR_APP_ID")]
    [InlineData("YOUR_APP_SECRET")]
    [InlineData("your_google_client_id")] // Test case insensitive
    [InlineData("Your_Client_Secret")] // Test mixed case
    public void ValidateSecrets_ShouldDetectCaseInsensitivePlaceholders(string placeholderValue)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", placeholderValue },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Should detect case-insensitive placeholder values");
        var error = result.ValidationErrors.FirstOrDefault(e => e.SecretKey.Contains("Google"));
        error.Should().NotBeNull("Should have validation error for case-insensitive placeholder");
        error!.Issue.Should().Be(SecretValidationIssue.Placeholder, "Should classify as placeholder issue");
    }

    #endregion

    #region Environment-Specific Validation Tests

    [Fact]
    public void ValidateSecretsOrThrow_ShouldNotThrow_InDevelopmentWithMissingSecrets()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string>(), "Development");

        // Act & Assert
        var action = () => service.ValidateSecretsOrThrow();
        action.Should().NotThrow("Should not throw in development environment");
    }

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
    public void ValidateSecretsOrThrow_ShouldNotThrow_InProductionWithPlaceholderSecretsInTestContext()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", "YOUR_GOOGLE_CLIENT_ID" },
            { "Authentication:Google:ClientSecret", "YOUR_GOOGLE_CLIENT_SECRET" }
        };
        var service = CreateService(configValues, "Production");

        // Act & Assert - Should not throw in test context even with placeholder secrets
        var action = () => service.ValidateSecretsOrThrow();
        action.Should().NotThrow("Test context should suppress exceptions for placeholder secrets in production");
    }

    [Fact]
    public void ValidateSecretsOrThrow_ShouldNotThrow_WithOnlyFormatErrors()
    {
        // Arrange - Short but real secrets (format errors but not critical)
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", "short-id" }, // Format error but not critical
            { "Authentication:Google:ClientSecret", "short-secret-12345" }, // Format error but not critical
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act & Assert
        var action = () => service.ValidateSecretsOrThrow();
        action.Should().NotThrow("Should not throw for non-critical format errors");
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public void ValidateSecrets_ShouldHandleNullSecretValues()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", null! },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Should handle null secret values");
        var error = result.ValidationErrors.FirstOrDefault(e => e.SecretKey.Contains("Google"));
        error.Should().NotBeNull("Should report error for null secret");
        error!.Issue.Should().Be(SecretValidationIssue.Missing, "Should classify null as missing");
    }

    [Fact]
    public void ValidateSecrets_ShouldHandleWhitespaceOnlySecrets()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", "   " },
            { "Authentication:Microsoft:ClientSecret", "\t\n\r" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Should handle whitespace-only secrets");
        result.ValidationErrors.Where(e => e.Issue == SecretValidationIssue.Missing)
            .Should().HaveCountGreaterThan(0, "Should classify whitespace-only as missing");
    }

    [Fact]
    public void ValidateSecrets_ShouldLogDebugForOptionalOAuthSecrets()
    {
        // Arrange - Google configured, others optional
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", "valid-google-client-id" },
            { "Authentication:Google:ClientSecret", "valid-google-client-secret-123456" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Development");

        // Act
        service.ValidateSecrets(strictValidation: true);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipping validation for optional OAuth secret")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1),
            "Should log debug message for optional OAuth secrets");
    }

    #endregion

    #region KeyVault Configuration Validation Tests

    [Fact]
    public void ValidateSecrets_WithEmptyKeyVaultName_ShouldLogInformationAndContinue()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "KeyVault:VaultName", "" }, // Empty KeyVault name
            { "Authentication:Google:ClientId", "valid-client-id" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        service.ValidateSecrets(strictValidation: true);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Azure Key Vault not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log information when KeyVault is not configured");
    }

    [Fact]
    public void ValidateSecrets_WithInvalidKeyVaultNameFormat_ShouldAddValidationError()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "KeyVault:VaultName", "invalid_vault_name_with_underscores" }, // Invalid format
            { "Authentication:Google:ClientId", "valid-client-id" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets(strictValidation: true);

        // Assert
        result.IsValid.Should().BeFalse("Invalid KeyVault name format should make validation fail");
        result.ValidationErrors.Should().ContainSingle(e => 
            e.SecretKey == "KeyVault:VaultName" && e.Issue == SecretValidationIssue.InvalidFormat);
    }

    [Fact]
    public void ValidateSecrets_WithKeyVaultNameTooShort_ShouldAddValidationError()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "KeyVault:VaultName", "ab" }, // Too short (less than 3 characters)
            { "Authentication:Google:ClientId", "valid-client-id" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets(strictValidation: true);

        // Assert
        result.IsValid.Should().BeFalse("KeyVault name too short should make validation fail");
        result.ValidationErrors.Should().ContainSingle(e => 
            e.SecretKey == "KeyVault:VaultName" && e.Issue == SecretValidationIssue.InvalidFormat);
    }

    [Fact]
    public void ValidateSecrets_WithKeyVaultNameTooLong_ShouldAddValidationError()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "KeyVault:VaultName", "this-is-a-very-long-key-vault-name-that-exceeds-twentyfour-characters" }, // Too long (>24 characters)
            { "Authentication:Google:ClientId", "valid-client-id" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets(strictValidation: true);

        // Assert
        result.IsValid.Should().BeFalse("KeyVault name too long should make validation fail");
        result.ValidationErrors.Should().ContainSingle(e => 
            e.SecretKey == "KeyVault:VaultName" && e.Issue == SecretValidationIssue.InvalidFormat);
    }

    [Fact]
    public void ValidateSecrets_WithKeyVaultPlaceholderValue_ShouldAddValidationError()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "KeyVault:VaultName", "YOUR_KEY_VAULT_NAME" }, // Placeholder value
            { "Authentication:Google:ClientId", "valid-client-id" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets(strictValidation: true);

        // Assert
        result.IsValid.Should().BeFalse("KeyVault placeholder value should make validation fail");
        result.ValidationErrors.Should().ContainSingle(e => 
            e.SecretKey == "KeyVault:VaultName" && e.Issue == SecretValidationIssue.Placeholder);
    }

    [Fact]
    public void ValidateSecrets_WithKeyVaultNameStartingWithDigit_ShouldAddValidationError()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "KeyVault:VaultName", "1invalid-start" }, // Cannot start with digit
            { "Authentication:Google:ClientId", "valid-client-id" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets(strictValidation: true);

        // Assert
        result.IsValid.Should().BeFalse("KeyVault name starting with digit should make validation fail");
        result.ValidationErrors.Should().ContainSingle(e => 
            e.SecretKey == "KeyVault:VaultName" && e.Issue == SecretValidationIssue.InvalidFormat);
    }

    [Fact]
    public void ValidateSecrets_WithKeyVaultNameEndingWithHyphen_ShouldAddValidationError()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "KeyVault:VaultName", "invalid-end-" }, // Cannot end with hyphen
            { "Authentication:Google:ClientId", "valid-client-id" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets(strictValidation: true);

        // Assert
        result.IsValid.Should().BeFalse("KeyVault name ending with hyphen should make validation fail");
        result.ValidationErrors.Should().ContainSingle(e => 
            e.SecretKey == "KeyVault:VaultName" && e.Issue == SecretValidationIssue.InvalidFormat);
    }

    [Fact]
    public void ValidateSecrets_WithValidKeyVaultName_ShouldLogSuccessAndContinue()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "KeyVault:VaultName", "valid-keyvault-name" }, // Valid format
            { "Authentication:Google:ClientId", "valid-client-id" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        service.ValidateSecrets(strictValidation: true);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Azure Key Vault configuration validated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log successful KeyVault validation");
    }

    [Fact]
    public void ValidateSecrets_WithWhitespaceKeyVaultName_ShouldLogInformationInTesting()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "KeyVault:VaultName", "   " }, // Whitespace only
            { "Authentication:Google:ClientId", "valid-client-id" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Testing");

        // Act
        service.ValidateSecrets(strictValidation: true);

        // Assert - Should NOT log the message in Testing environment
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Azure Key Vault not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "Should not log KeyVault message in Testing environment");
    }

    #endregion
}