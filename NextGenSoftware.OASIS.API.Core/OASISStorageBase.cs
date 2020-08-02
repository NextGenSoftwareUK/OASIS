using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public abstract class OASISStorageBase : OASISProvider, IOASISStorage
    {
        public event ProfileManager.StorageProviderError StorageProviderError;

        public Task<KarmaAkashicRecord> AddKarmaToProfileAsync(API.Core.IProfile profile, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return profile.KarmaEarnt(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        public Task<KarmaAkashicRecord> SubtractKarmaFromProfileAsync(API.Core.IProfile profile, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return profile.KarmaLost(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        protected void OnStorageProviderError(string endPoint, string reason, Exception errorDetails)
        {
            StorageProviderError?.Invoke(this, new ProfileManagerErrorEventArgs { EndPoint = endPoint, Reason = reason, ErrorDetails = errorDetails });
        }

        public abstract Task<IProfile> LoadProfileAsync(string providerKey);

        public abstract Task<IProfile> LoadProfileAsync(Guid Id);

        public abstract Task<IProfile> LoadProfileAsync(string username, string password);

        public abstract Task<IProfile> SaveProfileAsync(IProfile profile);

        public abstract Task<ISearchResults> SearchAsync(string searchTerm);


    }
}
