#!/usr/bin/env pwsh
# Clean test execution script to avoid file locking issues

param(
    [string]$Configuration = "Release",
    [string]$ResultsDirectory = "./TestResults/CleanRun",
    [switch]$SkipBuild = $false,
    [switch]$Verbose = $false
)

Write-Host "🧪 Starting clean test execution..." -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Results Directory: $ResultsDirectory" -ForegroundColor Cyan

# Step 1: Kill any existing dotnet processes that might be locking files
Write-Host ""
Write-Host "🔄 Cleaning up existing processes..." -ForegroundColor Yellow
if ($IsWindows) {
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -eq "dotnet" } | Stop-Process -Force -ErrorAction SilentlyContinue
} else {
    pkill -f "dotnet.*SetlistStudio" 2>/dev/null || true
}
Start-Sleep -Seconds 2

# Step 2: Clean previous builds
Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean SetlistStudio.sln --configuration $Configuration --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Clean failed" -ForegroundColor Red
    exit 1
}

# Step 3: Remove previous test results
Write-Host "🗑️ Removing previous test results..." -ForegroundColor Yellow
if (Test-Path $ResultsDirectory) {
    Remove-Item -Path $ResultsDirectory -Recurse -Force
}
New-Item -Path $ResultsDirectory -ItemType Directory -Force | Out-Null

# Step 4: Restore dependencies
Write-Host "📦 Restoring dependencies..." -ForegroundColor Yellow
dotnet restore SetlistStudio.sln --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Restore failed" -ForegroundColor Red
    exit 1
}

# Step 5: Build solution (unless skipped)
if (-not $SkipBuild) {
    Write-Host "🏗️ Building solution..." -ForegroundColor Yellow
    dotnet build SetlistStudio.sln --configuration $Configuration --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
}

# Step 6: Set test environment variables
$env:ASPNETCORE_ENVIRONMENT = "Test"
$env:DOTNET_ENVIRONMENT = "Test"

# Step 7: Run tests
Write-Host ""
Write-Host "🧪 Running tests..." -ForegroundColor Green
Write-Host "Environment: $env:ASPNETCORE_ENVIRONMENT" -ForegroundColor Cyan

$testArgs = @(
    "test"
    "SetlistStudio.sln"
    "--configuration"
    $Configuration
    "--no-build"
    "--logger"
    "trx"
    "--logger"
    "console;verbosity=normal"
    "--collect:XPlat Code Coverage"
    "--results-directory"
    $ResultsDirectory
)

if ($Verbose) {
    $testArgs += "--verbosity"
    $testArgs += "detailed"
} else {
    $testArgs += "--verbosity"
    $testArgs += "normal"
}

dotnet @testArgs

$testExitCode = $LASTEXITCODE

# Step 8: Generate coverage report if successful
if ($testExitCode -eq 0) {
    Write-Host ""
    Write-Host "✅ Tests completed successfully!" -ForegroundColor Green
    
    # Check if coverage files exist
    $coverageFiles = Get-ChildItem -Path $ResultsDirectory -Filter "coverage.cobertura.xml" -Recurse
    if ($coverageFiles.Count -gt 0) {
        Write-Host "📊 Generating coverage report..." -ForegroundColor Yellow
        
        $coverageReportDir = Join-Path $ResultsDirectory "CoverageReport"
        
        # Try to generate coverage report using reportgenerator if available
        try {
            $reportGeneratorPath = (Get-Command reportgenerator -ErrorAction SilentlyContinue).Source
            if ($reportGeneratorPath) {
                reportgenerator "-reports:$($coverageFiles[0].FullName)" "-targetdir:$coverageReportDir" "-reporttypes:Html"
                Write-Host "📈 Coverage report generated at: $coverageReportDir" -ForegroundColor Green
            } else {
                Write-Host "ℹ️ reportgenerator not found. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "⚠️ Could not generate coverage report: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    Write-Host "📋 Test Results Summary:" -ForegroundColor Cyan
    Write-Host "- Results Directory: $ResultsDirectory" -ForegroundColor White
    Write-Host "- Coverage Files: $($coverageFiles.Count) found" -ForegroundColor White
    
} else {
    Write-Host ""
    Write-Host "❌ Tests failed with exit code: $testExitCode" -ForegroundColor Red
}

# Step 9: Cleanup environment variables
Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
Remove-Item Env:DOTNET_ENVIRONMENT -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "🏁 Clean test execution completed." -ForegroundColor Green
exit $testExitCode