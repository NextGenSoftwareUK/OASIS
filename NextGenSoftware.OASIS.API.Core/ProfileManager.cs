using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public class ProfileManager
    {
        public IOASISStorage OASISStorageProvider { get; set; }

        public ProfileManager(IOASISStorage OASISStorageProvider)
        {
            this.OASISStorageProvider = OASISStorageProvider;
        }

        public ProfileManager()
        {
            //this.OASISStorageProvider = 
        }

        public async Task<IProfile> LoadProfile(Guid id)
        {
            return await OASISStorageProvider.LoadProfileAsync(id);
        }

        public async Task<IProfile> LoadProfile(string username, string password)
        {
            return await OASISStorageProvider.LoadProfileAsync(username, password);
        }

        public async Task<bool> SaveProfile(IProfile profile)
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
