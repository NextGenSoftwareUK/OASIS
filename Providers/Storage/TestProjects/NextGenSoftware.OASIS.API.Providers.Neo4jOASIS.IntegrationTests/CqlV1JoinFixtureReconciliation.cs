using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1FixtureReconciliationTests
{
    [TestMethod]
    public Task ReviewedSuccessfulJoinWireFixtureVerifiesTerminalOwnershipBeforeCleanup()
        => Reconcile("A2C3C7ACD6AB15EC2CD69A11ED3EF5AC31F83D0549CFC5586AC27F2E5293C518", true);

    [TestMethod]
    public async Task ReviewedJoinFixtureReproducesNativeProofBeforeExactReconciliation()
    {
        const string hash = "1F3BF279BA101AFaEFD38A7C2037DC6F9943ED247A4A5D780C787E8B6861A4B9";
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var work = Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        var bytes = await File.ReadAllBytesAsync(Path.Combine(work, "cql-v1-web4-fixture.json"));
        Assert.AreEqual(hash.ToUpperInvariant(), Convert.ToHexString(SHA256.HashData(bytes)));
        using var document = JsonDocument.Parse(bytes);
        var fixture = document.RootElement;
        var tag = fixture.GetProperty("tag").GetString();
        var filter = new { MetadataPath = new[] { "fixture" }, Operator = "Equal", Scalar = "String", Value = tag };
        var body = new
        {
            Kind = "Left", Left = new { Filters = new[] { filter } },
            Right = new { Filters = new object[] { filter, new { Field = "Name", Operator = "Equal", Value = "C-public" } } },
            Keys = new[] { new { Alias = "name", Left = new { Field = "Name" }, Right = new { Field = "Name" } } },
            Columns = new object[] {
                new { Alias = "name", Source = "Left", Selection = new { Field = "Name" } },
                new { Alias = "precision", Source = "Right", AllowMissing = true,
                    Selection = new { MetadataPath = new[] { "precision" }, ValueType = "Int64" } },
                new { Alias = "present", Source = "RightPresence" } }
        };
        var request = JsonSerializer.Deserialize<CqlJoinV1Request>(JsonSerializer.Serialize(body))!;
        var intent = CqlV1Compiler.CompileJoin(request, fixture.GetProperty("alice").GetGuid(), ProviderType.MongoDBOASIS);
        var provider = new MongoProvider(uri, "cosmic_cql_v1_web4_storage");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        try
        {
            var plan = CosmicJoinPlanner.Plan(intent, ProviderType.MongoDBOASIS,
                new[] { new CosmicJoinTarget(ProviderType.MongoDBOASIS, ((NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicQueryStorageProvider)provider).QueryCapabilities, provider.JoinCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            // Read-only query of the unchanged fixture exposes the actual decoder
            // rejection. Never clean up before the native proof passes.
            var result = await provider.ExecuteCosmicJoinAsync(plan.Result,
                new CosmicJoinExecutionLimits(100, 100, 1000, 25, 1048576, 1048576, 1048576));
            Assert.IsFalse(result.IsError, result.ErrorCode + ": " + result.Message + " " + result.Exception);
            Assert.AreEqual(3, result.Result.Rows.Count);
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }

        await Reconcile(hash.ToUpperInvariant(), true);
    }
}
