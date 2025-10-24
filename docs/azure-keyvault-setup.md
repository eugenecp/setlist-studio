# Azure Key Vault Setup Guide for Setlist Studio

This guide walks you through setting up Azure Key Vault to securely store OAuth secrets for Setlist Studio production deployments.

## 🎯 Overview

Azure Key Vault provides enterprise-grade secret management for OAuth provider credentials (Google, Microsoft, Facebook) used by Setlist Studio. This setup ensures:

- **Secure Secret Storage**: OAuth secrets are encrypted and stored in Azure Key Vault
- **Access Control**: RBAC-based access control with audit logging
- **Secret Rotation**: Easy secret updates without application redeployment
- **High Availability**: Azure-managed infrastructure with 99.9% SLA
- **Compliance**: Meets enterprise security and compliance requirements

## 🛠️ Prerequisites

### Required Tools
- **Azure CLI**: [Install Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- **PowerShell 7+**: [Install PowerShell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell)
- **Azure Subscription**: With permissions to create Key Vault and assign RBAC roles

### Required Permissions
- **Contributor** role on the resource group
- **Key Vault Administrator** or **Owner** role for Key Vault creation
- **Application Administrator** role for service principal creation (CI/CD setup)

### OAuth Provider Setup
Before proceeding, ensure you have configured OAuth applications with these providers:

#### Google OAuth Setup
1. Go to [Google Cloud Console](https://console.developers.google.com/)
2. Create a new project or select existing one
3. Enable Google+ API or Google Identity Services API
4. Create OAuth 2.0 credentials (Web application)
5. Add authorized redirect URIs:
   - Production: `https://yourdomain.com/signin-google`
   - Development: `http://localhost:5000/signin-google`
6. Note the **Client ID** and **Client Secret**

#### Microsoft OAuth Setup
1. Go to [Azure Portal](https://portal.azure.com/)
2. Navigate to **Azure Active Directory** > **App registrations**
3. Create a new application registration
4. Configure authentication:
   - Platform: Web
   - Redirect URIs: `https://yourdomain.com/signin-microsoft`
5. Generate a client secret under **Certificates & secrets**
6. Note the **Application (client) ID** and **Client Secret**

#### Facebook OAuth Setup
1. Go to [Facebook Developers](https://developers.facebook.com/)
2. Create a new app or select existing one
3. Add **Facebook Login** product
4. Configure OAuth settings:
   - Valid OAuth Redirect URIs: `https://yourdomain.com/signin-facebook`
5. Note the **App ID** and **App Secret** from app settings

## 🚀 Step-by-Step Setup

### Step 1: Authentication Setup

```powershell
# Authenticate with Azure CLI
az login

# Select your subscription (if you have multiple)
az account list --output table
az account set --subscription "Your Subscription Name"

# Verify authentication
az account show
```

### Step 2: Azure Key Vault Creation

```powershell
# Navigate to the Setlist Studio project directory
cd path\to\setlist-studio

# Run the Key Vault setup script
.\scripts\setup-azure-keyvault.ps1 `
    -KeyVaultName "setliststudio-prod-kv" `
    -ResourceGroupName "setliststudio-production" `
    -Location "East US"
```

**Parameters:**
- `KeyVaultName`: Unique name for your Key Vault (3-24 characters, alphanumeric and hyphens)
- `ResourceGroupName`: Azure resource group name (will be created if it doesn't exist)
- `Location`: Azure region for the Key Vault

**Example Output:**
```
🔒 Setlist Studio - Azure Key Vault Setup
================================================
Key Vault Name: setliststudio-prod-kv
Resource Group: setliststudio-production
Location: East US

✅ Authenticated as: user@domain.com
✅ Resource group exists
✅ Key Vault created successfully
✅ RBAC permissions configured
✅ OAuth secret placeholders created
```

### Step 3: Deploy OAuth Secrets

You have several options for deploying secrets:

#### Option A: Interactive Deployment (Recommended for Initial Setup)
```powershell
.\scripts\deploy-oauth-secrets.ps1 `
    -KeyVaultName "setliststudio-prod-kv" `
    -Interactive
```

This will prompt you for each OAuth secret:
```
🔐 Interactive secret entry mode
Leave blank to skip optional providers

📱 Google OAuth Configuration:
Enter Google ClientId: 123456789-abc.apps.googleusercontent.com
Enter Google ClientSecret: ********

📱 Microsoft OAuth Configuration:
Enter Microsoft ClientId: 12345678-1234-1234-1234-123456789012
Enter Microsoft ClientSecret: ********
```

#### Option B: Command Line Parameters
```powershell
.\scripts\deploy-oauth-secrets.ps1 `
    -KeyVaultName "setliststudio-prod-kv" `
    -GoogleClientId "your-google-client-id" `
    -GoogleClientSecret "your-google-client-secret" `
    -MicrosoftClientId "your-microsoft-client-id" `
    -MicrosoftClientSecret "your-microsoft-client-secret"
```

#### Option C: From Environment File
Create a `.env.prod` file (never commit this to version control):
```env
GOOGLE_CLIENT_ID=your_google_client_id_here
GOOGLE_CLIENT_SECRET=your_google_client_secret_here
MICROSOFT_CLIENT_ID=your_microsoft_client_id_here
MICROSOFT_CLIENT_SECRET=your_microsoft_client_secret_here
FACEBOOK_APP_ID=your_facebook_app_id_here
FACEBOOK_APP_SECRET=your_facebook_app_secret_here
```

Then deploy:
```powershell
.\scripts\deploy-oauth-secrets.ps1 `
    -KeyVaultName "setliststudio-prod-kv" `
    -SecretsFile ".env.prod"
```

### Step 4: Configure Application for Production

#### Set Environment Variables
Your production environment needs this configuration:

```bash
# Key Vault Configuration
KeyVault__VaultName=setliststudio-prod-kv

# Azure Authentication (Managed Identity recommended)
AZURE_CLIENT_ID=your-managed-identity-client-id
```

#### Docker Deployment Example
```yaml
# docker-compose.prod.yml
services:
  setliststudio-web:
    environment:
      - KeyVault__VaultName=setliststudio-prod-kv
      - AZURE_CLIENT_ID=${AZURE_MANAGED_IDENTITY_CLIENT_ID}
    # ... other configuration
```

#### Azure Container Instances Example
```bash
az container create \
  --resource-group setliststudio-production \
  --name setliststudio-app \
  --image your-registry/setliststudio:latest \
  --assign-identity \
  --environment-variables KeyVault__VaultName=setliststudio-prod-kv \
  --ports 80
```

### Step 5: Configure Managed Identity (Recommended)

For production deployments, use Azure Managed Identity for secure authentication:

```powershell
# Create a managed identity
az identity create \
  --resource-group setliststudio-production \
  --name setliststudio-app-identity

# Get the identity details
$identity = az identity show \
  --resource-group setliststudio-production \
  --name setliststudio-app-identity | ConvertFrom-Json

# Grant Key Vault access to the managed identity
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $identity.principalId \
  --scope "/subscriptions/{subscription-id}/resourceGroups/setliststudio-production/providers/Microsoft.KeyVault/vaults/setliststudio-prod-kv"
```

## 🔒 Security Best Practices

### Network Security
```powershell
# Restrict Key Vault to specific IP ranges (optional)
az keyvault network-rule add \
  --name setliststudio-prod-kv \
  --ip-address "YOUR_PRODUCTION_IP/32"

# Or integrate with Virtual Network
az keyvault network-rule add \
  --name setliststudio-prod-kv \
  --vnet-name production-vnet \
  --subnet production-subnet
```

### Monitoring and Auditing
```powershell
# Enable Key Vault logging
az monitor diagnostic-settings create \
  --name kv-audit-logs \
  --resource "/subscriptions/{subscription-id}/resourceGroups/setliststudio-production/providers/Microsoft.KeyVault/vaults/setliststudio-prod-kv" \
  --logs '[{"category":"AuditEvent","enabled":true}]' \
  --workspace "/subscriptions/{subscription-id}/resourceGroups/setliststudio-production/providers/Microsoft.OperationalInsights/workspaces/setliststudio-logs"
```

### Secret Rotation Schedule
Set up regular secret rotation:
1. **Monthly**: Review and rotate OAuth secrets
2. **Quarterly**: Audit Key Vault access permissions
3. **Annually**: Review and update security policies

### Access Control
- Use **Key Vault Secrets User** role for applications (read-only)
- Use **Key Vault Secrets Officer** role for administrators
- Never use **Key Vault Administrator** for applications
- Regularly review and remove unused access

## 🔄 Secret Management Operations

### View Current Secrets
```powershell
# List all secrets
az keyvault secret list --vault-name setliststudio-prod-kv --output table

# View secret metadata (not the value)
az keyvault secret show \
  --vault-name setliststudio-prod-kv \
  --name "Authentication--Google--ClientId" \
  --query "{name:name, updated:attributes.updated, tags:tags}"
```

### Update Secrets
```powershell
# Update a single secret
az keyvault secret set \
  --vault-name setliststudio-prod-kv \
  --name "Authentication--Google--ClientSecret" \
  --value "new-secret-value"

# Bulk update using the deployment script
.\scripts\deploy-oauth-secrets.ps1 \
  -KeyVaultName "setliststudio-prod-kv" \
  -Force `
  -GoogleClientSecret "new-google-secret"
```

### Backup and Recovery
```powershell
# Backup Key Vault (requires premium SKU)
az backup vault create \
  --resource-group setliststudio-production \
  --name setliststudio-backup-vault \
  --location "East US"

# Create backup policy for Key Vault
az backup policy create \
  --vault-name setliststudio-backup-vault \
  --resource-group setliststudio-production \
  --name keyvault-daily-backup \
  --policy backup-policy.json
```

## 🚨 Troubleshooting

### Common Issues and Solutions

#### 1. Key Vault Access Denied
```
Error: The user, group or application does not have secrets list permission
```

**Solution:**
```powershell
# Check current permissions
az role assignment list --scope "/subscriptions/{subscription}/resourceGroups/setliststudio-production/providers/Microsoft.KeyVault/vaults/setliststudio-prod-kv"

# Grant appropriate permissions
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee your-user@domain.com \
  --scope "/subscriptions/{subscription}/resourceGroups/setliststudio-production/providers/Microsoft.KeyVault/vaults/setliststudio-prod-kv"
```

#### 2. Authentication Failed in Application
```
Error: ManagedIdentityCredential authentication failed
```

**Solution:**
1. Verify managed identity is properly assigned to your Azure resource
2. Check that the identity has correct Key Vault permissions
3. Ensure `KeyVault__VaultName` environment variable is set correctly

#### 3. Secret Not Found
```
Error: Secret not found: Authentication--Google--ClientId
```

**Solution:**
```powershell
# Verify secret exists
az keyvault secret list --vault-name setliststudio-prod-kv | grep "Authentication--Google--ClientId"

# Redeploy if missing
.\scripts\deploy-oauth-secrets.ps1 -KeyVaultName "setliststudio-prod-kv" -Interactive
```

### Testing Key Vault Integration

Create a test script to validate the setup:

```powershell
# test-keyvault-access.ps1
param([string]$KeyVaultName)

Write-Host "Testing Key Vault access..." -ForegroundColor Yellow

$secrets = @(
    "Authentication--Google--ClientId",
    "Authentication--Google--ClientSecret",
    "Authentication--Microsoft--ClientId",
    "Authentication--Microsoft--ClientSecret"
)

foreach ($secret in $secrets) {
    try {
        $value = az keyvault secret show --vault-name $KeyVaultName --name $secret --query "value" -o tsv 2>$null
        if ($value -and $value -ne "REPLACE_WITH_ACTUAL_SECRET") {
            Write-Host "✅ $secret: Available" -ForegroundColor Green
        } else {
            Write-Host "⚠️  $secret: Not configured" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ $secret: Access denied" -ForegroundColor Red
    }
}
```

## 📞 Support and Resources

### Azure Documentation
- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Managed Identity Documentation](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [Azure RBAC Documentation](https://docs.microsoft.com/en-us/azure/role-based-access-control/)

### Setlist Studio Resources
- [Main README](../README.md)
- [Security Documentation](../SECURITY.md)
- [Deployment Guide](../docs/deployment.md)

### Getting Help
If you encounter issues:
1. Check the troubleshooting section above
2. Review Azure Key Vault logs in the Azure Portal
3. Verify permissions and network access
4. Create an issue in the project repository with detailed error information

---

**Security Note**: Never commit OAuth secrets or Key Vault access tokens to version control. Always use secure methods for secret deployment and management.