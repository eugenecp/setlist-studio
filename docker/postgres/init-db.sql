-- PostgreSQL Initialization Script for Setlist Studio
-- Creates optimized database schema and indexes for load balancing

-- Create extensions for performance monitoring and optimization
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "btree_gin";

-- Create database with optimal settings
ALTER DATABASE setliststudio SET timezone = 'UTC';
ALTER DATABASE setliststudio SET log_statement_stats = off;
ALTER DATABASE setliststudio SET log_min_duration_statement = 1000;

-- Grant privileges to application user
GRANT ALL PRIVILEGES ON DATABASE setliststudio TO setlist_user;
GRANT ALL PRIVILEGES ON SCHEMA public TO setlist_user;

-- Create performance monitoring view
CREATE OR REPLACE VIEW performance_stats AS
SELECT 
    schemaname,
    tablename,
    attname,
    n_distinct,
    correlation
FROM pg_stats 
WHERE schemaname = 'public'
ORDER BY tablename, attname;

-- Grant access to performance monitoring
GRANT SELECT ON performance_stats TO setlist_user;

-- Configure connection limits per user
ALTER USER setlist_user CONNECTION LIMIT 50;

-- Set up connection pooling hints
COMMENT ON DATABASE setliststudio IS 'Setlist Studio Database - Optimized for Connection Pooling';

-- Log successful initialization
\echo 'PostgreSQL initialization completed successfully for Setlist Studio'