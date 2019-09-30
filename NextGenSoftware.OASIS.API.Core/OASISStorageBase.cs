using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public abstract class OASISStorageBase : OASISProvider, IOASISStorage
    {

        public Task<KarmaAkashicRecord> AddKarmaToProfileAsync(API.Core.IProfile profile, KarmaType karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return profile.KarmaEarnt(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        public Task<KarmaAkashicRecord> SubtractKarmaFromProfileAsync(API.Core.IProfile profile, KarmaType karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return profile.KarmaLost(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        public Task<IProfile> LoadProfileAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public Task<IProfile> LoadProfileAsync(Guid Id)
        {
            throw new NotImplementedException();
        }

        public Task<IProfile> LoadProfileAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveKarmaFromProfileAsync(API.Core.IProfile profile, KarmaType karmaType)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveKarmaFromProfileAsync(IProfile profile, int karma)
        {
            throw new NotImplementedException();
        }

        public Task<IProfile> SaveProfileAsync(IProfile profile)
        {
            throw new NotImplementedException();
        }
    }
}
