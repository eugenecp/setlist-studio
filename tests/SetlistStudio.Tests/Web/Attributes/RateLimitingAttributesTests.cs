using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SetlistStudio.Web.Attributes;
using System.Reflection;
using Xunit;

namespace SetlistStudio.Tests.Web.Attributes;

/// <summary>
/// Comprehensive tests for RateLimitingAttributes covering all attribute classes and their properties.
/// These tests ensure proper initialization, property validation, and security configurations.
/// </summary>
public class RateLimitingAttributesTests
{
    #region RateLimitPolicies Tests

    [Fact]
    public void RateLimitPolicies_ShouldHaveCorrectConstantValues()
    {
        // Test all the constant policy names are properly defined
        RateLimitPolicies.Global.Should().Be("GlobalPolicy");
        RateLimitPolicies.Api.Should().Be("ApiPolicy");
        RateLimitPolicies.AuthenticatedApi.Should().Be("AuthenticatedApiPolicy");
        RateLimitPolicies.Auth.Should().Be("AuthPolicy");
        RateLimitPolicies.Authenticated.Should().Be("AuthenticatedPolicy");
        RateLimitPolicies.Strict.Should().Be("StrictPolicy");
        RateLimitPolicies.Sensitive.Should().Be("SensitivePolicy");
    }

    #endregion

    #region RequireCaptchaOnSuspiciousActivityAttribute Tests

    [Fact]
    public void RequireCaptchaOnSuspiciousActivityAttribute_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var attribute = new RequireCaptchaOnSuspiciousActivityAttribute();

        // Assert
        attribute.ViolationThreshold.Should().Be(3, "default violation threshold should be 3");
        attribute.TimeWindowMinutes.Should().Be(60, "default time window should be 60 minutes");
        attribute.RequireForUnauthenticated.Should().BeFalse("should not require CAPTCHA for unauthenticated by default");
    }

    [Fact]
    public void RequireCaptchaOnSuspiciousActivityAttribute_ViolationThreshold_ShouldBeSettable()
    {
        // Arrange
        var attribute = new RequireCaptchaOnSuspiciousActivityAttribute();

        // Act
        attribute.ViolationThreshold = 5;

        // Assert
        attribute.ViolationThreshold.Should().Be(5);
    }

    [Fact]
    public void RequireCaptchaOnSuspiciousActivityAttribute_TimeWindowMinutes_ShouldBeSettable()
    {
        // Arrange
        var attribute = new RequireCaptchaOnSuspiciousActivityAttribute();

        // Act
        attribute.TimeWindowMinutes = 120;

        // Assert
        attribute.TimeWindowMinutes.Should().Be(120);
    }

    [Fact]
    public void RequireCaptchaOnSuspiciousActivityAttribute_RequireForUnauthenticated_ShouldBeSettable()
    {
        // Arrange
        var attribute = new RequireCaptchaOnSuspiciousActivityAttribute();

        // Act
        attribute.RequireForUnauthenticated = true;

        // Assert
        attribute.RequireForUnauthenticated.Should().BeTrue();
    }

    [Fact]
    public void RequireCaptchaOnSuspiciousActivityAttribute_ShouldHaveCorrectAttributeUsage()
    {
        // Arrange & Act
        var attributeUsage = typeof(RequireCaptchaOnSuspiciousActivityAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.Should().NotBeNull("attribute should have AttributeUsage defined");
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Class, "should be applicable to classes");
        attributeUsage.ValidOn.Should().HaveFlag(AttributeTargets.Method, "should be applicable to methods");
        attributeUsage.AllowMultiple.Should().BeFalse("should not allow multiple instances");
    }

    #endregion

    #region MultiFactorRateLimitAttribute Tests

    [Fact]
    public void MultiFactorRateLimitAttribute_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var attribute = new MultiFactorRateLimitAttribute();

        // Assert
        attribute.EnableIpLimiting.Should().BeTrue("IP limiting should be enabled by default");
        attribute.EnableUserLimiting.Should().BeTrue("user limiting should be enabled by default");
        attribute.EnableSessionLimiting.Should().BeTrue("session limiting should be enabled by default");
        attribute.EnableUserAgentFingerprinting.Should().BeTrue("user agent fingerprinting should be enabled by default");
        attribute.EnableNetworkSegmentLimiting.Should().BeFalse("network segment limiting should be disabled by default");
        attribute.CustomRateLimit.Should().BeNull("custom rate limit should be null by default");
        attribute.CustomTimeWindowMinutes.Should().BeNull("custom time window should be null by default");
    }

    [Fact]
    public void MultiFactorRateLimitAttribute_EnableIpLimiting_ShouldBeSettable()
    {
        // Arrange
        var attribute = new MultiFactorRateLimitAttribute();

        // Act
        attribute.EnableIpLimiting = false;

        // Assert
        attribute.EnableIpLimiting.Should().BeFalse();
    }

    [Fact]
    public void MultiFactorRateLimitAttribute_EnableUserLimiting_ShouldBeSettable()
    {
        // Arrange
        var attribute = new MultiFactorRateLimitAttribute();

        // Act
        attribute.EnableUserLimiting = false;

        // Assert
        attribute.EnableUserLimiting.Should().BeFalse();
    }

    [Fact]
    public void MultiFactorRateLimitAttribute_EnableSessionLimiting_ShouldBeSettable()
    {
        // Arrange
        var attribute = new MultiFactorRateLimitAttribute();

        // Act
        attribute.EnableSessionLimiting = false;

        // Assert
        attribute.EnableSessionLimiting.Should().BeFalse();
    }

    [Fact]
    public void MultiFactorRateLimitAttribute_EnableUserAgentFingerprinting_ShouldBeSettable()
    {
        // Arrange
        var attribute = new MultiFactorRateLimitAttribute();

        // Act
        attribute.EnableUserAgentFingerprinting = false;

        // Assert
        attribute.EnableUserAgentFingerprinting.Should().BeFalse();
    }

    [Fact]
    public void MultiFactorRateLimitAttribute_EnableNetworkSegmentLimiting_ShouldBeSettable()
    {
        // Arrange
        var attribute = new MultiFactorRateLimitAttribute();

        // Act
        attribute.EnableNetworkSegmentLimiting = true;

        // Assert
        attribute.EnableNetworkSegmentLimiting.Should().BeTrue();
    }

    [Fact]
    public void MultiFactorRateLimitAttribute_CustomRateLimit_ShouldBeSettable()
    {
        // Arrange
        var attribute = new MultiFactorRateLimitAttribute();

        // Act
        attribute.CustomRateLimit = 100;

        // Assert
        attribute.CustomRateLimit.Should().Be(100);
    }

    [Fact]
    public void MultiFactorRateLimitAttribute_CustomTimeWindowMinutes_ShouldBeSettable()
    {
        // Arrange
        var attribute = new MultiFactorRateLimitAttribute();

        // Act
        attribute.CustomTimeWindowMinutes = 30;

        // Assert
        attribute.CustomTimeWindowMinutes.Should().Be(30);
    }

    [Fact]
    public void MultiFactorRateLimitAttribute_ShouldHaveCorrectAttributeUsage()
    {
        // Arrange & Act
        var attributeUsage = typeof(MultiFactorRateLimitAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.Should().NotBeNull("attribute should have AttributeUsage defined");
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Class, "should be applicable to classes");
        attributeUsage.ValidOn.Should().HaveFlag(AttributeTargets.Method, "should be applicable to methods");
        attributeUsage.AllowMultiple.Should().BeFalse("should not allow multiple instances");
    }

    #endregion

    #region SecurityRateLimitConfigAttribute Tests

    [Fact]
    public void SecurityRateLimitConfigAttribute_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var attribute = new SecurityRateLimitConfigAttribute();

        // Assert
        attribute.RequireCaptchaOnViolation.Should().BeTrue("should require CAPTCHA on violation by default");
        attribute.EnableSecurityLogging.Should().BeTrue("should enable security logging by default");
        attribute.BlockSuspiciousUserAgents.Should().BeTrue("should block suspicious user agents by default");
        attribute.PolicyName.Should().Be(RateLimitPolicies.Strict, "should use strict policy by default");
    }

    [Fact]
    public void SecurityRateLimitConfigAttribute_RequireCaptchaOnViolation_ShouldBeSettable()
    {
        // Arrange
        var attribute = new SecurityRateLimitConfigAttribute();

        // Act
        attribute.RequireCaptchaOnViolation = false;

        // Assert
        attribute.RequireCaptchaOnViolation.Should().BeFalse();
    }

    [Fact]
    public void SecurityRateLimitConfigAttribute_EnableSecurityLogging_ShouldBeSettable()
    {
        // Arrange
        var attribute = new SecurityRateLimitConfigAttribute();

        // Act
        attribute.EnableSecurityLogging = false;

        // Assert
        attribute.EnableSecurityLogging.Should().BeFalse();
    }

    [Fact]
    public void SecurityRateLimitConfigAttribute_BlockSuspiciousUserAgents_ShouldBeSettable()
    {
        // Arrange
        var attribute = new SecurityRateLimitConfigAttribute();

        // Act
        attribute.BlockSuspiciousUserAgents = false;

        // Assert
        attribute.BlockSuspiciousUserAgents.Should().BeFalse();
    }

    [Fact]
    public void SecurityRateLimitConfigAttribute_PolicyName_ShouldBeSettable()
    {
        // Arrange
        var attribute = new SecurityRateLimitConfigAttribute();

        // Act
        attribute.PolicyName = RateLimitPolicies.Api;

        // Assert
        attribute.PolicyName.Should().Be(RateLimitPolicies.Api);
    }

    [Fact]
    public void SecurityRateLimitConfigAttribute_ShouldHaveCorrectAttributeUsage()
    {
        // Arrange & Act
        var attributeUsage = typeof(SecurityRateLimitConfigAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.Should().NotBeNull("attribute should have AttributeUsage defined");
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Class, "should be applicable to classes");
        attributeUsage.ValidOn.Should().HaveFlag(AttributeTargets.Method, "should be applicable to methods");
        attributeUsage.AllowMultiple.Should().BeFalse("should not allow multiple instances");
    }

    #endregion

    #region RateLimitingExtensions Tests

    [Fact]
    public void RateLimitingExtensions_ApplyAuthRateLimit_ShouldAcceptControllerInstance()
    {
        // Arrange
        var mockController = new TestController();

        // Act & Assert
        // Should not throw when calling the extension method
        var action = () => mockController.ApplyAuthRateLimit();
        action.Should().NotThrow("extension method should execute without errors");
    }

    [Fact]
    public void RateLimitingExtensions_ApplyApiRateLimit_ShouldAcceptControllerInstance()
    {
        // Arrange
        var mockController = new TestController();

        // Act & Assert
        // Should not throw when calling the extension method
        var action = () => mockController.ApplyApiRateLimit();
        action.Should().NotThrow("extension method should execute without errors");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void RequireCaptchaOnSuspiciousActivityAttribute_WhenAppliedToMethod_ShouldBeRetrievable()
    {
        // Arrange & Act
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.TestActionWithCaptcha));
        var attribute = methodInfo?.GetCustomAttribute<RequireCaptchaOnSuspiciousActivityAttribute>();

        // Assert
        attribute.Should().NotBeNull("attribute should be retrievable from method");
        attribute!.ViolationThreshold.Should().Be(5, "custom threshold should be preserved");
        attribute.TimeWindowMinutes.Should().Be(90, "custom time window should be preserved");
        attribute.RequireForUnauthenticated.Should().BeTrue("custom authentication requirement should be preserved");
    }

    [Fact]
    public void MultiFactorRateLimitAttribute_WhenAppliedToMethod_ShouldBeRetrievable()
    {
        // Arrange & Act
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.TestActionWithMultiFactor));
        var attribute = methodInfo?.GetCustomAttribute<MultiFactorRateLimitAttribute>();

        // Assert
        attribute.Should().NotBeNull("attribute should be retrievable from method");
        attribute!.EnableIpLimiting.Should().BeFalse("custom IP limiting setting should be preserved");
        attribute.EnableNetworkSegmentLimiting.Should().BeTrue("custom network segment setting should be preserved");
    }

    [Fact]
    public void SecurityRateLimitConfigAttribute_WhenAppliedToMethod_ShouldBeRetrievable()
    {
        // Arrange & Act
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.TestActionWithSecurityConfig));
        var attribute = methodInfo?.GetCustomAttribute<SecurityRateLimitConfigAttribute>();

        // Assert
        attribute.Should().NotBeNull("attribute should be retrievable from method");
        attribute!.RequireCaptchaOnViolation.Should().BeFalse("custom CAPTCHA requirement should be preserved");
        attribute.PolicyName.Should().Be(RateLimitPolicies.Api, "custom policy name should be preserved");
        attribute.BlockSuspiciousUserAgents.Should().BeFalse("custom user agent blocking setting should be preserved");
    }

    [Fact]
    public void Attributes_WhenAppliedToClass_ShouldBeRetrievable()
    {
        // Arrange & Act
        var classType = typeof(TestControllerWithClassAttributes);
        var captchaAttribute = classType.GetCustomAttribute<RequireCaptchaOnSuspiciousActivityAttribute>();
        var multiFactorAttribute = classType.GetCustomAttribute<MultiFactorRateLimitAttribute>();
        var securityAttribute = classType.GetCustomAttribute<SecurityRateLimitConfigAttribute>();

        // Assert
        captchaAttribute.Should().NotBeNull("CAPTCHA attribute should be retrievable from class");
        multiFactorAttribute.Should().NotBeNull("multi-factor attribute should be retrievable from class");
        securityAttribute.Should().NotBeNull("security attribute should be retrievable from class");
    }

    #endregion

    #region Test Controller Classes

    /// <summary>
    /// Test controller for validating attribute functionality
    /// </summary>
    private class TestController : Controller
    {
        [RequireCaptchaOnSuspiciousActivity(ViolationThreshold = 5, TimeWindowMinutes = 90, RequireForUnauthenticated = true)]
        public IActionResult TestActionWithCaptcha()
        {
            return Ok();
        }

        [MultiFactorRateLimit(EnableIpLimiting = false, EnableNetworkSegmentLimiting = true)]
        public IActionResult TestActionWithMultiFactor()
        {
            return Ok();
        }

        [SecurityRateLimitConfig(RequireCaptchaOnViolation = false, PolicyName = RateLimitPolicies.Api, BlockSuspiciousUserAgents = false)]
        public IActionResult TestActionWithSecurityConfig()
        {
            return Ok();
        }
    }

    /// <summary>
    /// Test controller with class-level attributes
    /// </summary>
    [RequireCaptchaOnSuspiciousActivity]
    [MultiFactorRateLimit]
    [SecurityRateLimitConfig]
    private class TestControllerWithClassAttributes : Controller
    {
        public IActionResult TestAction()
        {
            return Ok();
        }
    }

    #endregion
}