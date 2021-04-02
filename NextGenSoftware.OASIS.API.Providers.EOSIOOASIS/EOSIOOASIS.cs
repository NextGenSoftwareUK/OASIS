using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EOSNewYork.EOSCore;
using EOSNewYork.EOSCore.Utilities;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.EOSIOClasses;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public class EOSIOOASIS : OASISStorageBase, IOASISStorage, IOASISNET, IOASISSuperStar
    {
        // Lookup Cache. TODO: Move to generic CacheManager in OASIS.API.Core, maybe also in ProviderManager so other providers can also share the cache.
        private static Dictionary<Guid, string> _avatarIdToEOSAccountNameLookup = new Dictionary<Guid, string>();
        private static Dictionary<Guid, Account> _avatarIdToEOSAccountLookup = new Dictionary<Guid, Account>();
        private static Dictionary<string, Guid> _eosAccountNameToAvatarIdLookup = new Dictionary<string, Guid>();
        private static Dictionary<string, IAvatar> _eosAccountNameToAvatarLookup = new Dictionary<string, IAvatar>();
        private AvatarManager _avatarManager = null;

        public const string OASIS_EOSIO_ACCOUNT = "oasis";
        //public const string OASIS_PASS_PHRASE = "oasis";
        public const string OASIS_PASS_PHRASE = "7g7GJ557j549':;#~~#$4jf&hjj4";

        public ChainAPI ChainAPI { get; set; }

        private AvatarManager AvatarManagerInstance
        {
            get
            {
                if (_avatarManager == null)
                     _avatarManager = new AvatarManager(this);

                return _avatarManager;
            }
        }

        public EOSIOOASIS(string host)
        {
            this.ProviderName = "EOSIOOASIS";
            this.ProviderDescription = "EOSIO Provider";
            this.ProviderType = new Core.Helpers.EnumValue<ProviderType>(Core.Enums.ProviderType.EOSOASIS);
            this.ProviderCategory = new Core.Helpers.EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            ChainAPI = new ChainAPI(host);
        }

        public override bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatar(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteHolon(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPlayer> GetPlayersNearMe()
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

        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.Holon)
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
            var rows = await ChainAPI.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", providerKey, providerKey, 1, 3);

            if (rows.rows.Count == 0)
                return null;

            var AvatarRow = (EOSIOAccountTableRow)rows.rows[0];
            var Avatar = AvatarRow.ToAvatar();
            Avatar.Password = StringCipher.Decrypt(Avatar.Password, OASIS_PASS_PHRASE);

            return Avatar;
        }

        public override async Task<IAvatar> LoadAvatarAsync(Guid Id)
        {
            //var rows = await chainApi.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", Id.ToString(), Id.ToString(), 1);
            var rows = await ChainAPI.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", Id.ToString(), Id.ToString(), 1, 1);

            if (rows.rows.Count == 0)
                return null;

            var AvatarRow = (EOSIOAccountTableRow)rows.rows[0];
            var Avatar = AvatarRow.ToAvatar();

            Avatar.Password = StringCipher.Decrypt(Avatar.Password, OASIS_PASS_PHRASE);
            return Avatar;
        }

        public override async Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            var rows = await ChainAPI.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", username, username, 1, 2);

            if (rows.rows.Count == 0)
                return null;

            var AvatarRow = (EOSIOAccountTableRow)rows.rows[0];
            var Avatar = AvatarRow.ToAvatar();

            Avatar.Password = StringCipher.Decrypt(Avatar.Password, OASIS_PASS_PHRASE);
            return Avatar;
        }

        public override IAvatar LoadAvatarForProviderKey(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(Guid id, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(string providerKey, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(Guid id, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(string providerKey, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            throw new NotImplementedException();
        }

        public override IAvatar SaveAvatar(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
        {
            var rows = await ChainAPI.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "config", "true", null, null, 1);

            if (rows.rows.Count == 0)
                return null;

            var configRow = (EOSIOConfigTableRow)rows.rows[0];
            var actions = new List<EOSNewYork.EOSCore.Params.Action>();

            actions.Add(new ActionUtility(ChainAPI.GetHost().AbsoluteUri).GetActionObject("openacct", configRow.admin, "active", OASIS_EOSIO_ACCOUNT,
                    new EOSIOOpenAccountParams()
                    {
                        userid = Avatar.Id.ToString(),
                        eosio_acc = Avatar.Username,
                        providerkey = Avatar.ProviderKey[Core.Enums.ProviderType.EOSOASIS],
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
            await ChainAPI.PushTransactionAsync(actions.ToArray(), privateKeysInWIF);

            return Avatar;
        }

        public override IHolon SaveHolon(IHolon holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> SaveHolonAsync(IHolon holon)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> SaveHolonsAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<ISearchResults> SearchAsync(ISearchParams searchTerm)
        {
            throw new NotImplementedException();
        }


        public async Task<Account> GetEOSAccountAsync(string eosAccountName)
        {
            var account = await ChainAPI.GetAccountAsync(eosAccountName);
            return account;
        }

        public Account GetEOSAccount(string eosAccountName)
        {
            var account = ChainAPI.GetAccount(eosAccountName);
            return account;
        }

        public async Task<string> GetBalanceAsync(string eosAccountName, string code, string symbol)
        {
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/balancecheck.js
            //eos.getCurrencyBalance("token.seeds", account, 'SEEDS')

            //var accountBalance = tableAPI.GetTokenAccountBalance(new GetTokenAccountBalanceConstructorSettings() { accountName = "wozzawozza11", tokenContract = "epraofficial" });
            //Console.WriteLine(accountBalance[0].balance_decimal);
            //Console.WriteLine(accountBalance[0].symbol);

            //var currencyBalance = await _eosioOaisis.ChainAPI.GetCurrencyBalanceAsync(eosAccountName, "seeds.seeds", "SEEDS");
            var currencyBalance = await ChainAPI.GetCurrencyBalanceAsync(eosAccountName, code, symbol);
            return currencyBalance.balances[0];
        }

        public string GetBalanceForEOSAccount(string eosAccountName, string code, string symbol)
        {
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/balancecheck.js
            //eos.getCurrencyBalance("token.seeds", account, 'SEEDS')

            var currencyBalance = ChainAPI.GetCurrencyBalance(eosAccountName, code, symbol);
            return currencyBalance.balances[0];
        }

        public string GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            return GetBalanceForEOSAccount(GetEOSAccountNameForAvatar(avatarId), code, symbol);
        }

        public string GetEOSAccountNameForAvatar(Guid avatarId)
        {
            if (!_avatarIdToEOSAccountNameLookup.ContainsKey(avatarId))
                _avatarIdToEOSAccountNameLookup[avatarId] = AvatarManagerInstance.LoadAvatar(avatarId).ProviderKey[Core.Enums.ProviderType.EOSOASIS];

            return _avatarIdToEOSAccountNameLookup[avatarId];
        }

        public Account GetEOSAccountForAvatar(Guid avatarId)
        {
            if (!_avatarIdToEOSAccountLookup.ContainsKey(avatarId))
                _avatarIdToEOSAccountLookup[avatarId] = GetEOSAccount(GetEOSAccountNameForAvatar(avatarId));

            return _avatarIdToEOSAccountLookup[avatarId];
        }

        public Guid GetAvatarIdForEOSAccountName(string eosAccountName)
        {
            if (!_eosAccountNameToAvatarIdLookup.ContainsKey(eosAccountName))
                _eosAccountNameToAvatarIdLookup[eosAccountName] = AvatarManagerInstance.LoadAllAvatars().FirstOrDefault(x => x.ProviderKey[Core.Enums.ProviderType.EOSOASIS] == eosAccountName).Id;

            return _eosAccountNameToAvatarIdLookup[eosAccountName];
        }

        public IAvatar GetAvatarForEOSAccountName(string eosAccountName)
        {
            if (!_eosAccountNameToAvatarLookup.ContainsKey(eosAccountName))
                _eosAccountNameToAvatarLookup[eosAccountName] = AvatarManagerInstance.LoadAllAvatars().FirstOrDefault(x => x.ProviderKey[Core.Enums.ProviderType.EOSOASIS] == eosAccountName);

            return _eosAccountNameToAvatarLookup[eosAccountName];
        }

    }
}
