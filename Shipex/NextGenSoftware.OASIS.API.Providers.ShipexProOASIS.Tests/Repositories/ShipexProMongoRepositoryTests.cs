using System;
using FluentAssertions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.Repositories;

/// <summary>
/// Tests for ShipexProMongoRepository
/// Note: These tests require a MongoDB instance running
/// </summary>
public class ShipexProMongoRepositoryTests : IClassFixture<MongoTestFixture>
{
    private readonly IShipexProRepository _repository;
    private readonly IMongoDatabase _testDatabase;

    public ShipexProMongoRepositoryTests(MongoTestFixture fixture)
    {
        _testDatabase = fixture.TestDatabase;
        var context = new ShipexProMongoDbContext(fixture.ConnectionString, fixture.DatabaseName);
        _repository = new ShipexProMongoRepository(context);
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
        var quote = new Quote 
        { 
            QuoteId = Guid.NewGuid(), 
            MerchantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
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

    [Fact]
    public async Task SaveMerchantAsync_ValidMerchant_SavesAndReturnsMerchant()
    {
        // Arrange
        var merchant = new Merchant
        {
            MerchantId = Guid.NewGuid(),
            CompanyName = "Test Company",
            ContactInfo = new ContactInfo
            {
                Email = $"test_{Guid.NewGuid()}@example.com",
                Phone = "555-1234",
                Address = "123 Test St"
            },
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.SaveMerchantAsync(merchant);

        // Assert
        result.IsError.Should().BeFalse();
        result.Result.Should().NotBeNull();
        result.Result.MerchantId.Should().Be(merchant.MerchantId);
    }
}

/// <summary>
/// Test fixture for MongoDB connection
/// </summary>
public class MongoTestFixture : IDisposable
{
    public IMongoDatabase TestDatabase { get; }
    public string ConnectionString { get; }
    public string DatabaseName { get; }

    public MongoTestFixture()
    {
        ConnectionString = Environment.GetEnvironmentVariable("MONGODB_TEST_CONNECTION") 
            ?? "mongodb://localhost:27017";
        
        DatabaseName = $"shipex_test_{Guid.NewGuid():N}";
        
        var client = new MongoClient(ConnectionString);
        TestDatabase = client.GetDatabase(DatabaseName);
    }

    public void Dispose()
    {
        // Drop test database
        try
        {
            var client = new MongoClient(ConnectionString);
            client.DropDatabase(DatabaseName);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
