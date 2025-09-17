# OASIS Provider Development Guide for Alpha Testers

## Overview

OASIS uses a modular provider architecture that allows you to extend functionality by creating custom providers. This guide covers how to build, test, and deploy custom OASIS providers.

---

## Provider Architecture

### Provider Categories

#### Storage Providers (`IOASISStorageProvider`)
Handle data persistence and retrieval:
- `MongoDBOASIS` - MongoDB document database
- `SQLLiteDBOASIS` - SQLite relational database
- `Neo4jOASIS` - Neo4j graph database
- `LocalFileOASIS` - Local file system storage

#### Blockchain Providers (`IOASISBlockchainStorageProvider`)
Handle blockchain operations:
- `EthereumOASIS` - Ethereum blockchain
- `ArbitrumOASIS` - Arbitrum layer 2
- `PolygonOASIS` - Polygon network
- `SolanaOASIS` - Solana blockchain

#### Network Providers (`IOASISNETProvider`)
Handle network operations:
- `HoloOASIS` - Holochain network
- `IPFSOASIS` - IPFS decentralized storage
- `PinataOASIS` - Pinata IPFS service

#### Smart Contract Providers (`IOASISSmartContractProvider`)
Handle smart contract operations:
- Contract deployment
- Function calls
- Event monitoring
- Gas management

#### NFT Providers (`IOASISNFTProvider`)
Handle NFT operations:
- NFT minting
- Metadata management
- Transfer operations
- Collection management

---

## Creating a Custom Provider

### Step 1: Project Structure

Create a new provider project following the naming convention:
```
NextGenSoftware.OASIS.API.Providers.YourProviderOASIS/
├── YourProviderOASIS.cs
├── YourProviderOASIS.TestHarness/
│   ├── Program.cs
│   └── OASIS_DNA.json
├── DNA.json
└── README.md
```

### Step 2: Provider Template

Use the `ProviderNameOASIS` template as a starting point:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.YourProviderOASIS
{
    public class YourProviderOASIS : OASISStorageProviderBase, 
        IOASISStorageProvider, 
        IOASISNETProvider,
        IOASISBlockchainStorageProvider,
        IOASISSmartContractProvider,
        IOASISNFTProvider
    {
        // Custom parameters for your provider
        private string _connectionString;
        private string _apiKey;
        private bool _isTestMode;

        public YourProviderOASIS(string customParams)
        {
            this.ProviderName = "YourProviderOASIS";
            this.ProviderDescription = "Your Custom Provider Description";
            this.ProviderType = new EnumValue<ProviderType>(ProviderType.YourProviderOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageAndNetwork);
            
            // Parse custom parameters
            ParseCustomParams(customParams);
        }

        private void ParseCustomParams(string customParams)
        {
            // Parse your custom parameters
            // Example: "connectionString=value1;apiKey=value2;testMode=true"
            var params = customParams.Split(';');
            foreach (var param in params)
            {
                var keyValue = param.Split('=');
                if (keyValue.Length == 2)
                {
                    switch (keyValue[0].ToLower())
                    {
                        case "connectionstring":
                            _connectionString = keyValue[1];
                            break;
                        case "apikey":
                            _apiKey = keyValue[1];
                            break;
                        case "testmode":
                            _isTestMode = bool.Parse(keyValue[1]);
                            break;
                    }
                }
            }
        }

        #region IOASISStorageProvider Implementation

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            try
            {
                // Initialize your provider here
                // Make connections, instantiate objects, etc.
                
                // Example initialization
                await InitializeConnection();
                await ValidateConfiguration();
                
                IsProviderActivated = true;
                return new OASISResult<bool>(true) { Message = "Provider activated successfully" };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>(false) 
                { 
                    IsError = true, 
                    Message = $"Failed to activate provider: {ex.Message}",
                    DetailedMessage = ex.ToString()
                };
            }
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            try
            {
                // Cleanup resources
                CleanupConnections();
                IsProviderActivated = false;
                return new OASISResult<bool>(true) { Message = "Provider deactivated successfully" };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>(false) 
                { 
                    IsError = true, 
                    Message = $"Failed to deactivate provider: {ex.Message}" 
                };
            }
        }

        // Implement required storage methods
        public override OASISResult<IAvatar> LoadAvatarAsync(string username, string password)
        {
            // Your implementation here
            return new OASISResult<IAvatar>();
        }

        public override OASISResult<IAvatar> LoadAvatarAsync(Guid id)
        {
            // Your implementation here
            return new OASISResult<IAvatar>();
        }

        public override OASISResult<IAvatar> SaveAvatarAsync(IAvatar avatar)
        {
            // Your implementation here
            return new OASISResult<IAvatar>();
        }

        // ... implement other required methods

        #endregion

        #region IOASISNFTProvider Implementation

        public OASISResult<INFTTransactionRespone> MintNFTAsync(INFTTransactionRequest request)
        {
            try
            {
                // Your NFT minting implementation
                return new OASISResult<INFTTransactionRespone>();
            }
            catch (Exception ex)
            {
                return new OASISResult<INFTTransactionRespone>() 
                { 
                    IsError = true, 
                    Message = $"NFT minting failed: {ex.Message}" 
                };
            }
        }

        public OASISResult<INFT> LoadNFTAsync(string hash)
        {
            // Your NFT loading implementation
            return new OASISResult<INFT>();
        }

        // ... implement other NFT methods

        #endregion

        #region Helper Methods

        private async Task InitializeConnection()
        {
            // Initialize your connection here
            // Example: database connection, API client, etc.
        }

        private async Task ValidateConfiguration()
        {
            // Validate your configuration
            // Check required parameters, connectivity, etc.
        }

        private void CleanupConnections()
        {
            // Cleanup resources
            // Close connections, dispose objects, etc.
        }

        #endregion
    }
}
```

### Step 3: Provider Registration

Add your provider to the `ProviderType` enum:

```csharp
// In NextGenSoftware.OASIS.API.Core.Enums.ProviderType
public enum ProviderType
{
    // ... existing providers
    YourProviderOASIS = 999  // Use appropriate number
}
```

### Step 4: Configuration

Create a `DNA.json` file for your provider:

```json
{
  "ProviderName": "YourProviderOASIS",
  "ProviderDescription": "Your Custom Provider",
  "ProviderType": "YourProviderOASIS",
  "ProviderCategory": "StorageAndNetwork",
  "IsEnabled": true,
  "Priority": 1,
        "CustomParams": "connectionString=your-test-connection-string;apiKey=your-test-api-key;testMode=true",
  "Dependencies": [
    "NextGenSoftware.OASIS.API.Core",
    "NextGenSoftware.OASIS.Common"
  ]
}
```

### Step 5: Test Harness

Create a test harness for your provider:

```csharp
using System;
using NextGenSoftware.OASIS.API.Providers.YourProviderOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.YourProviderOASIS.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("YOUR PROVIDER OASIS TEST HARNESS V1.0");
            Console.WriteLine("");

            // Initialize provider
            var customParams = "connectionString=test-connection;apiKey=test-key;testMode=true";
            var provider = new YourProviderOASIS(customParams);

            // Test provider activation
            Console.WriteLine("Testing provider activation...");
            var activationResult = provider.ActivateProvider();
            
            if (activationResult.IsError)
            {
                Console.WriteLine($"❌ Activation failed: {activationResult.Message}");
                return;
            }
            
            Console.WriteLine("✅ Provider activated successfully");

            // Test basic operations
            TestBasicOperations(provider);

            // Test NFT operations
            TestNFTOperations(provider);

            // Test cleanup
            provider.DeActivateProvider();
            Console.WriteLine("✅ Provider deactivated successfully");
        }

        static void TestBasicOperations(YourProviderOASIS provider)
        {
            Console.WriteLine("\n--- Testing Basic Operations ---");
            
            // Test avatar operations
            // Test data operations
            // Test provider-specific operations
        }

        static void TestNFTOperations(YourProviderOASIS provider)
        {
            Console.WriteLine("\n--- Testing NFT Operations ---");
            
            // Test NFT minting
            // Test NFT loading
            // Test NFT metadata
        }
    }
}
```

---

## Provider Interfaces

### Core Interfaces

#### IOASISStorageProvider
```csharp
public interface IOASISStorageProvider : IOASISProvider
{
    // Avatar operations
    OASISResult<IAvatar> LoadAvatar(string username, string password);
    OASISResult<IAvatar> LoadAvatar(Guid id);
    OASISResult<IAvatar> SaveAvatar(IAvatar avatar);
    OASISResult<bool> DeleteAvatar(Guid id);

    // Holon operations
    OASISResult<IHolon> LoadHolon(Guid id);
    OASISResult<IHolon> SaveHolon(IHolon holon);
    OASISResult<bool> DeleteHolon(Guid id);
    OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid parentId);
}
```

#### IOASISNFTProvider
```csharp
public interface IOASISNFTProvider : IOASISProvider
{
    OASISResult<INFTTransactionRespone> MintNFT(INFTTransactionRequest request);
    OASISResult<INFT> LoadNFT(string hash);
    OASISResult<IEnumerable<INFT>> LoadNFTsForAvatar(Guid avatarId);
    OASISResult<bool> DeleteNFT(string hash);
}
```

#### IOASISSmartContractProvider
```csharp
public interface IOASISSmartContractProvider : IOASISProvider
{
    OASISResult<string> DeploySmartContract(ISmartContractRequest request);
    OASISResult<ISmartContractTransactionRespone> CallSmartContractFunction(ISmartContractFunctionRequest request);
    OASISResult<IEnumerable<ISmartContractEvent>> GetSmartContractEvents(ISmartContractEventRequest request);
}
```

### Optional Interfaces

#### IOASISNETProvider
For network operations:
```csharp
public interface IOASISNETProvider : IOASISProvider
{
    OASISResult<string> SendMessage(string message, string targetAddress);
    OASISResult<IEnumerable<string>> GetMessages();
    OASISResult<bool> ConnectToNetwork(string networkAddress);
}
```

#### IOASISSearchProvider
For search functionality:
```csharp
public interface IOASISSearchProvider : IOASISProvider
{
    OASISResult<IEnumerable<ISearchResult>> Search(ISearchRequest request);
    OASISResult<IEnumerable<ISearchResult>> SearchByQuery(string query);
}
```

---

## Provider Configuration

### OASIS_DNA.json Integration

Add your provider to the main `OASIS_DNA.json`:

```json
{
  "OASIS": {
    "Providers": {
      "YourProviderOASIS": {
        "IsEnabled": true,
        "Priority": 1,
        "CustomParams": "connectionString=your-test-connection;apiKey=your-test-key",
        "AutoFailOver": {
          "IsEnabled": true,
          "FailOverTo": ["MongoDBOASIS", "SQLLiteDBOASIS"]
        },
        "AutoReplication": {
          "IsEnabled": true,
          "ReplicateTo": ["MongoDBOASIS"]
        }
      }
    },
    "AutoFailOver": {
      "IsEnabled": true,
      "Providers": ["YourProviderOASIS", "MongoDBOASIS", "SQLLiteDBOASIS"]
    }
  }
}
```

### Environment-Specific Configuration

#### Development
```json
{
  "YourProviderOASIS": {
        "CustomParams": "connectionString=dev-test-connection;apiKey=dev-test-key;testMode=true"
  }
}
```

#### Production
```json
{
  "YourProviderOASIS": {
        "CustomParams": "connectionString=prod-test-connection;apiKey=prod-test-key;testMode=false"
  }
}
```

---

## Testing Your Provider

### Unit Testing

Create unit tests for your provider:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.YourProviderOASIS;

[TestClass]
public class YourProviderOASISTests
{
    private YourProviderOASIS _provider;

    [TestInitialize]
    public void Setup()
    {
        var customParams = "connectionString=test-connection;apiKey=test-key;testMode=true";
        _provider = new YourProviderOASIS(customParams);
    }

    [TestMethod]
    public async Task ActivateProvider_ShouldSucceed()
    {
        var result = await _provider.ActivateProviderAsync();
        
        Assert.IsFalse(result.IsError);
        Assert.IsTrue(result.Result);
    }

    [TestMethod]
    public async Task LoadAvatar_WithValidId_ShouldReturnAvatar()
    {
        await _provider.ActivateProviderAsync();
        
        var result = await _provider.LoadAvatarAsync(Guid.NewGuid());
        
        // Assert based on your implementation
    }

    [TestCleanup]
    public void Cleanup()
    {
        _provider?.DeActivateProvider();
    }
}
```

### Integration Testing

Test your provider with the OASIS system:

```csharp
[TestClass]
public class YourProviderOASISIntegrationTests
{
    [TestMethod]
    public async Task ProviderIntegration_ShouldWorkWithOASIS()
    {
        // Initialize OASIS with your provider
        var oasisManager = new OASISManager();
        await oasisManager.InitializeAsync();
        
        // Set your provider as current
        ProviderManager.Instance.SetCurrentStorageProvider(ProviderType.YourProviderOASIS);
        
        // Test OASIS operations
        var avatarManager = new AvatarManager();
        var result = await avatarManager.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "TestPassword123!"
        });
        
        Assert.IsFalse(result.IsError);
    }
}
```

### Performance Testing

Test provider performance:

```csharp
[TestClass]
public class YourProviderOASISPerformanceTests
{
    [TestMethod]
    public async Task LoadAvatar_PerformanceTest()
    {
        var provider = new YourProviderOASIS("test-params");
        await provider.ActivateProviderAsync();
        
        var stopwatch = Stopwatch.StartNew();
        
        // Perform multiple operations
        for (int i = 0; i < 100; i++)
        {
            await provider.LoadAvatarAsync(Guid.NewGuid());
        }
        
        stopwatch.Stop();
        
        // Assert performance requirements
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000); // 5 seconds max
    }
}
```

---

## Provider Deployment

### Build Configuration

Configure your provider project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>NextGenSoftware.OASIS.API.Providers.YourProviderOASIS</AssemblyName>
    <RootNamespace>NextGenSoftware.OASIS.API.Providers.YourProviderOASIS</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NextGenSoftware.OASIS.API.Core" Version="4.0.0" />
    <PackageReference Include="NextGenSoftware.OASIS.Common" Version="4.0.0" />
    <!-- Add your provider-specific dependencies -->
  </ItemGroup>
</Project>
```

### Packaging

Create a NuGet package for your provider:

```xml
<PropertyGroup>
  <PackageId>NextGenSoftware.OASIS.API.Providers.YourProviderOASIS</PackageId>
  <Version>1.0.0</Version>
  <Authors>Your Name</Authors>
  <Description>Your Custom OASIS Provider</Description>
  <PackageTags>OASIS;Provider;YourProvider</PackageTags>
</PropertyGroup>
```

### Deployment Steps

1. **Build your provider**:
   ```bash
   dotnet build --configuration Release
   ```

2. **Run tests**:
   ```bash
   dotnet test
   ```

3. **Package**:
   ```bash
   dotnet pack --configuration Release
   ```

4. **Deploy**:
   ```bash
   dotnet nuget push *.nupkg --source https://api.nuget.org/v3/index.json
   ```

---

## Best Practices

### Error Handling
```csharp
public override OASISResult<IAvatar> LoadAvatarAsync(Guid id)
{
    try
    {
        // Your implementation
        return new OASISResult<IAvatar>(avatar);
    }
    catch (Exception ex)
    {
        return new OASISResult<IAvatar>()
        {
            IsError = true,
            Message = $"Failed to load avatar: {ex.Message}",
            DetailedMessage = ex.ToString()
        };
    }
}
```

### Logging
```csharp
using NextGenSoftware.Utilities;

public class YourProviderOASIS : OASISStorageProviderBase
{
    private readonly ILogger _logger;

    public YourProviderOASIS(string customParams) : base()
    {
        _logger = new Logger("YourProviderOASIS");
    }

    public override async Task<OASISResult<bool>> ActivateProviderAsync()
    {
        _logger.LogInfo("Activating provider...");
        
        try
        {
            // Implementation
            _logger.LogInfo("Provider activated successfully");
            return new OASISResult<bool>(true);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Provider activation failed: {ex.Message}", ex);
            return new OASISResult<bool>(false) { IsError = true, Message = ex.Message };
        }
    }
}
```

### Configuration Validation
```csharp
private void ValidateConfiguration()
{
    if (string.IsNullOrEmpty(_connectionString))
        throw new ArgumentException("Connection string is required");
    
    if (string.IsNullOrEmpty(_apiKey))
        throw new ArgumentException("API key is required");
    
    // Additional validation
}
```

### Resource Management
```csharp
public class YourProviderOASIS : OASISStorageProviderBase, IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Cleanup resources
            CleanupConnections();
            _disposed = true;
        }
    }

    ~YourProviderOASIS()
    {
        Dispose(false);
    }
}
```

---

## Provider Examples

### Database Provider Example

```csharp
public class CustomDatabaseOASIS : OASISStorageProviderBase, IOASISStorageProvider
{
    private IDbConnection _connection;

    public override async Task<OASISResult<bool>> ActivateProviderAsync()
    {
        try
        {
            _connection = new SqlConnection(_connectionString);
            await _connection.OpenAsync();
            
            // Create tables if they don't exist
            await CreateTablesAsync();
            
            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }
        catch (Exception ex)
        {
            return new OASISResult<bool>(false) { IsError = true, Message = ex.Message };
        }
    }

    public override OASISResult<IAvatar> LoadAvatarAsync(Guid id)
    {
        try
        {
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM Avatars WHERE Id = @id";
            command.Parameters.Add(new SqlParameter("@id", id));
            
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var avatar = new Avatar
                {
                    Id = reader.GetGuid("Id"),
                    Username = reader.GetString("Username"),
                    Email = reader.GetString("Email")
                    // Map other properties
                };
                
                return new OASISResult<IAvatar>(avatar);
            }
            
            return new OASISResult<IAvatar>() { IsError = true, Message = "Avatar not found" };
        }
        catch (Exception ex)
        {
            return new OASISResult<IAvatar>() { IsError = true, Message = ex.Message };
        }
    }
}
```

### Blockchain Provider Example

```csharp
public class CustomBlockchainOASIS : OASISStorageProviderBase, IOASISBlockchainStorageProvider
{
    private Web3 _web3;
    private string _contractAddress;

    public override async Task<OASISResult<bool>> ActivateProviderAsync()
    {
        try
        {
            _web3 = new Web3(_rpcUrl);
            
            // Verify connection
            var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            
            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }
        catch (Exception ex)
        {
            return new OASISResult<bool>(false) { IsError = true, Message = ex.Message };
        }
    }

    public OASISResult<INFTTransactionRespone> MintNFT(INFTTransactionRequest request)
    {
        try
        {
            // Deploy or get contract
            var contract = _web3.Eth.GetContract(contractAbi, _contractAddress);
            
            // Create mint function
            var mintFunction = contract.GetFunction("mint");
            
            // Execute transaction
            var transactionReceipt = mintFunction.SendTransactionAndWaitForReceiptAsync(
                request.MintWalletAddress,
                new HexBigInteger(300000),
                null,
                null,
                request.MintWalletAddress,
                request.Title,
                request.Description
            ).Result;
            
            var response = new NFTTransactionRespone
            {
                TransactionResult = transactionReceipt.TransactionHash,
                OASISNFT = new OASISNFT
                {
                    Hash = transactionReceipt.TransactionHash,
                    Title = request.Title,
                    Description = request.Description
                }
            };
            
            return new OASISResult<INFTTransactionRespone>(response);
        }
        catch (Exception ex)
        {
            return new OASISResult<INFTTransactionRespone>() { IsError = true, Message = ex.Message };
        }
    }
}
```

---

## Support and Resources

### Documentation
- [OASIS API Reference](./OASIS_API_Reference.md)
- [OASIS Test Harnesses Guide](./OASIS_Test_Harnesses_Guide.md)
- [OASIS Quick Start Guide](./OASIS_Quick_Start_Guide.md)

### Community
- [Telegram Chat](https://t.me/ourworldthegamechat)
- [Discord Server](https://discord.gg/q9gMKU6)
- [OASIS API Hackalong](https://t.me/oasisapihackalong)

### Technical Support
- **Email**: ourworld@nextgensoftware.co.uk
- **GitHub Issues**: [Create an issue](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/issues)

---

**Happy Provider Development! 🔧**

*This guide provides comprehensive information for developing custom OASIS providers. Start with simple storage providers and gradually move to more complex blockchain and network providers.*
