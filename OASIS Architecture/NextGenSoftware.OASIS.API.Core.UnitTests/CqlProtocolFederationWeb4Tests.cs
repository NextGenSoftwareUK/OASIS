using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private sealed class ProtocolFederationSources : ICqlV1Providers
    {
        internal readonly Dictionary<ProviderType, Source> Items = new();
        public IOASISStorageProvider Resolve(ProviderType? requested)
            => Items[requested ?? ProviderType.MongoDBOASIS].Resolve(requested);
        public HolonManager CreateManager(IOASISStorageProvider provider)
            => Items[provider.ProviderType.Value].CreateManager(provider);
        internal void NeverRead()
        {
            foreach (var source in Items.Values)
                source.Adapter.Verify(adapter => adapter.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(),
                    It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    private static (ProtocolFederationSources Sources, DefaultHttpContext Http, PreparedStorageExecution Policy,
        List<UsageProviderReceipt> Receipts, CqlFederationV1Request Request) ProtocolFederation(
        ProviderType protocol, bool allEligible = false, bool tariff = true, int admittedAttempts = 512)
    {
        var sources = new ProtocolFederationSources();
        var targets = allEligible ? FederationProviders.Append(protocol).ToArray()
            : new[] { ProviderType.MongoDBOASIS, protocol };
        foreach (var target in targets)
        {
            var source = new Source(target);
            // Controlled adapter capability fixture, not proof of a native database.
            source.Adapter.SetupGet(adapter => adapter.QueryCapabilities)
                .Returns(CosmicQueryCapabilities.ForStringMetadataKeyLookup("NativeKey", 128, 40));
            sources.Items.Add(target, source);
        }
        for (var index = 1; index <= 3; index++)
        {
            sources.Items[protocol].Rows.Add(new Holon { Id = Guid.NewGuid(), IsPublic = true,
                Name = "public " + index, MetaData = new() { ["NativeKey"] = index.ToString() } });
            if (index <= 2)
                sources.Items[ProviderType.MongoDBOASIS].Rows.Add(new Holon { Id = Guid.NewGuid(),
                    CreatedByAvatarId = Actor, Name = "owned " + index, MetaData = new() { ["NativeKey"] = index.ToString() } });
        }
        var admitted = Admission(ProviderType.MongoDBOASIS, maximumProviderAttempts: admittedAttempts);
        var handle = (UsageExecutionHandle)admitted.Http.Items[Web4UsageMeasurement.ExecutionKey]!;
        var policy = new PreparedStorageExecution(handle, "protocol-federation-test", new Dictionary<ProviderType, string>(),
            targets, targets.Where(type => tariff || type != protocol).Select(type => new StorageQueryTariff(type, 0.002m, "test-v1")),
            null, false, null, tariff ? new[] { protocol } : Array.Empty<ProviderType>());
        var request = new CqlFederationV1Request
        {
            Scope = allEligible ? "AllEligible" : "Named",
            Providers = allEligible ? Array.Empty<string>() : targets.Select(type => type.ToString()).ToArray(),
            MaximumProviderAttempts = targets.Length,
            Source = new CqlV1Request { OrderBy = "None", Take = 10, Filters = new[] {
                new CqlV1Filter { MetadataPath = new[] { "NativeKey" }, Operator = CqlV1Operator.In,
                    Value = JsonSerializer.SerializeToElement(new[] { "1", "2", "3" }) }
            } }
        };
        return (sources, admitted.Http, policy, admitted.Receipts, request);
    }

    [Theory]
    [InlineData(ProviderType.ActivityPubOASIS)]
    [InlineData(ProviderType.IPFSOASIS)]
    [InlineData(ProviderType.SOLIDOASIS)]
    public async Task UnorderedNamedProtocolFederationPreservesBothSourcesAndPricesEachPhysicalRead(ProviderType protocol)
    {
        var test = ProtocolFederation(protocol);
        var result = await test.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(test.Http, test.Request, test.Sources));
        using var response = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
        var output = response.RootElement.GetProperty("result");
        Assert.Equal(5, output.GetProperty("observedInputRows").GetInt32());
        var rows = output.GetProperty("rows").EnumerateArray().ToArray();
        Assert.Equal(3, rows.Count(row => row.GetProperty("selected").GetProperty("provider").GetString() == protocol.ToString()));
        Assert.Equal(2, rows.Count(row => row.GetProperty("selected").GetProperty("provider").GetString() == "MongoDBOASIS"));
        Assert.Equal(2, test.Receipts.Count);
        Assert.Equal(0.004m, test.Receipts.Sum(receipt => receipt.ActualCostUsd));
        foreach (var pair in test.Sources.Items)
        {
            Assert.Equal(Actor, pair.Value.Observed!.Query.ReadAccess.ActorId);
            Assert.Equal(pair.Key, Assert.Single(pair.Value.Observed.Query.Scope.Providers));
            Assert.Empty(pair.Value.Observed.Query.Orderings);
            Assert.IsType<CosmicQueryMembership>(Assert.Single(pair.Value.Observed.Query.BusinessPredicates));
        }
    }

    [Fact]
    public async Task AllEligibleDoesNotSilentlyOmitTheFourthTariffEnrolledReadSource()
    {
        var test = ProtocolFederation(ProviderType.ActivityPubOASIS, allEligible: true);
        var result = await test.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(test.Http, test.Request, test.Sources));
        using var response = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
        Assert.Equal(FederationProviders.Append(ProviderType.ActivityPubOASIS).Select(type => type.ToString()),
            response.RootElement.GetProperty("result").GetProperty("providers").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal(4, test.Receipts.Count);
    }

    [Theory]
    [InlineData("tariff")]
    [InlineData("capability")]
    [InlineData("attempts")]
    [InlineData("descending")]
    [InlineData("authorization")]
    public async Task CompleteProtocolFederationAdmissionPrecedesEveryNativeRead(string fault)
    {
        var test = ProtocolFederation(ProviderType.ActivityPubOASIS, tariff: fault != "tariff");
        using var services = new ServiceCollection().AddSingleton<ICqlReadAccessPolicy>(new FederationPolicy
            { Denied = fault == "authorization" ? ProviderType.ActivityPubOASIS : null }).BuildServiceProvider();
        if (fault == "authorization") test.Http.RequestServices = services;
        if (fault == "capability") test.Sources.Items[ProviderType.ActivityPubOASIS].Adapter
            .SetupGet(adapter => adapter.QueryCapabilities).Returns(CosmicQueryCapabilities.ForStringMetadataPointLookup("NativeKey"));
        if (fault == "attempts") test.Request.MaximumProviderAttempts = 1;
        if (fault == "descending") test.Request.Source.Descending = true;
        var result = await test.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(test.Http, test.Request, test.Sources));
        Assert.IsType<ObjectResult>(result);
        test.Sources.NeverRead();
        Assert.Empty(test.Receipts);
    }

    [Fact]
    public async Task CallerCannotExpandTheFrozenRouteAttemptReservationByRequestingMoreProviders()
    {
        var test = ProtocolFederation(ProviderType.ActivityPubOASIS, allEligible: true, admittedAttempts: 3);
        test.Request.MaximumProviderAttempts = 4;
        var result = await test.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(test.Http, test.Request, test.Sources));
        Assert.Equal("CQL_FEDERATION_ADMISSION_BUDGET", Code(result));
        test.Sources.NeverRead();
        Assert.Empty(test.Receipts);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.ActivityPubOASIS)]
    public async Task ProtocolFederationNeverPublishesAForeignPrivateRowFromEitherAdapter(ProviderType faulty)
    {
        var test = ProtocolFederation(ProviderType.ActivityPubOASIS);
        var row = test.Sources.Items[faulty].Rows[0];
        row.IsPublic = false;
        row.CreatedByAvatarId = Guid.NewGuid();
        var result = await test.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(test.Http, test.Request, test.Sources));
        Assert.Equal("CQL_VISIBILITY_VIOLATION", Code(result));
        Assert.Equal(503, Assert.IsType<ObjectResult>(result).StatusCode);
    }
}
