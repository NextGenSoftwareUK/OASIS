#!/bin/bash

# Build script for Docker that temporarily copies external libraries to expected locations
# This allows the existing project references to work without modification

echo "Setting up external libraries for Docker build..."

# Create temporary directories for external libraries
mkdir -p "OASIS Architecture/NextGenSoftware.OASIS.Common/NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Logging"
mkdir -p "OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Logging.NLog"
mkdir -p "Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS/holochain-client-csharp"

# Copy the actual external libraries to the expected locations
cp -r "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Logging"/* "OASIS Architecture/NextGenSoftware.OASIS.Common/NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Logging/"
cp -r "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Logging.NLog"/* "OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Logging.NLog/"
cp -r "holochain-client-csharp"/* "Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS/holochain-client-csharp/"

echo "External libraries set up. Building Docker image..."

# Build the Docker image
docker build -f Dockerfile.clean -t star-api .

echo "Cleaning up temporary directories..."

# Clean up temporary directories
rm -rf "OASIS Architecture/NextGenSoftware.OASIS.Common/NextGenSoftware-Libraries"
rm -rf "OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/NextGenSoftware-Libraries"
rm -rf "Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS/holochain-client-csharp"

echo "Build complete!"
