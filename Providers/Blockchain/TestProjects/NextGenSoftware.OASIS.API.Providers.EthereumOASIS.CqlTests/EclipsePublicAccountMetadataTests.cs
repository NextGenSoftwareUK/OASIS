using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.EclipseOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class EclipsePublicAccountMetadataTests
{
    private static readonly string[] Addresses = EclipsePublicAccountReaderTests.Keys;

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
        using var handler = new EclipsePublicAccountReaderTests.Native();
        using var client = new HttpClient(handler);
        var owner = Guid.NewGuid();
        using var source = new EclipsePublicAccountMetadataSource(client, new Uri("https://example.invalid/"), Addresses, Addresses[0], owner);
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        handler.Starts.Clear();
        if (scenario is "native-error" or "empty-page-error") handler.Fault = "absent";
        var selected = scenario == "pair" ? Addresses.Take(2).ToArray() : scenario == "unknown" ? new[] { "11111111111111111111111111111112" } : Addresses;
        var scope = CosmicQueryScope.Named(ProviderType.EclipseOASIS).Result;
        var query = CosmicQuery<Holon>.Create(scope).Where(h => selected.Contains(CosmicQueryMetadata.Value<string>(h, "EclipseAccountAddress"))).Result;
        if (scenario == "point") query = CosmicQuery<Holon>.Create(scope)
            .Where(h => CosmicQueryMetadata.Value<string>(h, "EclipseAccountAddress") == "11111111111111111111111111111111").Result;
        if (scenario == "page") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-page-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.EclipseOASIS,
            new[] { new CosmicQueryTarget(ProviderType.EclipseOASIS, source.QueryCapabilities) });
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
                Assert.AreEqual("Eclipse account " + row.MetaData["EclipseAccountAddress"], row.Name);
                Assert.AreEqual(ulong.MaxValue.ToString(), row.MetaData["EclipseSlot"]);
                Assert.AreEqual(ulong.MaxValue.ToString(), row.MetaData["EclipseBalanceUnits"]);
                Assert.AreEqual("ETH", row.MetaData["EclipseBalanceCurrency"]);
                Assert.AreEqual("9", row.MetaData["EclipseBalanceDecimals"]);
                Assert.AreEqual("false", row.MetaData["EclipseConsensusVerified"]);
                Assert.AreEqual(Addresses[0], row.MetaData["EclipseGenesisHash"]);
                Assert.AreEqual(row.MetaData["EclipseAccountAddress"], row.ProviderUniqueStorageKey[ProviderType.EclipseOASIS]);
                Assert.AreEqual(0, row.Children.Count());
            }
        }
        else
        {
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Result == null || result.Result.Count == 0);
        }
        if (scenario is "unknown" or "inactive" or "cancel") Assert.AreEqual(0, handler.Starts.Count);
        if (scenario == "empty-page-error") Assert.AreEqual(2, handler.Starts.Count);
    }

    [TestMethod]
    public async Task UnsupportedFiltersAndWritesDoNotReachNativeTransport()
    {
        using var handler = new EclipsePublicAccountReaderTests.Native();
        using var client = new HttpClient(handler);
        using var source = new EclipsePublicAccountMetadataSource(client, new Uri("https://example.invalid/"), Addresses, Addresses[0], Guid.NewGuid());
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.EclipseOASIS).Result).Where(h => h.Name == "11111111111111111111111111111111").Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(query, ProviderType.EclipseOASIS,
            new[] { new CosmicQueryTarget(ProviderType.EclipseOASIS, source.QueryCapabilities) }).IsError);
        Assert.IsTrue((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false)).IsError);
        Assert.IsTrue((await source.DeleteHolonAsync(Guid.NewGuid())).IsError);
        Assert.AreEqual(0, handler.Starts.Count);
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
            ["addresses"] = Addresses, ["genesisHash"] = Addresses[0], ["sourceOwner"] = Guid.NewGuid() };
        if (scenario == "unknown") fields["apiKey"] = "inert";
        if (scenario == "empty-owner") fields["sourceOwner"] = Guid.Empty;
        if (scenario == "uppercase") fields["addresses"] = new[] { "Nick.eth" };
        if (scenario == "duplicate-name") fields["addresses"] = new[] { "11111111111111111111111111111111", "11111111111111111111111111111111" };
        if (scenario == "credentials") fields["rpcUrl"] = "https://user:inert@example.invalid/";
        var json = JsonSerializer.Serialize(fields);
        if (scenario == "duplicate") json = json.Insert(1, "\"schema\":1,");
        try
        {
            using var source = EclipsePublicAccountMetadataSource.FromJson(json);
            Assert.Fail("Invalid source was accepted.");
        }
        catch (Exception error) when (error is ArgumentException or InvalidDataException) { }
    }
}
