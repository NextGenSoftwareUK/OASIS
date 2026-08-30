using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests
{
    [TestClass]
    public class DynamoDBOASISIntegrationTests
    {
        private DynamoDBOASIS _provider;
        private static readonly string AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "test-key";
        private static readonly string SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "test-secret";
        private static readonly string Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
        private static readonly string? ServiceUrl = Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL");

        [TestInitialize]
        public void Setup()
        {
            _provider = new DynamoDBOASIS(AccessKey, SecretKey, Region, ServiceUrl);
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "DynamoDBTestUser",
                Email = "dynamodbtest@example.com",
                FirstName = "DynamoDB",
                LastName = "Tester"
            };
            var result = await _provider.SaveAvatarAsync(avatar);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
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
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "DynamoDBTestHolon",
                Description = "Test Holon for DynamoDBOASIS integration"
            };
            var result = await _provider.SaveHolonAsync(holon);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
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
