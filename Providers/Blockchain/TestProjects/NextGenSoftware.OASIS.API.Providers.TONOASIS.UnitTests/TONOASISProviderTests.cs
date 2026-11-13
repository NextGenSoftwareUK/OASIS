using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.TONOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests
{
    /// <summary>
    /// Provider metadata checks. These run with no backend - they assert the provider
    /// identifies itself correctly and starts deactivated.
    /// </summary>
    [TestClass]
    public class TONOASISProviderTests
    {
        private TONOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = TONOASISTestFactory.Create();

        [TestMethod]
        public void ProviderType_ShouldBeSet()
            => Assert.AreNotEqual(ProviderType.None, _provider.ProviderType.Value);

        [TestMethod]
        public void ProviderName_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderName));

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderDescription));

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderCategory_ShouldBeSet()
            => Assert.IsNotNull(_provider.ProviderCategory);

        [TestMethod]
        public void ExistingEvmAdapterCannotAdvertiseNativeTonQueries()
        {
            // A TON label cannot grant access to the Polygon/Arbitrum EVM path.
            // No activation or RPC is permitted for this unsupported authority.
            var provider = new NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.Web3CoreOASISBaseProvider(
                "http://127.0.0.1:1", new string('0', 63) + "1", "0x0000000000000000000000000000000000000001")
                { ProviderType = new(ProviderType.TONOASIS) };
            var id = Guid.NewGuid();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TONOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.TONOASIS, new[] { id }).Result).Result
                .Where(row => row.Id == id).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.TONOASIS,
                new[] { new CosmicQueryTarget(ProviderType.TONOASIS, provider.QueryCapabilities) });
            Assert.IsTrue(plan.IsError, "Unsupported TON must be rejected during planning.");

            var result = NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.Web3CoreCqlCompiler.Compile(query, ProviderType.TONOASIS);

            Assert.IsTrue(result.IsError);
            Assert.AreEqual("CQL_ACCESS_INVALID", result.ErrorCode);
            Assert.IsFalse(provider.IsProviderActivated);

        }

        [DataTestMethod]
        [DataRow(ProviderType.PolygonOASIS)]
        [DataRow(ProviderType.ArbitrumOASIS)]
        public void SupportedEvmAuthoritiesRetainTheirActualQueryCapabilities(ProviderType authority)
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.Web3CoreOASISBaseProvider(
                "http://127.0.0.1:1", new string('0', 63) + "1", "0x0000000000000000000000000000000000000001")
                { ProviderType = new(authority) };
            var id = Guid.NewGuid();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, new[] { id }).Result).Result
                .Where(row => row.Id == id).Result;
            var plan = CosmicQueryPlanner.Plan(query, authority, new[] { new CosmicQueryTarget(authority, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var compiled = NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.Web3CoreCqlCompiler.Compile(query, authority);
            Assert.IsFalse(compiled.IsError, compiled.Message);
            CollectionAssert.AreEqual(new[] { id }, compiled.Result.ToArray());
        }

        [TestMethod]
        public void NativeProviderCannotInheritOrSelectEvmStorage()
        {
            Assert.IsFalse(typeof(NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.Web3CoreOASISBaseProvider).IsAssignableFrom(typeof(TONOASIS)));
#pragma warning disable CS0618
            Assert.ThrowsException<NotSupportedException>(() => new TONOASIS("http://127.0.0.1:1", "public test fixture", "0x01"));
#pragma warning restore CS0618
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
            _provider?.Dispose();
        }
    }
}
