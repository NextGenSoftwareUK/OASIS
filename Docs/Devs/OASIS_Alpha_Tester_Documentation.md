# OASIS Alpha Tester Documentation

## Table of Contents
1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
3. [HoloNET Usage](#holonet-usage)
4. [OASIS APIs](#oasis-apis)
5. [Provider Architecture](#provider-architecture)
6. [Avatar Authentication](#avatar-authentication)
7. [Test Harnesses](#test-harnesses)
8. [STAR System](#star-system)
9. [Additional Resources](#additional-resources)

---

## Introduction

Welcome to the OASIS (Open Advanced Secure Interoperable Systems) Alpha Testing Program! OASIS is a comprehensive Web3 platform that provides:

- **Programmable Digital Identity** (Avatars with wallets, NFTs, karma)
- **Cross-chain Interoperability** (Ethereum, Solana, Holochain, Arbitrum, etc.)
- **Modular Architecture** (APIs, providers, templates)
- **STAR Engine** (Smart Template and Action Renderer)

This documentation will guide you through the key components and how to use them effectively.

---

## Getting Started

### Prerequisites
- .NET 8.0 or later
- Node.js (for some components)
- Git
- Basic understanding of Web3 concepts

### Installation
1. Clone the OASIS repository
2. Navigate to the project root
3. Run `dotnet restore` to restore dependencies
4. Build the solution using `dotnet build`

### Quick Start
The fastest way to get started is through the test harnesses, which provide working examples of all major functionality.

---

## HoloNET Usage

HoloNET is the .NET client for Holochain, enabling you to connect any .NET or Unity application to Holochain's decentralized architecture.

### Basic Setup

```csharp
using NextGenSoftware.Holochain.HoloNET.Client;

// Initialize HoloNET client
var holoNETClient = new HoloNETClient("ws://localhost:8888");

// Wire up events
holoNETClient.OnConnected += HoloNETClient_OnConnected;
holoNETClient.OnDisconnected += HoloNETClient_OnDisconnected;
holoNETClient.OnError += HoloNETClient_OnError;

// Connect to Holochain conductor
await holoNETClient.ConnectAsync();
```

### Three Ways to Use HoloNET

#### 1. Direct HoloNETClient Calls
```csharp
// Make direct calls to Holochain Conductor
var result = await holoNETClient.CallZomeFunctionAsync(
    "test-instance", 
    "test_zome", 
    "create_test_entry", 
    new { content = "Hello Holochain!" }
);
```

#### 2. HoloNETEntryBaseClass (Recommended)
```csharp
public class MyEntry : HoloNETEntryBaseClass
{
    public string Content { get; set; }
    
    public MyEntry(HoloNETClient holoNETClient) : base(holoNETClient)
    {
        ZomeName = "test_zome";
        EntryType = "test_entry";
    }
}

// Usage
var entry = new MyEntry(holoNETClient);
entry.Content = "Hello World";
await entry.SaveAsync(); // Automatically handles CRUD operations
```

#### 3. HoloNETAuditEntryBaseClass (Advanced)
```csharp
public class MyAuditEntry : HoloNETAuditEntryBaseClass
{
    public string Content { get; set; }
    
    public MyAuditEntry(HoloNETClient holoNETClient) : base(holoNETClient)
    {
        ZomeName = "test_zome";
        EntryType = "test_entry";
    }
}

// Provides additional auditing and version control
var auditEntry = new MyAuditEntry(holoNETClient);
auditEntry.Content = "Versioned content";
await auditEntry.SaveAsync();
```

### HoloOASIS Integration

HoloOASIS uses HoloNET to implement OASIS storage and network providers:

```csharp
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;

// Initialize HoloOASIS
var holoOASIS = new HoloOASIS("ws://localhost:8888");

// Wire up events
holoOASIS.OnInitialized += HoloOASIS_OnInitialized;
holoOASIS.OnAvatarLoaded += HoloOASIS_OnAvatarLoaded;
holoOASIS.OnAvatarSaved += HoloOASIS_OnAvatarSaved;

// Initialize
await holoOASIS.InitializeAsync();
```

### Test Harness Usage

Use the HoloNET Test Harness to test functionality:

```csharp
using NextGenSoftware.Holochain.HoloNET.Client.TestHarness;

// Run specific tests
await HoloNETTestHarness.TestHoloNETClientAsync(TestToRun.WhoAmI);
await HoloNETTestHarness.TestHoloNETClientAsync(TestToRun.SaveLoadOASISEntry);
```

---

## OASIS APIs

The OASIS API provides RESTful endpoints for avatar management, data operations, and blockchain interactions.

### Base URL
- **Development**: `https://localhost:5002`
- **Staging**: `https://staging-api.oasisweb4.com`
- **Production**: `https://api.oasisweb4.com`

### Authentication Flow

#### 1. Register Avatar
```bash
curl -X POST "https://localhost:5002/api/avatar/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "SecurePassword123!",
    "firstName": "Test",
    "lastName": "User"
  }'
```

#### 2. Verify Email
```bash
curl -X GET "https://localhost:5002/api/avatar/verify-email?token=VERIFICATION_TOKEN"
```

#### 3. Authenticate
```bash
curl -X POST "https://localhost:5002/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "SecurePassword123!"
  }'
```

**Response includes JWT token for subsequent requests:**
```json
{
  "result": {
    "id": "avatar-id",
    "username": "testuser",
    "jwtToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh-token"
  }
}
```

### Core API Endpoints

#### Avatar Management
- `POST /api/avatar/register` - Register new avatar
- `POST /api/avatar/authenticate` - Login avatar
- `GET /api/avatar/verify-email` - Verify email address
- `GET /api/avatar/{id}` - Get avatar details
- `PUT /api/avatar/{id}` - Update avatar
- `DELETE /api/avatar/{id}` - Delete avatar

#### Data Operations
- `POST /api/data/save-holon` - Save data holon
- `GET /api/data/load-holon/{id}` - Load data holon
- `GET /api/data/load-holons-for-parent/{parentId}` - Load child holons
- `DELETE /api/data/delete-holon/{id}` - Delete holon

#### NFT Operations
- `POST /api/nft/mint-nft` - Mint new NFT
- `GET /api/nft/load-nft/{id}` - Load NFT details
- `GET /api/nft/load-nfts-for-avatar/{avatarId}` - Load avatar's NFTs

#### Provider Management
- `GET /api/provider/get-current-storage-provider` - Get current provider
- `POST /api/provider/activate-provider/{providerType}` - Activate provider
- `GET /api/provider/get-all-registered-providers` - List all providers

### Using Providers in API Calls

Most endpoints support provider-specific calls:

```bash
# Use specific provider for this request only
curl -X POST "https://localhost:5002/api/avatar/register/MongoDBOASIS/false" \
  -H "Content-Type: application/json" \
  -d '{"username": "testuser", "email": "test@example.com", "password": "password"}'

# Set provider globally for all future requests
curl -X POST "https://localhost:5002/api/avatar/register/MongoDBOASIS/true" \
  -H "Content-Type: application/json" \
  -d '{"username": "testuser", "email": "test@example.com", "password": "password"}'
```

### NFT Minting Example

Based on the NFT minting briefing, here's how to mint NFTs:

```bash
# First authenticate
curl -X POST "https://localhost:5002/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "your_username",
    "password": "your_password"
  }'

# Mint NFT using the returned JWT token
curl -X POST "https://localhost:5002/api/nft/mint-nft" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "OnChainProvider": "ArbitrumOASIS",
    "OffChainProvider": "MongoDBOASIS",
    "AvatarId": "your-avatar-id",
    "WalletAddress": "0xYourWalletAddress",
    "Title": "My Test NFT",
    "Description": "A test NFT for alpha testing",
    "ImageUrl": "https://example.com/image.png",
    "MetaData": {
      "testProperty": "testValue",
      "category": "test",
      "rarity": "common",
      "collection": "TestCollection",
      "creator": "Test Creator"
    }
  }'
```

---

## Provider Architecture

OASIS uses a modular provider architecture that allows you to extend functionality by creating custom providers.

### Available Providers

#### Storage Providers
- **MongoDBOASIS** - MongoDB document database
- **SQLLiteDBOASIS** - SQLite relational database
- **Neo4jOASIS** - Neo4j graph database
- **LocalFileOASIS** - Local file system storage

#### Blockchain Providers
- **EthereumOASIS** - Ethereum blockchain
- **ArbitrumOASIS** - Arbitrum layer 2
- **PolygonOASIS** - Polygon network
- **SolanaOASIS** - Solana blockchain
- **EOSIOOASIS** - EOSIO blockchain
- **TRONOASIS** - TRON blockchain

#### Network Providers
- **HoloOASIS** - Holochain network
- **IPFSOASIS** - IPFS decentralized storage
- **PinataOASIS** - Pinata IPFS service

### Creating Custom Providers

#### 1. Provider Template
Use the `ProviderNameOASIS` template as a starting point:

```csharp
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MyCustomOASIS
{
    public class MyCustomOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        public MyCustomOASIS(string customParams)
        {
            this.ProviderName = "MyCustomOASIS";
            this.ProviderDescription = "My Custom Provider";
            this.ProviderType = new EnumValue<ProviderType>(ProviderType.MyCustomOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageAndNetwork);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            // Initialize your provider here
            // Make connections, instantiate objects, etc.
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return new OASISResult<bool>(true);
        }

        // Implement required interface methods
        public override OASISResult<IAvatar> LoadAvatarAsync(string username, string password)
        {
            // Your custom implementation
            return new OASISResult<IAvatar>();
        }

        // ... implement other required methods
    }
}
```

#### 2. Provider Registration
Add your provider to the `OASISDNA.json` configuration:

```json
{
  "OASIS": {
    "Providers": {
      "MyCustomOASIS": {
        "IsEnabled": true,
        "Priority": 1,
        "CustomParams": "your-custom-params"
      }
    }
  }
}
```

#### 3. Provider Interfaces
Implement the interfaces your provider needs:

- `IOASISStorageProvider` - Basic storage operations
- `IOASISNETProvider` - Network operations
- `IOASISBlockchainStorageProvider` - Blockchain storage
- `IOASISSmartContractProvider` - Smart contract operations
- `IOASISNFTProvider` - NFT operations

### Provider Configuration

Configure providers in `OASISDNA.json`:

```json
{
  "OASIS": {
    "Providers": {
      "MongoDBOASIS": {
        "IsEnabled": true,
        "Priority": 1,
        "CustomParams": "mongodb://localhost:27017"
      },
      "ArbitrumOASIS": {
        "IsEnabled": true,
        "Priority": 2,
        "CustomParams": "https://sepolia-rollup.arbitrum.io/rpc"
      }
    },
    "AutoFailOver": {
      "IsEnabled": true,
      "Providers": ["MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS"]
    },
    "AutoReplication": {
      "IsEnabled": true,
      "Providers": ["MongoDBOASIS", "HoloOASIS"]
    }
  }
}
```

---

## Avatar Authentication

OASIS uses a comprehensive avatar authentication system with JWT tokens and refresh tokens.

### Authentication Flow

#### 1. Registration Process
```csharp
// Register new avatar
var registerRequest = new RegisterRequest
{
    Username = "testuser",
    Email = "test@example.com",
    Password = "SecurePassword123!",
    FirstName = "Test",
    LastName = "User"
};

var result = await Program.AvatarManager.RegisterAsync(registerRequest);
```

#### 2. Email Verification
```csharp
// Verify email with token from registration email
var verifyResult = await Program.AvatarManager.VerifyEmailAsync(verificationToken);
```

#### 3. Authentication
```csharp
// Authenticate avatar
var authResult = await Program.AvatarManager.AuthenticateAsync(
    username: "testuser",
    password: "SecurePassword123!",
    ipAddress: "127.0.0.1"
);

if (!authResult.IsError)
{
    var avatar = authResult.Result;
    var jwtToken = avatar.JwtToken;
    var refreshToken = avatar.RefreshToken;
}
```

### JWT Token Usage

Include JWT token in API requests:

```bash
curl -X GET "https://localhost:5002/api/avatar/current" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Refresh Token

When JWT expires, use refresh token:

```bash
curl -X POST "https://localhost:5002/api/avatar/refresh-token" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN"
  }'
```

### Avatar Properties

Avatars contain comprehensive identity information:

```csharp
public interface IAvatar
{
    Guid Id { get; set; }
    string Username { get; set; }
    string Email { get; set; }
    string FirstName { get; set; }
    string LastName { get; set; }
    string JwtToken { get; set; }
    string RefreshToken { get; set; }
    List<IWallet> Wallets { get; set; }
    List<INFT> NFTs { get; set; }
    int Karma { get; set; }
    AvatarType AvatarType { get; set; }
    bool IsBeamedIn { get; set; }
    DateTime LastBeamedIn { get; set; }
}
```

---

## Test Harnesses

Test harnesses provide working examples and testing capabilities for all OASIS components.

### Available Test Harnesses

#### 1. HoloNET Test Harness
Location: `holochain-client-csharp/NextGenSoftware.Holochain.HoloNET.Client.TestHarness/`

**Available Tests:**
- `WhoAmI` - Test basic connection
- `Numbers` - Test number operations
- `Signal` - Test signaling
- `SaveLoadOASISEntry` - Test OASIS entry operations
- `LoadTestNumbers` - Performance testing
- `AdminInstallApp` - Admin operations

**Usage:**
```csharp
using NextGenSoftware.Holochain.HoloNET.Client.TestHarness;

// Run specific test
await HoloNETTestHarness.TestHoloNETClientAsync(TestToRun.WhoAmI);

// Run OASIS-specific test
await HoloNETTestHarness.TestHoloNETClientAsync(TestToRun.SaveLoadOASISEntry);
```

#### 2. ONODE Core Test Harness
Location: `NextGenSoftware.OASIS.API.ONODE.Core.TestHarness/`

**Features:**
- NFT minting tests
- Avatar operations
- Provider testing
- Data operations

**Usage:**
```bash
cd NextGenSoftware.OASIS.API.ONODE.Core.TestHarness
dotnet run
```

#### 3. STAR Test Harness
Location: `STAR ODK/NextGenSoftware.OASIS.STAR.TestHarness/`

**Features:**
- Celestial body creation
- STAR engine testing
- OAPP generation
- Provider integration

**Usage:**
```bash
cd "STAR ODK/NextGenSoftware.OASIS.STAR.TestHarness"
dotnet run
```

#### 4. Provider-Specific Test Harnesses

Each provider has its own test harness:
- `NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.TestHarness/`
- `NextGenSoftware.OASIS.API.Providers.EthereumOASIS.TestHarness/`
- `NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness/`

### Running Tests

#### Command Line
```bash
# Run specific test harness
cd NextGenSoftware.OASIS.API.ONODE.Core.TestHarness
dotnet run

# Run with specific parameters
dotnet run -- --test-type nft-minting --provider ArbitrumOASIS
```

#### Programmatic
```csharp
// Create test harness instance
var testHarness = new ONODECoreTestHarness();

// Run specific tests
await testHarness.TestNFTMinting();
await testHarness.TestAvatarOperations();
await testHarness.TestProviderSwitching();
```

### Test Configuration

Configure test harnesses in their respective `OASIS_DNA.json` files:

```json
{
  "OASIS": {
    "TestHarness": {
      "IsEnabled": true,
      "TestProviders": ["MongoDBOASIS", "ArbitrumOASIS"],
      "TestData": {
        "AvatarCount": 10,
        "NFTCount": 5,
        "HolonCount": 20
      }
    }
  }
}
```

---

## STAR System

STAR (Synergiser Transformer/Translator Aggregator Resolver) is the heart of OASIS, providing a unified ontology and templating engine.

### STAR Architecture

STAR models the universe as a hierarchical structure:
- **Omiverse** (root)
  - **Multiverses**
    - **Universes**
      - **Galaxies**
        - **Solar Systems**
          - **Stars/Planets/Moons**

### Celestial Bodies

#### Stars
- Center of solar systems
- Can create moons and planets
- Basic level celestial body

#### SuperStars
- Center of galaxies
- Can create solar systems, stars, planets, moons
- Used for galaxy navigation

#### GrandSuperStars
- Center of universes
- Can create galaxies, super stars, stars, planets, moons
- Access to dimensions 1-7

#### GreatGrandSuperStar
- Center of the omiverse
- Can create multiverses and all celestial bodies
- Access to all dimensions (1-12)

### Using STAR

#### 1. Initialize STAR
```csharp
using NextGenSoftware.OASIS.STAR;

// Initialize STAR engine
var star = new STAR();
await star.InitializeAsync();
```

#### 2. Create Celestial Bodies
```csharp
// Create a planet
var planet = new Planet
{
    Name = "My Planet",
    Description = "A test planet",
    ParentStarId = starId
};

await star.CreateCelestialBodyAsync(planet);
```

#### 3. Generate OAPPs
```csharp
// Generate OAPP from template
var oapp = await star.GenerateOAPPAsync(
    templateType: OAPPType.Web,
    celestialBodyId: planetId,
    configuration: oappConfig
);
```

### STAR Templates

STAR can generate code for multiple platforms:

#### Web Applications
```csharp
var webOAPP = await star.GenerateWebOAPPAsync(
    template: "basic-web-app",
    features: ["authentication", "data-crud", "nft-support"]
);
```

#### Unity Games
```csharp
var unityOAPP = await star.GenerateUnityOAPPAsync(
    template: "3d-world",
    features: ["avatar-system", "multiplayer", "blockchain-integration"]
);
```

#### Mobile Apps
```csharp
var mobileOAPP = await star.GenerateMobileOAPPAsync(
    template: "cross-platform",
    features: ["wallet-integration", "nft-gallery", "social-features"]
);
```

### COSMIC ORM

STAR uses COSMIC (Computer Object-Orientated Super-Synergistic Machine Interface Code) as its ORM:

```csharp
// COSMIC provides unified data access
var holons = await cosmic.LoadHolonsAsync<MyHolon>(
    provider: ProviderType.MongoDBOASIS,
    parentId: parentHolonId
);

// Automatic provider switching
var result = await cosmic.SaveHolonAsync(
    holon: myHolon,
    autoFailOver: true,
    autoReplication: true
);
```

### STAR Test Harness

Test STAR functionality:

```csharp
using NextGenSoftware.OASIS.STAR.TestHarness;

// Run STAR tests
var testHarness = new STARTestHarness();
await testHarness.TestCelestialBodyCreation();
await testHarness.TestOAPPGeneration();
await testHarness.TestProviderIntegration();
```

---

## Additional Resources

### Documentation Links
- [OASIS GitHub Repository](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK)
- [HoloNET Documentation](https://github.com/holochain-open-dev/holochain-client-csharp)
- [OASIS API Postman Collection](https://oasisweb4.one/postman/OASIS_API.postman_collection.json)

### Community
- [Telegram General Chat](https://t.me/ourworldthegamechat)
- [Discord Server](https://discord.gg/q9gMKU6)
- [OASIS API Hackalong](https://t.me/oasisapihackalong)

### Support
- Email: ourworld@nextgensoftware.co.uk
- GitHub Issues: [Create an issue](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/issues)

### Development Environment Setup

#### Postman Collections
Download and import these Postman collections:
- [OASIS API Collection](https://oasisweb4.one/postman/OASIS_API.postman_collection.json)
- [DEV Environment](https://oasisweb4.one/postman/OASIS_API_DEV.postman_environment.json)
- [STAGING Environment](https://oasisweb4.one/postman/OASIS_API_STAGING.postman_environment.json)
- [LIVE Environment](https://oasisweb4.one/postman/OASIS_API_LIVE.postman_environment.json)

### Troubleshooting

#### Common Issues

1. **Provider Connection Issues**
   - Check provider configuration in `OASISDNA.json`
   - Verify network connectivity
   - Check provider-specific logs

2. **Authentication Failures**
   - Verify JWT token expiration
   - Check refresh token validity
   - Ensure proper authorization headers

3. **NFT Minting Issues**
   - Verify wallet balance for gas fees
   - Check contract deployment status
   - Review transaction parameters

4. **Test Harness Errors**
   - Ensure all dependencies are installed
   - Check configuration files
   - Verify provider availability

### Best Practices

1. **Always use test harnesses** for initial testing
2. **Start with simple operations** before complex workflows
3. **Test with multiple providers** to ensure compatibility
4. **Use proper error handling** in your applications
5. **Keep JWT tokens secure** and implement proper refresh logic
6. **Monitor provider performance** and implement failover strategies

---

## Conclusion

This documentation provides a comprehensive guide to using OASIS for alpha testing. Start with the test harnesses to familiarize yourself with the system, then explore the APIs and provider architecture. The STAR system offers advanced templating and code generation capabilities for building sophisticated applications.

For additional support or questions, please reach out through the community channels or create an issue on GitHub.

**Happy Testing! 🚀**
