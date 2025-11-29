# Agent D - Shipox Integration & Webhooks Tasks

## Overview

You are responsible for integrating **Shipox** (order management platform) and building the **Webhook System** that processes status updates from external services. You'll create API endpoints for Shipox's UI and handle incoming webhooks.

## What You're Building

1. **Shipox Integration**: API client and endpoints for Shipox's order management platform
2. **Webhook System**: Receive and process webhooks from iShip and Shipox
3. **Shipox UI Endpoints**: REST endpoints that Shipox's frontend will consume

## Architecture Context

```
┌─────────────────────────────────────────────────────────────┐
│              Shipox UI (Frontend)                            │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ Your API Endpoints
                        ↓
┌─────────────────────────────────────────────────────────────┐
│         Shipex Pro Middleware (Core Service)                 │
│  • Shipox Controller         ← You build                    │
│  • Webhook Controller        ← You build                    │
│  • Webhook Service           ← You build                    │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ External Services
                        ↓
┌───────────────────┐         ┌──────────────────────────────┐
│  Shipox Platform  │         │      iShip Platform          │
│  (sends webhooks) │         │      (sends webhooks)        │
└───────────────────┘         └──────────────────────────────┘
```

**Your Role**: Build Shipox API client, webhook receiver endpoints, and webhook processing logic.

---

## Reference Materials

1. **Main Implementation Plan**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_IMPLEMENTATION_PLAN.md`
2. **Task Breakdown**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_TASK_BREAKDOWN.md`
3. **Validation Framework**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_VALIDATION_FRAMEWORK.md`
4. **Shipox API Documentation**: [Shipox API Docs](https://zip24docs.notion.site/Shipox-APIs-Documentation-1d96cf7fe3848094a014cc07c23649d7)

---

## Your Tasks

### Task 4.1: Create Shipox API Client Base

**Priority**: HIGH - Foundation for Shipox operations  
**Dependencies**: Task 1.3 (Repository), Task 9.2 (API keys - can hardcode initially)  
**Estimated Time**: 10 hours

#### What to Build

Create the HTTP client that communicates with Shipox's API, similar to what Agent C built for iShip.

#### Files to Create

- `Connectors/Shipox/ShipoxApiClient.cs`
- `Connectors/Shipox/Models/` directory with request/response models

#### Implementation Details

- Study Shipox API documentation
- Implement authentication
- Create base HTTP client with error handling
- Define request/response models based on Shipox API structure

#### Acceptance Criteria

- [ ] HTTP client configured
- [ ] Authentication works
- [ ] Request/response models defined
- [ ] Error handling implemented

---

### Task 4.2: Implement Shipox Order Management

**Priority**: HIGH  
**Dependencies**: Task 4.1  
**Estimated Time**: 8 hours

#### What to Build

Implement methods to interact with Shipox's order management system:
- Create/update orders in Shipox
- Retrieve order status
- Handle carrier aggregation (if Shipox provides it)

#### Files to Create

- `Connectors/Shipox/ShipoxConnectorService.cs`

#### Implementation

```csharp
public class ShipoxConnectorService
{
    public async Task<OASISResult<Order>> CreateOrderAsync(OrderRequest request);
    public async Task<OASISResult<Order>> GetOrderAsync(string orderId);
    public async Task<OASISResult<Order>> UpdateOrderAsync(string orderId, OrderUpdate update);
}
```

#### Acceptance Criteria

- [ ] Order CRUD operations work
- [ ] Integration with Shipox API successful
- [ ] Error handling works

---

### Task 4.3: Implement Shipox UI Quote Endpoint

**Priority**: MEDIUM  
**Dependencies**: Task 4.1, Task 5.2 (Rate Engine - can mock initially)  
**Estimated Time**: 4 hours

#### What to Build

Create REST endpoint that Shipox's UI will call to get shipping quotes for customers.

#### Endpoint

**POST /api/shipexpro/shipox/quote-request**

- Same functionality as merchant rate endpoint (Agent B built)
- Used by Shipox's customer-facing UI
- Returns quotes with markup applied

#### Files to Create

- `Controllers/ShipexProShipoxController.cs`

#### Implementation

- Reuse RateService from Agent E
- Same response format as merchant rates endpoint
- May need different authentication (Shipox internal)

#### Acceptance Criteria

- [ ] Endpoint returns quotes
- [ ] Response format matches specification
- [ ] Authentication works

---

### Task 4.4: Implement Shipox UI Confirmation Endpoint

**Priority**: MEDIUM  
**Dependencies**: Task 4.3, Task 6.2 (Shipment Orchestrator - can mock initially)  
**Estimated Time**: 6 hours

#### What to Build

Endpoint for Shipox UI to confirm a shipment after customer selects a quote.

#### Endpoint

**POST /api/shipexpro/shipox/confirm-shipment**

- Accepts selected quote ID
- Creates shipment via ShipmentService (Agent E)
- Triggers label generation

#### Acceptance Criteria

- [ ] Confirmation creates shipment
- [ ] Label generation triggered
- [ ] Response includes tracking number

---

### Task 4.5: Implement Shipox Tracking Endpoint

**Priority**: LOW  
**Dependencies**: Task 4.1, Task 6.2  
**Estimated Time**: 4 hours

#### What to Build

Endpoint for Shipox UI to track shipments.

#### Endpoint

**GET /api/shipexpro/shipox/track/{trackingNumber}**

- Returns current status and tracking history
- Used by Shipox's tracking page

#### Acceptance Criteria

- [ ] Tracking lookup works
- [ ] Returns status history
- [ ] Real-time updates supported

---

### Task 8.1: Create Webhook Controller

**Priority**: CRITICAL - Foundation for webhook processing  
**Dependencies**: Task 1.4 (Service interfaces)  
**Estimated Time**: 4 hours

#### What to Build

Create ASP.NET Core controller that receives webhooks from iShip and Shipox.

#### Endpoints

- **POST /api/shipexpro/webhooks/iship** - Receive iShip webhooks
- **POST /api/shipexpro/webhooks/shipox** - Receive Shipox webhooks

#### Files to Create

- `Controllers/ShipexProWebhookController.cs`

#### Implementation

```csharp
[ApiController]
[Route("api/shipexpro/webhooks")]
public class ShipexProWebhookController : ControllerBase
{
    [HttpPost("iship")]
    public async Task<IActionResult> ProcessIShipWebhook([FromBody] object payload)
    {
        // Receive webhook
        // Store for processing
        // Return 200 OK immediately
    }
    
    [HttpPost("shipox")]
    public async Task<IActionResult> ProcessShipoxWebhook([FromBody] object payload)
    {
        // Similar to iShip
    }
}
```

#### Acceptance Criteria

- [ ] Endpoints receive webhooks
- [ ] Webhooks stored for processing
- [ ] Returns 200 OK quickly (async processing)
- [ ] Logging implemented

---

### Task 8.2: Implement Webhook Signature Verification

**Priority**: CRITICAL - Security requirement  
**Dependencies**: Task 8.1, Task 9.2 (Secret Vault for webhook secrets)  
**Estimated Time**: 8 hours

#### What to Build

Implement security to verify webhook signatures from iShip and Shipox.

#### Components

- **WebhookSecurityService.cs**
  - HMAC signature verification
  - IP whitelisting (optional)
  - Replay protection (nonce/timestamp checking)

#### Implementation

```csharp
public class WebhookSecurityService
{
    public bool VerifyIShipSignature(string payload, string signature, string secret);
    public bool VerifyShipoxSignature(string payload, string signature, string secret);
    public bool IsIPWhitelisted(string ipAddress); // Optional
}
```

#### HMAC Verification

- iShip and Shipox will send webhooks with HMAC signatures
- Verify signature using shared secret from Secret Vault (Agent F)
- Reject webhooks with invalid signatures

#### Acceptance Criteria

- [ ] HMAC verification works
- [ ] Invalid signatures rejected (401/403)
- [ ] IP whitelisting works (if enabled)
- [ ] Replay protection implemented

---

### Task 8.3: Implement Webhook Processing Service

**Priority**: CRITICAL - Core functionality  
**Dependencies**: Task 8.1, Task 8.2, Task 6.2, Task 7.3  
**Estimated Time**: 10 hours

#### What to Build

Process webhook events asynchronously. Update shipments, trigger invoices, etc.

#### Implementation

```csharp
public class WebhookService : IWebhookService
{
    public async Task<OASISResult<bool>> ProcessWebhookAsync(WebhookEvent webhook)
    {
        // 1. Verify signature (via WebhookSecurityService)
        // 2. Store webhook event (for audit)
        // 3. Route to appropriate handler based on event type
        // 4. Update shipment status
        // 5. Trigger QuickBooks invoice if status = SHIPPED
    }
    
    private async Task ProcessStatusUpdate(WebhookEvent webhook);
    private async Task ProcessShippedEvent(WebhookEvent webhook);
    private async Task ProcessTrackingUpdate(WebhookEvent webhook);
}
```

#### Event Types

- `shipment.status.changed` → Update shipment status
- `tracking.updated` → Update tracking information
- `shipment.shipped` → Trigger QuickBooks invoice creation

#### Acceptance Criteria

- [ ] Webhooks processed correctly
- [ ] Shipment status updated
- [ ] QuickBooks invoice triggered when shipped
- [ ] Error handling works
- [ ] Failed webhooks stored for retry

---

### Task 8.4: Implement Webhook Storage & Audit

**Priority**: HIGH - Compliance and debugging  
**Dependencies**: Task 1.3 (Repository), Task 8.3  
**Estimated Time**: 6 hours

#### What to Build

Store all webhook events for audit trail and retry failed webhooks.

#### Implementation

- Use Agent A's repository to store webhook events
- Track processing status
- Implement retry mechanism for failed webhooks
- Create admin endpoint to view webhook history

#### Acceptance Criteria

- [ ] All webhooks stored
- [ ] Processing status tracked
- [ ] Failed webhooks can be retried
- [ ] Audit trail complete

---

## Working with Other Agents

### Dependencies You Need

- **Agent A**: Repository for storing webhook events
- **Agent E**: ShipmentService and QuickBooksService (for processing webhooks)
- **Agent F**: Secret Vault for webhook secrets
- **Agent C**: iShip connector (to understand webhook format)

### Dependencies You Create

- **Agent E**: Uses your webhook processing to update shipments
- **Shipox Team**: Consumes your API endpoints

### Communication Points

1. **After Task 8.1**: Share webhook endpoint URLs with Agent C (for registration)
2. **Coordinate with Agent E**: Ensure webhook processing triggers correct services
3. **Coordinate with Agent F**: Get webhook secrets for signature verification

---

## Success Criteria

You will know you've succeeded when:

1. ✅ Shipox API integration works
2. ✅ Webhooks are received and processed correctly
3. ✅ Signature verification secures webhooks
4. ✅ Shipment status updates work via webhooks
5. ✅ QuickBooks invoices triggered correctly

---

## Important Notes

- **Webhook Processing**: Process webhooks asynchronously (don't block response)
- **Security**: Never skip signature verification in production
- **Idempotency**: Handle duplicate webhooks gracefully
- **Documentation**: Document webhook payload formats for Shipox team

---

**Questions?** Refer to the main implementation plan or coordinate with Agents E and F.

**Ready to start?** Begin with Task 4.1 or Task 8.1 depending on priorities.
