# Task 16: Corporate Action Data Source Integration

**Status:** 🟡 **Pending**  
**Priority:** ⭐ Critical  
**Estimated Time:** 1-2 weeks  
**Dependencies:** None

---

## 📋 Overview

Integrate corporate action data sources to track stock splits, reverse splits, dividends, mergers, and other corporate actions that affect equity prices. This is the foundation for accurate price adjustments.

---

## ✅ Objectives

1. Integrate multiple financial data APIs for corporate actions
2. Create database schema for storing corporate actions
3. Build ingestion service to fetch and store corporate actions
4. Implement data validation and deduplication
5. Create scheduled jobs to update corporate actions daily

---

## 🎯 Requirements

### 1. **Data Sources to Integrate**

#### Primary Sources (Implement all):
- **Alpha Vantage API**
  - Endpoint: `GET /query?function=SPLIT&symbol={symbol}&apikey={key}`
  - Free tier: 5 calls/minute, 500 calls/day
  - Supports: Splits, dividends

- **IEX Cloud API**
  - Endpoint: `GET /stable/stock/{symbol}/splits`
  - Requires subscription (paid)
  - Supports: Splits, dividends, corporate actions

- **Polygon.io API**
  - Endpoint: `GET /v2/reference/splits/{ticker}`
  - Free tier available, paid tiers for real-time
  - Supports: Splits, dividends

#### Secondary Sources (At least one):
- Financial Modeling Prep
- Yahoo Finance (scraping/unofficial API)
- Twelve Data

### 2. **Database Schema**

Create `CorporateActions` table:

```csharp
public class CorporateAction
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } // Stock ticker (e.g., "AAPL")
    public CorporateActionType Type { get; set; }
    public DateTime ExDate { get; set; } // Ex-dividend/split date
    public DateTime RecordDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    
    // Split-specific fields
    public decimal? SplitRatio { get; set; } // e.g., 2:1 split = 2.0, 1:5 reverse = 0.2
    
    // Dividend-specific fields
    public decimal? DividendAmount { get; set; }
    public string? DividendCurrency { get; set; }
    
    // Merger-specific fields
    public string? AcquiringSymbol { get; set; }
    public decimal? ExchangeRatio { get; set; } // Shares of acquiring per target share
    
    // Metadata
    public string DataSource { get; set; } // Which API provided this
    public string? ExternalId { get; set; } // ID from data source
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsVerified { get; set; } // Verified by multiple sources
}

public enum CorporateActionType
{
    StockSplit = 0,
    ReverseSplit = 1,
    Dividend = 2,
    SpecialDividend = 3,
    Merger = 4,
    Acquisition = 5,
    Spinoff = 6,
    StockDividend = 7,
    RightsIssue = 8
}
```

### 3. **Service Interface**

```csharp
public interface ICorporateActionService
{
    Task<List<CorporateAction>> FetchCorporateActionsAsync(string symbol, DateTime? fromDate = null);
    Task<List<CorporateAction>> GetCorporateActionsAsync(string symbol, DateTime? fromDate = null);
    Task<List<CorporateAction>> GetUpcomingCorporateActionsAsync(string symbol, int daysAhead = 30);
    Task<CorporateAction?> GetCorporateActionAsync(Guid id);
    Task SaveCorporateActionsAsync(List<CorporateAction> actions);
    Task<bool> ValidateCorporateActionAsync(CorporateAction action);
}
```

### 4. **Data Source Adapters**

Create adapter pattern for each data source:

```csharp
public interface ICorporateActionDataSource
{
    string SourceName { get; }
    Task<List<CorporateAction>> FetchSplitsAsync(string symbol);
    Task<List<CorporateAction>> FetchDividendsAsync(string symbol);
    Task<List<CorporateAction>> FetchAllActionsAsync(string symbol, DateTime? fromDate = null);
}
```

Implement:
- `AlphaVantageCorporateActionSource.cs`
- `IexCloudCorporateActionSource.cs`
- `PolygonCorporateActionSource.cs`

### 5. **Deduplication Logic**

- Compare corporate actions by Symbol + Type + EffectiveDate
- If same action found from multiple sources, mark as `IsVerified = true`
- Store multiple source references for auditability

### 6. **Scheduled Jobs**

Create background service to:
- Daily fetch corporate actions for all tracked symbols
- Update existing actions if new data available
- Alert on discrepancies between sources

---

## 📁 Files to Create

```
Domain/Entities/CorporateAction.cs
Domain/Enums/CorporateActionType.cs
Application/Contracts/ICorporateActionService.cs
Application/DTOs/CorporateAction/* (if needed)
Infrastructure/ImplementationContract/CorporateActionService.cs
Infrastructure/DataAccess/EntityConfigurations/CorporateActionConfig.cs
Infrastructure/ExternalServices/CorporateActions/
  ├── ICorporateActionDataSource.cs
  ├── AlphaVantageCorporateActionSource.cs
  ├── IexCloudCorporateActionSource.cs
  └── PolygonCorporateActionSource.cs
Infrastructure/DataAccess/Migrations/[timestamp]_AddCorporateActions.cs
API/Infrastructure/DI/CustomServiceRegister.cs (register services)
```

---

## 🔧 Implementation Steps

1. **Setup Database Schema**
   - Create `CorporateAction` entity
   - Create Entity Framework configuration
   - Create migration
   - Apply migration

2. **Create Data Source Interfaces**
   - Define `ICorporateActionDataSource` interface
   - Define data source configuration (API keys, base URLs)

3. **Implement Alpha Vantage Integration**
   - Create `AlphaVantageCorporateActionSource`
   - Parse API responses
   - Map to `CorporateAction` entities
   - Handle rate limiting and errors

4. **Implement Additional Sources**
   - IEX Cloud integration
   - Polygon.io integration
   - (Optional) Additional sources

5. **Build Service Layer**
   - Implement `ICorporateActionService`
   - Add deduplication logic
   - Add validation logic
   - Add multi-source consensus

6. **Create Scheduled Job**
   - Background service to fetch daily
   - Configurable symbol list
   - Error handling and retries
   - Logging

7. **Add Configuration**
   - API keys in configuration
   - Rate limiting settings
   - Source priority settings

8. **Testing**
   - Unit tests for each data source
   - Integration tests for service
   - Test with known symbols (AAPL, MSFT, etc.)
   - Validate deduplication works

---

## ✅ Acceptance Criteria

- [ ] Database schema created and migrated
- [ ] At least 3 data sources integrated (Alpha Vantage, IEX Cloud, Polygon)
- [ ] Corporate actions can be fetched for any symbol
- [ ] Deduplication logic works correctly
- [ ] Actions are marked as verified when found in multiple sources
- [ ] Scheduled job runs daily and updates data
- [ ] All API rate limits respected
- [ ] Error handling for API failures
- [ ] Unit tests with >80% coverage
- [ ] Integration tests pass

---

## 📊 Test Cases

### Test Symbol: AAPL (Apple)

1. **Fetch Splits:**
   - Should return 4-for-1 split on 2020-08-31
   - Should return 7-for-1 split on 2014-06-09

2. **Fetch Dividends:**
   - Should return quarterly dividend payments

3. **Deduplication:**
   - Fetch from 2 sources
   - Should merge into single record with `IsVerified = true`

4. **Edge Cases:**
   - Invalid symbol returns empty list
   - API failure falls back to other sources
   - Rate limiting waits appropriately

---

## 🔗 Related Tasks

- **Task 17:** Corporate Action Price Adjustment Engine (depends on this)
- **Task 18:** Equity Price Feed Service (uses corporate actions)
- **Task 22:** API Endpoints - Corporate Actions

---

## 📚 References

- [Alpha Vantage API Docs](https://www.alphavantage.co/documentation/)
- [IEX Cloud API Docs](https://iexcloud.io/docs/api/)
- [Polygon.io API Docs](https://polygon.io/docs/stocks/get_v2_reference_splits)

---

## 💡 Notes

- Start with free tier APIs (Alpha Vantage) for development
- Use paid APIs (IEX Cloud) for production reliability
- Cache API responses to reduce calls
- Consider webhook subscriptions for real-time updates (if available)

---

**Last Updated:** January 2025  
**Assigned To:** [Agent Name]
