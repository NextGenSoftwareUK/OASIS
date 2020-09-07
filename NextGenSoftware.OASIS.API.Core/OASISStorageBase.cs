using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public abstract class OASISStorageBase : OASISProvider, IOASISStorage
    {
        public event AvatarManager.StorageProviderError StorageProviderError;

        //TODO: COme back to this...
        //public List<Avatar> LoadAvatarsWithoutPasswords(IEnumerable<Avatar> avatars)
        //{
        //    return avatars.Select(x => x.WithoutPassword());
        //}

        //public Avatar WithoutPassword(this Avatar user)
        //{
        //    user.Password = null;
        //    return user;
        //}

        public Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(API.Core.IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return Avatar.KarmaEarnt(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        public Task<KarmaAkashicRecord> SubtractKarmaFromAvatarAsync(API.Core.IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return Avatar.KarmaLost(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        protected void OnStorageProviderError(string endPoint, string reason, Exception errorDetails)
        {
            StorageProviderError?.Invoke(this, new AvatarManagerErrorEventArgs { EndPoint = endPoint, Reason = reason, ErrorDetails = errorDetails });
        }

        public abstract Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync();
        public abstract Task<IAvatar> LoadAvatarAsync(string providerKey);

        public abstract Task<IAvatar> LoadAvatarAsync(Guid Id);
        public abstract IAvatar LoadAvatar(string username, string password);

        public abstract Task<IAvatar> LoadAvatarAsync(string username, string password);

        public abstract Task<IAvatar> SaveAvatarAsync(IAvatar Avatar);

        public abstract Task<ISearchResults> SearchAsync(string searchTerm);


    }
}
