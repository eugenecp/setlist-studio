#!/bin/bash

# Setlist Studio Build Script
# Builds, tests, and optionally runs the application

set -e  # Exit on any error

echo "🎵 Setlist Studio - Build & Run Script"
echo "======================================"

# Function to display help
show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help     Show this help message"
    echo "  -t, --test     Run tests only"
    echo "  -b, --build    Build only (no run)"
    echo "  -r, --run      Build and run"
    echo "  -d, --docker   Build and run with Docker"
    echo "  -c, --clean    Clean build artifacts"
    echo ""
    echo "Examples:"
    echo "  $0 --run      # Build and run the application"
    echo "  $0 --docker   # Run with Docker"
    echo "  $0 --test     # Run tests only"
}

# Function to clean build artifacts
clean_build() {
    echo "🧹 Cleaning build artifacts..."
    dotnet clean
    rm -rf bin/ obj/ publish/
    echo "✅ Clean complete"
}

# Function to restore packages
restore_packages() {
    echo "📦 Restoring NuGet packages..."
    dotnet restore
    echo "✅ Packages restored"
}

# Function to build application
build_app() {
    echo "🔨 Building application..."
    dotnet build --configuration Release --no-restore
    echo "✅ Build complete"
}

# Function to run tests
run_tests() {
    echo "🧪 Running tests..."
    dotnet test --configuration Release --no-build --verbosity normal
    echo "✅ Tests complete"
}

# Function to run application
run_app() {
    echo "🚀 Starting Setlist Studio..."
    echo "   Navigate to: https://localhost:5001"
    echo "   Press Ctrl+C to stop"
    echo ""
    cd src/SetlistStudio.Web
    dotnet run --configuration Release
}

# Function to run with Docker
run_docker() {
    echo "🐳 Building and running with Docker..."
    
    # Check if .env exists, create from example if not
    if [ ! -f .env ]; then
        echo "📝 Creating .env file from .env.example..."
        cp .env.example .env
        echo "⚠️  Please edit .env file with your OAuth credentials"
    fi
    
    # Build and run with Docker Compose
    docker-compose up --build -d
    
    echo "✅ Application running in Docker"
    echo "   Navigate to: http://localhost:5000"
    echo "   View logs: docker-compose logs -f"
    echo "   Stop: docker-compose down"
}

# Function to check prerequisites
check_prerequisites() {
    echo "🔍 Checking prerequisites..."
    
    # Check .NET 8
    if ! command -v dotnet &> /dev/null; then
        echo "❌ .NET SDK not found. Please install .NET 8 SDK."
        exit 1
    fi
    
    local dotnet_version=$(dotnet --version)
    echo "   .NET Version: $dotnet_version"
    
    # For Docker option, check Docker
    if [ "$1" = "docker" ]; then
        if ! command -v docker &> /dev/null; then
            echo "❌ Docker not found. Please install Docker."
            exit 1
        fi
        
        if ! command -v docker-compose &> /dev/null; then
            echo "❌ Docker Compose not found. Please install Docker Compose."
            exit 1
        fi
        
        echo "   Docker available"
    fi
    
    echo "✅ Prerequisites check complete"
}

# Parse command line arguments
ACTION="run"  # Default action

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -t|--test)
            ACTION="test"
            shift
            ;;
        -b|--build)
            ACTION="build"
            shift
            ;;
        -r|--run)
            ACTION="run"
            shift
            ;;
        -d|--docker)
            ACTION="docker"
            shift
            ;;
        -c|--clean)
            ACTION="clean"
            shift
            ;;
        *)
            echo "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Execute based on action
case $ACTION in
    "clean")
        clean_build
        ;;
    "test")
        check_prerequisites
        restore_packages
        build_app
        run_tests
        ;;
    "build")
        check_prerequisites
        restore_packages
        build_app
        ;;
    "run")
        check_prerequisites
        restore_packages
        build_app
        run_tests
        run_app
        ;;
    "docker")
        check_prerequisites "docker"
        run_docker
        ;;
esac