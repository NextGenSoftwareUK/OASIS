# Use the official .NET 9.0 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/SmartContractGen/ScGen.API/ScGen.API.csproj", "src/SmartContractGen/ScGen.API/"]
COPY ["src/SmartContractGen/ScGen.Lib/ScGen.Lib.csproj", "src/SmartContractGen/ScGen.Lib/"]
COPY ["src/common/BuildingBlocks/BuildingBlocks.csproj", "src/common/BuildingBlocks/"]

# Restore dependencies
RUN dotnet restore "src/SmartContractGen/ScGen.API/ScGen.API.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/src/SmartContractGen/ScGen.API"
RUN dotnet build "ScGen.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ScGen.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Create the final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "ScGen.API.dll"]



