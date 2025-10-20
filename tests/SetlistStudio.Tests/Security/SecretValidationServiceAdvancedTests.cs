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
        // Arrange - Only Google configured, others should be optional
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", "valid-google-client-id" },
            { "Authentication:Google:ClientSecret", "valid-google-client-secret-123456" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeTrue("Should allow missing OAuth providers when at least one is configured");
        result.ValidationErrors.Should().BeEmpty("Should not report errors for missing optional OAuth providers");
    }

    [Fact]
    public void ValidateSecrets_ShouldAllowOptionalOAuth_WhenMicrosoftProviderConfigured()
    {
        // Arrange - Only Microsoft configured
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Microsoft:ClientId", "valid-microsoft-client-id" },
            { "Authentication:Microsoft:ClientSecret", "valid-microsoft-client-secret-123456" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeTrue("Should allow missing OAuth providers when Microsoft is configured");
        result.ValidationErrors.Should().BeEmpty("Should not report errors for missing optional OAuth providers");
    }

    [Fact]
    public void ValidateSecrets_ShouldAllowOptionalOAuth_WhenFacebookProviderConfigured()
    {
        // Arrange - Only Facebook configured
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Facebook:AppId", "valid-facebook-app-id" },
            { "Authentication:Facebook:AppSecret", "valid-facebook-app-secret-123456" },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

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
    [InlineData("Authentication:Google:ClientId", "short", "OAuth Client ID appears to be too short")]
    [InlineData("Authentication:Microsoft:ClientId", "abc", "OAuth Client ID appears to be too short")]
    [InlineData("Authentication:Facebook:AppId", "12345", "OAuth Client ID appears to be too short")]
    public void ValidateSecrets_ShouldDetectShortClientIds(string secretKey, string secretValue, string expectedError)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { secretKey, secretValue },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
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
    [InlineData("Authentication:Google:ClientSecret", "short")]
    [InlineData("Authentication:Microsoft:ClientSecret", "tooshort123")]
    [InlineData("Authentication:Facebook:AppSecret", "1234567890")]
    public void ValidateSecrets_ShouldDetectShortClientSecrets(string secretKey, string secretValue)
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { secretKey, secretValue },
            { "ConnectionStrings:DefaultConnection", "Data Source=test.db" }
        };
        var service = CreateService(configValues, "Production");

        // Act
        var result = service.ValidateSecrets();

        // Assert
        result.IsValid.Should().BeFalse("Should detect short client secrets");
        var error = result.ValidationErrors.FirstOrDefault(e => e.SecretKey == secretKey);
        error.Should().NotBeNull("Should have validation error for short client secret");
        error!.Details.Should().Contain("OAuth Client Secret appears to be too short", "Should report specific format error");
        error.Issue.Should().Be(SecretValidationIssue.InvalidFormat, "Should classify as format issue");
    }

    [Theory]
    [InlineData("invalid-connection-string")]
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
    [InlineData("Data Source=valid.db")]
    [InlineData("Server=localhost;Database=MyDb")]
    [InlineData("Host=myserver;Port=5432")]
    public void ValidateSecrets_ShouldAcceptValidConnectionStrings(string connectionString)
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
        result.IsValid.Should().BeTrue("Should accept valid connection strings");
        result.ValidationErrors.Should().BeEmpty("Should not report errors for valid connection strings");
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
    public void ValidateSecretsOrThrow_ShouldThrow_InStagingWithMissingSecrets()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string>(), "Staging");

        // Act & Assert
        var action = () => service.ValidateSecretsOrThrow();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Critical secret validation failed in Staging environment*");
    }

    [Fact]
    public void ValidateSecretsOrThrow_ShouldThrow_InProductionWithPlaceholderSecrets()
    {
        // Arrange
        var configValues = new Dictionary<string, string>
        {
            { "Authentication:Google:ClientId", "YOUR_GOOGLE_CLIENT_ID" },
            { "Authentication:Google:ClientSecret", "YOUR_GOOGLE_CLIENT_SECRET" }
        };
        var service = CreateService(configValues, "Production");

        // Act & Assert
        var action = () => service.ValidateSecretsOrThrow();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Critical secret validation failed in Production environment*")
            .WithMessage("*Secret contains placeholder value*");
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Optional OAuth secret not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1),
            "Should log debug message for optional OAuth secrets");
    }

    #endregion
}