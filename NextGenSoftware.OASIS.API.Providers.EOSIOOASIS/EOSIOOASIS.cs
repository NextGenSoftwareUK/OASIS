using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EOSNewYork.EOSCore;
using EOSNewYork.EOSCore.Utilities;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.EOSIOClasses;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public class EOSIOOASIS : OASISStorageProviderBase, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISNETProvider, IOASISSuperStar
    {
        private static Dictionary<Guid, Account> _avatarIdToEOSIOAccountLookup = new Dictionary<Guid, Account>();
        private AvatarManager _avatarManager = null;
        private const string OASIS_EOSIO_ACCOUNT = "oasis";

        public string HostURI { get; set; }
        public ChainAPI ChainAPI { get; set; }

        private AvatarManager AvatarManagerInstance
        {
            get
            {
                if (_avatarManager == null)
                    _avatarManager = new AvatarManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.MongoDBOASIS), AvatarManagerInstance.OASISDNA);
                    //_avatarManager = new AvatarManager(this); // TODO: URGENT: PUT THIS BACK IN ASAP! TEMP USING MONGO UNTIL EOSIO METHODS IMPLEMENTED...

                return _avatarManager;
            }
        }

        public EOSIOOASIS(string hostURI)
        {
            this.ProviderName = "EOSIOOASIS";
            this.ProviderDescription = "EOSIO Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EOSIOOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            HostURI = hostURI;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            ChainAPI = new ChainAPI(HostURI);
            return base.ActivateProvider();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            //TODO: Find if there is a disconnect/shutdown 
            ChainAPI = null;
            return base.DeActivateProvider();
        }

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            //var rows = await chainApi.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", Id.ToString(), Id.ToString(), 1);
            var rows = await ChainAPI.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", Id.ToString(), Id.ToString(), 1, 1);

            if (rows.rows.Count == 0)
                return null;

            var AvatarRow = (EOSIOAccountTableRow)rows.rows[0];
            var Avatar = AvatarRow.ToAvatar();

            Avatar.Password = StringCipher.Decrypt(Avatar.Password);
            return new OASISResult<IAvatar>(Avatar);
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        {
            var rows = await ChainAPI.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", username, username, 1, 2);

            if (rows.rows.Count == 0)
                return null;

            var AvatarRow = (EOSIOAccountTableRow)rows.rows[0];
            var Avatar = AvatarRow.ToAvatar();

            Avatar.Password = StringCipher.Decrypt(Avatar.Password);
            return new OASISResult<IAvatar>(Avatar);
        }

        public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            var rows = await ChainAPI.GetTableRowsAsync(OASIS_EOSIO_ACCOUNT, OASIS_EOSIO_ACCOUNT, "accounts", "true", providerKey, providerKey, 1, 3);

            if (rows.rows.Count == 0)
                return null;

            var AvatarRow = (EOSIOAccountTableRow)rows.rows[0];
            var Avatar = AvatarRow.ToAvatar();
            Avatar.Password = StringCipher.Decrypt(Avatar.Password);
            result.Result = Avatar;
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
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
                        providerkey = Avatar.ProviderKey[Core.Enums.ProviderType.EOSIOOASIS],
                        password = StringCipher.Encrypt(Avatar.Password),
                        email = Avatar.Email,
                        title = Avatar.Title,
                        firstname = Avatar.FirstName,
                        lastname = Avatar.LastName,
                        //dob = Avatar.DOB.ToString(),
                        // playeraddr = Avatar.Address,
                        // karma = Avatar.Karma
                    }
                ));

            List<string> privateKeysInWIF = new List<string> { "private key in WIF" };
            await ChainAPI.PushTransactionAsync(actions.ToArray(), privateKeysInWIF);

            return new OASISResult<IAvatar>(Avatar);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            throw new NotImplementedException();
        }

        public async Task<Account> GetEOSIOAccountAsync(string eosioAccountName)
        {
            var account = await ChainAPI.GetAccountAsync(eosioAccountName);
            return account;
        }

        public Account GetEOSIOAccount(string eosioAccountName)
        {
            var account = ChainAPI.GetAccount(eosioAccountName);
            return account;
        }

        public async Task<string> GetBalanceAsync(string eosioAccountName, string code, string symbol)
        {
            var currencyBalance = await ChainAPI.GetCurrencyBalanceAsync(eosioAccountName, code, symbol);
            return currencyBalance.balances[0];
        }

        public string GetBalanceForEOSIOAccount(string eosioAccountName, string code, string symbol)
        {
            var currencyBalance = ChainAPI.GetCurrencyBalance(eosioAccountName, code, symbol);
            return currencyBalance.balances[0];
        }

        public string GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            return GetBalanceForEOSIOAccount(GetEOSIOAccountNameForAvatar(avatarId), code, symbol);
        }

        public string GetEOSIOAccountNameForAvatar(Guid avatarId)
        {
            return AvatarManagerInstance.GetProviderKeyForAvatar(avatarId, Core.Enums.ProviderType.EOSIOOASIS);
        }

        public string GetEOSIOAccountPrivateKeyForAvatar(Guid avatarId)
        {
            return AvatarManagerInstance.GetPrivateProviderKeyForAvatar(avatarId, Core.Enums.ProviderType.EOSIOOASIS);
        }

        public Account GetEOSIOAccountForAvatar(Guid avatarId)
        {
            //TODO: Do we need to cache this?
            if (!_avatarIdToEOSIOAccountLookup.ContainsKey(avatarId))
                _avatarIdToEOSIOAccountLookup[avatarId] = GetEOSIOAccount(GetEOSIOAccountNameForAvatar(avatarId));

            return _avatarIdToEOSIOAccountLookup[avatarId];
        }

        public Guid GetAvatarIdForEOSIOAccountName(string eosioAccountName)
        {
            return AvatarManagerInstance.GetAvatarIdForProviderKey(eosioAccountName, Core.Enums.ProviderType.EOSIOOASIS);
        }

        public IAvatar GetAvatarForEOSIOAccountName(string eosioAccountName)
        {
            return AvatarManagerInstance.GetAvatarForProviderKey(eosioAccountName, Core.Enums.ProviderType.EOSIOOASIS);
        }
    }
}
