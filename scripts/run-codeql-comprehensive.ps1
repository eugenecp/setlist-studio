# Comprehensive CodeQL Analysis  
# Matches the complete security-and-quality analysis used in GitHub Actions
# For full code quality and security analysis

param(
    [Parameter(Mandatory=$false)]
    [switch]$CleanDatabase,
    
    [Parameter(Mandatory=$false)]
    [switch]$OpenResults
)

Write-Host "🔍 Running Comprehensive CodeQL Analysis (GitHub Actions Match)..." -ForegroundColor Cyan

# Run the main script with security-and-quality suite (matches GitHub Actions)
& (Join-Path $PSScriptRoot "run-codeql-local.ps1") `
    -QuerySuite "security-and-quality" `
    -CleanDatabase:$CleanDatabase `
    -OpenResults:$OpenResults

Write-Host "`n🎯 Comprehensive Analysis Complete!" -ForegroundColor Green
Write-Host "This analysis exactly matches the GitHub Actions security.yml workflow." -ForegroundColor Cyan