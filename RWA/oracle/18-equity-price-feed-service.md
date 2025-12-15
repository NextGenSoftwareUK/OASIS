# Task 18: Equity Price Feed Service

**Status:** 🟡 **Pending**  
**Priority:** ⭐ Critical  
**Estimated Time:** 1-2 weeks  
**Dependencies:** Task 16, Task 17

---

## 📋 Overview

Build a comprehensive equity price feed service that aggregates prices from multiple sources, applies corporate action adjustments, and provides confidence-scored prices for on-chain use. This extends the existing oracle infrastructure to support equities.

---

## ✅ Objectives

1. Integrate multiple equity price data sources
2. Implement multi-source price aggregation with consensus
3. Apply corporate action adjustments to prices
4. Calculate confidence scores based on source reliability
5. Provide both real-time and historical price feeds
6. Integrate with existing OASIS oracle infrastructure

---

## 🎯 Requirements

### 1. **Data Sources to Integrate**

#### Primary Sources (Implement at least 3):
- **Alpha Vantage**
  - Endpoint: `GET /query?function=GLOBAL_QUOTE&symbol={symbol}`
  - Real-time quotes

- **IEX Cloud**
  - Endpoint: `GET /stable/stock/{symbol}/quote`
  - Real-time and delayed quotes

- **Polygon.io**
  - Endpoint: `GET /v2/aggs/ticker/{ticker}/prev`
  - Real-time and historical

#### Secondary Sources:
- Yahoo Finance (scraping/API)
- Twelve Data
- Finnhub

### 2. **Service Interface**

```csharp
public interface IEquityPriceService
{
    Task<EquityPriceResponse> GetAdjustedPriceAsync(string symbol);
    Task<EquityPriceResponse> GetRawPriceAsync(string symbol);
    Task<List<EquityPriceResponse>> GetBatchPricesAsync(List<string> symbols);
    Task<EquityPriceHistory> GetPriceHistoryAsync(string symbol, DateTime fromDate, DateTime toDate, bool adjusted = true);
    Task<EquityPriceResponse> GetPriceAtDateAsync(string symbol, DateTime date, bool adjusted = true);
}

public class EquityPriceResponse
{
    public string Symbol { get; set; }
    public decimal RawPrice { get; set; }
    public decimal AdjustedPrice { get; set; }
    public decimal Confidence { get; set; } // 0-1 scale
    public DateTime PriceDate { get; set; }
    public List<SourcePrice> Sources { get; set; }
    public List<CorporateActionInfo> CorporateActionsApplied { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class SourcePrice
{
    public string SourceName { get; set; }
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal ReliabilityScore { get; set; } // 0-1 scale
    public int LatencyMs { get; set; }
}

public class CorporateActionInfo
{
    public CorporateActionType Type { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal AdjustmentFactor { get; set; }
}
```

### 3. **Price Aggregation Logic**

#### Consensus Calculation:
1. Fetch prices from all available sources
2. Remove outliers (prices >3 standard deviations from median)
3. Calculate weighted average:
   ```
   WeightedPrice = Σ(Price × ReliabilityScore) / Σ(ReliabilityScore)
   ```
4. Calculate confidence:
   ```
   Confidence = (AgreementPercentage × AverageReliability) × SourceCountFactor
   ```
   Where:
   - AgreementPercentage = % of sources within 1% of weighted average
   - AverageReliability = Average reliability of agreeing sources
   - SourceCountFactor = min(1.0, sourceCount / 3) (more sources = higher confidence)

#### Source Reliability Scores (example):
- IEX Cloud: 0.95 (paid, reliable)
- Polygon.io: 0.90 (paid, reliable)
- Alpha Vantage: 0.75 (free tier, rate limited)
- Yahoo Finance: 0.70 (scraped, less reliable)

### 4. **Integration with Existing Oracle**

Extend the existing oracle feed builder to support equity feeds:

```csharp
// Add to existing Oracle Feed Builder
public class EquityPriceFeed : OracleFeed
{
    public string Symbol { get; set; }
    public bool UseAdjustedPrices { get; set; } = true;
    public List<string> PriceSources { get; set; }
    public decimal MinConfidence { get; set; } = 0.7m;
}
```

### 5. **Caching Strategy**

- Cache raw prices for 1 minute (real-time data)
- Cache adjusted prices for 5 minutes (adjustments change less frequently)
- Cache historical prices for 24 hours
- Use Redis or in-memory cache with TTL

### 6. **Error Handling**

- If primary source fails, fallback to secondary sources
- If all sources fail, return last known price with low confidence
- Log all failures for monitoring
- Alert on repeated failures

---

## 📁 Files to Create

```
Application/Contracts/IEquityPriceService.cs
Application/DTOs/EquityPrice/
  ├── EquityPriceResponse.cs
  ├── SourcePrice.cs
  ├── CorporateActionInfo.cs
  ├── EquityPriceHistory.cs
Infrastructure/ImplementationContract/EquityPriceService.cs
Infrastructure/ExternalServices/EquityPrices/
  ├── IEquityPriceDataSource.cs
  ├── AlphaVantagePriceSource.cs
  ├── IexCloudPriceSource.cs
  ├── PolygonPriceSource.cs
Domain/Entities/EquityPrice.cs (for caching/history)
Application/Contracts/IPriceAdjustmentService.cs (use from Task 17)
```

---

## 🔧 Implementation Steps

1. **Create Data Source Interfaces**
   - Define `IEquityPriceDataSource` interface
   - Standardize price response format

2. **Implement Data Source Adapters**
   - Alpha Vantage integration
   - IEX Cloud integration
   - Polygon.io integration
   - (Optional) Additional sources

3. **Build Price Aggregation Service**
   - Implement consensus calculation
   - Implement outlier detection
   - Implement weighted averaging
   - Calculate confidence scores

4. **Integrate Price Adjustment**
   - Use `IPriceAdjustmentService` from Task 17
   - Apply adjustments to aggregated price
   - Track which adjustments were applied

5. **Create Database Entity for Caching**
   - `EquityPrice` entity for storing recent prices
   - Store raw and adjusted prices
   - Store source breakdown

6. **Build Service Layer**
   - Implement `IEquityPriceService`
   - Add caching logic
   - Add error handling and fallbacks
   - Add batch operations

7. **Integrate with Oracle Infrastructure**
   - Extend `OracleFeed` to support equity feeds
   - Add equity feed creation to oracle builder
   - Add equity price endpoints to oracle API

8. **Performance Optimization**
   - Implement parallel fetching from sources
   - Add caching layer
   - Optimize database queries
   - Add connection pooling

9. **Testing**
   - Unit tests for aggregation logic
   - Integration tests with real APIs
   - Test with multiple symbols
   - Test error scenarios

---

## ✅ Acceptance Criteria

- [ ] At least 3 price data sources integrated
- [ ] Multi-source consensus calculation works correctly
- [ ] Outlier detection removes bad prices
- [ ] Confidence scores calculated accurately (0-1 scale)
- [ ] Corporate action adjustments applied correctly
- [ ] Raw and adjusted prices both available
- [ ] Caching reduces API calls
- [ ] Error handling with fallbacks works
- [ ] Batch operations support multiple symbols
- [ ] Historical price retrieval works
- [ ] Integration with existing oracle infrastructure complete
- [ ] Performance: <500ms for single symbol
- [ ] Performance: <2s for batch of 10 symbols
- [ ] Unit tests with >85% coverage
- [ ] Integration tests pass

---

## 📊 Test Cases

### Test Case 1: Single Symbol Price Fetch

**Symbol:** AAPL

**Expected:**
- Fetch from all available sources
- Aggregate to single price
- Apply corporate action adjustments
- Calculate confidence score
- Return within 500ms

### Test Case 2: Outlier Detection

**Scenario:** 3 sources return $150, $152, $200

**Expected:**
- $200 detected as outlier
- Consensus price: ~$151 (average of $150, $152)
- Confidence lowered due to outlier

### Test Case 3: Source Failure

**Scenario:** Primary source fails

**Expected:**
- Fallback to secondary sources
- Return price with lower confidence
- Log error for monitoring

### Test Case 4: Corporate Action Adjustment

**Symbol:** AAPL (with known 4-for-1 split on 2020-08-31)

**Test Date:** 2024-01-01

**Expected:**
- Raw price: ~$150
- Adjusted price: ~$150 (no adjustment needed for current date)
- Historical price (2020-07-31): Raw ~$400, Adjusted ~$100

### Test Case 5: Batch Operation

**Symbols:** ["AAPL", "MSFT", "GOOGL"]

**Expected:**
- Fetch prices in parallel
- Return all 3 prices
- Total time <2 seconds

---

## 🔗 Related Tasks

- **Task 16:** Corporate Action Data Source Integration (depends on)
- **Task 17:** Corporate Action Price Adjustment Engine (depends on)
- **Task 22:** API Endpoints - Equity Prices (exposes this service)
- **Task 25:** Frontend - Adjusted Price Feed Display (displays this data)

---

## 📚 References

- [Alpha Vantage Global Quote API](https://www.alphavantage.co/documentation/#latestprice)
- [IEX Cloud Quote API](https://iexcloud.io/docs/api/#quote)
- [Polygon.io Aggregates API](https://polygon.io/docs/stocks/get_v2_aggs_ticker__stocksticker__prev)

---

## 💡 Notes

- Start with free tier APIs for development
- Use paid APIs for production reliability
- Consider WebSocket subscriptions for real-time updates
- Monitor API rate limits carefully
- Cache aggressively to reduce API calls
- Confidence scores should be conservative (better to be cautious)

---

**Last Updated:** January 2025  
**Assigned To:** [Agent Name]
