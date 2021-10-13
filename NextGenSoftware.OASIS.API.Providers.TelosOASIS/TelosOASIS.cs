using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
{
    public class TelosOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        private static Dictionary<Guid, Account> _avatarIdToTelosAccountLookup = new Dictionary<Guid, Account>();
        private AvatarManager _avatarManager = null;

        public string Host { get; set; }
        public EOSIOOASIS.EOSIOOASIS EOSIOOASIS { get; set; }

        //TODO: We may just want to pass in the host and not the full OASISDNA is it is not needed?
        public TelosOASIS(string host) : base()
        {
            this.Host = host;
            Init();
        }

        // I think we should only pass in the OASISDNA if we need more than just the Host? Or if we need to update the OASISDNA like IPFSOASIS does...
        /*
        public TelosOASIS() : base()
        {

        }

        public TelosOASIS(string OASISDNAPath) : base(OASISDNAPath)
        {
            Init();
        }

        public TelosOASIS(OASISDNA OASISDNA) : base(OASISDNA)
        {
            Init();
        }

        public TelosOASIS(OASISDNA OASISDNA, string OASISDNAPath) : base(OASISDNA, OASISDNAPath)
        {
            Init();
        }*/

        private void Init()
        {
            this.ProviderName = "TelosOASIS";
            this.ProviderDescription = "Telos Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TelosOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            //EOSIOOASIS = new EOSIOOASIS.EOSIOOASIS(OASISDNA.OASIS.StorageProviders.TelosOASIS.ConnectionString);
            EOSIOOASIS = new EOSIOOASIS.EOSIOOASIS(this.Host);
        }

        private AvatarManager AvatarManagerInstance
        {
            get
            {
                if (_avatarManager == null)
                    _avatarManager = new AvatarManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.MongoDBOASIS), OASISDNA);
                    //_avatarManager = new AvatarManager(this); // TODO: URGENT: PUT THIS BACK IN ASAP! TEMP USING MONGO UNTIL EOSIO/Telos METHODS IMPLEMENTED...

                return _avatarManager;
            }
        }

        public override bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
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

        public override async Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
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

        public override IAvatar LoadAvatarByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatarByUsername(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override int CreateOland(IOland oland)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateOland(IOland oland)
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

        public override async Task<IAvatar> LoadAvatarByUsernameAsync(string avatarUsername)
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

        public override async Task<IAvatar> LoadAvatarByEmailAsync(string avatarEmail)
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

      

        public override IAvatar SaveAvatar(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<ISearchResults> SearchAsync(ISearchParams searchParams)
        {
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<IOland>> LoadAllOlandsAsync()
        {
            throw new NotImplementedException();
        }

        public override async Task<IOland> LoadOlandAsync(int id)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> DeleteOlandAsync(int id)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> DeleteOlandAsync(int id, bool safeDelete)
        {
            throw new NotImplementedException();
        }

        public override async Task<int> CreateOlandAsync(IOland oland)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> UpdateOlandAsync(IOland oland)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IOland> LoadAllOlands()
        {
            throw new NotImplementedException();
        }

        public override IOland LoadOland(int id)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteOland(int id)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteOland(int id, bool safeDelete)
        {
            throw new NotImplementedException();
        }

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

        public string GetTelosAccountNameForAvatar(Guid avatarId)
        {
            return AvatarManagerInstance.GetProviderKeyForAvatar(avatarId, Core.Enums.ProviderType.TelosOASIS);
        }

        public string GetTelosAccountPrivateKeyForAvatar(Guid avatarId)
        {
            return AvatarManagerInstance.GetPrivateProviderKeyForAvatar(avatarId, Core.Enums.ProviderType.TelosOASIS);
        }

        public Account GetTelosAccountForAvatar(Guid avatarId)
        {
            //TODO: Do we need to cache this?
            if (!_avatarIdToTelosAccountLookup.ContainsKey(avatarId))
                _avatarIdToTelosAccountLookup[avatarId] = GetTelosAccount(GetTelosAccountNameForAvatar(avatarId));

            return _avatarIdToTelosAccountLookup[avatarId];
        }

        public Guid GetAvatarIdForTelosAccountName(string telosAccountName)
        {
            return AvatarManagerInstance.GetAvatarIdForProviderKey(telosAccountName, Core.Enums.ProviderType.TelosOASIS);
        }

        public IAvatar GetAvatarForTelosAccountName(string telosAccountName)
        {
            return AvatarManagerInstance.GetAvatarForProviderKey(telosAccountName, Core.Enums.ProviderType.TelosOASIS);
        }

        public override IHolon LoadHolon(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetail(Guid id)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail SaveAvatarDetail(IAvatarDetail Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildrenRecursive = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildrenRecursive = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
        {
            throw new NotImplementedException();
        }
    }
}
