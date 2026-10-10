using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.CloudinaryOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CloudinaryOASIS.IntegrationTests
{
    [TestClass]
    public class CloudinaryOASISIntegrationTests
    {
        private CloudinaryOASIS _provider = null!;
        private static readonly string CloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME") ?? "test-cloud";
        private static readonly string ApiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY") ?? "test-key";
        private static readonly string ApiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET") ?? "test-secret";

        [TestInitialize]
        public void Setup()
        {
            _provider = new CloudinaryOASIS(CloudName, ApiKey, ApiSecret);
            _provider.ActivateProvider();
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "cloudinary-test-user", Email = "cloudinary@test.com" };
            var result = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            var result = await _provider.LoadAvatarAsync(Guid.NewGuid());
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = "Cloudinary Test Holon" };
            var result = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnHolon()
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
