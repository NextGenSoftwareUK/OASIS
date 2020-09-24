using EOSNewYork.EOSCore;
using EOSNewYork.EOSCore.Utilities;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.EOSIOClasses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public class EOSIOOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        public const string OASIS_EOSIO_ACCOUNT = "oasis";
        //public const string OASIS_PASS_PHRASE = "oasis";
        public const string OASIS_PASS_PHRASE = "7g7GJ557j549':;#~~#$4jf&hjj4";

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

        public override IEnumerable<IAvatar> LoadAllAvatars()
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> LoadAvatarAsync(string providerKey)
        {
            var chainApi = new ChainAPI();
            var rows = await chainApi.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", providerKey, providerKey, 1, 3);

            if (rows.rows.Count == 0)
                return null;

            var AvatarRow = (EOSIOAccountTableRow)rows.rows[0];
            var Avatar = AvatarRow.ToAvatar();
            Avatar.Password = StringCipher.Decrypt(Avatar.Password, OASIS_PASS_PHRASE);

            return Avatar;
        }

        public override async Task<IAvatar> LoadAvatarAsync(Guid Id)
        {
            var chainApi = new ChainAPI();
            //var rows = await chainApi.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", Id.ToString(), Id.ToString(), 1);
            var rows = await chainApi.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", Id.ToString(), Id.ToString(), 1, 1);

            if (rows.rows.Count == 0)
                return null;

            var AvatarRow = (EOSIOAccountTableRow)rows.rows[0];
            var Avatar = AvatarRow.ToAvatar();

            Avatar.Password = StringCipher.Decrypt(Avatar.Password, OASIS_PASS_PHRASE);
            return Avatar;
        }

        public override async Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            var chainApi = new ChainAPI();
            var rows = await chainApi.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", username, username, 1, 2);

            if (rows.rows.Count == 0)
                return null;

            var AvatarRow = (EOSIOAccountTableRow)rows.rows[0];
            var Avatar = AvatarRow.ToAvatar();

            Avatar.Password = StringCipher.Decrypt(Avatar.Password, OASIS_PASS_PHRASE);
            return Avatar;
        }

        public override IAvatar SaveAvatar(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
        {
            var chainApi = new ChainAPI();
            var rows = await chainApi.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "config", "true", null, null, 1);

            if (rows.rows.Count == 0)
                return null;

            var configRow = (EOSIOConfigTableRow)rows.rows[0];
            var actions = new List<EOSNewYork.EOSCore.Params.Action>();

            actions.Add(new ActionUtility(chainApi.GetHost().AbsoluteUri).GetActionObject("openacct", configRow.admin, "active", OASIS_EOSIO_ACCOUNT,
                    new EOSIOOpenAccountParams()
                    {
                        userid = Avatar.Id.ToString(),
                        eosio_acc = Avatar.Username,
                        providerkey = Avatar.ProviderKey,
                        password = StringCipher.Encrypt(Avatar.Password, OASIS_PASS_PHRASE),
                        email = Avatar.Email,
                        title = Avatar.Title,
                        firstname = Avatar.FirstName,
                        lastname = Avatar.LastName,
                        dob = Avatar.DOB.ToString(),
                        playeraddr = Avatar.Address,
                        karma = Avatar.Karma
                    }
                ));

            List<string> privateKeysInWIF = new List<string> { "private key in WIF" };
            await chainApi.PushTransactionAsync(actions.ToArray(), privateKeysInWIF);

            return Avatar;
        }

        public override Task<ISearchResults> SearchAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }
    }
}
