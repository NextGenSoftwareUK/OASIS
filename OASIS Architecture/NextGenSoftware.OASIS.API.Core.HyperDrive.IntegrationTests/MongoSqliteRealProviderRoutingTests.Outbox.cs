using System.Globalization;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    // Synthetic serialization evidence only: no native transaction, quote authority or user charge.
    private static async Task VerifyNativeUsageOutboxSerializationAsync(string uri,string database,IMongoDatabase rawDatabase)
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var token=deadline.Token;
        // Exercise class-map resolution before the store, as another host component could do.
        BsonSerializer.LookupSerializer<NativeFeePriceQuote>();
        BsonSerializer.LookupSerializer<NativeFeeValuation>();
        var store=new MongoUsageOutboxStore(uri,database);
        await store.InitializeAsync(token);
        var operationId=Guid.NewGuid();var owner=Guid.NewGuid().ToString("D");
        var now=DateTime.UtcNow.AddSeconds(-1).AddTicks(1234);
        const decimal rate=1.234567890123456789m;
        var quote=new NativeFeePriceQuote(operationId,"SuiOASIS","serialization-fixture","MIST","synthetic-asset",9,rate,
            "serialization-only","fixture-v1","fixture-v1",now,now,now,now.AddMinutes(5));
        (await store.TryBeginAsync(new UsageOutboxOperation {Id=operationId.ToString("D"),ExecutionOwner=owner,Service="WEB4",
            UserId=Guid.NewGuid().ToString("D"),Fingerprint=new string('a',64),CreatedAtUtc=now,UpdatedAtUtc=now,
            Policy=new UsageEndpointPolicy {MeterCategory="api.request",ReservedUnits=1}},token)).Should().BeTrue();
        await store.BeginProviderAttemptAsync(operationId.ToString("D"),owner,new UsageProviderAttempt {
            ClientRequestId="serialization-only",Provider="SuiOASIS",ProviderRequestId="synthetic-not-submitted",StartedAtUtc=now,
            NativePreparation=new() {ChainId=quote.ChainId,Sender="synthetic-payer",SignedTransactionBase64="AQID",PriceQuotes=new[]{quote}}},token);
        var valuation=quote.ValueOriginalReceipt("synthetic-not-submitted","1000000000");
        await store.AppendReceiptAsync(operationId.ToString("D"),owner,new UsageProviderReceipt {
            ClientRequestId="serialization-only",Provider="SuiOASIS",ProviderRequestId="synthetic-not-submitted",Units=1,
            ActualCostUsd=rate,CostSource="serialization-only",PricingCatalogueVersion="fixture-v1",NativeFeeValuations=new(){valuation}},token);
        // Reconstruct provider and store after both sets of BSON maps have frozen.
        new MongoDBOASIS(uri,database);
        var loaded=await new MongoUsageOutboxStore(uri,database).GetAsync(operationId.ToString("D"),token);
        loaded.ProviderAttempts.Single().NativePreparation.PriceQuotes.Single().Should().Be(quote);
        loaded.Receipts.Single().NativeFeeValuations.Single().Should().Be(valuation);
        loaded.Receipts.Single().ActualCostUsd.Should().Be(rate);
        var raw=await rawDatabase.GetCollection<BsonDocument>("subscription_usage_outbox_v1")
            .Find(new BsonDocument("_id",operationId.ToString("D"))).SingleAsync(token);
        var storedQuote=raw["ProviderAttempts"][0]["NativePreparation"]["PriceQuotes"][0];
        storedQuote["OperationId"].AsBsonBinaryData.SubType.Should().Be(BsonBinarySubType.UuidStandard);
        storedQuote["UsdPerWholeUnit"].AsDecimal128.Should().Be(new Decimal128(rate));
        storedQuote["BoundAtUtc"].BsonType.Should().Be(BsonType.String);
        DateTime.Parse(storedQuote["BoundAtUtc"].AsString,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).Should().Be(now);
        var storedValue=raw["Receipts"][0]["NativeFeeValuations"][0];
        storedValue["OperationId"].AsBsonBinaryData.SubType.Should().Be(BsonBinarySubType.UuidStandard);
        storedValue["NetCostUsd"].AsDecimal128.Should().Be(new Decimal128(rate));
        storedValue["ValuedAtUtc"].BsonType.Should().Be(BsonType.String);
    }
}
