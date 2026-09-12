using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.PineconeOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PineconeOASIS.IntegrationTests
{
    [TestClass]
    public class PineconeOASISIntegrationTests
    {
        private PineconeOASIS _provider = null!;
        private static readonly string connStr = Environment.GetEnvironmentVariable("PINECONE_URL") ?? "https://oasis-storage-abc123.svc.us-east-1-aws.pinecone.io";

        [TestInitialize]
        public void Setup()
        {
            _provider = new PineconeOASIS(connStr, Environment.GetEnvironmentVariable("PINECONE_API_KEY") ?? "APIKEY");
            _provider.ActivateProvider();
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "pinecone-test-user", Email = "pinecone@test.com" };
            var result = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnResult()
        {
            var result = await _provider.LoadAvatarAsync(Guid.NewGuid());
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = "Pinecone Test Holon" };
            var result = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnResult()
        {
            var result = await _provider.LoadHolonAsync(Guid.NewGuid());
            Assert.IsNotNull(result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
