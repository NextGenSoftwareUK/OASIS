using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.NATSJetStreamOASIS;
using MongoSource = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlNatsHostedAdmissionTests
{
    [Fact]
    public void ConfiguredOwnedModeRequiresExplicitFlagAndSandbox()
    {
        var json = OwnedSource();
        using var source = NatsPublicCqlProfile.FromConfiguredJson(json, "cql_reader", new string('x', 43), "true", true);
        Assert.False(source.IsProviderActivated);
        Assert.Throws<InvalidOperationException>(() => NatsPublicCqlProfile.FromConfiguredJson(
            json, "cql_reader", new string('x', 43), "true", false));
    }

    [Fact]
    public void ConfiguredRemoteModeNeverAdmitsOwnedLoopback()
    {
        foreach (var flag in new string?[] { null, "false" })
            Assert.Throws<ArgumentException>(() => NatsPublicCqlProfile.FromConfiguredJson(
                OwnedSource(), "cql_reader", new string('x', 43), flag, true));
    }

    [Theory]
    [InlineData("TRUE")]
    [InlineData("yes")]
    [InlineData("")]
    public void ConfiguredModeRejectsInvalidRuntimeFlag(string flag)
        => Assert.Throws<ArgumentException>(() => NatsPublicCqlProfile.FromConfiguredJson(
            OwnedSource(), "cql_reader", new string('x', 43), flag, true));

    private static string OwnedSource(string? endpoint = null, string? bucket = null) => JsonSerializer.Serialize(new
    {
        schema = 1, endpoint = endpoint ?? NatsPublicCqlProfile.OwnedEndpoint,
        bucket = bucket ?? NatsPublicCqlProfile.OwnedBucket,
        sourceOwner = Guid.NewGuid(), ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }
    });

    [Fact]
    public void OwnedSourceRequiresSandboxAndRemainsInactiveUntilVerified()
    {
        var json = OwnedSource();
        Assert.Throws<InvalidOperationException>(() => NatsPublicCqlProfile.FromOwnedSandboxJson(
            json, NatsPublicCqlProfile.OwnedReader, new string('x', 43), sandboxEnabled: false));
        using var source = NatsPublicCqlProfile.FromOwnedSandboxJson(
            json, NatsPublicCqlProfile.OwnedReader, new string('x', 43), sandboxEnabled: true);
        Assert.False(source.IsProviderActivated);
        Assert.Throws<ArgumentException>(() => NatsPublicCqlProfile.FromJson(json));
    }

    [Theory]
    [InlineData("nats://127.0.0.1:24222")]
    [InlineData("tls://127.0.0.1:24223")]
    [InlineData("tls://nats.example:24222")]
    public void OwnedSourceRejectsPlaintextAndForeignEndpoints(string endpoint)
        => Assert.Throws<ArgumentException>(() => NatsPublicCqlProfile.FromOwnedSandboxJson(
            OwnedSource(endpoint), NatsPublicCqlProfile.OwnedReader, new string('x', 43), sandboxEnabled: true));

    [Theory]
    [InlineData("writer", "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")]
    [InlineData("cql_reader", "short")]
    [InlineData("cql_reader", "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx ")]
    public void OwnedSourceRequiresItsExplicitBoundedReaderCredentials(string username, string password)
        => Assert.Throws<ArgumentException>(() => NatsPublicCqlProfile.FromOwnedSandboxJson(
            OwnedSource(), username, password, sandboxEnabled: true));

    [Fact]
    public void OwnedSourceRejectsAnotherBucket()
        => Assert.Throws<ArgumentException>(() => NatsPublicCqlProfile.FromOwnedSandboxJson(
            OwnedSource(bucket: "foreign"), NatsPublicCqlProfile.OwnedReader, new string('x', 43), sandboxEnabled: true));

    [Fact]
    public void ExactKeyPresetsPlanNativeAndMongoQueries()
    {
        const string provider = nameof(ProviderType.NATSJetStreamOASIS);
        var keys = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        using var source = NatsPublicCqlProfile.FromJson(JsonSerializer.Serialize(new
        {
            schema = 1,
            endpoint = "tls://nats.example:4222", bucket = "cosmic_demo",
            ids = keys,
            sourceOwner = Guid.NewGuid()
        }));
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.MongoDBOASIS },
            new[] { new StorageQueryTariff(ProviderType.NATSJetStreamOASIS, 0, "demo-read-v1") },
            null, new[] { ProviderType.NATSJetStreamOASIS });
        Assert.Contains(ProviderType.NATSJetStreamOASIS, admission.QueryOnlyProviders);
        var rows = new List<object>();
        foreach (int count in new[] { 3, 2 })
        {
            var selection = new
            {
                orderBy = "None", take = 25, includeMetadata = true,
                filters = new[] { new
                {
                    field = "Id",
                    scalar = "Guid", @operator = "In", value = keys.Take(count).ToArray()
                } }
            };
            rows.Add(new { id = $"native-{count}", label = "NATS records", request = new
            {
                provider, selection.orderBy, selection.take, selection.includeMetadata, selection.filters
            } });
            rows.Add(new { id = $"paired-{count}", label = "NATS and Mongo", operation = "Federation", request = new
            {
                scope = "Named", providers = new[] { nameof(ProviderType.MongoDBOASIS), provider },
                source = selection, identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 25
            } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [provider] = rows
        }));
        var planned = examples.For(ProviderType.NATSJetStreamOASIS, source.QueryCapabilities,
            target => target == ProviderType.MongoDBOASIS ? MongoSource.CqlCapabilities : null,
            (target, actor) => target == ProviderType.NATSJetStreamOASIS
                ? CosmicQueryReadAccess.AuthoritativeRecords(actor, target, keys).Result
                : CosmicQueryReadAccess.OwnerOrPublic(actor).Result);
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));
        Assert.False(source.IsProviderActivated);
    }
}
