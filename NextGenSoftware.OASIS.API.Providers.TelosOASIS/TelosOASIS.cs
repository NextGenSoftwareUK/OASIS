using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
{
    public class TelosOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        //private static Dictionary<Guid, string> _avatarIdToTelosAccountNameLookup = new Dictionary<Guid, string>();
        //private static Dictionary<Guid, Account> _avatarIdToTelosAccountLookup = new Dictionary<Guid, Account>();
        //private static Dictionary<string, Guid> _telosAccountNameToAvatarIdLookup = new Dictionary<string, Guid>();
        //private static Dictionary<string, IAvatar> _telosAccountNameToAvatarLookup = new Dictionary<string, IAvatar>();

        public TelosOASIS()
        {
            this.ProviderName = "TelosOASIS";
            this.ProviderDescription = "Telos Provider";
            this.ProviderType = new API.Core.Helpers.EnumValue<ProviderType>(API.Core.Enums.ProviderType.TelosOASIS);
            this.ProviderCategory = new Core.Helpers.EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
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

        public override IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> SaveHolonsAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<ISearchResults> SearchAsync(ISearchParams searchParams)
        {
            throw new NotImplementedException();
        }


        /*
        public async Task<Account> GetTelosAccountAsync(string telosAccountName)
        {
            var account = await EOSIOOASIS.ChainAPI.GetAccountAsync(telosAccountName);
            return account;
        }

        public Account GetTelosAccount(string telosAccountName)
        {
            var account = EOSIOOASIS.ChainAPI.GetAccount(telosAccountName);
            return account;
        }

        public async Task<string> GetBalanceAsync(string telosAccountName)
        {
            //var currencyBalance = await EOSIOOASIS.ChainAPI.GetCurrencyBalanceAsync(telosAccountName, "seeds.seeds", "SEEDS");
            //var currencyBalance = await EOSIOOASIS.ChainAPI.GetCurrencyBalanceAsync(telosAccountName, "token.seeds", "SEEDS");
            //return currencyBalance.balances[0];

            return await EOSIOOASIS.GetBalanceAsync(telosAccountName, "token.seeds", "SEEDS");
        }

        public string GetBalanceForTelosAccount(string telosAccountName)
        {
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/balancecheck.js
            //eos.getCurrencyBalance("token.seeds", account, 'SEEDS')

            // var currencyBalance = EOSIOOASIS.ChainAPI.GetCurrencyBalance(telosAccountName, "token.seeds", "SEEDS");
            //return currencyBalance.balances[0];

            return EOSIOOASIS.GetBalanceForEOSAccount(telosAccountName, "token.seeds", "SEEDS");
        }

        public string GetBalanceForAvatar(Guid avatarId)
        {
            //return GetBalanceForTelosAccount(GetTelosAccountNameForAvatar(avatarId));
            return EOSIOOASIS.GetBalanceForAvatar(avatarId, "token.seeds", "SEEDS");
        }


        // TODO: URGENT - NEED TO MOVE THESE TO THE TELOSOASIS PROVIDER (EOSOASIS is a different chain so the account names will be different so cant use the EOS methods as SEEDSOASIS currently does).
        // Need to make these cache lookups generic for all OASIS Providers, sort this ASAP! ;-)

        public string GetTelosAccountNameForAvatar(Guid avatarId)
        {
            if (!_avatarIdToTelosAccountNameLookup.ContainsKey(avatarId))
                _avatarIdToTelosAccountNameLookup[avatarId] = AvatarManagerInstance.LoadAvatar(avatarId).ProviderKey[Core.Enums.ProviderType.TelosOASIS];

            return _avatarIdToTelosAccountNameLookup[avatarId];
        }

        public Account GetTelosAccountForAvatar(Guid avatarId)
        {
            if (!_avatarIdToTelosAccountLookup.ContainsKey(avatarId))
                _avatarIdToTelosAccountLookup[avatarId] = GetTelosAccount(GetTelosAccountNameForAvatar(avatarId));

            return _avatarIdToTelosAccountLookup[avatarId];
        }

        public Guid GetAvatarIdForTelosAccountName(string telosAccountName)
        {
            if (!_telosAccountNameToAvatarIdLookup.ContainsKey(telosAccountName))
                _telosAccountNameToAvatarIdLookup[telosAccountName] = AvatarManagerInstance.LoadAllAvatars().FirstOrDefault(x => x.ProviderKey[Core.Enums.ProviderType.TelosOASIS] == telosAccountName).Id;

            return _telosAccountNameToAvatarIdLookup[telosAccountName];
        }

        public IAvatar GetAvatarForTelosAccountName(string telosAccountName)
        {
            if (!_telosAccountNameToAvatarLookup.ContainsKey(telosAccountName))
                _telosAccountNameToAvatarLookup[telosAccountName] = AvatarManagerInstance.LoadAllAvatars().FirstOrDefault(x => x.ProviderKey[Core.Enums.ProviderType.TelosOASIS] == telosAccountName);

            return _telosAccountNameToAvatarLookup[telosAccountName];
        }
        */
    }
}
