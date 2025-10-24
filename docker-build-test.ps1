# Docker Build Simulation Script (PowerShell)
# This script simulates the Docker build process to help debug build issues

Write-Host "=== Docker Build Simulation ===" -ForegroundColor Green

# Create temporary build directory
$BuildDir = ".\docker-build-test"
Write-Host "Creating build directory: $BuildDir" -ForegroundColor Yellow
if (Test-Path $BuildDir) {
    Remove-Item -Path $BuildDir -Recurse -Force
}
New-Item -Path $BuildDir -ItemType Directory -Force | Out-Null
New-Item -Path "$BuildDir\src" -ItemType Directory -Force | Out-Null

try {
    # Step 1: Copy project files (simulate Docker COPY commands)
    Write-Host "Step 1: Copying project files..." -ForegroundColor Yellow
    Copy-Item -Path "src\*" -Destination "$BuildDir\src\" -Recurse -Force

    # Step 2: Restore dependencies  
    Write-Host "Step 2: Restoring dependencies..." -ForegroundColor Yellow
    Push-Location $BuildDir
    
    $restoreResult = dotnet restore "src\SetlistStudio.Web\SetlistStudio.Web.csproj" --source https://api.nuget.org/v3/index.json --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed with exit code $LASTEXITCODE"
    }

    # Step 3: Build the application
    Write-Host "Step 3: Building application..." -ForegroundColor Yellow
    Push-Location "src\SetlistStudio.Web"
    
    $buildResult = dotnet build "SetlistStudio.Web.csproj" -c Release -o "..\..\app\build" --no-restore --verbosity minimal -p:TreatWarningsAsErrors=true -p:WarningsAsErrors="" -p:WarningsNotAsErrors="NU1603"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "SUCCESS: Build completed successfully" -ForegroundColor Green
        Write-Host "Build artifacts:" -ForegroundColor Cyan
        Get-ChildItem "..\..\app\build\" | Select-Object -First 10 | Format-Table Name, Length
    } else {
        Write-Host "ERROR: Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        Write-Host "Source files in context:" -ForegroundColor Cyan
        Get-ChildItem -Path "..\.." -Filter "*.cs" -Recurse | Select-Object -First 20 | ForEach-Object { $_.FullName }
        throw "Build failed"
    }

    # Step 4: Publish
    Write-Host "Step 4: Publishing application..." -ForegroundColor Yellow
    $publishResult = dotnet publish "SetlistStudio.Web.csproj" -c Release -o "..\..\app\publish" --no-restore --no-build --verbosity minimal
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "SUCCESS: Publish completed successfully" -ForegroundColor Green
        Write-Host "Published artifacts:" -ForegroundColor Cyan
        Get-ChildItem "..\..\app\publish\" | Select-Object -First 10 | Format-Table Name, Length
    } else {
        Write-Host "ERROR: Publish failed with exit code $LASTEXITCODE" -ForegroundColor Red
        throw "Publish failed"
    }

} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    # Return to original location and cleanup
    Pop-Location
    Pop-Location
    
    Write-Host "Cleaning up build directory..." -ForegroundColor Yellow
    if (Test-Path $BuildDir) {
        Remove-Item -Path $BuildDir -Recurse -Force
    }
}

Write-Host "=== Docker Build Simulation Complete ===" -ForegroundColor Green