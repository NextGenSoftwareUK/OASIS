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

        public override Task<IProfile> LoadProfileAsync(string providerKey)
        {
            throw new NotImplementedException();
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
                        eosio_acc = profile.Username
                    }
                ));

            actions.Add(new ActionUtility(chainApi.GetHost().AbsoluteUri).GetActionObject("setstrval", profile.Username, "active", OASIS_EOSIO_ACCOUNT,
                    new EOSIOSetStringValueParams()
                    {
                        eosio_acc = profile.Username,
                        field_name = "password",
                        new_value = StringCipher.Encrypt(profile.Password, OASIS_PASS_PHRASE)
                    }
                ));

            actions.Add(new ActionUtility(chainApi.GetHost().AbsoluteUri).GetActionObject("setstrval", profile.Username, "active", OASIS_EOSIO_ACCOUNT,
                    new EOSIOSetStringValueParams()
                    {
                        eosio_acc = profile.Username,
                        field_name = "email",
                        new_value = profile.Email
                    }
                ));

            actions.Add(new ActionUtility(chainApi.GetHost().AbsoluteUri).GetActionObject("setstrval", profile.Username, "active", OASIS_EOSIO_ACCOUNT,
                    new EOSIOSetStringValueParams()
                    {
                        eosio_acc = profile.Username,
                        field_name = "title",
                        new_value = profile.Title
                    }
                ));

            actions.Add(new ActionUtility(chainApi.GetHost().AbsoluteUri).GetActionObject("setstrval", profile.Username, "active", OASIS_EOSIO_ACCOUNT,
                    new EOSIOSetStringValueParams()
                    {
                        eosio_acc = profile.Username,
                        field_name = "firstname",
                        new_value = profile.FirstName
                    }
                ));

            actions.Add(new ActionUtility(chainApi.GetHost().AbsoluteUri).GetActionObject("setstrval", profile.Username, "active", OASIS_EOSIO_ACCOUNT,
                    new EOSIOSetStringValueParams()
                    {
                        eosio_acc = profile.Username,
                        field_name = "lastname",
                        new_value = profile.LastName
                    }
                ));

            actions.Add(new ActionUtility(chainApi.GetHost().AbsoluteUri).GetActionObject("setstrval", profile.Username, "active", OASIS_EOSIO_ACCOUNT,
                    new EOSIOSetStringValueParams()
                    {
                        eosio_acc = profile.Username,
                        field_name = "dob",
                        new_value = profile.DOB
                    }
                ));

            actions.Add(new ActionUtility(chainApi.GetHost().AbsoluteUri).GetActionObject("setstrval", profile.Username, "active", OASIS_EOSIO_ACCOUNT,
                    new EOSIOSetStringValueParams()
                    {
                        eosio_acc = profile.Username,
                        field_name = "playeraddr",
                        new_value = profile.PlayerAddress
                    }
                ));

            actions.Add(new ActionUtility(chainApi.GetHost().AbsoluteUri).GetActionObject("setintval", profile.Username, "active", OASIS_EOSIO_ACCOUNT,
                    new EOSIOSetIntValueParams()
                    {
                        eosio_acc = profile.Username,
                        field_name = "karma",
                        new_value = profile.Karma
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
