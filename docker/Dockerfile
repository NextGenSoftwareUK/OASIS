# Production Dockerfile for OASIS API ONODE WebAPI
# Based on working image: sha256:96a7bd158ecb (mainnet-update tag)
# Optimized for AWS ECS Fargate deployment
# Updated for .NET 8.0 and latest build fixes (EnumValue BSON fix, MongoDB health check)

ARG OASIS_API_VERSION=4.4.4
ARG BUILD_DATE
ARG VCS_REF

# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG OASIS_API_VERSION=4.4.4
WORKDIR /app
EXPOSE 80
EXPOSE 443

LABEL org.opencontainers.image.title="OASIS API ONODE WebAPI" \
      org.opencontainers.image.version="${OASIS_API_VERSION}" \
      org.opencontainers.image.description="OASIS API ONODE WebAPI - avatars, NFTs, MongoDB, health" \
      org.opencontainers.image.vendor="NextGen Software" \
      oasis.api.version="${OASIS_API_VERSION}"

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Use the .NET 9 SDK for building (can build both .NET 8.0 and 9.0 projects)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all source code (excluding Holochain provider which has missing dependencies)
# The solution file references all providers, so restoring from solution will include them all
COPY . .
# Remove Holochain provider and related files to avoid build errors
RUN rm -rf /src/Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS && \
    rm -f /src/ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/HoloNETClientExtensions.cs && \
    rm -f /src/ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/HoloNETP2PProvider.cs && \
    rm -f /src/ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/NetworkMetricsService.cs
# NetworkMetrics.cs is now committed to the repo — no need to generate it here
# Comment out HoloOASIS references in OASISBootLoader
RUN sed -i 's/^using NextGenSoftware.OASIS.API.Providers.HoloOASIS;/\/\/using NextGenSoftware.OASIS.API.Providers.HoloOASIS;/' "/src/OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.cs" && \
    sed -i '/case ProviderType.HoloOASIS:/,/^[[:space:]]*break;/s/^/\/\//' "/src/OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.cs" && \
    sed -i '/private static void HoloOASIS_StorageProviderError/,/^[[:space:]]*}/s/^/\/\//' "/src/OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.cs" || true
# Comment out HoloOASIS references in ONETManager (be careful not to comment out other fields)
RUN sed -i 's/^using NextGenSoftware.OASIS.API.Providers.HoloOASIS;/\/\/using NextGenSoftware.OASIS.API.Providers.HoloOASIS;/' "/src/ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/ONETManager.cs" && \
    sed -i 's/^[[:space:]]*private HoloOASIS/\/\/private HoloOASIS/' "/src/ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/ONETManager.cs" && \
    sed -i '/case P2PNetworkType.HoloNET:/,/^[[:space:]]*break;/s/^/\/\//' "/src/ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/ONETManager.cs" || true

# Create symlink for NextGenSoftware-Libraries that the solution file expects one level up
# The solution file references ../NextGenSoftware-Libraries
# But in Docker, everything is in /src/, so we create a symlink to match expected paths
# Note: holochain-client-csharp is excluded (not needed for ONODE WebAPI)
RUN cd /src && \
    if [ ! -d "../NextGenSoftware-Libraries" ] && [ -d "NextGenSoftware-Libraries" ]; then \
        ln -s /src/NextGenSoftware-Libraries ../NextGenSoftware-Libraries; \
    fi

# Clear Libraries obj/bin to avoid "file already exists" when same project is referenced via symlink and /src path
RUN rm -rf /src/NextGenSoftware-Libraries/NextGenSoftware\ Libraries/*/obj /src/NextGenSoftware-Libraries/NextGenSoftware\ Libraries/*/bin 2>/dev/null || true

# Restore dependencies from project file directly (avoids solution file path issues)
# This will automatically restore all dependencies including all 30+ providers
WORKDIR "/src/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
RUN dotnet restore "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" --verbosity minimal

# Build the application (targeting .NET 8.0 specifically to avoid multi-target issues)
WORKDIR "/src/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
RUN dotnet build "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release -f net8.0 --no-incremental

# Publish the application (targeting .NET 8.0 specifically)
FROM build AS publish
WORKDIR "/src/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
RUN dotnet publish "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release -f net8.0 -o /app/publish /p:UseAppHost=false
# Remove HTTPS endpoint from appsettings.json for Docker (no certificate available)
# Install jq for JSON manipulation
RUN apt-get update && apt-get install -y jq && rm -rf /var/lib/apt/lists/*
RUN cd /app/publish && jq 'del(.Kestrel.Endpoints.Https) | .Kestrel.Endpoints.Http.Url = "http://+:80"' appsettings.json > appsettings.json.tmp && mv appsettings.json.tmp appsettings.json

# Final stage
FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# OASIS_DNA.json is taken from ONODE/.../WebAPI/OASIS_DNA.json at build time (CopyToOutputDirectory in csproj).
# Keep that file in sync with OASIS Architecture/.../DNA/OASIS_DNA.json when updating MongoDB or other config for ECS.

# Set environment variables (can be overridden by ECS task definition or docker-compose)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Health check - use Swagger endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:80/api/health || exit 1

ENTRYPOINT ["dotnet", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll"]

