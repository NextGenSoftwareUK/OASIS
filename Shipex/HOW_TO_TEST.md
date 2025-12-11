# How to Test Shipex Pro OASIS Provider

This guide covers all testing approaches for the Shipex Pro OASIS provider, from unit tests to full integration testing.

---

## 🎯 Quick Start - Run Existing Tests

### 1. Run All Unit Tests

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex/NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests
dotnet test
```

**Expected Output:**
```
✅ ShipmentStatusValidatorTests: 9 tests passed
✅ RateServiceTests: 1 test passed
✅ ShipexProMongoRepositoryTests: 4 tests (requires MongoDB)
```

### 2. Run Specific Test Classes

```bash
# Run only status validator tests (no dependencies)
dotnet test --filter "FullyQualifiedName~ShipmentStatusValidatorTests"

# Run repository tests (requires MongoDB)
dotnet test --filter "FullyQualifiedName~ShipexProMongoRepositoryTests"
```

---

## 📋 Testing Levels

### Level 1: Unit Tests (No Dependencies) ✅ Ready Now

**What's Tested:**
- Business logic validation
- Status transition rules
- Helper functions

**Run:**
```bash
dotnet test --filter "FullyQualifiedName~ShipmentStatusValidatorTests"
```

**Status:** ✅ **9 tests passing** - No setup required

---

### Level 2: Repository Tests (Requires MongoDB)

**What's Tested:**
- MongoDB CRUD operations
- Data persistence
- Query operations

**Prerequisites:**
1. MongoDB running locally or accessible
2. Connection string configured

**Setup:**
```bash
# Option 1: Use local MongoDB (default)
# Ensure MongoDB is running on localhost:27017

# Option 2: Use environment variable for custom connection
export MONGODB_TEST_CONNECTION="mongodb://your-mongodb-host:27017"
```

**Run:**
```bash
dotnet test --filter "FullyQualifiedName~ShipexProMongoRepositoryTests"
```

**What Happens:**
- Creates a temporary test database (`shipex_test_<guid>`)
- Runs tests against it
- Cleans up after tests complete

---

### Level 3: Integration Testing (Manual)

Since this is a **provider library** (not a standalone API), it needs to be integrated into the OASIS API to test end-to-end.

#### Option A: Test via OASIS API Integration

**Step 1: Register Provider in OASIS API**

1. **Add to OASIS_DNA.json:**
```json
{
  "StorageProviders": {
    "ShipexProOASIS": {
      "ConnectionString": "mongodb://localhost:27017",
      "DatabaseName": "ShipexPro"
    }
  }
}
```

2. **Start OASIS API:**
```bash
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

3. **Register Provider via API:**
```bash
# Get JWT token first
curl -X POST "https://localhost:5002/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "your_username", "password": "your_password"}' \
  -k

# Register ShipexProOASIS provider
curl -X POST "https://localhost:5002/api/provider/register-provider-type/ShipexProOASIS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -k

# Activate provider
curl -X POST "https://localhost:5002/api/provider/activate-provider/ShipexProOASIS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -k
```

#### Option B: Create Test Web API Project

Create a minimal ASP.NET Core app to test the provider directly:

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex
dotnet new webapi -n ShipexProTestApi
cd ShipexProTestApi
dotnet add reference ../NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.csproj
```

**Program.cs:**
```csharp
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSingleton<IShipexProRepository>(sp =>
{
    var connectionString = builder.Configuration["ShipexPro:ConnectionString"] 
        ?? "mongodb://localhost:27017";
    var dbName = builder.Configuration["ShipexPro:DatabaseName"] ?? "ShipexPro";
    var context = new ShipexProMongoDbContext(connectionString, dbName);
    return new ShipexProMongoRepository(context);
});

// Add Shipex Pro services
builder.Services.AddScoped<IRateService, RateService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<MerchantAuthService>();

var app = builder.Build();
app.MapControllers();
app.Run();
```

---

## 🧪 Test Scenarios

### Scenario 1: Test Provider Activation

**Test:** Verify provider can be activated and connects to MongoDB

```csharp
var provider = new ShipexProOASIS(
    "mongodb://localhost:27017",
    "ShipexPro"
);

var result = provider.ActivateProvider();
Assert.True(result.Result);
Assert.True(provider.IsProviderActivated);
```

### Scenario 2: Test Merchant Registration

**Test:** Register a new merchant and verify it's saved

```csharp
var authService = new MerchantAuthService(repository, avatarManager, logger);
var request = new MerchantRegistrationRequest
{
    Email = "test@example.com",
    Password = "SecurePass123!",
    CompanyName = "Test Company",
    ContactInfo = new ContactInfo
    {
        Email = "test@example.com",
        Phone = "555-1234",
        Address = "123 Test St"
    },
    RateLimitTier = "Standard"
};

var result = await authService.RegisterAsync(request);
Assert.False(result.IsError);
Assert.NotNull(result.Result);
```

### Scenario 3: Test Rate Calculation

**Test:** Get shipping rates for a shipment

```csharp
var rateService = new RateService(iShipConnector, markupEngine, repository, logger);
var request = new RateRequest
{
    MerchantId = merchantId,
    Weight = 10.0m,
    Dimensions = new Dimensions { Length = 10, Width = 5, Height = 3 },
    Origin = new Address { /* ... */ },
    Destination = new Address { /* ... */ }
};

var result = await rateService.GetRatesAsync(request);
Assert.False(result.IsError);
Assert.NotNull(result.Result);
Assert.NotEmpty(result.Result.Quotes);
```

### Scenario 4: Test Status Transitions

**Test:** Verify shipment status transitions are valid

```csharp
// Valid transition
Assert.True(ShipmentStatusValidator.IsValidTransition(
    ShipmentStatus.QuoteRequested,
    ShipmentStatus.QuoteProvided
));

// Invalid transition
Assert.False(ShipmentStatusValidator.IsValidTransition(
    ShipmentStatus.Delivered,
    ShipmentStatus.InTransit
));
```

---

## 🔧 Test Configuration

### MongoDB Setup

**Local MongoDB:**
```bash
# macOS
brew install mongodb-community
brew services start mongodb-community

# Linux
sudo systemctl start mongod

# Docker
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

**Verify MongoDB is running:**
```bash
mongosh --eval "db.adminCommand('ping')"
# Should return: { ok: 1 }
```

### Environment Variables

```bash
# Test MongoDB connection
export MONGODB_TEST_CONNECTION="mongodb://localhost:27017"

# API Keys (for integration tests)
export ISHIP_API_KEY="your-key"
export SHIPOX_API_KEY="your-key"
export QUICKBOOKS_CLIENT_ID="your-id"
export QUICKBOOKS_CLIENT_SECRET="your-secret"
```

---

## 📊 Test Coverage Report

Generate coverage report:

```bash
cd NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

View report:
```bash
# Install report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:coverage.opencover.xml \
  -targetdir:coverage-report \
  -reporttypes:Html
```

---

## 🐛 Debugging Tests

### Run Tests with Debug Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Debug in Visual Studio / Rider

1. Set breakpoints in test code
2. Right-click test → Debug Test
3. Step through execution

### Debug in VS Code

1. Install C# extension
2. Create `.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Test",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "dotnet",
      "args": ["test", "--no-build"],
      "cwd": "${workspaceFolder}/NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests",
      "console": "internalConsole",
      "stopAtEntry": false
    }
  ]
}
```

---

## ✅ Test Checklist

### Before Testing

- [ ] MongoDB is running and accessible
- [ ] Test database connection string configured
- [ ] All dependencies installed (`dotnet restore`)
- [ ] Project builds successfully (`dotnet build`)

### Unit Tests

- [ ] ShipmentStatusValidator tests pass (9 tests)
- [ ] RateService basic test passes
- [ ] No compilation errors

### Integration Tests

- [ ] MongoDB repository tests pass (4 tests)
- [ ] Test database is created and cleaned up
- [ ] Data persists correctly

### Manual Testing

- [ ] Provider can be activated
- [ ] Merchant registration works
- [ ] Rate calculation returns results
- [ ] Status transitions are validated

---

## 🚀 Next Steps

1. **Add More Unit Tests:**
   - Service layer tests (RateService, ShipmentService)
   - Middleware tests (MerchantAuthMiddleware, RateLimitMiddleware)
   - Controller tests

2. **Add Integration Tests:**
   - Full merchant registration flow
   - Quote request → Shipment creation flow
   - Webhook processing flow

3. **Add API Tests:**
   - HTTP endpoint testing
   - Authentication flow testing
   - Error response testing

4. **Set Up CI/CD:**
   - Run tests on every commit
   - Generate coverage reports
   - Block merges if tests fail

---

## 📝 Quick Reference

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~ShipmentStatusValidatorTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage
dotnet test /p:CollectCoverage=true

# Build only
dotnet build

# Clean and rebuild
dotnet clean && dotnet build
```

---

**Status:** ✅ **Ready for Testing**  
**Last Updated:** January 2025
