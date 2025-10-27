# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the necessary project files first
COPY "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj" "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/"
COPY "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Controllers/" "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Controllers/"
COPY "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Program.cs" "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/"
COPY "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Properties/" "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Properties/"
COPY "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/appsettings*.json" "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/"

# Copy the Core project files
COPY "STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/NextGenSoftware.OASIS.STAR.CLI.Lib.csproj" "STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/"
COPY "STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/" "STAR ODK/NextGenSoftware.OASIS.STAR.CLI.Lib/"

# Copy OASIS Core dependencies
COPY "OASIS Architecture/NextGenSoftware.OASIS.API.Core/NextGenSoftware.OASIS.API.Core.csproj" "OASIS Architecture/NextGenSoftware.OASIS.API.Core/"
COPY "OASIS Architecture/NextGenSoftware.OASIS.API.Core/" "OASIS Architecture/NextGenSoftware.OASIS.API.Core/"

# Copy Providers
COPY "Providers/" "Providers/"

# Build the STAR WebAPI project
WORKDIR "/src/STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI"
RUN dotnet restore "NextGenSoftware.OASIS.STAR.WebAPI.csproj"
RUN dotnet build "NextGenSoftware.OASIS.STAR.WebAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
WORKDIR "/src/STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI"
RUN dotnet publish "NextGenSoftware.OASIS.STAR.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "NextGenSoftware.OASIS.STAR.WebAPI.dll"]