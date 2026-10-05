# OASIS API Docker Context Document

## Project Overview
This is the OASIS API - a comprehensive blockchain and NFT management platform that supports multiple blockchain providers including Solana, Ethereum, and others. The API is built with .NET 9.0 and provides REST endpoints for avatar management, NFT operations, and blockchain interactions.

## Current Status
✅ **API BUILD STATUS: SUCCESSFUL**
- Build completed with 0 errors, 74 warnings (mostly XML documentation issues)
- Target Framework: .NET 9.0
- All NFT controller compilation issues have been resolved
- Ready for Docker image creation

## Project Structure
```
/Volumes/Storage/OASIS_CLEAN/
├── NextGenSoftware.OASIS.API.ONODE.WebAPI/          # Main API project
│   ├── Controllers/
│   │   ├── NftController.cs                         # ✅ Fixed - NFT operations
│   │   ├── AvatarController.cs                      # Avatar management
│   │   ├── DataController.cs                       # Data operations
│   │   └── ...other controllers
│   ├── Models/                                      # Request/Response models
│   ├── Services/                                    # Business logic
│   ├── OASIS_DNA.json                              # Configuration file
│   └── NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj
├── NextGenSoftware.OASIS.API.Core/                 # Core API library
├── NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/ # Solana provider
├── NextGenSoftware.OASIS.API.Providers.MongoOASIS/  # MongoDB provider
└── ...other provider projects
```

## Key Configuration Files

### 1. Project File
**Location**: `NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj`
- Target Framework: `net9.0`
- Project Type: Web API
- Dependencies: Multiple OASIS provider projects

### 2. Configuration File
**Location**: `NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`
- Contains database connection strings
- Provider configurations
- MongoDB connection: `mongodb+srv://OASISWEB4:Uppermall1!@oasisweb4.ifxnugb.mongodb.net/?retryWrites=true&w=majority&appName=OASISWeb4`

## Recent Fixes Applied

### 1. NFT Controller Issues (RESOLVED)
- **Problem**: NftController.cs had compilation errors due to incorrect enum parsing
- **Solution**: Updated to use `EnumValue<T>.Value` property instead of trying to parse already-parsed values
- **Files Modified**: 
  - `NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/NftController.cs`
  - Added `using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;`

### 2. .NET Version Mismatch (RESOLVED)
- **Problem**: Project was targeting .NET 8.0 but system had .NET 9.0.7
- **Solution**: Updated project file to target `net9.0`
- **File Modified**: `NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj`

### 3. Build Dependencies (RESOLVED)
- All project references are correctly configured
- All provider projects build successfully
- No missing dependencies

## Docker Build Instructions

### Dockerfile Location
Create the Dockerfile in the root directory: `/Volumes/Storage/OASIS_CLEAN/Dockerfile`

### Dockerfile Content
```dockerfile
# Use the official .NET 9.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the solution file
COPY "The OASIS.sln" .

# Copy all project files
COPY "NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" "NextGenSoftware.OASIS.API.ONODE.WebAPI/"
COPY "NextGenSoftware.OASIS.API.Core/NextGenSoftware.OASIS.API.Core.csproj" "NextGenSoftware.OASIS.API.Core/"
COPY "NextGenSoftware.OASIS.API.DNA/NextGenSoftware.OASIS.API.DNA.csproj" "NextGenSoftware.OASIS.API.DNA/"
COPY "NextGenSoftware.OASIS.API.ONODE.Core/NextGenSoftware.OASIS.API.ONODE.Core.csproj" "NextGenSoftware.OASIS.API.ONODE.Core/"
COPY "NextGenSoftware.OASIS.OASISBootLoader/NextGenSoftware.OASIS.OASISBootLoader.csproj" "NextGenSoftware.OASIS.OASISBootLoader/"

# Copy all provider projects
COPY "NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.csproj" "NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/"
COPY "NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj" "NextGenSoftware.OASIS.API.Providers.MongoOASIS/"
COPY "NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj" "NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/"
COPY "NextGenSoftware.OASIS.API.Providers.IPFSOASIS/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.csproj" "NextGenSoftware.OASIS.API.Providers.IPFSOASIS/"
COPY "NextGenSoftware.OASIS.API.Providers.EthereumOASIS/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.csproj" "NextGenSoftware.OASIS.API.Providers.EthereumOASIS/"
COPY "NextGenSoftware.OASIS.API.Providers.SOLIDOASIS/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.csproj" "NextGenSoftware.OASIS.API.Providers.SOLIDOASIS/"
COPY "NextGenSoftware.OASIS.API.Providers.BlockStackOASIS/NextGenSoftware.OASIS.API.Providers.BlockStackOASIS.csproj" "NextGenSoftware.OASIS.API.Providers.BlockStackOASIS/"
COPY "NextGenSoftware.OASIS.API.Providers.EOSIOOASIS/NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.csproj" "NextGenSoftware.OASIS.API.Providers.EOSIOOASIS/"

# Copy external libraries
COPY "External Libs/net-ipfs-http-client-master/src/IpfsHttpClient.csproj" "External Libs/net-ipfs-http-client-master/src/"
COPY "holochain-client-csharp/NextGenSoftware.Holochain.HoloNET.Client/NextGenSoftware.Holochain.HoloNET.Client.csproj" "holochain-client-csharp/NextGenSoftware.Holochain.HoloNET.Client/"
COPY "holochain-client-csharp/NextGenSoftware.Holochain.HoloNET.ORM/NextGenSoftware.Holochain.HoloNET.ORM.csproj" "holochain-client-csharp/NextGenSoftware.Holochain.HoloNET.ORM/"

# Copy NextGenSoftware libraries
COPY "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Utilities/NextGenSoftware.Utilities.csproj" "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Utilities/"
COPY "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.ErrorHandling/NextGenSoftware.ErrorHandling.csproj" "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.ErrorHandling/"
COPY "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.CLI.Engine/NextGenSoftware.CLI.Engine.csproj" "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.CLI.Engine/"
COPY "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Logging/NextGenSoftware.Logging.csproj" "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Logging/"
COPY "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Logging.NLog/NextGenSoftware.Logging.NLog.csproj" "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.Logging.NLog/"
COPY "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.WebSocket/NextGenSoftware.WebSocket.csproj" "NextGenSoftware-Libraries/NextGenSoftware Libraries/NextGenSoftware.WebSocket/"

# Restore dependencies
RUN dotnet restore "NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"

# Copy the rest of the source code
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

# Copy configuration files
COPY "NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json" .

ENTRYPOINT ["dotnet", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll"]
```

### Docker Build Commands
```bash
# Navigate to the project root
cd /Volumes/Storage/OASIS_CLEAN

# Build the Docker image
docker build -t oasis-api .

# Run the container
docker run -p 5000:80 -p 5001:443 oasis-api

# Run with environment variables (for MongoDB connection)
docker run -p 5000:80 -p 5001:443 \
  -e "ConnectionStrings__MongoDBOASIS=mongodb+srv://OASISWEB4:Uppermall1!@oasisweb4.ifxnugb.mongodb.net/?retryWrites=true&w=majority&appName=OASISWeb4" \
  oasis-api
```

### Docker Compose (Alternative)
Create `docker-compose.yml` in the root directory:
```yaml
version: '3.8'
services:
  oasis-api:
    build: .
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ConnectionStrings__MongoDBOASIS=mongodb+srv://OASISWEB4:Uppermall1!@oasisweb4.ifxnugb.mongodb.net/?retryWrites=true&w=majority&appName=OASISWeb4
    restart: unless-stopped
```

### Build Commands
```bash
# Using Docker Compose
docker-compose up --build

# Or build and run separately
docker-compose build
docker-compose up
```

### Docker Requirements

### Base Image
- Use .NET 9.0 runtime image
- Recommended: `mcr.microsoft.com/dotnet/aspnet:9.0`

### Port Configuration
- The API typically runs on port 5000 (HTTP) and 5001 (HTTPS)
- Check `launchSettings.json` for exact port configuration

### Environment Variables
- Database connection strings should be configurable via environment variables
- MongoDB connection string is in `OASIS_DNA.json`

### Required Files for Docker
1. `NextGenSoftware.OASIS.API.ONODE.WebAPI/` (main project)
2. All referenced provider projects
3. `OASIS_DNA.json` configuration file
4. Solution file: `The OASIS.sln`

## Build Commands
```bash
# Navigate to project directory
cd /Volumes/Storage/OASIS_CLEAN/NextGenSoftware.OASIS.API.ONODE.WebAPI

# Build the project
dotnet build

# Run the project (for testing)
dotnet run --configuration Release
```

## Key Features
- **Avatar Management**: User registration, authentication, profile management
- **NFT Operations**: Minting, transferring, managing NFTs across multiple blockchains
- **Multi-Provider Support**: Solana, Ethereum, MongoDB, IPFS, and more
- **REST API**: Swagger documentation available at `/swagger/index.html`

## Testing Endpoints
- Swagger UI: `http://localhost:5000/swagger/index.html`
- Avatar Registration: `POST /api/avatar/register`
- NFT Minting: `POST /api/nft/mint-nft`

## Notes for Docker Agent
1. The API is currently in a working state with all compilation errors resolved
2. Focus on creating a production-ready Docker image
3. Ensure all provider dependencies are included
4. Consider multi-stage build for optimization
5. The API requires MongoDB connection - ensure this is configurable
6. All recent fixes have been applied and tested

## Contact Information
- User prefers methodical approach
- User prefers not to be asked for permission on known working steps
- User wants to take things slowly rather than rushing ahead

---
**Last Updated**: Current session
**Status**: Ready for Docker image creation
**Build Status**: ✅ Successful (0 errors, 74 warnings)
