# PostgreSQL Migration Guide for Load Balancing for Setlist Studio

This guide explains how to migrate from SQLite to PostgreSQL for better multi-user support, connection pooling, and read replica capabilities.

## Overview

The PostgreSQL migration provides:

- **Connection Pooling**: Support for 100+ concurrent connections with configurable pool sizes
- **Read Replicas**: Automatic read/write splitting for better performance
- **Scalability**: Better support for concurrent users and large datasets
- **Production Ready**: Full Docker setup with monitoring and security
- **Load Balancing**: Optimized for multiple application instances with shared database backend
- **High Availability**: Enterprise-grade database with automatic failover capabilities

## Quick Start

### 1. Development Setup (Local PostgreSQL)

```bash
# Start PostgreSQL with Docker Compose
docker-compose -f docker-compose.postgresql.yml up -d

# Run with PostgreSQL configuration
ASPNETCORE_ENVIRONMENT=PostgreSQL dotnet run --project src/SetlistStudio.Web
```

### 2. Environment Configuration

Create `.env` file from the example:

```bash
cp .env.postgresql.example .env
# Edit .env with your database passwords
```

Key environment variables:
```env
POSTGRES_PASSWORD=your_secure_password
POSTGRES_REPLICATION_PASSWORD=your_replication_password
Database__Provider=PostgreSQL
```

### 3. Configuration Options

Update `appsettings.json` or use environment variables:

```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "Pool": {
      "MaxSize": 100,
      "MinSize": 5,
      "ConnectionTimeout": 30,
      "CommandTimeout": 120,
      "Enabled": true
    },
    "ReadReplicas": [
      "Host=replica1;Port=5432;Database=setliststudio;Username=readonly;Password=password;"
    ]
  }
}
```

## Architecture

### Database Configuration System

The new configuration system supports multiple providers:

```csharp
// Automatic provider selection
services.AddSingleton<IDatabaseConfiguration>(provider => 
    new DatabaseConfiguration(configuration, logger));

// Separate contexts for read/write operations
services.AddDbContext<SetlistStudioDbContext>(); // Write operations
services.AddDbContext<ReadOnlySetlistStudioDbContext>(); // Read operations
```

### Connection Pooling

PostgreSQL connections are pooled using Npgsql's built-in pooling:

- **Write Pool**: Optimized for transactional operations
- **Read Pool**: Optimized for query operations
- **Automatic Scaling**: Pool size adjusts based on load
- **Health Monitoring**: Connection health checks and retry logic

### Read Replica Support

Read operations are automatically distributed across replicas:

```csharp
// Write operations use primary database
await context.Songs.AddAsync(song);
await context.SaveChangesAsync();

// Read operations use replicas (round-robin)
var songs = await readOnlyContext.Songs
    .Where(s => s.UserId == userId)
    .ToListAsync();
```

## Load Balancing with PostgreSQL

PostgreSQL is essential for load balancing scenarios as it supports multiple concurrent application instances sharing a single database backend. SQLite limitations (single writer, file-based) make it unsuitable for production load balancing.

### Multi-Instance Configuration

Configure connection pooling for load-balanced applications:

```csharp
services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    });
}, ServiceLifetime.Scoped);

// Connection pooling settings for multiple instances
services.Configure<NpgsqlConnectionStringBuilder>(builder =>
{
    builder.Pooling = true;
    builder.MinPoolSize = 5;    // Per instance
    builder.MaxPoolSize = 50;   // Per instance
    builder.ConnectionIdleLifetime = 300;
    builder.ConnectionPruningInterval = 10;
});
```

### Load Balanced Docker Compose

Example configuration for 3 application instances:

```yaml
version: '3.8'
services:
  app1:
    build: .
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=SetlistStudio;Username=setlist_app;Password=${POSTGRES_PASSWORD};Pooling=true;Minimum Pool Size=5;Maximum Pool Size=50;"
      ASPNETCORE_ENVIRONMENT: Production
    depends_on:
      postgres:
        condition: service_healthy

  app2:
    build: .
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=SetlistStudio;Username=setlist_app;Password=${POSTGRES_PASSWORD};Pooling=true;Minimum Pool Size=5;Maximum Pool Size=50;"
      ASPNETCORE_ENVIRONMENT: Production
    depends_on:
      postgres:
        condition: service_healthy

  app3:
    build: .
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=SetlistStudio;Username=setlist_app;Password=${POSTGRES_PASSWORD};Pooling=true;Minimum Pool Size=5;Maximum Pool Size=50;"
      ASPNETCORE_ENVIRONMENT: Production
    depends_on:
      postgres:
        condition: service_healthy

  nginx:
    image: nginx:1.25-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - app1
      - app2
      - app3

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: SetlistStudio
      POSTGRES_USER: setlist_app
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    command: >
      postgres
      -c max_connections=200
      -c shared_buffers=256MB
      -c effective_cache_size=1GB
      -c maintenance_work_mem=64MB
      -c checkpoint_completion_target=0.9
      -c wal_buffers=16MB
      -c default_statistics_target=100
      -c random_page_cost=1.1
      -c effective_io_concurrency=200
      -c work_mem=4MB
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U setlist_app -d SetlistStudio"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  postgres_data:
```

### Connection Pool Sizing Guidelines

For load balanced applications:

- **Small Load (1-3 instances)**: 5-20 connections per instance
- **Medium Load (3-5 instances)**: 10-30 connections per instance  
- **Large Load (5+ instances)**: 20-50 connections per instance

Total PostgreSQL `max_connections` should be:
```
max_connections = (instances × max_pool_size) + 20 (for maintenance)
```

### Performance Tuning for Load Balancing

Optimize PostgreSQL for concurrent access:

```sql
-- Connection and memory settings
ALTER SYSTEM SET max_connections = 200;
ALTER SYSTEM SET shared_buffers = '256MB';
ALTER SYSTEM SET effective_cache_size = '1GB';

-- Concurrent access optimization
ALTER SYSTEM SET checkpoint_completion_target = 0.9;
ALTER SYSTEM SET wal_buffers = '16MB';
ALTER SYSTEM SET default_statistics_target = 100;

-- Performance settings
ALTER SYSTEM SET random_page_cost = 1.1;
ALTER SYSTEM SET effective_io_concurrency = 200;
ALTER SYSTEM SET work_mem = '4MB';

-- Apply changes
SELECT pg_reload_conf();
```

## Docker Deployment

### Development Environment

```bash
# Start all services (primary + 2 replicas)
docker-compose -f docker-compose.postgresql.yml up -d

# View logs
docker-compose -f docker-compose.postgresql.yml logs -f

# Access PgAdmin (development only)
# http://localhost:5050 (admin@setliststudio.local / admin_dev)
```

### Production Environment

```bash
# Production deployment with security hardening
docker-compose -f docker-compose.postgresql.yml --profile production up -d

# Environment variables required:
# POSTGRES_PASSWORD=secure_production_password
# POSTGRES_REPLICATION_PASSWORD=secure_replication_password
```

## Database Migration

### From SQLite to PostgreSQL

1. **Backup SQLite data**:
```bash
sqlite3 setliststudio.db .dump > backup.sql
```

2. **Convert to PostgreSQL**:
```bash
pgloader sqlite://setliststudio.db postgresql://user:pass@localhost/setliststudio
```

3. **Run migrations**:
```bash
$env:Database__Provider="PostgreSQL"
dotnet ef database update --project src/SetlistStudio.Web
```

### Schema Compatibility

The migration includes:
- **Identity Tables**: ASP.NET Core Identity schema
- **Application Tables**: Songs, Setlists, SetlistSongs, AuditLogs
- **Indexes**: Optimized for user-specific queries
- **Constraints**: Foreign keys and data validation

## Performance Optimization

### Connection Pool Tuning

Adjust pool sizes based on your load:

```json
{
  "Database": {
    "Pool": {
      "MaxSize": 200,      // Max concurrent connections
      "MinSize": 10,       // Always-open connections
      "ConnectionTimeout": 30,    // Connection timeout (seconds)
      "CommandTimeout": 300       // Query timeout (seconds)
    }
  }
}
```

### Query Performance

Indexes are automatically created for:
- User-specific queries (`UserId`)
- Artist and genre filtering
- Setlist ordering and positioning
- Audit log tracking

### Monitoring

Built-in monitoring views:
```sql
-- Connection statistics
SELECT * FROM v_connection_stats;

-- Replication status
SELECT * FROM v_replication_stats;
```

### Load Balancing Monitoring

Monitor PostgreSQL performance with multiple application instances:

```sql
-- Active connections by application
SELECT 
    application_name,
    client_addr,
    COUNT(*) as connection_count,
    MAX(backend_start) as latest_connection,
    string_agg(DISTINCT state, ', ') as states
FROM pg_stat_activity 
WHERE application_name LIKE 'SetlistStudio%'
GROUP BY application_name, client_addr
ORDER BY connection_count DESC;

-- Connection pool utilization
SELECT 
    datname,
    numbackends as active_connections,
    (SELECT setting::int FROM pg_settings WHERE name = 'max_connections') as max_connections,
    ROUND(
        (numbackends::float / (SELECT setting::int FROM pg_settings WHERE name = 'max_connections')::float) * 100, 
        2
    ) as utilization_percent
FROM pg_stat_database 
WHERE datname = 'SetlistStudio';

-- Slow queries impacting load balancing
SELECT 
    query,
    calls,
    total_time,
    mean_time,
    rows,
    100.0 * shared_blks_hit / nullif(shared_blks_hit + shared_blks_read, 0) AS hit_percent
FROM pg_stat_statements 
WHERE total_time > 1000  -- Queries taking >1 second
ORDER BY total_time DESC 
LIMIT 10;

-- Lock conflicts between instances
SELECT 
    blocked_locks.pid AS blocked_pid,
    blocked_activity.usename AS blocked_user,
    blocking_locks.pid AS blocking_pid,
    blocking_activity.usename AS blocking_user,
    blocked_activity.query AS blocked_statement,
    blocking_activity.query AS current_statement_in_blocking_process
FROM pg_catalog.pg_locks blocked_locks
JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
JOIN pg_catalog.pg_locks blocking_locks 
    ON blocking_locks.locktype = blocked_locks.locktype
    AND blocking_locks.DATABASE IS NOT DISTINCT FROM blocked_locks.DATABASE
    AND blocking_locks.relation IS NOT DISTINCT FROM blocked_locks.relation
    AND blocking_locks.page IS NOT DISTINCT FROM blocked_locks.page
    AND blocking_locks.tuple IS NOT DISTINCT FROM blocked_locks.tuple
    AND blocking_locks.virtualxid IS NOT DISTINCT FROM blocked_locks.virtualxid
    AND blocking_locks.transactionid IS NOT DISTINCT FROM blocked_locks.transactionid
    AND blocking_locks.classid IS NOT DISTINCT FROM blocked_locks.classid
    AND blocking_locks.objid IS NOT DISTINCT FROM blocked_locks.objid
    AND blocking_locks.objsubid IS NOT DISTINCT FROM blocked_locks.objsubid
    AND blocking_locks.pid != blocked_locks.pid
JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
WHERE NOT blocked_locks.GRANTED;
```

### Performance Alerts

Set up alerts for load balancing issues:

```sql
-- High connection utilization (>80%)
SELECT 'CONNECTION_WARNING' as alert_type,
    CASE 
        WHEN utilization > 90 THEN 'CRITICAL'
        WHEN utilization > 80 THEN 'WARNING'
        ELSE 'OK'
    END as severity,
    utilization as current_value
FROM (
    SELECT ROUND(
        (COUNT(*)::float / (SELECT setting::int FROM pg_settings WHERE name = 'max_connections')::float) * 100, 
        2
    ) as utilization
    FROM pg_stat_activity 
    WHERE state = 'active'
) t
WHERE utilization > 80;

-- Long running queries (>30 seconds)
SELECT 'SLOW_QUERY' as alert_type,
    'WARNING' as severity,
    pid,
    now() - pg_stat_activity.query_start AS duration,
    query
FROM pg_stat_activity 
WHERE (now() - pg_stat_activity.query_start) > interval '30 seconds'
  AND state = 'active'
  AND query NOT LIKE '%pg_stat_activity%';
```

## Testing

### Unit Tests (SQLite/In-Memory)
```bash
# Fast unit tests with in-memory database
dotnet test --filter "Category=Unit"
```

### Integration Tests (PostgreSQL)
```bash
# Full integration tests with PostgreSQL containers
dotnet test --filter "Category=Integration"
```

### Test Configuration

Tests automatically use appropriate providers:
- **Unit Tests**: In-memory or SQLite for speed
- **Integration Tests**: PostgreSQL with Testcontainers
- **Load Tests**: Full PostgreSQL with read replicas

## Security Considerations

### Connection Security
- SSL/TLS encryption in production
- Separate read-only users for replicas
- Connection string validation
- IP allowlisting support

### Database Security
- Strong password requirements
- Replication user with minimal permissions
- Audit logging for all operations
- SQL injection protection

## Troubleshooting

### Common Issues

1. **Connection Pool Exhausted**:
```
Solution: Increase MaxPoolSize or check for connection leaks
```

2. **Replica Lag**:
```sql
-- Check replication lag
SELECT * FROM v_replication_stats;
```

3. **Migration Failures**:
```bash
# Reset and re-run migrations
dotnet ef database drop --project src/SetlistStudio.Web
dotnet ef database update --project src/SetlistStudio.Web
```

### Performance Issues

1. **Slow Queries**:
```sql
-- Enable query logging
SET log_statement = 'all';
SET log_min_duration_statement = 1000;
```

2. **Connection Timeouts**:
```json
{
  "Database": {
    "Pool": {
      "ConnectionTimeout": 60,
      "CommandTimeout": 600
    }
  }
}
```

## Configuration Reference

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `Database__Provider` | Database provider (PostgreSQL/SQLite) | SQLite |
| `Database__Pool__MaxSize` | Maximum pool size | 100 |
| `Database__Pool__MinSize` | Minimum pool size | 5 |
| `POSTGRES_PASSWORD` | Database password | Required |
| `POSTGRES_REPLICATION_PASSWORD` | Replication password | Required |

### Connection Strings

**Write Connection (Primary)**:
```
Host=localhost;Port=5432;Database=setliststudio;Username=setliststudio;Password=xxx;SSL Mode=Require;
```

**Read Connections (Replicas)**:
```
Host=replica1;Port=5432;Database=setliststudio;Username=readonly;Password=xxx;SSL Mode=Require;
```

## Monitoring and Maintenance

### Health Checks
Built-in health checks for:
- Database connectivity
- Replication status
- Connection pool health
- Query performance

### Backup Strategy
1. **Continuous Archiving**: WAL-based point-in-time recovery
2. **Regular Snapshots**: Daily pg_dump backups
3. **Replica Verification**: Ensure replica consistency

### Scaling Guidelines

| Users | Configuration | Resources |
|-------|---------------|-----------|
| 1-100 | Single instance | 2GB RAM, 2 CPU |
| 100-500 | Primary + 1 replica | 4GB RAM, 4 CPU |
| 500-2000 | Primary + 2 replicas | 8GB RAM, 8 CPU |
| 2000+ | Horizontal partitioning | 16GB+ RAM, 16+ CPU |

## Load Balancing Deployment Verification

### Test Multiple Instances

Verify that multiple application instances can connect concurrently:

```bash
# Start load balanced deployment
docker-compose -f docker-compose.loadbalanced.yml up -d

# Check all instances are healthy
curl http://localhost/health
curl http://localhost:8081/health
curl http://localhost:8082/health
curl http://localhost:8083/health

# Monitor connection distribution
docker exec postgres psql -U setlist_app -d SetlistStudio -c "
SELECT 
    application_name,
    client_addr,
    COUNT(*) as connections
FROM pg_stat_activity 
WHERE application_name LIKE 'SetlistStudio%'
GROUP BY application_name, client_addr;"
```

### Load Testing

Validate database performance under load:

```bash
# Install Apache Bench (if not installed)
# Test concurrent user simulation
ab -n 1000 -c 10 http://localhost/api/songs

# Monitor PostgreSQL during test
docker exec postgres psql -U setlist_app -d SetlistStudio -c "
SELECT 
    COUNT(*) as active_connections,
    MAX(now() - query_start) as longest_query,
    COUNT(CASE WHEN state = 'active' THEN 1 END) as active_queries
FROM pg_stat_activity 
WHERE application_name LIKE 'SetlistStudio%';"
```

### Sticky Session Verification

Test that Blazor Server sessions stay connected:

```javascript
// Browser console test
// Open multiple tabs to same application
// Verify SignalR connections maintain session state
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/blazorhub")
    .build();

connection.start().then(function () {
    console.log("Connected to instance via NGINX load balancer");
}).catch(function (err) {
    console.error(err.toString());
});
```

### Database Consistency Verification

Ensure data consistency across all instances:

```sql
-- Run from any instance - should show same data
SELECT COUNT(*) as song_count FROM Songs;
SELECT COUNT(*) as setlist_count FROM Setlists;
SELECT COUNT(*) as user_count FROM AspNetUsers;

-- Verify user isolation works correctly
SELECT 
    u.Email,
    COUNT(s.Id) as song_count,
    COUNT(sl.Id) as setlist_count
FROM AspNetUsers u
LEFT JOIN Songs s ON u.Id = s.UserId
LEFT JOIN Setlists sl ON u.Id = sl.UserId
GROUP BY u.Id, u.Email
ORDER BY u.Email;
```

## Migration Checklist

- [ ] Backup existing SQLite database
- [ ] Configure PostgreSQL connection strings
- [ ] Set environment variables for database provider
- [ ] Run database migrations
- [ ] Test read/write operations
- [ ] Verify connection pooling
- [ ] Test read replica functionality
- [ ] **Verify load balancer configuration**
- [ ] **Test multiple instance deployment**
- [ ] **Validate sticky sessions for Blazor Server**
- [ ] **Monitor connection distribution**
- [ ] **Test database consistency across instances**
- [ ] Run full test suite
- [ ] Monitor performance metrics
- [ ] Document custom configurations

## Support and Maintenance

For production deployments:
1. Monitor connection pool usage
2. Track replication lag
3. Review slow query logs
4. Update security patches
5. Scale replicas based on read load

The PostgreSQL migration provides a solid foundation for scaling Setlist Studio to support thousands of concurrent users while maintaining high performance and reliability.