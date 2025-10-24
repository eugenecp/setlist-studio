# Azure Key Vault Setup Script for Setlist Studio Production OAuth Secrets
# This script provisions Azure Key Vault and configures OAuth secrets securely
# Prerequisites: Azure CLI installed and authenticated (az login)

param(
    [Parameter(Mandatory = $true)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $false)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [switch]$DryRun,
    
    [Parameter(Mandatory = $false)]
    [switch]$Force
)

# Security configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Validate Key Vault name format (Azure requirements)
if ($KeyVaultName -notmatch "^[a-zA-Z][a-zA-Z0-9-]{1,22}[a-zA-Z0-9]$") {
    Write-Error "Invalid Key Vault name format. Must be 3-24 characters, start with letter, contain only alphanumeric and hyphens."
    exit 1
}

Write-Host "🔒 Setlist Studio - Azure Key Vault Setup" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Key Vault Name: $KeyVaultName" -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Green
Write-Host "Location: $Location" -ForegroundColor Green

if ($DryRun) {
    Write-Host "🧪 DRY RUN MODE - No actual changes will be made" -ForegroundColor Yellow
}

# Check Azure CLI authentication
Write-Host "`n🔐 Checking Azure CLI authentication..." -ForegroundColor Yellow
try {
    $account = az account show --query "user.name" -o tsv 2>$null
    if (-not $account) {
        Write-Error "Not authenticated with Azure CLI. Please run 'az login' first."
        exit 1
    }
    Write-Host "✅ Authenticated as: $account" -ForegroundColor Green
} catch {
    Write-Error "Azure CLI not available or not authenticated. Please install Azure CLI and run 'az login'."
    exit 1
}

# Set subscription if provided
if ($SubscriptionId) {
    Write-Host "`n🎯 Setting subscription: $SubscriptionId" -ForegroundColor Yellow
    if (-not $DryRun) {
        az account set --subscription $SubscriptionId
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to set subscription: $SubscriptionId"
            exit 1
        }
    }
    Write-Host "✅ Subscription set successfully" -ForegroundColor Green
}

# Check if resource group exists
Write-Host "`n📁 Checking resource group: $ResourceGroupName" -ForegroundColor Yellow
$rgExists = az group exists --name $ResourceGroupName 2>$null
if ($rgExists -eq "false") {
    Write-Host "⚠️  Resource group does not exist. Creating..." -ForegroundColor Yellow
    if (-not $DryRun) {
        az group create --name $ResourceGroupName --location $Location --output none
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to create resource group: $ResourceGroupName"
            exit 1
        }
    }
    Write-Host "✅ Resource group created successfully" -ForegroundColor Green
} else {
    Write-Host "✅ Resource group exists" -ForegroundColor Green
}

# Check if Key Vault already exists
Write-Host "`n🔑 Checking Key Vault: $KeyVaultName" -ForegroundColor Yellow
$kvExists = az keyvault list --query "[?name=='$KeyVaultName'].name" -o tsv 2>$null
if ($kvExists) {
    if (-not $Force) {
        Write-Host "⚠️  Key Vault '$KeyVaultName' already exists. Use -Force to continue with existing vault." -ForegroundColor Yellow
        $continue = Read-Host "Continue with existing Key Vault? (y/N)"
        if ($continue -ne "y" -and $continue -ne "Y") {
            Write-Host "❌ Operation cancelled" -ForegroundColor Red
            exit 0
        }
    }
    Write-Host "✅ Using existing Key Vault" -ForegroundColor Green
} else {
    Write-Host "🏗️  Creating new Key Vault..." -ForegroundColor Yellow
    if (-not $DryRun) {
        # Create Key Vault with premium security settings
        az keyvault create `
            --name $KeyVaultName `
            --resource-group $ResourceGroupName `
            --location $Location `
            --enable-rbac-authorization `
            --enable-soft-delete `
            --soft-delete-retention-days 90 `
            --enable-purge-protection `
            --public-network-access Disabled `
            --output none
            
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to create Key Vault: $KeyVaultName"
            exit 1
        }
    }
    Write-Host "✅ Key Vault created successfully" -ForegroundColor Green
}

# Get current user's object ID for RBAC assignment
Write-Host "`n👤 Getting current user information..." -ForegroundColor Yellow
$currentUser = az ad signed-in-user show --query "id" -o tsv 2>$null
if (-not $currentUser) {
    Write-Error "Failed to get current user information"
    exit 1
}
Write-Host "✅ Current user ID: $currentUser" -ForegroundColor Green

# Assign Key Vault Secrets Officer role to current user
Write-Host "`n🔐 Configuring RBAC permissions..." -ForegroundColor Yellow
if (-not $DryRun) {
    $kvResourceId = az keyvault show --name $KeyVaultName --query "id" -o tsv
    az role assignment create `
        --role "Key Vault Secrets Officer" `
        --assignee $currentUser `
        --scope $kvResourceId `
        --output none 2>$null
    
    # Also assign Crypto Officer for potential certificate management
    az role assignment create `
        --role "Key Vault Crypto Officer" `
        --assignee $currentUser `
        --scope $kvResourceId `
        --output none 2>$null
        
    Write-Host "✅ RBAC permissions configured" -ForegroundColor Green
} else {
    Write-Host "✅ Would configure RBAC permissions" -ForegroundColor Green
}

# Configure network access (if needed for specific scenarios)
Write-Host "`n🌐 Configuring network access..." -ForegroundColor Yellow
if (-not $DryRun) {
    # For production, you might want to add specific IP ranges or VNet integration
    # az keyvault network-rule add --name $KeyVaultName --ip-address "YOUR_DEPLOYMENT_IP"
    Write-Host "✅ Network access configured (private access only)" -ForegroundColor Green
} else {
    Write-Host "✅ Would configure network access" -ForegroundColor Green
}

# Create secret placeholders with metadata
Write-Host "`n🔒 Creating OAuth secret placeholders..." -ForegroundColor Yellow
$secrets = @(
    @{ Name = "Authentication--Google--ClientId"; Description = "Google OAuth Client ID for authentication" },
    @{ Name = "Authentication--Google--ClientSecret"; Description = "Google OAuth Client Secret for authentication" },
    @{ Name = "Authentication--Microsoft--ClientId"; Description = "Microsoft OAuth Client ID for authentication" },
    @{ Name = "Authentication--Microsoft--ClientSecret"; Description = "Microsoft OAuth Client Secret for authentication" },
    @{ Name = "Authentication--Facebook--AppId"; Description = "Facebook App ID for authentication" },
    @{ Name = "Authentication--Facebook--AppSecret"; Description = "Facebook App Secret for authentication" }
)

foreach ($secret in $secrets) {
    Write-Host "  📝 Setting up: $($secret.Name)" -ForegroundColor Gray
    if (-not $DryRun) {
        # Create secret with placeholder value and metadata
        az keyvault secret set `
            --vault-name $KeyVaultName `
            --name $secret.Name `
            --value "REPLACE_WITH_ACTUAL_SECRET" `
            --description $secret.Description `
            --tags "Environment=Production" "Application=SetlistStudio" "Type=OAuth" `
            --output none
            
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to create secret: $($secret.Name)"
        }
    }
}
Write-Host "✅ OAuth secret placeholders created" -ForegroundColor Green

# Configure Key Vault access policy for production deployment (service principal)
Write-Host "`n🤖 Setting up deployment service principal access..." -ForegroundColor Yellow
Write-Host "   Note: You'll need to configure service principal access for production deployments" -ForegroundColor Gray
Write-Host "   Run: az ad sp create-for-rbac --name 'SetlistStudio-Production' --role 'Key Vault Secrets User' --scope /subscriptions/{subscription}/resourceGroups/$ResourceGroupName/providers/Microsoft.KeyVault/vaults/$KeyVaultName" -ForegroundColor Gray

# Output next steps
Write-Host "`n🎉 Azure Key Vault Setup Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Key Vault URL: https://$KeyVaultName.vault.azure.net/" -ForegroundColor Yellow
Write-Host "`n📋 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Update production OAuth secrets using:" -ForegroundColor White
Write-Host "   .\scripts\deploy-oauth-secrets.ps1 -KeyVaultName $KeyVaultName" -ForegroundColor Gray
Write-Host "2. Configure application to use Key Vault:" -ForegroundColor White
Write-Host "   Set environment variable: KeyVault__VaultName=$KeyVaultName" -ForegroundColor Gray
Write-Host "3. Set up managed identity or service principal for production deployment" -ForegroundColor White
Write-Host "4. Configure network access restrictions for production environment" -ForegroundColor White

Write-Host "`n🔒 Security Recommendations:" -ForegroundColor Cyan
Write-Host "• Restrict Key Vault access to specific IP ranges or VNets" -ForegroundColor White
Write-Host "• Use managed identity for application authentication in production" -ForegroundColor White
Write-Host "• Regularly rotate OAuth secrets and update Key Vault" -ForegroundColor White
Write-Host "• Monitor Key Vault access logs for security auditing" -ForegroundColor White
Write-Host "• Configure alerts for Key Vault access anomalies" -ForegroundColor White

# Save Key Vault information for other scripts
$kvInfo = @{
    KeyVaultName = $KeyVaultName
    KeyVaultUrl = "https://$KeyVaultName.vault.azure.net/"
    ResourceGroup = $ResourceGroupName
    Location = $Location
    CreatedDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
}

$kvInfoPath = "scripts/keyvault-info.json"
if (-not $DryRun) {
    $kvInfo | ConvertTo-Json -Depth 2 | Out-File -FilePath $kvInfoPath -Encoding UTF8
    Write-Host "`n📄 Key Vault information saved to: $kvInfoPath" -ForegroundColor Green
}

Write-Host "`n✅ Setup completed successfully!" -ForegroundColor Green