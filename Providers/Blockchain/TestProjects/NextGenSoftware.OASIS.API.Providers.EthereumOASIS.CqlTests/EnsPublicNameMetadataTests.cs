using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.ENSOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class EnsPublicNameMetadataTests
{
    private static readonly string[] Names = { "nick.eth", "vitalik.eth", "ur.integration-tests.eth" };

    [DataTestMethod]
    [DataRow("full")]
    [DataRow("point")]
    [DataRow("pair")]
    [DataRow("page")]
    [DataRow("unknown")]
    [DataRow("inactive")]
    [DataRow("cancel")]
    [DataRow("budget")]
    [DataRow("native-error")]
    [DataRow("empty-page-error")]
    public async Task SelectionPreservesProvenanceOrFailsWithoutPartialRows(string scenario)
    {
        using var handler = new EnsPublicResolutionReaderTests.RpcHandler();
        using var client = new HttpClient(handler);
        var owner = Guid.NewGuid();
        using var source = new EnsPublicNameMetadataSource(client, new Uri("https://example.invalid/"), Names, owner);
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        handler.Calls = 0;
        if (scenario is "native-error" or "empty-page-error") handler.Fault = "sentinel";
        var selected = scenario == "pair" ? Names.Take(2).ToArray() : scenario == "unknown" ? new[] { "unknown.eth" } : Names;
        var scope = CosmicQueryScope.Named(ProviderType.ENSOASIS).Result;
        var query = CosmicQuery<Holon>.Create(scope).Where(h => selected.Contains(CosmicQueryMetadata.Value<string>(h, "EnsName"))).Result;
        if (scenario == "point") query = CosmicQuery<Holon>.Create(scope)
            .Where(h => CosmicQueryMetadata.Value<string>(h, "EnsName") == "nick.eth").Result;
        if (scenario == "page") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-page-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.ENSOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ENSOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "full" or "point" or "pair" or "page")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario is "point" or "page" ? 1 : scenario == "pair" ? 2 : 3, result.Result.Count);
            foreach (var row in result.Result)
            {
                Assert.IsTrue(row.IsPublic); Assert.AreEqual(owner, row.CreatedByAvatarId);
                Assert.AreEqual(row.Name, row.MetaData["EnsName"]);
                Assert.AreEqual("0x" + new string('a',64), row.MetaData["EnsBlockHash"]);
                Assert.AreEqual("false", row.MetaData["EnsConsensusVerified"]);
                Assert.AreEqual(EnsUniversalResolverCodec.MainnetProxy, row.MetaData["EnsUniversalResolver"]);
                Assert.AreEqual(row.Name, row.ProviderUniqueStorageKey[ProviderType.ENSOASIS]);
                Assert.AreEqual(0, row.Children.Count());
            }
        }
        else
        {
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Result == null || result.Result.Count == 0);
        }
        if (scenario is "unknown" or "inactive" or "cancel") Assert.AreEqual(0, handler.Calls);
        if (scenario == "empty-page-error") Assert.AreEqual(6, handler.Calls);
    }

    [TestMethod]
    public async Task UnsupportedFiltersAndWritesDoNotReachNativeTransport()
    {
        using var handler = new EnsPublicResolutionReaderTests.RpcHandler();
        using var client = new HttpClient(handler);
        using var source = new EnsPublicNameMetadataSource(client, new Uri("https://example.invalid/"), Names, Guid.NewGuid());
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ENSOASIS).Result).Where(h => h.Name == "nick.eth").Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(query, ProviderType.ENSOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ENSOASIS, source.QueryCapabilities) }).IsError);
        Assert.IsTrue((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false)).IsError);
        Assert.IsTrue((await source.DeleteHolonAsync(Guid.NewGuid())).IsError);
        Assert.AreEqual(0, handler.Calls);
    }

    [DataTestMethod]
    [DataRow("duplicate")]
    [DataRow("unknown")]
    [DataRow("empty-owner")]
    [DataRow("uppercase")]
    [DataRow("duplicate-name")]
    [DataRow("credentials")]
    public void ConfigurationRejectsAmbiguityAndUnsupportedIdentity(string scenario)
    {
        var fields = new Dictionary<string, object> { ["schema"] = 1, ["rpcUrl"] = "https://example.invalid/",
            ["names"] = Names, ["sourceOwner"] = Guid.NewGuid() };
        if (scenario == "unknown") fields["apiKey"] = "inert";
        if (scenario == "empty-owner") fields["sourceOwner"] = Guid.Empty;
        if (scenario == "uppercase") fields["names"] = new[] { "Nick.eth" };
        if (scenario == "duplicate-name") fields["names"] = new[] { "nick.eth", "nick.eth" };
        if (scenario == "credentials") fields["rpcUrl"] = "https://user:inert@example.invalid/";
        var json = JsonSerializer.Serialize(fields);
        if (scenario == "duplicate") json = json.Insert(1, "\"schema\":1,");
        try
        {
            using var source = EnsPublicNameMetadataSource.FromJson(json);
            Assert.Fail("Invalid source was accepted.");
        }
        catch (Exception error) when (error is ArgumentException or InvalidDataException) { }
    }
}
