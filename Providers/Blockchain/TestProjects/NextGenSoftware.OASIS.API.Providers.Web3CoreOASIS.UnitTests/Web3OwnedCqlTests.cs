using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public class Web3OwnedCqlTests
{
    private static readonly Guid One = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Two = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid Three = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private sealed class Provider : Web3CoreOASISBaseProvider
    {
        public readonly List<Guid> Reads = new();
        public Func<Guid, Task<OASISResult<IHolon>>> Read = id =>
            Task.FromResult(new OASISResult<IHolon>(new Holon { Id = id }));

        public Provider() : base("http://synthetic.invalid", "synthetic", "synthetic", 31337)
        {
            ProviderType = new NextGenSoftware.Utilities.EnumValue<ProviderType>(Core.Enums.ProviderType.EthereumOASIS);
            IsProviderActivated = true;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            Assert.IsFalse(loadChildren);
            Assert.IsFalse(recursive);
            Assert.IsFalse(loadChildrenFromProvider);
            Assert.AreEqual(0, version);
            Reads.Add(id);
            return Read(id);
        }
    }

    private static CosmicQuery<Holon> Query() => CosmicQuery<Holon>
        .Create(CosmicQueryScope.Named(ProviderType.EthereumOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),
            ProviderType.EthereumOASIS, new[] { One, Two, Three }).Result).Result;

    private static CosmicQueryPlan<Holon> Plan(Provider provider, CosmicQuery<Holon> query)
    {
        var result = CosmicQueryPlanner.Plan(query, provider.QueryProviderType,
            new[] { new CosmicQueryTarget(provider.QueryProviderType, provider.QueryCapabilities) });
        Assert.IsFalse(result.IsError, result.Message);
        return result.Result;
    }

    [TestMethod]
    public async Task CanonicalReadsApplyVisibilityBeforePaging()
    {
        var provider = new Provider();
        provider.Read = id => Task.FromResult(id == One
            ? new OASISResult<IHolon> { IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound }
            : new OASISResult<IHolon>(new Holon { Id = id }));
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query().Skip(1).Result.Take(1).Result));
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { Three }, result.Result.Select(x => x.Id).ToArray());
        CollectionAssert.AreEqual(new[] { One, Two, Three }, provider.Reads);
    }

    [DataTestMethod]
    [DataRow("identity")]
    [DataRow("nativeError")]
    [DataRow("timeout")]
    [DataRow("bytes")]
    [DataRow("count")]
    public async Task FailedCompleteQueryPublishesNoPartialRows(string mode)
    {
        var provider = new Provider();
        provider.Read = id =>
        {
            if (id == Two && mode == "timeout") throw new OperationCanceledException("SDK deadline");
            if (id == Two && mode == "nativeError") return Task.FromResult(new OASISResult<IHolon>
                { IsError = true, ErrorCode = "NATIVE_DENIED" });
            return Task.FromResult(new OASISResult<IHolon>(new Holon
            {
                Id = id == Two && mode == "identity" ? Guid.NewGuid() : id,
                Description = id == Two && mode == "bytes" ? new string('x', 4 * 1024 * 1024) : "small"
            }));
        };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query()), maximumResults: mode == "count" ? 1 : 100);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.IsFalse(result.IsLoaded);
        Assert.AreEqual(0, result.LoadedCount);
        if (mode == "timeout") Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
        if (mode == "nativeError") Assert.AreEqual("NATIVE_DENIED", result.ErrorCode);
    }

    [TestMethod]
    public async Task AdmissionAndCancellationPrecedeNativeDispatch()
    {
        var provider = new Provider();
        var unsupported = Query().Where(x => x.Name == "coffee").Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(unsupported, provider.QueryProviderType,
            new[] { new CosmicQueryTarget(provider.QueryProviderType, provider.QueryCapabilities) }).IsError);
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query()), cancellationToken: cancel.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.AreEqual(0, provider.Reads.Count);
        provider.IsProviderActivated = false;
        result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query()));
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", result.ErrorCode);
        Assert.AreEqual(0, provider.Reads.Count);
    }

    [TestMethod]
    public async Task CancellationDuringNativeReadPublishesNothingAndDispatchesNoNextKey()
    {
        var provider = new Provider();
        using var cancel = new CancellationTokenSource();
        provider.Read = id => { cancel.Cancel(); return Task.FromResult(new OASISResult<IHolon>(new Holon { Id = id })); };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query()), cancellationToken: cancel.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.IsNull(result.Result);
        Assert.AreEqual(1, provider.Reads.Count);
    }

    [TestMethod]
    public void ShippingV2ArbitrumFactoryExposesSharedOwnedQueryContract()
    {
        var provider = NextGenSoftware.OASIS.OASISBootLoader.EvmProviderFactory.CreateArbitrum(
            new NextGenSoftware.OASIS.API.DNA.ArbitrumOASISProviderSettings
            {
                ConnectionString = "http://synthetic.invalid", ChainPrivateKey = "synthetic",
                ContractAddress = "synthetic", ChainId = 31337
            }, NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2, null!);
        Assert.IsInstanceOfType(provider, typeof(NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.ArbitrumOASIS_Web3Core));
        Assert.IsInstanceOfType(provider, typeof(ICosmicQueryStorageProvider));
        Assert.AreEqual(ProviderType.ArbitrumOASIS, ((ICosmicQueryStorageProvider)provider).QueryProviderType);
    }

    [TestMethod]
    public async Task DeletedRowsDoNotConsumePagePositions()
    {
        var provider = new Provider();
        provider.Read = id => Task.FromResult(new OASISResult<IHolon>(new Holon
            { Id = id, DeletedDate = id == One ? DateTime.UtcNow : DateTime.MinValue }));
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query().Take(1).Result));
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { Two }, result.Result.Select(x => x.Id).ToArray());
    }
}
