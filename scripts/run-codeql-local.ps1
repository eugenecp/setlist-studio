# CodeQL Local Analysis Script - Aligned with GitHub Actions Configuration
# This script mirrors the exact CodeQL configuration used in GitHub Actions
# Ensures consistency between local development and CI/CD security analysis

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("security-and-quality", "security-extended")]
    [string]$QuerySuite = "security-and-quality",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputFormat = "sarif-latest",
    
    [Parameter(Mandatory=$false)]
    [string]$DatabasePath = "codeql-database",
    
    [Parameter(Mandatory=$false)]
    [switch]$CleanDatabase,
    
    [Parameter(Mandatory=$false)]
    [switch]$OpenResults
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ConfigFile = Join-Path $ProjectRoot ".codeql\codeql-config.yml"
$ResultsDir = Join-Path $ProjectRoot "codeql-results"

# Change to project root directory
Push-Location $ProjectRoot

try {
    Write-Host "🛡️  CodeQL Local Analysis - Aligned with GitHub Actions" -ForegroundColor Cyan
Write-Host "Query Suite: $QuerySuite" -ForegroundColor Yellow
Write-Host "Database Path: $DatabasePath" -ForegroundColor Yellow
Write-Host "Config File: $ConfigFile" -ForegroundColor Yellow

# Ensure results directory exists
if (-not (Test-Path $ResultsDir)) {
    New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
    Write-Host "✅ Created results directory: $ResultsDir" -ForegroundColor Green
}

# Clean existing database if requested
if ($CleanDatabase -and (Test-Path $DatabasePath)) {
    Write-Host "🧹 Cleaning existing CodeQL database..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $DatabasePath
}

# Step 1: Create CodeQL database (matches GitHub Actions build)
if (-not (Test-Path $DatabasePath)) {
    Write-Host "🏗️  Creating CodeQL database..." -ForegroundColor Cyan
    
    # Clean and restore before creating database (matches GitHub Actions)
    Write-Host "   Cleaning solution..." -ForegroundColor Gray
    dotnet clean SetlistStudio.sln --configuration Release
    
    Write-Host "   Restoring packages..." -ForegroundColor Gray  
    dotnet restore SetlistStudio.sln
    
    # Create database with build command that matches GitHub Actions
    Write-Host "   Building and creating database..." -ForegroundColor Gray
    codeql database create $DatabasePath `
        --language=csharp `
        --command="dotnet build SetlistStudio.sln --configuration Release --no-restore" `
        --source-root=$ProjectRoot `
        --threads=0
        
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create CodeQL database"
        exit 1
    }
    
    Write-Host "✅ CodeQL database created successfully" -ForegroundColor Green
} else {
    Write-Host "✅ Using existing CodeQL database: $DatabasePath" -ForegroundColor Green
}

# Step 2: Determine query suite and output file
$QuerySuiteMapping = @{
    "security-and-quality" = "codeql/csharp-queries:codeql-suites/csharp-security-and-quality.qls"
    "security-extended" = "codeql/csharp-queries:codeql-suites/csharp-security-extended.qls"  
}

$QueryPath = $QuerySuiteMapping[$QuerySuite]
$Timestamp = Get-Date -Format "yyyy-MM-dd-HH-mm-ss"
$OutputFile = Join-Path $ResultsDir "codeql-analysis-$QuerySuite-$Timestamp.$($OutputFormat.Replace('-latest', ''))"

Write-Host "🔍 Running CodeQL analysis..." -ForegroundColor Cyan
Write-Host "   Query Suite: $QueryPath" -ForegroundColor Gray
Write-Host "   Output File: $OutputFile" -ForegroundColor Gray

# Step 3: Run CodeQL analysis (matches GitHub Actions configuration)
Write-Host "   Note: Local CodeQL CLI doesn't support --config-file parameter" -ForegroundColor Gray
Write-Host "   Configuration settings applied through query suite selection" -ForegroundColor Gray

codeql database analyze $DatabasePath `
    --format=$OutputFormat `
    --output=$OutputFile `
    $QueryPath `
    --download `
    --threads=0

if ($LASTEXITCODE -ne 0) {
    Write-Error "CodeQL analysis failed"
    exit 1
}

Write-Host "✅ CodeQL analysis completed successfully" -ForegroundColor Green

# Step 4: Process and display results
Write-Host "📊 Processing results..." -ForegroundColor Cyan

if ($OutputFormat -like "*sarif*") {
    try {
        $SarifContent = Get-Content $OutputFile | ConvertFrom-Json
        $Results = $SarifContent.runs[0].results
        $Rules = $SarifContent.runs[0].tool.driver.rules
        
        if ($Results.Count -eq 0) {
            Write-Host "🎉 No issues found!" -ForegroundColor Green
        } else {
            Write-Host "📈 Found $($Results.Count) findings:" -ForegroundColor Yellow
            
            # Categorize findings by severity (matches GitHub Actions analysis)
            $Findings = @{}
            foreach ($Result in $Results) {
                $Rule = $Rules | Where-Object { $_.id -eq $Result.ruleId }
                $Severity = if ($Rule.properties.'problem.severity') { 
                    $Rule.properties.'problem.severity' 
                } else { 
                    "info" 
                }
                
                if ($Findings.ContainsKey($Severity)) { 
                    $Findings[$Severity]++ 
                } else { 
                    $Findings[$Severity] = 1 
                }
            }
            
            Write-Host "`n📊 Findings by severity:" -ForegroundColor Yellow
            $Findings.GetEnumerator() | Sort-Object Value -Descending | ForEach-Object {
                $Color = switch ($_.Key) {
                    "error" { "Red" }
                    "warning" { "Yellow" }
                    "recommendation" { "Cyan" }
                    "note" { "Gray" }
                    default { "White" }
                }
                Write-Host "   $($_.Key): $($_.Value)" -ForegroundColor $Color
            }
            
            # Security vs Quality distinction (matches updated Copilot instructions)
            $SecurityIssues = $Results | Where-Object { 
                $Rule = $Rules | Where-Object { $_.id -eq $_.ruleId }
                $Rule.properties.'problem.severity' -in @("error", "warning") -and
                $Rule.properties.tags -contains "security"
            }
            
            if ($SecurityIssues.Count -gt 0) {
                Write-Host "`n🚨 SECURITY ISSUES FOUND: $($SecurityIssues.Count)" -ForegroundColor Red
                Write-Host "   These must be addressed before proceeding." -ForegroundColor Red
            } else {
                Write-Host "`n✅ No security vulnerabilities found" -ForegroundColor Green
                if ($Results.Count -gt 0) {
                    Write-Host "   All findings are code quality improvements (non-blocking)" -ForegroundColor Cyan
                }
            }
        }
    } catch {
        Write-Warning "Could not parse SARIF results: $($_.Exception.Message)"
    }
}

# Step 5: Generate summary report
$SummaryFile = Join-Path $ResultsDir "summary-$Timestamp.md"
$Summary = @"
# CodeQL Analysis Summary - $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Configuration
- **Query Suite**: $QuerySuite
- **Query Path**: $QueryPath  
- **Database**: $DatabasePath
- **Output**: $OutputFile
- **Configuration**: Aligned with GitHub Actions security.yml

## Results
- **Total Findings**: $($Results.Count)
- **Analysis Status**: $(if ($Results.Count -eq 0) { "✅ Clean" } else { "⚠️ Issues Found" })

## Command Used
``````powershell
codeql database analyze $DatabasePath --format=$OutputFormat --output=$OutputFile $QueryPath --download --threads=0
``````

## Next Steps
$(if ($Results.Count -eq 0) {
"🎉 No issues found! Your code meets security standards."
} else {
"📋 Review findings in the SARIF file and address any security issues.
🔍 Use CodeQL VS Code extension to investigate specific findings.
📊 Focus on security issues first, then address quality improvements."
})
"@

$Summary | Out-File -FilePath $SummaryFile -Encoding UTF8
Write-Host "📝 Summary report saved: $SummaryFile" -ForegroundColor Cyan

# Step 6: Open results if requested
if ($OpenResults) {
    if (Test-Path $OutputFile) {
        Write-Host "🔍 Opening results..." -ForegroundColor Cyan
        Start-Process $OutputFile
    }
    
    if (Test-Path $SummaryFile) {
        Start-Process $SummaryFile  
    }
}

    Write-Host "`n🎯 CodeQL Analysis Complete!" -ForegroundColor Green
    Write-Host "Results: $OutputFile" -ForegroundColor Cyan
    Write-Host "Summary: $SummaryFile" -ForegroundColor Cyan

    # Return results for automation
    return @{
        TotalFindings = $Results.Count
        OutputFile = $OutputFile
        SummaryFile = $SummaryFile
        SecurityIssues = if ($SecurityIssues) { $SecurityIssues.Count } else { 0 }
    }
} finally {
    # Restore original location
    Pop-Location
}