# Shipex Pro - Quick Test Start Guide

This guide helps you quickly set up and run tests for Shipex Pro.

---

## Prerequisites

1. **MongoDB** - Running locally or accessible test instance
2. **.NET 8.0 SDK** - Installed and available
3. **Test Database** - Separate test database recommended

---

## Quick Setup (5 Minutes)

### Step 1: Create Test Project

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex
dotnet new xunit -n NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests
cd NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests
```

### Step 2: Add Required NuGet Packages

```bash
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
dotnet add package Moq --version 4.20.70
dotnet add package FluentAssertions --version 6.12.0
dotnet add package coverlet.collector --version 6.0.2
dotnet add package MongoDB.Driver --version 2.19.0
```

### Step 3: Add Project Reference

```bash
dotnet add reference ../NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.csproj
```

### Step 4: Configure Test Environment

Create `appsettings.Test.json`:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "shipex_test"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

## Running Your First Test

### 1. Simple Unit Test Example

Create `Services/RateServiceTests.cs`:

```csharp
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.Services;

public class RateServiceTests
{
    [Fact]
    public void Test_ShouldPass()
    {
        // Arrange
        var expected = true;
        
        // Act
        var actual = true;
        
        // Assert
        actual.Should().Be(expected);
    }
}
```

### 2. Run the Test

```bash
dotnet test
```

You should see:
```
Test Run Successful.
Total tests: 1
     Passed: 1
```

---

## Testing Checklist

### Immediate Tests to Create

- [ ] **Repository Tests**: Test MongoDB CRUD operations
- [ ] **Service Tests**: Test RateService, ShipmentService
- [ ] **Controller Tests**: Test MerchantAuthController, ShipexProMerchantController
- [ ] **Middleware Tests**: Test authentication, rate limiting

### Quick Test Commands

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~RateServiceTests"

# Run with coverage
dotnet test /p:CollectCoverage=true

# Watch mode (rerun on file changes)
dotnet watch test
```

---

## Testing Scenarios

### 1. Test Merchant Registration

**Goal**: Verify merchant can register and receive API key

```csharp
[Fact]
public async Task Register_ValidEmail_CreatesMerchant()
{
    // TODO: Implement test
}
```

### 2. Test Rate Request

**Goal**: Verify rate request returns quotes with markup

```csharp
[Fact]
public async Task GetRates_ValidRequest_ReturnsQuotes()
{
    // TODO: Implement test
}
```

### 3. Test Authentication

**Goal**: Verify protected endpoints require authentication

```csharp
[Fact]
public async Task GetRates_NoAuth_Returns401()
{
    // TODO: Implement test
}
```

---

## Common Issues & Solutions

### Issue: MongoDB Connection Failed

**Solution**: 
- Ensure MongoDB is running: `mongod`
- Check connection string in `appsettings.Test.json`
- Use test database: `mongodb://localhost:27017/shipex_test`

### Issue: Test Project Won't Compile

**Solution**:
- Verify all NuGet packages installed: `dotnet restore`
- Check project references: `dotnet list reference`
- Build project: `dotnet build`

### Issue: Tests Fail with Missing Dependencies

**Solution**:
- Check service registration in test setup
- Use Moq to mock external dependencies
- See `TESTING_GUIDE.md` for mocking examples

---

## Next Steps

1. ✅ Create test project (done above)
2. Read `TESTING_GUIDE.md` for detailed examples
3. Start with repository tests (easiest to test)
4. Move to service tests
5. Add API endpoint tests last

---

**Need Help?** See `TESTING_GUIDE.md` for comprehensive examples.




