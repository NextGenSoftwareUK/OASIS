# Production Dockerfile for OASIS API ONODE WebAPI
# Based on working image: sha256:96a7bd158ecb (mainnet-update tag)
# Optimized for AWS ECS Fargate deployment
# Updated for .NET 9.0 and latest build fixes

# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Use the .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all source code (including all provider projects)
# The solution file references all providers, so restoring from solution will include them all
COPY . .

# Create symlink for NextGenSoftware-Libraries that the solution file expects one level up
# The solution file references ../NextGenSoftware-Libraries
# But in Docker, everything is in /src/, so we create a symlink to match expected paths
# Note: holochain-client-csharp is excluded (not needed for ONODE WebAPI)
RUN cd /src && \
    if [ ! -d "../NextGenSoftware-Libraries" ] && [ -d "NextGenSoftware-Libraries" ]; then \
        ln -s /src/NextGenSoftware-Libraries ../NextGenSoftware-Libraries; \
    fi

# Restore dependencies from project file directly (avoids solution file path issues)
# This will automatically restore all dependencies including all 30+ providers
WORKDIR "/src/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
RUN dotnet restore "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" --verbosity minimal

# Build the application (without explicit output to avoid file locking issues)
WORKDIR "/src/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
RUN dotnet build "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release --no-incremental

# Publish the application
FROM build AS publish
WORKDIR "/src/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
RUN dotnet publish "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# OASIS_DNA.json should be included in the published output if it exists in the project
# If not present, configuration can be provided via environment variables in ECS task definition

# Set environment variables (can be overridden by ECS task definition or docker-compose)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Health check - use Swagger endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:80/swagger/index.html || exit 1

ENTRYPOINT ["dotnet", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll"]

