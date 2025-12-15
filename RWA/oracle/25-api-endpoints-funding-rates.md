# Task 25: API Endpoints - Funding Rates

**Status:** 🟡 **Pending**  
**Priority:** ⭐ Critical  
**Estimated Time:** 3-5 days  
**Dependencies:** Task 19, Task 20, Task 22

---

## 📋 Overview

Create REST API endpoints for funding rates. Provide endpoints to get current funding rates, funding rate history, and on-chain status.

---

## ✅ Objectives

1. Create GET endpoints for funding rates
2. Add funding rate history queries
3. Add batch funding rate queries
4. Include on-chain status in responses
5. Add response DTOs with rate factors
6. Add caching for current rates

---

## 🎯 Requirements

### 1. **Endpoints**

#### GET /api/oracle/rwa/funding/{symbol}/rate
Get current funding rate for a symbol.

**Response:**
```json
{
  "result": {
    "symbol": "AAPL",
    "rate": 0.20,
    "hourlyRate": 0.0000228,
    "markPrice": 152.50,
    "spotPrice": 150.00,
    "adjustedSpotPrice": 150.00,
    "premium": 2.50,
    "premiumPercentage": 1.67,
    "factors": {
      "baseRate": 0.167,
      "corporateActionAdjustment": 0.0,
      "liquidityAdjustment": 0.06,
      "volatilityAdjustment": 0.01,
      "finalRate": 0.20
    },
    "calculatedAt": "2024-01-25T10:00:00Z",
    "validUntil": "2024-01-25T11:00:00Z",
    "onChainStatus": {
      "published": true,
      "transactionHash": "tx-hash",
      "accountAddress": "pda-address",
      "lastPublishedAt": "2024-01-25T10:00:05Z"
    }
  },
  "isError": false
}
```

#### GET /api/oracle/rwa/funding/{symbol}/rate/history
Get funding rate history for a symbol.

**Query Parameters:**
- `hours` (optional): Number of hours to look back (default: 24, max: 168)

**Response:**
```json
{
  "result": {
    "symbol": "AAPL",
    "rates": [
      {
        "rate": 0.20,
        "hourlyRate": 0.0000228,
        "markPrice": 152.50,
        "spotPrice": 150.00,
        "premium": 2.50,
        "calculatedAt": "2024-01-25T10:00:00Z",
        "validUntil": "2024-01-25T11:00:00Z"
      },
      {
        "rate": 0.18,
        "hourlyRate": 0.0000205,
        "markPrice": 151.00,
        "spotPrice": 150.00,
        "premium": 1.00,
        "calculatedAt": "2024-01-25T09:00:00Z",
        "validUntil": "2024-01-25T10:00:00Z"
      }
    ],
    "fromDate": "2024-01-24T10:00:00Z",
    "toDate": "2024-01-25T10:00:00Z"
  },
  "isError": false
}
```

#### GET /api/oracle/rwa/funding/rates/batch
Get funding rates for multiple symbols.

**Query Parameters:**
- `symbols` (required): Comma-separated list

**Response:**
```json
{
  "result": {
    "rates": [
      {
        "symbol": "AAPL",
        "rate": 0.20,
        "hourlyRate": 0.0000228,
        "calculatedAt": "2024-01-25T10:00:00Z",
        "validUntil": "2024-01-25T11:00:00Z",
        "onChainPublished": true
      },
      {
        "symbol": "MSFT",
        "rate": 0.15,
        "hourlyRate": 0.0000171,
        "calculatedAt": "2024-01-25T10:00:00Z",
        "validUntil": "2024-01-25T11:00:00Z",
        "onChainPublished": true
      }
    ],
    "requestedAt": "2024-01-25T10:30:00Z"
  },
  "isError": false
}
```

#### POST /api/oracle/rwa/funding/{symbol}/publish-onchain
Manually trigger on-chain publishing (admin only).

**Response:**
```json
{
  "result": {
    "symbol": "AAPL",
    "transactionHash": "tx-hash",
    "accountAddress": "pda-address",
    "publishedAt": "2024-01-25T10:30:00Z"
  },
  "isError": false
}
```

### 2. **DTOs**

```csharp
// Application/DTOs/FundingRate/FundingRateResponseDto.cs
public class FundingRateResponseDto
{
    public string Symbol { get; set; }
    public decimal Rate { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal MarkPrice { get; set; }
    public decimal SpotPrice { get; set; }
    public decimal AdjustedSpotPrice { get; set; }
    public decimal Premium { get; set; }
    public decimal PremiumPercentage { get; set; }
    public FundingRateFactorsDto Factors { get; set; }
    public DateTime CalculatedAt { get; set; }
    public DateTime ValidUntil { get; set; }
    public OnChainStatusDto? OnChainStatus { get; set; }
}

public class FundingRateFactorsDto
{
    public decimal BaseRate { get; set; }
    public decimal CorporateActionAdjustment { get; set; }
    public decimal LiquidityAdjustment { get; set; }
    public decimal VolatilityAdjustment { get; set; }
    public decimal FinalRate { get; set; }
}

public class OnChainStatusDto
{
    public bool Published { get; set; }
    public string? TransactionHash { get; set; }
    public string? AccountAddress { get; set; }
    public DateTime? LastPublishedAt { get; set; }
}

// Application/DTOs/FundingRate/FundingRateHistoryResponseDto.cs
public class FundingRateHistoryResponseDto
{
    public string Symbol { get; set; }
    public List<FundingRateHistoryItemDto> Rates { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

public class FundingRateHistoryItemDto
{
    public decimal Rate { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal MarkPrice { get; set; }
    public decimal SpotPrice { get; set; }
    public decimal Premium { get; set; }
    public DateTime CalculatedAt { get; set; }
    public DateTime ValidUntil { get; set; }
}

// Application/DTOs/FundingRate/BatchFundingRateResponseDto.cs
public class BatchFundingRateResponseDto
{
    public List<FundingRateSummaryDto> Rates { get; set; }
    public DateTime RequestedAt { get; set; }
}

public class FundingRateSummaryDto
{
    public string Symbol { get; set; }
    public decimal Rate { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime CalculatedAt { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool OnChainPublished { get; set; }
}
```

### 3. **Controller**

```csharp
// API/Controllers/RwaOracle/FundingRatesController.cs

[ApiController]
[Route("api/oracle/rwa/funding")]
public class FundingRatesController : ControllerBase
{
    private readonly IFundingRateService _fundingRateService;
    private readonly IOnChainFundingPublisher _onChainPublisher;
    
    public FundingRatesController(
        IFundingRateService fundingRateService,
        IOnChainFundingPublisher onChainPublisher)
    {
        _fundingRateService = fundingRateService;
        _onChainPublisher = onChainPublisher;
    }
    
    [HttpGet("{symbol}/rate")]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    public async Task<IActionResult> GetFundingRate(string symbol)
    {
        // Implementation
    }
    
    [HttpGet("{symbol}/rate/history")]
    public async Task<IActionResult> GetFundingRateHistory(
        string symbol,
        [FromQuery] int hours = 24)
    {
        // Implementation
    }
    
    [HttpGet("rates/batch")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetBatchFundingRates(
        [FromQuery] string symbols)
    {
        // Implementation
    }
    
    [HttpPost("{symbol}/publish-onchain")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PublishOnChain(string symbol)
    {
        // Implementation
    }
}
```

---

## 📁 Files to Create

```
API/Controllers/RwaOracle/
  └── FundingRatesController.cs

Application/DTOs/FundingRate/
  ├── FundingRateResponseDto.cs
  ├── FundingRateFactorsDto.cs
  ├── OnChainStatusDto.cs
  ├── FundingRateHistoryResponseDto.cs
  ├── FundingRateHistoryItemDto.cs
  ├── BatchFundingRateResponseDto.cs
  └── FundingRateSummaryDto.cs

Application/Mappings/
  └── FundingRateMappingProfile.cs (AutoMapper)
```

---

## 🔧 Implementation Steps

1. **Create DTOs**
   - Response DTOs
   - History DTOs
   - Batch DTOs
   - On-chain status DTOs

2. **Create AutoMapper Profile**
   - Map service responses to DTOs

3. **Create Controller**
   - Implement all endpoints
   - Add caching for current rates
   - Add authorization for admin endpoints

4. **Integrate Services**
   - Call funding rate service
   - Call on-chain publisher for status
   - Handle on-chain publishing

5. **Add Error Handling**
   - Handle invalid symbols
   - Handle calculation errors
   - Handle on-chain publishing failures

6. **Testing**
   - Unit tests
   - Integration tests
   - Test all endpoints
   - Test on-chain integration

---

## ✅ Acceptance Criteria

- [ ] All endpoints created and working
- [ ] GET /api/oracle/rwa/funding/{symbol}/rate returns current rate
- [ ] Funding rate factors included in response
- [ ] On-chain status included in response
- [ ] Historical rates queryable
- [ ] Batch queries work
- [ ] On-chain publishing works (admin)
- [ ] Caching works correctly
- [ ] Error handling works
- [ ] Unit tests with >80% coverage
- [ ] Integration tests pass

---

## 📊 Test Cases

### Test Case 1: Get Current Funding Rate

**Request:**
```
GET /api/oracle/rwa/funding/AAPL/rate
```

**Expected:**
- Returns current funding rate for AAPL
- Includes all factors
- Includes on-chain status
- Status: 200 OK

### Test Case 2: Get Funding Rate History

**Request:**
```
GET /api/oracle/rwa/funding/AAPL/rate/history?hours=48
```

**Expected:**
- Returns funding rates for last 48 hours
- One entry per hour
- Status: 200 OK

### Test Case 3: Batch Query

**Request:**
```
GET /api/oracle/rwa/funding/rates/batch?symbols=AAPL,MSFT
```

**Expected:**
- Returns funding rates for both symbols
- Status: 200 OK

### Test Case 4: Publish On-Chain (Admin)

**Request:**
```
POST /api/oracle/rwa/funding/AAPL/publish-onchain
Authorization: Bearer [admin-token]
```

**Expected:**
- Publishes funding rate to Solana
- Returns transaction hash
- Status: 200 OK

---

## 🔗 Related Tasks

- **Task 19:** Funding Rate Calculation Service (uses service from this task)
- **Task 20:** Funding Rate On-Chain Publishing (uses publisher from this task)
- **Task 22:** Database Schema (uses entities from this task)

---

## 📚 References

- ASP.NET Core Web API Documentation
- RESTful API Design Best Practices

---

## 💡 Notes

- Cache current funding rates for 5 minutes (rates update hourly)
- Don't cache historical rates
- Include on-chain status in all responses
- Consider WebSocket for real-time funding rate updates
- Add rate limiting for batch queries

---

**Last Updated:** January 2025  
**Assigned To:** [Agent Name]
