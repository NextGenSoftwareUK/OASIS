using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.UrbitOASIS;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private static readonly ProviderType[] UrbitFederationProviders = { ProviderType.MongoDBOASIS, ProviderType.UrbitOASIS };

    private sealed class UrbitFederatedSources(Source mongo, UrbitContactBookQuerySource urbit) : ICqlV1Providers
    {
        internal int Managers;
        public IOASISStorageProvider Resolve(ProviderType? requested)
        {
            if (requested is null or ProviderType.MongoDBOASIS) return mongo.Resolve(ProviderType.MongoDBOASIS);
            if (requested == ProviderType.UrbitOASIS && urbit.IsProviderActivated) return urbit;
            throw new InvalidOperationException("Every requested source must be explicitly active.");
        }
        public HolonManager CreateManager(IOASISStorageProvider provider)
        {
            Managers++;
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
            return new HolonManager(provider, dna, new ProviderManager(null, dna) {
                IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false
            });
        }
    }

    private static CqlFederationV1Request UrbitFederationRequest(Guid[] ids) => new() {
        Scope = "Named", Providers = UrbitFederationProviders.Select(type => type.ToString()).ToArray(),
        Source = new CqlV1Request { OrderBy = "None", IncludeMetadata = true, Filters = new[] {
            new CqlV1Filter { Field = "Id", Operator = CqlV1Operator.In, Value = JsonSerializer.SerializeToElement(ids) }
        } }, Take = 8, MaximumInputRows = 16, MaximumRowsPerProvider = 8, MaximumProviderAttempts = 2
    };

    private static Source ControlledUrbitMongo(IEnumerable<Guid> ids)
    {
        var mongo = new Source(ProviderType.MongoDBOASIS);
        mongo.Adapter.SetupGet(adapter => adapter.QueryCapabilities).Returns(new CosmicQueryCapabilities(
            Capabilities.Features | CosmicQueryFeatures.Membership, Capabilities.CanonicalFields.ToArray()));
        mongo.Rows.AddRange(ids.Select(id => new Holon { Id = id, CreatedByAvatarId = Actor,
            IsPublic = false, Name = "Controlled canonical copy" }));
        return mongo;
    }

    [Fact]
    public async Task UrbitAdmittedFederationPreservesCopiesProvenanceAndIndependentPermissions()
    {
        var wire = new UrbitWire();
        using var http = new HttpClient(wire);
        using var reader = new UrbitContactsV1Client(new Uri("https://ship.example.test"), http);
        using var urbit = new UrbitContactBookQuerySource(reader, Actor, UrbitConnection, UrbitCids);
        Assert.False((await urbit.ActivateQuerySourceAsync()).IsError);
        var ids = UrbitCids.Select(cid => UrbitContactBookQuerySource.LogicalId(Actor, UrbitConnection, cid)).ToArray();
        var mongo = ControlledUrbitMongo(new[] { ids[0], ids[2] });
        var sources = new UrbitFederatedSources(mongo, urbit);
        using var services = UrbitPolicyServices(Actor);
        foreach (var selected in new[] { ids, new[] { ids[0], ids[2] } })
        {
            var admitted = Admission(ProviderType.MongoDBOASIS);
            admitted.Http.RequestServices = services;
            var handle = (UsageExecutionHandle)admitted.Http.Items[Web4UsageMeasurement.ExecutionKey]!;
            var policy = new PreparedStorageExecution(handle, "urbit-federation-test", new Dictionary<ProviderType, string>(),
                UrbitFederationProviders, UrbitFederationProviders.Select(type => new StorageQueryTariff(type, 0.002m, "test-v1")));
            var result = await policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(admitted.Http,
                UrbitFederationRequest(selected), sources));
            using var document = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
            var output = document.RootElement.GetProperty("result");
            var rows = output.GetProperty("rows").EnumerateArray().Select(row => row.GetProperty("selected")).ToArray();
            Assert.Equal(selected.Length + 2, output.GetProperty("observedInputRows").GetInt32());
            Assert.Equal(selected.Length + 2, rows.Length);
            Assert.Equal(selected.Length, rows.Count(row => row.GetProperty("provider").GetString() == "UrbitOASIS"));
            Assert.Equal(2, rows.Count(row => row.GetProperty("provider").GetString() == "MongoDBOASIS"));
            Assert.Equal(selected.Length, rows.Select(row => row.GetProperty("row").GetProperty("id").GetGuid()).Distinct().Count());
            Assert.All(rows, row => {
                Assert.False(row.GetProperty("row").GetProperty("isPublic").GetBoolean());
                Assert.Equal(Actor, row.GetProperty("row").GetProperty("createdByAvatarId").GetGuid());
            });
            Assert.Equal(2, admitted.Receipts.Count);
            Assert.Equal(UrbitFederationProviders.Order(), admitted.Receipts.Select(receipt => Enum.Parse<ProviderType>(receipt.Provider)).Order());
            Assert.All(admitted.Receipts, receipt => Assert.Equal(1, receipt.Units));
            Assert.Null(mongo.Observed!.Query.ReadAccess.Authority);
            Assert.Equal(Actor, mongo.Observed.Query.ReadAccess.ActorId);
        }
        Assert.Equal(6, wire.Calls);
        Assert.Equal(2, sources.Managers);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UrbitAdmittedFederationRejectsPrivateDenialOrInactiveSourceBeforeAnyRead(bool foreignOwner)
    {
        var owner = foreignOwner ? Guid.NewGuid() : Actor;
        var wire = new UrbitWire();
        using var http = new HttpClient(wire);
        using var reader = new UrbitContactsV1Client(new Uri("https://ship.example.test"), http);
        using var urbit = new UrbitContactBookQuerySource(reader, owner, UrbitConnection, UrbitCids);
        Assert.False((await urbit.ActivateQuerySourceAsync()).IsError);
        if (!foreignOwner) Assert.False((await urbit.DeActivateProviderAsync()).IsError);
        var ids = UrbitCids.Select(cid => UrbitContactBookQuerySource.LogicalId(owner, UrbitConnection, cid)).ToArray();
        var mongo = ControlledUrbitMongo(new[] { ids[0], ids[2] });
        var sources = new UrbitFederatedSources(mongo, urbit);
        using var services = UrbitPolicyServices(owner);
        var admitted = Admission(ProviderType.MongoDBOASIS);
        admitted.Http.RequestServices = services;
        var handle = (UsageExecutionHandle)admitted.Http.Items[Web4UsageMeasurement.ExecutionKey]!;
        var policy = new PreparedStorageExecution(handle, "urbit-federation-rejection", new Dictionary<ProviderType, string>(),
            UrbitFederationProviders, UrbitFederationProviders.Select(type => new StorageQueryTariff(type, 0.002m, "test-v1")));
        var result = await policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(admitted.Http,
            UrbitFederationRequest(ids), sources));
        Assert.Equal(foreignOwner ? 403 : 503, Assert.IsType<ObjectResult>(result).StatusCode);
        if (foreignOwner) Assert.Equal("CQL_ACCESS_FORBIDDEN", Code(result));
        Assert.Equal(1, wire.Calls);
        Assert.Equal(0, sources.Managers);
        Assert.Empty(admitted.Receipts);
        mongo.Adapter.Verify(adapter => adapter.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
