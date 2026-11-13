using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public class AvalancheCqlTests
{
    [TestMethod]
    public void DefaultConstructorSuppliesTheConfiguredCChainIdentityToEncryption()
    {
        Assert.IsNotNull(typeof(NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.AvalancheOASIS)
            .GetConstructor(new[]{typeof(string),typeof(string),typeof(string)}),
            "Keep the existing constructor signature for compiled clients.");
        var provider = new NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.AvalancheOASIS(
            "http://127.0.0.1:1","0x"+new string('1',64),"0x"+new string('3',40));
        var context = typeof(Web3CoreOASISBaseProvider).GetMethod("PrivateContext",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(provider,new object[]{"holon",System.Numerics.BigInteger.One,new byte[32]}) as string;
        StringAssert.Contains(context,"/AvalancheOASIS/43114/");
    }

    [DataTestMethod]
    [DataRow("0xa86a", "43114")]
    [DataRow("43114", "43114")]
    [DataRow("31338", "31338")]
    public void ShippingFactoryPreservesExplicitPositiveChainIdentity(string configured,string expected)
    {
        var provider = NextGenSoftware.OASIS.OASISBootLoader.EvmProviderFactory.CreateAvalanche(
            new NextGenSoftware.OASIS.API.DNA.AvalancheOASISProviderSettings
            {RpcEndpoint="http://127.0.0.1:1",ChainPrivateKey="0x"+new string('1',64),
                ContractAddress="0x"+new string('3',40),ChainId=configured},null!);
        Assert.IsInstanceOfType(provider,typeof(NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.AvalancheOASIS));
        var context = typeof(Web3CoreOASISBaseProvider).GetMethod("PrivateContext",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(provider,new object[]{"holon",System.Numerics.BigInteger.One,new byte[32]}) as string;
        StringAssert.Contains(context,"/AvalancheOASIS/"+expected+"/");
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("0")]
    [DataRow("-1")]
    [DataRow("0x")]
    [DataRow("not-a-chain")]
    public void InvalidConfiguredChainRejectsBeforeNativeAccess(string chain)
        => Assert.ThrowsException<ArgumentException>(() =>
            NextGenSoftware.OASIS.OASISBootLoader.EvmProviderFactory.CreateAvalanche(
                new NextGenSoftware.OASIS.API.DNA.AvalancheOASISProviderSettings {ChainId=chain},null!));

    private static CosmicQuery<Holon> Query(Guid[] ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AvalancheOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(
                Guid.NewGuid(), ProviderType.AvalancheOASIS, ids).Result).Result;

    [TestMethod]
    public void CapturedBusinessKeysCannotEscapeOrMutateAuthorityCatalogue()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var expected = ids[0];
        var selected = new[] { ids[0], ids[2] };
        var query = Query(ids.Take(2).ToArray()).Where(h => selected.Contains(h.Id)).Result;
        selected[0] = Guid.NewGuid();
        ids[0] = Guid.NewGuid();

        var result = Web3CoreCqlCompiler.Compile(query, ProviderType.AvalancheOASIS);
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { expected }, result.Result.ToArray());
        Assert.IsTrue(Web3CoreCqlCompiler.Compile(query, ProviderType.EthereumOASIS).IsError);
        Assert.AreEqual(0, Web3CoreCqlCompiler.Compile(Query(Array.Empty<Guid>()), ProviderType.AvalancheOASIS).Result.Count);
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

        var result = Web3CoreCqlCompiler.Compile(query, ProviderType.AvalancheOASIS);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task AdapterRejectsUnsupportedQueryBeforeActivationAndHonoursPreCancellation()
    {
        var provider = new NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.AvalancheOASIS(
            "http://127.0.0.1:1", "0x" + new string('1', 64), "0x" + new string('3', 40));
        var query = Query(new[] { Guid.NewGuid() });
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.AvalancheOASIS,
            new[] { new CosmicQueryTarget(ProviderType.AvalancheOASIS, provider.QueryCapabilities) });
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
