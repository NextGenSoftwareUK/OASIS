# OASIS Test Harnesses Guide for Alpha Testers

## Overview

Test harnesses are pre-built testing applications that demonstrate OASIS functionality and provide working examples for alpha testers. Each test harness focuses on specific components and use cases.

---

## Available Test Harnesses

### 1. ONODE Core Test Harness
**Location**: `NextGenSoftware.OASIS.API.ONODE.Core.TestHarness/`

**Purpose**: Tests core OASIS functionality including NFT minting, avatar operations, and data management.

**Key Features**:
- NFT minting with various providers
- Avatar registration and authentication
- Data holon operations
- Provider switching and failover

**Running the Test Harness**:
```bash
cd NextGenSoftware.OASIS.API.ONODE.Core.TestHarness
dotnet run
```

**Test Output Example**:
```
NEXTGEN SOFTWARE ONODE CORE TEST HARNESS V1.3

Minting SIMPLEST NFT...
✅ Transaction ID: 0x1234...
✅ OASIS NFT ID: your-nft-id
✅ Hash: your-hash...
✅ MintedByAddress: 0xYourWalletAddress
✅ Minted Date: 2024-01-01T00:00:00Z
✅ Meta Data JSON URL: https://gateway.pinata.cloud/ipfs/...
✅ Image URL: https://gateway.pinata.cloud/ipfs/...
```

### 2. HoloNET Test Harness
**Location**: `holochain-client-csharp/NextGenSoftware.Holochain.HoloNET.Client.TestHarness/`

**Purpose**: Tests Holochain connectivity and HoloNET client functionality.

**Available Tests**:
- `WhoAmI` - Test basic Holochain connection
- `Numbers` - Test number operations
- `Signal` - Test signaling functionality
- `SaveLoadOASISEntry` - Test OASIS entry operations
- `LoadTestNumbers` - Performance testing
- `AdminInstallApp` - Admin operations
- `AdminEnableApp` - Enable hApp
- `AdminListApps` - List installed apps

**Running the Test Harness**:
```bash
cd holochain-client-csharp/NextGenSoftware.Holochain.HoloNET.Client.TestHarness
dotnet run
```

**Programmatic Usage**:
```csharp
using NextGenSoftware.Holochain.HoloNET.Client.TestHarness;

// Run specific test
await HoloNETTestHarness.TestHoloNETClientAsync(TestToRun.WhoAmI);

// Run OASIS-specific test
await HoloNETTestHarness.TestHoloNETClientAsync(TestToRun.SaveLoadOASISEntry);
```

### 3. STAR Test Harness
**Location**: `STAR ODK/NextGenSoftware.OASIS.STAR.TestHarness/`

**Purpose**: Tests STAR engine functionality including celestial body creation and OAPP generation.

**Key Features**:
- Celestial body creation (planets, moons, stars)
- OAPP generation from templates
- Provider integration testing
- COSMIC ORM operations

**Running the Test Harness**:
```bash
cd "STAR ODK/NextGenSoftware.OASIS.STAR.TestHarness"
dotnet run
```

**Test Configuration**:
```csharp
private const string defaultGenesisNamespace = "NextGenSoftware.OASIS.STAR.TestHarness.Genesis";
private const string celestialBodyDNAFolder = "CelestialBodyDNA";
private const OAPPType DefaultOAPPType = OAPPType.Console;
```

### 4. Provider-Specific Test Harnesses

#### Arbitrum Test Harness
**Location**: `NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.TestHarness/`

**Purpose**: Tests Arbitrum blockchain integration and NFT operations.

**Features**:
- Smart contract deployment
- NFT minting on Arbitrum
- Transaction management
- Gas fee handling

#### Ethereum Test Harness
**Location**: `NextGenSoftware.OASIS.API.Providers.EthereumOASIS.TestHarness/`

**Purpose**: Tests Ethereum blockchain integration.

**Features**:
- Ethereum network connectivity
- Smart contract interactions
- Token operations
- Transaction monitoring

#### Solana Test Harness
**Location**: `NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness/`

**Purpose**: Tests Solana blockchain integration.

**Features**:
- Solana network connectivity
- SPL token operations
- Program interactions
- Account management

---

## Test Harness Configuration

### OASIS_DNA.json Configuration

Each test harness uses an `OASIS_DNA.json` file for configuration:

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
    },
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
    }
  }
}
```

### Environment Variables

Set these environment variables for testing:

```bash
# Database connections
export MONGODB_CONNECTION_STRING="mongodb://localhost:27017"
export NEO4J_CONNECTION_STRING="bolt://localhost:7687"

# Blockchain RPC endpoints
export ARBITRUM_RPC_URL="https://sepolia-rollup.arbitrum.io/rpc"
export ETHEREUM_RPC_URL="https://sepolia.infura.io/v3/YOUR_INFURA_PROJECT_ID"

# Wallet configuration
export PRIVATE_KEY="your-test-private-key-here"
export WALLET_ADDRESS="your-test-wallet-address-here"
```

---

## Running Tests

### Command Line Execution

#### Basic Execution
```bash
# Navigate to test harness directory
cd NextGenSoftware.OASIS.API.ONODE.Core.TestHarness

# Run with default configuration
dotnet run

# Run with specific configuration
dotnet run --configuration Release

# Run with custom parameters
dotnet run -- --test-type nft-minting --provider ArbitrumOASIS
```

#### Batch Testing
```bash
# Run multiple test harnesses
cd NextGenSoftware.OASIS.API.ONODE.Core.TestHarness && dotnet run
cd ../NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.TestHarness && dotnet run
cd ../../STAR\ ODK/NextGenSoftware.OASIS.STAR.TestHarness && dotnet run
```

### Programmatic Execution

#### Custom Test Runner
```csharp
using NextGenSoftware.OASIS.API.ONODE.Core.TestHarness;

public class CustomTestRunner
{
    public async Task RunAllTests()
    {
        var testHarness = new ONODECoreTestHarness();
        
        // Run specific tests
        await testHarness.TestNFTMinting();
        await testHarness.TestAvatarOperations();
        await testHarness.TestProviderSwitching();
        await testHarness.TestDataOperations();
    }
}
```

#### Provider-Specific Testing
```csharp
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.TestHarness;

public class ArbitrumTestRunner
{
    public async Task RunArbitrumTests()
    {
        var testHarness = new ArbitrumTestHarness();
        
        await testHarness.TestContractDeployment();
        await testHarness.TestNFTMinting();
        await testHarness.TestTransactionHandling();
    }
}
```

---

## Test Scenarios

### 1. Basic Functionality Test

**Objective**: Verify core OASIS functionality works correctly.

**Steps**:
1. Run ONODE Core Test Harness
2. Verify avatar registration
3. Test authentication
4. Verify NFT minting
5. Test data operations

**Expected Results**:
- All operations complete successfully
- No error messages
- Proper data persistence

### 2. Provider Integration Test

**Objective**: Test multiple provider integration and failover.

**Steps**:
1. Configure multiple providers in OASIS_DNA.json
2. Run tests with different providers
3. Test provider switching
4. Test auto-failover
5. Test auto-replication

**Expected Results**:
- Providers switch correctly
- Failover works as expected
- Data replicates across providers

### 3. NFT Minting Test

**Objective**: Test NFT minting across different blockchains.

**Steps**:
1. Configure blockchain providers
2. Set up wallet with test funds
3. Run NFT minting tests
4. Verify on-chain transactions
5. Test metadata storage

**Expected Results**:
- NFTs mint successfully
- Transactions confirmed on-chain
- Metadata stored correctly

### 4. STAR Engine Test

**Objective**: Test STAR engine and celestial body creation.

**Steps**:
1. Run STAR Test Harness
2. Create celestial bodies
3. Test OAPP generation
4. Verify provider integration
5. Test COSMIC ORM operations

**Expected Results**:
- Celestial bodies created successfully
- OAPPs generated correctly
- ORM operations work properly

### 5. HoloNET Integration Test

**Objective**: Test Holochain integration via HoloNET.

**Steps**:
1. Set up Holochain conductor
2. Run HoloNET Test Harness
3. Test zome function calls
4. Test entry operations
5. Verify data persistence

**Expected Results**:
- Holochain connection established
- Zome functions execute correctly
- Data persists on Holochain

---

## Troubleshooting Test Harnesses

### Common Issues

#### 1. Provider Connection Issues
**Problem**: Test harness fails to connect to providers.

**Solutions**:
- Check provider configuration in OASIS_DNA.json
- Verify network connectivity
- Check provider-specific logs
- Ensure required services are running

#### 2. Authentication Failures
**Problem**: Avatar authentication fails in tests.

**Solutions**:
- Verify avatar registration completed
- Check email verification status
- Ensure proper credentials
- Check JWT token validity

#### 3. NFT Minting Failures
**Problem**: NFT minting tests fail.

**Solutions**:
- Verify wallet has sufficient funds
- Check contract deployment status
- Review transaction parameters
- Check gas fee settings

#### 4. Database Connection Issues
**Problem**: Database operations fail.

**Solutions**:
- Check database connection strings
- Verify database services are running
- Check database permissions
- Review connection pool settings

### Debug Mode

Enable debug mode for detailed logging:

```bash
# Set debug environment variable
export OASIS_DEBUG=true

# Run test harness with debug logging
dotnet run -- --debug --verbose
```

### Log Files

Test harnesses generate log files in:
- `logs/test-harness.log` - General test logs
- `logs/provider-errors.log` - Provider-specific errors
- `logs/transaction-logs.log` - Blockchain transaction logs

---

## Test Data Management

### Test Data Cleanup

After running tests, clean up test data:

```csharp
public class TestDataCleanup
{
    public async Task CleanupTestData()
    {
        // Clean up test avatars
        await CleanupTestAvatars();
        
        // Clean up test NFTs
        await CleanupTestNFTs();
        
        // Clean up test holons
        await CleanupTestHolons();
    }
}
```

### Test Data Isolation

Use separate test databases:

```json
{
  "OASIS": {
    "Providers": {
      "MongoDBOASIS": {
        "CustomParams": "mongodb://localhost:27017/oasis_test"
      }
    }
  }
}
```

---

## Performance Testing

### Load Testing

Run load tests to verify performance:

```csharp
public class LoadTestRunner
{
    public async Task RunLoadTests()
    {
        var tasks = new List<Task>();
        
        // Create multiple concurrent operations
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(CreateTestAvatar($"testuser{i}"));
        }
        
        await Task.WhenAll(tasks);
    }
}
```

### Performance Metrics

Monitor these metrics during testing:
- Response times
- Memory usage
- CPU utilization
- Database connection counts
- Transaction success rates

---

## Best Practices

### Test Organization
1. **Group related tests** in separate test harnesses
2. **Use descriptive test names** for clarity
3. **Clean up test data** after each run
4. **Use separate test environments** for isolation

### Error Handling
1. **Implement proper error handling** in tests
2. **Log detailed error information** for debugging
3. **Retry failed operations** where appropriate
4. **Validate results** after each operation

### Configuration Management
1. **Use environment-specific configurations**
2. **Store sensitive data** in environment variables
3. **Document configuration requirements**
4. **Version control configuration templates**

---

## Support and Resources

### Documentation
- [OASIS API Reference](./OASIS_API_Reference.md)
- [OASIS Quick Start Guide](./OASIS_Quick_Start_Guide.md)
- [Full OASIS Documentation](./OASIS_Alpha_Tester_Documentation.md)

### Community Support
- [Telegram Chat](https://t.me/ourworldthegamechat)
- [Discord Server](https://discord.gg/q9gMKU6)
- [OASIS API Hackalong](https://t.me/oasisapihackalong)

### Technical Support
- **Email**: ourworld@nextgensoftware.co.uk
- **GitHub Issues**: [Create an issue](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/issues)

---

**Happy Testing! 🧪**

*This guide provides comprehensive information for using OASIS test harnesses effectively. Start with the basic functionality tests and gradually move to more advanced scenarios.*
