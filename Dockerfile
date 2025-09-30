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

# Install curl and create non-root user for security
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/* \
    && groupadd -r setliststudio && useradd -r -g setliststudio setliststudio

# Copy published app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R setliststudio:setliststudio /app/logs

# Switch to non-root user
USER setliststudio

# Set environment variables for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

EXPOSE 5000
ENTRYPOINT ["dotnet", "SetlistStudio.Web.dll"]