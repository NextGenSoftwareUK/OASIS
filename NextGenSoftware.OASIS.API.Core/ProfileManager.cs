using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public class ProfileManager
    {
        public IOASISStorage OASISStorageProvider { get; set; }

        //Events
        public delegate void ProfileManagerError(object sender, ProfileManagerErrorEventArgs e);
        public event ProfileManagerError OnProfileManagerError;

        public delegate void StorageProviderError(object sender, ProfileManagerErrorEventArgs e);

        public ProfileManager(IOASISStorage OASISStorageProvider)
        {
            this.OASISStorageProvider = OASISStorageProvider;
            this.OASISStorageProvider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;
        }

        private void OASISStorageProvider_OnStorageProviderError(object sender, ProfileManagerErrorEventArgs e)
        {
            //TODO: Not sure if we need to have a OnProfileManagerError as well as the StorageProvider Error Event?
            OnProfileManagerError?.Invoke(this, e);
        }

        public async Task<IProfile> LoadProfileAsync(string providerKey)
        {
            return await OASISStorageProvider.LoadProfileAsync(providerKey);
        }

        public async Task<IProfile> LoadProfileAsync(Guid id)
        {
            return await OASISStorageProvider.LoadProfileAsync(id);
        }

        public async Task<IProfile> LoadProfileAsync(string username, string password)
        {
            return await OASISStorageProvider.LoadProfileAsync(username, password);
        }

        public async Task<IProfile> SaveProfileAsync(IProfile profile)
        {
            return await OASISStorageProvider.SaveProfileAsync(profile);
        }

        public async Task<bool> AddKarmaToProfileAsync(IProfile profile, int karma)
        {
            return await OASISStorageProvider.AddKarmaToProfileAsync(profile, karma);
        }

        public async Task<bool> RemoveKarmaToProfileAsync(IProfile profile, int karma)
        {
            return await OASISStorageProvider.RemoveKarmaFromProfileAsync(profile, karma);
        }
    }
}
