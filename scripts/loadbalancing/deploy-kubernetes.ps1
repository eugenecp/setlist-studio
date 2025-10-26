# Deploy Setlist Studio on Kubernetes with Auto-scaling
# This script deploys the application with NGINX Ingress and HPA

param(
    [Parameter(HelpMessage="Kubernetes namespace")]
    [string]$Namespace = "setlist-studio",
    
    [Parameter(HelpMessage="Domain name for ingress")]
    [string]$Domain = "setliststudio.local",
    
    [Parameter(HelpMessage="Enable SSL with cert-manager")]
    [switch]$EnableSSL,
    
    [Parameter(HelpMessage="Apply configuration without prompting")]
    [switch]$Force
)

Write-Host "☸️ Deploying Setlist Studio on Kubernetes" -ForegroundColor Green
Write-Host "Namespace: $Namespace" -ForegroundColor Yellow
Write-Host "Domain: $Domain" -ForegroundColor Yellow
Write-Host "SSL: $(if ($EnableSSL) { 'Enabled' } else { 'Disabled' })" -ForegroundColor Yellow

# Check kubectl availability
if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Error "kubectl is not installed or not in PATH"
    exit 1
}

# Check cluster connection
try {
    kubectl cluster-info --request-timeout=10s | Out-Null
    Write-Host "✅ Connected to Kubernetes cluster" -ForegroundColor Green
} catch {
    Write-Error "Cannot connect to Kubernetes cluster. Please check your kubeconfig."
    exit 1
}

# Ensure we're in the project root
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $projectRoot

# Validate required files
$requiredFiles = @(
    "k8s/setlist-studio-deployment.yaml",
    "k8s/redis-deployment.yaml",
    "k8s/ingress.yaml"
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        Write-Error "Required file not found: $file"
        exit 1
    }
}

try {
    # Create namespace if it doesn't exist
    Write-Host "📦 Creating namespace '$Namespace'..." -ForegroundColor Blue
    kubectl create namespace $Namespace --dry-run=client -o yaml | kubectl apply -f -
    
    # Create secrets (interactive for production)
    Write-Host "🔐 Creating secrets..." -ForegroundColor Blue
    
    $secrets = @{
        "database-connection" = "Host=setlist-studio-postgres-service;Database=setliststudio;Username=setlist_user;Password=setlist-postgres-k8s-password;Pooling=true;MinPoolSize=5;MaxPoolSize=50;"
        "postgres-password" = "setlist-postgres-k8s-password"
        "redis-connection" = "redis:6379,password=setlist-redis-k8s-password"
        "redis-password" = "setlist-redis-k8s-password"
        "google-client-id" = $env:GOOGLE_CLIENT_ID ?? "YOUR_GOOGLE_CLIENT_ID"
        "google-client-secret" = $env:GOOGLE_CLIENT_SECRET ?? "YOUR_GOOGLE_CLIENT_SECRET"
        "microsoft-client-id" = $env:MICROSOFT_CLIENT_ID ?? "YOUR_MICROSOFT_CLIENT_ID"
        "microsoft-client-secret" = $env:MICROSOFT_CLIENT_SECRET ?? "YOUR_MICROSOFT_CLIENT_SECRET"
        "facebook-app-id" = $env:FACEBOOK_APP_ID ?? "YOUR_FACEBOOK_APP_ID"
        "facebook-app-secret" = $env:FACEBOOK_APP_SECRET ?? "YOUR_FACEBOOK_APP_SECRET"
    }
    
    # Create main secrets
    $secretArgs = @()
    foreach ($secret in $secrets.GetEnumerator()) {
        $secretArgs += "--from-literal=$($secret.Key)=$($secret.Value)"
    }
    
    kubectl create secret generic setlist-studio-secrets -n $Namespace $secretArgs --dry-run=client -o yaml | kubectl apply -f -
    
    # Create OAuth secrets
    $oauthSecrets = $secrets.Keys | Where-Object { $_ -like "*-client-*" -or $_ -like "*-app-*" }
    $oauthArgs = @()
    foreach ($key in $oauthSecrets) {
        $oauthArgs += "--from-literal=$key=$($secrets[$key])"
    }
    
    kubectl create secret generic oauth-secrets -n $Namespace $oauthArgs --dry-run=client -o yaml | kubectl apply -f -
    
    # Create persistent volumes
    Write-Host "💾 Creating persistent volumes..." -ForegroundColor Blue
    
    $pvcYaml = @"
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: setlist-studio-data-pvc
  namespace: $Namespace
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
  storageClassName: standard
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: setlist-studio-redis-pvc
  namespace: $Namespace
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 2Gi
  storageClassName: standard
"@
    
    $pvcYaml | kubectl apply -f -
    
    # Create ConfigMap for Redis
    Write-Host "⚙️ Creating Redis configuration..." -ForegroundColor Blue
    kubectl create configmap redis-config -n $Namespace --from-file=redis.conf=docker/redis/redis.conf --dry-run=client -o yaml | kubectl apply -f -
    
    # Deploy PostgreSQL
    Write-Host "🐘 Deploying PostgreSQL..." -ForegroundColor Blue
    $postgresYaml = Get-Content "k8s/postgres-deployment.yaml" -Raw
    $postgresYaml = $postgresYaml -replace "namespace: setlist-studio", "namespace: $Namespace"
    $postgresYaml | kubectl apply -f -
    
    # Wait for PostgreSQL to be ready
    Write-Host "⏳ Waiting for PostgreSQL to be ready..." -ForegroundColor Yellow
    kubectl wait --for=condition=available --timeout=300s deployment/setlist-studio-postgres -n $Namespace
    
    # Deploy Redis
    Write-Host "🔴 Deploying Redis..." -ForegroundColor Blue
    $redisYaml = Get-Content "k8s/redis-deployment.yaml" -Raw
    $redisYaml = $redisYaml -replace "namespace: setlist-studio", "namespace: $Namespace"
    $redisYaml | kubectl apply -f -
    
    # Wait for Redis to be ready
    Write-Host "⏳ Waiting for Redis to be ready..." -ForegroundColor Yellow
    kubectl wait --for=condition=available --timeout=300s deployment/setlist-studio-redis -n $Namespace
    
    # Deploy application
    Write-Host "🚀 Deploying Setlist Studio application..." -ForegroundColor Blue
    $appYaml = Get-Content "k8s/setlist-studio-deployment.yaml" -Raw
    $appYaml = $appYaml -replace "namespace: setlist-studio", "namespace: $Namespace"
    $appYaml = $appYaml -replace "image: setlist-studio:latest", "image: setlist-studio:1.0.0"
    $appYaml | kubectl apply -f -
    
    # Wait for application to be ready
    Write-Host "⏳ Waiting for application to be ready..." -ForegroundColor Yellow
    kubectl wait --for=condition=available --timeout=300s deployment/setlist-studio-web -n $Namespace
    
    # Deploy ingress
    Write-Host "🌐 Deploying ingress..." -ForegroundColor Blue
    $ingressYaml = Get-Content "k8s/ingress.yaml" -Raw
    $ingressYaml = $ingressYaml -replace "namespace: setlist-studio", "namespace: $Namespace"
    $ingressYaml = $ingressYaml -replace "setliststudio.com", $Domain
    $ingressYaml = $ingressYaml -replace "www.setliststudio.com", "www.$Domain"
    
    if (-not $EnableSSL) {
        # Remove SSL configuration for local development
        $ingressYaml = $ingressYaml -replace "cert-manager\.io/cluster-issuer.*", ""
        $ingressYaml = $ingressYaml -replace "nginx\.ingress\.kubernetes\.io/ssl-redirect.*", ""
        $ingressYaml = $ingressYaml -replace "nginx\.ingress\.kubernetes\.io/force-ssl-redirect.*", ""
        $ingressYaml = ($ingressYaml -split "tls:")[0]  # Remove TLS section
    }
    
    $ingressYaml | kubectl apply -f -
    
    # Display deployment status
    Write-Host "`n🎉 Deployment completed!" -ForegroundColor Green
    Write-Host "`n📊 Deployment Status:" -ForegroundColor Cyan
    kubectl get pods -n $Namespace
    
    Write-Host "`n🔗 Services:" -ForegroundColor Cyan
    kubectl get services -n $Namespace
    
    Write-Host "`n🌐 Ingress:" -ForegroundColor Cyan
    kubectl get ingress -n $Namespace
    
    Write-Host "`n📈 Horizontal Pod Autoscaler:" -ForegroundColor Cyan
    kubectl get hpa -n $Namespace
    
    # Get ingress IP/hostname
    $ingressInfo = kubectl get ingress setlist-studio-ingress -n $Namespace -o jsonpath='{.status.loadBalancer.ingress[0]}'
    if ($ingressInfo) {
        $ingressAddress = ($ingressInfo | ConvertFrom-Json).ip ?? ($ingressInfo | ConvertFrom-Json).hostname
        if ($ingressAddress) {
            Write-Host "`n🌐 Application URL: http$(if ($EnableSSL) { 's' })://$ingressAddress" -ForegroundColor Green
            Write-Host "   Health Check: http$(if ($EnableSSL) { 's' })://$ingressAddress/health" -ForegroundColor White
        }
    } else {
        Write-Host "`n🌐 Application will be available at: http$(if ($EnableSSL) { 's' })://$Domain" -ForegroundColor Green
        Write-Host "   Add this to your hosts file or DNS: <ingress-ip> $Domain" -ForegroundColor Yellow
    }
    
    Write-Host "`n📋 Useful Commands:" -ForegroundColor Cyan
    Write-Host "  View logs: kubectl logs -f deployment/setlist-studio-web -n $Namespace" -ForegroundColor White
    Write-Host "  Scale pods: kubectl scale deployment setlist-studio-web --replicas=5 -n $Namespace" -ForegroundColor White
    Write-Host "  Port forward: kubectl port-forward service/setlist-studio-web-service 8080:80 -n $Namespace" -ForegroundColor White
    Write-Host "  Delete deployment: kubectl delete namespace $Namespace" -ForegroundColor White
    
    Write-Host "`n⚖️ Auto-scaling is configured:" -ForegroundColor Cyan
    Write-Host "  Min replicas: 2" -ForegroundColor White
    Write-Host "  Max replicas: 10" -ForegroundColor White
    Write-Host "  CPU threshold: 70%" -ForegroundColor White
    Write-Host "  Memory threshold: 80%" -ForegroundColor White
    
} catch {
    Write-Error "Kubernetes deployment failed: $($_.Exception.Message)"
    Write-Host "🔍 Checking pod status..." -ForegroundColor Yellow
    kubectl get pods -n $Namespace
    Write-Host "🔍 Checking events..." -ForegroundColor Yellow
    kubectl get events -n $Namespace --sort-by='.lastTimestamp'
    exit 1
}

Write-Host "`n✨ Kubernetes deployment completed! Your Setlist Studio is now running with auto-scaling." -ForegroundColor Green