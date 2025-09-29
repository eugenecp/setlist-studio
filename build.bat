@echo off
REM Setlist Studio Build Script for Windows
REM Builds, tests, and optionally runs the application

setlocal EnableDelayedExpansion

echo 🎵 Setlist Studio - Build ^& Run Script
echo ======================================

if "%1"=="" goto run
if "%1"=="-h" goto help
if "%1"=="--help" goto help
if "%1"=="-t" goto test
if "%1"=="--test" goto test
if "%1"=="-b" goto build
if "%1"=="--build" goto build
if "%1"=="-r" goto run
if "%1"=="--run" goto run
if "%1"=="-d" goto docker
if "%1"=="--docker" goto docker
if "%1"=="-c" goto clean
if "%1"=="--clean" goto clean

goto help

:help
echo Usage: build.bat [OPTIONS]
echo.
echo Options:
echo   -h, --help     Show this help message
echo   -t, --test     Run tests only
echo   -b, --build    Build only (no run)
echo   -r, --run      Build and run
echo   -d, --docker   Build and run with Docker
echo   -c, --clean    Clean build artifacts
echo.
echo Examples:
echo   build.bat --run      # Build and run the application
echo   build.bat --docker   # Run with Docker
echo   build.bat --test     # Run tests only
goto end

:clean
echo 🧹 Cleaning build artifacts...
dotnet clean
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
if exist publish rmdir /s /q publish
echo ✅ Clean complete
goto end

:check_dotnet
where dotnet >nul 2>&1
if errorlevel 1 (
    echo ❌ .NET SDK not found. Please install .NET 8 SDK.
    exit /b 1
)

dotnet --version
echo ✅ .NET SDK found
goto :eof

:restore
echo 📦 Restoring NuGet packages...
dotnet restore
if errorlevel 1 (
    echo ❌ Package restore failed
    exit /b 1
)
echo ✅ Packages restored
goto :eof

:build_only
echo 🔨 Building application...
dotnet build --configuration Release --no-restore
if errorlevel 1 (
    echo ❌ Build failed
    exit /b 1
)
echo ✅ Build complete
goto :eof

:run_tests
echo 🧪 Running tests...
dotnet test --configuration Release --no-build --verbosity normal
if errorlevel 1 (
    echo ❌ Tests failed
    exit /b 1
)
echo ✅ Tests complete
goto :eof

:test
echo 🔍 Running tests only...
call :check_dotnet
call :restore
call :build_only
call :run_tests
goto end

:build
echo 🔍 Building application...
call :check_dotnet
call :restore
call :build_only
goto end

:run
echo 🚀 Building and running Setlist Studio...
call :check_dotnet
call :restore
call :build_only
call :run_tests

echo 🚀 Starting Setlist Studio...
echo    Navigate to: https://localhost:5001
echo    Press Ctrl+C to stop
echo.

cd src\SetlistStudio.Web
dotnet run --configuration Release
goto end

:docker
echo 🐳 Building and running with Docker...

REM Check if Docker is available
where docker >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker not found. Please install Docker.
    exit /b 1
)

where docker-compose >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker Compose not found. Please install Docker Compose.
    exit /b 1
)

REM Check if .env exists, create from example if not
if not exist .env (
    echo 📝 Creating .env file from .env.example...
    copy .env.example .env
    echo ⚠️  Please edit .env file with your OAuth credentials
)

REM Build and run with Docker Compose
docker-compose up --build -d

if errorlevel 1 (
    echo ❌ Docker deployment failed
    exit /b 1
)

echo ✅ Application running in Docker
echo    Navigate to: http://localhost:5000
echo    View logs: docker-compose logs -f
echo    Stop: docker-compose down
goto end

:end
endlocal