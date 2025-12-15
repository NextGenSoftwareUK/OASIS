# Task 24: API Endpoints - Equity Prices

**Status:** 🟡 **Pending**  
**Priority:** ⭐ Critical  
**Estimated Time:** 3-5 days  
**Dependencies:** Task 18, Task 22

---

## 📋 Overview

Create REST API endpoints for equity price feeds. Provide endpoints to get current prices (raw and adjusted), historical prices, and batch price queries.

---

## ✅ Objectives

1. Create GET endpoints for equity prices
2. Support both raw and adjusted prices
3. Add historical price queries
4. Add batch price queries
5. Add response DTOs with source breakdown
6. Add caching headers

---

## 🎯 Requirements

### 1. **Endpoints**

#### GET /api/oracle/rwa/equity/{symbol}/price
Get current price for a symbol (defaults to adjusted).

**Query Parameters:**
- `adjusted` (optional): true/false, default: true

**Response:**
```json
{
  "result": {
    "symbol": "AAPL",
    "rawPrice": 150.25,
    "adjustedPrice": 150.25,
    "confidence": 0.95,
    "priceDate": "2024-01-25T10:30:00Z",
    "sources": [
      {
        "sourceName": "IEX Cloud",
        "price": 150.26,
        "timestamp": "2024-01-25T10:29:55Z",
        "reliabilityScore": 0.95,
        "latencyMs": 120
      },
      {
        "sourceName": "Polygon.io",
        "price": 150.24,
        "timestamp": "2024-01-25T10:29:58Z",
        "reliabilityScore": 0.90,
        "latencyMs": 150
      }
    ],
    "corporateActionsApplied": [],
    "lastUpdated": "2024-01-25T10:30:00Z"
  },
  "isError": false
}
```

#### GET /api/oracle/rwa/equity/{symbol}/price/history
Get historical prices for a symbol.

**Query Parameters:**
- `from` (required): Start date
- `to` (required): End date
- `adjusted` (optional): true/false, default: true
- `interval` (optional): "1d", "1h", "1m" (default: "1d")

**Response:**
```json
{
  "result": {
    "symbol": "AAPL",
    "prices": [
      {
        "priceDate": "2024-01-24T00:00:00Z",
        "rawPrice": 150.10,
        "adjustedPrice": 150.10,
        "confidence": 0.94
      },
      {
        "priceDate": "2024-01-25T00:00:00Z",
        "rawPrice": 150.25,
        "adjustedPrice": 150.25,
        "confidence": 0.95
      }
    ],
    "fromDate": "2024-01-24T00:00:00Z",
    "toDate": "2024-01-25T00:00:00Z",
    "adjusted": true
  },
  "isError": false
}
```

#### GET /api/oracle/rwa/equity/prices/batch
Get prices for multiple symbols at once.

**Query Parameters:**
- `symbols` (required): Comma-separated list, e.g., "AAPL,MSFT,GOOGL"
- `adjusted` (optional): true/false, default: true

**Response:**
```json
{
  "result": {
    "prices": [
      {
        "symbol": "AAPL",
        "rawPrice": 150.25,
        "adjustedPrice": 150.25,
        "confidence": 0.95,
        "priceDate": "2024-01-25T10:30:00Z",
        "lastUpdated": "2024-01-25T10:30:00Z"
      },
      {
        "symbol": "MSFT",
        "rawPrice": 385.50,
        "adjustedPrice": 385.50,
        "confidence": 0.93,
        "priceDate": "2024-01-25T10:30:00Z",
        "lastUpdated": "2024-01-25T10:30:00Z"
      }
    ],
    "requestedAt": "2024-01-25T10:30:00Z"
  },
  "isError": false
}
```

#### GET /api/oracle/rwa/equity/{symbol}/price/at-date
Get price at a specific date (for historical queries).

**Query Parameters:**
- `date` (required): Target date
- `adjusted` (optional): true/false, default: true

**Response:**
```json
{
  "result": {
    "symbol": "AAPL",
    "priceDate": "2020-07-31T00:00:00Z",
    "rawPrice": 400.00,
    "adjustedPrice": 100.00,
    "confidence": 0.94,
    "corporateActionsApplied": [
      {
        "type": "StockSplit",
        "effectiveDate": "2020-08-31T00:00:00Z",
        "adjustmentFactor": 0.25
      }
    ]
  },
  "isError": false
}
```

### 2. **DTOs**

```csharp
// Application/DTOs/EquityPrice/EquityPriceResponseDto.cs
public class EquityPriceResponseDto
{
    public string Symbol { get; set; }
    public decimal RawPrice { get; set; }
    public decimal AdjustedPrice { get; set; }
    public decimal Confidence { get; set; }
    public DateTime PriceDate { get; set; }
    public List<SourcePriceDto> Sources { get; set; }
    public List<CorporateActionInfoDto> CorporateActionsApplied { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class SourcePriceDto
{
    public string SourceName { get; set; }
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal ReliabilityScore { get; set; }
    public int LatencyMs { get; set; }
}

public class CorporateActionInfoDto
{
    public string Type { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal AdjustmentFactor { get; set; }
}

// Application/DTOs/EquityPrice/EquityPriceHistoryResponseDto.cs
public class EquityPriceHistoryResponseDto
{
    public string Symbol { get; set; }
    public List<EquityPriceDataPointDto> Prices { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public bool Adjusted { get; set; }
}

public class EquityPriceDataPointDto
{
    public DateTime PriceDate { get; set; }
    public decimal RawPrice { get; set; }
    public decimal AdjustedPrice { get; set; }
    public decimal Confidence { get; set; }
}

// Application/DTOs/EquityPrice/BatchEquityPriceResponseDto.cs
public class BatchEquityPriceResponseDto
{
    public List<EquityPriceSummaryDto> Prices { get; set; }
    public DateTime RequestedAt { get; set; }
}

public class EquityPriceSummaryDto
{
    public string Symbol { get; set; }
    public decimal RawPrice { get; set; }
    public decimal AdjustedPrice { get; set; }
    public decimal Confidence { get; set; }
    public DateTime PriceDate { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

### 3. **Controller**

```csharp
// API/Controllers/RwaOracle/EquityPricesController.cs

[ApiController]
[Route("api/oracle/rwa/equity")]
public class EquityPricesController : ControllerBase
{
    private readonly IEquityPriceService _equityPriceService;
    
    public EquityPricesController(IEquityPriceService equityPriceService)
    {
        _equityPriceService = equityPriceService;
    }
    
    [HttpGet("{symbol}/price")]
    [ResponseCache(Duration = 60)] // Cache for 1 minute
    public async Task<IActionResult> GetPrice(
        string symbol,
        [FromQuery] bool adjusted = true)
    {
        // Implementation
    }
    
    [HttpGet("{symbol}/price/history")]
    public async Task<IActionResult> GetPriceHistory(
        string symbol,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] bool adjusted = true,
        [FromQuery] string interval = "1d")
    {
        // Implementation
    }
    
    [HttpGet("prices/batch")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetBatchPrices(
        [FromQuery] string symbols,
        [FromQuery] bool adjusted = true)
    {
        // Implementation
    }
    
    [HttpGet("{symbol}/price/at-date")]
    public async Task<IActionResult> GetPriceAtDate(
        string symbol,
        [FromQuery] DateTime date,
        [FromQuery] bool adjusted = true)
    {
        // Implementation
    }
}
```

---

## 📁 Files to Create

```
API/Controllers/RwaOracle/
  └── EquityPricesController.cs

Application/DTOs/EquityPrice/
  ├── EquityPriceResponseDto.cs
  ├── SourcePriceDto.cs
  ├── CorporateActionInfoDto.cs
  ├── EquityPriceHistoryResponseDto.cs
  ├── EquityPriceDataPointDto.cs
  ├── BatchEquityPriceResponseDto.cs
  └── EquityPriceSummaryDto.cs

Application/Mappings/
  └── EquityPriceMappingProfile.cs (AutoMapper)
```

---

## 🔧 Implementation Steps

1. **Create DTOs**
   - Response DTOs
   - History DTOs
   - Batch DTOs
   - Nested DTOs

2. **Create AutoMapper Profile**
   - Map service responses to DTOs

3. **Create Controller**
   - Implement all GET endpoints
   - Add caching attributes
   - Add validation

4. **Add Business Logic**
   - Call equity price service
   - Handle adjusted vs raw prices
   - Handle historical queries
   - Handle batch queries

5. **Add Caching**
   - Response caching for current prices
   - Cache headers

6. **Add Error Handling**
   - Handle invalid symbols
   - Handle date range errors
   - Proper error responses

7. **Testing**
   - Unit tests
   - Integration tests
   - Test all endpoints
   - Test caching

---

## ✅ Acceptance Criteria

- [ ] All endpoints created and working
- [ ] GET /api/oracle/rwa/equity/{symbol}/price returns current price
- [ ] Adjusted prices calculated correctly
- [ ] Source breakdown included in response
- [ ] Historical prices queryable
- [ ] Batch queries work for multiple symbols
- [ ] Price at specific date works
- [ ] Caching headers set correctly
- [ ] Error handling works
- [ ] Unit tests with >80% coverage
- [ ] Integration tests pass

---

## 📊 Test Cases

### Test Case 1: Get Current Price

**Request:**
```
GET /api/oracle/rwa/equity/AAPL/price
```

**Expected:**
- Returns current price for AAPL
- Includes raw and adjusted prices
- Includes source breakdown
- Status: 200 OK
- Cache header present

### Test Case 2: Get Historical Prices

**Request:**
```
GET /api/oracle/rwa/equity/AAPL/price/history?from=2024-01-01&to=2024-01-31&adjusted=true
```

**Expected:**
- Returns price history for January
- All prices adjusted
- Status: 200 OK

### Test Case 3: Batch Query

**Request:**
```
GET /api/oracle/rwa/equity/prices/batch?symbols=AAPL,MSFT,GOOGL
```

**Expected:**
- Returns prices for all 3 symbols
- Status: 200 OK

### Test Case 4: Price at Date with Adjustment

**Request:**
```
GET /api/oracle/rwa/equity/AAPL/price/at-date?date=2020-07-31&adjusted=true
```

**Expected:**
- Returns price at that date
- Adjusted price accounts for 4-for-1 split
- Shows corporate actions applied
- Status: 200 OK

---

## 🔗 Related Tasks

- **Task 18:** Equity Price Feed Service (uses service from this task)
- **Task 22:** Database Schema (uses entities from this task)

---

## 📚 References

- ASP.NET Core Web API Documentation
- Response Caching in ASP.NET Core
- RESTful API Design Best Practices

---

## 💡 Notes

- Use ResponseCache attribute for current prices (short TTL)
- Don't cache historical prices (always fresh)
- Consider ETags for better caching
- Add rate limiting for batch queries
- Validate date ranges (don't allow future dates for historical)
- Limit batch query size (e.g., max 50 symbols)

---

**Last Updated:** January 2025  
**Assigned To:** [Agent Name]
