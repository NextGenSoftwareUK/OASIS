using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public class OASISAPIManager
    {
        public ProfileManager ProfileManager { get; set; }

        //public OASISAPIManager(IOASISStorage OASISStorageProvider, IOASISNET OASISNETProvider)
        public OASISAPIManager(IOASISStorage OASISStorageProvider)
        {
            this.ProfileManager = new ProfileManager(OASISStorageProvider);
        }

        public async Task<IProfile> LoadProfileAsync(string providerKey)
        {
            return await ProfileManager.LoadProfileAsync(providerKey);
        }

        public async Task<IProfile> LoadProfileAsync(Guid id)
        {
            return await ProfileManager.LoadProfileAsync(id);
        }

        public async Task<IProfile> LoadProfileAsync(string username, string password)
        {
            return await ProfileManager.LoadProfileAsync(username, password);
        }

        public async Task<IProfile> SaveProfileAsync(IProfile profile)
        {
            return await ProfileManager.SaveProfileAsync(profile);
        }

        public async Task<bool> AddKarmaToProfileAsync(IProfile profile, int karma)
        {
            return await ProfileManager.AddKarmaToProfileAsync(profile, karma);
        }

        public async Task<bool> RemoveKarmaToProfileAsync(IProfile profile, int karma)
        {
            return await ProfileManager.RemoveKarmaFromProfileAsync(profile, karma);
        }
    }
}
