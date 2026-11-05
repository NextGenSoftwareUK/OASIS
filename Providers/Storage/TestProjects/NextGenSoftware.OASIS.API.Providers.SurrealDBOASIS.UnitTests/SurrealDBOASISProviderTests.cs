using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.UnitTests
{
    /// <summary>
    /// Provider metadata checks. These run with no backend - they assert the provider
    /// identifies itself correctly and starts deactivated.
    /// </summary>
    [TestClass]
    public class SurrealDBOASISProviderTests
    {
        private SurrealDBOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = SurrealDBOASISTestFactory.Create();

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
        public async Task BoundedCqlRequiresActivationAndEnforcesResultBudget()
        {
            const ProviderType provider = ProviderType.SurrealDBOASIS;
            var id = Guid.NewGuid();
            var actor = Guid.NewGuid();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, provider, new[] { id }).Result).Result
                .Where(row => row.Id == id).Result;
            var planned = CosmicQueryPlanner.Plan(query, provider,
                new[] { new CosmicQueryTarget(provider, _provider.QueryCapabilities) });
            Assert.IsFalse(planned.IsError, planned.Message);

            var inactive = await _provider.ExecuteCosmicQueryAsync(planned.Result);
            Assert.AreEqual("CQL_PROVIDER_INACTIVE", inactive.ErrorCode);
            Assert.IsNull(inactive.Result);

            var overBudget = await _provider.ExecuteCosmicQueryAsync(planned.Result, maximumResults: 10001);
            Assert.AreEqual("CQL_QUERY_LIMIT", overBudget.ErrorCode);
            Assert.IsNull(overBudget.Result);
        }

        [TestMethod]
        public async Task QueryActivationRejectsMissingConfigurationAndCancellation()
        {
            var missing = await _provider.ActivateQuerySourceAsync(default);
            Assert.AreEqual("CQL_QUERY_SOURCE_UNAVAILABLE", missing.ErrorCode);
            Assert.IsFalse(_provider.IsProviderActivated);

            var querySource = new SurrealDBOASIS("http://127.0.0.1:1", "reader", "unused", "demo", "demo", Guid.NewGuid());
            var provisioning = await querySource.ActivateProviderAsync();
            Assert.AreEqual("CQL_QUERY_ONLY_SOURCE", provisioning.ErrorCode);
            Assert.IsFalse(querySource.IsProviderActivated);

            var cancelled = await querySource.ActivateQuerySourceAsync(new System.Threading.CancellationToken(canceled: true));
            Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
            Assert.IsFalse(querySource.IsProviderActivated);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
