using System;
using System.IO;
using FluentAssertions;
using SetlistStudio.Core.Security;
using Xunit;

namespace SetlistStudio.Tests.Core.Security;

/// <summary>
/// Comprehensive tests for DatabasePathValidator security functionality
/// Tests path validation, sanitization, and protection against path traversal attacks
/// </summary>
public class DatabasePathValidatorTests
{
    #region GetSecureDatabaseConnectionString Tests

    [Fact]
    public void GetSecureDatabaseConnectionString_ContainerizedEnvironment_ReturnsSecureContainerPath()
    {
        // Act
        var result = DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: true);

        // Assert
        result.Should().Be("Data Source=/app/data/setliststudio.db");
    }

    [Fact]
    public void GetSecureDatabaseConnectionString_LocalEnvironment_ReturnsSecureLocalPath()
    {
        // Act
        var result = DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: false);

        // Assert
        result.Should().StartWith("Data Source=");
        result.Should().EndWith("setliststudio.db");
        result.Should().Contain("Data");
    }

    [Fact]
    public void GetSecureDatabaseConnectionString_ValidCustomPath_ReturnsCustomPath()
    {
        // Arrange
        var customPath = Path.Combine(Path.GetTempPath(), "custom.db");

        // Act
        var result = DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: false, customPath);

        // Assert
        result.Should().StartWith("Data Source=");
        result.Should().Contain("custom.db");
    }

    [Fact]
    public void GetSecureDatabaseConnectionString_InvalidCustomPath_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "../../../etc/passwd";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: false, maliciousPath));

        exception.Message.Should().Contain("dangerous characters or patterns");
    }

    [Fact]
    public void GetSecureDatabaseConnectionString_NullCustomPath_UsesDefaults()
    {
        // Act
        var result = DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: true, null);

        // Assert
        result.Should().Be("Data Source=/app/data/setliststudio.db");
    }

    [Fact]
    public void GetSecureDatabaseConnectionString_EmptyCustomPath_UsesDefaults()
    {
        // Act
        var result = DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: true, "");

        // Assert
        result.Should().Be("Data Source=/app/data/setliststudio.db");
    }

    #endregion

    #region IsSecurePath Tests

    [Theory]
    [InlineData("setliststudio.db", true)]
    [InlineData("test-database.db", true)]
    [InlineData("my_app_data.db", true)]
    [InlineData("database123.db", true)]
    [InlineData("app.data.db", true)]
    public void IsSecurePath_ValidFilenames_ReturnsTrue(string path, bool expected)
    {
        // Act
        var result = DatabasePathValidator.IsSecurePath(path);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("../../../etc/passwd", false)]
    [InlineData("..\\..\\windows\\system32", false)]
    [InlineData("/etc/passwd", false)]
    [InlineData("C:\\Windows\\System32\\config", false)]
    [InlineData("database.exe", false)]
    [InlineData("malicious<script>.db", false)]
    [InlineData("path|with|pipes.db", false)]
    [InlineData("path\"with\"quotes.db", false)]
    [InlineData("path?with?query.db", false)]
    [InlineData("path*with*wildcards.db", false)]
    public void IsSecurePath_DangerousPaths_ReturnsFalse(string path, bool expected)
    {
        // Act
        var result = DatabasePathValidator.IsSecurePath(path);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsSecurePath_NullPath_ReturnsFalse()
    {
        // Act
        var result = DatabasePathValidator.IsSecurePath(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSecurePath_EmptyPath_ReturnsFalse()
    {
        // Act
        var result = DatabasePathValidator.IsSecurePath("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSecurePath_WhitespacePath_ReturnsFalse()
    {
        // Act
        var result = DatabasePathValidator.IsSecurePath("   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSecurePath_ExcessivelyLongPath_ReturnsFalse()
    {
        // Arrange
        var longPath = new string('a', 300) + ".db";

        // Act
        var result = DatabasePathValidator.IsSecurePath(longPath);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Path Traversal Attack Tests

    [Theory]
    [InlineData("../database.db")]
    [InlineData("../../config/database.db")]
    [InlineData("..\\..\\database.db")]
    [InlineData("/../../../../etc/shadow")]
    [InlineData("C:\\..\\..\\Windows\\System32\\database.db")]
    public void GetSecureDatabaseConnectionString_PathTraversalAttempts_ThrowsArgumentException(string maliciousPath)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: false, maliciousPath));

        exception.Message.Should().Contain("dangerous characters or patterns");
    }

    [Theory]
    [InlineData("database<script>.db")]
    [InlineData("database|evil.db")]
    [InlineData("database\"injection.db")]
    [InlineData("database?query=evil.db")]
    [InlineData("database*wildcard.db")]
    [InlineData("database>redirect.db")]
    public void GetSecureDatabaseConnectionString_InjectionAttempts_ThrowsArgumentException(string maliciousPath)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: false, maliciousPath));

        exception.Message.Should().Contain("dangerous characters or patterns");
    }

    [Theory]
    [InlineData("database:colon.db")]
    [InlineData("database with spaces.db")]
    [InlineData("database@symbol.db")]
    [InlineData("database#hash.db")]
    [InlineData("database$dollar.db")]
    [InlineData("database%percent.db")]
    [InlineData("database^caret.db")]
    [InlineData("database&ampersand.db")]
    [InlineData("database(paren.db")]
    [InlineData("database)paren.db")]
    [InlineData("database=equals.db")]
    [InlineData("database+plus.db")]
    [InlineData("database[bracket.db")]
    [InlineData("database]bracket.db")]
    [InlineData("database{brace.db")]
    [InlineData("database}brace.db")]  
    [InlineData("database;semicolon.db")]
    [InlineData("database,comma.db")]
    public void GetSecureDatabaseConnectionString_InvalidFilenames_ThrowsArgumentException(string invalidPath)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: false, invalidPath));

        exception.Message.Should().Contain("invalid characters");
    }

    #endregion

    #region Validation Edge Cases

    [Fact]
    public void GetSecureDatabaseConnectionString_NullWhitespaceCustomPath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: false, "   "));

        exception.Message.Should().Contain("cannot be null or whitespace");
    }

    [Fact]
    public void GetSecureDatabaseConnectionString_ExcessivelyLongPath_ThrowsArgumentException()
    {
        // Arrange
        var longPath = new string('a', 300) + ".db";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: false, longPath));

        exception.Message.Should().Contain("exceeds maximum length");
    }

    [Theory]
    [InlineData("database.txt")]
    [InlineData("database.exe")]
    [InlineData("database")]
    [InlineData("database.")]
    [InlineData(".db")]
    public void GetSecureDatabaseConnectionString_InvalidFileExtension_ThrowsArgumentException(string invalidPath)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: false, invalidPath));

        exception.Message.Should().Contain("invalid characters");
    }

    #endregion

    #region ValidateDatabaseDirectory Tests

    [Fact]
    public void ValidateDatabaseDirectory_ValidPath_ReturnsTrue()
    {
        // Arrange
        var tempPath = Path.Join(Path.GetTempPath(), "test.db");

        // Act
        var result = DatabasePathValidator.ValidateDatabaseDirectory(tempPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateDatabaseDirectory_InvalidPath_ReturnsFalse()
    {
        // Arrange - Use a path that should not exist and cannot be created
        var invalidPath = "Z:\\NonExistent\\Path\\database.db";

        // Act
        var result = DatabasePathValidator.ValidateDatabaseDirectory(invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateDatabaseDirectory_EmptyDirectory_ReturnsFalse()
    {
        // Act
        var result = DatabasePathValidator.ValidateDatabaseDirectory("database.db");

        // Assert - This should return false because GetDirectoryName returns null for just a filename
        result.Should().BeFalse();
    }

    #endregion

    #region Security Boundary Tests

    [Theory]
    [InlineData("valid-database.db")]
    [InlineData("app_database.db")]
    [InlineData("test123.db")]
    [InlineData("my.app.database.db")]
    public void IsSecurePath_ValidDatabaseNames_ReturnsTrue(string validName)
    {
        // Act
        var result = DatabasePathValidator.IsSecurePath(validName);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("database with spaces.db")]
    [InlineData("database@symbol.db")]
    [InlineData("database#hash.db")]
    [InlineData("database$dollar.db")]
    [InlineData("database%percent.db")]
    [InlineData("database^caret.db")]
    [InlineData("database&ampersand.db")]
    [InlineData("database(paren.db")]
    [InlineData("database)paren.db")]
    [InlineData("database=equals.db")]
    [InlineData("database+plus.db")]
    [InlineData("database[bracket.db")]
    [InlineData("database]bracket.db")]
    [InlineData("database{brace.db")]
    [InlineData("database}brace.db")]
    [InlineData("database;semicolon.db")]
    [InlineData("database,comma.db")]
    [InlineData("database:colon.db")]
    public void IsSecurePath_InvalidCharacters_ReturnsFalse(string invalidName)
    {
        // Act
        var result = DatabasePathValidator.IsSecurePath(invalidName);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Container vs Local Environment Tests

    [Fact]
    public void GetSecureDatabaseConnectionString_ContainerEnvironment_UsesAppDataPath()
    {
        // Act
        var result = DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: true);

        // Assert
        result.Should().Contain("/app/data/");
        result.Should().EndWith("setliststudio.db");
    }

    [Fact]
    public void GetSecureDatabaseConnectionString_LocalEnvironment_UsesDataSubdirectory()
    {
        // Act
        var result = DatabasePathValidator.GetSecureDatabaseConnectionString(isContainerized: false);

        // Assert
        result.Should().Contain("Data");
        result.Should().EndWith("setliststudio.db");
        result.Should().StartWith("Data Source=");
        result.Should().NotContain("/app/data"); // Should not use container path
    }

    #endregion
}