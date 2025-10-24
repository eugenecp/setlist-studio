# Quick CodeQL Security Analysis
# Matches the security-focused analysis mentioned in Copilot instructions
# For rapid security validation during development

param(
    [Parameter(Mandatory=$false)]
    [switch]$CleanDatabase,
    
    [Parameter(Mandatory=$false)]
    [switch]$OpenResults
)

Write-Host "🔐 Running Security-Focused CodeQL Analysis..." -ForegroundColor Red

# Run the main script with security-extended suite
& (Join-Path $PSScriptRoot "run-codeql-local.ps1") `
    -QuerySuite "security-extended" `
    -CleanDatabase:$CleanDatabase `
    -OpenResults:$OpenResults

Write-Host "`n🎯 Security Analysis Complete!" -ForegroundColor Green
Write-Host "This analysis matches the security-focused validation described in Copilot instructions." -ForegroundColor Cyan