# Task 19: Funding Rate Calculation Service

**Status:** 🟡 **Pending**  
**Priority:** ⭐ Critical  
**Estimated Time:** 1-2 weeks  
**Dependencies:** Task 18 (Equity Price Feed Service)

---

## 📋 Overview

Build a funding rate calculation service for perpetual futures on RWAs/equities. Unlike crypto perps which use premium/discount to spot, RWA funding rates need to account for corporate action windows, volatility, and liquidity factors.

---

## ✅ Objectives

1. Calculate funding rates based on mark price vs adjusted spot price
2. Factor in corporate action windows (higher volatility expected)
3. Adjust for liquidity conditions
4. Adjust for volatility
5. Provide funding rate history
6. Support hourly funding rate updates

---

## 🎯 Requirements

### 1. **Service Interface**

```csharp
public interface IFundingRateService
{
    Task<FundingRate> CalculateFundingRateAsync(string symbol, decimal markPrice);
    Task<FundingRate> GetCurrentFundingRateAsync(string symbol);
    Task<List<FundingRate>> GetFundingRateHistoryAsync(string symbol, int hours = 24);
    Task<Dictionary<string, FundingRate>> GetBatchFundingRatesAsync(List<string> symbols);
    Task<FundingRateFactors> GetFundingRateFactorsAsync(string symbol);
}

public class FundingRate
{
    public string Symbol { get; set; }
    public decimal Rate { get; set; } // Annualized percentage (e.g., 0.1 = 10%)
    public decimal HourlyRate { get; set; } // Rate per hour
    public decimal MarkPrice { get; set; }
    public decimal SpotPrice { get; set; }
    public decimal AdjustedSpotPrice { get; set; }
    public decimal Premium { get; set; } // Mark - Spot
    public decimal PremiumPercentage { get; set; } // (Mark - Spot) / Spot * 100
    public FundingRateFactors Factors { get; set; }
    public DateTime CalculatedAt { get; set; }
    public DateTime ValidUntil { get; set; } // Usually +1 hour
}

public class FundingRateFactors
{
    public decimal BaseRate { get; set; } // Standard funding from premium
    public decimal CorporateActionAdjustment { get; set; } // Boost during CA windows
    public decimal LiquidityAdjustment { get; set; } // Adjustment for low liquidity
    public decimal VolatilityAdjustment { get; set; } // Adjustment based on volatility
    public decimal FinalRate { get; set; } // Sum of all factors
}
```

### 2. **Funding Rate Calculation Logic**

#### Base Rate Calculation:
```
Premium = MarkPrice - AdjustedSpotPrice
PremiumPercentage = (Premium / AdjustedSpotPrice) * 100
BaseRate = PremiumPercentage * FundingMultiplier

Example:
- Mark: $152
- Spot: $150
- Premium: $2 (1.33%)
- FundingMultiplier: 0.1
- BaseRate: 1.33% * 0.1 = 0.133% (annualized)
```

#### Corporate Action Adjustment:
```
If upcoming corporate action within 7 days:
  CorporateActionAdjustment = 0.5% (fixed boost)

If corporate action within 3 days:
  CorporateActionAdjustment = 1.0% (higher boost)

Rationale: Higher volatility expected, increase funding to compensate
```

#### Liquidity Adjustment:
```
LiquidityScore = CalculateLiquidityScore(symbol) // 0-1 scale
LiquidityAdjustment = (1.0 - LiquidityScore) * 0.3%

Example:
- LiquidityScore: 0.4 (low liquidity)
- LiquidityAdjustment: (1.0 - 0.4) * 0.3% = 0.18%
```

#### Volatility Adjustment:
```
Volatility = CalculateVolatility(symbol, days: 30) // 30-day rolling
If Volatility > 20%:
  VolatilityAdjustment = (Volatility - 0.2) * 0.2%

Example:
- Volatility: 35%
- VolatilityAdjustment: (0.35 - 0.2) * 0.2% = 0.03%
```

#### Final Rate Calculation:
```
FinalRate = BaseRate + CorporateActionAdjustment + LiquidityAdjustment + VolatilityAdjustment
HourlyRate = FinalRate / (365 * 24)

Cap rates:
- Maximum annualized rate: 100% (extreme cases)
- Minimum annualized rate: -100% (extreme cases)
- Typical range: -10% to +10%
```

### 3. **Supporting Services**

#### Liquidity Score Calculation:
```csharp
public interface ILiquidityService
{
    Task<decimal> GetLiquidityScoreAsync(string symbol); // 0-1 scale
}

// Calculate based on:
// - Average daily volume
// - Bid-ask spread
// - Order book depth
// - Trade frequency
```

#### Volatility Calculation:
```csharp
public interface IVolatilityService
{
    Task<decimal> GetVolatilityAsync(string symbol, int days = 30); // Returns as decimal (e.g., 0.25 = 25%)
}

// Calculate 30-day rolling volatility:
// - Get daily prices for last 30 days
// - Calculate daily returns
// - Calculate standard deviation of returns
// - Annualize: StdDev * sqrt(252)
```

### 4. **Funding Rate Storage**

Store funding rates in database for history:

```csharp
public class FundingRate
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public decimal Rate { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal MarkPrice { get; set; }
    public decimal SpotPrice { get; set; }
    public decimal AdjustedSpotPrice { get; set; }
    public decimal Premium { get; set; }
    public decimal PremiumPercentage { get; set; }
    public decimal BaseRate { get; set; }
    public decimal CorporateActionAdjustment { get; set; }
    public decimal LiquidityAdjustment { get; set; }
    public decimal VolatilityAdjustment { get; set; }
    public DateTime CalculatedAt { get; set; }
    public DateTime ValidUntil { get; set; }
    public string? OnChainTransactionHash { get; set; } // For Solana publishing
}
```

### 5. **Scheduled Calculation**

Create background service to:
- Calculate funding rates hourly for all tracked symbols
- Store in database
- Publish to on-chain (separate task)
- Alert on extreme rates

---

## 📁 Files to Create

```
Application/Contracts/IFundingRateService.cs
Application/Contracts/ILiquidityService.cs
Application/Contracts/IVolatilityService.cs
Application/DTOs/FundingRate/
  ├── FundingRate.cs
  ├── FundingRateFactors.cs
Infrastructure/ImplementationContract/FundingRateService.cs
Infrastructure/ImplementationContract/LiquidityService.cs
Infrastructure/ImplementationContract/VolatilityService.cs
Domain/Entities/FundingRate.cs (database entity)
Infrastructure/DataAccess/EntityConfigurations/FundingRateConfig.cs
Infrastructure/DataAccess/Migrations/[timestamp]_AddFundingRates.cs
```

---

## 🔧 Implementation Steps

1. **Create Service Interfaces**
   - Define `IFundingRateService`
   - Define `ILiquidityService`
   - Define `IVolatilityService`

2. **Implement Volatility Service**
   - Calculate 30-day rolling volatility
   - Use historical price data
   - Annualize volatility

3. **Implement Liquidity Service**
   - Calculate liquidity score from volume data
   - Consider bid-ask spread (if available)
   - Normalize to 0-1 scale

4. **Implement Base Funding Rate Calculation**
   - Calculate premium to spot
   - Apply funding multiplier
   - Handle negative premiums (discounts)

5. **Implement Adjustments**
   - Corporate action adjustment
   - Liquidity adjustment
   - Volatility adjustment

6. **Build Complete Funding Rate Service**
   - Combine all factors
   - Calculate final rate
   - Calculate hourly rate
   - Apply rate caps

7. **Create Database Schema**
   - `FundingRate` entity
   - Entity configuration
   - Migration

8. **Add History Tracking**
   - Store all calculated rates
   - Query historical rates
   - Calculate rate trends

9. **Create Scheduled Job**
   - Calculate rates hourly
   - Store in database
   - Handle errors and retries

10. **Testing**
    - Unit tests for each calculation component
    - Integration tests with real data
    - Test edge cases (extreme premiums, zero liquidity)
    - Validate against expected ranges

---

## ✅ Acceptance Criteria

- [ ] Base funding rate calculated correctly from premium
- [ ] Corporate action adjustments applied correctly
- [ ] Liquidity adjustments applied correctly
- [ ] Volatility adjustments applied correctly
- [ ] Final rate combines all factors correctly
- [ ] Hourly rate calculated correctly
- [ ] Rate caps applied (max 100%, min -100%)
- [ ] Funding rate history stored and retrievable
- [ ] Scheduled job runs hourly
- [ ] Performance: <1 second per symbol
- [ ] Unit tests with >85% coverage
- [ ] Integration tests pass

---

## 📊 Test Cases

### Test Case 1: Standard Funding Rate

**Input:**
- Symbol: AAPL
- Mark Price: $152
- Spot Price: $150
- No upcoming corporate actions
- Normal liquidity (0.8)
- Normal volatility (25%)

**Expected:**
- Premium: $2 (1.33%)
- Base Rate: 0.133%
- Corporate Action Adjustment: 0%
- Liquidity Adjustment: 0.06% ((1-0.8) * 0.3%)
- Volatility Adjustment: 0.01% ((0.25-0.2) * 0.2%)
- Final Rate: ~0.2% annualized
- Hourly Rate: ~0.000023% per hour

### Test Case 2: High Premium

**Input:**
- Mark Price: $160
- Spot Price: $150
- Premium: $10 (6.67%)

**Expected:**
- Base Rate: 0.667% (higher due to premium)
- Final rate proportionally higher

### Test Case 3: Corporate Action Window

**Input:**
- Upcoming split in 5 days
- Other factors normal

**Expected:**
- Corporate Action Adjustment: 0.5%
- Final rate increased by 0.5%

### Test Case 4: Low Liquidity

**Input:**
- Liquidity Score: 0.2 (very low)

**Expected:**
- Liquidity Adjustment: 0.24% ((1-0.2) * 0.3%)
- Final rate increased

### Test Case 5: High Volatility

**Input:**
- Volatility: 50%

**Expected:**
- Volatility Adjustment: 0.06% ((0.5-0.2) * 0.2%)
- Final rate increased

### Test Case 6: Negative Premium (Discount)

**Input:**
- Mark Price: $148
- Spot Price: $150
- Premium: -$2 (-1.33%)

**Expected:**
- Base Rate: -0.133% (negative funding)
- Longs pay shorts (opposite of premium case)

---

## 🔗 Related Tasks

- **Task 18:** Equity Price Feed Service (depends on - needs spot prices)
- **Task 20:** Funding Rate On-Chain Publishing (publishes rates from this service)
- **Task 23:** API Endpoints - Funding Rates (exposes this service)
- **Task 26:** Frontend - Funding Rate Monitor (displays this data)

---

## 📚 References

- [Perpetual Funding Rates Explained](https://www.binance.com/en/support/faq/how-are-funding-rates-calculated)
- [Funding Rate Calculation Methodology](https://docs.gmx.io/docs/trading/v2#funding-rates)

---

## 💡 Notes

- Funding multiplier (0.1) should be configurable
- Consider different multipliers for different asset classes
- Monitor funding rates for unusual patterns
- Consider time-weighted funding rates for periods with varying rates
- Test thoroughly with edge cases (zero liquidity, extreme volatility)

---

**Last Updated:** January 2025  
**Assigned To:** [Agent Name]
