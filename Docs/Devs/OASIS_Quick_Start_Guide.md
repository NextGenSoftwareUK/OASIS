# OASIS Quick Start Guide for Alpha Testers

## 🚀 Getting Started in 5 Minutes

### Step 1: Setup Environment
```bash
# Clone the repository
git clone https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK.git
cd Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

### Step 2: Run Your First Test
```bash
# Navigate to ONODE Core Test Harness
cd NextGenSoftware.OASIS.API.ONODE.Core.TestHarness

# Run the test harness
dotnet run
```

### Step 3: Test API Endpoints
```bash
# Start the WebAPI (in separate terminal)
cd NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run

# Test authentication
curl -X POST "https://localhost:5002/api/avatar/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com", 
    "password": "TestPassword123!",
    "firstName": "Test",
    "lastName": "User"
  }'
```

---

## 🧪 Test Harnesses Quick Reference

### Available Test Harnesses

| Test Harness | Location | Purpose |
|-------------|----------|---------|
| **ONODE Core** | `NextGenSoftware.OASIS.API.ONODE.Core.TestHarness/` | NFT minting, avatar ops, data operations |
| **HoloNET** | `holochain-client-csharp/NextGenSoftware.Holochain.HoloNET.Client.TestHarness/` | Holochain connectivity testing |
| **STAR** | `STAR ODK/NextGenSoftware.OASIS.STAR.TestHarness/` | Celestial body creation, OAPP generation |
| **Arbitrum** | `NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.TestHarness/` | Arbitrum blockchain operations |
| **Ethereum** | `NextGenSoftware.OASIS.API.Providers.EthereumOASIS.TestHarness/` | Ethereum blockchain operations |

### Running Tests
```bash
# Run any test harness
cd [TestHarnessFolder]
dotnet run

# Run with specific parameters
dotnet run -- --test-type nft-minting --provider ArbitrumOASIS
```

---

## 🔌 Provider Quick Reference

### Available Providers

#### Storage Providers
- **MongoDBOASIS** - MongoDB document database
- **SQLLiteDBOASIS** - SQLite relational database  
- **Neo4jOASIS** - Neo4j graph database
- **LocalFileOASIS** - Local file system

#### Blockchain Providers
- **EthereumOASIS** - Ethereum mainnet/testnet
- **ArbitrumOASIS** - Arbitrum layer 2
- **PolygonOASIS** - Polygon network
- **SolanaOASIS** - Solana blockchain
- **EOSIOOASIS** - EOSIO blockchain
- **TRONOASIS** - TRON blockchain

#### Network Providers
- **HoloOASIS** - Holochain network
- **IPFSOASIS** - IPFS decentralized storage
- **PinataOASIS** - Pinata IPFS service

### Provider Usage
```csharp
// Use specific provider for API call
var result = await Program.AvatarManager.LoadAvatarAsync(
    avatarId, 
    ProviderType.MongoDBOASIS
);

// Set provider globally
ProviderManager.Instance.SetCurrentStorageProvider(ProviderType.MongoDBOASIS);
```

---

## 🔐 Authentication Quick Reference

### Complete Authentication Flow

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

#### 4. Use JWT Token
```bash
curl -X GET "https://localhost:5002/api/avatar/current" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## 🎨 NFT Minting Quick Reference

### Basic NFT Minting
```bash
# Authenticate first (get JWT token)
curl -X POST "https://localhost:5002/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "your_username", "password": "your_password"}'

# Mint NFT using JWT token
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
      "testProperty": "testValue"
    }
  }'
```

### NFT Operations
```bash
# Load NFT details
curl -X GET "https://localhost:5002/api/nft/load-nft/{nftId}" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Load all NFTs for avatar
curl -X GET "https://localhost:5002/api/nft/load-nfts-for-avatar/{avatarId}" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## 🌟 STAR Quick Reference

### Basic STAR Usage
```csharp
using NextGenSoftware.OASIS.STAR;

// Initialize STAR
var star = new STAR();
await star.InitializeAsync();

// Create a planet
var planet = new Planet
{
    Name = "Test Planet",
    Description = "A test planet for alpha testing"
};

await star.CreateCelestialBodyAsync(planet);
```

### Generate OAPP
```csharp
// Generate web application
var webOAPP = await star.GenerateWebOAPPAsync(
    template: "basic-web-app",
    features: ["authentication", "data-crud"]
);

// Generate Unity game
var unityOAPP = await star.GenerateUnityOAPPAsync(
    template: "3d-world", 
    features: ["avatar-system", "multiplayer"]
);
```

---

## 🔧 HoloNET Quick Reference

### Basic HoloNET Setup
```csharp
using NextGenSoftware.Holochain.HoloNET.Client;

// Initialize client
var holoNETClient = new HoloNETClient("ws://localhost:8888");

// Connect
await holoNETClient.ConnectAsync();

// Make zome call
var result = await holoNETClient.CallZomeFunctionAsync(
    "test-instance",
    "test_zome", 
    "create_test_entry",
    new { content = "Hello Holochain!" }
);
```

### Using HoloNET Entry Classes
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
await entry.SaveAsync();
```

---

## 📊 API Endpoints Quick Reference

### Core Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/avatar/register` | POST | Register new avatar |
| `/api/avatar/authenticate` | POST | Login avatar |
| `/api/avatar/verify-email` | GET | Verify email address |
| `/api/avatar/current` | GET | Get current avatar |
| `/api/nft/mint-nft` | POST | Mint new NFT |
| `/api/nft/load-nft/{id}` | GET | Load NFT details |
| `/api/data/save-holon` | POST | Save data holon |
| `/api/data/load-holon/{id}` | GET | Load data holon |
| `/api/provider/get-current-storage-provider` | GET | Get current provider |

### Provider-Specific Endpoints
Most endpoints support provider overrides:
- `/api/avatar/register/{providerType}/{setGlobally}`
- `/api/nft/mint-nft/{providerType}/{setGlobally}`
- `/api/data/save-holon/{providerType}/{setGlobally}`

---

## 🐛 Troubleshooting Quick Reference

### Common Issues & Solutions

#### 1. Provider Connection Issues
```bash
# Check provider status
curl -X GET "https://localhost:5002/api/provider/get-current-storage-provider" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Activate specific provider
curl -X POST "https://localhost:5002/api/provider/activate-provider/MongoDBOASIS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### 2. Authentication Issues
- Verify JWT token expiration
- Check refresh token validity
- Ensure proper authorization headers

#### 3. NFT Minting Issues
- Verify wallet balance for gas fees
- Check contract deployment status
- Review transaction parameters

#### 4. Test Harness Errors
- Ensure all dependencies installed
- Check configuration files
- Verify provider availability

---

## 📚 Additional Resources

### Documentation
- [Full OASIS Documentation](./OASIS_Alpha_Tester_Documentation.md)
- [HoloNET Documentation](https://github.com/holochain-open-dev/holochain-client-csharp)
- [OASIS GitHub Repository](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK)

### Postman Collections
- [OASIS API Collection](https://oasisweb4.one/postman/OASIS_API.postman_collection.json)
- [DEV Environment](https://oasisweb4.one/postman/OASIS_API_DEV.postman_environment.json)

### Community
- [Telegram Chat](https://t.me/ourworldthegamechat)
- [Discord Server](https://discord.gg/q9gMKU6)
- [OASIS API Hackalong](https://t.me/oasisapihackalong)

### Support
- Email: ourworld@nextgensoftware.co.uk
- [GitHub Issues](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/issues)

---

## 🎯 Testing Checklist

### Initial Setup
- [ ] Environment configured
- [ ] Dependencies installed
- [ ] Solution built successfully
- [ ] Test harnesses running

### Core Functionality
- [ ] Avatar registration working
- [ ] Email verification working
- [ ] Authentication working
- [ ] JWT token generation working

### Provider Testing
- [ ] MongoDB provider working
- [ ] Blockchain provider working
- [ ] Provider switching working
- [ ] Auto-failover working

### Advanced Features
- [ ] NFT minting working
- [ ] STAR system working
- [ ] HoloNET integration working
- [ ] OAPP generation working

### API Testing
- [ ] All endpoints accessible
- [ ] Authentication required endpoints working
- [ ] Provider-specific endpoints working
- [ ] Error handling working

---

**Happy Testing! 🚀**

*This quick start guide provides the essential information needed to begin testing OASIS. For detailed information, refer to the full documentation.*
