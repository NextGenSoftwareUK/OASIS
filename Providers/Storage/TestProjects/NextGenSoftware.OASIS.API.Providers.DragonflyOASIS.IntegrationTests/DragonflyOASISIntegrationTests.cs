using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.DragonflyOASIS;

namespace NextGenSoftware.OASIS.API.Providers.DragonflyOASIS.IntegrationTests
{
    [TestClass]
    public class DragonflyOASISIntegrationTests
    {
        private DragonflyOASIS _provider = null!;
        private static readonly string connStr = Environment.GetEnvironmentVariable("DRAGONFLY_CONN") ?? "localhost:6379";

        [TestInitialize]
        public void Setup()
        {
            _provider = new DragonflyOASIS(connStr);
            _provider.ActivateProvider();
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "dragonfly-test-user", Email = "dragonfly@test.com" };
            var result = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnResult()
            => Assert.IsNotNull(await _provider.LoadAvatarAsync(Guid.NewGuid()));

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = "Dragonfly Test Holon" };
            var result = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnResult()
            => Assert.IsNotNull(await _provider.LoadHolonAsync(Guid.NewGuid()));

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
