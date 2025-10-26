# Multi-stage build for .NET 8 + React
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage for .NET
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["NextGenSoftware.OASIS.API.ONODE.OPORTAL/NextGenSoftware.OASIS.API.ONODE.OPORTAL.csproj", "NextGenSoftware.OASIS.API.ONODE.OPORTAL/"]
COPY ["NextGenSoftware.OASIS.API.ONODE.Core/NextGenSoftware.OASIS.API.ONODE.Core.csproj", "NextGenSoftware.OASIS.API.ONODE.Core/"]
COPY ["../../OASIS Architecture/NextGenSoftware.OASIS.API.DNA/NextGenSoftware.OASIS.API.DNA.csproj", "../../OASIS Architecture/NextGenSoftware.OASIS.API.DNA/"]

# Restore .NET dependencies
RUN dotnet restore "NextGenSoftware.OASIS.API.ONODE.OPORTAL/NextGenSoftware.OASIS.API.ONODE.OPORTAL.csproj"

# Copy all source code
COPY . .

# Build .NET application
WORKDIR "/src/NextGenSoftware.OASIS.API.ONODE.OPORTAL"
RUN dotnet build "NextGenSoftware.OASIS.API.ONODE.OPORTAL.csproj" -c Release -o /app/build

# Publish .NET application
FROM build AS publish
RUN dotnet publish "NextGenSoftware.OASIS.API.ONODE.OPORTAL.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Node.js stage for React build
FROM node:18-alpine AS node-build
WORKDIR /app

# Copy package files
COPY NextGenSoftware.OASIS.API.ONODE.OPORTAL/package*.json ./

# Install dependencies
RUN npm ci --only=production

# Copy React source
COPY NextGenSoftware.OASIS.API.ONODE.OPORTAL/ClientApp/ ./ClientApp/

# Build React app
WORKDIR /app/ClientApp
RUN npm run build

# Final stage
FROM base AS final
WORKDIR /app

# Copy .NET application
COPY --from=publish /app/publish .

# Copy React build
COPY --from=node-build /app/ClientApp/build ./wwwroot/

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "NextGenSoftware.OASIS.API.ONODE.OPORTAL.dll"]