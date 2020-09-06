using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS
{
    public class IPFSOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        public IPFSOASIS()
        {
            this.ProviderName = "IPFSOASIS";
            this.ProviderDescription = "IPFS Provider";
            this.ProviderType = ProviderType.IPFSOASIS;
            this.ProviderCategory = ProviderCategory.StorageAndNetwork;
        }

        public List<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public List<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<ISearchResults> SearchAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }
    }
}
