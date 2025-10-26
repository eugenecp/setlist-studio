#!/bin/bash
set -e

# PostgreSQL Read Replica Initialization Script
# Configures streaming replication from primary database

echo "Initializing PostgreSQL read replica..."

# Wait for primary database to be fully ready
until pg_isready -h "$POSTGRES_MASTER_HOST" -p "$POSTGRES_MASTER_PORT" -U "$POSTGRES_MASTER_USER"; do
    echo "Waiting for primary database at $POSTGRES_MASTER_HOST:$POSTGRES_MASTER_PORT..."
    sleep 2
done

echo "Primary database is ready. Setting up streaming replication..."

# Configure recovery settings for streaming replication
cat > "$PGDATA/postgresql.conf" <<-EOCONF
# Read replica configuration
hot_standby = on
max_connections = 100
shared_buffers = 128MB
effective_cache_size = 512MB
maintenance_work_mem = 32MB
work_mem = 2MB

# Replication settings
hot_standby_feedback = on
max_standby_archive_delay = 30s
max_standby_streaming_delay = 30s
wal_receiver_status_interval = 10s
hot_standby_feedback = on

# Logging
log_destination = 'stderr'
log_line_prefix = '[REPLICA] %t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '
log_connections = on
log_disconnections = on
log_min_duration_statement = 1000

# Performance settings for read replica
checkpoint_completion_target = 0.9
wal_buffers = 8MB
default_statistics_target = 100
random_page_cost = 1.1

# Autovacuum (less aggressive on replica)
autovacuum = on
autovacuum_max_workers = 2
autovacuum_naptime = 60s
EOCONF

echo "Read replica configuration completed."
echo "Replica will connect to primary at $POSTGRES_MASTER_HOST:$POSTGRES_MASTER_PORT"