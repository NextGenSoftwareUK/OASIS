# Shipex Pro - Task Validation Framework

This document provides a comprehensive validation framework to verify that each task has been completed correctly. Each task includes specific validation criteria, test procedures, and acceptance criteria.

---

## Validation Process Overview

1. **Code Review Checklist**: Verify code quality, structure, and adherence to patterns
2. **Functional Tests**: Verify functionality works as specified
3. **Integration Tests**: Verify integration with dependencies works correctly
4. **Acceptance Criteria**: Verify all deliverables are met
5. **Automated Validation**: Where possible, automated scripts verify task completion

---

## Validation Tools & Scripts

### Automated Validation Script
A PowerShell script can be created to automatically check:
- File existence
- Code compilation
- Basic structure validation
- Required dependencies

---

## Phase 1: Foundation & Core Infrastructure

### Task 1.1: Create OASIS Provider Project Structure

**Validation Checklist:**
- [ ] Project file exists at: `NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.csproj`
- [ ] Project compiles without errors
- [ ] `ShipexProOASIS.cs` file exists
- [ ] `ShipexProOASISProvider.cs` file exists (or main provider class)
- [ ] Provider inherits from appropriate OASIS base class (e.g., `OASISStorageProviderBase`)
- [ ] Provider implements required OASIS interfaces
- [ ] `Models/` directory exists
- [ ] `Services/` directory exists
- [ ] `Repositories/` directory exists
- [ ] `Connectors/` directory exists
- [ ] `appsettings.json` exists with basic configuration structure

**Automated Validation:**
```powershell
# Check file existence
Test-Path "NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.csproj"
Test-Path "NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/ShipexProOASIS.cs"

# Check compilation
dotnet build NextGenSoftware.OASIS.API.Providers.ShipexProOASIS
```

**Acceptance Criteria:**
- Project structure follows OASIS provider pattern
- Provider class properly initialized with name, description, type, and category
- Project can be referenced by other projects in solution
- No compilation errors

**Manual Review Points:**
- Verify provider class structure matches MongoDBOASIS pattern
- Verify namespace follows OASIS conventions
- Check that all required NuGet packages are referenced

---

### Task 1.2: Design MongoDB Database Schema

**Validation Checklist:**
- [ ] Schema documentation exists (`DATABASE_SCHEMA.md`)
- [ ] All 6 collections documented:
  - `quotes`
  - `markups`
  - `shipments`
  - `invoices`
  - `webhook_events`
  - `merchants`
- [ ] Index definitions documented for each collection
- [ ] Data validation rules specified
- [ ] Field types and constraints documented
- [ ] Relationships between collections documented

**Automated Validation:**
```powershell
# Check documentation exists
Test-Path "DATABASE_SCHEMA.md"

# Check for required collection names in documentation
Select-String -Path "DATABASE_SCHEMA.md" -Pattern "quotes|markups|shipments|invoices|webhook_events|merchants"
```

**Acceptance Criteria:**
- Complete schema documentation for all collections
- Index definitions optimize common queries
- Validation rules prevent invalid data
- Schema supports all use cases from implementation plan

**Manual Review Points:**
- Verify schema matches implementation plan requirements
- Check that indexes support query patterns
- Ensure proper data types for all fields

---

### Task 1.3: Implement MongoDB Repository Layer

**Validation Checklist:**
- [ ] `IShipexProRepository.cs` interface exists with all required methods
- [ ] `ShipexProMongoRepository.cs` implementation exists
- [ ] All model classes exist:
  - `Quote.cs`
  - `Markup.cs`
  - `Shipment.cs`
  - `Invoice.cs`
  - `WebhookEvent.cs`
  - `Merchant.cs`
- [ ] Repository implements all CRUD operations for each entity
- [ ] Query helpers exist for common operations
- [ ] MongoDB context/service properly configured
- [ ] Dependency injection configured

**Automated Validation:**
```powershell
# Check required files exist
$files = @(
    "Repositories/IShipexProRepository.cs",
    "Repositories/ShipexProMongoRepository.cs",
    "Models/Quote.cs",
    "Models/Markup.cs",
    "Models/Shipment.cs",
    "Models/Invoice.cs",
    "Models/WebhookEvent.cs",
    "Models/Merchant.cs"
)
foreach ($file in $files) {
    Test-Path "NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/$file"
}

# Check compilation
dotnet build NextGenSoftware.OASIS.API.Providers.ShipexProOASIS
```

**Functional Tests:**
```csharp
// Test repository instantiation
var repository = serviceProvider.GetService<IShipexProRepository>();
Assert.IsNotNull(repository);

// Test CRUD operations (using test database)
var quote = new Quote { /* ... */ };
var savedQuote = await repository.SaveQuoteAsync(quote);
Assert.IsNotNull(savedQuote);

var retrievedQuote = await repository.GetQuoteAsync(savedQuote.QuoteId);
Assert.AreEqual(savedQuote.QuoteId, retrievedQuote.QuoteId);
```

**Acceptance Criteria:**
- All interfaces properly defined
- All implementations compile without errors
- Repository methods follow async/await pattern
- Error handling uses OASISResult pattern
- All models match schema design

**Manual Review Points:**
- Verify OASISResult pattern used consistently
- Check error handling and logging
- Verify async/await usage
- Check MongoDB driver usage follows best practices

---

### Task 1.4: Create Core Service Interfaces

**Validation Checklist:**
- [ ] `IRateService.cs` exists
- [ ] `IShipmentService.cs` exists
- [ ] `IWebhookService.cs` exists
- [ ] `IQuickBooksService.cs` exists
- [ ] All interfaces define required methods
- [ ] Methods use OASISResult pattern
- [ ] Dependency injection configured in startup

**Automated Validation:**
```powershell
$interfaces = @(
    "Services/IRateService.cs",
    "Services/IShipmentService.cs",
    "Services/IWebhookService.cs",
    "Services/IQuickBooksService.cs"
)
foreach ($interface in $interfaces) {
    Test-Path "NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/$interface"
}
```

**Acceptance Criteria:**
- All service interfaces defined
- Methods properly typed with async/await
- Return types use OASISResult<T>
- Interfaces follow OASIS service patterns

---

## Phase 2: Merchant API Layer

### Task 2.1: Implement Merchant Authentication

**Validation Checklist:**
- [ ] `MerchantAuthService.cs` exists
- [ ] `MerchantAuthMiddleware.cs` exists
- [ ] `MerchantAuthController.cs` exists
- [ ] JWT token generation works
- [ ] JWT token validation works
- [ ] Merchant registration endpoint functional
- [ ] Merchant login endpoint functional
- [ ] API key generation endpoint functional
- [ ] Authentication middleware properly intercepts requests

**Automated Validation:**
```powershell
# Check files exist
Test-Path "Controllers/MerchantAuthController.cs"
Test-Path "Services/MerchantAuthService.cs"
Test-Path "Middleware/MerchantAuthMiddleware.cs"

# Check compilation
dotnet build
```

**Functional Tests:**
```csharp
// Test merchant registration
var response = await client.PostAsync("/api/shipexpro/merchant/register", 
    new StringContent(JsonConvert.SerializeObject(new { /* ... */ }), 
    Encoding.UTF8, "application/json"));
Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

// Test login
var loginResponse = await client.PostAsync("/api/shipexpro/merchant/login", /* ... */);
var token = await loginResponse.Content.ReadAsStringAsync();
Assert.IsNotNull(token);

// Test protected endpoint
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
var protectedResponse = await client.GetAsync("/api/shipexpro/merchant/rates");
Assert.AreEqual(HttpStatusCode.OK, protectedResponse.StatusCode);
```

**Acceptance Criteria:**
- Registration creates merchant and generates API key
- Login returns valid JWT token
- Protected endpoints require authentication
- Invalid tokens are rejected
- API keys work as alternative authentication

---

### Task 2.2: Implement Rate Request Endpoints

**Validation Checklist:**
- [ ] `ShipexProMerchantController.cs` exists
- [ ] `POST /api/shipexpro/merchant/rates` endpoint exists
- [ ] `GET /api/shipexpro/merchant/quotes/{quoteId}` endpoint exists
- [ ] Request models exist (`RateRequest.cs`)
- [ ] Response models exist (`QuoteResponse.cs`)
- [ ] Input validation works
- [ ] Endpoints return proper HTTP status codes
- [ ] Response format matches specification

**Automated Validation:**
```powershell
# Check files exist
Test-Path "Controllers/ShipexProMerchantController.cs"
Test-Path "Models/RateRequest.cs"
Test-Path "Models/QuoteResponse.cs"
```

**Functional Tests:**
```csharp
// Test rate request
var rateRequest = new RateRequest {
    MerchantId = Guid.NewGuid(),
    Dimensions = new Dimensions { Length = 10, Width = 5, Height = 3 },
    Weight = 2.5,
    Origin = new Address { /* ... */ },
    Destination = new Address { /* ... */ },
    ServiceLevel = "standard"
};

var response = await client.PostAsync("/api/shipexpro/merchant/rates",
    new StringContent(JsonConvert.SerializeObject(rateRequest), Encoding.UTF8, "application/json"));

Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
var quoteResponse = JsonConvert.DeserializeObject<OASISResult<QuoteResponse>>(
    await response.Content.ReadAsStringAsync());
Assert.IsFalse(quoteResponse.IsError);
Assert.IsNotNull(quoteResponse.Result.QuoteId);

// Test quote retrieval
var quoteResponse = await client.GetAsync($"/api/shipexpro/merchant/quotes/{quoteId}");
Assert.AreEqual(HttpStatusCode.OK, quoteResponse.StatusCode);
```

**Acceptance Criteria:**
- Rate request returns quotes with markup applied
- Quote retrieval returns correct quote
- Invalid requests return appropriate error codes
- Response format matches API specification

---

### Task 2.3: Implement Rate Limiting

**Validation Checklist:**
- [ ] `RateLimitMiddleware.cs` exists
- [ ] `RateLimitService.cs` exists
- [ ] Middleware properly intercepts requests
- [ ] Rate limits enforced per merchant
- [ ] Rate limit headers in responses
- [ ] Different tiers have different limits
- [ ] Rate limit exceeded returns 429 status

**Functional Tests:**
```csharp
// Test rate limiting
var requests = new List<Task<HttpResponseMessage>>();
for (int i = 0; i < 100; i++) {
    requests.Add(client.GetAsync("/api/shipexpro/merchant/rates"));
}
var responses = await Task.WhenAll(requests);

var rateLimitedResponses = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests);
Assert.IsTrue(rateLimitedResponses.Any());

// Check rate limit headers
var response = await client.GetAsync("/api/shipexpro/merchant/rates");
Assert.IsTrue(response.Headers.Contains("X-RateLimit-Limit"));
Assert.IsTrue(response.Headers.Contains("X-RateLimit-Remaining"));
```

**Acceptance Criteria:**
- Rate limits enforced correctly
- Headers provide rate limit information
- Different merchant tiers have different limits

---

### Task 2.4: Implement Order Intake Endpoints

**Validation Checklist:**
- [ ] `POST /api/shipexpro/merchant/orders` endpoint exists
- [ ] `GET /api/shipexpro/merchant/orders/{orderId}` endpoint exists
- [ ] `PUT /api/shipexpro/merchant/orders/{orderId}` endpoint exists
- [ ] Order models exist
- [ ] CRUD operations work correctly

**Functional Tests:**
```csharp
// Test order creation
var orderRequest = new OrderRequest { /* ... */ };
var createResponse = await client.PostAsync("/api/shipexpro/merchant/orders", /* ... */);
Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

// Test order retrieval
var getResponse = await client.GetAsync($"/api/shipexpro/merchant/orders/{orderId}");
Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

// Test order update
var updateResponse = await client.PutAsync($"/api/shipexpro/merchant/orders/{orderId}", /* ... */);
Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);
```

**Acceptance Criteria:**
- Order CRUD operations work correctly
- Orders linked to merchants
- Orders linked to quotes/shipments

---

## Phase 3: iShip Integration

### Task 3.1: Create iShip API Client Base

**Validation Checklist:**
- [ ] `IShipApiClient.cs` exists
- [ ] HTTP client properly configured
- [ ] Request/response models exist
- [ ] Error handling implemented
- [ ] Base URL configurable

**Functional Tests:**
```csharp
// Test API client initialization
var client = new IShipApiClient(apiKey, baseUrl);
Assert.IsNotNull(client);

// Test error handling
try {
    await client.GetRatesAsync(invalidRequest);
} catch (IShipApiException ex) {
    Assert.IsNotNull(ex.Message);
}
```

---

### Task 3.2: Implement iShip Rate Request

**Validation Checklist:**
- [ ] `GetRatesAsync` method exists
- [ ] Request properly formatted
- [ ] Response properly parsed
- [ ] Error handling works
- [ ] Retry logic implemented

**Functional Tests:**
```csharp
// Test rate request (with mock or sandbox)
var rates = await iShipConnector.GetRatesAsync(rateRequest);
Assert.IsFalse(rates.IsError);
Assert.IsNotNull(rates.Result);
Assert.IsTrue(rates.Result.Count > 0);
```

---

### Task 3.3: Implement iShip Shipment Creation

**Validation Checklist:**
- [ ] `CreateShipmentAsync` method exists
- [ ] Label retrieved (PDF/base64)
- [ ] Tracking number extracted
- [ ] Shipment stored in database

**Functional Tests:**
```csharp
// Test shipment creation
var shipment = await iShipConnector.CreateShipmentAsync(shipmentRequest);
Assert.IsFalse(shipment.IsError);
Assert.IsNotNull(shipment.Result.TrackingNumber);
Assert.IsNotNull(shipment.Result.Label);
```

---

## Phase 4: Shipox Integration

### Task 4.1: Create Shipox API Client Base

**Validation Checklist:**
- [ ] `ShipoxApiClient.cs` exists
- [ ] Authentication works
- [ ] Request/response models exist

**Functional Tests:**
```csharp
var client = new ShipoxApiClient(credentials);
var isAuthenticated = await client.ValidateConnectionAsync();
Assert.IsTrue(isAuthenticated);
```

---

## Phase 5: Markup Engine

### Task 5.2: Implement Rate & Markup Engine

**Validation Checklist:**
- [ ] `RateMarkupEngine.cs` exists
- [ ] Percentage markup calculation works
- [ ] Fixed markup calculation works
- [ ] Markup selection logic works

**Functional Tests:**
```csharp
// Test percentage markup
var carrierQuote = new RateQuote { Rate = 100 };
var markup = new MarkupConfiguration { Type = MarkupType.Percentage, Value = 10 };
var quote = markupEngine.ApplyMarkup(carrierQuote, markup);
Assert.AreEqual(110, quote.ClientPrice);

// Test fixed markup
markup = new MarkupConfiguration { Type = MarkupType.Fixed, Value = 5 };
quote = markupEngine.ApplyMarkup(carrierQuote, markup);
Assert.AreEqual(105, quote.ClientPrice);
```

**Acceptance Criteria:**
- Markup calculations are accurate
- Both percentage and fixed markups work
- Markup selection prioritizes merchant-specific over carrier-specific

---

## Phase 6: Shipment Orchestrator

### Task 6.2: Implement Shipment Orchestrator Service

**Validation Checklist:**
- [ ] Complete orchestration flow works
- [ ] Status transitions are correct
- [ ] Error handling works
- [ ] Retry logic implemented

**Functional Tests:**
```csharp
// Test complete flow
var quote = await rateService.GetRatesAsync(request);
var shipment = await shipmentService.CreateShipmentAsync(new CreateShipmentRequest {
    QuoteId = quote.Result.QuoteId
});

Assert.AreEqual(ShipmentStatus.ShipmentCreated, shipment.Result.Status);
Assert.IsNotNull(shipment.Result.TrackingNumber);
```

---

## Phase 7: QuickBooks Integration

### Task 7.1: Implement QuickBooks OAuth2 Service

**Validation Checklist:**
- [ ] OAuth2 flow works
- [ ] Token refresh works
- [ ] Tokens stored in STAR ledger

**Functional Tests:**
```csharp
// Test OAuth flow
var authUrl = await quickBooksOAuth.GetAuthorizationUrlAsync();
Assert.IsNotNull(authUrl);

// Test token exchange
var tokens = await quickBooksOAuth.ExchangeCodeAsync(authCode);
Assert.IsNotNull(tokens.AccessToken);
Assert.IsNotNull(tokens.RefreshToken);

// Test token refresh
var newTokens = await quickBooksOAuth.RefreshTokenAsync(tokens.RefreshToken);
Assert.IsNotNull(newTokens.AccessToken);
```

---

## Phase 8: Webhook System

### Task 8.2: Implement Webhook Signature Verification

**Validation Checklist:**
- [ ] HMAC verification works
- [ ] Invalid signatures rejected
- [ ] IP whitelisting works (if enabled)
- [ ] Replay protection works

**Functional Tests:**
```csharp
// Test valid signature
var webhook = CreateWebhookWithValidSignature();
var isValid = await webhookSecurity.VerifySignature(webhook);
Assert.IsTrue(isValid);

// Test invalid signature
webhook.Signature = "invalid";
var isValid = await webhookSecurity.VerifySignature(webhook);
Assert.IsFalse(isValid);
```

---

## Phase 9: Secret Vault & Credentials

### Task 9.1: Integrate OASIS STAR Ledger for Secrets

**Validation Checklist:**
- [ ] STAR ledger integration works
- [ ] Encryption/decryption works
- [ ] Secrets stored securely

**Functional Tests:**
```csharp
// Test secret storage
var secret = "test-api-key";
await secretVault.StoreSecretAsync("iship-api-key", secret);

// Test secret retrieval
var retrieved = await secretVault.GetSecretAsync("iship-api-key");
Assert.AreEqual(secret, retrieved);
```

---

## Automated Validation Script Template

```powershell
# validate-task.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$TaskId,
    
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath
)

function Validate-Task {
    param($TaskId, $ProjectPath)
    
    Write-Host "Validating Task: $TaskId" -ForegroundColor Cyan
    
    # Load task validation criteria (from JSON or this document)
    $validationCriteria = Get-Content "task-validations.json" | ConvertFrom-Json
    $task = $validationCriteria.$TaskId
    
    if (-not $task) {
        Write-Host "Task $TaskId not found in validation criteria" -ForegroundColor Red
        return $false
    }
    
    $allPassed = $true
    
    # Check files exist
    foreach ($file in $task.RequiredFiles) {
        $fullPath = Join-Path $ProjectPath $file
        if (Test-Path $fullPath) {
            Write-Host "✓ $file exists" -ForegroundColor Green
        } else {
            Write-Host "✗ $file missing" -ForegroundColor Red
            $allPassed = $false
        }
    }
    
    # Check compilation
    if ($task.CheckCompilation) {
        Push-Location $ProjectPath
        $buildResult = dotnet build 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Project compiles successfully" -ForegroundColor Green
        } else {
            Write-Host "✗ Compilation failed" -ForegroundColor Red
            $allPassed = $false
        }
        Pop-Location
    }
    
    return $allPassed
}

$result = Validate-Task -TaskId $TaskId -ProjectPath $ProjectPath
if ($result) {
    Write-Host "`nTask validation PASSED" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nTask validation FAILED" -ForegroundColor Red
    exit 1
}
```

---

## Validation Checklist Template

For each task, create a checklist with:
- [ ] **Code Review**: Code quality, patterns, best practices
- [ ] **Functional Tests**: Manual or automated functional tests pass
- [ ] **Integration Tests**: Integration with dependencies works
- [ ] **Documentation**: Code comments and documentation complete
- [ ] **Error Handling**: Proper error handling and logging
- [ ] **Performance**: Meets performance requirements
- [ ] **Security**: Security requirements met

---

**Document Version**: 1.0  
**Last Updated**: January 2025  
**Status**: Ready for Use
