using EOSNewYork.EOSCore;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public class EOSIOOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        public const string OASIS_EOSIO_ACCOUNT = "oasis";

        public EOSIOOASIS()
        {
            this.ProviderName = "EOSIOOASIS";
            this.ProviderDescription = "EOSIO Provider";
            this.ProviderType = ProviderType.EOSOASIS;
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

        public override Task<IProfile> LoadProfileAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override async Task<IProfile> LoadProfileAsync(Guid Id)
        {
            var chainAPI = new ChainAPI();

            var rows = await chainAPI.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", Id.ToString(), Id.ToString(), 1);

            if(rows.rows.Count == 0)
            {
                return null;
            }

            var profileRow = (OasisAccountTableRow)rows.rows[0];
            return profileRow.ToProfile();
        }

        public override async Task<IProfile> LoadProfileAsync(string username, string password)
        {
            var chainAPI = new ChainAPI();

            var rows = await chainAPI.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", username, username, 1, 2);

            if (rows.rows.Count == 0)
            {
                return null;
            }

            var profileRow = (OasisAccountTableRow)rows.rows[0];
            return profileRow.ToProfile();
        }

        public override Task<IProfile> SaveProfileAsync(IProfile profile)
        {
            throw new NotImplementedException();
        }

        public override Task<ISearchResults> SearchAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }
    }
}
