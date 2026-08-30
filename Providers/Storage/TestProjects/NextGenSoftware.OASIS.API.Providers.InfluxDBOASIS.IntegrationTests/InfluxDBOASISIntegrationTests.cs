using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.IntegrationTests
{
    [TestClass]
    public class InfluxDBOASISIntegrationTests
    {
        private InfluxDBOASIS _provider;
        private static readonly string Url = Environment.GetEnvironmentVariable("INFLUXDB_URL") ?? "http://localhost:8086";
        private static readonly string Token = Environment.GetEnvironmentVariable("INFLUXDB_TOKEN") ?? "test-token";
        private static readonly string Org = Environment.GetEnvironmentVariable("INFLUXDB_ORG") ?? "oasis-org";
        private static readonly string Bucket = Environment.GetEnvironmentVariable("INFLUXDB_BUCKET") ?? "oasis-bucket";

        [TestInitialize]
        public void Setup()
        {
            _provider = new InfluxDBOASIS(Url, Token, Org, Bucket);
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "InfluxDBTestUser",
                Email = "influxdbtest@example.com",
                FirstName = "InfluxDB",
                LastName = "Tester"
            };
            var result = await _provider.SaveAvatarAsync(avatar);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            var avatarId = Guid.NewGuid();
            var result = await _provider.LoadAvatarAsync(avatarId);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "InfluxDBTestHolon",
                Description = "Test Holon for InfluxDBOASIS integration"
            };
            var result = await _provider.SaveHolonAsync(holon);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnHolon()
        {
            var holonId = Guid.NewGuid();
            var result = await _provider.LoadHolonAsync(holonId);
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
