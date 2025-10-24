#!/bin/bash

# Database Security Hardening Script for Setlist Studio
# This script implements enhanced database security measures

set -e

echo "🔒 Implementing enhanced database security for Setlist Studio"

# Configuration
DATA_DIR="/app/data"
DB_FILE="$DATA_DIR/setliststudio.db"
LOG_FILE="/app/logs/database-security.log"

# Function to log security actions
log_security_action() {
    local message="$1"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo "[$timestamp] DATABASE_SECURITY: $message" | tee -a "$LOG_FILE"
}

# Function to check if running in privileged mode
is_privileged() {
    # Check if we can write to /proc/sys (indicator of privileged mode)
    if [ -w /proc/sys ]; then
        return 0
    else
        return 1
    fi
}

# Function to apply advanced file system security
apply_advanced_security() {
    log_security_action "Applying advanced database security measures"
    
    # Set strict permissions on data directory (owner-only access)
    chmod 700 "$DATA_DIR" || log_security_action "WARNING: Failed to set directory permissions"
    
    # Set strict permissions on database file (owner read/write only)
    if [ -f "$DB_FILE" ]; then
        chmod 600 "$DB_FILE" || log_security_action "WARNING: Failed to set database file permissions"
        log_security_action "Database file permissions set to 600 (owner read/write only)"
    fi
    
    # Try to apply immutable attributes if supported and privileged
    if command -v chattr >/dev/null 2>&1; then
        if is_privileged; then
            # Make data directory immutable (prevents directory structure changes)
            if chattr +i "$DATA_DIR" 2>/dev/null; then
                log_security_action "Applied immutable attribute to data directory"
            else
                log_security_action "WARNING: Could not apply immutable attribute to data directory (may require privileged container)"
            fi
        else
            log_security_action "INFO: Skipping immutable attributes (requires privileged container)"
        fi
    else
        log_security_action "INFO: chattr not available, skipping immutable attributes"
    fi
    
    # Set extended attributes for additional security if supported
    if command -v setfattr >/dev/null 2>&1 && [ -f "$DB_FILE" ]; then
        # Set security label to indicate sensitive data
        if setfattr -n user.security.level -v "confidential" "$DB_FILE" 2>/dev/null; then
            log_security_action "Applied security label to database file"
        else
            log_security_action "INFO: Could not set extended attributes (filesystem may not support)"
        fi
    fi
}

# Function to implement database file monitoring
setup_database_monitoring() {
    log_security_action "Setting up database file monitoring"
    
    # Create a simple monitoring script that can detect unauthorized changes
    cat > /app/monitor-database.sh << 'EOF'
#!/bin/bash
# Simple database file integrity monitor

DB_FILE="/app/data/setliststudio.db"
CHECKSUM_FILE="/app/data/.db-checksum"

if [ -f "$DB_FILE" ]; then
    CURRENT_CHECKSUM=$(sha256sum "$DB_FILE" | cut -d' ' -f1)
    
    if [ -f "$CHECKSUM_FILE" ]; then
        STORED_CHECKSUM=$(cat "$CHECKSUM_FILE")
        if [ "$CURRENT_CHECKSUM" != "$STORED_CHECKSUM" ]; then
            echo "$(date '+%Y-%m-%d %H:%M:%S') DATABASE_SECURITY: Database file checksum changed" >> /app/logs/database-security.log
        fi
    fi
    
    echo "$CURRENT_CHECKSUM" > "$CHECKSUM_FILE"
    chmod 600 "$CHECKSUM_FILE"
fi
EOF

    chmod 750 /app/monitor-database.sh
    log_security_action "Database monitoring script created"
}

# Function to implement secure database backup permissions
setup_backup_security() {
    log_security_action "Configuring secure backup permissions"
    
    # Create backup directory with restricted permissions
    BACKUP_DIR="/app/data/backups"
    mkdir -p "$BACKUP_DIR"
    chmod 700 "$BACKUP_DIR"
    
    log_security_action "Backup directory created with secure permissions"
}

# Function to set up database access logging
setup_database_logging() {
    log_security_action "Configuring database access logging"
    
    # Ensure logs directory has proper permissions
    chmod 750 /app/logs
    
    # Create database security log with restricted permissions
    touch "$LOG_FILE"
    chmod 600 "$LOG_FILE"
    
    log_security_action "Database security logging configured"
}

# Function to implement defense in depth
implement_defense_in_depth() {
    log_security_action "Implementing defense-in-depth security measures"
    
    # Create security policy file
    cat > /app/data/.security-policy << 'EOF'
# Database Security Policy for Setlist Studio
# This file contains security settings for the database

# File permissions
DATABASE_FILE_MODE=600
DATA_DIRECTORY_MODE=700

# Security features
CHECKSUM_VALIDATION=enabled
ACCESS_LOGGING=enabled
BACKUP_ENCRYPTION=recommended

# Compliance
SECURITY_LEVEL=confidential
DATA_CLASSIFICATION=user-personal-data
EOF

    chmod 600 /app/data/.security-policy
    log_security_action "Security policy file created"
}

# Main execution
main() {
    log_security_action "Starting database security hardening"
    
    # Ensure directories exist
    mkdir -p "$DATA_DIR" /app/logs
    
    # Apply security measures
    setup_database_logging
    apply_advanced_security
    setup_database_monitoring
    setup_backup_security
    implement_defense_in_depth
    
    log_security_action "Database security hardening completed successfully"
    
    # Display security status
    echo ""
    echo "🔒 Database Security Status:"
    echo "   Data Directory: $(ls -ld "$DATA_DIR" | cut -d' ' -f1,3,4)"
    if [ -f "$DB_FILE" ]; then
        echo "   Database File:  $(ls -l "$DB_FILE" | cut -d' ' -f1,3,4)"
    fi
    echo "   Security Log:   $LOG_FILE"
    echo ""
    
    # Check for potential security improvements
    echo "🛡️  Security Recommendations:"
    if ! is_privileged; then
        echo "   • Run container with --privileged for advanced security features"
    fi
    if ! command -v chattr >/dev/null 2>&1; then
        echo "   • Use base image with extended filesystem tools for enhanced security"
    fi
    echo "   • Regular security audits and backup validation recommended"
    echo "   • Monitor /app/logs/database-security.log for security events"
}

# Run main function
main "$@"