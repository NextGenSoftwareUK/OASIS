using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;
using Azure.Storage.Blobs;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests
{
    /// <summary>
    /// Exercises the real CRUD surface against a live backend.
    ///
    /// Uses the retained owned Azurite runner. Missing setup is a failure, never a skipped success.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public partial class AzureStorageOASISIntegrationTests
    {
        private AzureStorageOASIS _provider = null!;
        private bool _live;
        private BlobServiceClient _service=null!;
        private bool _ownsContainers;
        private readonly string[] _containers={"oasis-crud-holons","oasis-crud-avatars","oasis-crud-details"};

        [TestInitialize]
        public async Task Setup()
        {
            Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_AZURE_NATIVE_TEST"),"Use the owned reusable Azure runner.");
            _service=new BlobServiceClient("UseDevelopmentStorage=true");
            foreach(var name in _containers)
                Assert.IsFalse((await _service.GetBlobContainerClient(name).ExistsAsync()).Value,"Refusing pre-existing fixture container.");
            _ownsContainers=true;
            _provider=new AzureStorageOASIS("UseDevelopmentStorage=true",_containers[0],_containers[1],_containers[2]);
            var activated=await _provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError,activated.Message);_live=_provider.IsProviderActivated;
        }

        private void RequireLive()
        {
            if (!_live)
                Assert.Fail("Owned Azure provider did not activate.");
        }

        [TestMethod]
        public async Task SaveAndLoadAvatar_RoundTrips()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsNotNull(loaded.Result);
            Assert.AreEqual(avatar.Username, loaded.Result!.Username);

            await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
        }

        [TestMethod]
        public async Task LoadAvatarByUsername_FindsTheSavedAvatar()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);

            var found = await _provider.LoadAvatarByUsernameAsync(avatar.Username);
            Assert.IsFalse(found.IsError, found.Message);
            Assert.AreEqual(avatar.Id, found.Result!.Id);

            await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
        }

        [TestMethod]
        public async Task SaveAndLoadHolon_RoundTrips()
        {
            RequireLive();

            var holon = new Holon { Id = Guid.NewGuid(), Name = $"OASIS IT Holon {Guid.NewGuid():N}" };

            var saved = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadHolonAsync(holon.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(holon.Name, loaded.Result!.Name);

            await _provider.DeleteHolonAsync(holon.Id);
        }

        [TestMethod]
        public async Task DeletedAvatar_IsNotReturnedAsLive()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            await _provider.SaveAvatarAsync(avatar);
            var deleted = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
            Assert.IsFalse(deleted.IsError, deleted.Message);

            var reloaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsTrue(reloaded.IsError || reloaded.Result == null || reloaded.Result.IsDeleted,
                "A soft-deleted avatar should not come back as a live record.");
        }

        [TestMethod]
        public async Task LoadAllAvatars_ReturnsAResult()
        {
            RequireLive();
            var all = await _provider.LoadAllAvatarsAsync();
            Assert.IsNotNull(all);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            try {if (_provider != null) await _provider.DeActivateProviderAsync();}
            finally
            {
                if(_ownsContainers)
                    foreach(var name in _containers) await _service.GetBlobContainerClient(name).DeleteIfExistsAsync();
            }
        }
    }
}
