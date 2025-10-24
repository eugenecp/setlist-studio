#!/bin/bash

# Docker Secrets Setup Script for Setlist Studio
# This script creates Docker secrets for OAuth credentials

set -e

echo "🔐 Setting up Docker secrets for Setlist Studio OAuth credentials"
echo "This script will create Docker secrets from environment variables or prompt for input"

# Function to create a Docker secret
create_secret() {
    local secret_name=$1
    local env_var=$2
    local prompt_message=$3
    
    # Remove existing secret if it exists
    if docker secret ls --format "{{.Name}}" | grep -q "^${secret_name}$"; then
        echo "📝 Removing existing secret: ${secret_name}"
        docker secret rm ${secret_name} || true
    fi
    
    # Get value from environment variable or prompt
    local secret_value=""
    if [ -n "${!env_var}" ]; then
        secret_value="${!env_var}"
        echo "✅ Using ${env_var} environment variable for ${secret_name}"
    else
        echo -n "${prompt_message}: "
        read -s secret_value
        echo ""
        
        if [ -z "$secret_value" ]; then
            echo "⚠️  Warning: Empty value provided for ${secret_name}"
            secret_value="PLACEHOLDER_${secret_name^^}"
        fi
    fi
    
    # Create the secret
    echo -n "$secret_value" | docker secret create ${secret_name} -
    echo "🔒 Created secret: ${secret_name}"
}

# Check if Docker Swarm is initialized
if ! docker info --format '{{.Swarm.LocalNodeState}}' | grep -q active; then
    echo "🚀 Initializing Docker Swarm (required for secrets)..."
    docker swarm init --advertise-addr 127.0.0.1
fi

echo ""
echo "📱 Creating OAuth secrets..."
echo "Leave empty to use placeholder values (for testing only)"
echo ""

# Create OAuth secrets
create_secret "setliststudio_google_client_id" "GOOGLE_CLIENT_ID" "Enter Google OAuth Client ID"
create_secret "setliststudio_google_client_secret" "GOOGLE_CLIENT_SECRET" "Enter Google OAuth Client Secret"
create_secret "setliststudio_microsoft_client_id" "MICROSOFT_CLIENT_ID" "Enter Microsoft OAuth Client ID" 
create_secret "setliststudio_microsoft_client_secret" "MICROSOFT_CLIENT_SECRET" "Enter Microsoft OAuth Client Secret"
create_secret "setliststudio_facebook_app_id" "FACEBOOK_APP_ID" "Enter Facebook App ID"
create_secret "setliststudio_facebook_app_secret" "FACEBOOK_APP_SECRET" "Enter Facebook App Secret"

echo ""
echo "✅ Docker secrets created successfully!"
echo ""
echo "🚀 To deploy with secrets, use:"
echo "   docker-compose -f docker-compose.yml -f docker-compose.secrets.yml up -d"
echo ""
echo "🔍 To view secrets (names only):"
echo "   docker secret ls"
echo ""
echo "🗑️  To remove all secrets:"
echo "   docker secret rm setliststudio_google_client_id setliststudio_google_client_secret setliststudio_microsoft_client_id setliststudio_microsoft_client_secret setliststudio_facebook_app_id setliststudio_facebook_app_secret"