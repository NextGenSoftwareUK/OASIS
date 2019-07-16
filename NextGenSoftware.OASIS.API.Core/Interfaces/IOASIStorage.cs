using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static NextGenSoftware.OASIS.API.Core.ProfileManager;

namespace NextGenSoftware.OASIS.API.Core
{
    // This interface is responsbile for persisting data/state to storage, this could be a local DB or other local 
    // storage or through a distributed/decentralised provider such as IPFS or Holochain (these two implementations 
    // will be implemented soon (IPFSOASIS & HoloOASIS).
    public interface IOASISStorage
    {
        Task<IProfile> LoadProfileAsync(string providerKey);
        Task<IProfile> LoadProfileAsync(Guid Id);
        Task<IProfile> LoadProfileAsync(string username, string password);

        //Task<bool> SaveProfileAsync(IProfile profile);
        Task<IProfile> SaveProfileAsync(IProfile profile);
        Task<bool> AddKarmaToProfileAsync(IProfile profile, int karma);
        Task<bool> RemoveKarmaFromProfileAsync(IProfile profile, int karma);

        event StorageProviderError OnStorageProviderError;

        //TODO: Lots more to come! ;-)
    }
}
