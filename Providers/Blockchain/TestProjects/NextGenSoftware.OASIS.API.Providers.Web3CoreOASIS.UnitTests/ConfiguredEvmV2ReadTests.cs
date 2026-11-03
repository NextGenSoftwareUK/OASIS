using System.Text.Json;
using CosmicCqlDemo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

internal sealed record EvmV2ReadSource(string Provider, string RpcEndpoint, long ChainId,
    string Contract, Guid Actor, Guid[] Ids, string? MongoDatabase = null, Guid[]? MongoIds = null)
{
    internal Web3CoreOASISBaseProvider Create()
    {
        // This local teaching reader must never sign production transactions.
        if (!Uri.TryCreate(RpcEndpoint, UriKind.Absolute, out var rpc) || !rpc.IsLoopback
            || rpc.Scheme != "http" || rpc.UserInfo.Length != 0 || rpc.Query.Length != 0 || rpc.Fragment.Length != 0
            || ChainId <= 0 || Actor == Guid.Empty || Ids == null || Ids.Length != 3
            || Ids.Contains(Guid.Empty) || Ids.Distinct().Count() != 3 || Contract == null
            || Contract.Length != 42 || !Contract.StartsWith("0x", StringComparison.Ordinal)
            || Contract[2..].Any(c => !Uri.IsHexDigit(c)) || Contract[2..].All(c => c == '0'))
            throw new ArgumentException("Supply explicit loopback RPC, positive chain, compatible contract, actor and three authorized IDs.");

        // Public local-test identity, used only by the read-only consumer.
        const string signer = "0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d";
        return Provider switch
        {
            "Ethereum" => (Web3CoreOASISBaseProvider)EthereumProviderFactory.CreateForMode(
                new EthereumOASISProviderSettings { ConnectionString = RpcEndpoint, ChainPrivateKey = signer,
                    ContractAddress = Contract, ChainId = ChainId }, HyperDriveModes.V2, null!),
            "Arbitrum" => (Web3CoreOASISBaseProvider)EvmProviderFactory.CreateArbitrum(
                new ArbitrumOASISProviderSettings { ConnectionString = RpcEndpoint, ChainPrivateKey = signer,
                    ContractAddress = Contract, ChainId = ChainId }, HyperDriveModes.V2, null!),
            "Polygon" => EvmProviderFactory.CreatePolygon(
                new PolygonOASISProviderSettings { ConnectionString = RpcEndpoint, ChainPrivateKey = signer,
                    ContractAddress = Contract, ChainId = ChainId }, null!),
            _ => throw new ArgumentException("Choose an explicit Ethereum, Polygon or Arbitrum V2 source.")
        };
    }
}

[TestClass]
public class ConfiguredEvmV2ReadTests
{
    [DataTestMethod]
    [DataRow("Ethereum", "EthereumOASIS_Web3Core")]
    [DataRow("Arbitrum", "ArbitrumOASIS_Web3Core")]
    [DataRow("Polygon", "PolygonOASIS")]
    public void ConfigurationSelectsShippingV2FacadeWithoutNativeIo(string name, string type)
    {
        var provider = Source(name).Create();
        Assert.AreEqual(type, provider.GetType().Name);
        Assert.IsInstanceOfType(provider, typeof(NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicQueryStorageProvider));
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [DataTestMethod]
    [DataRow("remote")]
    [DataRow("chain")]
    [DataRow("ids")]
    [DataRow("legacy")]
    public void InvalidOrImplicitSourceRejectsBeforeActivation(string mode)
    {
        var source = Source("Ethereum");
        source = mode switch
        {
            "remote" => source with { RpcEndpoint = "https://example.invalid" },
            "chain" => source with { ChainId = 0 },
            "ids" => source with { Ids = new[] { Guid.NewGuid() } },
            _ => source with { Provider = "LegacyEthereum" }
        };
        Assert.ThrowsException<ArgumentException>(() => source.Create());
    }

    private static EvmV2ReadSource Source(string provider) => new(provider, "http://127.0.0.1:18547",
        31338, "0x" + new string('1', 40), Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());

    [TestMethod]
    public async Task ExplicitConfiguredV2SourceRunsReadOnlyTeachingConsumer()
    {
        if (Environment.GetEnvironmentVariable("OASIS_EVM_V2_CONFIGURED_READ") != "1")
            Assert.Inconclusive("Explicit compatible retained configuration required.");
        var path = Environment.GetEnvironmentVariable("OASIS_EVM_V2_SOURCE_CONFIG")!;
        var original = await File.ReadAllBytesAsync(path);
        var source = JsonSerializer.Deserialize<EvmV2ReadSource>(original)!;
        var provider = source.Create();
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS? mongo = null;
        try
        {
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
            var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
            var paired = Environment.GetEnvironmentVariable("OASIS_EVM_V2_CONFIGURED_FEDERATED") == "1";
            if (paired)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(source.MongoDatabase));
                Assert.IsNotNull(source.MongoIds);
                Assert.IsTrue(source.MongoIds.Length > 0 && source.MongoIds.All(id => id != Guid.Empty));
                var client = new MongoDB.Driver.MongoClient("mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000");
                using var databases = await client.ListDatabaseNamesAsync();
                var names = await databases.ToListAsync();
                Assert.IsTrue(names.Contains(source.MongoDatabase), "Select an existing database; do not initialize another.");
                mongo = new("mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000", source.MongoDatabase);
                Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
                var full = await EvmV2OwnedWalkthrough.RunWithMongoAsync(manager, mongo, provider, source.Ids,
                    source.MongoIds, source.Ids, source.Actor, Console.Out);
                var copies = source.MongoIds.Intersect(source.Ids).Count();
                Assert.IsTrue(copies > 0, "Mongo must contribute actual selected records.");
                Assert.AreEqual(3 + copies, full.Rows.Count);
                var selected = source.Ids.Take(2).ToArray();
                var subset = await EvmV2OwnedWalkthrough.RunWithMongoAsync(manager, mongo, provider, selected,
                    source.MongoIds, source.Ids, source.Actor, Console.Out);
                Assert.AreEqual(2 + source.MongoIds.Intersect(selected).Count(), subset.Rows.Count);
                if (Environment.GetEnvironmentVariable("OASIS_EVM_V2_CONFIGURED_SERVE") == "1")
                {
                    var address = source.Provider switch
                    {
                        "Polygon" => "http://127.0.0.1:5098",
                        "Arbitrum" => "http://127.0.0.1:5099",
                        _ => "http://127.0.0.1:5097"
                    };
                    await using var app = IdentityFederationApiHost.Build(manager, mongo, provider,
                        source.Actor, source.Ids, source.MongoIds, source.Ids, address,
                        preserveExistingRecords: true);
                    await app.StartAsync();
                    await Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions.WaitForShutdownAsync(app);
                }
            }
            else
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var counts = await EvmV2OwnedWalkthrough.RunAsync(manager, provider, source.Ids,
                    source.Actor, Console.Out, deadline.Token);
                Assert.AreEqual(3, counts.Full);
                Assert.AreEqual(2, counts.Subset);
            }
            CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            if (mongo != null) Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
        }
    }
}
