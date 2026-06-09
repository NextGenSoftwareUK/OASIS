# Build context must be the repo root (C:/Source/OASIS2/).
# Railway uses repo root automatically; for local builds run from C:/Source/OASIS2/:
#   docker build -f ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Dockerfile -t oasis-onode-webapi .

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Clone the sibling repos that csproj files reference via ../../../../ relative paths.
# From /src/OASIS Architecture/<Project>/, ../../../../ resolves to the filesystem root,
# so we clone directly there to match the expected paths.
ARG LIBRARIES_REPO=https://github.com/NextGenSoftwareUK/NextGenSoftware-Libraries.git
ARG HOLOCHAIN_REPO=https://github.com/NextGenSoftwareUK/holochain-client-csharp.git
RUN apt-get update && apt-get install -y --no-install-recommends git \
    && git clone --depth 1 "${LIBRARIES_REPO}" /NextGenSoftware-Libraries \
    && git clone --depth 1 "${HOLOCHAIN_REPO}" /holochain-client-csharp \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

WORKDIR "/src/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
RUN dotnet restore "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"
RUN dotnet publish "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll"]
