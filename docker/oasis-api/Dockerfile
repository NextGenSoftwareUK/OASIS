# Build Stage - Depends on the Base Image
FROM oasis-monorepo-base:latest AS build
WORKDIR /src

# Restore and Build OASIS API
RUN dotnet restore "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"
RUN dotnet publish "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy compiled app from build stage
COPY --from=build /app/publish .

# Copy Configuration from Build Stage (it was in the base image source)
COPY --from=build ["/src/OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASIS_DNA.json", "./"]

# Copy entrypoint script
COPY docker/oasis-api/entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh

# Set Entrypoint
ENTRYPOINT ["./entrypoint.sh", "dotnet", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll"]
