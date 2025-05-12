using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
{
    public class TelosOASIS : OASISStorageProviderBase, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISNETProvider
    {
        private static Dictionary<Guid, GetAccountResponseDto> _avatarIdToTelosAccountLookup = new Dictionary<Guid, GetAccountResponseDto>();
        private AvatarManager _avatarManager = null;
        private KeyManager _keyManager = null;

        public EOSIOOASIS.EOSIOOASIS EOSIOOASIS { get; set; }

        public TelosOASIS(string host, string eosAccountName, string eosChainId, string eosAccountPk)
        {
            this.ProviderName = "TelosOASIS";
            this.ProviderDescription = "Telos Provider";
            this.ProviderType = new EnumValue<ProviderType>(API.Core.Enums.ProviderType.TelosOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            EOSIOOASIS = new EOSIOOASIS.EOSIOOASIS(host, eosAccountName, eosChainId, eosAccountPk);
        }

        private AvatarManager AvatarManager
        {
            get
            {
                if (_avatarManager == null)
                    _avatarManager = new AvatarManager(ProviderManager.Instance.GetStorageProvider(Core.Enums.ProviderType.MongoDBOASIS));
                    //_avatarManager = new AvatarManager(this); // TODO: URGENT: PUT THIS BACK IN ASAP! TEMP USING MONGO UNTIL EOSIO/Telos METHODS IMPLEMENTED...

                return _avatarManager;
            }
        }

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(ProviderManager.Instance.GetStorageProvider(Core.Enums.ProviderType.MongoDBOASIS));
                    //_keyManager = new KeyManager(this, AvatarManager); // TODO: URGENT: PUT THIS BACK IN ASAP! TEMP USING MONGO UNTIL EOSIO METHODS IMPLEMENTED...

                return _keyManager;
            }
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            if (!EOSIOOASIS.IsProviderActivated)
                await EOSIOOASIS.ActivateProviderAsync();

            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> ActivateProvider()
        {
            if (!EOSIOOASIS.IsProviderActivated)
                EOSIOOASIS.ActivateProvider();

            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            if (EOSIOOASIS.IsProviderActivated)
                await EOSIOOASIS.DeActivateProviderAsync();

            _avatarManager = null;
            _keyManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            if (EOSIOOASIS.IsProviderActivated)
                EOSIOOASIS.DeActivateProvider();

            _avatarManager = null;
            _keyManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        // TODO: Implement GetAccountAsync in EOS provider
        public async Task<Account> GetTelosAccountAsync(string telosAccountName)
        {
            // var account = await EOSIOOASIS.ChainAPI.GetAccountAsync(telosAccountName);
            return new Account();
        }

        // TODO: Implement GetAccount in EOS provider
        public GetAccountResponseDto GetTelosAccount(string telosAccountName)
        { 
            return EOSIOOASIS.GetEOSIOAccount(telosAccountName);
        }

        public async Task<string> GetBalanceAsync(string telosAccountName, string code, string symbol)
        {
            return await EOSIOOASIS.GetBalanceAsync(telosAccountName, code, symbol);
        }

        public string GetBalanceForTelosAccount(string telosAccountName, string code, string symbol)
        {
            return EOSIOOASIS.GetBalanceForEOSIOAccount(telosAccountName, code, symbol);
        }

        public string GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            return EOSIOOASIS.GetBalanceForAvatar(avatarId, code, symbol);
        }

        public List<string> GetTelosAccountNamesForAvatar(Guid avatarId)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetProviderPublicKeysForAvatarById(avatarId, Core.Enums.ProviderType.TelosOASIS).Result;
        }

        public string GetTelosAccountPrivateKeyForAvatar(Guid avatarId)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, Core.Enums.ProviderType.TelosOASIS).Result[0];
        }

        public GetAccountResponseDto GetTelosAccountForAvatar(Guid avatarId)
        {
            //TODO: Do we need to cache this?
            if (!_avatarIdToTelosAccountLookup.ContainsKey(avatarId))
                _avatarIdToTelosAccountLookup[avatarId] = GetTelosAccount(GetTelosAccountNamesForAvatar(avatarId)[0]);

            //TODO: The OASIS can store multiple Public Keys (Telos Accounts) per Avatar but currently we will only retreive the first one.
            // Need to add support to load multiple if needed?
            return _avatarIdToTelosAccountLookup[avatarId];
        }

        public Guid GetAvatarIdForTelosAccountName(string telosAccountName)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetAvatarIdForProviderPublicKey(telosAccountName, Core.Enums.ProviderType.TelosOASIS).Result;
        }

        public IAvatar GetAvatarForTelosAccountName(string telosAccountName)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetAvatarForProviderPublicKey(telosAccountName, Core.Enums.ProviderType.TelosOASIS).Result;
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

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            throw new NotImplementedException();
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

        //public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}


        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
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

        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            throw new NotImplementedException();
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

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return null;
        }

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
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

        public OASISResult<ITransactionRespone> SendTransaction(IWalletTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<ITransactionRespone>> SendTransactionAsync(IWalletTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        OASISResult<ITransactionRespone> IOASISBlockchainStorageProvider.SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        Task<OASISResult<ITransactionRespone>> IOASISBlockchainStorageProvider.SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        Task<OASISResult<ITransactionRespone>> IOASISBlockchainStorageProvider.SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        OASISResult<ITransactionRespone> IOASISBlockchainStorageProvider.SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        Task<OASISResult<ITransactionRespone>> IOASISBlockchainStorageProvider.SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        OASISResult<ITransactionRespone> IOASISBlockchainStorageProvider.SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        Task<OASISResult<INFTTransactionRespone>> IOASISNFTProvider.SendNFTAsync(INFTWalletTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequestForProvider transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequestForProvider transation)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IOASISNFT> LoadNFT(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IOASISNFT>> LoadNFTAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IOASISNFT> LoadNFT(string hash)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IOASISNFT>> LoadNFTAsync(string hash)
        {
            throw new NotImplementedException();
        }

        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId)
        {
            throw new NotImplementedException();
        }

        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        {
            throw new NotImplementedException();
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForAvatar(Guid avatarId)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId)
        {
            throw new NotImplementedException();
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForMintAddress(string mintWalletAddress)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IOASISGeoSpatialNFT> PlaceGeoNFT(IPlaceGeoSpatialNFTRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IOASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
