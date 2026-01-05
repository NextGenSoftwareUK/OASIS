# Build Stage - Depends on the Base Image
FROM oasis-monorepo-base:latest AS build
WORKDIR /src

# Restore and Build STAR API
RUN dotnet restore "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj"
RUN dotnet publish "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy compiled app from build stage
COPY --from=build /app/publish .

# Copy Configuration from Build Stage (it was in the base image source)
COPY --from=build ["/src/OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASIS_DNA.json", "./"]

# Set Entrypoint
ENTRYPOINT ["dotnet", "NextGenSoftware.OASIS.STAR.WebAPI.dll"]
