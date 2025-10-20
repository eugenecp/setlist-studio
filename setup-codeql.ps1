# CodeQL Setup Script for Setlist Studio
# This script sets up CodeQL for local analysis

Write-Host "🔧 Setting up CodeQL for Setlist Studio..." -ForegroundColor Cyan

# Check if CodeQL CLI is available
if (!(Get-Command "codeql" -ErrorAction SilentlyContinue)) {
    Write-Host "❌ CodeQL CLI not found in PATH" -ForegroundColor Red
    Write-Host "Please download CodeQL CLI from: https://github.com/github/codeql-cli-binaries/releases/latest" -ForegroundColor Yellow
    Write-Host "Extract to C:\codeql and add C:\codeql\codeql to your PATH" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ CodeQL CLI found" -ForegroundColor Green

# Create CodeQL database
Write-Host "📊 Creating CodeQL database..." -ForegroundColor Cyan
if (Test-Path "./codeql-database") {
    Write-Host "🗑️ Removing existing database..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "./codeql-database"
}

# Build the project first
Write-Host "🏗️ Building project..." -ForegroundColor Cyan
dotnet build --configuration Release

# Create the database
codeql database create --language=csharp --source-root=. --command="dotnet build --no-restore" ./codeql-database

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ CodeQL database created successfully" -ForegroundColor Green
    
    # Download query packs
    Write-Host "📦 Downloading CodeQL query packs..." -ForegroundColor Cyan
    codeql pack download codeql/csharp-queries
    
    Write-Host "🎉 CodeQL setup complete!" -ForegroundColor Green
    Write-Host "You can now run queries in VS Code or use:" -ForegroundColor Cyan
    Write-Host "  codeql database analyze ./codeql-database codeql/csharp-queries:codeql-suites/csharp-security-and-quality.qls --format=csv --output=results.csv" -ForegroundColor Gray
} else {
    Write-Host "❌ Failed to create CodeQL database" -ForegroundColor Red
    exit 1
}