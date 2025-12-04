# Stablecoin Solution Completion Task

## 🎯 Objective

Complete the Zcash-backed stablecoin (zUSD) implementation by creating missing service implementations and registering them in the API.

## 📋 Current Status

### ✅ Completed

1. **Frontend (100% Complete)**
   - `StablecoinDashboard.tsx` - Full UI with mint/redeem/dashboard tabs
   - `stablecoinApi.ts` - Complete API client
   - Integrated into wallet navigation

2. **Backend Manager (100% Complete)**
   - `StablecoinManager.cs` - Complete implementation
   - All business logic implemented (mint, redeem, liquidation, yield)

3. **Backend Controller (✅ Just Created)**
   - `StablecoinController.cs` - All endpoints implemented
   - Routes: `/api/v1/stablecoin/*`

4. **DTOs (100% Complete)**
   - `MintStablecoinRequest.cs`
   - `RedeemStablecoinRequest.cs`
   - `StablecoinPosition.cs`
   - `SystemStatus.cs`
   - `PositionHealthResponse.cs`

5. **Interfaces (100% Complete)**
   - `IZcashCollateralService.cs`
   - `IAztecStablecoinService.cs`
   - `IZecPriceOracle.cs`
   - `IStablecoinRepository.cs`
   - `IViewingKeyService.cs`

6. **Price Oracle (✅ Complete)**
   - `CoinGeckoZecPriceOracle.cs` - Uses CoinGecko API

### ⚠️ Missing Implementations

The following service implementations need to be created:

1. **ZcashCollateralService** (implements `IZcashCollateralService`)
   - `LockCollateralAsync()` - Lock ZEC on Zcash
   - `UnlockCollateralAsync()` - Unlock ZEC on Zcash
   - **Location**: Create in `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Stablecoin/Services/`

2. **AztecStablecoinService** (implements `IAztecStablecoinService`)
   - `MintStablecoinAsync()` - Mint zUSD on Aztec
   - `BurnStablecoinAsync()` - Burn zUSD on Aztec
   - **Location**: Create in `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Stablecoin/Services/`

3. **StablecoinRepository** (implements `IStablecoinRepository`)
   - Store/retrieve positions from OASIS storage (MongoDB/LocalFile)
   - **Location**: Create in `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Stablecoin/Repositories/`

4. **ViewingKeyService** (implements `IViewingKeyService`)
   - Generate viewing keys for private position tracking
   - **Location**: Create in `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Stablecoin/Services/`

## 🔧 Implementation Tasks

### Task 1: Create ZcashCollateralService

**File**: `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Stablecoin/Services/ZcashCollateralService.cs`

**Requirements**:
- Use `ZcashOASIS` provider to lock/unlock ZEC
- Lock: Transfer ZEC to a locked address/contract
- Unlock: Transfer ZEC back to user's address
- Handle testnet/mainnet

**Example Structure**:
```csharp
public class ZcashCollateralService : IZcashCollateralService
{
    private readonly IZcashProvider _zcashProvider;
    
    public async Task<OASISResult<string>> LockCollateralAsync(
        string zcashAddress, 
        decimal amount, 
        CancellationToken cancellationToken = default)
    {
        // 1. Get Zcash provider
        // 2. Create transaction to lock ZEC
        // 3. Return transaction ID
    }
    
    public async Task<OASISResult<string>> UnlockCollateralAsync(
        string zcashAddress, 
        decimal amount, 
        CancellationToken cancellationToken = default)
    {
        // 1. Get Zcash provider
        // 2. Create transaction to unlock ZEC
        // 3. Return transaction ID
    }
}
```

**Integration Points**:
- Use `ZcashOASIS` provider from `NextGenSoftware.OASIS.API.Providers.ZcashOASIS`
- May need to use `ZcashRPCClient` for transactions
- Check `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ZcashOASIS/` for available methods

### Task 2: Create AztecStablecoinService

**File**: `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Stablecoin/Services/AztecStablecoinService.cs`

**Requirements**:
- Use `AztecOASIS` provider to mint/burn zUSD
- Mint: Create zUSD tokens on Aztec
- Burn: Destroy zUSD tokens on Aztec
- Handle private transactions

**Example Structure**:
```csharp
public class AztecStablecoinService : IAztecStablecoinService
{
    private readonly IAztecProvider _aztecProvider;
    
    public async Task<OASISResult<string>> MintStablecoinAsync(
        string aztecAddress, 
        decimal amount, 
        CancellationToken cancellationToken = default)
    {
        // 1. Get Aztec provider
        // 2. Mint zUSD to address
        // 3. Return transaction ID
    }
    
    public async Task<OASISResult<string>> BurnStablecoinAsync(
        string aztecAddress, 
        decimal amount, 
        CancellationToken cancellationToken = default)
    {
        // 1. Get Aztec provider
        // 2. Burn zUSD from address
        // 3. Return transaction ID
    }
}
```

**Integration Points**:
- Use `AztecOASIS` provider from `NextGenSoftware.OASIS.API.Providers.AztecOASIS`
- Check `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AztecOASIS/` for available methods
- May need `AztecAPIClient` or `AztecCLIService`

### Task 3: Create StablecoinRepository

**File**: `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Stablecoin/Repositories/StablecoinRepository.cs`

**Requirements**:
- Store positions as Holons in OASIS storage
- Use MongoDB or LocalFile provider
- Implement all repository methods

**Example Structure**:
```csharp
public class StablecoinRepository : IStablecoinRepository
{
    private readonly IOASISStorageProvider _storageProvider;
    
    public async Task<OASISResult<StablecoinPosition>> GetPositionAsync(
        Guid positionId, 
        CancellationToken cancellationToken = default)
    {
        // Load position as Holon from storage
    }
    
    public async Task<OASISResult<bool>> SavePositionAsync(
        StablecoinPosition position, 
        CancellationToken cancellationToken = default)
    {
        // Save position as Holon to storage
    }
    
    // ... other methods
}
```

**Integration Points**:
- Use `HolonManager` to save/load positions
- Positions should be stored as `Holon` objects
- Use `ProviderManager` to get storage provider

### Task 4: Create ViewingKeyService

**File**: `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Stablecoin/Services/ViewingKeyService.cs`

**Requirements**:
- Generate viewing keys for private position tracking
- Hash keys for storage
- Support Zcash viewing key format

**Example Structure**:
```csharp
public class ViewingKeyService : IViewingKeyService
{
    public async Task<OASISResult<string>> GenerateViewingKeyAsync(
        Guid avatarId, 
        CancellationToken cancellationToken = default)
    {
        // 1. Generate viewing key
        // 2. Hash it
        // 3. Return hash
    }
}
```

### Task 5: Register Services in Startup.cs

**File**: `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs`

**Requirements**:
- Register `StablecoinManager` with all dependencies
- Register service implementations
- Ensure services are injected into controller

**Example**:
```csharp
// Register services
services.AddSingleton<IZecPriceOracle, CoinGeckoZecPriceOracle>();
services.AddSingleton<IZcashCollateralService, ZcashCollateralService>();
services.AddSingleton<IAztecStablecoinService, AztecStablecoinService>();
services.AddSingleton<IStablecoinRepository, StablecoinRepository>();
services.AddSingleton<IViewingKeyService, ViewingKeyService>();
services.AddSingleton<StablecoinManager>();
```

## 📝 Implementation Notes

### Zcash Integration
- Check `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ZcashOASIS/` for available methods
- May need to use RPC calls for locking/unlocking
- Testnet addresses: `tm...` (transparent) or `u1...` (unified)

### Aztec Integration
- Check `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AztecOASIS/` for available methods
- May need to use Aztec CLI or API client
- Support private transactions

### Storage
- Positions should be stored as Holons
- Use `HolonManager` for persistence
- Store in MongoDB or LocalFile provider

## 🧪 Testing

After implementation, test:

1. **Mint Flow**:
   ```bash
   # Via API
   POST /api/v1/stablecoin/mint
   {
     "zecAmount": 1.0,
     "stablecoinAmount": 0.5,
     "aztecAddress": "...",
     "zcashAddress": "...",
     "generateViewingKey": true
   }
   ```

2. **Redeem Flow**:
   ```bash
   POST /api/v1/stablecoin/redeem
   {
     "positionId": "...",
     "stablecoinAmount": 0.25,
     "zcashAddress": "..."
   }
   ```

3. **Get Positions**:
   ```bash
   GET /api/v1/stablecoin/positions
   ```

4. **Get System Status**:
   ```bash
   GET /api/v1/stablecoin/system
   ```

## 📚 Related Files

- **Controller**: `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/StablecoinController.cs` ✅
- **Manager**: `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/Stablecoin/StablecoinManager.cs` ✅
- **Frontend**: `zypherpunk-wallet-ui/components/stablecoin/StablecoinDashboard.tsx` ✅
- **API Client**: `zypherpunk-wallet-ui/lib/api/stablecoinApi.ts` ✅
- **Review Doc**: `STABLECOIN_IMPLEMENTATION_REVIEW.md`

## ✅ Success Criteria

1. ✅ All service implementations created
2. ✅ Services registered in `Startup.cs`
3. ✅ API builds without errors
4. ✅ Mint endpoint creates position
5. ✅ Redeem endpoint unlocks ZEC
6. ✅ Positions stored and retrieved correctly
7. ✅ System status endpoint works
8. ✅ Frontend can successfully call all endpoints

## 🚀 Quick Start

1. Create the 4 missing service implementations
2. Register them in `Startup.cs`
3. Build and test the API
4. Test via UI or API calls
5. Verify positions are stored correctly

---

**Status**: Frontend and Manager complete. Controller created. Need 4 service implementations and registration.

