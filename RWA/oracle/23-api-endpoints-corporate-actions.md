# Task 23: API Endpoints - Corporate Actions

**Status:** 🟡 **Pending**  
**Priority:** ⭐ Critical  
**Estimated Time:** 3-5 days  
**Dependencies:** Task 16, Task 22

---

## 📋 Overview

Create REST API endpoints for corporate actions. Provide endpoints to query corporate actions, get upcoming actions, and manage corporate action data.

---

## ✅ Objectives

1. Create GET endpoints for querying corporate actions
2. Create POST endpoint for manually adding corporate actions (admin)
3. Add filtering and pagination
4. Add response DTOs
5. Add input validation
6. Add error handling

---

## 🎯 Requirements

### 1. **Endpoints**

#### GET /api/oracle/rwa/corporate-actions/{symbol}
Get all corporate actions for a symbol.

**Query Parameters:**
- `fromDate` (optional): Filter actions from this date
- `toDate` (optional): Filter actions to this date
- `type` (optional): Filter by action type (Split, Dividend, etc.)
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 20, max: 100)

**Response:**
```json
{
  "result": {
    "symbol": "AAPL",
    "corporateActions": [
      {
        "id": "guid",
        "type": "StockSplit",
        "exDate": "2024-08-31T00:00:00Z",
        "recordDate": "2024-08-30T00:00:00Z",
        "effectiveDate": "2024-09-03T00:00:00Z",
        "splitRatio": 4.0,
        "dividendAmount": null,
        "acquiringSymbol": null,
        "exchangeRatio": null,
        "dataSource": "IEX Cloud",
        "isVerified": true,
        "createdAt": "2024-08-15T10:30:00Z"
      }
    ],
    "totalCount": 5,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  },
  "isError": false
}
```

#### GET /api/oracle/rwa/corporate-actions/{symbol}/upcoming
Get upcoming corporate actions for a symbol.

**Query Parameters:**
- `daysAhead` (optional): Days to look ahead (default: 30, max: 90)

**Response:**
```json
{
  "result": {
    "symbol": "AAPL",
    "upcomingActions": [
      {
        "id": "guid",
        "type": "StockSplit",
        "exDate": "2024-08-31T00:00:00Z",
        "effectiveDate": "2024-09-03T00:00:00Z",
        "splitRatio": 4.0,
        "daysUntil": 15
      }
    ]
  },
  "isError": false
}
```

#### POST /api/oracle/rwa/corporate-actions
Manually add a corporate action (admin only).

**Request:**
```json
{
  "symbol": "AAPL",
  "type": "StockSplit",
  "exDate": "2024-08-31T00:00:00Z",
  "recordDate": "2024-08-30T00:00:00Z",
  "effectiveDate": "2024-09-03T00:00:00Z",
  "splitRatio": 4.0,
  "dataSource": "Manual Entry"
}
```

**Response:**
```json
{
  "result": {
    "id": "guid",
    "symbol": "AAPL",
    "type": "StockSplit",
    "createdAt": "2024-01-25T10:30:00Z"
  },
  "isError": false
}
```

#### GET /api/oracle/rwa/corporate-actions/{id}
Get a specific corporate action by ID.

**Response:**
```json
{
  "result": {
    "id": "guid",
    "symbol": "AAPL",
    "type": "StockSplit",
    "exDate": "2024-08-31T00:00:00Z",
    "recordDate": "2024-08-30T00:00:00Z",
    "effectiveDate": "2024-09-03T00:00:00Z",
    "splitRatio": 4.0,
    "dataSource": "IEX Cloud",
    "isVerified": true,
    "createdAt": "2024-08-15T10:30:00Z",
    "updatedAt": "2024-08-16T10:30:00Z"
  },
  "isError": false
}
```

### 2. **DTOs**

```csharp
// Application/DTOs/CorporateAction/CorporateActionResponseDto.cs
public class CorporateActionResponseDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public string Type { get; set; }
    public DateTime ExDate { get; set; }
    public DateTime RecordDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal? SplitRatio { get; set; }
    public decimal? DividendAmount { get; set; }
    public string? DividendCurrency { get; set; }
    public string? AcquiringSymbol { get; set; }
    public decimal? ExchangeRatio { get; set; }
    public string DataSource { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Application/DTOs/CorporateAction/CorporateActionListResponseDto.cs
public class CorporateActionListResponseDto
{
    public string Symbol { get; set; }
    public List<CorporateActionResponseDto> CorporateActions { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

// Application/DTOs/CorporateAction/UpcomingCorporateActionResponseDto.cs
public class UpcomingCorporateActionResponseDto
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public DateTime ExDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal? SplitRatio { get; set; }
    public decimal? DividendAmount { get; set; }
    public int DaysUntil { get; set; }
}

// Application/DTOs/CorporateAction/CreateCorporateActionRequestDto.cs
public class CreateCorporateActionRequestDto
{
    [Required]
    [MaxLength(20)]
    public string Symbol { get; set; }
    
    [Required]
    public CorporateActionType Type { get; set; }
    
    [Required]
    public DateTime ExDate { get; set; }
    
    [Required]
    public DateTime RecordDate { get; set; }
    
    [Required]
    public DateTime EffectiveDate { get; set; }
    
    public decimal? SplitRatio { get; set; }
    public decimal? DividendAmount { get; set; }
    public string? DividendCurrency { get; set; }
    public string? AcquiringSymbol { get; set; }
    public decimal? ExchangeRatio { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string DataSource { get; set; }
}
```

### 3. **Controller**

```csharp
// API/Controllers/RwaOracle/CorporateActionsController.cs

[ApiController]
[Route("api/oracle/rwa/corporate-actions")]
[Authorize] // Require authentication
public class CorporateActionsController : ControllerBase
{
    private readonly ICorporateActionService _corporateActionService;
    
    public CorporateActionsController(ICorporateActionService corporateActionService)
    {
        _corporateActionService = corporateActionService;
    }
    
    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetCorporateActions(
        string symbol,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] CorporateActionType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Implementation
    }
    
    [HttpGet("{symbol}/upcoming")]
    public async Task<IActionResult> GetUpcomingCorporateActions(
        string symbol,
        [FromQuery] int daysAhead = 30)
    {
        // Implementation
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCorporateAction(Guid id)
    {
        // Implementation
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")] // Admin only
    public async Task<IActionResult> CreateCorporateAction(
        [FromBody] CreateCorporateActionRequestDto request)
    {
        // Implementation
    }
}
```

---

## 📁 Files to Create

```
API/Controllers/RwaOracle/
  └── CorporateActionsController.cs

Application/DTOs/CorporateAction/
  ├── CorporateActionResponseDto.cs
  ├── CorporateActionListResponseDto.cs
  ├── UpcomingCorporateActionResponseDto.cs
  └── CreateCorporateActionRequestDto.cs

Application/Mappings/
  └── CorporateActionMappingProfile.cs (AutoMapper)
```

---

## 🔧 Implementation Steps

1. **Create DTOs**
   - Response DTOs
   - Request DTOs
   - List response DTOs with pagination

2. **Create AutoMapper Profile**
   - Map Entity -> ResponseDto
   - Map RequestDto -> Entity

3. **Create Controller**
   - Implement GET endpoints
   - Implement POST endpoint
   - Add authorization attributes
   - Add validation attributes

4. **Add Business Logic**
   - Query corporate actions with filters
   - Pagination logic
   - Date filtering
   - Type filtering

5. **Add Validation**
   - Input validation on request DTOs
   - Business rule validation
   - Error responses

6. **Add Error Handling**
   - Try-catch blocks
   - Proper error responses
   - Logging

7. **Testing**
   - Unit tests for controller
   - Integration tests
   - Test all endpoints
   - Test error cases

---

## ✅ Acceptance Criteria

- [ ] All endpoints created and working
- [ ] GET /api/oracle/rwa/corporate-actions/{symbol} returns data
- [ ] GET /api/oracle/rwa/corporate-actions/{symbol}/upcoming returns upcoming actions
- [ ] GET /api/oracle/rwa/corporate-actions/{id} returns single action
- [ ] POST /api/oracle/rwa/corporate-actions creates new action (admin only)
- [ ] Pagination works correctly
- [ ] Filtering by date works
- [ ] Filtering by type works
- [ ] Input validation works
- [ ] Error handling works
- [ ] Authorization works (admin for POST)
- [ ] Unit tests with >80% coverage
- [ ] Integration tests pass

---

## 📊 Test Cases

### Test Case 1: Get Corporate Actions

**Request:**
```
GET /api/oracle/rwa/corporate-actions/AAPL
```

**Expected:**
- Returns list of corporate actions for AAPL
- Includes pagination metadata
- Status: 200 OK

### Test Case 2: Get Upcoming Actions

**Request:**
```
GET /api/oracle/rwa/corporate-actions/AAPL/upcoming?daysAhead=30
```

**Expected:**
- Returns only upcoming actions within 30 days
- Includes daysUntil field
- Status: 200 OK

### Test Case 3: Filter by Type

**Request:**
```
GET /api/oracle/rwa/corporate-actions/AAPL?type=StockSplit
```

**Expected:**
- Returns only stock splits
- Status: 200 OK

### Test Case 4: Create Corporate Action (Admin)

**Request:**
```
POST /api/oracle/rwa/corporate-actions
Authorization: Bearer [admin-token]
Body: { ... }
```

**Expected:**
- Creates new corporate action
- Returns created action
- Status: 201 Created

### Test Case 5: Unauthorized Access

**Request:**
```
POST /api/oracle/rwa/corporate-actions
Authorization: Bearer [user-token] (not admin)
```

**Expected:**
- Returns 403 Forbidden
- Error message indicates insufficient permissions

---

## 🔗 Related Tasks

- **Task 16:** Corporate Action Data Source Integration (uses service from this task)
- **Task 22:** Database Schema (uses entities from this task)

---

## 📚 References

- ASP.NET Core Web API Documentation
- RESTful API Design Best Practices
- AutoMapper Documentation

---

## 💡 Notes

- Use standard OASIS API response format (result, isError, message)
- Add Swagger/OpenAPI documentation
- Consider caching for frequently accessed endpoints
- Add rate limiting if needed
- Use async/await for all database operations

---

**Last Updated:** January 2025  
**Assigned To:** [Agent Name]
