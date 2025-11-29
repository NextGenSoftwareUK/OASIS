# Agent C - iShip Integration Tasks

## Overview

You are responsible for integrating **iShip** - a carrier aggregation service that provides rate requests, label creation, and tracking functionality. You'll build the connector layer that communicates with iShip's API.

## What You're Building

The **iShip Connector** enables Shipex Pro to:
- Get shipping rates from multiple carriers
- Create shipments and generate shipping labels
- Track shipments and retrieve status updates
- Register webhooks for status changes

This connector is used by Agent E's RateService and ShipmentService.

## Architecture Context

```
┌─────────────────────────────────────────────────────────────┐
│         Shipex Pro Middleware (Core Service)                 │
│  • Rate Service         ← Uses your connector                │
│  • Shipment Service     ← Uses your connector                │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ Your Connector
                        ↓
┌─────────────────────────────────────────────────────────────┐
│          iShip API (External Service)                        │
│  • Rate requests                                             │
│  • Shipment creation                                         │
│  • Label generation                                          │
│  • Tracking                                                  │
└─────────────────────────────────────────────────────────────┘
```

**Your Role**: Build the HTTP client and connector service that translates between Shipex Pro's internal models and iShip's API.

---

## Reference Materials

1. **Main Implementation Plan**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_IMPLEMENTATION_PLAN.md`
2. **Task Breakdown**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_TASK_BREAKDOWN.md`
3. **Validation Framework**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_VALIDATION_FRAMEWORK.md`
4. **iShip API Documentation**: (You'll need to obtain this from the project stakeholders)

---

## Your Tasks

### Task 3.1: Create iShip API Client Base

**Priority**: CRITICAL - Foundation for all iShip operations  
**Dependencies**: Task 1.3 (Repository), Task 9.2 (API keys from Secret Vault - can hardcode initially)  
**Estimated Time**: 8 hours

#### What to Build

Create the base HTTP client that will communicate with iShip's API. This includes configuration, error handling, and base request/response structures.

#### Files to Create

1. **IShipApiClient.cs**
   - HTTP client wrapper
   - Base URL configuration
   - Authentication header management
   - Error handling
   - Retry logic base

2. **Request/Response Models** (in `Connectors/IShip/Models/`)
   - `IShipRateRequest.cs` - Request format for rate queries
   - `IShipRateResponse.cs` - Response format from iShip
   - `IShipShipmentRequest.cs` - Request format for shipment creation
   - `IShipShipmentResponse.cs` - Response format for shipments
   - `IShipTrackingResponse.cs` - Tracking response format

#### Implementation Details

- Use HttpClient or RestSharp for HTTP calls
- Handle API authentication (API key from Agent F's Secret Vault)
- Implement proper error handling with retry logic
- Use OASISResult pattern for responses
- Log all API calls for debugging

#### Example Structure

```csharp
public class IShipApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    
    public IShipApiClient(string baseUrl, string apiKey)
    {
        _baseUrl = baseUrl;
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
    
    protected async Task<OASISResult<T>> ExecuteRequestAsync<T>(
        HttpMethod method, 
        string endpoint, 
        object request = null)
    {
        // Implementation with error handling, retries, logging
    }
}
```

#### Acceptance Criteria

- [ ] HTTP client properly configured
- [ ] Authentication works
- [ ] Error handling implemented
- [ ] Request/response models defined
- [ ] Base retry logic in place

---

### Task 3.2: Implement iShip Rate Request

**Priority**: CRITICAL - Used by RateService  
**Dependencies**: Task 3.1  
**Estimated Time**: 8 hours

#### What to Build

Implement the rate request functionality that calls iShip's rate API and transforms the response into Shipex Pro's internal format.

#### Implementation

Create `IShipConnectorService.cs` with:

```csharp
public class IShipConnectorService
{
    private readonly IShipApiClient _apiClient;
    
    public async Task<OASISResult<List<RateQuote>>> GetRatesAsync(RateRequest request)
    {
        // 1. Transform internal RateRequest to iShip format
        // 2. Call iShip API
        // 3. Transform iShip response to internal RateQuote format
        // 4. Return OASISResult
    }
}
```

#### Request Transformation

Transform from Shipex Pro's `RateRequest` to iShip's API format:
- Dimensions → iShip format
- Weight → iShip format  
- Origin/Destination addresses → iShip format
- Service level → iShip service codes

#### Response Transformation

Transform from iShip's response to internal `RateQuote` format:
- Multiple carriers returned
- Each carrier has rate and estimated delivery
- Handle different service levels

#### Acceptance Criteria

- [ ] Rate requests successfully call iShip API
- [ ] Response properly transformed
- [ ] Multiple carriers supported
- [ ] Error handling works
- [ ] Retry logic works for failed requests

---

### Task 3.3: Implement iShip Shipment Creation

**Priority**: CRITICAL - Used by ShipmentService  
**Dependencies**: Task 3.1  
**Estimated Time**: 10 hours

#### What to Build

Implement shipment creation that:
1. Creates shipment via iShip API
2. Retrieves shipping label (PDF or base64)
3. Gets tracking number
4. Returns all data to ShipmentService

#### Implementation

```csharp
public async Task<OASISResult<ShipmentResponse>> CreateShipmentAsync(
    CreateShipmentRequest request)
{
    // 1. Transform to iShip shipment format
    // 2. Call iShip create shipment API
    // 3. Extract tracking number
    // 4. Retrieve label (PDF/base64)
    // 5. Return ShipmentResponse
}
```

#### Label Handling

- Label may come as PDF URL, base64 string, or signed URL
- Store in Shipment model (Agent A's repository)
- Support multiple formats

#### Acceptance Criteria

- [ ] Shipment creation works
- [ ] Tracking number retrieved
- [ ] Label retrieved (handle all formats)
- [ ] Error handling works

---

### Task 3.4: Implement iShip Tracking

**Priority**: MEDIUM  
**Dependencies**: Task 3.1  
**Estimated Time**: 6 hours

#### What to Build

Implement tracking lookup that retrieves shipment status and history from iShip.

#### Implementation

```csharp
public async Task<OASISResult<TrackingInfo>> TrackShipmentAsync(
    string trackingNumber)
{
    // 1. Call iShip tracking API
    // 2. Parse tracking status
    // 3. Extract tracking history
    // 4. Return TrackingInfo
}
```

#### Acceptance Criteria

- [ ] Tracking lookup works
- [ ] Status correctly parsed
- [ ] History retrieved
- [ ] Error handling works

---

### Task 3.5: Implement iShip Webhook Registration

**Priority**: MEDIUM  
**Dependencies**: Task 3.1, Task 8.1 (Webhook endpoint)  
**Estimated Time**: 4 hours

#### What to Build

Implement webhook registration so iShip can send status updates to Shipex Pro.

#### Implementation

```csharp
public async Task<OASISResult<bool>> RegisterWebhookAsync(
    string shipmentId, 
    string webhookUrl)
{
    // 1. Call iShip webhook registration API
    // 2. Configure webhook URL for shipment
    // 3. Return success/failure
}
```

#### Webhook URL Format

- Format: `https://yourdomain.com/api/shipexpro/webhooks/iship`
- iShip will POST status updates to this URL
- Agent D will handle receiving these webhooks

#### Acceptance Criteria

- [ ] Webhook registration works
- [ ] Webhook URL properly configured
- [ ] Error handling works

---

## Working with Other Agents

### Dependencies You Need

- **Agent A**: Repository interface (for any data storage needs)
- **Agent F**: Secret Vault service for iShip API key (can hardcode initially)

### Dependencies You Create

- **Agent E**: Will use your connector in RateService and ShipmentService
- **Agent D**: Will need to handle webhooks that come from iShip

### Communication Points

1. **After Task 3.1**: Share connector interface with Agent E
2. **After Task 3.2**: Test with Agent E's RateService
3. **Coordinate with Agent D**: Ensure webhook format matches expectations

---

## Success Criteria

You will know you've succeeded when:

1. ✅ All iShip API operations work correctly
2. ✅ Error handling is robust
3. ✅ Agent E can successfully use your connector
4. ✅ Webhooks are properly registered
5. ✅ All API responses are properly transformed

---

## Important Notes

- **API Documentation**: You'll need access to iShip's API documentation. Request this from project stakeholders if not provided.
- **Sandbox/Test Environment**: Use iShip's sandbox/test environment during development.
- **API Keys**: Initially hardcode API keys; later integrate with Agent F's Secret Vault.
- **Error Handling**: iShip API may have rate limits, timeouts, etc. Handle gracefully.

---

**Questions?** Refer to the main implementation plan or coordinate with Agent E who will use your connector.

**Ready to start?** Begin with Task 3.1 to build the foundation.
