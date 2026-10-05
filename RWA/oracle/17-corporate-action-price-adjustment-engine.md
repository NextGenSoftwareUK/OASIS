# Task 17: Corporate Action Price Adjustment Engine

**Status:** 🟡 **Pending**  
**Priority:** ⭐ Critical  
**Estimated Time:** 1 week  
**Dependencies:** Task 16 (Corporate Action Data Source Integration)

---

## 📋 Overview

Build the core price adjustment engine that applies corporate action adjustments to historical and current equity prices. This enables accurate price comparisons over time despite splits, dividends, and mergers.

---

## ✅ Objectives

1. Create service to calculate adjusted prices
2. Implement adjustment algorithms for each corporate action type
3. Build adjustment history tracking
4. Provide both raw and adjusted prices
5. Optimize for performance (cached calculations)

---

## 🎯 Requirements

### 1. **Service Interface**

```csharp
public interface IPriceAdjustmentService
{
    /// <summary>
    /// Get adjusted price for a given raw price and date
    /// </summary>
    Task<decimal> GetAdjustedPriceAsync(string symbol, decimal rawPrice, DateTime priceDate);
    
    /// <summary>
    /// Get adjustment factor (multiplier) for a date
    /// </summary>
    Task<decimal> GetAdjustmentFactorAsync(string symbol, DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Get all adjustments applied between two dates
    /// </summary>
    Task<List<PriceAdjustment>> GetAdjustmentHistoryAsync(string symbol, DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Apply a single corporate action to a price
    /// </summary>
    decimal ApplyCorporateAction(decimal price, CorporateAction action);
    
    /// <summary>
    /// Get adjusted price for current date
    /// </summary>
    Task<decimal> GetCurrentAdjustedPriceAsync(string symbol, decimal rawPrice);
}
```

### 2. **Adjustment Calculation Logic**

#### Stock Split (e.g., 2-for-1)
```
Adjusted Price = Raw Price / SplitRatio
Example: $100 stock with 2:1 split → $50 adjusted
```

#### Reverse Split (e.g., 1-for-5)
```
Adjusted Price = Raw Price * SplitRatio
Example: $1 stock with 1:5 reverse split → $5 adjusted
Note: SplitRatio should be stored as 0.2 (1/5) for reverse splits
```

#### Dividend
```
No price adjustment needed
Dividends are tracked separately but don't adjust historical prices
```

#### Merger/Acquisition
```
If acquiring company: Adjusted Price = Raw Price * ExchangeRatio
If target company: Price becomes acquiring company's price * ExchangeRatio
Example: Target at $50, exchange ratio 0.5 → Acquiring at $100
```

#### Stock Dividend
```
Similar to split: Adjusted Price = Raw Price / (1 + StockDividendRatio)
Example: 10% stock dividend → divide by 1.1
```

### 3. **Price Adjustment History Model**

```csharp
public class PriceAdjustment
{
    public CorporateAction CorporateAction { get; set; }
    public decimal PriceBefore { get; set; }
    public decimal PriceAfter { get; set; }
    public decimal AdjustmentFactor { get; set; }
    public DateTime AppliedAt { get; set; }
}

public class AdjustedPriceResult
{
    public string Symbol { get; set; }
    public decimal RawPrice { get; set; }
    public decimal AdjustedPrice { get; set; }
    public decimal AdjustmentFactor { get; set; }
    public DateTime PriceDate { get; set; }
    public List<PriceAdjustment> AppliedAdjustments { get; set; }
}
```

### 4. **Adjustment Factor Calculation**

Calculate cumulative adjustment factor from base date to target date:

```
Factor = 1.0
For each corporate action between base and target:
  Factor = Factor * ActionAdjustmentFactor
  
AdjustedPrice = RawPrice * Factor
```

Example:
- Base date: 2020-01-01, Price: $100
- 2020-08-31: 4-for-1 split → Factor = 0.25 (1/4)
- 2021-06-01: 2-for-1 split → Factor = 0.125 (0.25 * 0.5)
- 2024-01-01 adjusted price: $100 * 0.125 = $12.50

### 5. **Performance Optimization**

- Cache adjustment factors per symbol
- Pre-calculate factors for common date ranges
- Use database indexes on Symbol + EffectiveDate
- Consider materialized views for frequently queried adjustments

---

## 📁 Files to Create

```
Application/Contracts/IPriceAdjustmentService.cs
Application/DTOs/PriceAdjustment/
  ├── PriceAdjustment.cs
  ├── AdjustedPriceResult.cs
Infrastructure/ImplementationContract/PriceAdjustmentService.cs
```

---

## 🔧 Implementation Steps

1. **Create Service Interface**
   - Define `IPriceAdjustmentService` with all methods

2. **Implement Core Adjustment Logic**
   - Create `ApplyCorporateAction` method
   - Handle each corporate action type
   - Add unit tests for each type

3. **Implement Historical Adjustment**
   - Query corporate actions between dates
   - Apply adjustments chronologically
   - Calculate cumulative adjustment factor

4. **Build Adjustment History**
   - Return list of all adjustments applied
   - Include before/after prices
   - Include adjustment factors

5. **Add Caching**
   - Cache adjustment factors per symbol
   - Invalidate cache when new corporate actions added
   - Use Redis or in-memory cache

6. **Performance Testing**
   - Test with symbols with many splits (e.g., AAPL)
   - Ensure sub-100ms response time
   - Load test with concurrent requests

7. **Validation Testing**
   - Test with known historical prices
   - Validate against financial data sources
   - Test edge cases (no adjustments, many adjustments)

---

## ✅ Acceptance Criteria

- [ ] Service calculates adjusted prices correctly for all action types
- [ ] Stock splits reduce price correctly
- [ ] Reverse splits increase price correctly
- [ ] Mergers handled correctly
- [ ] Adjustment factors calculated accurately
- [ ] Historical adjustments work for any date range
- [ ] Performance: <100ms for single price adjustment
- [ ] Performance: <500ms for 1-year adjustment history
- [ ] Unit tests with >90% coverage
- [ ] Integration tests with real corporate action data
- [ ] Validation tests pass against known historical prices

---

## 📊 Test Cases

### Test Case 1: Apple Stock Split History

**Symbol:** AAPL  
**Known Events:**
- 2020-08-31: 4-for-1 split
- 2014-06-09: 7-for-1 split
- 2005-02-28: 2-for-1 split

**Test:**
- Raw price on 2020-07-31: $400
- Adjusted price on 2024-01-01: Should be $400 ÷ 4 = $100
- Adjustment factor from 2020-07-31 to 2024-01-01: 0.25

### Test Case 2: Reverse Split

**Symbol:** Example with reverse split  
**Event:** 1-for-5 reverse split on 2023-01-01

**Test:**
- Raw price before: $1.00
- Adjusted price: $5.00
- Adjustment factor: 5.0

### Test Case 3: Multiple Adjustments

**Symbol:** Stock with multiple splits  
**Events:**
- 2020-01-01: 2-for-1 split
- 2021-06-01: 3-for-1 split

**Test:**
- Raw price: $100
- After first split: $50
- After second split: $16.67
- Cumulative factor: 0.1667 (1/2 * 1/3)

### Test Case 4: Dividend (No Adjustment)

**Symbol:** Stock with dividend  
**Event:** $0.50 dividend

**Test:**
- Price before: $100
- Price after: $100 (no adjustment)
- Adjustment factor: 1.0

---

## 🔗 Related Tasks

- **Task 16:** Corporate Action Data Source Integration (depends on)
- **Task 18:** Equity Price Feed Service (uses this service)
- **Task 22:** API Endpoints - Corporate Actions (exposes adjusted prices)

---

## 📚 References

- [Stock Split Adjustment Explanation](https://www.investopedia.com/terms/s/stocksplit.asp)
- [Corporate Action Price Adjustment Standards](https://en.wikipedia.org/wiki/Corporate_action)

---

## 💡 Notes

- Always apply adjustments chronologically (oldest first)
- Store adjustment factors with high precision (decimal 18,8)
- Consider using base date (e.g., IPO date) for normalization
- Document adjustment methodology for auditability
- Test edge cases: symbols with no actions, symbols with many actions

---

**Last Updated:** January 2025  
**Assigned To:** [Agent Name]
