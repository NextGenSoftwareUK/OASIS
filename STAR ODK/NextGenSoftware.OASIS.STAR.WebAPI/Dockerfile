# Build context must be the repo root (C:/Source/OASIS2/).
# Railway uses repo root as build context automatically.
# For local builds run from C:/Source/OASIS2/:
#   docker build -f "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Dockerfile" -t oasis-star-api .

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Clone sibling repos that csproj files reference via relative paths.
# Cloned to / so that ../../../../ and ../../../ relative paths resolve correctly.
ARG LIBRARIES_REPO=https://github.com/NextGenSoftwareUK/NextGenSoftware-Libraries.git
ARG HOLOCHAIN_REPO=https://github.com/holochain-open-dev/holochain-client-csharp.git
RUN apt-get update && apt-get install -y --no-install-recommends git \
    && git clone --depth 1 "${LIBRARIES_REPO}" /NextGenSoftware-Libraries \
    && git clone --depth 1 "${HOLOCHAIN_REPO}" /holochain-client-csharp \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

# OASIS_DNA.json is gitignored. The Default version is used here only as a build-time
# placeholder — it has CopyToPublishDirectory=Never so it is NOT included in the
# published image. The real OASIS_DNA.json must be mounted by Railway at runtime.
RUN test -f "/src/OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASIS_DNA.json" || \
    cp "/src/OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json" \
       "/src/OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASIS_DNA.json"

WORKDIR "/src/STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI"
RUN dotnet restore "NextGenSoftware.OASIS.STAR.WebAPI.csproj"
RUN dotnet publish "NextGenSoftware.OASIS.STAR.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/entrypoint.sh" /entrypoint.sh
RUN sed -i 's/\r//' /entrypoint.sh && chmod +x /entrypoint.sh

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["/entrypoint.sh"]
