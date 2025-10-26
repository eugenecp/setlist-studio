using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using SetlistStudio.Web.Utilities;
using System.Net;
using Xunit;
using FluentAssertions;

namespace SetlistStudio.Tests.Web.Utilities;

/// <summary>
/// Unit tests for IpAddressUtility class
/// Tests IP address extraction from various HTTP headers and connection contexts
/// </summary>
public class IpAddressUtilityTests
{
    /// <summary>
    /// Creates a mock HttpContext with specified headers and connection IP
    /// </summary>
    private static HttpContext CreateMockContext(
        Dictionary<string, StringValues>? headers = null,
        IPAddress? remoteIpAddress = null)
    {
        var context = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();
        var connection = new Mock<ConnectionInfo>();
        var headerDictionary = new HeaderDictionary();

        if (headers != null)
        {
            foreach (var header in headers)
            {
                headerDictionary[header.Key] = header.Value;
            }
        }

        request.Setup(r => r.Headers).Returns(headerDictionary);
        connection.Setup(c => c.RemoteIpAddress).Returns(remoteIpAddress);
        context.Setup(c => c.Request).Returns(request.Object);
        context.Setup(c => c.Connection).Returns(connection.Object);

        return context.Object;
    }

    [Fact]
    public void GetClientIpAddress_ShouldReturnForwardedIp_WhenXForwardedForHeaderExists()
    {
        // Arrange
        var expectedIp = "192.168.1.100";
        var headers = new Dictionary<string, StringValues>
        {
            ["X-Forwarded-For"] = expectedIp
        };
        var context = CreateMockContext(headers);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be(expectedIp);
    }

    [Fact]
    public void GetClientIpAddress_ShouldReturnFirstIp_WhenXForwardedForHasMultipleIps()
    {
        // Arrange
        var expectedIp = "203.0.113.195";
        var headers = new Dictionary<string, StringValues>
        {
            ["X-Forwarded-For"] = "203.0.113.195, 70.41.3.18, 150.172.238.178"
        };
        var context = CreateMockContext(headers);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be(expectedIp);
    }

    [Fact]
    public void GetClientIpAddress_ShouldHandleWhitespace_WhenXForwardedForHasSpaces()
    {
        // Arrange
        var expectedIp = "198.51.100.42";
        var headers = new Dictionary<string, StringValues>
        {
            ["X-Forwarded-For"] = "  198.51.100.42  ,  192.168.1.1  "
        };
        var context = CreateMockContext(headers);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be(expectedIp);
    }

    [Fact]
    public void GetClientIpAddress_ShouldReturnRealIp_WhenXRealIpHeaderExists()
    {
        // Arrange
        var expectedIp = "10.0.0.50";
        var headers = new Dictionary<string, StringValues>
        {
            ["X-Real-IP"] = expectedIp
        };
        var context = CreateMockContext(headers);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be(expectedIp);
    }

    [Fact]
    public void GetClientIpAddress_ShouldPrioritizeForwardedIp_WhenBothHeadersExist()
    {
        // Arrange
        var forwardedIp = "172.16.0.10";
        var realIp = "192.168.1.20";
        var headers = new Dictionary<string, StringValues>
        {
            ["X-Forwarded-For"] = forwardedIp,
            ["X-Real-IP"] = realIp
        };
        var context = CreateMockContext(headers);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be(forwardedIp, "X-Forwarded-For should take priority over X-Real-IP");
    }

    [Fact]
    public void GetClientIpAddress_ShouldReturnRemoteConnectionIp_WhenNoHeadersPresent()
    {
        // Arrange
        var expectedIp = IPAddress.Parse("127.0.0.1");
        var context = CreateMockContext(remoteIpAddress: expectedIp);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be("127.0.0.1");
    }

    [Fact]
    public void GetClientIpAddress_ShouldReturnUnknown_WhenNoIpAvailable()
    {
        // Arrange
        var context = CreateMockContext();

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be("Unknown");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void GetClientIpAddress_ShouldIgnoreEmptyHeaders_AndFallbackToConnection(string emptyValue)
    {
        // Arrange
        var expectedIp = IPAddress.Parse("192.168.1.99");
        var headers = new Dictionary<string, StringValues>
        {
            ["X-Forwarded-For"] = emptyValue,
            ["X-Real-IP"] = emptyValue
        };
        var context = CreateMockContext(headers, expectedIp);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be("192.168.1.99");
    }

    [Fact]
    public void GetClientIpAddress_ShouldHandleCommaOnlyForwardedFor_AndFallbackToRealIp()
    {
        // Arrange
        var expectedIp = "10.1.1.100";
        var headers = new Dictionary<string, StringValues>
        {
            ["X-Forwarded-For"] = ",,,",
            ["X-Real-IP"] = expectedIp
        };
        var context = CreateMockContext(headers);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be(expectedIp);
    }

    [Fact]
    public void GetClientIpAddress_ShouldHandleIpv6Address()
    {
        // Arrange
        var expectedIp = "2001:db8:85a3::8a2e:370:7334";
        var headers = new Dictionary<string, StringValues>
        {
            ["X-Forwarded-For"] = expectedIp
        };
        var context = CreateMockContext(headers);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be(expectedIp);
    }

    [Fact]
    public void GetClientIpAddress_ShouldHandleLoopbackAddress()
    {
        // Arrange
        var expectedIp = IPAddress.Loopback;
        var context = CreateMockContext(remoteIpAddress: expectedIp);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be("127.0.0.1");
    }

    [Fact]
    public void GetClientIpAddress_ShouldHandleIpv6Loopback()
    {
        // Arrange
        var expectedIp = IPAddress.IPv6Loopback;
        var context = CreateMockContext(remoteIpAddress: expectedIp);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be("::1");
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("203.0.113.1")]
    public void GetClientIpAddress_ShouldHandleVariousValidIpAddresses(string ipAddress)
    {
        // Arrange
        var headers = new Dictionary<string, StringValues>
        {
            ["X-Forwarded-For"] = ipAddress
        };
        var context = CreateMockContext(headers);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ipAddress);
    }

    [Fact]
    public void GetClientIpAddress_ShouldHandleNullForwardedForValue()
    {
        // Arrange
        var expectedIp = "192.168.1.50";
        var headers = new Dictionary<string, StringValues>
        {
            ["X-Real-IP"] = expectedIp
        };
        var context = CreateMockContext(headers);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be(expectedIp);
    }

    [Fact]
    public void GetClientIpAddress_ShouldHandleMixedValidAndInvalidEntries()
    {
        // Arrange
        var expectedIp = "198.51.100.1";
        var headers = new Dictionary<string, StringValues>
        {
            ["X-Forwarded-For"] = $" , {expectedIp}, invalid-ip, 192.168.1.1"
        };
        var context = CreateMockContext(headers);

        // Act
        var result = IpAddressUtility.GetClientIpAddress(context);

        // Assert
        result.Should().Be(expectedIp, "should return first non-empty IP in forwarded list");
    }
}