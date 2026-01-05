# Base Build Image - Copies Source & Dependencies
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base-env
WORKDIR /src

# Copy the entire monorepo source once
COPY . .

# Run a basic list to verify (optional, helpful for debug)
RUN ls -la

# We do NOT run restore/build here, we let the specific services do that
# to allow for parallel building of different services if needed, 
# although they share this base layer.
