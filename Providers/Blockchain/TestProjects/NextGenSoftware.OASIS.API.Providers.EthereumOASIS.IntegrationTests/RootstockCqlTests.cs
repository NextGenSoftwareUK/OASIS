using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public class RootstockCqlTests
{
    [TestMethod]
    public void ExistingConstructorAndDnaPreserveDefaultThirty()
    {
        Assert.IsNotNull(typeof(NextGenSoftware.OASIS.API.Providers.RootstockOASIS.RootstockOASIS)
            .GetConstructor(new[]{typeof(string),typeof(string),typeof(string)}));
        var provider = new NextGenSoftware.OASIS.API.Providers.RootstockOASIS.RootstockOASIS(
            "http://127.0.0.1:1", "0x"+new string('1',64), "0x"+new string('3',40));
        Assert.AreEqual(new System.Numerics.BigInteger(30), Field(provider,"_expectedChainId"));
        Assert.AreEqual(30L,new NextGenSoftware.OASIS.API.DNA.RootstockOASISProviderSettings().ChainId);
    }

    [DataTestMethod][DataRow(30L)][DataRow(31L)][DataRow(31338L)]
    public void ShippingFactoryCarriesConfiguredChainAndEndpointOverride(long chain)
    {
        var provider = NextGenSoftware.OASIS.OASISBootLoader.EvmProviderFactory.CreateRootstock(
            new NextGenSoftware.OASIS.API.DNA.RootstockOASISProviderSettings {
                ConnectionString="http://127.0.0.1:1", ChainId=chain,
                ChainPrivateKey="0x"+new string('1',64),ContractAddress="0x"+new string('3',40)},
            "http://127.0.0.1:2");
        Assert.AreEqual(new System.Numerics.BigInteger(chain),Field(provider,"_expectedChainId"));
        Assert.AreEqual("http://127.0.0.1:2",Field(provider,"_hostURI"));
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [DataTestMethod][DataRow(0L)][DataRow(-1L)]
    public void InvalidConfiguredIdentityRejectsBeforeNetworkAccess(long chain)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            NextGenSoftware.OASIS.OASISBootLoader.EvmProviderFactory.CreateRootstock(
                new NextGenSoftware.OASIS.API.DNA.RootstockOASISProviderSettings{ChainId=chain},null!));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            new NextGenSoftware.OASIS.API.Providers.RootstockOASIS.RootstockOASIS("http://127.0.0.1:1","","",chain));
    }

    private static object? Field(object provider,string name)
        => typeof(Web3CoreOASISBaseProvider).GetField(name,
            System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(provider);

    private static CosmicQuery<Holon> Query(Guid[] ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.RootstockOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(
                Guid.NewGuid(), ProviderType.RootstockOASIS, ids).Result).Result;

    [TestMethod]
    public void CapturedBusinessKeysCannotEscapeOrMutateAuthorityCatalogue()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var expected = ids[0];
        var selected = new[] { ids[0], ids[2] };
        var query = Query(ids.Take(2).ToArray()).Where(h => selected.Contains(h.Id)).Result;
        selected[0] = Guid.NewGuid();
        ids[0] = Guid.NewGuid();

        var result = Web3CoreCqlCompiler.Compile(query, ProviderType.RootstockOASIS);
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { expected }, result.Result.ToArray());
        Assert.IsTrue(Web3CoreCqlCompiler.Compile(query, ProviderType.EthereumOASIS).IsError);
        Assert.AreEqual(0, Web3CoreCqlCompiler.Compile(Query(Array.Empty<Guid>()), ProviderType.RootstockOASIS).Result.Count);
    }

    [DataTestMethod]
    [DataRow("name")]
    [DataRow("metadata")]
    [DataRow("order")]
    [DataRow("graph")]
    [DataRow("bound")]
    public void UnsupportedShapesRejectInsteadOfScanningNativeRecords(string shape)
    {
        var query = Query(Enumerable.Range(0, shape == "bound" ? 101 : 3).Select(_ => Guid.NewGuid()).ToArray());
        query = shape switch
        {
            "name" => query.Where(h => h.Name == "coffee").Result,
            "metadata" => query.Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result,
            "order" => query.OrderBy(h => h.Id).Result,
            "graph" => query.WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth: 1).Result).Result,
            _ => query
        };

        var result = Web3CoreCqlCompiler.Compile(query, ProviderType.RootstockOASIS);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task AdapterRejectsUnsupportedQueryBeforeActivationAndHonoursPreCancellation()
    {
        var provider = new NextGenSoftware.OASIS.API.Providers.RootstockOASIS.RootstockOASIS(
            "http://127.0.0.1:1", "0x" + new string('1', 64), "0x" + new string('3', 40));
        var query = Query(new[] { Guid.NewGuid() });
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.RootstockOASIS,
            new[] { new CosmicQueryTarget(ProviderType.RootstockOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);

        var inactive = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", inactive.ErrorCode);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var stopped = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancelled.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", stopped.ErrorCode);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
