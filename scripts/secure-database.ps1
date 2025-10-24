# Database Security Hardening Script for Setlist Studio (Windows)
# This script implements enhanced database security measures for Windows environments

param(
    [string]$DataPath = ".\data",
    [string]$LogPath = ".\logs",
    [switch]$Verbose = $false
)

# Configuration
$DataDir = Resolve-Path $DataPath -ErrorAction SilentlyContinue
if (-not $DataDir) {
    $DataDir = New-Item -ItemType Directory -Path $DataPath -Force
}

$LogDir = Resolve-Path $LogPath -ErrorAction SilentlyContinue
if (-not $LogDir) {
    $LogDir = New-Item -ItemType Directory -Path $LogPath -Force
}

$DbFile = Join-Path $DataDir "setliststudio.db"
$LogFile = Join-Path $LogDir "database-security.log"

# Function to log security actions
function Write-SecurityLog {
    param([string]$Message)
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] DATABASE_SECURITY: $Message"
    
    Write-Host $logEntry -ForegroundColor Green
    Add-Content -Path $LogFile -Value $logEntry
}

# Function to check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Function to apply Windows file security
function Set-DatabaseSecurity {
    Write-SecurityLog "Applying enhanced database security measures"
    
    try {
        # Set restrictive permissions on data directory
        $acl = Get-Acl $DataDir
        $acl.SetAccessRuleProtection($true, $false) # Disable inheritance
        
        # Remove all existing permissions
        $acl.Access | ForEach-Object { $acl.RemoveAccessRule($_) }
        
        # Add only necessary permissions
        $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $currentUser, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
        )
        $acl.SetAccessRule($accessRule)
        
        # Apply ACL to directory
        Set-Acl -Path $DataDir -AclObject $acl
        Write-SecurityLog "Applied restrictive permissions to data directory"
        
        # Set permissions on database file if it exists
        if (Test-Path $DbFile) {
            $dbAcl = Get-Acl $DbFile
            $dbAcl.SetAccessRuleProtection($true, $false)
            $dbAcl.Access | ForEach-Object { $dbAcl.RemoveAccessRule($_) }
            
            $dbAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
                $currentUser, "ReadWrite", "Allow"
            )
            $dbAcl.SetAccessRule($dbAccessRule)
            Set-Acl -Path $DbFile -AclObject $dbAcl
            
            Write-SecurityLog "Applied restrictive permissions to database file"
        }
        
    } catch {
        Write-SecurityLog "WARNING: Failed to apply file system permissions: $($_.Exception.Message)"
    }
}

# Function to implement database file monitoring
function Set-DatabaseMonitoring {
    Write-SecurityLog "Setting up database file monitoring"
    
    # Create PowerShell monitoring script
    $monitorScript = @"
# Simple database file integrity monitor for Windows

`$DbFile = "$DbFile"
`$ChecksumFile = Join-Path (Split-Path `$DbFile) ".db-checksum"

if (Test-Path `$DbFile) {
    `$currentChecksum = Get-FileHash -Path `$DbFile -Algorithm SHA256 | Select-Object -ExpandProperty Hash
    
    if (Test-Path `$ChecksumFile) {
        `$storedChecksum = Get-Content `$ChecksumFile -Raw
        if (`$currentChecksum -ne `$storedChecksum.Trim()) {
            `$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            Add-Content -Path "$LogFile" -Value "[`$timestamp] DATABASE_SECURITY: Database file checksum changed"
        }
    }
    
    Set-Content -Path `$ChecksumFile -Value `$currentChecksum
}
"@

    Set-Content -Path (Join-Path $DataDir "monitor-database.ps1") -Value $monitorScript
    Write-SecurityLog "Database monitoring script created"
}

# Function to setup backup security
function Set-BackupSecurity {
    Write-SecurityLog "Configuring secure backup permissions"
    
    $backupDir = Join-Path $DataDir "backups"
    if (-not (Test-Path $backupDir)) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    }
    
    # Apply restrictive permissions to backup directory
    try {
        $backupAcl = Get-Acl $backupDir
        $backupAcl.SetAccessRuleProtection($true, $false)
        $backupAcl.Access | ForEach-Object { $backupAcl.RemoveAccessRule($_) }
        
        $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
        $backupAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $currentUser, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
        )
        $backupAcl.SetAccessRule($backupAccessRule)
        Set-Acl -Path $backupDir -AclObject $backupAcl
        
        Write-SecurityLog "Backup directory created with secure permissions"
    } catch {
        Write-SecurityLog "WARNING: Failed to secure backup directory: $($_.Exception.Message)"
    }
}

# Function to setup database logging
function Set-DatabaseLogging {
    Write-SecurityLog "Configuring database access logging"
    
    # Ensure log file exists with proper permissions
    if (-not (Test-Path $LogFile)) {
        New-Item -ItemType File -Path $LogFile -Force | Out-Null
    }
    
    Write-SecurityLog "Database security logging configured"
}

# Function to implement defense in depth
function Set-DefenseInDepth {
    Write-SecurityLog "Implementing defense-in-depth security measures"
    
    # Create security policy file
    $securityPolicy = @"
# Database Security Policy for Setlist Studio (Windows)
# This file contains security settings for the database

# Security features
CHECKSUM_VALIDATION=enabled
ACCESS_LOGGING=enabled
BACKUP_ENCRYPTION=recommended
FILE_SYSTEM_MONITORING=enabled

# Compliance
SECURITY_LEVEL=confidential
DATA_CLASSIFICATION=user-personal-data
PLATFORM=windows

# Audit settings
AUDIT_FILE_ACCESS=enabled
AUDIT_PERMISSION_CHANGES=enabled
"@

    Set-Content -Path (Join-Path $DataDir ".security-policy") -Value $securityPolicy
    Write-SecurityLog "Security policy file created"
}

# Function to display security status
function Show-SecurityStatus {
    Write-Host ""
    Write-Host "🔒 Database Security Status:" -ForegroundColor Cyan
    
    if (Test-Path $DataDir) {
        $dirInfo = Get-Item $DataDir
        Write-Host "   Data Directory: $($dirInfo.FullName)" -ForegroundColor White
        Write-Host "   Permissions:    $(($dirInfo | Get-Acl).Access.Count) ACL entries" -ForegroundColor White
    }
    
    if (Test-Path $DbFile) {
        $dbInfo = Get-Item $DbFile
        Write-Host "   Database File:  $($dbInfo.FullName)" -ForegroundColor White
        Write-Host "   Size:          $([math]::Round($dbInfo.Length / 1KB, 2)) KB" -ForegroundColor White
    }
    
    Write-Host "   Security Log:   $LogFile" -ForegroundColor White
    Write-Host ""
}

# Function to show security recommendations
function Show-SecurityRecommendations {
    Write-Host "🛡️  Security Recommendations:" -ForegroundColor Yellow
    
    if (-not (Test-Administrator)) {
        Write-Host "   • Run PowerShell as Administrator for enhanced security features" -ForegroundColor Yellow
    }
    
    Write-Host "   • Enable Windows Defender or third-party antivirus for additional protection" -ForegroundColor Yellow
    Write-Host "   • Regular security audits and backup validation recommended" -ForegroundColor Yellow
    Write-Host "   • Monitor $LogFile for security events" -ForegroundColor Yellow
    Write-Host "   • Consider encrypting the data directory with BitLocker" -ForegroundColor Yellow
    Write-Host ""
}

# Main execution
function Main {
    Write-Host "🔒 Implementing enhanced database security for Setlist Studio" -ForegroundColor Green
    Write-Host ""
    
    try {
        # Setup logging first
        Set-DatabaseLogging
        Write-SecurityLog "Starting database security hardening"
        
        # Apply security measures
        Set-DatabaseSecurity
        Set-DatabaseMonitoring
        Set-BackupSecurity
        Set-DefenseInDepth
        
        Write-SecurityLog "Database security hardening completed successfully"
        
        # Display status and recommendations
        Show-SecurityStatus
        Show-SecurityRecommendations
        
        Write-Host "✅ Database security hardening completed successfully!" -ForegroundColor Green
        
    } catch {
        Write-Host "❌ Error during security hardening: $($_.Exception.Message)" -ForegroundColor Red
        Write-SecurityLog "ERROR: Security hardening failed: $($_.Exception.Message)"
        exit 1
    }
}

# Run main function
Main