using EOSNewYork.EOSCore;
using EOSNewYork.EOSCore.Utilities;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.EOSIOClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public class EOSIOOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        public const string OASIS_EOSIO_ACCOUNT = "oasis";
        public const string OASIS_PASS_PHRASE = "oasis";

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

        public override async Task<IProfile> LoadProfileAsync(string providerKey)
        {
            var chainApi = new ChainAPI();

            var rows = await chainApi.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", providerKey, providerKey, 1, 3);

            if (rows.rows.Count == 0)
            {
                return null;
            }

            var profileRow = (EOSIOAccountTableRow)rows.rows[0];
            var profile = profileRow.ToProfile();

            profile.Password = StringCipher.Decrypt(profile.Password, OASIS_PASS_PHRASE);

            return profile;
        }

        public override async Task<IProfile> LoadProfileAsync(Guid Id)
        {
            var chainApi = new ChainAPI();

            var rows = await chainApi.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", Id.ToString(), Id.ToString(), 1);

            if(rows.rows.Count == 0)
            {
                return null;
            }

            var profileRow = (EOSIOAccountTableRow)rows.rows[0];
            var profile = profileRow.ToProfile();

            profile.Password = StringCipher.Decrypt(profile.Password, OASIS_PASS_PHRASE);

            return profile;
        }

        public override async Task<IProfile> LoadProfileAsync(string username, string password)
        {
            var chainApi = new ChainAPI();

            var rows = await chainApi.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", username, username, 1, 2);

            if (rows.rows.Count == 0)
            {
                return null;
            }

            var profileRow = (EOSIOAccountTableRow)rows.rows[0];
            var profile = profileRow.ToProfile();

            profile.Password = StringCipher.Decrypt(profile.Password, OASIS_PASS_PHRASE);

            return profile;
        }

        public override async Task<IProfile> SaveProfileAsync(IProfile profile)
        {
            var chainApi = new ChainAPI();

            var rows = await chainApi.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "config", "true", null, null, 1);

            if (rows.rows.Count == 0)
            {
                return null;
            }

            var configRow = (EOSIOConfigTableRow)rows.rows[0];

            var actions = new List<EOSNewYork.EOSCore.Params.Action>();

            actions.Add(new ActionUtility(chainApi.GetHost().AbsoluteUri).GetActionObject("openacct", configRow.admin, "active", OASIS_EOSIO_ACCOUNT,
                    new EOSIOOpenAccountParams()
                    {
                        userid = profile.UserId.ToString(),
                        eosio_acc = profile.Username,
                        providerkey = profile.ProviderKey,
                        password = StringCipher.Encrypt(profile.Password, OASIS_PASS_PHRASE),
                        email = profile.Email,
                        title = profile.Title,
                        firstname = profile.FirstName,
                        lastname = profile.LastName,
                        dob = profile.DOB,
                        playeraddr = profile.PlayerAddress,
                        karma = profile.Karma
                    }
                ));

            List<string> privateKeysInWIF = new List<string> { "private key in WIF" };

            await chainApi.PushTransactionAsync(actions.ToArray(), privateKeysInWIF);

            return profile;
        }

        public override Task<ISearchResults> SearchAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }
    }
}
