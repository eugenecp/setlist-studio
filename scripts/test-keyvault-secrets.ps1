# Test Azure Key Vault Secret Access
# This script validates that OAuth secrets are properly deployed and accessible in Azure Key Vault

param(
    [Parameter(Mandatory = $true)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory = $false)]
    [switch]$Detailed,
    
    [Parameter(Mandatory = $false)]
    [switch]$TestConnection
)

# Security configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Host "🔍 Azure Key Vault Secret Validation" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Key Vault: $KeyVaultName" -ForegroundColor Green

# Check Azure CLI authentication
Write-Host "`n🔐 Checking Azure authentication..." -ForegroundColor Yellow
try {
    $account = az account show --query "user.name" -o tsv 2>$null
    if (-not $account) {
        Write-Error "Not authenticated with Azure CLI. Please run 'az login' first."
        exit 1
    }
    Write-Host "✅ Authenticated as: $account" -ForegroundColor Green
} catch {
    Write-Error "Azure CLI not available. Please install Azure CLI and authenticate."
    exit 1
}

# Test Key Vault connectivity
if ($TestConnection) {
    Write-Host "`n🔗 Testing Key Vault connectivity..." -ForegroundColor Yellow
    try {
        $kvInfo = az keyvault show --name $KeyVaultName --query "{name:name,location:location,sku:properties.sku.name}" 2>$null | ConvertFrom-Json
        if ($kvInfo) {
            Write-Host "✅ Key Vault accessible" -ForegroundColor Green
            Write-Host "   Name: $($kvInfo.name)" -ForegroundColor Gray
            Write-Host "   Location: $($kvInfo.location)" -ForegroundColor Gray  
            Write-Host "   SKU: $($kvInfo.sku)" -ForegroundColor Gray
        } else {
            Write-Error "Key Vault '$KeyVaultName' not found or not accessible"
            exit 1
        }
    } catch {
        Write-Error "Failed to access Key Vault '$KeyVaultName': $($_.Exception.Message)"
        exit 1
    }
}

# Define OAuth secrets to validate
$oauthSecrets = @(
    @{
        Name = "Authentication--Google--ClientId"
        Provider = "Google"
        Type = "ClientId"
        Required = $false
    },
    @{
        Name = "Authentication--Google--ClientSecret" 
        Provider = "Google"
        Type = "ClientSecret"
        Required = $false
    },
    @{
        Name = "Authentication--Microsoft--ClientId"
        Provider = "Microsoft"
        Type = "ClientId"
        Required = $false
    },
    @{
        Name = "Authentication--Microsoft--ClientSecret"
        Provider = "Microsoft"
        Type = "ClientSecret"
        Required = $false
    },
    @{
        Name = "Authentication--Facebook--AppId"
        Provider = "Facebook"
        Type = "AppId"
        Required = $false
    },
    @{
        Name = "Authentication--Facebook--AppSecret"
        Provider = "Facebook"
        Type = "AppSecret"
        Required = $false
    }
)

Write-Host "`n🔍 Validating OAuth secrets..." -ForegroundColor Yellow

$validSecrets = 0
$configuredProviders = @()
$missingSecrets = @()
$placeholderSecrets = @()

foreach ($secret in $oauthSecrets) {
    Write-Host "  📝 Checking: $($secret.Name)" -ForegroundColor Gray
    
    try {
        # Try to get the secret value
        $secretValue = az keyvault secret show --vault-name $KeyVaultName --name $secret.Name --query "value" -o tsv 2>$null
        
        if ($secretValue) {
            # Check if it's a placeholder value
            $placeholders = @(
                "REPLACE_WITH_ACTUAL_SECRET",
                "YOUR_$($secret.Provider.ToUpper())_$($secret.Type.ToUpper())",
                "your_$($secret.Provider.ToLower())_$($secret.Type.ToLower())_here"
            )
            
            $isPlaceholder = $false
            foreach ($placeholder in $placeholders) {
                if ($secretValue -eq $placeholder) {
                    $isPlaceholder = $true
                    break
                }
            }
            
            if ($isPlaceholder) {
                Write-Host "    ⚠️  Contains placeholder value" -ForegroundColor Yellow
                $placeholderSecrets += $secret.Name
            } else {
                Write-Host "    ✅ Configured" -ForegroundColor Green
                $validSecrets++
                
                if ($Detailed) {
                    # Show secret metadata without exposing the value
                    $secretInfo = az keyvault secret show --vault-name $KeyVaultName --name $secret.Name --query "{updated:attributes.updated,tags:tags}" 2>$null | ConvertFrom-Json
                    if ($secretInfo.updated) {
                        Write-Host "       Updated: $($secretInfo.updated)" -ForegroundColor DarkGray
                    }
                    if ($secretInfo.tags) {
                        $tags = $secretInfo.tags | ConvertTo-Json -Compress
                        Write-Host "       Tags: $tags" -ForegroundColor DarkGray
                    }
                }
            }
        } else {
            Write-Host "    ❌ Not found" -ForegroundColor Red
            $missingSecrets += $secret.Name
        }
    } catch {
        Write-Host "    ❌ Access denied or error" -ForegroundColor Red
        $missingSecrets += $secret.Name
    }
}

# Analyze provider configurations
Write-Host "`n📊 Provider Analysis:" -ForegroundColor Cyan

$providers = @("Google", "Microsoft", "Facebook")
foreach ($provider in $providers) {
    $providerSecrets = $oauthSecrets | Where-Object { $_.Provider -eq $provider }
    $configuredCount = 0
    
    foreach ($secret in $providerSecrets) {
        $secretValue = az keyvault secret show --vault-name $KeyVaultName --name $secret.Name --query "value" -o tsv 2>$null
        if ($secretValue -and $secretValue -ne "REPLACE_WITH_ACTUAL_SECRET") {
            $configuredCount++
        }
    }
    
    if ($configuredCount -eq $providerSecrets.Count) {
        Write-Host "  ✅ $provider: Fully configured ($configuredCount/$($providerSecrets.Count))" -ForegroundColor Green
        $configuredProviders += $provider
    } elseif ($configuredCount -gt 0) {
        Write-Host "  ⚠️  $provider: Partially configured ($configuredCount/$($providerSecrets.Count))" -ForegroundColor Yellow
    } else {
        Write-Host "  ❌ $provider: Not configured (0/$($providerSecrets.Count))" -ForegroundColor Red
    }
}

# Summary
Write-Host "`n📋 Validation Summary" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host "✅ Valid secrets: $validSecrets/$($oauthSecrets.Count)" -ForegroundColor Green
Write-Host "🏢 Configured providers: $($configuredProviders.Count) ($($configuredProviders -join ', '))" -ForegroundColor Green

if ($placeholderSecrets.Count -gt 0) {
    Write-Host "⚠️  Placeholder secrets: $($placeholderSecrets.Count)" -ForegroundColor Yellow
    $placeholderSecrets | ForEach-Object { Write-Host "   • $_" -ForegroundColor Gray }
}

if ($missingSecrets.Count -gt 0) {
    Write-Host "❌ Missing secrets: $($missingSecrets.Count)" -ForegroundColor Red
    $missingSecrets | ForEach-Object { Write-Host "   • $_" -ForegroundColor Gray }
}

# Recommendations
Write-Host "`n💡 Recommendations:" -ForegroundColor Cyan

if ($configuredProviders.Count -eq 0) {
    Write-Host "• Configure at least one OAuth provider for user authentication" -ForegroundColor Yellow
    Write-Host "• Run: .\scripts\deploy-oauth-secrets.ps1 -KeyVaultName $KeyVaultName -Interactive" -ForegroundColor Gray
}

if ($placeholderSecrets.Count -gt 0) {
    Write-Host "• Replace placeholder values with actual OAuth credentials" -ForegroundColor Yellow
    Write-Host "• Run: .\scripts\deploy-oauth-secrets.ps1 -KeyVaultName $KeyVaultName -Force" -ForegroundColor Gray
}

if ($missingSecrets.Count -gt 0) {
    Write-Host "• Deploy missing OAuth secrets to Key Vault" -ForegroundColor Yellow
    Write-Host "• Run: .\scripts\deploy-oauth-secrets.ps1 -KeyVaultName $KeyVaultName" -ForegroundColor Gray
}

# Application configuration check
Write-Host "`n🔧 Application Configuration:" -ForegroundColor Cyan
Write-Host "Set this environment variable in your production deployment:" -ForegroundColor White
Write-Host "KeyVault__VaultName=$KeyVaultName" -ForegroundColor Gray

# Exit with appropriate code
if ($configuredProviders.Count -gt 0 -and $placeholderSecrets.Count -eq 0) {
    Write-Host "`n🎉 Key Vault validation successful!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n⚠️  Key Vault validation completed with issues" -ForegroundColor Yellow
    exit 1
}