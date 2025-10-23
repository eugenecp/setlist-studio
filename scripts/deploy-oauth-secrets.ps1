# Deploy OAuth Secrets to Azure Key Vault
# This script securely deploys OAuth provider secrets to Azure Key Vault for production use
# Prerequisites: Azure CLI authenticated and Key Vault already created

param(
    [Parameter(Mandatory = $true)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory = $false)]
    [string]$GoogleClientId,
    
    [Parameter(Mandatory = $false)]
    [string]$GoogleClientSecret,
    
    [Parameter(Mandatory = $false)]
    [string]$MicrosoftClientId,
    
    [Parameter(Mandatory = $false)]
    [string]$MicrosoftClientSecret,
    
    [Parameter(Mandatory = $false)]
    [string]$FacebookAppId,
    
    [Parameter(Mandatory = $false)]
    [string]$FacebookAppSecret,
    
    [Parameter(Mandatory = $false)]
    [string]$SecretsFile,
    
    [Parameter(Mandatory = $false)]
    [switch]$Interactive,
    
    [Parameter(Mandatory = $false)]
    [switch]$DryRun,
    
    [Parameter(Mandatory = $false)]
    [switch]$Force
)

# Security configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Host "🔐 Setlist Studio - OAuth Secrets Deployment" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Key Vault: $KeyVaultName" -ForegroundColor Green

if ($DryRun) {
    Write-Host "🧪 DRY RUN MODE - No actual secrets will be deployed" -ForegroundColor Yellow
}

# Check Azure CLI authentication
Write-Host "`n🔓 Checking Azure authentication..." -ForegroundColor Yellow
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

# Verify Key Vault exists and accessible
Write-Host "`n🔑 Verifying Key Vault access..." -ForegroundColor Yellow
try {
    $kvInfo = az keyvault show --name $KeyVaultName --query "{name:name,location:location}" 2>$null | ConvertFrom-Json
    if (-not $kvInfo) {
        Write-Error "Key Vault '$KeyVaultName' not found or not accessible"
        exit 1
    }
    Write-Host "✅ Key Vault accessible: $($kvInfo.name) in $($kvInfo.location)" -ForegroundColor Green
} catch {
    Write-Error "Failed to access Key Vault '$KeyVaultName'. Check permissions and vault name."
    exit 1
}

# Function to securely read secrets from file
function Read-SecretsFromFile($filePath) {
    if (-not (Test-Path $filePath)) {
        Write-Error "Secrets file not found: $filePath"
        exit 1
    }
    
    Write-Host "📄 Reading secrets from file: $filePath" -ForegroundColor Yellow
    
    try {
        $content = Get-Content $filePath -Raw
        if ($filePath.EndsWith(".json")) {
            return $content | ConvertFrom-Json
        } elseif ($filePath.EndsWith(".env")) {
            # Parse .env file format
            $secrets = @{}
            $content -split "`n" | ForEach-Object {
                $line = $_.Trim()
                if ($line -and -not $line.StartsWith("#")) {
                    $parts = $line -split "=", 2
                    if ($parts.Length -eq 2) {
                        $key = $parts[0].Trim()
                        $value = $parts[1].Trim().Trim('"').Trim("'")
                        $secrets[$key] = $value
                    }
                }
            }
            return $secrets
        } else {
            Write-Error "Unsupported file format. Use .json or .env file."
            exit 1
        }
    } catch {
        Write-Error "Failed to parse secrets file: $($_.Exception.Message)"
        exit 1
    }
}

# Function to securely prompt for secrets
function Read-SecretInteractively($provider, $secretType) {
    $prompt = "Enter $provider $secretType"
    if ($secretType -like "*Secret*" -or $secretType -like "*AppSecret*") {
        return Read-Host -Prompt $prompt -AsSecureString | ConvertFrom-SecureString -AsPlainText
    } else {
        return Read-Host -Prompt $prompt
    }
}

# Collect secrets from various sources
$secrets = @{}

if ($SecretsFile) {
    Write-Host "`n📖 Loading secrets from file..." -ForegroundColor Yellow
    $fileSecrets = Read-SecretsFromFile $SecretsFile
    
    # Map common file formats to our secret names
    $secretMappings = @{
        "GOOGLE_CLIENT_ID" = "GoogleClientId"
        "GOOGLE_CLIENT_SECRET" = "GoogleClientSecret"
        "MICROSOFT_CLIENT_ID" = "MicrosoftClientId"
        "MICROSOFT_CLIENT_SECRET" = "MicrosoftClientSecret"
        "FACEBOOK_APP_ID" = "FacebookAppId"
        "FACEBOOK_APP_SECRET" = "FacebookAppSecret"
        "GoogleClientId" = "GoogleClientId"
        "GoogleClientSecret" = "GoogleClientSecret"
        "MicrosoftClientId" = "MicrosoftClientId"
        "MicrosoftClientSecret" = "MicrosoftClientSecret"
        "FacebookAppId" = "FacebookAppId"
        "FacebookAppSecret" = "FacebookAppSecret"
    }
    
    foreach ($mapping in $secretMappings.GetEnumerator()) {
        if ($fileSecrets.PSObject.Properties.Name -contains $mapping.Key -or $fileSecrets.ContainsKey($mapping.Key)) {
            $value = if ($fileSecrets.PSObject.Properties.Name -contains $mapping.Key) { 
                $fileSecrets.$($mapping.Key) 
            } else { 
                $fileSecrets[$mapping.Key] 
            }
            if ($value -and $value -ne "your_${mapping.Key.ToLower()}_here" -and $value -ne "YOUR_$($mapping.Key.ToUpper())") {
                $secrets[$mapping.Value] = $value
            }
        }
    }
}

# Override with command line parameters
if ($GoogleClientId) { $secrets["GoogleClientId"] = $GoogleClientId }
if ($GoogleClientSecret) { $secrets["GoogleClientSecret"] = $GoogleClientSecret }
if ($MicrosoftClientId) { $secrets["MicrosoftClientId"] = $MicrosoftClientId }
if ($MicrosoftClientSecret) { $secrets["MicrosoftClientSecret"] = $MicrosoftClientSecret }
if ($FacebookAppId) { $secrets["FacebookAppId"] = $FacebookAppId }
if ($FacebookAppSecret) { $secrets["FacebookAppSecret"] = $FacebookAppSecret }

# Interactive mode for missing secrets
if ($Interactive) {
    Write-Host "`n🔐 Interactive secret entry mode" -ForegroundColor Yellow
    Write-Host "Leave blank to skip optional providers" -ForegroundColor Gray
    
    $providers = @{
        "Google" = @("GoogleClientId", "GoogleClientSecret")
        "Microsoft" = @("MicrosoftClientId", "MicrosoftClientSecret") 
        "Facebook" = @("FacebookAppId", "FacebookAppSecret")
    }
    
    foreach ($provider in $providers.GetEnumerator()) {
        Write-Host "`n📱 $($provider.Key) OAuth Configuration:" -ForegroundColor Cyan
        foreach ($secretKey in $provider.Value) {
            if (-not $secrets.ContainsKey($secretKey) -or -not $secrets[$secretKey]) {
                $secretType = $secretKey -replace "^$($provider.Key)", ""
                $value = Read-SecretInteractively $provider.Key $secretType
                if ($value) {
                    $secrets[$secretKey] = $value
                }
            } else {
                Write-Host "  ✅ $secretKey already provided" -ForegroundColor Green
            }
        }
    }
}

# Validate we have at least one complete provider configuration
$hasCompleteProvider = $false
$providers = @{
    "Google" = @("GoogleClientId", "GoogleClientSecret")
    "Microsoft" = @("MicrosoftClientId", "MicrosoftClientSecret")
    "Facebook" = @("FacebookAppId", "FacebookAppSecret")
}

Write-Host "`n🔍 Validating OAuth provider configurations..." -ForegroundColor Yellow
foreach ($provider in $providers.GetEnumerator()) {
    $hasId = $secrets.ContainsKey($provider.Value[0]) -and $secrets[$provider.Value[0]]
    $hasSecret = $secrets.ContainsKey($provider.Value[1]) -and $secrets[$provider.Value[1]]
    
    if ($hasId -and $hasSecret) {
        Write-Host "  ✅ $($provider.Key): Complete configuration" -ForegroundColor Green
        $hasCompleteProvider = $true
    } elseif ($hasId -or $hasSecret) {
        Write-Host "  ⚠️  $($provider.Key): Incomplete configuration (missing $( if(-not $hasId){'ID'}else{'Secret'} ))" -ForegroundColor Yellow
    } else {
        Write-Host "  ⏭️  $($provider.Key): Not configured (skipping)" -ForegroundColor Gray
    }
}

if (-not $hasCompleteProvider) {
    Write-Error "No complete OAuth provider configuration found. At least one provider must have both ID and secret."
    exit 1
}

# Deploy secrets to Key Vault
Write-Host "`n🚀 Deploying secrets to Azure Key Vault..." -ForegroundColor Yellow

$secretMappings = @{
    "GoogleClientId" = "Authentication--Google--ClientId"
    "GoogleClientSecret" = "Authentication--Google--ClientSecret"
    "MicrosoftClientId" = "Authentication--Microsoft--ClientId"
    "MicrosoftClientSecret" = "Authentication--Microsoft--ClientSecret"
    "FacebookAppId" = "Authentication--Facebook--AppId"
    "FacebookAppSecret" = "Authentication--Facebook--AppSecret"
}

$deployedSecrets = @()
$skippedSecrets = @()

foreach ($mapping in $secretMappings.GetEnumerator()) {
    $localKey = $mapping.Key
    $kvSecretName = $mapping.Value
    
    if ($secrets.ContainsKey($localKey) -and $secrets[$localKey]) {
        Write-Host "  📝 Deploying: $kvSecretName" -ForegroundColor Gray
        
        if (-not $DryRun) {
            try {
                # Check if secret already exists
                $existingSecret = az keyvault secret show --vault-name $KeyVaultName --name $kvSecretName --query "value" -o tsv 2>$null
                
                if ($existingSecret -and -not $Force) {
                    Write-Host "    ⚠️  Secret already exists. Use -Force to overwrite." -ForegroundColor Yellow
                    $overwrite = Read-Host "    Overwrite existing secret? (y/N)"
                    if ($overwrite -ne "y" -and $overwrite -ne "Y") {
                        $skippedSecrets += $kvSecretName
                        continue
                    }
                }
                
                # Deploy the secret
                az keyvault secret set `
                    --vault-name $KeyVaultName `
                    --name $kvSecretName `
                    --value $secrets[$localKey] `
                    --description "OAuth secret for Setlist Studio production deployment" `
                    --tags "Environment=Production" "Application=SetlistStudio" "Type=OAuth" "UpdatedBy=$account" "UpdatedDate=$(Get-Date -Format 'yyyy-MM-dd')" `
                    --output none
                    
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "    ✅ Successfully deployed" -ForegroundColor Green
                    $deployedSecrets += $kvSecretName
                } else {
                    Write-Warning "    ❌ Failed to deploy: $kvSecretName"
                }
            } catch {
                Write-Warning "    ❌ Error deploying $kvSecretName : $($_.Exception.Message)"
            }
        } else {
            Write-Host "    ✅ Would deploy successfully (dry run)" -ForegroundColor Green
            $deployedSecrets += $kvSecretName
        }
    } else {
        Write-Host "  ⏭️  Skipping: $kvSecretName (not provided)" -ForegroundColor Gray
        $skippedSecrets += $kvSecretName
    }
}

# Clear sensitive data from memory
foreach ($key in $secrets.Keys) {
    $secrets[$key] = $null
}
$secrets.Clear()

# Summary
Write-Host "`n📊 Deployment Summary" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan
Write-Host "✅ Deployed: $($deployedSecrets.Count) secrets" -ForegroundColor Green
if ($deployedSecrets.Count -gt 0) {
    $deployedSecrets | ForEach-Object { Write-Host "   • $_" -ForegroundColor Gray }
}

Write-Host "⏭️  Skipped: $($skippedSecrets.Count) secrets" -ForegroundColor Yellow
if ($skippedSecrets.Count -gt 0) {
    $skippedSecrets | ForEach-Object { Write-Host "   • $_" -ForegroundColor Gray }
}

if ($deployedSecrets.Count -gt 0) {
    Write-Host "`n🎉 OAuth secrets deployed successfully!" -ForegroundColor Green
    Write-Host "`n📋 Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Set environment variable in production: KeyVault__VaultName=$KeyVaultName" -ForegroundColor White
    Write-Host "2. Configure managed identity or service principal for your application" -ForegroundColor White
    Write-Host "3. Test authentication with deployed secrets" -ForegroundColor White
    Write-Host "4. Remove local secret files and clear environment variables" -ForegroundColor White
    
    Write-Host "`n🔒 Security Reminder:" -ForegroundColor Red
    Write-Host "• Delete any local secret files after successful deployment" -ForegroundColor White
    Write-Host "• Clear PowerShell history if secrets were entered interactively" -ForegroundColor White
    Write-Host "• Rotate OAuth secrets regularly and update Key Vault" -ForegroundColor White
    Write-Host "• Monitor Key Vault access logs for security" -ForegroundColor White
} else {
    Write-Host "`n⚠️  No secrets were deployed. Check configuration and try again." -ForegroundColor Yellow
}

Write-Host "`n✅ Deployment completed!" -ForegroundColor Green