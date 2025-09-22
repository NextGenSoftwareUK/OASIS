# Multi-stage build approach that mimics the successful individual builds

# Use the official .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything at once
COPY . .

# Restore and publish in one step (this approach works like the individual builds)
RUN dotnet publish "NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release -o /app/publish

# Use the official .NET 9.0 runtime as base image for final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy the published application
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll"]