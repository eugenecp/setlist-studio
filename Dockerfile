# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/SetlistStudio.Web/SetlistStudio.Web.csproj", "SetlistStudio.Web/"]
COPY ["src/SetlistStudio.Core/SetlistStudio.Core.csproj", "SetlistStudio.Core/"]
COPY ["src/SetlistStudio.Infrastructure/SetlistStudio.Infrastructure.csproj", "SetlistStudio.Infrastructure/"]

RUN dotnet restore "SetlistStudio.Web/SetlistStudio.Web.csproj"

# Copy all source code
COPY src/ .

# Build the application
WORKDIR /src/SetlistStudio.Web
RUN dotnet build "SetlistStudio.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SetlistStudio.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl, ICU packages for globalization, and create non-root user for security
RUN apt-get update && apt-get install -y curl libicu-dev locales && \
    rm -rf /var/lib/apt/lists/* && \
    sed -i '/en_US.UTF-8/s/^# //g' /etc/locale.gen && \
    locale-gen && \
    groupadd -r setliststudio && useradd -r -g setliststudio setliststudio

# Copy published app
COPY --from=publish /app/publish .

# Create logs and data directories with proper permissions
RUN mkdir -p /app/logs /app/data && \
    chmod 755 /app/data /app/logs && \
    chown -R setliststudio:setliststudio /app/logs /app/data

# Set environment variables for ASP.NET Core
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=5000
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LC_ALL=en_US.UTF-8
ENV LANG=en_US.UTF-8

# Switch to non-root user
USER setliststudio

# Pre-create empty database file with proper permissions
RUN touch /app/data/setliststudio.db && chmod 644 /app/data/setliststudio.db

# Health check with longer start period for application initialization
# Using dedicated status endpoint that allows anonymous access
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:5000/api/status || exit 1

EXPOSE 5000

# Add detailed logging for startup debugging
ENV DOTNET_SYSTEM_DIAGNOSTICS_DEBUGLEVEL=Verbose
ENV ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Information

ENTRYPOINT ["dotnet", "SetlistStudio.Web.dll"]