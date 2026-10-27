using System.Security.Cryptography;
using System.Text.Json;
using CosmicCqlDemo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed partial class CqlAllRetainedFixtureTests
{
    [TestMethod]
    public async Task ReviewedAllPredicatesPreserveExistingFixtureAndProveEmptyInput()
    {
        // Read-only reproduction of the failed run. Never seed, reset, reissue
        // writes or discard its durable receipts to test a query correction.
        var work = Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        var path = Path.Combine(work, "cql-v1-web4-fixture.json");
        var original = await File.ReadAllBytesAsync(path);
        Assert.AreEqual("892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5",
            Convert.ToHexString(SHA256.HashData(original)));
        using var json = JsonDocument.Parse(original);
        var actor = json.RootElement.GetProperty("alice").GetGuid();
        var tag = json.RootElement.GetProperty("tag").GetString();
        var providers = new IOASISStorageProvider[] {
            new MongoProvider("mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000", "cosmic_cql_v1_web4_storage"),
            new SqliteProvider("Data Source=" + Path.Combine(work, "cql-v1-web4.sqlite")),
            new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only") };
        foreach (var provider in providers)
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            try
            {
                var adapter = (ICosmicQueryStorageProvider)provider;
                var scope = CosmicQueryScope.Named(adapter.QueryProviderType).Result;
                var visible = CosmicQuery<Holon>.Create(scope)
                    .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                    .Where(row => CosmicQueryMetadata.Value<string>(row, "fixture") == tag);
                Assert.IsFalse(visible.IsError, visible.Message);
                var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                {
                    HyperDriveMode = HyperDriveModes.V2,
                    StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
                }};
                var registry = new ProviderManager(null, dna)
                {
                    IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false
                };
                Assert.AreEqual(92, await V1AllQueries.RunAsync(new HolonManager(provider, dna, registry),
                    adapter, visible.Result, TextWriter.Null, tag));
            }
            finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
        }
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path));
    }
}
