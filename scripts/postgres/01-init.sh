#!/bin/bash
set -e

# PostgreSQL Primary Database Initialization Script
# Sets up replication user and security configurations

echo "Initializing PostgreSQL primary database for Setlist Studio..."

# Create replication user for streaming replication
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    -- Create replication user
    CREATE USER $POSTGRES_REPLICATION_USER REPLICATION LOGIN CONNECTION LIMIT 5 PASSWORD '$POSTGRES_REPLICATION_PASSWORD';
    
    -- Grant necessary permissions
    GRANT CONNECT ON DATABASE $POSTGRES_DB TO $POSTGRES_REPLICATION_USER;
    
    -- Create readonly user for read replicas
    CREATE USER setliststudio_readonly WITH PASSWORD '${POSTGRES_READONLY_PASSWORD:-readonly_dev}';
    GRANT CONNECT ON DATABASE $POSTGRES_DB TO setliststudio_readonly;
    GRANT USAGE ON SCHEMA public TO setliststudio_readonly;
    GRANT SELECT ON ALL TABLES IN SCHEMA public TO setliststudio_readonly;
    ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO setliststudio_readonly;
    
    -- Create performance monitoring views
    CREATE OR REPLACE VIEW v_connection_stats AS
    SELECT 
        datname,
        usename,
        client_addr,
        state,
        query_start,
        state_change,
        query
    FROM pg_stat_activity 
    WHERE datname = '$POSTGRES_DB';
    
    -- Create replication monitoring view
    CREATE OR REPLACE VIEW v_replication_stats AS
    SELECT 
        client_addr,
        state,
        sent_lsn,
        write_lsn,
        flush_lsn,
        replay_lsn,
        write_lag,
        flush_lag,
        replay_lag
    FROM pg_stat_replication;
EOSQL

# Configure pg_hba.conf for replication
echo "Configuring PostgreSQL authentication for replication..."
cat >> "$PGDATA/pg_hba.conf" <<-EOHBA
# Replication connections
host replication $POSTGRES_REPLICATION_USER 0.0.0.0/0 md5
host replication $POSTGRES_REPLICATION_USER ::0/0 md5

# Application connections
host $POSTGRES_DB $POSTGRES_USER 0.0.0.0/0 md5
host $POSTGRES_DB setliststudio_readonly 0.0.0.0/0 md5
EOHBA

# Performance tuning based on container resources
echo "Applying performance configurations..."
cat >> "$PGDATA/postgresql.conf" <<-EOCONF
# Performance Tuning for Container Environment
# Memory settings (adjust based on container memory limits)
shared_buffers = 256MB
effective_cache_size = 1GB
maintenance_work_mem = 64MB
work_mem = 4MB

# Connection settings
max_connections = 200
superuser_reserved_connections = 3

# WAL settings for replication
wal_level = replica
max_wal_senders = 3
max_replication_slots = 3
wal_keep_size = 1GB
hot_standby = on
hot_standby_feedback = on

# Checkpoint settings
checkpoint_completion_target = 0.9
wal_buffers = 16MB
checkpoint_timeout = 10min
max_wal_size = 2GB
min_wal_size = 1GB

# Query planning
default_statistics_target = 100
random_page_cost = 1.1

# Logging
log_destination = 'stderr'
log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '
log_checkpoints = on
log_connections = on
log_disconnections = on
log_lock_waits = on
log_temp_files = 0
log_autovacuum_min_duration = 0
log_error_verbosity = default

# Autovacuum settings
autovacuum = on
autovacuum_max_workers = 3
autovacuum_naptime = 30s
autovacuum_vacuum_threshold = 50
autovacuum_analyze_threshold = 50
autovacuum_vacuum_scale_factor = 0.1
autovacuum_analyze_scale_factor = 0.05

# SSL settings (can be enabled for production)
ssl = off
#ssl_cert_file = 'server.crt'
#ssl_key_file = 'server.key'
EOCONF

echo "PostgreSQL primary database initialization completed successfully."
echo "Replication user: $POSTGRES_REPLICATION_USER"
echo "Database ready for streaming replication."