# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj", "NextGenSoftware.OASIS.API.ONODE.WebAPI/"]
COPY ["NextGenSoftware.OASIS.API.ONODE.Core/NextGenSoftware.OASIS.API.ONODE.Core.csproj", "NextGenSoftware.OASIS.API.ONODE.Core/"]
COPY ["../../OASIS Architecture/NextGenSoftware.OASIS.API.DNA/NextGenSoftware.OASIS.API.DNA.csproj", "../../OASIS Architecture/NextGenSoftware.OASIS.API.DNA/"]

# Restore dependencies
RUN dotnet restore "NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/NextGenSoftware.OASIS.API.ONODE.WebAPI"
RUN dotnet build "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll"]