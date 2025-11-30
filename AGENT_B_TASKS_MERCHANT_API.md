# Agent B - Merchant API Layer Tasks

## Overview

You are responsible for building the **Merchant API Layer** - the external-facing REST API that merchants will use to integrate Shipex Pro into their e-commerce platforms (Shopify, custom CMS, etc.). This includes authentication, rate requests, order management, and rate limiting.

## What You're Building

The **Merchant API Layer** is the primary integration point for merchants. It provides:
- **Authentication**: JWT-based auth using OASIS Avatar system
- **Rate Requests**: Get shipping quotes with markup applied
- **Order Management**: Create and manage shipping orders
- **Rate Limiting**: Protect system resources per merchant tier

This API sits at the top of the architecture and communicates with the core middleware services built by other agents.

## Architecture Context

Your API layer sits here:

```
┌─────────────────────────────────────────────────────────────┐
│              Merchant API Layer                              │
│    (Authentication, Rate Requests, Order Intake)  ← YOU    │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ Internal Events / RPC
                        ↓
┌─────────────────────────────────────────────────────────────┐
│         Shipex Pro Middleware (Core Service)                 │
│  • Rate & Markup Engine         ← Agent E                   │
│  • Shipment Orchestrator        ← Agent E                   │
│  • DB Repository                ← Agent A                   │
└─────────────────────────────────────────────────────────────┘
```

**Your Role**: Create the REST API endpoints, authentication system, and rate limiting that merchants will consume.

---

## Reference Materials

1. **Main Implementation Plan**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_IMPLEMENTATION_PLAN.md`
2. **Task Breakdown**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_TASK_BREAKDOWN.md`
3. **Validation Framework**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_VALIDATION_FRAMEWORK.md`
4. **Agent A Outputs**: Repository interfaces and models (when available)

---

## Your Tasks

### Task 2.1: Implement Merchant Authentication

**Priority**: CRITICAL - All endpoints need this  
**Dependencies**: Task 1.3 (Repository)  
**Estimated Time**: 10 hours

#### What to Build

Build a complete authentication system using JWT tokens and OASIS Avatar system. Merchants will authenticate to get API access.

#### Components to Build

1. **MerchantAuthService.cs**
   - Merchant registration
   - JWT token generation
   - API key generation
   - Password hashing (or use OASIS Avatar)

2. **MerchantAuthMiddleware.cs**
   - JWT token validation
   - API key validation
   - Add merchant context to request

3. **MerchantAuthController.cs**
   - `POST /api/shipexpro/merchant/register` - Register new merchant
   - `POST /api/shipexpro/merchant/login` - Login and get JWT
   - `POST /api/shipexpro/merchant/apikeys` - Generate API key

#### Implementation Details

**Registration Flow:**
1. Merchant provides company info, email, password
2. Create merchant record in database (via Agent A's repository)
3. Create OASIS Avatar (if not exists) or link to existing
4. Generate API key
5. Return merchant ID and API key

**Login Flow:**
1. Merchant provides email/username and password
2. Validate credentials against OASIS Avatar system
3. Generate JWT token (include merchant ID, tier, permissions)
4. Return JWT token

**JWT Token Claims:**
- MerchantId (Guid)
- Email/Username
- RateLimitTier (Basic, Premium, Enterprise)
- Expiration time (e.g., 24 hours)

#### Files to Create

- `Services/MerchantAuthService.cs`
- `Middleware/MerchantAuthMiddleware.cs`
- `Controllers/MerchantAuthController.cs` (in ONODE.WebAPI project)
- `Models/MerchantRegistrationRequest.cs`
- `Models/MerchantLoginRequest.cs`
- `Models/MerchantAuthResponse.cs`

#### Acceptance Criteria

- [ ] Registration creates merchant and OASIS Avatar
- [ ] Login returns valid JWT token
- [ ] Middleware validates tokens on protected endpoints
- [ ] API key authentication works as alternative
- [ ] Invalid tokens return 401 Unauthorized
- [ ] Password security follows OASIS patterns

#### Validation

See validation framework - Task 2.1

---

### Task 2.2: Implement Rate Request Endpoints

**Priority**: CRITICAL - Core functionality  
**Dependencies**: Task 2.1, Task 1.3 (Repository), Task 5.2 (Rate Engine - can use mock initially)  
**Estimated Time**: 8 hours

#### What to Build

Implement the rate request endpoints that merchants use to get shipping quotes. This will call the RateService (built by Agent E) or use mock data initially.

#### Endpoints to Build

**POST /api/shipexpro/merchant/rates**
- Request: Shipment details (dimensions, weight, origin, destination, service level)
- Response: Quote with multiple carrier options and prices
- Flow:
  1. Validate request (dimensions, weight, addresses)
  2. Call RateService.GetRatesAsync() (Agent E will implement)
  3. Return quotes with markup applied
  4. Store quote in database (via repository)

**GET /api/shipexpro/merchant/quotes/{quoteId}**
- Request: Quote ID
- Response: Quote details
- Flow:
  1. Validate merchant owns quote
  2. Retrieve quote from database
  3. Return quote details

#### Request Model

```csharp
public class RateRequest
{
    public Guid MerchantId { get; set; }
    public Dimensions Dimensions { get; set; }
    public decimal Weight { get; set; }
    public Address Origin { get; set; }
    public Address Destination { get; set; }
    public string ServiceLevel { get; set; } // "standard", "express", "overnight"
}
```

#### Response Model

```csharp
public class QuoteResponse
{
    public Guid QuoteId { get; set; }
    public List<QuoteOption> Quotes { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class QuoteOption
{
    public string Carrier { get; set; }
    public decimal CarrierRate { get; set; }
    public decimal ClientPrice { get; set; }
    public decimal MarkupAmount { get; set; }
    public int EstimatedDays { get; set; }
}
```

#### Files to Create

- `Controllers/ShipexProMerchantController.cs`
- `Models/RateRequest.cs`
- `Models/QuoteResponse.cs`
- `Models/Dimensions.cs`
- `Models/Address.cs`

#### Implementation Notes

- Initially, you can mock the RateService call until Agent E completes it
- Use Agent A's repository to store/retrieve quotes
- Validate merchant has permission to access quote
- Return proper HTTP status codes (200, 400, 401, 404, 500)

#### Acceptance Criteria

- [ ] Rate request endpoint works
- [ ] Quotes stored in database
- [ ] Quote retrieval works
- [ ] Merchant validation works
- [ ] Response format matches specification
- [ ] Error handling returns proper codes

#### Validation

See validation framework - Task 2.2

---

### Task 2.3: Implement Rate Limiting

**Priority**: HIGH - Protect system resources  
**Dependencies**: Task 2.1  
**Estimated Time**: 6 hours

#### What to Build

Implement rate limiting middleware that enforces different limits based on merchant tier.

#### Components to Build

1. **RateLimitMiddleware.cs**
   - Intercept all requests
   - Check rate limit for merchant
   - Add rate limit headers to response
   - Return 429 Too Many Requests when exceeded

2. **RateLimitService.cs**
   - Track requests per merchant
   - Store limits per tier
   - Check if limit exceeded
   - Reset counters based on time window

#### Rate Limit Tiers

- **Basic**: 100 requests/hour
- **Premium**: 1000 requests/hour
- **Enterprise**: 10000 requests/hour

#### Implementation Details

- Use in-memory cache (Redis recommended for production)
- Sliding window or fixed window algorithm
- Add headers:
  - `X-RateLimit-Limit`: Maximum requests allowed
  - `X-RateLimit-Remaining`: Remaining requests
  - `X-RateLimit-Reset`: Time when limit resets

#### Files to Create

- `Middleware/RateLimitMiddleware.cs`
- `Services/RateLimitService.cs`
- `Models/RateLimitConfig.cs`

#### Acceptance Criteria

- [ ] Rate limits enforced per merchant tier
- [ ] Headers added to all responses
- [ ] 429 returned when limit exceeded
- [ ] Limits reset correctly

#### Validation

See validation framework - Task 2.3

---

### Task 2.4: Implement Order Intake Endpoints

**Priority**: MEDIUM  
**Dependencies**: Task 2.2, Task 6.2 (Shipment Orchestrator - can use mock initially)  
**Estimated Time**: 6 hours

#### What to Build

Implement endpoints for merchants to create and manage shipping orders.

#### Endpoints to Build

**POST /api/shipexpro/merchant/orders**
- Create new shipping order from quote
- Request: Quote ID, selected carrier, customer info
- Response: Order ID and shipment details

**GET /api/shipexpro/merchant/orders/{orderId}**
- Get order details
- Response: Full order information

**PUT /api/shipexpro/merchant/orders/{orderId}**
- Update order (before shipment created)
- Request: Updated order details
- Response: Updated order

#### Implementation Details

- Orders link to quotes
- Orders eventually create shipments (via Agent E's ShipmentService)
- Initially can create order record; shipment creation happens later

#### Files to Create/Update

- Update `ShipexProMerchantController.cs`
- `Models/OrderRequest.cs`
- `Models/OrderResponse.cs`
- `Models/CustomerInfo.cs`

#### Acceptance Criteria

- [ ] Order creation works
- [ ] Order retrieval works
- [ ] Order update works
- [ ] Orders linked to merchants and quotes

#### Validation

See validation framework - Task 2.4

---

## Additional Tasks

### Task A.2: Create Integration Tests

**Priority**: HIGH  
**Dependencies**: All tasks complete  
**Estimated Time**: Variable

#### What to Build

Create integration tests for the complete merchant API flow.

#### Test Scenarios

1. Register merchant → Login → Get rates → Create order
2. Rate limiting enforcement
3. Authentication failures
4. Error handling

---

### Task A.3: Create API Documentation

**Priority**: HIGH  
**Dependencies**: All endpoints complete  
**Estimated Time**: 8 hours

#### What to Build

Create comprehensive API documentation using Swagger/OpenAPI.

#### Deliverables

1. Swagger/OpenAPI specification
2. Endpoint documentation with examples
3. Authentication documentation
4. Error code reference

---

## Working with Other Agents

### Dependencies You Need

- **Agent A**: Repository interface for storing merchants, quotes, orders
- **Agent E**: RateService and ShipmentService (can mock initially)

### Dependencies You Create

- **Agent D**: May reuse authentication patterns
- **Agent E**: Needs API contracts to implement services

### Communication Points

1. **After Task 2.1**: Share authentication patterns
2. **After Task 2.2**: Share API contracts with Agent E
3. **Coordinate with Agent E**: Ensure API contracts match service implementations

---

## Success Criteria

You will know you've succeeded when:

1. ✅ Merchants can register and authenticate
2. ✅ Rate requests work end-to-end
3. ✅ Rate limiting protects the system
4. ✅ All endpoints properly documented
5. ✅ Integration tests pass

---

## Resources

- OASIS Avatar System: Search for Avatar-related code in codebase
- JWT Authentication: Use standard JWT libraries
- ASP.NET Core Middleware: Standard ASP.NET Core patterns

---

**Questions?** Refer to the main implementation plan or coordinate with Agent A and Agent E.

**Ready to start?** Begin with Task 2.1 (authentication) as it's required for all other endpoints.
