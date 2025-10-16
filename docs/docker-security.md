# Docker Security Configuration

## Overview

Setlist Studio implements comprehensive container security hardening following industry best practices and security standards. This document outlines the security measures implemented in our Docker configuration.

## Security Hardening Measures

### 1. Base Image Security

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
```

- **Alpine Linux Base**: Minimal attack surface with fewer packages and vulnerabilities
- **Official Microsoft Images**: Regularly updated and security-scanned base images
- **Multi-stage Build**: Separates build and runtime environments to minimize attack surface

### 2. Non-Root User Execution

```dockerfile
RUN addgroup -g 1001 -S setliststudio && \
    adduser -u 1001 -S setliststudio -G setliststudio
USER setliststudio:setliststudio
```

- **Custom User**: Application runs as non-root user (UID/GID 1001)
- **Principle of Least Privilege**: Prevents privilege escalation attacks
- **Security Group**: Dedicated group for proper file permissions

### 3. File Permission Hardening

```dockerfile
RUN chmod 640 /app/*.dll /app/*.json /app/*.pdb && \
    chmod 750 /app/wwwroot /app/Views /app/Areas && \
    chown -R setliststudio:setliststudio /app
```

- **Restrictive Permissions**: 640 for files, 750 for directories
- **Owner-only Write**: Prevents unauthorized file modifications
- **Read-only Application**: Runtime files are read-only for security

### 4. Network Security

```dockerfile
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
```

- **Non-Privileged Port**: Uses port 8080 instead of privileged port 80
- **Explicit Port Binding**: No automatic port exposure
- **Network Isolation**: Separate networks for frontend/backend communication

### 5. Container Runtime Security

```yaml
security_opt:
  - no-new-privileges:true
  - apparmor:docker-default
cap_drop:
  - ALL
cap_add:
  - CHOWN
  - SETGID
  - SETUID
```

- **No New Privileges**: Prevents privilege escalation at runtime
- **AppArmor Profile**: Mandatory access control for additional protection
- **Minimal Capabilities**: Only essential Linux capabilities enabled

### 6. Resource Constraints

```yaml
mem_limit: 512m
cpu_quota: 50000
cpu_period: 100000
pids_limit: 100
```

- **Memory Limits**: Prevents memory exhaustion attacks
- **CPU Throttling**: 50% CPU usage limit for stability
- **Process Limits**: Maximum 100 processes to prevent fork bombs

### 7. Read-Only Filesystem

```yaml
read_only: true
tmpfs:
  - /tmp:noexec,nosuid,size=32m
  - /app/logs:noexec,nosuid,size=64m
```

- **Immutable Runtime**: Prevents file system modifications
- **Temporary Filesystems**: Secure tmpfs mounts for necessary write operations
- **NoExec/NoSuid**: Prevents execution of malicious binaries

## Environment-Specific Configurations

### Production Environment (docker-compose.prod.yml)

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - DOTNET_EnableDiagnostics=0
  - ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Warning
```

**Security Features:**
- Diagnostics disabled to prevent information leakage
- Minimal logging to reduce attack surface
- Production security headers enabled
- HTTPS enforcement with Let's Encrypt certificates

### Development Environment (docker-compose.dev.yml)

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - DOTNET_EnableDiagnostics=1
  - ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Debug
```

**Security Features:**
- Enhanced debugging for development productivity
- Relaxed (but still secure) resource constraints
- Development-friendly port mappings
- Hot reload support for faster development

## Security Scanning Integration

### 1. Build-Time Security Scanning

```dockerfile
# Security scanning in build stage
RUN if [ "$SECURITY_SCAN" = "true" ]; then \
    apk add --no-cache trivy && \
    trivy filesystem --exit-code 1 /app; \
fi
```

### 2. Runtime Vulnerability Detection

- **Container Image Scanning**: Automated vulnerability scanning in CI/CD
- **Dependency Scanning**: OWASP dependency check integration
- **Security Monitoring**: Runtime threat detection and logging

## Network Security Architecture

### Production Network Topology

```yaml
networks:
  setlist-frontend:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
  setlist-backend:
    driver: bridge
    internal: true  # No external access
    ipam:
      config:
        - subnet: 172.21.0.0/16
```

**Security Benefits:**
- **Network Segmentation**: Separate frontend and backend networks
- **Internal Backend**: Database network has no external access
- **Subnet Isolation**: Dedicated IP ranges for service isolation

## Secrets Management

### Secure Configuration

```dockerfile
# No secrets in container images
ENV ConnectionStrings__DefaultConnection=""
ENV Authentication__Google__ClientId="YOUR_CLIENT_ID"
```

**Best Practices:**
- **No Hardcoded Secrets**: All secrets provided via environment variables
- **Runtime Configuration**: Secrets injected at container startup
- **Validation**: Application validates secrets are not placeholder values

## Monitoring and Logging

### Security Event Logging

```yaml
logging:
  driver: "json-file"
  options:
    max-size: "10m"
    max-file: "3"
```

**Security Monitoring:**
- **Structured Logging**: JSON format for security event analysis
- **Log Rotation**: Prevents disk exhaustion attacks
- **Audit Trail**: Comprehensive security event tracking

## Compliance and Standards

### Security Standards Alignment

- **CIS Docker Benchmark**: Follows CIS Docker security recommendations
- **NIST Framework**: Aligns with NIST cybersecurity framework
- **OWASP Container Security**: Implements OWASP container security guidelines
- **Defense in Depth**: Multiple layers of security controls

### Security Testing

```bash
# Container security testing
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image setliststudio:latest

# Runtime security testing
docker run --rm -it --pid container:setliststudio \
  --net container:setliststudio --cap-add SYS_PTRACE \
  aquasec/tracee
```

## Deployment Commands

### Production Deployment

```bash
# Production deployment with security hardening
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Security scan before deployment
docker-compose -f docker-compose.yml -f docker-compose.prod.yml build --build-arg SECURITY_SCAN=true
```

### Development Setup

```bash
# Development with debugging enabled
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up

# Hot reload development
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

## Security Incident Response

### Container Security Events

1. **Detection**: Automated scanning detects vulnerability
2. **Assessment**: Security team evaluates impact and severity
3. **Remediation**: Build new image with security patches
4. **Deployment**: Zero-downtime deployment of secure image
5. **Validation**: Post-deployment security verification

### Emergency Procedures

```bash
# Emergency container shutdown
docker-compose down --remove-orphans

# Security incident investigation
docker logs setliststudio-web > security-incident.log
docker inspect setliststudio-web > container-state.json
```

## Security Maintenance

### Regular Security Updates

- **Base Image Updates**: Weekly scanning for new base images
- **Dependency Updates**: Monthly .NET and package updates
- **Security Patches**: Emergency patching for critical vulnerabilities
- **Configuration Reviews**: Quarterly security configuration audits

### Security Metrics

- **Container Vulnerability Count**: Target < 5 high/critical vulnerabilities
- **Security Scan Pass Rate**: Target 100% for production deployments
- **Incident Response Time**: Target < 4 hours for critical security issues
- **Patch Deployment Time**: Target < 24 hours for security patches

This comprehensive Docker security configuration ensures Setlist Studio maintains enterprise-grade container security across all deployment environments.