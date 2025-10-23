using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SetlistStudio.Core.Security;

/// <summary>
/// Provides secure database path validation and sanitization to prevent path traversal attacks
/// </summary>
public static class DatabasePathValidator
{
    /// <summary>
    /// Valid database filename pattern (alphanumeric, hyphens, underscores, periods)
    /// </summary>
    private static readonly Regex ValidDatabaseFilenamePattern = new(@"^[a-zA-Z0-9._-]+\.db$", RegexOptions.Compiled);
    
    /// <summary>
    /// Dangerous path patterns that indicate potential path traversal attacks
    /// </summary>
    private static readonly Regex DangerousPathPatterns = new(@"\.\.|[<>""|?*]|\.\.[\\/]|[\\/]\.\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    /// <summary>
    /// Maximum allowed path length to prevent buffer overflow attacks
    /// </summary>
    private const int MaxPathLength = 260;
    
    /// <summary>
    /// Default secure database directory for containerized environments
    /// </summary>
    private const string SecureContainerDataPath = "/app/data";
    
    /// <summary>
    /// Default secure database directory for local development
    /// </summary>
    private const string SecureLocalDataPath = "Data";
    
    /// <summary>
    /// Default database filename
    /// </summary>
    private const string DefaultDatabaseFilename = "setliststudio.db";
    
    /// <summary>
    /// Validates and constructs a secure database connection string
    /// </summary>
    /// <param name="isContainerized">Whether the application is running in a container</param>
    /// <param name="customPath">Optional custom database path (will be validated)</param>
    /// <returns>Secure database connection string</returns>
    /// <exception cref="ArgumentException">Thrown when path validation fails</exception>
    public static string GetSecureDatabaseConnectionString(bool isContainerized, string? customPath = null)
    {
        string databasePath;
        
        if (!string.IsNullOrEmpty(customPath))
        {
            // Validate and sanitize custom path
            databasePath = ValidateAndSanitizePath(customPath);
        }
        else
        {
            // Use secure default paths
            databasePath = GetSecureDefaultPath(isContainerized);
        }
        
        return $"Data Source={databasePath}";
    }
    
    /// <summary>
    /// Validates a database path for security vulnerabilities
    /// </summary>
    /// <param name="path">Path to validate</param>
    /// <returns>True if path is secure, false otherwise</returns>
    public static bool IsSecurePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
            
        if (path.Length > MaxPathLength)
            return false;
            
        // Check for dangerous patterns
        if (DangerousPathPatterns.IsMatch(path))
            return false;
            
        // Validate filename if it's just a filename
        var filename = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(filename) && !ValidDatabaseFilenamePattern.IsMatch(filename))
            return false;
            
        return true;
    }
    
    /// <summary>
    /// Validates and sanitizes a database path, ensuring it's secure
    /// </summary>
    /// <param name="path">Path to validate and sanitize</param>
    /// <returns>Sanitized secure path</returns>
    /// <exception cref="ArgumentException">Thrown when path cannot be made secure</exception>
    private static string ValidateAndSanitizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Database path cannot be null or whitespace", nameof(path));
            
        if (path.Length > MaxPathLength)
            throw new ArgumentException($"Database path exceeds maximum length of {MaxPathLength} characters", nameof(path));
        
        // Check for dangerous patterns
        if (DangerousPathPatterns.IsMatch(path))
            throw new ArgumentException("Database path contains potentially dangerous characters or patterns", nameof(path));
        
        try
        {
            // Normalize the path to handle different path separators
            var normalizedPath = Path.GetFullPath(path);
            
            // Ensure the filename is valid
            var filename = Path.GetFileName(normalizedPath);
            if (!string.IsNullOrEmpty(filename) && !ValidDatabaseFilenamePattern.IsMatch(filename))
                throw new ArgumentException("Database filename contains invalid characters. Only alphanumeric characters, hyphens, underscores, periods, and .db extension are allowed", nameof(path));
            
            // Ensure the path doesn't escape the intended directory structure
            if (normalizedPath.Contains(".."))
                throw new ArgumentException("Database path cannot contain parent directory references", nameof(path));
                
            return normalizedPath;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid database path format", nameof(path), ex);
        }
    }
    
    /// <summary>
    /// Gets the secure default database path based on environment
    /// </summary>
    /// <param name="isContainerized">Whether the application is running in a container</param>
    /// <returns>Secure default database path</returns>
    private static string GetSecureDefaultPath(bool isContainerized)
    {
        if (isContainerized)
        {
            // Container environment - use fixed secure path with Unix-style separators
            return $"{SecureContainerDataPath}/{DefaultDatabaseFilename}";
        }
        else
        {
            // Local development - create secure data directory
            var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), SecureLocalDataPath);
            
            // Ensure the data directory exists
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }
            
            return Path.Combine(dataDirectory, DefaultDatabaseFilename);
        }
    }
    
    /// <summary>
    /// Validates that a database directory is secure and accessible
    /// </summary>
    /// <param name="path">Database file path</param>
    /// <returns>True if directory is secure and accessible</returns>
    public static bool ValidateDatabaseDirectory(string path)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
                return false;
                
            // Check if directory exists or can be created
            if (!Directory.Exists(directory))
            {
                // Try to create the directory to test permissions
                Directory.CreateDirectory(directory);
            }
            
            // Test write permissions
            var testFile = Path.Combine(directory, $"test_write_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}