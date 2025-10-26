# Load Balancing Guide for Setlist Studio

This guide covers implementing load balancing, sticky sessions, health checks, and auto-scaling for Setlist Studio using NGINX, Docker Compose, Docker Swarm, and Kubernetes.

## Overview

Setlist Studio's load balancing implementation provides:

- **NGINX Load Balancer** with sticky sessions for Blazor Server SignalR connections
- **Redis-based session storage** for distributed state management  
- **Health checks** for intelligent traffic routing and auto-scaling decisions
- **Auto-scaling** capabilities using Docker Swarm and Kubernetes HPA
- **Monitoring** with Prometheus and Grafana for metrics-driven scaling

## Architecture

### Load Balancing Components

```
Internet → NGINX Load Balancer → Multiple App Instances
                ↓                        ↓
            Redis Session Store ← → Database (Shared)
                ↓
          Monitoring Stack
```

### Key Features

- **Sticky Sessions**: IP-based session affinity for Blazor Server SignalR connections
- **Health Checks**: Liveness, readiness, and detailed health endpoints  
- **Auto-scaling**: CPU/Memory-based scaling with configurable thresholds
- **SSL Termination**: HTTPS support with automatic certificate management
- **Security Headers**: Comprehensive security headers for production deployment
- **Rate Limiting**: Protection against DoS attacks and abuse

## Deployment Options

### 1. Docker Compose Load Balanced Setup

**Use Case**: Development, small to medium production deployments

```powershell
# Deploy with load balancing
.\scripts\loadbalancing\deploy-loadbalanced.ps1 -Environment Production -Instances 3 -EnableMonitoring

# Scale instances
docker-compose -f docker-compose.yml -f docker-compose.loadbalanced.yml up -d --scale setliststudio-web-3=5
```

**Features**:
- 3 web instances by default (configurable)
- NGINX load balancer with sticky sessions
- Redis for distributed sessions and SignalR backplane
- Optional Prometheus/Grafana monitoring
- Health checks for all services

**Configuration**:
- **NGINX**: `nginx/nginx.conf` - Load balancer configuration
- **Redis**: `docker/redis/redis.conf` - Session storage settings
- **Compose**: `docker-compose.loadbalanced.yml` - Service orchestration

### 2. Docker Swarm Auto-scaling

**Use Case**: Production deployments requiring auto-scaling

```powershell
# Initialize swarm
docker swarm init

# Deploy stack with auto-scaling
docker stack deploy -c docker-swarm.yml setlist-studio

# Monitor auto-scaling
docker service logs setlist-studio_autoscaler -f
```

**Features**:
- Automatic scaling based on CPU/Memory metrics
- Service discovery and load balancing
- Rolling updates with zero downtime
- Traefik reverse proxy with SSL
- Built-in health checks and service recovery

**Auto-scaling Logic**:
- **Scale Up**: CPU > 70% OR Memory > 80% (max 10 instances)
- **Scale Down**: CPU < 30% AND Memory < 40% (min 2 instances)  
- **Evaluation**: Every 60 seconds with stabilization windows

### 3. Kubernetes with HPA

**Use Case**: Large-scale production, cloud-native deployments

```powershell
# Deploy to Kubernetes
.\scripts\loadbalancing\deploy-kubernetes.ps1 -Domain "setliststudio.com" -EnableSSL

# Monitor auto-scaling
kubectl get hpa -w -n setlist-studio
```

**Features**:
- Horizontal Pod Autoscaler (HPA) with CPU/Memory metrics
- NGINX Ingress Controller with sticky sessions
- Persistent storage for database and Redis
- Network policies for security
- Pod disruption budgets for high availability

**Auto-scaling Configuration**:
- **Min Replicas**: 2
- **Max Replicas**: 10  
- **CPU Target**: 70%
- **Memory Target**: 80%
- **Scale Down Stabilization**: 5 minutes
- **Scale Up Stabilization**: 1 minute

## Configuration Details

### NGINX Load Balancer

**Upstream Configuration**:
```nginx
upstream setlist_studio_backend {
    ip_hash;  # Sticky sessions for SignalR
    server setliststudio-web-1:8080 weight=1 max_fails=3 fail_timeout=30s;
    server setliststudio-web-2:8080 weight=1 max_fails=3 fail_timeout=30s;
    server setliststudio-web-3:8080 weight=1 max_fails=3 fail_timeout=30s backup;
    keepalive 32;
}
```

**Key Features**:
- **IP Hash**: Ensures SignalR connections stick to same backend
- **Health Checks**: Automatic backend failure detection
- **Connection Pooling**: Persistent connections for better performance
- **Rate Limiting**: Per-IP and global rate limits
- **SSL/TLS**: Modern cipher suites and HSTS headers

### Redis Session Storage

**Configuration**: `docker/redis/redis.conf`
```redis
maxmemory 256mb
maxmemory-policy allkeys-lru
save 900 1
appendonly yes
appendfsync everysec
```

**Security**:
- Password authentication required
- Disabled dangerous commands (FLUSHALL, CONFIG, etc.)
- Memory limits and eviction policies
- Persistence for session durability

### Blazor Server Load Balancing

**Program.cs Configuration**:
```csharp
services.AddSignalR()
    .AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = "SetlistStudio";
    });

services.AddSession(options =>
{
    options.Cookie.Name = ".SetlistStudio.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
```

**Features**:
- Redis backplane for SignalR scaling
- Distributed session storage  
- Sticky session cookies
- Circuit state management

### Health Check Endpoints

| Endpoint | Purpose | Load Balancer Use |
|----------|---------|------------------|
| `/api/health/simple` | Basic health check | Upstream health |
| `/api/health/detailed` | Resource metrics | Scaling decisions |
| `/api/health/ready` | Readiness probe | Traffic routing |
| `/api/health/live` | Liveness probe | Container restart |

**Health Check Response Example**:
```json
{
  "status": "Healthy",
  "instance": {
    "id": "web-1",
    "name": "Setlist Studio Web 1",
    "isLoadBalanced": true
  },
  "performance": {
    "cpuUsage": 45.2,
    "memoryUsage": {
      "workingSetMB": 234.5
    },
    "threadCount": 45
  },
  "loadBalancer": {
    "canAcceptTraffic": true,
    "responseTime": 12.3
  }
}
```

## Monitoring and Metrics

### Prometheus Configuration

**Scrape Targets**:
- Application instances (metrics endpoint)
- Redis (connection, memory, performance)  
- NGINX (request rates, response times)
- System metrics (CPU, memory, disk)

**Key Metrics for Auto-scaling**:
- `process_cpu_seconds_total` - CPU usage rate
- `dotnet_gc_memory_total_available_bytes` - Memory usage
- `http_request_duration_seconds` - Response times
- `blazor_server_circuit_disconnect_total` - SignalR health

### Alert Rules

**Auto-scaling Triggers**:
- **Scale Up**: CPU > 80% for 2min OR Memory > 90% for 1min
- **Scale Down**: CPU < 20% AND Memory < 30% for 10min  
- **Critical**: Instance down, Redis unavailable, high error rates

### Grafana Dashboards

**Included Dashboards**:
- **Load Balancer Overview**: Request rates, response times, backend health
- **Application Performance**: CPU, memory, GC metrics, SignalR connections
- **Auto-scaling Dashboard**: Scaling events, thresholds, predictions
- **Redis Monitoring**: Memory usage, connections, command rates

## Security Considerations

### Load Balancer Security

- **Rate Limiting**: 100 req/min per IP for APIs, 5 req/min for auth
- **Security Headers**: CSP, HSTS, X-Frame-Options, etc.
- **SSL/TLS**: Modern cipher suites, perfect forward secrecy
- **DDoS Protection**: Connection limits, request size limits

### Network Security

- **Internal Networks**: Backend services on private networks
- **Network Policies**: Kubernetes network segmentation  
- **Firewall Rules**: Minimal port exposure
- **Service Mesh**: Consider Istio for advanced security (future)

### Session Security

- **Secure Cookies**: HttpOnly, Secure, SameSite attributes
- **Session Encryption**: AES encryption for sensitive session data
- **Redis Security**: Password auth, disabled dangerous commands
- **Session Timeout**: Configurable idle timeouts

## Performance Optimization

### NGINX Tuning

```nginx
worker_processes auto;
worker_connections 1024;
keepalive_timeout 60s;
client_max_body_size 8M;
gzip_comp_level 6;
```

### Redis Optimization

```redis
tcp-keepalive 60
timeout 300
maxclients 1000
hash-max-ziplist-entries 512
```

### Application Tuning

- **Connection Pooling**: EF Core connection pool sizing
- **Memory Management**: GC tuning for server workloads  
- **SignalR Settings**: Message size limits, timeout configuration
- **Static Content**: CDN integration for production

## Troubleshooting

### Common Issues

**Sticky Sessions Not Working**:
- Verify `ip_hash` in NGINX upstream
- Check SignalR Redis backplane configuration
- Ensure session cookies are properly set

**Auto-scaling Issues**:
- Check metrics collection (Prometheus connectivity)
- Verify HPA/autoscaler permissions
- Review scaling thresholds and windows

**Health Check Failures**:
- Check application startup time vs. probe timing
- Verify database connectivity
- Review resource limits vs. health check thresholds

**Redis Connection Issues**:
- Verify password configuration across services
- Check Redis memory limits and eviction policy
- Monitor connection pool exhaustion

### Monitoring Commands

```powershell
# Docker Compose
docker-compose -f docker-compose.yml -f docker-compose.loadbalanced.yml logs -f nginx-lb
docker-compose -f docker-compose.yml -f docker-compose.loadbalanced.yml exec redis redis-cli info

# Kubernetes  
kubectl logs -f deployment/setlist-studio-web -n setlist-studio
kubectl describe hpa setlist-studio-web-hpa -n setlist-studio
kubectl top pods -n setlist-studio

# Docker Swarm
docker service logs setlist-studio_web -f
docker service ps setlist-studio_web
```

## Migration Path

### Single Instance → Load Balanced

1. **Add Redis**: Deploy Redis container/service
2. **Update Configuration**: Enable load balancing flags
3. **Deploy Load Balancer**: Add NGINX proxy  
4. **Scale Instances**: Add additional app instances
5. **Test & Monitor**: Verify session affinity and health checks

### Load Balanced → Auto-scaling

1. **Add Monitoring**: Deploy Prometheus and metrics collection
2. **Configure Auto-scaler**: Set up HPA or custom scaling logic
3. **Tune Thresholds**: Adjust scaling triggers based on load patterns
4. **Test Scaling**: Verify scale up/down behavior under load

## Best Practices

### Deployment

- **Blue/Green Deployments**: Use for zero-downtime updates
- **Health Check Grace Period**: Allow startup time before routing traffic
- **Resource Limits**: Set appropriate CPU/memory limits for predictable scaling
- **Monitoring First**: Deploy monitoring before enabling auto-scaling

### Operations

- **Load Testing**: Regularly test scaling behavior
- **Capacity Planning**: Monitor trends for proactive scaling
- **Disaster Recovery**: Plan for Redis/database failover scenarios  
- **Security Updates**: Keep load balancer and container images updated

### Development

- **Local Testing**: Use `docker-compose.loadbalanced.yml` for development
- **Feature Flags**: Use for gradual rollout of load balancing features
- **Logging**: Ensure instance identification in logs for debugging
- **Session State**: Minimize server-side session state for better scalability

## Conclusion

This load balancing implementation provides Setlist Studio with:

- **Horizontal Scalability**: Handle increased load by adding instances
- **High Availability**: Automatic failover and health-based routing
- **Performance**: Optimized for Blazor Server SignalR connections  
- **Monitoring**: Comprehensive metrics for informed scaling decisions
- **Flexibility**: Support for different deployment environments

The architecture scales from development (Docker Compose) to production (Kubernetes) while maintaining consistent behavior and configuration patterns.