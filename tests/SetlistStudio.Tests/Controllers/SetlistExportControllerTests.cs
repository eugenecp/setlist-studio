using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Interfaces;
using SetlistStudio.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SetlistStudio.Tests.Controllers;

/// <summary>
/// Comprehensive unit tests for SetlistExportController.
/// Tests export functionality, authorization, and error handling.
/// Ensures proper CSV export API functionality and access control.
/// </summary>
public class SetlistExportControllerTests
{
    private readonly Mock<ISetlistExportService> _mockExportService;
    private readonly Mock<ISetlistService> _mockSetlistService;
    private readonly Mock<ILogger<SetlistExportController>> _mockLogger;
    private readonly SetlistExportController _controller;
    private const string TestUserId = "test-user-123";

    public SetlistExportControllerTests()
    {
        _mockExportService = new Mock<ISetlistExportService>();
        _mockSetlistService = new Mock<ISetlistService>();
        _mockLogger = new Mock<ILogger<SetlistExportController>>();
        _controller = new SetlistExportController(
            _mockExportService.Object,
            _mockSetlistService.Object,
            _mockLogger.Object);

        SetupAuthenticatedUser();
    }

    #region Authorization Tests

    [Fact]
    public async Task ExportToCsv_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var setlistId = 1;

        // Act
        var result = await _controller.ExportToCsv(setlistId);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.Value.Should().Be("User authentication required");
    }

    [Fact]
    public async Task ExportToCsv_ShouldReturnNotFound_WhenSetlistDoesNotExist()
    {
        // Arrange
        var setlistId = 999;
        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync((Setlist?)null);

        // Act
        var result = await _controller.ExportToCsv(setlistId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be($"Setlist with ID {setlistId} not found or you don't have permission to access it");
    }

    [Fact]
    public async Task ExportToCsv_ShouldReturnNotFound_WhenUserDoesNotOwnSetlist()
    {
        // Arrange
        var setlistId = 1;
        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync((Setlist?)null); // Simulates authorization failure

        // Act
        var result = await _controller.ExportToCsv(setlistId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Export Success Tests

    [Fact]
    public async Task ExportToCsv_ShouldReturnFileResult_WhenExportSucceeds()
    {
        // Arrange
        var setlistId = 1;
        var setlist = CreateTestSetlist(setlistId, "Test Setlist");
        var csvBytes = System.Text.Encoding.UTF8.GetBytes("test,csv,data");

        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync(setlist);
        _mockExportService.Setup(s => s.ExportSetlistToCsvAsync(setlistId, TestUserId))
            .ReturnsAsync(csvBytes);
        _mockExportService.Setup(s => s.GenerateCsvFilename(setlist))
            .Returns("setlist_Test_Setlist_2024-01-01.csv");

        // Act
        var result = await _controller.ExportToCsv(setlistId);

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = result as FileContentResult;
        fileResult!.ContentType.Should().Be("text/csv");
        fileResult.FileContents.Should().BeEquivalentTo(csvBytes);
        fileResult.FileDownloadName.Should().Be("setlist_Test_Setlist_2024-01-01.csv");
    }

    [Fact]
    public async Task ExportToCsv_ShouldCallExportServiceWithCorrectParameters()
    {
        // Arrange
        var setlistId = 1;
        var setlist = CreateTestSetlist(setlistId, "Test Setlist");
        var csvBytes = System.Text.Encoding.UTF8.GetBytes("test,csv,data");

        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync(setlist);
        _mockExportService.Setup(s => s.ExportSetlistToCsvAsync(setlistId, TestUserId))
            .ReturnsAsync(csvBytes);
        _mockExportService.Setup(s => s.GenerateCsvFilename(setlist))
            .Returns("test.csv");

        // Act
        await _controller.ExportToCsv(setlistId);

        // Assert
        _mockExportService.Verify(s => s.ExportSetlistToCsvAsync(setlistId, TestUserId), Times.Once);
        _mockExportService.Verify(s => s.GenerateCsvFilename(setlist), Times.Once);
    }

    [Fact]
    public async Task ExportToCsv_ShouldReturnNotFound_WhenExportServiceReturnsNull()
    {
        // Arrange
        var setlistId = 1;
        var setlist = CreateTestSetlist(setlistId, "Test Setlist");

        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync(setlist);
        _mockExportService.Setup(s => s.ExportSetlistToCsvAsync(setlistId, TestUserId))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _controller.ExportToCsv(setlistId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be($"Could not export setlist with ID {setlistId}");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExportToCsv_ShouldReturnBadRequest_WhenArgumentNullExceptionIsThrown()
    {
        // Arrange
        var setlistId = 1;
        var setlist = CreateTestSetlist(setlistId, "Test Setlist");

        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync(setlist);
        _mockExportService.Setup(s => s.ExportSetlistToCsvAsync(setlistId, TestUserId))
            .ThrowsAsync(new ArgumentNullException("testParam"));

        // Act
        var result = await _controller.ExportToCsv(setlistId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Invalid request parameters");
    }

    [Fact]
    public async Task ExportToCsv_ShouldReturnForbid_WhenUnauthorizedAccessExceptionIsThrown()
    {
        // Arrange
        var setlistId = 1;
        var setlist = CreateTestSetlist(setlistId, "Test Setlist");

        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync(setlist);
        _mockExportService.Setup(s => s.ExportSetlistToCsvAsync(setlistId, TestUserId))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.ExportToCsv(setlistId);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ExportToCsv_ShouldReturn503_WhenInvalidOperationExceptionIsThrown()
    {
        // Arrange
        var setlistId = 1;
        var setlist = CreateTestSetlist(setlistId, "Test Setlist");

        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync(setlist);
        _mockExportService.Setup(s => s.ExportSetlistToCsvAsync(setlistId, TestUserId))
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _controller.ExportToCsv(setlistId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
        objectResult.Value.Should().Be("Export service temporarily unavailable");
    }

    [Fact]
    public async Task ExportToCsv_ShouldReturn500_WhenUnexpectedExceptionIsThrown()
    {
        // Arrange
        var setlistId = 1;
        var setlist = CreateTestSetlist(setlistId, "Test Setlist");

        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync(setlist);
        _mockExportService.Setup(s => s.ExportSetlistToCsvAsync(setlistId, TestUserId))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.ExportToCsv(setlistId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        objectResult.Value.Should().Be("An error occurred while exporting the setlist");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task ExportToCsv_ShouldLogInformation_WhenExportSucceeds()
    {
        // Arrange
        var setlistId = 1;
        var setlist = CreateTestSetlist(setlistId, "Test Setlist");
        var csvBytes = System.Text.Encoding.UTF8.GetBytes("test,csv,data");

        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync(setlist);
        _mockExportService.Setup(s => s.ExportSetlistToCsvAsync(setlistId, TestUserId))
            .ReturnsAsync(csvBytes);
        _mockExportService.Setup(s => s.GenerateCsvFilename(setlist))
            .Returns("test.csv");

        // Act
        await _controller.ExportToCsv(setlistId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Export CSV requested")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExportToCsv_ShouldLogWarning_WhenSetlistNotFound()
    {
        // Arrange
        var setlistId = 999;
        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync((Setlist?)null);

        // Act
        await _controller.ExportToCsv(setlistId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found or unauthorized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportToCsv_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var setlistId = 1;
        var setlist = CreateTestSetlist(setlistId, "Test Setlist");

        _mockSetlistService.Setup(s => s.GetSetlistByIdAsync(setlistId, TestUserId))
            .ReturnsAsync(setlist);
        _mockExportService.Setup(s => s.ExportSetlistToCsvAsync(setlistId, TestUserId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _controller.ExportToCsv(setlistId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenExportServiceIsNull()
    {
        // Act
        Action act = () => new SetlistExportController(
            null!,
            _mockSetlistService.Object,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exportService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSetlistServiceIsNull()
    {
        // Act
        Action act = () => new SetlistExportController(
            _mockExportService.Object,
            null!,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("setlistService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        Action act = () => new SetlistExportController(
            _mockExportService.Object,
            _mockSetlistService.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetupUnauthenticatedUser()
    {
        var identity = new ClaimsIdentity(); // Not authenticated
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private Setlist CreateTestSetlist(int id, string name)
    {
        return new Setlist
        {
            Id = id,
            Name = name,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            SetlistSongs = new List<SetlistSong>()
        };
    }

    #endregion
}
