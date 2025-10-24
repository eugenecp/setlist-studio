# PowerShell script to setup Docker secrets for Setlist Studio
# This script creates Docker secrets for OAuth credentials

param(
    [switch]$Interactive = $false,
    [switch]$RemoveExisting = $false
)

Write-Host "🔐 Setting up Docker secrets for Setlist Studio OAuth credentials" -ForegroundColor Green

# Function to create a Docker secret
function Create-Secret {
    param(
        [string]$SecretName,
        [string]$EnvVarName,
        [string]$PromptMessage
    )
    
    # Remove existing secret if it exists
    $existingSecret = docker secret ls --format "{{.Name}}" | Where-Object { $_ -eq $SecretName }
    if ($existingSecret -or $RemoveExisting) {
        Write-Host "📝 Removing existing secret: $SecretName" -ForegroundColor Yellow
        docker secret rm $SecretName 2>$null
    }
    
    # Get value from environment variable or prompt
    $secretValue = ""
    $envValue = [System.Environment]::GetEnvironmentVariable($EnvVarName)
    
    if ($envValue -and !$Interactive) {
        $secretValue = $envValue
        Write-Host "✅ Using $EnvVarName environment variable for $SecretName" -ForegroundColor Green
    } else {
        $secretValue = Read-Host -Prompt $PromptMessage -AsSecureString
        $secretValue = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secretValue))
        
        if ([string]::IsNullOrEmpty($secretValue)) {
            Write-Host "⚠️  Warning: Empty value provided for $SecretName" -ForegroundColor Yellow
            $secretValue = "PLACEHOLDER_$($SecretName.Replace('-', '_').ToUpper())"
        }
    }
    
    # Create the secret
    $secretValue | docker secret create $SecretName -
    Write-Host "🔒 Created secret: $SecretName" -ForegroundColor Green
}

# Check if Docker Swarm is initialized
$swarmState = docker info --format '{{.Swarm.LocalNodeState}}' 2>$null
if ($swarmState -ne "active") {
    Write-Host "🚀 Initializing Docker Swarm (required for secrets)..." -ForegroundColor Yellow
    docker swarm init --advertise-addr 127.0.0.1
}

Write-Host ""
Write-Host "📱 Creating OAuth secrets..." -ForegroundColor Cyan
if ($Interactive) {
    Write-Host "Interactive mode: You will be prompted for each secret" -ForegroundColor Yellow
} else {
    Write-Host "Using environment variables where available" -ForegroundColor Yellow
}
Write-Host ""

try {
    # Create OAuth secrets
    Create-Secret "setliststudio_google_client_id" "GOOGLE_CLIENT_ID" "Enter Google OAuth Client ID"
    Create-Secret "setliststudio_google_client_secret" "GOOGLE_CLIENT_SECRET" "Enter Google OAuth Client Secret"
    Create-Secret "setliststudio_microsoft_client_id" "MICROSOFT_CLIENT_ID" "Enter Microsoft OAuth Client ID"
    Create-Secret "setliststudio_microsoft_client_secret" "MICROSOFT_CLIENT_SECRET" "Enter Microsoft OAuth Client Secret"
    Create-Secret "setliststudio_facebook_app_id" "FACEBOOK_APP_ID" "Enter Facebook App ID"
    Create-Secret "setliststudio_facebook_app_secret" "FACEBOOK_APP_SECRET" "Enter Facebook App Secret"

    Write-Host ""
    Write-Host "✅ Docker secrets created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🚀 To deploy with secrets, use:" -ForegroundColor Cyan
    Write-Host "   docker-compose -f docker-compose.yml -f docker-compose.secrets.yml up -d" -ForegroundColor White
    Write-Host ""
    Write-Host "🔍 To view secrets (names only):" -ForegroundColor Cyan
    Write-Host "   docker secret ls" -ForegroundColor White
    Write-Host ""
    Write-Host "🗑️  To remove all secrets:" -ForegroundColor Cyan
    Write-Host "   docker secret rm setliststudio_google_client_id setliststudio_google_client_secret setliststudio_microsoft_client_id setliststudio_microsoft_client_secret setliststudio_facebook_app_id setliststudio_facebook_app_secret" -ForegroundColor White
}
catch {
    Write-Host "❌ Error creating secrets: $_" -ForegroundColor Red
    exit 1
}