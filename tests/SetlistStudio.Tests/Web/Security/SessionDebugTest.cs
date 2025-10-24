using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using Xunit;
using SetlistStudio.Web;
using System.Net.Http;

namespace SetlistStudio.Tests.Web.Security;

/// <summary>
/// Debug test to understand why SessionSecurityTests are failing
/// </summary>
public class SessionDebugTest
{
    [Fact]
    public async Task Debug_SessionSecurity_TestSession_Response()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing"); // Use Testing environment to match CI
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                        ["Authentication:Google:ClientId"] = "test-client-id",
                        ["Authentication:Google:ClientSecret"] = "test-client-secret",
                        ["AllowedHosts"] = "*"
                    });
                });
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "SetlistStudio-Debug-Client/1.0");

        // Act
        var requestContent = new StringContent("\"test-data\"", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/test-session", requestContent);

        // Debug: Log actual response details
        var responseContent = await response.Content.ReadAsStringAsync();
        var statusCode = response.StatusCode;
        var headers = response.Headers.ToString();
        
        // Assert with debug information
        response.IsSuccessStatusCode.Should().BeTrue($"Expected success but got {statusCode}. Content: {responseContent}. Headers: {headers}");
    }
}