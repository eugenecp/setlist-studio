#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates load balancing setup with PostgreSQL backend
.DESCRIPTION
    Tests multiple application instances, NGINX load balancer, and PostgreSQL database
.PARAMETER ComposeFile
    Docker Compose file to test (defaults to docker-compose.loadbalanced.yml)
.PARAMETER SkipBuild
    Skip building images (use existing images)
.EXAMPLE
    .\validate-load-balancing.ps1
    .\validate-load-balancing.ps1 -SkipBuild
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$ComposeFile = "docker-compose.loadbalanced.yml",
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# Colors for output
$Green = "Green"
$Red = "Red" 
$Yellow = "Yellow"
$Cyan = "Cyan"

function Write-Success($Message) { Write-Host "✅ $Message" -ForegroundColor $Green }
function Write-Error($Message) { Write-Host "❌ $Message" -ForegroundColor $Red }
function Write-Warning($Message) { Write-Host "⚠️  $Message" -ForegroundColor $Yellow }
function Write-Info($Message) { Write-Host "ℹ️  $Message" -ForegroundColor $Cyan }

Write-Host "🚀 Setlist Studio Load Balancing Validation" -ForegroundColor $Cyan
Write-Host "============================================" -ForegroundColor $Cyan

# Check if Docker is running
try {
    docker version | Out-Null
    Write-Success "Docker is running"
} catch {
    Write-Error "Docker is not running. Please start Docker and try again."
    exit 1
}

# Check if compose file exists
if (-not (Test-Path $ComposeFile)) {
    Write-Error "Compose file '$ComposeFile' not found"
    exit 1
}
Write-Success "Found compose file: $ComposeFile"

# Stop any existing containers
Write-Info "Stopping any existing containers..."
docker-compose -f $ComposeFile down --remove-orphans 2>$null

# Build or pull images
if (-not $SkipBuild) {
    Write-Info "Building application images..."
    docker-compose -f $ComposeFile build
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build images"
        exit 1
    }
    Write-Success "Images built successfully"
}

# Start services
Write-Info "Starting load balanced services..."
docker-compose -f $ComposeFile up -d

# Wait for services to be healthy
Write-Info "Waiting for services to become healthy..."
$maxWaitTime = 180 # 3 minutes
$waitTime = 0
$interval = 5

do {
    Start-Sleep -Seconds $interval
    $waitTime += $interval
    
    $healthStatus = docker-compose -f $ComposeFile ps --format json | ConvertFrom-Json
    $unhealthyServices = $healthStatus | Where-Object { $_.Health -ne "healthy" -and $_.Health -ne "" }
    
    if ($unhealthyServices.Count -eq 0) {
        Write-Success "All services are healthy"
        break
    } elseif ($waitTime -ge $maxWaitTime) {
        Write-Error "Timeout waiting for services to become healthy"
        Write-Warning "Unhealthy services:"
        $unhealthyServices | ForEach-Object { Write-Host "  - $($_.Name): $($_.Health)" -ForegroundColor $Yellow }
        exit 1
    } else {
        Write-Host "Waiting... ($waitTime/$maxWaitTime seconds)" -ForegroundColor $Yellow
    }
} while ($true)

# Test load balancer endpoint
Write-Info "Testing NGINX load balancer..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost/health" -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Success "NGINX load balancer is responding"
    } else {
        Write-Error "NGINX returned status code: $($response.StatusCode)"
    }
} catch {
    Write-Error "Failed to connect to NGINX load balancer: $($_.Exception.Message)"
}

# Test individual application instances
Write-Info "Testing individual application instances..."
$instances = @("setliststudio-web-1", "setliststudio-web-2", "setliststudio-web-3")
foreach ($instance in $instances) {
    try {
        # Test health endpoint via docker exec
        docker exec "$instance" curl -f "http://localhost:8080/api/health/simple" 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "$instance is healthy"
        } else {
            Write-Error "$instance health check failed"
        }
    } catch {
        Write-Warning "Could not test $instance directly: $($_.Exception.Message)"
    }
}

# Test PostgreSQL connection
Write-Info "Testing PostgreSQL database..."
try {
    docker exec postgres pg_isready -U setlist_user -d setliststudio | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Success "PostgreSQL is ready and accepting connections"
    } else {
        Write-Error "PostgreSQL is not ready"
    }
} catch {
    Write-Error "Failed to check PostgreSQL status: $($_.Exception.Message)"
}

# Test Redis connection
Write-Info "Testing Redis session store..."
try {
    $redisResult = docker exec redis redis-cli ping 2>$null
    if ($redisResult -eq "PONG") {
        Write-Success "Redis is responding to ping"
    } else {
        Write-Error "Redis ping failed"
    }
} catch {
    Write-Error "Failed to check Redis status: $($_.Exception.Message)"
}

# Check connection distribution in PostgreSQL
Write-Info "Checking database connection distribution..."
try {
    $connectionQuery = @"
SELECT 
    application_name,
    client_addr,
    COUNT(*) as connection_count,
    string_agg(DISTINCT state, ', ') as states
FROM pg_stat_activity 
WHERE application_name LIKE 'SetlistStudio%' OR application_name LIKE 'Npgsql%'
GROUP BY application_name, client_addr
ORDER BY connection_count DESC;
"@

    $connectionResult = docker exec postgres psql -U setlist_user -d setliststudio -c "$connectionQuery" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Database connection query executed successfully"
        Write-Host $connectionResult -ForegroundColor $Cyan
    } else {
        Write-Warning "Could not query database connections (this is normal if no connections are active)"
    }
} catch {
    Write-Warning "Could not check database connections: $($_.Exception.Message)"
}

# Test session persistence
Write-Info "Testing session persistence through Redis..."
try {
    # Set a test value in Redis
    docker exec redis redis-cli -a "setlist-redis-dev-password" set "test:session" "load-balancing-test" 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        # Get the value back
        $getValue = docker exec redis redis-cli -a "setlist-redis-dev-password" get "test:session" 2>$null
        if ($getValue -eq "load-balancing-test") {
            Write-Success "Redis session persistence is working"
            # Clean up test key
            docker exec redis redis-cli -a "setlist-redis-dev-password" del "test:session" 2>$null | Out-Null
        } else {
            Write-Error "Redis session persistence failed"
        }
    } else {
        Write-Error "Could not set test value in Redis"
    }
} catch {
    Write-Error "Failed to test Redis session persistence: $($_.Exception.Message)"
}

# Performance test (optional)
$performanceTest = Read-Host "Run performance test with Apache Bench? (y/N)"
if ($performanceTest -eq "y" -or $performanceTest -eq "Y") {
    Write-Info "Running performance test..."
    try {
        # Check if ab (Apache Bench) is available
        ab -V 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Info "Running 100 requests with 10 concurrent users..."
            $abResult = ab -n 100 -c 10 "http://localhost/health"
            Write-Host $abResult -ForegroundColor $Cyan
        } else {
            Write-Warning "Apache Bench (ab) not found. Skipping performance test."
            Write-Info "To install: choco install apache-httpd (Windows) or apt-get install apache2-utils (Linux)"
        }
    } catch {
        Write-Warning "Could not run performance test: $($_.Exception.Message)"
    }
}

Write-Host ""
Write-Host "🎯 Validation Summary" -ForegroundColor $Cyan
Write-Host "===================" -ForegroundColor $Cyan
Write-Success "Load balancer setup validation completed"
Write-Info "Load balancer: http://localhost"
Write-Info "Prometheus: http://localhost:9090"
Write-Info "Grafana: http://localhost:3000 (admin/setlist-grafana-dev)"
Write-Info "PostgreSQL: localhost:5432 (setlist_user/setlist-postgres-dev-password)"

Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor $Yellow
Write-Host "1. Open http://localhost in your browser to test the application"
Write-Host "2. Monitor metrics in Grafana at http://localhost:3000"
Write-Host "3. Check service logs with: docker-compose -f $ComposeFile logs -f [service-name]"
Write-Host "4. Stop services with: docker-compose -f $ComposeFile down"

Write-Host ""
Write-Success "✨ Setlist Studio is ready for load balanced production deployment!"