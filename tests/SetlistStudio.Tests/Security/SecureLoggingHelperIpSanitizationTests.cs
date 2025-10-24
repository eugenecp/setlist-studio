using FluentAssertions;
using SetlistStudio.Core.Security;
using Xunit;

namespace SetlistStudio.Tests.Security;

/// <summary>
/// Tests for SecureLoggingHelper IP address sanitization functionality.
/// Validates that IP addresses are properly sanitized to protect user privacy
/// while maintaining security monitoring capabilities.
/// </summary>
public class SecureLoggingHelperIpSanitizationTests
{
    #region IPv4 Sanitization Tests

    [Theory]
    [InlineData("192.168.1.100", "192.168.1.xxx")]
    [InlineData("10.0.0.1", "10.0.0.xxx")]
    [InlineData("172.16.254.1", "172.16.254.xxx")]
    [InlineData("203.0.113.195", "203.0.113.xxx")]
    public void SanitizeIpAddress_ShouldMaskLastOctet_ForIPv4Addresses(string input, string expected)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeIpAddress(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SanitizeIpAddress_ShouldMaskLocalhost_IPv4()
    {
        // Arrange
        var localhost = "127.0.0.1";

        // Act
        var result = SecureLoggingHelper.SanitizeIpAddress(localhost);

        // Assert
        result.Should().Be("127.0.0.xxx");
    }

    #endregion

    #region IPv6 Sanitization Tests

    [Theory]
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334", "2001:0db8:85a3:xxxx")]
    [InlineData("2001:db8::1", "2001:db8::xxxx")]
    [InlineData("fe80::1%lo0", "fe80::1:xxxx")]
    [InlineData("::1", "::xxxx")]
    public void SanitizeIpAddress_ShouldMaskLastSegments_ForIPv6Addresses(string input, string expected)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeIpAddress(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Edge Cases Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeIpAddress_ShouldReturnUnknown_ForNullOrEmptyInput(string? input)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeIpAddress(input);

        // Assert
        result.Should().Be("unknown");
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("192.168.1")]
    [InlineData("192.168.1.1.1")]
    [InlineData("malicious<script>")]
    [InlineData("256.256.256.256")]
    public void SanitizeIpAddress_ShouldReturnMaskedIp_ForInvalidInput(string input)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeIpAddress(input);

        // Assert
        result.Should().Be("[MASKED-IP]");
    }

    [Fact]
    public void SanitizeIpAddress_ShouldHandleXssInIpInput()
    {
        // Arrange
        var maliciousInput = "192.168.1.1<script>alert('xss')</script>";

        // Act
        var result = SecureLoggingHelper.SanitizeIpAddress(maliciousInput);

        // Assert
        result.Should().Be("[MASKED-IP]");
        result.Should().NotContain("<script>");
        result.Should().NotContain("alert");
    }

    #endregion

    #region Privacy Protection Tests

    [Fact]
    public void SanitizeIpAddress_ShouldProtectUserPrivacy_WhileMaintainingNetworkInfo()
    {
        // Arrange
        var realIp = "203.0.113.45";

        // Act
        var result = SecureLoggingHelper.SanitizeIpAddress(realIp);

        // Assert
        result.Should().Be("203.0.113.xxx");
        result.Should().StartWith("203.0.113"); // Network portion preserved
        result.Should().NotContain("45"); // Host portion masked
    }

    [Fact]
    public void SanitizeIpAddress_ShouldPreserveNetworkSegment_ForIPv4()
    {
        // Arrange
        var corporateIp = "192.168.100.50";

        // Act
        var result = SecureLoggingHelper.SanitizeIpAddress(corporateIp);

        // Assert
        result.Should().Be("192.168.100.xxx");
        // This allows network administrators to identify the network segment
        // while protecting individual host identification
    }

    [Fact]
    public void SanitizeIpAddress_ShouldPreserveNetworkPrefix_ForIPv6()
    {
        // Arrange
        var ipv6 = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";

        // Act
        var result = SecureLoggingHelper.SanitizeIpAddress(ipv6);

        // Assert
        result.Should().Be("2001:0db8:85a3:xxxx");
        result.Should().StartWith("2001:0db8:85a3"); // Network prefix preserved
        result.Should().NotContain("8a2e"); // Host portion masked
        result.Should().NotContain("0370");
        result.Should().NotContain("7334");
    }

    #endregion

    #region Security Tests

    [Fact]
    public void SanitizeIpAddress_ShouldNotLeakOriginalIp_InResult()
    {
        // Arrange
        var sensitiveIp = "198.51.100.123";

        // Act
        var result = SecureLoggingHelper.SanitizeIpAddress(sensitiveIp);

        // Assert
        result.Should().NotContain("123"); // Last octet should not appear
        result.Should().Be("198.51.100.xxx");
    }

    [Theory]
    [InlineData("192.168.1.1\r\n")]
    [InlineData("192.168.1.1\n")]
    [InlineData("192.168.1.1\t")]
    public void SanitizeIpAddress_ShouldHandleControlCharacters(string input)
    {
        // Act
        var result = SecureLoggingHelper.SanitizeIpAddress(input);

        // Assert
        result.Should().NotContain("\r");
        result.Should().NotContain("\n");
        result.Should().NotContain("\t");
        // Should either be properly sanitized IP or masked
        result.Should().Match(r => r == "192.168.1.xxx" || r == "[MASKED-IP]");
    }

    [Fact]
    public void SanitizeIpAddress_ShouldProvideConsistentOutput_ForSameInput()
    {
        // Arrange
        var ip = "10.0.0.100";

        // Act
        var result1 = SecureLoggingHelper.SanitizeIpAddress(ip);
        var result2 = SecureLoggingHelper.SanitizeIpAddress(ip);

        // Assert
        result1.Should().Be(result2);
        result1.Should().Be("10.0.0.xxx");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void SanitizeIpAddress_ShouldHandleLargeInput_Efficiently()
    {
        // Arrange
        var largeInput = new string('1', 1000) + ".2.3.4";

        // Act & Assert - Should not throw and should complete quickly
        var result = SecureLoggingHelper.SanitizeIpAddress(largeInput);
        result.Should().Be("[MASKED-IP]");
    }

    #endregion
}