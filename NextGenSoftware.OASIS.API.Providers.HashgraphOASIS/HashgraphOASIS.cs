using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public class HashgraphOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        public HashgraphOASIS()
        {
            this.ProviderName = "HashgraphOASIS";
            this.ProviderDescription = "Hashgraph Provider";
            this.ProviderType = ProviderType.HashgraphOASIS;
            this.ProviderCategory = ProviderCategory.StorageAndNetwork;
        }

        public override bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public List<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public List<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IAvatar> LoadAllAvatars()
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username)
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

        public override IHolon LoadHolon(Guid id)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override List<IHolon> LoadHolons(Guid id)
        {
            throw new NotImplementedException();
        }

        public override List<IHolon> LoadHolons(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<List<IHolon>> LoadHolonsAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<List<IHolon>> LoadHolonsAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override IAvatar SaveAvatar(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override IHolon SaveHolon(IHolon holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> SaveHolonAsync(IHolon holon)
        {
            throw new NotImplementedException();
        }

        public override List<IHolon> SaveHolons(List<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<List<IHolon>> SaveHolonsAsync(List<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<ISearchResults> SearchAsync(ISearchParams searchParams)
        {
            throw new NotImplementedException();
        }
    }
}
