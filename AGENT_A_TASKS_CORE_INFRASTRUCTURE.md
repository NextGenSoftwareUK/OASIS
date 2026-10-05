# Agent A - Core Infrastructure & Database Tasks

## Overview

You are responsible for building the foundation of the Shipex Pro logistics middleware system. This includes creating the OASIS provider structure, designing and implementing the database schema, and setting up the core repository layer that all other components will depend on.

## What You're Building

**Shipex Pro** is an OASIS-based logistics middleware that enables merchants to integrate shipping services through a unified API. The system integrates with:
- **Shipox**: Order management and customer UI
- **iShip**: Carrier services (rates, labels, tracking)
- **QuickBooks**: Automated invoicing and payment tracking

Your foundation work enables all other agents to build their components on top of a solid, well-structured base.

## Architecture Context

You're building the **Core Infrastructure Layer** which sits at the bottom of the architecture:

```
┌─────────────────────────────────────────────────────────────┐
│         Shipex Pro Middleware (Core Service)                 │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ • Rate & Markup Engine          ← Agent E builds     │ │
│  │ • Shipment Orchestrator         ← Agent E builds     │ │
│  │ • DB: quotes, markups, shipments, invoices  ← YOU    │ │
│  │ • Webhook Receiver              ← Agent D builds     │ │
│  │ • QuickBooks Billing Worker     ← Agent E builds     │ │
│  │ • STAR ledger + secret vault    ← Agent F builds     │ │
│  └──────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

**Your Role**: Create the database layer and core service interfaces that all other components will use.

---

## Reference Materials

1. **Main Implementation Plan**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_IMPLEMENTATION_PLAN.md`
2. **Task Breakdown**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_TASK_BREAKDOWN.md`
3. **Validation Framework**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_VALIDATION_FRAMEWORK.md`
4. **OASIS Provider Example**: `/Volumes/Storage/OASIS_CLEAN/Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoDBOASIS.cs`

---

## Your Tasks

### Task 1.1: Create OASIS Provider Project Structure

**Priority**: CRITICAL - Other agents depend on this  
**Dependencies**: None  
**Estimated Time**: 4 hours

#### What to Build

Create the foundational project structure for the Shipex Pro OASIS provider following the OASIS provider pattern.

#### Files to Create

1. **Project File**
   - Location: `NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.csproj`
   - Template: Follow existing OASIS provider projects (check MongoDBOASIS for reference)

2. **Main Provider Class**
   - Location: `NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/ShipexProOASIS.cs`
   - Should inherit from `OASISStorageProviderBase` (or appropriate OASIS base class)
   - Should implement required OASIS interfaces
   - Reference: See `MongoDBOASIS.cs` lines 23-73 for pattern

3. **Directory Structure**
   ```
   NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/
   ├── Models/
   ├── Repositories/
   ├── Services/
   ├── Connectors/
   ├── Middleware/
   └── Helpers/
   ```

4. **Configuration**
   - `appsettings.json` with basic structure
   - Connection strings placeholders

#### Implementation Steps

1. Review existing OASIS provider structure (MongoDBOASIS)
2. Create new .NET project following same pattern
3. Add required NuGet package references
4. Create provider class that inherits from OASIS base
5. Set provider properties:
   - `ProviderName = "ShipexProOASIS"`
   - `ProviderDescription = "Shipex Pro Logistics Middleware Provider"`
   - `ProviderType = ProviderType.ShipexProOASIS`
   - `ProviderCategory = ProviderCategory.StorageAndNetwork`
6. Create directory structure
7. Add basic configuration files

#### Acceptance Criteria

- [ ] Project compiles without errors
- [ ] Provider class properly initializes
- [ ] All directories exist
- [ ] Configuration file structure in place
- [ ] Follows OASIS provider pattern

#### Validation

See `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_VALIDATION_FRAMEWORK.md` - Task 1.1

---

### Task 1.2: Design MongoDB Database Schema

**Priority**: CRITICAL - Foundation for all data operations  
**Dependencies**: Task 1.1  
**Estimated Time**: 8 hours

#### What to Build

Design complete MongoDB schema for all collections needed by the system. This schema will be used by the repository layer and all services.

#### Collections to Design

1. **quotes** - Store rate quotes with markup calculations
   - Quote ID (unique identifier)
   - Merchant ID (link to merchant)
   - Shipment details (dimensions, weight, origin, destination, service level)
   - Carrier rates array (multiple carriers, each with rate and estimated days)
   - Client quotes array (applied markup, showing carrier_rate and client_price)
   - Selected quote reference
   - Expiration timestamp
   - Created/updated timestamps

2. **markups** - Markup configuration per merchant/carrier
   - Markup ID (unique identifier)
   - Merchant ID (optional - null means global/default)
   - Carrier name (UPS, FedEx, DHL, etc.)
   - Markup type (Fixed or Percentage)
   - Markup value (amount or percentage)
   - Effective date range (from/to)
   - Active flag

3. **shipments** - Shipment records with full lifecycle
   - Shipment ID (unique identifier)
   - Merchant ID
   - Quote ID (link to original quote)
   - Carrier shipment ID (from iShip/Shipox)
   - Tracking number
   - Status (enum: QuoteRequested, QuoteProvided, QuoteAccepted, ShipmentCreated, LabelGenerated, InTransit, Delivered, Cancelled, Error)
   - Label data (PDF URL, base64, or signed URL)
   - Financial data (amount charged, carrier cost, markup amount)
   - Customer ID/reference
   - Status history array (audit trail)
   - Created/updated timestamps

4. **invoices** - QuickBooks invoice tracking
   - Invoice ID (unique identifier)
   - Shipment ID (link to shipment)
   - Merchant ID
   - QuickBooks invoice ID (external ID)
   - QuickBooks customer ID
   - Amount (total invoice amount)
   - Line items array (carrier cost, markup, taxes, etc.)
   - Status (Draft, Sent, Paid)
   - Payment date
   - Created/updated timestamps

5. **webhook_events** - Audit trail for all webhook events
   - Event ID (unique identifier)
   - Source (iShip, Shipox, etc.)
   - Event type
   - Payload (full webhook data)
   - Signature (for verification)
   - Processing status (Pending, Processed, Failed)
   - Error message (if failed)
   - Retry count
   - Processed timestamp
   - Created timestamp

6. **merchants** - Merchant configuration and credentials
   - Merchant ID (unique identifier)
   - Company name
   - Contact information
   - API keys (hashed)
   - Rate limit tier
   - Active status
   - QuickBooks connection status
   - Configuration settings
   - Created/updated timestamps

#### Schema Design Requirements

1. **Indexes** - Define indexes for:
   - Fast lookups by ID (primary key)
   - Merchant ID lookups (foreign key)
   - Tracking number lookups
   - Status queries
   - Date range queries

2. **Data Validation**
   - Required fields
   - Data types
   - Value constraints
   - References integrity (where applicable)

3. **Documentation**
   - Create `DATABASE_SCHEMA.md` document
   - Include JSON schema examples
   - Document all indexes
   - Document relationships

#### Implementation Steps

1. Review data requirements from implementation plan
2. Design each collection schema
3. Define indexes for performance
4. Document relationships between collections
5. Create schema documentation file
6. Create JSON examples for each collection

#### Example Schema Structure

```json
{
  "_id": "ObjectId",
  "quoteId": "Guid (string)",
  "merchantId": "Guid (string)",
  "shipmentDetails": {
    "dimensions": { "length": 10, "width": 5, "height": 3 },
    "weight": 2.5,
    "origin": {
      "address": "...",
      "city": "...",
      "state": "...",
      "postalCode": "...",
      "country": "..."
    },
    "destination": { /* same structure */ },
    "serviceLevel": "standard|express|overnight"
  },
  "carrierRates": [
    {
      "carrier": "UPS",
      "rate": 15.50,
      "estimatedDays": 3,
      "serviceName": "Ground"
    }
  ],
  "clientQuotes": [
    {
      "carrier": "UPS",
      "carrierRate": 15.50,
      "markupAmount": 3.10,
      "clientPrice": 18.60,
      "markupConfigId": "Guid (string)"
    }
  ],
  "selectedQuote": "ObjectId (reference)",
  "expiresAt": "DateTime (ISO 8601)",
  "createdAt": "DateTime (ISO 8601)",
  "updatedAt": "DateTime (ISO 8601)"
}
```

#### Acceptance Criteria

- [ ] All 6 collections fully documented
- [ ] Indexes defined for all performance-critical queries
- [ ] Data validation rules specified
- [ ] JSON schema examples provided
- [ ] Relationships documented
- [ ] Schema supports all use cases from implementation plan

#### Validation

See validation framework - Task 1.2

---

### Task 1.3: Implement MongoDB Repository Layer

**Priority**: CRITICAL - All services depend on this  
**Dependencies**: Task 1.2  
**Estimated Time**: 12 hours

#### What to Build

Implement the complete repository layer that provides data access for all collections. This will be used by all services (RateService, ShipmentService, etc.).

#### Files to Create

1. **Repository Interface**
   - `Repositories/IShipexProRepository.cs`
   - Define all CRUD and query methods
   - Use async/await pattern
   - Return types use OASISResult<T>

2. **Repository Implementation**
   - `Repositories/ShipexProMongoRepository.cs`
   - Implement IShipexProRepository
   - Use MongoDB driver
   - Handle errors properly

3. **Model Classes** (in Models/ directory)
   - `Models/Quote.cs`
   - `Models/Markup.cs`
   - `Models/Shipment.cs`
   - `Models/Invoice.cs`
   - `Models/WebhookEvent.cs`
   - `Models/Merchant.cs`

4. **MongoDB Context**
   - `Repositories/MongoDbContext.cs` (or similar)
   - Handle MongoDB connection
   - Configure indexes

#### Repository Methods to Implement

**Quotes:**
- `SaveQuoteAsync(Quote quote)`
- `GetQuoteAsync(Guid quoteId)`
- `GetQuotesByMerchantIdAsync(Guid merchantId)`
- `GetExpiredQuotesAsync()`
- `UpdateQuoteAsync(Quote quote)`
- `DeleteQuoteAsync(Guid quoteId)`

**Markups:**
- `SaveMarkupAsync(Markup markup)`
- `GetMarkupAsync(Guid markupId)`
- `GetMarkupsByMerchantIdAsync(Guid merchantId)`
- `GetMarkupsByCarrierAsync(string carrier)`
- `GetActiveMarkupsAsync(Guid? merchantId, string carrier)`
- `UpdateMarkupAsync(Markup markup)`
- `DeleteMarkupAsync(Guid markupId)`

**Shipments:**
- `SaveShipmentAsync(Shipment shipment)`
- `GetShipmentAsync(Guid shipmentId)`
- `GetShipmentByTrackingNumberAsync(string trackingNumber)`
- `GetShipmentsByMerchantIdAsync(Guid merchantId)`
- `GetShipmentsByStatusAsync(ShipmentStatus status)`
- `UpdateShipmentAsync(Shipment shipment)`
- `UpdateShipmentStatusAsync(Guid shipmentId, ShipmentStatus status)`

**Invoices:**
- `SaveInvoiceAsync(Invoice invoice)`
- `GetInvoiceAsync(Guid invoiceId)`
- `GetInvoiceByShipmentIdAsync(Guid shipmentId)`
- `GetInvoicesByMerchantIdAsync(Guid merchantId)`
- `UpdateInvoiceAsync(Invoice invoice)`
- `UpdateInvoiceStatusAsync(Guid invoiceId, InvoiceStatus status)`

**Webhook Events:**
- `SaveWebhookEventAsync(WebhookEvent webhookEvent)`
- `GetWebhookEventAsync(Guid eventId)`
- `GetPendingWebhookEventsAsync()`
- `GetFailedWebhookEventsAsync()`
- `UpdateWebhookEventStatusAsync(Guid eventId, ProcessingStatus status)`

**Merchants:**
- `SaveMerchantAsync(Merchant merchant)`
- `GetMerchantAsync(Guid merchantId)`
- `GetMerchantByApiKeyAsync(string apiKey)`
- `GetAllMerchantsAsync()`
- `UpdateMerchantAsync(Merchant merchant)`

#### Implementation Pattern

Follow OASIS patterns:
- Use `OASISResult<T>` for all return types
- Handle errors using OASIS error handling
- Use async/await throughout
- Log important operations
- Use MongoDB driver best practices

#### Example Method Signature

```csharp
public interface IShipexProRepository
{
    Task<OASISResult<Quote>> SaveQuoteAsync(Quote quote);
    Task<OASISResult<Quote>> GetQuoteAsync(Guid quoteId);
    // ... other methods
}
```

#### Acceptance Criteria

- [ ] All repository methods implemented
- [ ] All model classes created matching schema
- [ ] MongoDB connection properly configured
- [ ] Indexes created on repository initialization
- [ ] Error handling uses OASISResult pattern
- [ ] All methods are async
- [ ] Unit tests can be written (testable design)

#### Validation

See validation framework - Task 1.3

---

### Task 1.4: Create Core Service Interfaces

**Priority**: HIGH - Other agents need these interfaces  
**Dependencies**: Task 1.3  
**Estimated Time**: 4 hours

#### What to Build

Create the service interfaces that define contracts for all core services. Other agents will implement these interfaces.

#### Files to Create

1. **Rate Service Interface**
   - `Services/IRateService.cs`
   - Methods for rate requests and quote management

2. **Shipment Service Interface**
   - `Services/IShipmentService.cs`
   - Methods for shipment lifecycle management

3. **Webhook Service Interface**
   - `Services/IWebhookService.cs`
   - Methods for webhook processing

4. **QuickBooks Service Interface**
   - `Services/IQuickBooksService.cs`
   - Methods for invoice creation and payment tracking

#### Interface Definitions

**IRateService.cs:**
```csharp
public interface IRateService
{
    Task<OASISResult<QuoteResponse>> GetRatesAsync(RateRequest request);
    Task<OASISResult<Quote>> GetQuoteAsync(Guid quoteId);
}
```

**IShipmentService.cs:**
```csharp
public interface IShipmentService
{
    Task<OASISResult<ShipmentResponse>> CreateShipmentAsync(CreateShipmentRequest request);
    Task<OASISResult<Shipment>> GetShipmentAsync(Guid shipmentId);
    Task<OASISResult<Shipment>> UpdateShipmentStatusAsync(Guid shipmentId, ShipmentStatus status);
}
```

**IWebhookService.cs:**
```csharp
public interface IWebhookService
{
    Task<OASISResult<bool>> ProcessWebhookAsync(WebhookEvent webhook);
    Task<OASISResult<bool>> RetryFailedWebhookAsync(Guid webhookEventId);
}
```

**IQuickBooksService.cs:**
```csharp
public interface IQuickBooksService
{
    Task<OASISResult<Invoice>> CreateInvoiceAsync(Shipment shipment);
    Task<OASISResult<Invoice>> GetInvoiceAsync(Guid invoiceId);
    Task<OASISResult<bool>> CheckPaymentStatusAsync(Guid invoiceId);
}
```

#### Implementation Steps

1. Review service requirements from implementation plan
2. Define interface methods based on service responsibilities
3. Use OASISResult<T> pattern
4. Use async/await
5. Define request/response models as needed
6. Set up dependency injection configuration

#### Acceptance Criteria

- [ ] All service interfaces defined
- [ ] Methods properly typed
- [ ] Use OASISResult pattern
- [ ] Async/await pattern used
- [ ] Dependency injection configured
- [ ] Other agents can implement these interfaces

#### Validation

See validation framework - Task 1.4

---

## Additional Tasks

### Task A.4: Setup Development Environment

**Priority**: MEDIUM  
**Dependencies**: None  
**Estimated Time**: 4 hours

#### What to Build

Document and set up the development environment for the project.

#### Deliverables

1. **Development Setup Guide**
   - MongoDB installation/connection instructions
   - Visual Studio / VS Code setup
   - NuGet package restoration
   - Configuration setup
   - Running the project locally

2. **Environment Configuration**
   - Development appsettings.json template
   - Required environment variables
   - Connection string format

#### Acceptance Criteria

- [ ] Setup guide documented
- [ ] Development environment can be replicated
- [ ] Configuration templates provided

---

### Task A.5: Create Deployment Configuration

**Priority**: LOW (Can be done later)  
**Dependencies**: All phases complete  
**Estimated Time**: 10 hours

#### What to Build

Create deployment configuration files and documentation.

#### Deliverables

1. Docker configuration (if applicable)
2. Environment variable documentation
3. Deployment scripts
4. CI/CD pipeline configuration

---

## Working with Other Agents

### Dependencies You Create

- **Task 1.1**: Agent B, C, D, E, F need the project structure
- **Task 1.3**: Agent B, E need the repository layer
- **Task 1.4**: Agent E needs the service interfaces

### Communication Points

1. **After Task 1.1**: Share project structure with all agents
2. **After Task 1.2**: Share schema documentation (Agents B, E need this)
3. **After Task 1.3**: Share repository interface (Agents B, E need this)
4. **After Task 1.4**: Share service interfaces (Agent E needs this)

### Coordinate With

- **Agent B**: Needs repository interface early for mock implementations
- **Agent E**: Will implement the service interfaces you define
- **Agent F**: May need to integrate with repository for secret storage

---

## Success Criteria

You will know you've succeeded when:

1. ✅ Project structure follows OASIS patterns perfectly
2. ✅ Database schema supports all use cases
3. ✅ Repository layer is complete and testable
4. ✅ Other agents can start their work using your foundation
5. ✅ All code compiles and follows OASIS conventions

---

## Resources

- OASIS Provider Pattern: `/Volumes/Storage/OASIS_CLEAN/Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/`
- MongoDB Driver Documentation: https://www.mongodb.com/docs/drivers/csharp/
- OASIS Core Types: Search for `OASISResult`, `OASISStorageProviderBase` in codebase

---

**Questions?** Refer to the main implementation plan or task breakdown documents for more context.

**Ready to start?** Begin with Task 1.1 and work sequentially through the tasks.
