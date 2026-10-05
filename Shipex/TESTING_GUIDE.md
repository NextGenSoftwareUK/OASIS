# Shipex Pro - Testing Guide

**Date**: January 2025  
**Project**: Shipex Pro Logistics Middleware  
**Testing Framework**: xUnit, ASP.NET Core Test Host

---

## Table of Contents

1. [Testing Overview](#testing-overview)
2. [Test Project Setup](#test-project-setup)
3. [Unit Tests](#unit-tests)
4. [Integration Tests](#integration-tests)
5. [API Endpoint Tests](#api-endpoint-tests)
6. [Database Tests](#database-tests)
7. [Mocking Strategies](#mocking-strategies)
8. [Running Tests](#running-tests)
9. [Test Examples](#test-examples)

---

## Testing Overview

Shipex Pro testing strategy includes:

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions
- **API Tests**: Test HTTP endpoints end-to-end
- **Database Tests**: Test repository layer with test database
- **Contract Tests**: Verify external API integrations

### Test Levels

1. **Unit Tests** (~60% coverage target)
   - Services, Repositories, Middleware
   - Business logic validation
   - Helper functions

2. **Integration Tests** (~30% coverage target)
   - Service-to-service communication
   - Repository-to-database
   - Middleware-to-controller

3. **API Tests** (~10% coverage target)
   - End-to-end HTTP requests
   - Authentication flows
   - Error handling

---

## Test Project Setup

### 1. Create Test Project

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex
dotnet new xunit -n NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests
cd NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests
```

### 2. Update Project File

**File**: `NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="MongoDB.Driver" Version="2.19.0" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="FluentAssertions" />
    <Using Include="Moq" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\NextGenSoftware.OASIS.API.Providers.ShipexProOASIS\NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.csproj" />
  </ItemGroup>

</Project>
```

### 3. Create Test Base Classes

**File**: `TestHelpers/TestBase.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.TestHelpers;

public abstract class TestBase : IClassFixture<TestFixture>
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IShipexProRepository Repository;
    protected readonly ILogger Logger;

    protected TestBase(TestFixture fixture)
    {
        ServiceProvider = fixture.ServiceProvider;
        Repository = ServiceProvider.GetRequiredService<IShipexProRepository>();
        Logger = ServiceProvider.GetRequiredService<ILogger>();
    }
}

public class TestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }

    public TestFixture()
    {
        var services = new ServiceCollection();
        
        // Add test configuration
        services.AddLogging(builder => builder.AddConsole());
        
        // Add repositories with test database
        services.AddSingleton<IShipexProRepository, ShipexProMongoRepository>();
        
        // Add test services
        ConfigureTestServices(services);
        
        ServiceProvider = services.BuildServiceProvider();
    }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Override in test classes to add mocks
    }

    public void Dispose()
    {
        // Cleanup test database if needed
    }
}
```

---

## Unit Tests

### Service Tests

**File**: `Services/RateServiceTests.cs`

```csharp
using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.Services;

public class RateServiceTests
{
    private readonly Mock<IShipConnectorService> _mockIShipConnector;
    private readonly Mock<RateMarkupEngine> _mockMarkupEngine;
    private readonly Mock<IShipexProRepository> _mockRepository;
    private readonly RateService _rateService;

    public RateServiceTests()
    {
        _mockIShipConnector = new Mock<IShipConnectorService>();
        _mockMarkupEngine = new Mock<RateMarkupEngine>();
        _mockRepository = new Mock<IShipexProRepository>();
        
        _rateService = new RateService(
            _mockIShipConnector.Object,
            _mockMarkupEngine.Object,
            _mockRepository.Object,
            null // logger
        );
    }

    [Fact]
    public async Task GetRatesAsync_ValidRequest_ReturnsQuotesWithMarkup()
    {
        // Arrange
        var request = new RateRequest
        {
            MerchantId = Guid.NewGuid(),
            Weight = 10.0m,
            Dimensions = new Dimensions { Length = 10, Width = 5, Height = 3 },
            Origin = new Address { /* ... */ },
            Destination = new Address { /* ... */ }
        };

        var carrierRates = new List<CarrierRate>
        {
            new CarrierRate { Carrier = "UPS", Rate = 25.00m, ServiceName = "Ground" }
        };

        var quoteOption = new QuoteOption
        {
            Carrier = "UPS",
            CarrierRate = 25.00m,
            ClientPrice = 30.00m,
            MarkupAmount = 5.00m
        };

        _mockIShipConnector
            .Setup(x => x.GetRatesAsync(It.IsAny<RateRequest>()))
            .ReturnsAsync(new OASISResult<List<CarrierRate>> { Result = carrierRates });

        _mockMarkupEngine
            .Setup(x => x.ApplyMarkup(It.IsAny<CarrierRate>(), It.IsAny<MarkupConfiguration>()))
            .Returns(quoteOption);

        // Act
        var result = await _rateService.GetRatesAsync(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Result.Should().NotBeNull();
        result.Result.Quotes.Should().HaveCount(1);
        result.Result.Quotes[0].ClientPrice.Should().Be(30.00m);
    }

    [Fact]
    public async Task GetRatesAsync_InvalidWeight_ReturnsError()
    {
        // Arrange
        var request = new RateRequest
        {
            MerchantId = Guid.NewGuid(),
            Weight = -1.0m, // Invalid weight
            Dimensions = new Dimensions { Length = 10, Width = 5, Height = 3 }
        };

        // Act
        var result = await _rateService.GetRatesAsync(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("weight");
    }
}
```

### Repository Tests

**File**: `Repositories/ShipexProMongoRepositoryTests.cs`

```csharp
using FluentAssertions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.Repositories;

public class ShipexProMongoRepositoryTests : IClassFixture<MongoTestFixture>
{
    private readonly IShipexProRepository _repository;
    private readonly IMongoDatabase _testDatabase;

    public ShipexProMongoRepositoryTests(MongoTestFixture fixture)
    {
        _testDatabase = fixture.TestDatabase;
        _repository = new ShipexProMongoRepository(
            new ShipexProMongoDbContext(fixture.ConnectionString, "shipex_test")
        );
    }

    [Fact]
    public async Task SaveQuoteAsync_ValidQuote_SavesAndReturnsQuote()
    {
        // Arrange
        var quote = new Quote
        {
            QuoteId = Guid.NewGuid(),
            MerchantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Act
        var result = await _repository.SaveQuoteAsync(quote);

        // Assert
        result.IsError.Should().BeFalse();
        result.Result.Should().NotBeNull();
        result.Result.QuoteId.Should().Be(quote.QuoteId);
    }

    [Fact]
    public async Task GetQuoteAsync_ExistingQuoteId_ReturnsQuote()
    {
        // Arrange
        var quote = new Quote { QuoteId = Guid.NewGuid(), MerchantId = Guid.NewGuid() };
        await _repository.SaveQuoteAsync(quote);

        // Act
        var result = await _repository.GetQuoteAsync(quote.QuoteId);

        // Assert
        result.IsError.Should().BeFalse();
        result.Result.Should().NotBeNull();
        result.Result.QuoteId.Should().Be(quote.QuoteId);
    }

    [Fact]
    public async Task GetQuoteAsync_NonExistentQuoteId_ReturnsError()
    {
        // Act
        var result = await _repository.GetQuoteAsync(Guid.NewGuid());

        // Assert
        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
    }
}

public class MongoTestFixture : IDisposable
{
    public IMongoDatabase TestDatabase { get; }
    public string ConnectionString { get; }

    public MongoTestFixture()
    {
        ConnectionString = Environment.GetEnvironmentVariable("MONGODB_TEST_CONNECTION") 
            ?? "mongodb://localhost:27017";
        
        var client = new MongoClient(ConnectionString);
        TestDatabase = client.GetDatabase($"shipex_test_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        // Drop test database
        var client = new MongoClient(ConnectionString);
        client.DropDatabase(TestDatabase.DatabaseNamespace.DatabaseName);
    }
}
```

---

## Integration Tests

### Service Integration Tests

**File**: `Integration/RateServiceIntegrationTests.cs`

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.Integration;

[Collection("Integration")]
public class RateServiceIntegrationTests
{
    private readonly RateService _rateService;
    private readonly IServiceProvider _serviceProvider;

    public RateServiceIntegrationTests(IntegrationTestFixture fixture)
    {
        _serviceProvider = fixture.ServiceProvider;
        _rateService = _serviceProvider.GetRequiredService<RateService>();
    }

    [Fact]
    public async Task GetRatesAsync_FullFlow_ReturnsQuotesWithMarkup()
    {
        // Arrange
        var request = new RateRequest
        {
            MerchantId = Guid.NewGuid(),
            Weight = 10.0m,
            Dimensions = new Dimensions { Length = 10, Width = 5, Height = 3 },
            Origin = new Address { /* ... */ },
            Destination = new Address { /* ... */ }
        };

        // Act
        var result = await _rateService.GetRatesAsync(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Result.Should().NotBeNull();
        result.Result.Quotes.Should().NotBeEmpty();
    }
}
```

---

## API Endpoint Tests

### API Test Base

**File**: `Api/ShipexProApiTests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.Api;

public class ShipexProApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ShipexProApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsAuthResponse()
    {
        // Arrange
        var request = new MerchantRegistrationRequest
        {
            Email = $"test_{Guid.NewGuid()}@example.com",
            Password = "SecurePassword123!",
            CompanyName = "Test Company",
            ContactInfo = new ContactInfo { /* ... */ }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/shipexpro/merchant/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<MerchantAuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRates_Authenticated_ReturnsQuotes()
    {
        // Arrange - Register and login first
        var registerRequest = new MerchantRegistrationRequest { /* ... */ };
        var registerResponse = await _client.PostAsJsonAsync("/api/shipexpro/merchant/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<MerchantAuthResponse>();
        
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);

        var rateRequest = new RateRequest
        {
            Weight = 10.0m,
            Dimensions = new Dimensions { Length = 10, Width = 5, Height = 3 },
            // ... other properties
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/shipexpro/merchant/rates", rateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var quoteResponse = await response.Content.ReadFromJsonAsync<QuoteResponse>();
        quoteResponse.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRates_Unauthenticated_Returns401()
    {
        // Arrange - No authentication headers
        var rateRequest = new RateRequest { /* ... */ };

        // Act
        var response = await _client.PostAsJsonAsync("/api/shipexpro/merchant/rates", rateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

---

## Mocking Strategies

### Mock External Services

**File**: `TestHelpers/MockFactory.cs`

```csharp
using Moq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.TestHelpers;

public static class MockFactory
{
    public static Mock<IShipConnectorService> CreateIShipConnectorMock(
        bool returnError = false,
        List<CarrierRate>? rates = null)
    {
        var mock = new Mock<IShipConnectorService>();

        if (returnError)
        {
            mock.Setup(x => x.GetRatesAsync(It.IsAny<RateRequest>()))
                .ReturnsAsync(new OASISResult<List<CarrierRate>>
                {
                    IsError = true,
                    Message = "Test error"
                });
        }
        else
        {
            mock.Setup(x => x.GetRatesAsync(It.IsAny<RateRequest>()))
                .ReturnsAsync(new OASISResult<List<CarrierRate>>
                {
                    Result = rates ?? new List<CarrierRate>
                    {
                        new CarrierRate
                        {
                            Carrier = "UPS",
                            Rate = 25.00m,
                            ServiceName = "Ground"
                        }
                    }
                });
        }

        return mock;
    }
}
```

---

## Running Tests

### Run All Tests

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex/NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~RateServiceTests"
```

### Run with Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Run Integration Tests Only

```bash
dotnet test --filter "Category=Integration"
```

---

## Test Examples

### Complete Test Suite Structure

```
Tests/
├── Services/
│   ├── RateServiceTests.cs
│   ├── ShipmentServiceTests.cs
│   ├── MerchantAuthServiceTests.cs
│   └── MarkupConfigurationServiceTests.cs
├── Repositories/
│   └── ShipexProMongoRepositoryTests.cs
├── Controllers/
│   ├── MerchantAuthControllerTests.cs
│   └── ShipexProMerchantControllerTests.cs
├── Integration/
│   ├── RateServiceIntegrationTests.cs
│   └── ShipmentFlowIntegrationTests.cs
├── Api/
│   └── ShipexProApiTests.cs
└── TestHelpers/
    ├── TestBase.cs
    ├── MockFactory.cs
    └── MongoTestFixture.cs
```

---

## Test Checklist

### Before Running Tests

- [ ] Test database configured (MongoDB test instance)
- [ ] Test environment variables set
- [ ] Mock services configured
- [ ] Test data fixtures prepared

### Test Coverage Goals

- [ ] Unit tests: ~60% coverage
- [ ] Integration tests: All critical paths
- [ ] API tests: All endpoints
- [ ] Error scenarios tested

### Common Test Scenarios

- [ ] Happy path flows
- [ ] Error handling
- [ ] Validation failures
- [ ] Authentication/authorization
- [ ] Rate limiting
- [ ] Database failures
- [ ] External API failures

---

## Quick Start Testing

### 1. Setup Test Environment

```bash
# Set test MongoDB connection
export MONGODB_TEST_CONNECTION="mongodb://localhost:27017"

# Set test API keys (optional - use mocks)
export ISHIP_TEST_API_KEY="test-key"
export SHIPOX_TEST_API_KEY="test-key"
```

### 2. Run Unit Tests

```bash
cd Shipex/NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests
dotnet test --filter "Category=Unit"
```

### 3. Run Integration Tests

```bash
dotnet test --filter "Category=Integration"
```

### 4. Run API Tests

```bash
# Start test server first
dotnet run --project ../NextGenSoftware.OASIS.API.ONODE.WebAPI

# In another terminal
dotnet test --filter "Category=Api"
```

---

## Next Steps

1. Create test project structure
2. Implement unit tests for all services
3. Add integration tests for critical flows
4. Add API tests for all endpoints
5. Set up CI/CD test pipeline
6. Monitor test coverage

---

**Document Version**: 1.0  
**Last Updated**: January 2025  
**Status**: Ready for Implementation




