using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests
{
    [TestClass]
    [DoNotParallelize]
    public partial class AzureCosmosDBOASISIntegrationTests
    {
        private AzureCosmosDBOASIS _provider = null!;
        private CosmosClient _client=null!;
        private bool _ownsDatabase;
        private const string DatabaseName="oasis-cosmos-crud";

        [TestInitialize]
        public async Task Setup()
        {
            Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_COSMOSDB_NATIVE"),"Use the retained owned Cosmos DB runner.");
            const string endpoint="https://127.0.0.1:8081";
            // Public Microsoft emulator key; cannot authenticate to a hosted account.
            const string key="C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            _client=new CosmosClient(endpoint,key,new CosmosClientOptions {ConnectionMode=ConnectionMode.Gateway,
                RequestTimeout=TimeSpan.FromSeconds(10),MaxRetryAttemptsOnRateLimitedRequests=0,
                HttpClientFactory=()=>new System.Net.Http.HttpClient(new System.Net.Http.HttpClientHandler {
                    ServerCertificateCustomValidationCallback=System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator})});
            try {await _client.GetDatabase(DatabaseName).ReadAsync();await VerifyEmptyFixtureAsync();}
            catch(CosmosException cause) when(cause.StatusCode==HttpStatusCode.NotFound) { }
            _ownsDatabase=true;

            _provider = new AzureCosmosDBOASIS(
                new Uri(endpoint),
                key,
                DatabaseName,
                new List<string> { "avatarItems", "avatarDetailItems", "holonItems" });

            var activation = await _provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);
            Assert.IsTrue(activation.Result, activation.Message);
        }

        [TestMethod]
        public async Task AvatarCrud_PersistsThroughOfficialCosmosSdk()
        {
            var avatar = new Avatar
            {
                Username = $"cosmos-{Guid.NewGuid():N}",
                Email = $"cosmos-{Guid.NewGuid():N}@example.test",
                FirstName = "Azure",
                LastName = "Cosmos"
            };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.IsNotNull(saved.Result);

            var loaded = await _provider.LoadAvatarAsync(saved.Result.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(avatar.Username, loaded.Result.Username);

            loaded.Result.FirstName = "Updated";
            var updated = await _provider.SaveAvatarAsync(loaded.Result);
            Assert.IsFalse(updated.IsError, updated.Message);
            Assert.AreEqual(saved.Result.Id, updated.Result.Id);

            var byEmail = await _provider.LoadAvatarByEmailAsync(avatar.Email);
            Assert.IsFalse(byEmail.IsError, byEmail.Message);
            Assert.AreEqual("Updated", byEmail.Result.FirstName);

            var byUsername = await _provider.LoadAvatarByUsernameAsync(avatar.Username);
            Assert.IsFalse(byUsername.IsError, byUsername.Message);
            Assert.AreEqual(saved.Result.Id, byUsername.Result.Id);

            var deleted = await _provider.DeleteAvatarAsync(saved.Result.Id, false);
            Assert.IsFalse(deleted.IsError, deleted.Message);
            Assert.IsTrue(deleted.Result, deleted.Message);
        }

        [TestMethod]
        public async Task HolonCrud_PersistsThroughOfficialCosmosSdk()
        {
            var holon = new Holon
            {
                Name = $"cosmos-holon-{Guid.NewGuid():N}",
                Description = "Official Cosmos SDK integration verification"
            };

            var saved = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.IsNotNull(saved.Result);

            var loaded = await _provider.LoadHolonAsync(saved.Result.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(holon.Name, loaded.Result.Name);

            var deleted = await _provider.DeleteHolonAsync(saved.Result.Id);
            Assert.IsFalse(deleted.IsError, deleted.Message);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            try {_provider?.DeActivateProvider();}
            finally
            {
                try
                {
                    if(_ownsDatabase)await VerifyEmptyFixtureAsync();
                }
                finally {_client?.Dispose();}
            }
        }

        private async Task VerifyEmptyFixtureAsync()
        {
            using var containers=_client.GetDatabase(DatabaseName).GetContainerQueryIterator<ContainerProperties>();
            var expected=new HashSet<string>{"avatarItems","avatarDetailItems","holonItems"};
            while(containers.HasMoreResults)
                foreach(var container in await containers.ReadNextAsync())
                {
                    Assert.IsTrue(expected.Contains(container.Id),"Unexpected retained fixture container; preserve for inspection.");
                    Assert.AreEqual("/id",container.PartitionKeyPath);
                    using var query=_client.GetContainer(DatabaseName,container.Id).GetItemQueryIterator<long>("SELECT VALUE COUNT(1) FROM c");
                    long count=0;while(query.HasMoreResults)count+=(await query.ReadNextAsync()).Single();
                    Assert.AreEqual(0L,count,"Retained fixture records require inspection; no reset or broad cleanup allowed.");
                }
        }
    }
}
