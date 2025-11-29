# Agent E - Business Logic Tasks

## Overview

You are responsible for the **core business logic** of Shipex Pro: the markup engine, shipment orchestrator, and QuickBooks integration. These are the heart of the system that tie everything together.

## What You're Building

1. **Markup Engine**: Calculate markups on carrier rates (fixed or percentage)
2. **Rate Service**: Orchestrate rate requests and markup application
3. **Shipment Orchestrator**: Manage complete shipment lifecycle
4. **QuickBooks Integration**: Automatically create invoices and track payments

## Architecture Context

```
┌─────────────────────────────────────────────────────────────┐
│         Shipex Pro Middleware (Core Service)                 │
│  • Rate Service              ← You build                    │
│  • Markup Engine             ← You build                    │
│  • Shipment Orchestrator     ← You build                    │
│  • QuickBooks Billing Worker ← You build                    │
└───────┬───────────────────────────────┬─────────────────────┘
        │                               │
        │ Uses                          │ Uses
        ↓                               ↓
┌───────────────────┐         ┌──────────────────────────────┐
│  iShip Connector  │         │   QuickBooks API             │
│  (Agent C)        │         │   (External Service)         │
└───────────────────┘         └──────────────────────────────┘
```

**Your Role**: Implement the core services that implement Agent A's interfaces and coordinate with connectors built by other agents.

---

## Reference Materials

1. **Main Implementation Plan**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_IMPLEMENTATION_PLAN.md`
2. **Task Breakdown**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_TASK_BREAKDOWN.md`
3. **Validation Framework**: `/Volumes/Storage/OASIS_CLEAN/OASIS_LOGISTICS_MIDDLEWARE_VALIDATION_FRAMEWORK.md`
4. **Agent A Outputs**: Service interfaces (IRateService, IShipmentService, etc.)
5. **QuickBooks API**: https://developer.intuit.com/app/developer/qbo/docs

---

## Your Tasks

### Task 5.1: Design Markup Configuration Model

**Priority**: HIGH - Foundation for markup engine  
**Dependencies**: Task 1.3 (Repository)  
**Estimated Time**: 4 hours

#### What to Build

Design the data model for markup configurations that merchants can set up per carrier.

#### Files to Create/Update

- Update `Models/Markup.cs` (created by Agent A)
- `Models/MarkupConfiguration.cs`

#### Model Design

```csharp
public class MarkupConfiguration
{
    public Guid MarkupId { get; set; }
    public Guid? MerchantId { get; set; } // null = global/default
    public string Carrier { get; set; } // "UPS", "FedEx", "DHL"
    public MarkupType Type { get; set; } // Fixed or Percentage
    public decimal Value { get; set; } // amount or percentage
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public enum MarkupType
{
    Fixed,        // Add fixed amount
    Percentage    // Add percentage of base rate
}
```

#### Business Rules

- Merchant-specific markups override global/default markups
- Effective date ranges control when markups apply
- Only one active markup per merchant/carrier at a time

#### Acceptance Criteria

- [ ] Model properly designed
- [ ] Validation rules defined
- [ ] Date range logic clear

---

### Task 5.2: Implement Rate & Markup Engine

**Priority**: CRITICAL - Core business logic  
**Dependencies**: Task 5.1, Task 3.2 (iShip connector - Agent C)  
**Estimated Time**: 10 hours

#### What to Build

Implement the RateService that:
1. Calls iShip connector for carrier rates
2. Retrieves markup configurations
3. Applies markups to carrier rates
4. Returns quotes with both carrier_rate and client_price

#### Files to Create

- `Services/RateMarkupEngine.cs` - Markup calculation logic
- `Services/RateService.cs` - Implements IRateService interface

#### RateMarkupEngine Implementation

```csharp
public class RateMarkupEngine
{
    public Quote ApplyMarkup(RateQuote carrierQuote, MarkupConfiguration markup)
    {
        decimal clientPrice;
        
        if (markup.Type == MarkupType.Percentage)
        {
            clientPrice = carrierQuote.Rate * (1 + markup.Value / 100);
        }
        else // Fixed
        {
            clientPrice = carrierQuote.Rate + markup.Value;
        }
        
        return new Quote
        {
            Carrier = carrierQuote.Carrier,
            CarrierRate = carrierQuote.Rate,
            ClientPrice = clientPrice,
            MarkupAmount = clientPrice - carrierQuote.Rate,
            MarkupConfigId = markup.MarkupId,
            EstimatedDays = carrierQuote.EstimatedDays
        };
    }
    
    public MarkupConfiguration SelectMarkup(
        List<MarkupConfiguration> markups, 
        string carrier)
    {
        // Priority: Merchant-specific > Carrier-specific > Global
        // Filter by carrier and active dates
        return markups
            .Where(m => m.Carrier == carrier && m.IsActive)
            .Where(m => DateTime.UtcNow >= m.EffectiveFrom)
            .Where(m => !m.EffectiveTo.HasValue || DateTime.UtcNow <= m.EffectiveTo.Value)
            .OrderByDescending(m => m.MerchantId.HasValue) // Merchant-specific first
            .FirstOrDefault();
    }
}
```

#### RateService Implementation

```csharp
public class RateService : IRateService
{
    private readonly IShipConnectorService _iShipConnector;
    private readonly RateMarkupEngine _markupEngine;
    private readonly IShipexProRepository _repository;
    
    public async Task<OASISResult<QuoteResponse>> GetRatesAsync(RateRequest request)
    {
        try
        {
            // 1. Get carrier rates from iShip
            var carrierRatesResult = await _iShipConnector.GetRatesAsync(request);
            if (carrierRatesResult.IsError)
                return new OASISResult<QuoteResponse> { IsError = true, Message = carrierRatesResult.Message };
            
            // 2. Get markup configurations for merchant
            var markupsResult = await _repository.GetActiveMarkupsAsync(request.MerchantId);
            var markups = markupsResult.Result ?? new List<MarkupConfiguration>();
            
            // 3. Apply markup to each rate
            var quotes = new List<Quote>();
            foreach (var carrierRate in carrierRatesResult.Result)
            {
                var markup = _markupEngine.SelectMarkup(markups, carrierRate.Carrier);
                var quote = _markupEngine.ApplyMarkup(carrierRate, markup);
                quotes.Add(quote);
            }
            
            // 4. Store quote in database
            var quote = new Quote
            {
                QuoteId = Guid.NewGuid(),
                MerchantId = request.MerchantId,
                ShipmentDetails = request,
                CarrierRates = carrierRatesResult.Result,
                ClientQuotes = quotes,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
            
            var saveResult = await _repository.SaveQuoteAsync(quote);
            
            return new OASISResult<QuoteResponse>(new QuoteResponse
            {
                QuoteId = saveResult.Result,
                Quotes = quotes,
                ExpiresAt = quote.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            return new OASISResult<QuoteResponse>
            {
                IsError = true,
                Message = $"Failed to get rates: {ex.Message}"
            };
        }
    }
}
```

#### Acceptance Criteria

- [ ] Markup calculations are accurate
- [ ] Both percentage and fixed markups work
- [ ] Markup selection prioritizes correctly
- [ ] Quotes stored in database
- [ ] Error handling works

---

### Task 5.3: Implement Markup Configuration Management

**Priority**: MEDIUM  
**Dependencies**: Task 5.1, Task 1.3 (Repository)  
**Estimated Time**: 6 hours

#### What to Build

Create API endpoints for managing markup configurations (CRUD operations).

#### Endpoints

- `GET /api/shipexpro/markups` - List markups (with filters)
- `POST /api/shipexpro/markups` - Create markup
- `PUT /api/shipexpro/markups/{markupId}` - Update markup
- `DELETE /api/shipexpro/markups/{markupId}` - Delete markup

#### Files to Create

- `Services/MarkupConfigurationService.cs`
- `Controllers/MarkupController.cs`

#### Acceptance Criteria

- [ ] CRUD operations work
- [ ] Validation prevents invalid configurations
- [ ] Date range validation works

---

### Task 6.1: Define Shipment Status Enum and Models

**Priority**: HIGH - Foundation for orchestrator  
**Dependencies**: Task 1.3  
**Estimated Time**: 2 hours

#### What to Build

Define shipment status enum and status transition rules.

#### Files to Create/Update

- Update `Models/Shipment.cs`
- `Models/ShipmentStatus.cs`

#### Status Enum

```csharp
public enum ShipmentStatus
{
    QuoteRequested,      // Initial quote requested
    QuoteProvided,       // Quote returned to customer
    QuoteAccepted,       // Customer accepted quote
    ShipmentCreated,     // Shipment created with carrier
    LabelGenerated,      // Shipping label generated
    InTransit,          // Package in transit
    Delivered,          // Package delivered
    Cancelled,          // Shipment cancelled
    Error               // Error occurred
}
```

#### Status Transition Rules

- Valid transitions defined
- Invalid transitions blocked

#### Acceptance Criteria

- [ ] Status enum defined
- [ ] Transition rules documented
- [ ] Models updated

---

### Task 6.2: Implement Shipment Orchestrator Service

**Priority**: CRITICAL - Core orchestration logic  
**Dependencies**: Task 6.1, Task 3.3 (iShip connector), Task 5.2 (RateService)  
**Estimated Time**: 16 hours

#### What to Build

Implement ShipmentService that orchestrates the complete shipment lifecycle:
1. Quote Request → Store quote, apply markup
2. Quote Acceptance → Create shipment with iShip
3. Label Generation → Store label
4. Status Updates → Process webhooks
5. Delivery → Trigger QuickBooks invoice

#### Files to Create

- `Services/ShipmentService.cs` - Implements IShipmentService

#### Implementation Flow

```csharp
public class ShipmentService : IShipmentService
{
    public async Task<OASISResult<ShipmentResponse>> CreateShipmentAsync(
        CreateShipmentRequest request)
    {
        // 1. Load quote
        var quote = await _repository.GetQuoteAsync(request.QuoteId);
        if (quote == null || quote.ExpiresAt < DateTime.UtcNow)
        {
            return new OASISResult<ShipmentResponse>
            {
                IsError = true,
                Message = "Quote not found or expired"
            };
        }
        
        // 2. Create shipment with iShip
        var selectedQuote = quote.ClientQuotes.First(q => q.Carrier == request.SelectedCarrier);
        var iShipRequest = new IShipShipmentRequest
        {
            QuoteId = request.QuoteId,
            SelectedCarrier = request.SelectedCarrier,
            CustomerInfo = request.CustomerInfo
        };
        
        var iShipResponse = await _iShipConnector.CreateShipmentAsync(iShipRequest);
        if (iShipResponse.IsError)
        {
            return new OASISResult<ShipmentResponse>
            {
                IsError = true,
                Message = iShipResponse.Message
            };
        }
        
        // 3. Store shipment in database
        var shipment = new Shipment
        {
            ShipmentId = Guid.NewGuid(),
            MerchantId = quote.MerchantId,
            QuoteId = request.QuoteId,
            CarrierShipmentId = iShipResponse.Result.ShipmentId,
            TrackingNumber = iShipResponse.Result.TrackingNumber,
            Label = iShipResponse.Result.Label,
            Status = ShipmentStatus.ShipmentCreated,
            AmountCharged = selectedQuote.ClientPrice,
            CarrierCost = quote.CarrierRates.First(r => r.Carrier == request.SelectedCarrier).Rate,
            MarkupAmount = selectedQuote.MarkupAmount
        };
        
        await _repository.SaveShipmentAsync(shipment);
        
        // 4. Register webhook for status updates
        await _iShipConnector.RegisterWebhookAsync(shipment.CarrierShipmentId);
        
        return new OASISResult<ShipmentResponse>(new ShipmentResponse
        {
            ShipmentId = shipment.ShipmentId,
            TrackingNumber = shipment.TrackingNumber,
            Label = shipment.Label,
            Status = shipment.Status.ToString()
        });
    }
    
    public async Task<OASISResult<Shipment>> UpdateShipmentStatusAsync(
        Guid shipmentId, 
        ShipmentStatus status)
    {
        var shipment = await _repository.GetShipmentAsync(shipmentId);
        if (shipment == null)
        {
            return new OASISResult<Shipment>
            {
                IsError = true,
                Message = "Shipment not found"
            };
        }
        
        // Validate status transition
        if (!IsValidStatusTransition(shipment.Status, status))
        {
            return new OASISResult<Shipment>
            {
                IsError = true,
                Message = $"Invalid status transition from {shipment.Status} to {status}"
            };
        }
        
        shipment.Status = status;
        shipment.StatusHistory.Add(new StatusHistoryEntry
        {
            Status = status,
            Timestamp = DateTime.UtcNow,
            Source = "webhook"
        });
        
        await _repository.UpdateShipmentAsync(shipment);
        
        // If delivered, trigger QuickBooks invoice
        if (status == ShipmentStatus.Delivered)
        {
            await _quickBooksWorker.CreateInvoiceAsync(shipment);
        }
        
        return new OASISResult<Shipment>(shipment);
    }
}
```

#### Acceptance Criteria

- [ ] Complete orchestration flow works
- [ ] Status transitions validated
- [ ] QuickBooks invoice triggered on delivery
- [ ] Error handling robust

---

### Task 6.3: Implement Error Handling & Retry Logic

**Priority**: HIGH - Production readiness  
**Dependencies**: Task 6.2  
**Estimated Time**: 8 hours

#### What to Build

Implement retry mechanism for failed operations and dead letter queue.

#### Files to Create

- `Services/RetryService.cs`
- `Models/FailedOperation.cs`

#### Implementation

- Exponential backoff retry
- Maximum retry attempts
- Dead letter queue for permanently failed operations
- Alerting for critical failures

#### Acceptance Criteria

- [ ] Retry logic works
- [ ] Dead letter queue implemented
- [ ] Alerting configured

---

### Task 7.1: Implement QuickBooks OAuth2 Service

**Priority**: HIGH - Required for invoicing  
**Dependencies**: Task 9.2 (Secret Vault for token storage)  
**Estimated Time**: 12 hours

#### What to Build

Implement OAuth2 flow for QuickBooks integration. Merchants must authorize access to their QuickBooks account.

#### Files to Create

- `Connectors/QuickBooks/QuickBooksOAuthService.cs`
- `Controllers/QuickBooksAuthController.cs`

#### OAuth2 Flow

1. Merchant clicks "Connect QuickBooks"
2. Redirect to QuickBooks authorization URL
3. Merchant authorizes
4. QuickBooks redirects back with authorization code
5. Exchange code for access token and refresh token
6. Store tokens securely in Secret Vault (Agent F)
7. Auto-refresh tokens before expiry

#### Endpoints

- `GET /api/shipexpro/quickbooks/authorize` - Start OAuth flow
- `GET /api/shipexpro/quickbooks/callback` - OAuth callback
- `POST /api/shipexpro/quickbooks/refresh-token` - Refresh token

#### Acceptance Criteria

- [ ] OAuth2 flow works
- [ ] Tokens stored securely
- [ ] Token refresh automated

---

### Task 7.2: Implement QuickBooks API Client

**Priority**: HIGH  
**Dependencies**: Task 7.1  
**Estimated Time**: 10 hours

#### What to Build

Create HTTP client for QuickBooks API (Intuit API).

#### Files to Create

- `Connectors/QuickBooks/QuickBooksApiClient.cs`
- `Connectors/QuickBooks/Models/` directory

#### Implementation

- Handle authentication (OAuth tokens)
- Customer management endpoints
- Invoice creation endpoints
- Handle QuickBooks API rate limits

#### Acceptance Criteria

- [ ] API client works
- [ ] Authentication handled
- [ ] Customer operations work

---

### Task 7.3: Implement QuickBooks Billing Worker

**Priority**: CRITICAL - Core billing functionality  
**Dependencies**: Task 7.2, Task 6.2  
**Estimated Time**: 10 hours

#### What to Build

Create invoices in QuickBooks when shipments are delivered.

#### Files to Create

- `Services/QuickBooksBillingWorker.cs` - Implements IQuickBooksService

#### Implementation

```csharp
public async Task<OASISResult<Invoice>> CreateInvoiceAsync(Shipment shipment)
{
    // 1. Find or create QuickBooks Customer
    var customer = await FindOrCreateCustomerAsync(shipment.MerchantId);
    
    // 2. Create invoice with line items
    var invoice = new QuickBooksInvoice
    {
        CustomerRef = customer.Id,
        LineItems = new[]
        {
            new LineItem
            {
                Description = $"Shipping Service - {shipment.TrackingNumber}",
                Amount = shipment.CarrierCost
            },
            new LineItem
            {
                Description = "Shipping Markup",
                Amount = shipment.MarkupAmount
            }
        },
        TotalAmount = shipment.AmountCharged
    };
    
    // 3. Create invoice via QuickBooks API
    var quickBooksInvoice = await _quickBooksApiClient.CreateInvoiceAsync(invoice);
    
    // 4. Store invoice link in database
    var invoiceRecord = new Invoice
    {
        InvoiceId = Guid.NewGuid(),
        ShipmentId = shipment.ShipmentId,
        MerchantId = shipment.MerchantId,
        QuickBooksInvoiceId = quickBooksInvoice.Id,
        QuickBooksCustomerId = customer.Id,
        Amount = shipment.AmountCharged,
        Status = InvoiceStatus.Draft
    };
    
    await _repository.SaveInvoiceAsync(invoiceRecord);
    
    return new OASISResult<Invoice>(invoiceRecord);
}
```

#### Acceptance Criteria

- [ ] Invoices created successfully
- [ ] Customer matching/creation works
- [ ] Line items correct (carrier cost + markup)
- [ ] Invoice linked to shipment

---

### Task 7.4: Implement Payment Tracking

**Priority**: MEDIUM  
**Dependencies**: Task 7.3  
**Estimated Time**: 6 hours

#### What to Build

Monitor payment status and create ReceivePayment transactions.

#### Implementation

- Poll QuickBooks for payment status
- Create ReceivePayment when paid
- Update invoice status

#### Acceptance Criteria

- [ ] Payment status monitoring works
- [ ] ReceivePayment created correctly
- [ ] Invoice status updated

---

## Working with Other Agents

### Dependencies You Need

- **Agent A**: Repository interfaces, service interfaces (IRateService, IShipmentService)
- **Agent C**: iShip connector (for rates and shipment creation)
- **Agent F**: Secret Vault (for QuickBooks tokens)

### Dependencies You Create

- **Agent B**: Uses your RateService and ShipmentService
- **Agent D**: Uses your services via webhook processing

### Communication Points

1. **Coordinate with Agent A**: Ensure interfaces match your implementations
2. **Coordinate with Agent C**: Test iShip connector integration
3. **Coordinate with Agent D**: Ensure webhook processing calls your services correctly

---

## Success Criteria

You will know you've succeeded when:

1. ✅ Markup calculations work correctly
2. ✅ Complete shipment lifecycle works end-to-end
3. ✅ QuickBooks invoices created automatically
4. ✅ All services use OASISResult pattern
5. ✅ Error handling is robust

---

## Important Notes

- **Business Logic**: You're implementing the core value of the system - markup application
- **Orchestration**: Coordinate between multiple services and connectors
- **Error Handling**: Critical for production - handle all failure scenarios
- **Testing**: Write comprehensive tests for markup calculations

---

**Questions?** Refer to the main implementation plan or coordinate with other agents.

**Ready to start?** Begin with Task 5.1 (Markup Configuration Model) as it's the foundation.
