# Build stage with security scanning
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /app

# Install security scanning tools in build stage
RUN apk add --no-cache git curl

# Copy entire source structure to maintain project references
COPY . .

# Restore dependencies from the Web project
RUN dotnet restore "src/SetlistStudio.Web/SetlistStudio.Web.csproj" \
    --source https://api.nuget.org/v3/index.json \
    --verbosity minimal

# Build the application with production settings
WORKDIR /app/src/SetlistStudio.Web
RUN dotnet build "SetlistStudio.Web.csproj" \
    -c Release \
    --no-restore \
    --verbosity minimal \
    -p:TreatWarningsAsErrors=true \
    -p:WarningsAsErrors="" \
    -p:WarningsNotAsErrors="NU1603" \
    -p:NoWarn="CS0162"

# Publish stage 
FROM build AS publish
WORKDIR /app/src/SetlistStudio.Web
RUN dotnet publish "SetlistStudio.Web.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    --verbosity minimal

# Runtime stage with minimal attack surface
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

# Security: Update packages and install minimal required packages
RUN apk update && \
    apk upgrade --no-cache && \
    apk add --no-cache \
        curl \
        icu-data-full \
        icu-libs \
        tzdata && \
    rm -rf /var/cache/apk/* && \
    addgroup -g 1001 -S setliststudio && \
    adduser -S setliststudio -u 1001 -G setliststudio -h /app

WORKDIR /app

# Copy published app with specific ownership
COPY --from=publish --chown=setliststudio:setliststudio /app/publish .

# Copy database security scripts
COPY --chown=setliststudio:setliststudio scripts/secure-database.sh /app/secure-database.sh

# Create logs and data directories with enhanced security permissions and setup scripts
RUN mkdir -p /app/logs /app/data && \
    chmod 700 /app/data && \
    chmod 750 /app/logs && \
    chown -R setliststudio:setliststudio /app/logs /app/data /app && \
    find /app -type f -exec chmod 640 {} \; && \
    chmod 750 /app/SetlistStudio.Web.dll && \
    chmod +x /app/secure-database.sh

# Security: Set restrictive environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV LC_ALL=en_US.UTF-8
ENV LANG=en_US.UTF-8

# Security: Remove unnecessary capabilities and set read-only filesystem
# The container should run with --read-only flag and temp volumes for /app/logs and /app/data

# Switch to non-root user for security
USER setliststudio

# Create empty database file with enhanced security permissions (will be overridden by volume in production)
# Note: chattr +i (immutable) requires privileged container or specific capabilities
RUN touch /app/data/setliststudio.db && \
    chmod 600 /app/data/setliststudio.db && \
    # Set restrictive umask for any future file creation
    echo 'umask 077' >> ~/.bashrc

# Security-enhanced health check with minimal privileges
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/api/status || exit 1

# Use non-root port for better security
EXPOSE 8080

# Security: Minimize information leakage in logs
ENV DOTNET_SYSTEM_DIAGNOSTICS_DEBUGLEVEL=Error
ENV ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Warning
ENV ASPNETCORE_LOGGING__LOGLEVEL__Microsoft=Error
ENV ASPNETCORE_LOGGING__LOGLEVEL__System=Error

# Security labels for container scanning
LABEL security.scan="enabled"
LABEL security.non-root="true"
LABEL security.readonly="recommended"
LABEL maintainer="eugenecp"
LABEL org.opencontainers.image.source="https://github.com/eugenecp/setlist-studio"
LABEL org.opencontainers.image.title="Setlist Studio"
LABEL org.opencontainers.image.description="Secure music setlist management application"

ENTRYPOINT ["dotnet", "SetlistStudio.Web.dll"]