# PostgreSQL Migration Guide for Setlist Studio

This guide explains how to migrate from SQLite to PostgreSQL for better multi-user support, connection pooling, and read replica capabilities.

## Overview

The PostgreSQL migration provides:

- **Connection Pooling**: Support for 100+ concurrent connections with configurable pool sizes
- **Read Replicas**: Automatic read/write splitting for better performance
- **Scalability**: Better support for concurrent users and large datasets
- **Production Ready**: Full Docker setup with monitoring and security

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

## Migration Checklist

- [ ] Backup existing SQLite database
- [ ] Configure PostgreSQL connection strings
- [ ] Set environment variables for database provider
- [ ] Run database migrations
- [ ] Test read/write operations
- [ ] Verify connection pooling
- [ ] Test read replica functionality
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