# Zcash-Backed Stablecoin Architecture

## 🎯 Overview

This document defines the architecture for a Zcash-backed stablecoin on Aztec with private yield generation, following OASIS holonic architecture principles.

---

## 📐 Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    Frontend (Wallet UI)                  │
│  - Mint/Redeem UI  - Yield Dashboard  - Position Health │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│              OASIS API Layer (REST/GraphQL)             │
│  - StablecoinController  - OracleController             │
│  - RiskController        - YieldController              │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│              Service Layer (Business Logic)             │
│  - StablecoinManager  - OracleService                   │
│  - RiskManager         - YieldManager                   │
│  - PositionManager     - CollateralManager              │
└──────────────────────┬──────────────────────────────────┘
                       │
        ┌──────────────┼──────────────┐
        │              │              │
        ▼              ▼              ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│  Zcash      │ │   Aztec     │ │   Oracle    │
│  Provider   │ │   Provider  │ │   Service   │
└─────────────┘ └─────────────┘ └─────────────┘
        │              │              │
        └──────────────┼──────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│              Holonic Storage Layer                       │
│  - Position Holons  - Stablecoin Holons                 │
│  - Yield Holons     - Oracle Holons                      │
│  - Risk Holons      - Transaction Holons                │
└─────────────────────────────────────────────────────────┘
```

---

## 🗄️ Data Models (Holons)

### 1. Stablecoin System Holon

```csharp
public class ZcashBackedStablecoinHolon : Holon
{
    // Identity
    public string Name { get; set; } = "zUSD"; // or "zUSDC"
    public string Symbol { get; set; } = "zUSD";
    public int Decimals { get; set; } = 18;
    
    // System State
    public decimal TotalSupply { get; set; }
    public decimal TotalCollateral { get; set; } // Total ZEC locked
    public decimal TotalDebt { get; set; } // Total stablecoin minted
    
    // Risk Parameters
    public decimal CollateralRatio { get; set; } = 150m; // 150% (1.5x)
    public decimal LiquidationThreshold { get; set; } = 120m; // 120% (1.2x)
    public decimal LiquidationBonus { get; set; } = 5m; // 5% bonus for liquidators
    public decimal MaxCollateralRatio { get; set; } = 200m; // 200% max
    public decimal MinCollateralRatio { get; set; } = 130m; // 130% min
    
    // Oracle Configuration
    public string OracleProvider { get; set; } = "CustomZcashOracle";
    public decimal CurrentZECPrice { get; set; }
    public DateTime LastPriceUpdate { get; set; }
    public int PriceUpdateInterval { get; set; } = 60; // seconds
    
    // Yield Configuration
    public YieldStrategy ActiveYieldStrategy { get; set; }
    public decimal CurrentAPY { get; set; }
    public decimal TotalYieldGenerated { get; set; }
    public DateTime LastYieldDistribution { get; set; }
    
    // Aztec Contract Address
    public string AztecContractAddress { get; set; }
    public string AztecContractHash { get; set; }
    
    // Provider Keys
    public Dictionary<ProviderType, string> ProviderKeys { get; set; }
    // Zcash: Bridge contract address
    // Aztec: Stablecoin contract address
    // MongoDB: Holon ID for fast queries
    // IPFS: Immutable backup
    
    // Metadata
    public Dictionary<string, object> MetaData { get; set; }
    // - deploymentDate
    // - lastAuditDate
    // - governanceAddress
    // - emergencyPauseStatus
}
```

### 2. Position Holon (User Position)

```csharp
public class StablecoinPositionHolon : Holon
{
    // Identity
    public string PositionId { get; set; } // GUID
    public string AvatarId { get; set; } // OASIS Avatar ID
    public string AztecAddress { get; set; } // User's Aztec address
    public string ZcashAddress { get; set; } // User's Zcash address
    
    // Collateral
    public decimal CollateralAmount { get; set; } // ZEC locked
    public string CollateralTxHash { get; set; } // Zcash transaction hash
    public DateTime CollateralLockedAt { get; set; }
    public string CollateralViewingKey { get; set; } // For auditability
    
    // Debt
    public decimal StablecoinDebt { get; set; } // Stablecoin minted
    public decimal StablecoinBalance { get; set; } // Current balance
    
    // Position Health
    public decimal CollateralRatio { get; set; } // Current ratio
    public PositionHealthStatus HealthStatus { get; set; }
    public DateTime LastHealthCheck { get; set; }
    
    // Yield
    public decimal YieldEarned { get; set; }
    public decimal YieldAPY { get; set; }
    public YieldStrategy YieldStrategy { get; set; }
    public DateTime LastYieldUpdate { get; set; }
    
    // Liquidation
    public bool IsLiquidated { get; set; }
    public DateTime? LiquidatedAt { get; set; }
    public string LiquidationTxHash { get; set; }
    
    // Provider Keys
    public Dictionary<ProviderType, string> ProviderKeys { get; set; }
    // Zcash: Lock transaction hash
    // Aztec: Position note ID
    // MongoDB: Holon ID
    
    // Metadata
    public Dictionary<string, object> MetaData { get; set; }
    // - creationDate
    // - lastModified
    // - transactionHistory
    // - yieldHistory
}
```

### 3. Oracle Price Holon

```csharp
public class ZcashPriceOracleHolon : Holon
{
    // Identity
    public string OracleId { get; set; }
    public string OracleName { get; set; } = "Zcash Price Oracle";
    
    // Price Data
    public decimal CurrentPrice { get; set; }
    public decimal PreviousPrice { get; set; }
    public decimal PriceChange24h { get; set; }
    public decimal PriceChangePercent24h { get; set; }
    
    // Price History
    public List<PricePoint> PriceHistory { get; set; }
    
    // Oracle Sources
    public List<OracleSource> Sources { get; set; }
    // - Chainlink (if available)
    // - DEX aggregators
    // - Custom price feeds
    // - OASIS price aggregation
    
    // Verification
    public string LastUpdateProof { get; set; } // Merkle proof or signature
    public DateTime LastUpdateTime { get; set; }
    public string LastUpdatedBy { get; set; } // Oracle operator address
    
    // Provider Keys
    public Dictionary<ProviderType, string> ProviderKeys { get; set; }
    
    // Metadata
    public Dictionary<string, object> MetaData { get; set; }
}

public class PricePoint
{
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; }
    public string Proof { get; set; }
}

public class OracleSource
{
    public string Name { get; set; }
    public string Endpoint { get; set; }
    public decimal Weight { get; set; } // Weight in aggregation
    public bool IsActive { get; set; }
    public DateTime LastSuccessfulUpdate { get; set; }
}
```

### 4. Yield Strategy Holon

```csharp
public class YieldStrategyHolon : Holon
{
    // Identity
    public string StrategyId { get; set; }
    public YieldStrategy StrategyType { get; set; }
    public string StrategyName { get; set; }
    
    // Performance
    public decimal CurrentAPY { get; set; }
    public decimal HistoricalAPY { get; set; }
    public decimal TotalYieldGenerated { get; set; }
    public decimal TotalAssetsDeployed { get; set; }
    
    // Configuration
    public decimal MinDeploymentAmount { get; set; }
    public decimal MaxDeploymentAmount { get; set; }
    public int LockPeriod { get; set; } // days
    public decimal FeePercentage { get; set; }
    
    // Status
    public bool IsActive { get; set; }
    public bool IsPaused { get; set; }
    public DateTime LastUpdate { get; set; }
    
    // Provider Keys
    public Dictionary<ProviderType, string> ProviderKeys { get; set; }
    // Aztec: Strategy contract address
    // MongoDB: Holon ID
    
    // Metadata
    public Dictionary<string, object> MetaData { get; set; }
}

public enum YieldStrategy
{
    Lending,      // Lend to private lending pools
    Liquidity,    // Provide liquidity to private DEX
    Staking,      // Stake in private validators
    Custom        // Custom strategy
}
```

### 5. Transaction Holon

```csharp
public class StablecoinTransactionHolon : Holon
{
    // Identity
    public string TransactionId { get; set; }
    public TransactionType Type { get; set; }
    public string PositionId { get; set; }
    
    // Transaction Details
    public decimal Amount { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    
    // Chain Transactions
    public string ZcashTxHash { get; set; } // If collateral operation
    public string AztecTxHash { get; set; } // If stablecoin operation
    public string BridgeTxHash { get; set; } // If bridge operation
    
    // Status
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Privacy
    public bool IsPrivate { get; set; }
    public string ViewingKey { get; set; } // For auditability
    
    // Provider Keys
    public Dictionary<ProviderType, string> ProviderKeys { get; set; }
    
    // Metadata
    public Dictionary<string, object> MetaData { get; set; }
}

public enum TransactionType
{
    Mint,           // Mint stablecoin
    Redeem,         // Redeem stablecoin
    YieldGenerate,  // Generate yield
    YieldDistribute, // Distribute yield
    Liquidate,      // Liquidate position
    Transfer        // Transfer stablecoin
}

public enum TransactionStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}
```

---

## 🔧 Service Layer Architecture

### 1. StablecoinManager

**Location**: `NextGenSoftware.OASIS.API.Core/Managers/StablecoinManager.cs`

```csharp
public class StablecoinManager
{
    private readonly ZcashOASIS _zcashProvider;
    private readonly AztecOASIS _aztecProvider;
    private readonly OracleService _oracleService;
    private readonly RiskManager _riskManager;
    private readonly YieldManager _yieldManager;
    private readonly HolonManager _holonManager;
    
    // Mint stablecoin with ZEC collateral
    public async Task<OASISResult<StablecoinPositionHolon>> MintStablecoinAsync(
        string avatarId,
        decimal zecAmount,
        decimal stablecoinAmount,
        string aztecAddress,
        string zcashAddress,
        bool generateViewingKey = true
    )
    {
        var result = new OASISResult<StablecoinPositionHolon>();
        
        try
        {
            // 1. Validate inputs
            if (zecAmount <= 0 || stablecoinAmount <= 0)
            {
                result.IsError = true;
                result.Message = "Amounts must be greater than zero";
                return result;
            }
            
            // 2. Get current ZEC price from oracle
            var priceResult = await _oracleService.GetZECPriceAsync();
            if (priceResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Oracle error: {priceResult.Message}";
                return result;
            }
            
            var zecPrice = priceResult.Result;
            
            // 3. Calculate collateral ratio
            var collateralValue = zecAmount * zecPrice;
            var collateralRatio = (collateralValue / stablecoinAmount) * 100;
            
            // 4. Get system parameters
            var systemHolon = await GetSystemHolonAsync();
            if (collateralRatio < systemHolon.MinCollateralRatio)
            {
                result.IsError = true;
                result.Message = $"Collateral ratio {collateralRatio:F2}% below minimum {systemHolon.MinCollateralRatio}%";
                return result;
            }
            
            // 5. Lock ZEC on Zcash (shielded transaction)
            var lockResult = await _zcashProvider.LockZECForBridgeAsync(
                zecAmount,
                "Aztec", // destination chain
                aztecAddress,
                generateViewingKey ? null : null // Will generate if true
            );
            
            if (lockResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Zcash lock failed: {lockResult.Message}";
                return result;
            }
            
            // 6. Generate viewing key if requested
            string viewingKey = null;
            if (generateViewingKey)
            {
                var viewingKeyResult = await _zcashProvider.GenerateViewingKeyAsync(zcashAddress);
                if (!viewingKeyResult.IsError)
                {
                    viewingKey = viewingKeyResult.Result.Key;
                }
            }
            
            // 7. Wait for Zcash transaction confirmation
            await WaitForZcashConfirmationAsync(lockResult.Result);
            
            // 8. Mint stablecoin on Aztec (private)
            var mintResult = await _aztecProvider.MintStablecoinAsync(
                aztecAddress,
                stablecoinAmount,
                lockResult.Result, // Proof of Zcash lock
                viewingKey // For auditability
            );
            
            if (mintResult.IsError)
            {
                // Rollback: Release ZEC if mint fails
                await _zcashProvider.ReleaseZECAsync(lockResult.Result);
                result.IsError = true;
                result.Message = $"Aztec mint failed: {mintResult.Message}";
                return result;
            }
            
            // 9. Create position holon
            var position = new StablecoinPositionHolon
            {
                PositionId = Guid.NewGuid().ToString(),
                AvatarId = avatarId,
                AztecAddress = aztecAddress,
                ZcashAddress = zcashAddress,
                CollateralAmount = zecAmount,
                CollateralTxHash = lockResult.Result,
                CollateralLockedAt = DateTime.UtcNow,
                CollateralViewingKey = viewingKey,
                StablecoinDebt = stablecoinAmount,
                StablecoinBalance = stablecoinAmount,
                CollateralRatio = collateralRatio,
                HealthStatus = PositionHealthStatus.Healthy,
                LastHealthCheck = DateTime.UtcNow,
                YieldEarned = 0,
                YieldAPY = systemHolon.CurrentAPY,
                YieldStrategy = systemHolon.ActiveYieldStrategy,
                IsLiquidated = false,
                ProviderKeys = new Dictionary<ProviderType, string>
                {
                    { ProviderType.ZcashOASIS, lockResult.Result },
                    { ProviderType.AztecOASIS, mintResult.Result },
                }
            };
            
            // 10. Save position holon (auto-replicates to MongoDB, IPFS, etc.)
            var saveResult = await _holonManager.SaveHolonAsync(position);
            if (saveResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Failed to save position: {saveResult.Message}";
                return result;
            }
            
            // 11. Update system totals
            await UpdateSystemTotalsAsync(zecAmount, stablecoinAmount);
            
            // 12. Enable yield generation for position
            await _yieldManager.EnableYieldForPositionAsync(position.PositionId);
            
            result.Result = position;
            result.IsError = false;
            result.Message = "Stablecoin minted successfully";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = ex.Message;
            result.Exception = ex;
        }
        
        return result;
    }
    
    // Redeem stablecoin for ZEC
    public async Task<OASISResult<string>> RedeemStablecoinAsync(
        string positionId,
        decimal stablecoinAmount,
        string zcashAddress
    )
    {
        var result = new OASISResult<string>();
        
        try
        {
            // 1. Load position holon
            var positionResult = await _holonManager.LoadHolonAsync<StablecoinPositionHolon>(
                Guid.Parse(positionId)
            );
            
            if (positionResult.IsError || positionResult.Result == null)
            {
                result.IsError = true;
                result.Message = "Position not found";
                return result;
            }
            
            var position = positionResult.Result;
            
            // 2. Validate redemption amount
            if (stablecoinAmount > position.StablecoinBalance)
            {
                result.IsError = true;
                result.Message = "Insufficient stablecoin balance";
                return result;
            }
            
            // 3. Get current ZEC price
            var priceResult = await _oracleService.GetZECPriceAsync();
            if (priceResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Oracle error: {priceResult.Message}";
                return result;
            }
            
            var zecPrice = priceResult.Result;
            
            // 4. Calculate ZEC to return
            var zecToReturn = (stablecoinAmount / zecPrice) * (100m / position.CollateralRatio);
            
            // 5. Check if redemption would make position unhealthy
            var newCollateral = position.CollateralAmount - zecToReturn;
            var newCollateralValue = newCollateral * zecPrice;
            var newDebt = position.StablecoinDebt - stablecoinAmount;
            var newRatio = (newCollateralValue / newDebt) * 100;
            
            var systemHolon = await GetSystemHolonAsync();
            if (newRatio < systemHolon.MinCollateralRatio && newDebt > 0)
            {
                result.IsError = true;
                result.Message = $"Redemption would make position unhealthy. New ratio: {newRatio:F2}%";
                return result;
            }
            
            // 6. Burn stablecoin on Aztec
            var burnResult = await _aztecProvider.BurnStablecoinAsync(
                position.AztecAddress,
                stablecoinAmount,
                position.PositionId
            );
            
            if (burnResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Burn failed: {burnResult.Message}";
                return result;
            }
            
            // 7. Release ZEC from Zcash
            var releaseResult = await _zcashProvider.ReleaseZECAsync(
                position.CollateralTxHash,
                zecToReturn,
                zcashAddress
            );
            
            if (releaseResult.IsError)
            {
                // Rollback: Re-mint stablecoin if release fails
                await _aztecProvider.MintStablecoinAsync(
                    position.AztecAddress,
                    stablecoinAmount,
                    position.CollateralTxHash,
                    position.CollateralViewingKey
                );
                result.IsError = true;
                result.Message = $"ZEC release failed: {releaseResult.Message}";
                return result;
            }
            
            // 8. Update position holon
            position.CollateralAmount = newCollateral;
            position.StablecoinDebt = newDebt;
            position.StablecoinBalance = position.StablecoinBalance - stablecoinAmount;
            position.CollateralRatio = newRatio;
            position.LastHealthCheck = DateTime.UtcNow;
            
            await _holonManager.SaveHolonAsync(position);
            
            // 9. Update system totals
            await UpdateSystemTotalsAsync(-zecToReturn, -stablecoinAmount);
            
            // 10. Create transaction holon
            var transaction = new StablecoinTransactionHolon
            {
                TransactionId = Guid.NewGuid().ToString(),
                Type = TransactionType.Redeem,
                PositionId = positionId,
                Amount = stablecoinAmount,
                FromAddress = position.AztecAddress,
                ToAddress = zcashAddress,
                ZcashTxHash = releaseResult.Result,
                AztecTxHash = burnResult.Result,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                IsPrivate = true,
                ViewingKey = position.CollateralViewingKey
            };
            
            await _holonManager.SaveHolonAsync(transaction);
            
            result.Result = releaseResult.Result;
            result.IsError = false;
            result.Message = "Stablecoin redeemed successfully";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = ex.Message;
            result.Exception = ex;
        }
        
        return result;
    }
    
    // Get position health
    public async Task<OASISResult<PositionHealth>> GetPositionHealthAsync(string positionId)
    {
        // Implementation
    }
    
    // Get system holon
    private async Task<ZcashBackedStablecoinHolon> GetSystemHolonAsync()
    {
        // Load or create system holon
    }
    
    // Update system totals
    private async Task UpdateSystemTotalsAsync(decimal zecDelta, decimal stablecoinDelta)
    {
        // Update total supply, total collateral
    }
}
```

### 2. OracleService

**Location**: `NextGenSoftware.OASIS.API.Core/Services/OracleService.cs`

```csharp
public class OracleService
{
    private readonly List<IOracleSource> _sources;
    private readonly HolonManager _holonManager;
    
    // Get current ZEC price (aggregated from multiple sources)
    public async Task<OASISResult<decimal>> GetZECPriceAsync()
    {
        var result = new OASISResult<decimal>();
        
        try
        {
            var prices = new List<PricePoint>();
            
            // Query all active sources
            foreach (var source in _sources.Where(s => s.IsActive))
            {
                try
                {
                    var priceResult = await source.GetPriceAsync();
                    if (!priceResult.IsError)
                    {
                        prices.Add(new PricePoint
                        {
                            Price = priceResult.Result,
                            Timestamp = DateTime.UtcNow,
                            Source = source.Name,
                            Proof = priceResult.Proof
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue with other sources
                    Console.WriteLine($"Oracle source {source.Name} failed: {ex.Message}");
                }
            }
            
            if (prices.Count == 0)
            {
                result.IsError = true;
                result.Message = "No oracle sources available";
                return result;
            }
            
            // Weighted average (or median, depending on strategy)
            var weightedPrice = CalculateWeightedAverage(prices);
            
            // Update oracle holon
            await UpdateOracleHolonAsync(weightedPrice, prices);
            
            result.Result = weightedPrice;
            result.IsError = false;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = ex.Message;
            result.Exception = ex;
        }
        
        return result;
    }
    
    private decimal CalculateWeightedAverage(List<PricePoint> prices)
    {
        // Implementation: weighted average based on source weights
    }
    
    private async Task UpdateOracleHolonAsync(decimal price, List<PricePoint> history)
    {
        // Update oracle holon with new price
    }
}

public interface IOracleSource
{
    string Name { get; }
    bool IsActive { get; }
    decimal Weight { get; }
    Task<OASISResult<decimal>> GetPriceAsync();
    string Proof { get; }
}
```

### 3. RiskManager

**Location**: `NextGenSoftware.OASIS.API.Core/Managers/RiskManager.cs`

```csharp
public class RiskManager
{
    private readonly OracleService _oracleService;
    private readonly HolonManager _holonManager;
    private readonly StablecoinManager _stablecoinManager;
    
    // Check position health
    public async Task<OASISResult<PositionHealth>> CheckPositionHealthAsync(string positionId)
    {
        var result = new OASISResult<PositionHealth>();
        
        try
        {
            // 1. Load position
            var positionResult = await _holonManager.LoadHolonAsync<StablecoinPositionHolon>(
                Guid.Parse(positionId)
            );
            
            if (positionResult.IsError || positionResult.Result == null)
            {
                result.IsError = true;
                result.Message = "Position not found";
                return result;
            }
            
            var position = positionResult.Result;
            
            // 2. Get current ZEC price
            var priceResult = await _oracleService.GetZECPriceAsync();
            if (priceResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Oracle error: {priceResult.Message}";
                return result;
            }
            
            var zecPrice = priceResult.Result;
            
            // 3. Calculate current collateral ratio
            var collateralValue = position.CollateralAmount * zecPrice;
            var currentRatio = (collateralValue / position.StablecoinDebt) * 100;
            
            // 4. Get system parameters
            var systemHolon = await _stablecoinManager.GetSystemHolonAsync();
            
            // 5. Determine health status
            PositionHealthStatus status;
            if (currentRatio >= systemHolon.CollateralRatio)
            {
                status = PositionHealthStatus.Healthy;
            }
            else if (currentRatio >= systemHolon.LiquidationThreshold)
            {
                status = PositionHealthStatus.Warning;
            }
            else
            {
                status = PositionHealthStatus.Critical;
            }
            
            // 6. Update position
            position.CollateralRatio = currentRatio;
            position.HealthStatus = status;
            position.LastHealthCheck = DateTime.UtcNow;
            await _holonManager.SaveHolonAsync(position);
            
            // 7. Return health result
            result.Result = new PositionHealth
            {
                PositionId = positionId,
                CollateralRatio = currentRatio,
                Status = status,
                CollateralValue = collateralValue,
                Debt = position.StablecoinDebt,
                LiquidationPrice = CalculateLiquidationPrice(position, systemHolon),
                HealthScore = CalculateHealthScore(currentRatio, systemHolon)
            };
            
            result.IsError = false;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = ex.Message;
            result.Exception = ex;
        }
        
        return result;
    }
    
    // Liquidate undercollateralized position
    public async Task<OASISResult<string>> LiquidatePositionAsync(string positionId)
    {
        // Implementation: liquidate position if below threshold
    }
    
    // Monitor all positions (background job)
    public async Task MonitorAllPositionsAsync()
    {
        // Implementation: check all positions periodically
    }
}

public class PositionHealth
{
    public string PositionId { get; set; }
    public decimal CollateralRatio { get; set; }
    public PositionHealthStatus Status { get; set; }
    public decimal CollateralValue { get; set; }
    public decimal Debt { get; set; }
    public decimal LiquidationPrice { get; set; }
    public decimal HealthScore { get; set; } // 0-100
}

public enum PositionHealthStatus
{
    Healthy,    // Above collateral ratio
    Warning,    // Between collateral ratio and liquidation threshold
    Critical    // Below liquidation threshold
}
```

### 4. YieldManager

**Location**: `NextGenSoftware.OASIS.API.Core/Managers/YieldManager.cs`

```csharp
public class YieldManager
{
    private readonly HolonManager _holonManager;
    private readonly AztecOASIS _aztecProvider;
    
    // Generate yield for a position
    public async Task<OASISResult<decimal>> GenerateYieldAsync(string positionId)
    {
        var result = new OASISResult<decimal>();
        
        try
        {
            // 1. Load position
            var position = await LoadPositionAsync(positionId);
            
            // 2. Get yield strategy
            var strategy = await GetYieldStrategyAsync(position.YieldStrategy);
            
            // 3. Calculate yield based on strategy
            var yieldAmount = await CalculateYieldAsync(position, strategy);
            
            // 4. Deploy to yield strategy (private on Aztec)
            var deployResult = await _aztecProvider.DeployToYieldStrategyAsync(
                position.AztecAddress,
                position.CollateralAmount,
                strategy.StrategyId
            );
            
            if (deployResult.IsError)
            {
                result.IsError = true;
                result.Message = deployResult.Message;
                return result;
            }
            
            // 5. Update position
            position.YieldEarned += yieldAmount;
            position.LastYieldUpdate = DateTime.UtcNow;
            await _holonManager.SaveHolonAsync(position);
            
            // 6. Create yield transaction holon
            var transaction = new StablecoinTransactionHolon
            {
                TransactionId = Guid.NewGuid().ToString(),
                Type = TransactionType.YieldGenerate,
                PositionId = positionId,
                Amount = yieldAmount,
                Status = TransactionStatus.Completed,
                IsPrivate = true
            };
            await _holonManager.SaveHolonAsync(transaction);
            
            result.Result = yieldAmount;
            result.IsError = false;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = ex.Message;
            result.Exception = ex;
        }
        
        return result;
    }
    
    // Distribute yield to users
    public async Task<OASISResult<bool>> DistributeYieldAsync()
    {
        // Implementation: distribute yield to all positions
    }
    
    private async Task<decimal> CalculateYieldAsync(
        StablecoinPositionHolon position,
        YieldStrategyHolon strategy
    )
    {
        // Calculate yield based on APY and time
        var timeElapsed = DateTime.UtcNow - position.LastYieldUpdate;
        var days = (decimal)timeElapsed.TotalDays;
        var annualYield = position.CollateralAmount * (strategy.CurrentAPY / 100m);
        var yieldAmount = (annualYield / 365m) * days;
        return yieldAmount;
    }
}
```

---

## 🌐 API Endpoints

### StablecoinController

**Location**: `NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/StablecoinController.cs`

```csharp
[ApiController]
[Route("api/v1/stablecoin")]
[Authorize]
public class StablecoinController : ControllerBase
{
    private readonly StablecoinManager _stablecoinManager;
    private readonly RiskManager _riskManager;
    private readonly YieldManager _yieldManager;
    
    // Mint stablecoin
    [HttpPost("mint")]
    public async Task<IActionResult> MintStablecoin([FromBody] MintStablecoinRequest request)
    {
        var avatarId = GetAvatarIdFromToken(); // From JWT
        
        var result = await _stablecoinManager.MintStablecoinAsync(
            avatarId,
            request.ZecAmount,
            request.StablecoinAmount,
            request.AztecAddress,
            request.ZcashAddress,
            request.GenerateViewingKey
        );
        
        if (result.IsError)
        {
            return BadRequest(result);
        }
        
        return Ok(result);
    }
    
    // Redeem stablecoin
    [HttpPost("redeem")]
    public async Task<IActionResult> RedeemStablecoin([FromBody] RedeemStablecoinRequest request)
    {
        var avatarId = GetAvatarIdFromToken();
        
        var result = await _stablecoinManager.RedeemStablecoinAsync(
            request.PositionId,
            request.StablecoinAmount,
            request.ZcashAddress
        );
        
        if (result.IsError)
        {
            return BadRequest(result);
        }
        
        return Ok(result);
    }
    
    // Get position
    [HttpGet("position/{positionId}")]
    public async Task<IActionResult> GetPosition(string positionId)
    {
        // Implementation
    }
    
    // Get position health
    [HttpGet("position/{positionId}/health")]
    public async Task<IActionResult> GetPositionHealth(string positionId)
    {
        var result = await _riskManager.CheckPositionHealthAsync(positionId);
        return Ok(result);
    }
    
    // Get user positions
    [HttpGet("positions")]
    public async Task<IActionResult> GetUserPositions()
    {
        var avatarId = GetAvatarIdFromToken();
        // Implementation
    }
    
    // Get system info
    [HttpGet("system")]
    public async Task<IActionResult> GetSystemInfo()
    {
        // Implementation
    }
}

public class MintStablecoinRequest
{
    public decimal ZecAmount { get; set; }
    public decimal StablecoinAmount { get; set; }
    public string AztecAddress { get; set; }
    public string ZcashAddress { get; set; }
    public bool GenerateViewingKey { get; set; } = true;
}

public class RedeemStablecoinRequest
{
    public string PositionId { get; set; }
    public decimal StablecoinAmount { get; set; }
    public string ZcashAddress { get; set; }
}
```

---

## 🔄 Flow Diagrams

### Mint Flow

```
User Request
    ↓
1. Validate inputs (amounts, addresses)
    ↓
2. Get ZEC price from Oracle
    ↓
3. Calculate collateral ratio
    ↓
4. Check against minimum ratio
    ↓
5. Lock ZEC on Zcash (shielded)
    ├─→ Generate viewing key (if requested)
    └─→ Wait for confirmation
    ↓
6. Mint stablecoin on Aztec (private)
    ├─→ Verify Zcash lock (via proof)
    └─→ Create private note
    ↓
7. Create Position Holon
    ├─→ Store in MongoDB (fast access)
    ├─→ Replicate to IPFS (permanent)
    └─→ Replicate to Arbitrum (immutable)
    ↓
8. Update system totals
    ↓
9. Enable yield generation
    ↓
10. Return position to user
```

### Redeem Flow

```
User Request
    ↓
1. Load position holon
    ↓
2. Validate redemption amount
    ↓
3. Get current ZEC price
    ↓
4. Calculate ZEC to return
    ↓
5. Check position health after redemption
    ↓
6. Burn stablecoin on Aztec
    ↓
7. Release ZEC from Zcash
    ↓
8. Update position holon
    ↓
9. Update system totals
    ↓
10. Create transaction holon
    ↓
11. Return ZEC to user
```

### Yield Generation Flow

```
Background Job (Periodic)
    ↓
1. Load all active positions
    ↓
2. For each position:
    ├─→ Get yield strategy
    ├─→ Calculate yield (APY * time)
    ├─→ Deploy to yield strategy (private)
    ├─→ Update position yield
    └─→ Create yield transaction holon
    ↓
3. Update system APY
    ↓
4. Distribute yield to users (private)
```

### Risk Monitoring Flow

```
Background Job (Periodic)
    ↓
1. Load all positions
    ↓
2. For each position:
    ├─→ Get current ZEC price
    ├─→ Calculate collateral ratio
    ├─→ Check against thresholds
    └─→ Update health status
    ↓
3. For critical positions:
    ├─→ Trigger liquidation
    ├─→ Seize collateral
    ├─→ Burn stablecoin
    └─→ Distribute liquidation bonus
```

---

## 🔌 Integration Points

### Zcash Provider Integration

```csharp
// ZcashOASIS provider methods needed:
- LockZECForBridgeAsync(amount, destinationChain, destinationAddress, viewingKey)
- ReleaseZECAsync(txHash, amount, recipientAddress)
- GenerateViewingKeyAsync(address)
- GetTransactionStatusAsync(txHash)
```

### Aztec Provider Integration

```csharp
// AztecOASIS provider methods needed:
- MintStablecoinAsync(address, amount, zcashProof, viewingKey)
- BurnStablecoinAsync(address, amount, positionId)
- DeployToYieldStrategyAsync(address, amount, strategyId)
- GetStablecoinBalanceAsync(address)
- TransferStablecoinAsync(from, to, amount)
```

### Oracle Integration

```csharp
// Oracle sources to integrate:
- Chainlink (if available for ZEC)
- DEX aggregators (Uniswap, etc.)
- Custom price feeds
- OASIS price aggregation service
```

---

## 📋 Implementation Checklist

### Phase 1: Core Infrastructure
- [ ] Create Holon models (Stablecoin, Position, Oracle, Yield, Transaction)
- [ ] Implement StablecoinManager
- [ ] Implement OracleService
- [ ] Implement RiskManager
- [ ] Implement YieldManager
- [ ] Create API endpoints

### Phase 2: Provider Integration
- [ ] Integrate Zcash provider (lock/release)
- [ ] Integrate Aztec provider (mint/burn)
- [ ] Test cross-chain operations
- [ ] Implement viewing key generation

### Phase 3: Oracle Integration
- [ ] Set up oracle sources
- [ ] Implement price aggregation
- [ ] Test price updates
- [ ] Implement price history

### Phase 4: Risk Management
- [ ] Implement health checks
- [ ] Implement liquidation system
- [ ] Set up monitoring jobs
- [ ] Test edge cases

### Phase 5: Yield Generation
- [ ] Implement yield strategies
- [ ] Implement yield calculation
- [ ] Implement yield distribution
- [ ] Test yield generation

### Phase 6: Testing & Security
- [ ] End-to-end testing
- [ ] Security audit
- [ ] Performance testing
- [ ] Documentation

---

**Status**: Architecture Design Complete  
**Next Steps**: Begin Phase 1 implementation

