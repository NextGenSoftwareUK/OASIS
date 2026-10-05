# Task 26: API Endpoints - Risk Management

**Status:** 🟡 **Pending**  
**Priority:** ⭐ Critical  
**Estimated Time:** 3-5 days  
**Dependencies:** Task 21, Task 22

---

## 📋 Overview

Create REST API endpoints for risk management. Provide endpoints to get risk assessments, risk windows, and risk recommendations (deleveraging and return-to-baseline).

---

## ✅ Objectives

1. Create GET endpoints for risk assessments
2. Create GET endpoints for risk windows
3. Create GET endpoints for risk recommendations
4. Create POST endpoint for acknowledging recommendations
5. Add response DTOs
6. Add filtering and pagination

---

## 🎯 Requirements

### 1. **Endpoints**

#### GET /api/oracle/rwa/risk/{symbol}/assessment
Get current risk assessment for a symbol.

**Query Parameters:**
- `positionId` (optional): Include position-specific assessment

**Response:**
```json
{
  "result": {
    "symbol": "AAPL",
    "level": "High",
    "riskScore": 75,
    "currentLeverage": 8.0,
    "recommendedLeverage": 2.5,
    "factors": [
      {
        "type": "CorporateAction",
        "description": "Stock split effective 2024-08-31",
        "impact": 0.8,
        "effectiveDate": "2024-08-31T00:00:00Z"
      },
      {
        "type": "HighVolatility",
        "description": "High volatility: 45%",
        "impact": 0.9,
        "effectiveDate": "2024-01-25T00:00:00Z"
      }
    ],
    "activeRiskWindow": {
      "id": "guid",
      "startDate": "2024-08-28T00:00:00Z",
      "endDate": "2024-09-03T00:00:00Z",
      "level": "High"
    },
    "recommendations": [
      {
        "id": "guid",
        "action": "Deleverage",
        "targetLeverage": 2.5,
        "reductionPercentage": 68.75,
        "reason": "Risk window identified: Corporate action effective 2024-08-31",
        "priority": "High"
      }
    ],
    "assessedAt": "2024-01-25T10:30:00Z"
  },
  "isError": false
}
```

#### GET /api/oracle/rwa/risk/{symbol}/window
Get active or upcoming risk window for a symbol.

**Query Parameters:**
- `date` (optional): Check for risk window at this date (default: now)

**Response:**
```json
{
  "result": {
    "symbol": "AAPL",
    "hasActiveWindow": true,
    "riskWindow": {
      "id": "guid",
      "level": "High",
      "startDate": "2024-08-28T00:00:00Z",
      "endDate": "2024-09-03T00:00:00Z",
      "factors": [
        {
          "type": "CorporateAction",
          "description": "Stock split effective 2024-08-31",
          "impact": 0.8,
          "effectiveDate": "2024-08-31T00:00:00Z"
        }
      ],
      "createdAt": "2024-08-15T10:00:00Z"
    }
  },
  "isError": false
}
```

#### GET /api/oracle/rwa/risk/{symbol}/recommendations
Get risk recommendations for a symbol.

**Query Parameters:**
- `positionId` (optional): Filter by position
- `acknowledged` (optional): Filter by acknowledged status (true/false)
- `action` (optional): Filter by action type (Deleverage, ReturnToBaseline, etc.)
- `page` (optional): Page number
- `pageSize` (optional): Items per page

**Response:**
```json
{
  "result": {
    "symbol": "AAPL",
    "recommendations": [
      {
        "id": "guid",
        "positionId": "position-123",
        "action": "Deleverage",
        "currentLeverage": 8.0,
        "targetLeverage": 2.5,
        "reductionPercentage": 68.75,
        "reason": "Risk window identified: Corporate action effective 2024-08-31",
        "priority": "High",
        "recommendedBy": "2024-01-25T10:00:00Z",
        "validUntil": "2024-09-03T00:00:00Z",
        "acknowledged": false,
        "relatedFactors": [
          {
            "type": "CorporateAction",
            "description": "Stock split effective 2024-08-31",
            "impact": 0.8
          }
        ]
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  },
  "isError": false
}
```

#### GET /api/oracle/rwa/risk/recommendations/return-to-baseline
Get return-to-baseline recommendations for all symbols (or filtered).

**Query Parameters:**
- `symbols` (optional): Comma-separated list of symbols
- `acknowledged` (optional): Filter by acknowledged status

**Response:**
```json
{
  "result": {
    "recommendations": [
      {
        "id": "guid",
        "symbol": "AAPL",
        "positionId": "position-123",
        "action": "ReturnToBaseline",
        "currentLeverage": 2.0,
        "targetLeverage": 5.0,
        "increasePercentage": 150.0,
        "reason": "Risk window has passed, returning to baseline leverage",
        "priority": "Low",
        "recommendedBy": "2024-01-25T10:00:00Z",
        "acknowledged": false
      }
    ]
  },
  "isError": false
}
```

#### POST /api/oracle/rwa/risk/recommendation/{id}/acknowledge
Acknowledge a risk recommendation.

**Response:**
```json
{
  "result": {
    "id": "guid",
    "acknowledged": true,
    "acknowledgedAt": "2024-01-25T10:35:00Z",
    "acknowledgedBy": "user-id"
  },
  "isError": false
}
```

#### GET /api/oracle/rwa/risk/windows/active
Get all active risk windows.

**Query Parameters:**
- `symbols` (optional): Comma-separated list
- `level` (optional): Filter by risk level (Low, Medium, High, Critical)

**Response:**
```json
{
  "result": {
    "riskWindows": [
      {
        "symbol": "AAPL",
        "level": "High",
        "startDate": "2024-08-28T00:00:00Z",
        "endDate": "2024-09-03T00:00:00Z",
        "factors": [...]
      }
    ],
    "totalCount": 1
  },
  "isError": false
}
```

### 2. **DTOs**

```csharp
// Application/DTOs/RiskManagement/RiskAssessmentResponseDto.cs
public class RiskAssessmentResponseDto
{
    public string Symbol { get; set; }
    public string Level { get; set; }
    public decimal RiskScore { get; set; }
    public decimal CurrentLeverage { get; set; }
    public decimal RecommendedLeverage { get; set; }
    public List<RiskFactorDto> Factors { get; set; }
    public RiskWindowSummaryDto? ActiveRiskWindow { get; set; }
    public List<RiskRecommendationSummaryDto> Recommendations { get; set; }
    public DateTime AssessedAt { get; set; }
}

public class RiskFactorDto
{
    public string Type { get; set; }
    public string Description { get; set; }
    public decimal Impact { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class RiskWindowSummaryDto
{
    public Guid Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Level { get; set; }
}

// Application/DTOs/RiskManagement/RiskWindowResponseDto.cs
public class RiskWindowResponseDto
{
    public string Symbol { get; set; }
    public bool HasActiveWindow { get; set; }
    public RiskWindowDetailDto? RiskWindow { get; set; }
}

public class RiskWindowDetailDto
{
    public Guid Id { get; set; }
    public string Level { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<RiskFactorDto> Factors { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Application/DTOs/RiskManagement/RiskRecommendationResponseDto.cs
public class RiskRecommendationResponseDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public string? PositionId { get; set; }
    public string Action { get; set; }
    public decimal CurrentLeverage { get; set; }
    public decimal TargetLeverage { get; set; }
    public decimal? ReductionPercentage { get; set; }
    public decimal? IncreasePercentage { get; set; }
    public string Reason { get; set; }
    public string Priority { get; set; }
    public DateTime RecommendedBy { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool Acknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public List<RiskFactorDto> RelatedFactors { get; set; }
}

// Application/DTOs/RiskManagement/RiskRecommendationListResponseDto.cs
public class RiskRecommendationListResponseDto
{
    public string Symbol { get; set; }
    public List<RiskRecommendationResponseDto> Recommendations { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

### 3. **Controller**

```csharp
// API/Controllers/RwaOracle/RiskManagementController.cs

[ApiController]
[Route("api/oracle/rwa/risk")]
[Authorize]
public class RiskManagementController : ControllerBase
{
    private readonly IRiskAssessmentService _riskAssessmentService;
    private readonly IRiskWindowService _riskWindowService;
    private readonly IRiskRecommendationService _riskRecommendationService;
    
    public RiskManagementController(
        IRiskAssessmentService riskAssessmentService,
        IRiskWindowService riskWindowService,
        IRiskRecommendationService riskRecommendationService)
    {
        _riskAssessmentService = riskAssessmentService;
        _riskWindowService = riskWindowService;
        _riskRecommendationService = riskRecommendationService;
    }
    
    [HttpGet("{symbol}/assessment")]
    public async Task<IActionResult> GetRiskAssessment(
        string symbol,
        [FromQuery] string? positionId = null)
    {
        // Implementation
    }
    
    [HttpGet("{symbol}/window")]
    public async Task<IActionResult> GetRiskWindow(
        string symbol,
        [FromQuery] DateTime? date = null)
    {
        // Implementation
    }
    
    [HttpGet("{symbol}/recommendations")]
    public async Task<IActionResult> GetRecommendations(
        string symbol,
        [FromQuery] string? positionId = null,
        [FromQuery] bool? acknowledged = null,
        [FromQuery] RiskAction? action = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Implementation
    }
    
    [HttpGet("recommendations/return-to-baseline")]
    public async Task<IActionResult> GetReturnToBaselineRecommendations(
        [FromQuery] string? symbols = null,
        [FromQuery] bool? acknowledged = null)
    {
        // Implementation
    }
    
    [HttpPost("recommendation/{id}/acknowledge")]
    public async Task<IActionResult> AcknowledgeRecommendation(Guid id)
    {
        // Implementation
    }
    
    [HttpGet("windows/active")]
    public async Task<IActionResult> GetActiveRiskWindows(
        [FromQuery] string? symbols = null,
        [FromQuery] RiskLevel? level = null)
    {
        // Implementation
    }
}
```

---

## 📁 Files to Create

```
API/Controllers/RwaOracle/
  └── RiskManagementController.cs

Application/DTOs/RiskManagement/
  ├── RiskAssessmentResponseDto.cs
  ├── RiskFactorDto.cs
  ├── RiskWindowSummaryDto.cs
  ├── RiskRecommendationSummaryDto.cs
  ├── RiskWindowResponseDto.cs
  ├── RiskWindowDetailDto.cs
  ├── RiskRecommendationResponseDto.cs
  └── RiskRecommendationListResponseDto.cs

Application/Mappings/
  └── RiskManagementMappingProfile.cs (AutoMapper)
```

---

## 🔧 Implementation Steps

1. **Create DTOs**
   - All response DTOs
   - List response DTOs with pagination

2. **Create AutoMapper Profile**
   - Map entities to DTOs

3. **Create Controller**
   - Implement all endpoints
   - Add authorization
   - Add validation

4. **Integrate Services**
   - Call risk assessment service
   - Call risk window service
   - Call risk recommendation service

5. **Add Filtering & Pagination**
   - Implement filters
   - Implement pagination

6. **Add Error Handling**
   - Handle invalid symbols
   - Handle missing data
   - Proper error responses

7. **Testing**
   - Unit tests
   - Integration tests
   - Test all endpoints

---

## ✅ Acceptance Criteria

- [ ] All endpoints created and working
- [ ] Risk assessments returned correctly
- [ ] Risk windows queried correctly
- [ ] Risk recommendations returned correctly
- [ ] Recommendations can be acknowledged
- [ ] Filtering works (position, acknowledged, action)
- [ ] Pagination works
- [ ] Authorization works
- [ ] Error handling works
- [ ] Unit tests with >80% coverage
- [ ] Integration tests pass

---

## 📊 Test Cases

### Test Case 1: Get Risk Assessment

**Request:**
```
GET /api/oracle/rwa/risk/AAPL/assessment
```

**Expected:**
- Returns risk assessment for AAPL
- Includes risk level, score, leverage recommendations
- Includes active risk window (if any)
- Includes recommendations
- Status: 200 OK

### Test Case 2: Get Risk Window

**Request:**
```
GET /api/oracle/rwa/risk/AAPL/window
```

**Expected:**
- Returns active or upcoming risk window
- Includes factors
- Status: 200 OK

### Test Case 3: Get Recommendations

**Request:**
```
GET /api/oracle/rwa/risk/AAPL/recommendations?acknowledged=false
```

**Expected:**
- Returns unacknowledged recommendations
- Includes pagination
- Status: 200 OK

### Test Case 4: Acknowledge Recommendation

**Request:**
```
POST /api/oracle/rwa/risk/recommendation/{id}/acknowledge
Authorization: Bearer [token]
```

**Expected:**
- Recommendation marked as acknowledged
- AcknowledgedAt set
- Status: 200 OK

---

## 🔗 Related Tasks

- **Task 21:** Risk Management Module (uses services from this task)
- **Task 22:** Database Schema (uses entities from this task)

---

## 📚 References

- ASP.NET Core Web API Documentation
- RESTful API Design Best Practices

---

## 💡 Notes

- All endpoints require authentication
- Consider adding user context for acknowledgments
- Add WebSocket support for real-time risk updates
- Consider caching risk assessments (short TTL)
- Add rate limiting

---

**Last Updated:** January 2025  
**Assigned To:** [Agent Name]
