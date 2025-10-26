# Deploy Setlist Studio with Load Balancing using Docker Compose
# This script sets up NGINX load balancing with multiple app instances and Redis

param(
    [Parameter(HelpMessage="Environment to deploy (Development, Production)")]
    [ValidateSet("Development", "Production")]
    [string]$Environment = "Development",
    
    [Parameter(HelpMessage="Number of web instances to deploy")]
    [ValidateRange(2, 10)]
    [int]$Instances = 3,
    
    [Parameter(HelpMessage="Enable monitoring stack (Prometheus, Grafana)")]
    [switch]$EnableMonitoring,
    
    [Parameter(HelpMessage="Clean existing deployment before starting")]
    [switch]$Clean,
    
    [Parameter(HelpMessage="Build images before deployment")]
    [switch]$Build
)

Write-Host "🚀 Deploying Setlist Studio with Load Balancing" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Web Instances: $Instances" -ForegroundColor Yellow
Write-Host "Monitoring: $(if ($EnableMonitoring) { 'Enabled' } else { 'Disabled' })" -ForegroundColor Yellow

# Ensure we're in the project root
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

# Validate required files
$requiredFiles = @(
    "docker-compose.yml",
    "docker-compose.loadbalanced.yml",
    "nginx/nginx.conf",
    "Dockerfile"
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        Write-Error "Required file not found: $file"
        exit 1
    }
}

# Clean existing deployment if requested
if ($Clean) {
    Write-Host "🧹 Cleaning existing deployment..." -ForegroundColor Yellow
    docker-compose -f docker-compose.yml -f docker-compose.loadbalanced.yml down -v --remove-orphans
}

# Set environment variables
$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:REDIS_PASSWORD = if ($Environment -eq "Production") { 
    Read-Host "Enter Redis password for production" -AsSecureString | ConvertFrom-SecureString 
} else { 
    "setlist-redis-dev-password" 
}
$env:POSTGRES_PASSWORD = if ($Environment -eq "Production") { 
    Read-Host "Enter PostgreSQL password for production" -AsSecureString | ConvertFrom-SecureString 
} else { 
    "setlist-postgres-dev-password" 
}

# Build images if requested
if ($Build) {
    Write-Host "🔨 Building application images..." -ForegroundColor Blue
    docker-compose -f docker-compose.yml -f docker-compose.loadbalanced.yml build --no-cache
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker build failed"
        exit 1
    }
}

try {
    # Start the load balanced deployment
    Write-Host "🐳 Starting load balanced deployment..." -ForegroundColor Blue
    
    $composeFiles = @("-f", "docker-compose.yml", "-f", "docker-compose.loadbalanced.yml")
    
    # Scale web instances
    Write-Host "⚖️ Scaling to $Instances web instances..." -ForegroundColor Blue
    docker-compose $composeFiles up -d --scale setliststudio-web-1=1 --scale setliststudio-web-2=1 --scale setliststudio-web-3=($Instances - 2)
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker deployment failed"
        exit 1
    }
    
    # Wait for services to be healthy
    Write-Host "⏳ Waiting for services to be healthy..." -ForegroundColor Yellow
    $timeout = 180
    $elapsed = 0
    
    do {
        Start-Sleep 5
        $elapsed += 5
        
        $healthyServices = docker-compose $composeFiles ps --services --filter "status=running" | Measure-Object | Select-Object -ExpandProperty Count
        $totalServices = docker-compose $composeFiles config --services | Measure-Object | Select-Object -ExpandProperty Count
        
        Write-Host "Healthy services: $healthyServices/$totalServices" -ForegroundColor Cyan
        
        if ($elapsed -ge $timeout) {
            Write-Warning "Timeout waiting for services to be healthy"
            break
        }
    } while ($healthyServices -lt $totalServices)
    
    # Test load balancer
    Write-Host "🔍 Testing load balancer..." -ForegroundColor Blue
    $testUrl = "http://localhost/health"
    
    for ($i = 1; $i -le 3; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $testUrl -UseBasicParsing -TimeoutSec 10
            if ($response.StatusCode -eq 200) {
                Write-Host "✅ Load balancer test $i/3 passed" -ForegroundColor Green
            } else {
                Write-Warning "Load balancer test $i/3 failed with status $($response.StatusCode)"
            }
        } catch {
            Write-Warning "Load balancer test $i/3 failed: $($_.Exception.Message)"
        }
        Start-Sleep 2
    }
    
    # Display deployment information
    Write-Host "`n🎉 Deployment completed successfully!" -ForegroundColor Green
    Write-Host "`n📊 Service Information:" -ForegroundColor Cyan
    docker-compose $composeFiles ps
    
    Write-Host "`n🌐 Access URLs:" -ForegroundColor Cyan
    Write-Host "  Application: http://localhost" -ForegroundColor White
    Write-Host "  Health Check: http://localhost/health" -ForegroundColor White
    Write-Host "  Detailed Health: http://localhost/api/health/detailed" -ForegroundColor White
    
    if ($EnableMonitoring) {
        Write-Host "  Prometheus: http://localhost:9090" -ForegroundColor White
        Write-Host "  Grafana: http://localhost:3000 (admin/setlist-grafana-dev)" -ForegroundColor White
    }
    
    Write-Host "`n📋 Useful Commands:" -ForegroundColor Cyan
    Write-Host "  View logs: docker-compose -f docker-compose.yml -f docker-compose.loadbalanced.yml logs -f" -ForegroundColor White
    Write-Host "  Scale instances: docker-compose -f docker-compose.yml -f docker-compose.loadbalanced.yml up -d --scale setliststudio-web-1=1 --scale setliststudio-web-2=1 --scale setliststudio-web-3=N" -ForegroundColor White
    Write-Host "  Stop deployment: docker-compose -f docker-compose.yml -f docker-compose.loadbalanced.yml down" -ForegroundColor White
    
    # Show connection info
    Write-Host "`n💾 Database Information:" -ForegroundColor Cyan
    Write-Host "  PostgreSQL: postgres:5432" -ForegroundColor White
    Write-Host "  Database: setliststudio" -ForegroundColor White
    Write-Host "  User: setlist_user" -ForegroundColor White
    Write-Host "  Redis: redis:6379" -ForegroundColor White
    
} catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    Write-Host "🔍 Checking container logs..." -ForegroundColor Yellow
    docker-compose $composeFiles logs --tail=50
    exit 1
}

Write-Host "`n✨ Load balancing deployment completed! Your Setlist Studio is now running with $Instances instances." -ForegroundColor Green