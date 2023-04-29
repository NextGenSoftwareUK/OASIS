using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.Holochain.HoloNET.Client;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HoloOASIS : OASISStorageProviderBase, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISLocalStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
    {
        private const string ZOME_LOAD_AVATAR_BY_ID_FUNCTION = "get_entry_avatar_by_id";
        private const string ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION = "get_entry_avatar_by_username";
        private const string ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION = "get_entry_avatar_by_email";
        private const string ZOME_DELETE_AVATAR_BY_ID_FUNCTION = "delete_entry_avatar_by_id";
        private const string ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION = "delete_entry_avatar_by_username";
        private const string ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION = "delete_entry_avatar_by_email";
        private const string ZOME_LOAD_HOLON_BY_ID_FUNCTION = "get_entry_holon_by_id";

        private bool _useReflection = false;
        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        //TODO: Not sure if we need these?
        //public delegate void AvatarSaved(object sender, AvatarSavedEventArgs e);
        //public event AvatarSaved OnPlayerAvatarSaved;

        //public delegate void AvatarLoaded(object sender, AvatarLoadedEventArgs e);
        //public event AvatarLoaded OnPlayerAvatarLoaded;

        public delegate void HoloOASISError(object sender, HoloOASISErrorEventArgs e);
        public event HoloOASISError OnHoloOASISError;

        public HoloNETClient HoloNETClient { get; private set; }

        public HoloOASIS(HoloNETClient holoNETClient, bool useReflection = true)
        {
            _useReflection = useReflection;
            this.HoloNETClient = holoNETClient;
            Initialize();
        }

        public HoloOASIS(string holochainConductorURI, bool useReflection = true)
        {
            _useReflection = useReflection;
            HoloNETClient = new HoloNETClient(holochainConductorURI);
            Initialize();
        }

        private async Task Initialize()
        {
            this.ProviderName = "HoloOASIS";
            this.ProviderDescription = "Holochain Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.HoloOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        #region IOASISStorageProvider Implementation

        public override OASISResult<bool> ActivateProvider()
        {
            if (HoloNETClient.State != System.Net.WebSockets.WebSocketState.Open && HoloNETClient.State != System.Net.WebSockets.WebSocketState.Connecting)
                HoloNETClient.Connect();

            return base.ActivateProvider();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            HoloNETClient.Disconnect();
            // HoloNETClient = null;
            return base.DeActivateProvider();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            //ProviderKey is the entry hash.
            return await LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "providerKey (entryhash)", providerKey);
        }

        public override OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            //ProviderKey is the entry hash.
            return Load<IAvatar>(HcObjectTypeEnum.Avatar, "providerKey (entryhash)", providerKey);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            //return await ExecuteOperationAsync<IAvatar>(OperationEnum.Read, "avatar", "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
            return await LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            //return ExecuteOperation<IAvatar>(OperationEnum.Read, "avatar", "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
            return Load<IAvatar>(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            //return await ExecuteOperationAsync<IAvatar>(OperationEnum.Read, "avatar", "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION, version);
            return await LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            //return ExecuteOperation<IAvatar>(OperationEnum.Read, "avatar", "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION, version);
            return Load<IAvatar>(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            //return await ExecuteOperationAsync<IAvatar>(HcOperationEnum.Read, "avatar", "username", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION, version);
            return await LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION);
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            //return ExecuteOperation<IAvatar>(HcOperationEnum.Read, "avatar", "email", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION, version);
            return Load<IAvatar>(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            //return await ExecuteOperationAsync<IAvatarDetail>(HcOperationEnum.Read, "avatar detail", "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION, version);
            return await LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            //return ExecuteOperation<IAvatarDetail>(HcOperationEnum.Read, "avatar detail", "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION, version);
            return Load<IAvatar>(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            //return await ExecuteOperationAsync<IAvatarDetail>(HcOperationEnum.Read, "avatar detail", "email", avatarEmail, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION, version);
            return await LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "email", email, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            //return ExecuteOperation<IAvatarDetail>(HcOperationEnum.Read, "avatar detail", "email", avatarEmail, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION, version);
            return Load<IAvatar>(HcObjectTypeEnum.Avatar, "email", email, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            //return await ExecuteOperationAsync<IAvatarDetail>(HcOperationEnum.Read, "avatar detail", "username", avatarUsername, ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION, version);
            return Load<IAvatar>(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            //return ExecuteOperation<IAvatarDetail>(HcOperationEnum.Read, "avatar detail", "username", avatarUsername, ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION, version);
            return Load<IAvatar>(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION);
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            return await ExecuteOperationAsync<IEnumerable<IAvatar>>(CollectionOperationEnum.ReadCollection, "avatars", "listanchor", ZOME_LOAD_ALL_AVATARS_FUNCTION, version);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return ExecuteOperation<IEnumerable<IAvatar>>(CollectionOperationEnum.ReadCollection, "avatars", "listanchor", ZOME_LOAD_ALL_AVATARS_FUNCTION, version);
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            return await ExecuteOperationAsync<IEnumerable<IAvatarDetail>>(CollectionOperationEnum.ReadCollection, "avatar details", "listanchor", ZOME_LOAD_ALL_AVATAR_DETAILS_FUNCTION, version);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return ExecuteOperation<IEnumerable<IAvatarDetail>>(CollectionOperationEnum.ReadCollection, "avatar details", "listanchor", ZOME_LOAD_ALL_AVATAR_DETAILS_FUNCTION, version);
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            return await SaveAsync(HcObjectTypeEnum.Avatar, avatar);
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return Save(HcObjectTypeEnum.Avatar, avatar);
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            return await SaveAsync(HcObjectTypeEnum.AvatarDetail, avatarDetail);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return Save(HcObjectTypeEnum.AvatarDetail, avatarDetail);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            return await DeleteAsync(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_DELETE_AVATAR_BY_ID_FUNCTION, softDelete);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return Delete(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_DELETE_AVATAR_BY_ID_FUNCTION, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await DeleteAsync(HcObjectTypeEnum.Avatar, "providerKey (entryHash)", providerKey, "", softDelete);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return Delete(HcObjectTypeEnum.Avatar, "providerKey (entryHash)", providerKey, "", softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            return await DeleteAsync(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return Delete(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            return await DeleteAsync(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return Delete(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION, softDelete);
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_FUNCTION_BY_ID, version, new { loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError});
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return Load<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_FUNCTION_BY_ID, version, new { loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, "", version, new { loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return Load<IHolon>(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, "", version, new { loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            //return await ExecuteOperationAsync<IEnumerable<IHolon>>(CollectionOperationEnum.ReadCollection, "holons", "listanchor", ZOME_LOAD_HOLONS_FOR_PARENT_FUNCTION, version);
            return await LoadCollectionAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION, version, new { id = id, type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            //return ExecuteOperation<IEnumerable<IHolon>>(CollectionOperationEnum.ReadCollection, "holons", "listanchor", ZOME_LOAD_HOLONS_FOR_PARENT_FUNCTION, version);
            return LoadCollection("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION, version, new { id = id, type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await LoadCollectionAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new { providerKey = providerKey, type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return LoadCollection("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new { providerKey = providerKey, type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await LoadCollectionAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new { type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return LoadCollection("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new { type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
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

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISSuperStar
        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<string> SendTransaction(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<string>> SendTransactionAsync(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<bool> SendNFT(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<bool>> SendNFTAsync(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISLocalStorageProvider

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            throw new NotImplementedException();
        }

        //public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWallets()
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

        //    //TODO: Finish Implementing.

        //    return result;
        //}

        //public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsAsync()
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

        //    //TODO: Finish Implementing.

        //    return result;
        //}

        //public OASISResult<bool> SaveProviderWallets(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    //TODO: Finish Implementing.

        //    return result;
        //}

        //public async Task<OASISResult<bool>> SaveProviderWalletsAsync(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    //TODO: Finish Implementing.

        //    return result;
        //}

        //public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsername(string username)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username)
        //{
        //    throw new NotImplementedException();
        //}

        //public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmail(string email)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email)
        //{
        //    throw new NotImplementedException();
        //}



        //public OASISResult<bool> SaveProviderWalletsForAvatarByUsername(string username, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<OASISResult<bool>> SaveProviderWalletsForAvatarByUsernameAsync(string username, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    throw new NotImplementedException();
        //}

        //public OASISResult<bool> SaveProviderWalletsForAvatarByEmail(string email, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<OASISResult<bool>> SaveProviderWalletsForAvatarByEmailAsync(string email, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    throw new NotImplementedException();
        //}

        #endregion

        #region Private Methods

        private IHcAvatar ConvertAvatarToHoloOASISAvatar(IAvatar avatar, IHcAvatar hcAvatar)
        {
            hcAvatar.Id = avatar.Id;
            hcAvatar.Username = avatar.Username;
            hcAvatar.Password = avatar.Password;
            hcAvatar.Email = avatar.Email;
            hcAvatar.Title = avatar.Title;
            hcAvatar.FirstName = avatar.FirstName;
            hcAvatar.LastName = avatar.LastName;
            hcAvatar.ProviderUniqueStorageKey = avatar.ProviderUniqueStorageKey == null ? string.Empty : avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS];
            hcAvatar.HolonType = avatar.HolonType;

            //TODO: Finish mapping

            return hcAvatar;
        }

        private IAvatar ConvertHcAvatarToAvatar(IHcAvatar hcAvatar)
        {
            Avatar avatar = new Avatar
            {
                Email = hcAvatar.Email,
                FirstName = hcAvatar.FirstName,
                HolonType = hcAvatar.HolonType,
                Id = hcAvatar.Id,
                LastName = hcAvatar.LastName,
                Password = hcAvatar.Password,
                Title = hcAvatar.Title,
                Username = hcAvatar.Username
                
                //TODO: Finish mapping
            };

            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS] = hcAvatar.ProviderUniqueStorageKey;
            return avatar;
        }

        private IAvatar ConvertKeyValuePairToAvatar(Dictionary<string, string> keyValuePair)
        {
            Avatar avatar = new Avatar
            {
                Id = new Guid(keyValuePair["id"]),
                Username = keyValuePair["username"],
                Password = keyValuePair["password"],
                Email = keyValuePair["email"],
                Title = keyValuePair["title"],
                FirstName = keyValuePair["first_name"],
                LastName = keyValuePair["last_name"],
                HolonType = (HolonType)Enum.Parse(typeof(HolonType), keyValuePair["holon_type"]),

                //TODO: Finish mapping
            };

            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS] = keyValuePair["provider_unique_storage_key"];
            return avatar;
        }

        private IHcAvatarDetail ConvertAvatarDetailToHoloOASISAvatarDetail(IAvatarDetail avatarDetail, IHcAvatarDetail hcAvatarDetail)
        {
            hcAvatarDetail.Id = avatarDetail.Id;
            hcAvatarDetail.Username = avatarDetail.Username;
            hcAvatarDetail.Email = avatarDetail.Email;
            //TODO: Finish implementing...
    
            return hcAvatarDetail;
        }

        private IAvatarDetail ConvertHcAvatarDetailToAvatarDetail(IHcAvatarDetail hcAvatarDetail)
        {
            AvatarDetail avatarDetail = new AvatarDetail
            {
                Id = hcAvatarDetail.Id,
                Email = hcAvatarDetail.Email,
                Username = hcAvatarDetail.Username,
                HolonType = hcAvatarDetail.HolonType,
                
                //TODO: Finish mapping
            };

            avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS] = hcAvatarDetail.ProviderUniqueStorageKey;
            return avatarDetail;
        }

        private IAvatarDetail ConvertKeyValuePairToAvatarDetail(Dictionary<string, string> keyValuePair)
        {
            AvatarDetail avatarDetail = new AvatarDetail
            {
                Id = new Guid(keyValuePair["id"]),
                Email = keyValuePair["email"],
                Username = keyValuePair["username"],
                HolonType = (HolonType)Enum.Parse(typeof(HolonType), keyValuePair["holon_type"])

                //TODO: Finish mapping
            };

            avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS] = keyValuePair["provider_unique_storage_key"];
            return avatarDetail;
        }

        private dynamic ConvertAvatarToParamsObject(IAvatar avatar)
        {
            return new
            {
                id = avatar.Id.ToString(),
                username = avatar.Username,
                password = avatar.Password,
                email = avatar.Email,
                title = avatar.Title,
                first_name = avatar.FirstName,
                last_name = avatar.LastName,
                provider_unique_storage_key = avatar.ProviderUniqueStorageKey,
                holon_type = avatar.HolonType

                //TODDO: Finish mapping rest of the properties.
            };
        }

        private dynamic ConvertAvatarDetailToParamsObject(IAvatarDetail avatar)
        {
            return new
            {
                id = avatar.Id.ToString(),
                username = avatar.Username,
                email = avatar.Email,
                provider_unique_storage_key = avatar.ProviderUniqueStorageKey,
                holon_type = avatar.HolonType

                //TODDO: Finish mapping rest of the properties.
            };
        }

        /*
        private async Task<OASISResult<T>> ExecuteOperationAsync<T>(HcOperationEnum operation, string objectName, string fieldName, string fieldValue, string zomeFunctionName = "", int version = 0) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatar != null)
                {
                    //if (T.Id == Guid.Empty)
                    //    T.Id = Guid.NewGuid();

                    //hcAvatar = (HcAvatar)ConvertAvatarToHoloOASISAvatar(T, hcAvatar);

                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeFunctionName))
                    {
                        switch (operation)
                        {
                            case HcOperationEnum.Create:
                                {
                                    hcAvatar.ZomeCreateEntryFunction = zomeFunctionName;
                                    response = await hcAvatar.SaveAsync(response);
                                }
                                break;

                            case HcOperationEnum.Read:
                                {
                                    hcAvatar.ZomeLoadEntryFunction = zomeFunctionName;
                                    response = await hcAvatar.LoadAsync(fieldValue);
                                }
                                break;

                            case HcOperationEnum.Update:
                                {
                                    hcAvatar.ZomeUpdateEntryFunction = zomeFunctionName;
                                    response = await hcAvatar.SaveAsync(fieldValue);
                                }
                                break;

                            case HcOperationEnum.Delete:
                                {
                                    hcAvatar.ZomeDeleteEntryFunction = zomeFunctionName;
                                    response = await hcAvatar.DeleteAsync(fieldValue);
                                }
                                break;
                        }
                    }

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject; 
                        else
                            ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private OASISResult<T> ExecuteOperation<T>(HcOperationEnum operation, string objectName, string fieldName, string fieldValue, string zomeFunctionName = "", int version = 0) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatar != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeFunctionName))
                    {
                        switch (operation)
                        {
                            case HcOperationEnum.Create:
                                {
                                    hcAvatar.ZomeCreateEntryFunction = zomeFunctionName;
                                    response = hcAvatar.Save(fieldValue);
                                }
                                break;

                            case HcOperationEnum.Read:
                                {
                                    hcAvatar.ZomeLoadEntryFunction = zomeFunctionName;
                                    response = hcAvatar.Load(fieldValue);
                                }
                                break;

                            case HcOperationEnum.Update:
                                {
                                    hcAvatar.ZomeUpdateEntryFunction = zomeFunctionName;
                                    response = hcAvatar.Save(fieldValue);
                                }
                                break;

                            case HcOperationEnum.Delete:
                                {
                                    hcAvatar.ZomeDeleteEntryFunction = zomeFunctionName;
                                    response = hcAvatar.Delete(fieldValue);
                                }
                                break;
                        }
                    }

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }*/


        private async Task<OASISResult<IEnumerable<T>>> LoadCollectionAsync<T>(string collectionName, string collectionAnchor, string zomeFunctionName = "", int version = 0, dynamic additionalParams = null) where T : IHolonBase
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            //TODO: Need to implement loading collections in HoloNET ASAP! :)

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatar != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeFunctionName))
                        hcAvatar.ZomeLoadCollectionFunction = zomeFunctionName;

                    result = HandleLoadCollectionResponse(await hcAvatar.LoadCollectionAsync(collectionAnchor, version, additionalParams), collectionName, collectionAnchor, result);
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the LoadCollectionAsync method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> LoadCollection<T>(string collectionName, string collectionAnchor, string zomeFunctionName = "", int version = 0, dynamic additionalParams = null) where T : IHolonBase
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            //TODO: Need to implement loading collections in HoloNET ASAP! :)

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatar != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeFunctionName))
                        hcAvatar.ZomeLoadCollectionFunction = zomeFunctionName;

                    result = HandleLoadCollectionResponse(hcAvatar.LoadCollection(collectionAnchor, version, additionalParams), collectionName, collectionAnchor, result);
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the LoadCollectionAsync method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> ExecuteOperationAsync<T>(CollectionOperationEnum operation, string collectionName, string collectionAnchor, string zomeFunctionName = "", int version = 0) where T : IHolonBase
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            //TODO: Need to implement loading collections in HoloNET ASAP! :)

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatar != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeFunctionName))
                    {
                        switch (operation)
                        {
                            //case CollectionOperationEnum.CreateCollection:
                            //    {
                            //        hcAvatar.ZomeCreateEntryCollectionFunction = zomeFunctionName;
                            //        response = await hcAvatar.SaveCollectionAsync(collectionAnchor);
                            //    }
                            //    break;

                            case CollectionOperationEnum.ReadCollection:
                                {
                                    hcAvatar.ZomeLoadCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.LoadCollectionAsync(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.AddToCollection:
                                {
                                    hcAvatar.ZomeAddToCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.AddToCollectionAsync(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.UpdateCollection:
                                {
                                    hcAvatar.ZomeUpdateCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.UpdateCollectionAsync(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.RemoveFromCollection:
                                {
                                    hcAvatar.ZomeRemoveFromCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.RemoveFromCollectionAsync(collectionAnchor);
                                }
                                break;

                                //case CollectionOperationEnum.DeleteCollection:
                                //    {
                                //        hcAvatar.ZomeDeleteEntryCollectionFunction = zomeFunctionName;
                                //        response = await hcAvatar.DeleteCollectionAsync(collectionAnchor);
                                //    }
                                //    break;
                        }
                    }

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperationAsync method in the HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperationAsync method in the HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperationAsync method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> ExecuteOperation<T>(CollectionOperationEnum operation, string collectionName, string collectionAnchor, string zomeFunctionName = "", int version = 0) where T : IHolonBase
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            //TODO: Need to implement loading collections in HoloNET ASAP! :)

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatar != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeFunctionName))
                    {
                        switch (operation)
                        {
                            //case CollectionOperationEnum.CreateCollection:
                            //    {
                            //        hcAvatar.ZomeCreateEntryCollectionFunction = zomeFunctionName;
                            //        response = await hcAvatar.SaveCollectionAsync(collectionAnchor);
                            //    }
                            //    break;

                            case CollectionOperationEnum.ReadCollection:
                                {
                                    hcAvatar.ZomeLoadCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.LoadCollectionAsync(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.AddToCollection:
                                {
                                    hcAvatar.ZomeAddToCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.AddToCollectionAsync(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.UpdateCollection:
                                {
                                    hcAvatar.ZomeUpdateCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.UpdateCollectionAsync(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.RemoveFromCollection:
                                {
                                    hcAvatar.ZomeRemoveFromCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.RemoveFromCollectionAsync(collectionAnchor);
                                }
                                break;

                                //case CollectionOperationEnum.DeleteCollection:
                                //    {
                                //        hcAvatar.ZomeDeleteEntryCollectionFunction = zomeFunctionName;
                                //        response = await hcAvatar.DeleteCollectionAsync(collectionAnchor);
                                //    }
                                //    break;
                        }
                    }

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperation method in the HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperation method in the HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperation method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private async Task<OASISResult<T>> LoadAsync<T>(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeLoadFunctionName = "", int version = 0, object additionalParams = null) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                IHcObject hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClient);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClient);
                        break;

                    //case HcObjectTypeEnum.Holon:
                }

                if (hcObject != null)
                {
                    if (!string.IsNullOrEmpty(zomeLoadFunctionName))
                        hcObject.ZomeLoadEntryFunction = zomeLoadFunctionName;

                    //result = HandleLoadResponse(hcObject.Load(fieldValue, _useReflection, version, additionalParams), hcObjectType, fieldName, fieldValue, hcObject, result); //TODO: Once HoloNET has been upgraded to support these params we can uncomment this line and remove the one below...
                    result = HandleLoadResponse(await hcObject.LoadAsync(fieldValue, _useReflection), hcObjectType, fieldName, fieldValue, hcObject, result);
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with {fieldValue} in the LoadAsync method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private OASISResult<T> Load<T>(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeLoadFunctionName = "", int version = 0, object additionalParams = null) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                IHcObject hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClient);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClient);
                        break;

                        //case HcObjectTypeEnum.Holon:
                }

                if (hcObject != null)
                {
                    if (!string.IsNullOrEmpty(zomeLoadFunctionName))
                        hcObject.ZomeLoadEntryFunction = zomeLoadFunctionName;

                    //result = HandleLoadResponse(hcObject.Load(fieldValue, _useReflection, version, additionalParams), hcObjectType, fieldName, fieldValue, hcObject, result); //TODO: Once HoloNET has been upgraded to support these params we can uncomment this line and remove the one below...
                    result = HandleLoadResponse(hcObject.Load(fieldValue, _useReflection), hcObjectType, fieldName, fieldValue, hcObject, result);
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with {fieldValue} in the Load method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private async Task<OASISResult<T>> SaveAsync<T>(HcObjectTypeEnum hcObjectType, T holon) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                IHcObject hcObject = null;
                ZomeFunctionCallBackEventArgs response = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClient);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClient);
                        break;
                }

                if (holon.Id == Guid.Empty)
                    holon.Id = Guid.NewGuid();

                //If it is configured to not use Reflection then we would do it like this passing in our own params object.
                if (!_useReflection)
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            response = await hcObject.SaveAsync(ConvertAvatarToParamsObject((IAvatar)holon));
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            response = await hcObject.SaveAsync(ConvertAvatarDetailToParamsObject((IAvatarDetail)holon));
                            break;
                    }
                }
                else
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            hcObject = ConvertAvatarToHoloOASISAvatar((IAvatar)holon, (IHcAvatar)hcObject);
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            hcObject = ConvertAvatarDetailToHoloOASISAvatarDetail((IAvatarDetail)holon, (IHcAvatarDetail)hcObject);
                            break;
                    }

                    //Otherwise we could just use this dyanmic version (which uses reflection) to dyamically build the params object (but we need to make sure properties have the HolochainFieldName attribute).
                    response = await hcObject.SaveAsync();
                }

                if (response != null)
                    result = HandleSaveResponse(response, hcObjectType, holon, hcObject, result);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the SaveAsync method in HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        private OASISResult<T> Save<T>(HcObjectTypeEnum hcObjectType, T holon) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                IHcObject hcObject = null;
                ZomeFunctionCallBackEventArgs response = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClient);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClient);
                        break;
                }

                if (holon.Id == Guid.Empty)
                    holon.Id = Guid.NewGuid();

                //If it is configured to not use Reflection then we would do it like this passing in our own params object.
                if (!_useReflection)
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            response = hcObject.Save(ConvertAvatarToParamsObject((IAvatar)holon));
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            response = hcObject.Save(ConvertAvatarDetailToParamsObject((IAvatarDetail)holon));
                            break;
                    }
                }
                else
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            hcObject = ConvertAvatarToHoloOASISAvatar((IAvatar)holon, (IHcAvatar)hcObject);
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            hcObject = ConvertAvatarDetailToHoloOASISAvatarDetail((IAvatarDetail)holon, (IHcAvatarDetail)hcObject);
                            break;
                    }

                    //Otherwise we could just use this dyanmic version (which uses reflection) to dyamically build the params object (but we need to make sure properties have the HolochainFieldName attribute).
                    response = hcObject.Save();
                }

                if (response != null)
                    result = HandleSaveResponse(response, hcObjectType, holon, hcObject, result);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the Save method in HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        private async Task<OASISResult<bool>> DeleteAsync(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeDeleteFunctionName = "", bool softDelete = true, object additionalParams = null)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (!string.IsNullOrEmpty(zomeDeleteFunctionName))
                    hcAvatar.ZomeDeleteEntryFunction = ZOME_DELETE_AVATAR_BY_ID_FUNCTION;

                //hcAvatar.Delete(fieldValue, softDelete, additionalParams); //TODO: Once HoloNET has been upgraded to support these params we can uncomment this line and remove the one below...
                await hcAvatar.DeleteAsync(fieldValue);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the DeleteAsync method in HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        private OASISResult<bool> Delete(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeDeleteFunctionName = "", bool softDelete = true, object additionalParams = null)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (!string.IsNullOrEmpty(zomeDeleteFunctionName))
                    hcAvatar.ZomeDeleteEntryFunction = ZOME_DELETE_AVATAR_BY_ID_FUNCTION;

                //hcAvatar.Delete(fieldValue, softDelete, additionalParams); //TODO: Once HoloNET has been upgraded to support these params we can uncomment this line and remove the one below...
                hcAvatar.Delete(fieldValue);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the Delete method in HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        private OASISResult<T> HandleLoadResponse<T>(ZomeFunctionCallBackEventArgs response, HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, IHcObject hcObject, OASISResult<T> result) where T : IHolonBase
        {
            if (response != null)
            {
                if (response.IsCallSuccessful && !response.IsError)
                {
                    if (_useReflection)
                    {
                        switch (hcObjectType)
                        {
                            case HcObjectTypeEnum.Avatar:
                                result.Result = (T)ConvertHcAvatarToAvatar((IHcAvatar)hcObject);
                                break;

                            case HcObjectTypeEnum.AvatarDetail:
                                result.Result = (T)ConvertHcAvatarDetailToAvatarDetail((IHcAvatarDetail)hcObject);
                                break;
                        }
                    }
                    else
                    {
                        // Not using reflection for the HoloOASIS use case may be more efficient because it uses very slightly less code and has small performance improvement.
                        // However, using relection would suit other use cases better (would use a lot less code because HoloNET would manage all the mappings (from the Holochain Conductor KeyValue pair data response) for you) such as where object mapping to external objects (like the OASIS) is not required. Please see HoloNET Test Harness for more examples of this...
                        switch (hcObjectType)
                        {
                            case HcObjectTypeEnum.Avatar:
                                result.Result = (T)ConvertKeyValuePairToAvatar(response.KeyValuePair);
                                break;

                            case HcObjectTypeEnum.AvatarDetail:
                                result.Result = (T)ConvertKeyValuePairToAvatarDetail(response.KeyValuePair);
                                break;
                        }
                    }
                }
                else
                    ErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the HandleLoadResponse method in the HoloOASIS Provider. Reason: { response.Message }");
            }
            else
                ErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the HandleLoadResponse method in the HoloOASIS Provider. Reason: Unknown.");

            return result;
        }

        private OASISResult<T> HandleSaveResponse<T>(ZomeFunctionCallBackEventArgs response, HcObjectTypeEnum hcObjectType, IHolonBase holon, IHcObject hcObject, OASISResult<T> result) where T : IHolonBase
        {
            if (response != null)
            {
                if (response.IsCallSuccessful && !response.IsError)
                {
                    if (_useReflection)
                    {
                        switch (hcObjectType)
                        {
                            case HcObjectTypeEnum.Avatar:
                                result.Result = (T)ConvertHcAvatarToAvatar((IHcAvatar)hcObject);
                                break;

                            case HcObjectTypeEnum.AvatarDetail:
                                result.Result = (T)ConvertHcAvatarDetailToAvatarDetail((IHcAvatarDetail)hcObject);
                                break;
                        }
                    }
                    else
                    {
                        // Not using reflection for the HoloOASIS use case may be more efficient because it uses very slightly less code and has small performance improvement.
                        // However, using relection would suit other use cases better (would use a lot less code because HoloNET would manage all the mappings (from the Holochain Conductor KeyValue pair data response) for you) such as where object mapping to external objects (like the OASIS) is not required. Please see HoloNET Test Harness for more examples of this...
                        switch (hcObjectType)
                        {
                            case HcObjectTypeEnum.Avatar:
                                result.Result = (T)ConvertKeyValuePairToAvatar(response.KeyValuePair);
                                break;

                            case HcObjectTypeEnum.AvatarDetail:
                                result.Result = (T)ConvertKeyValuePairToAvatarDetail(response.KeyValuePair);
                                break;
                        }
                    }
                }
                else
                    ErrorHandling.HandleError(ref result, $"Error saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the HandleSaveResponse method in the HoloOASIS Provider. Reason: { response.Message }");
            }
            else
                ErrorHandling.HandleError(ref result, $"Error saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the HandleSaveResponse method in the HoloOASIS Provider. Reason: Unknown.");

            return result;
        }

        private OASISResult<T> HandleLoadCollectionResponse<T>(ZomeFunctionCallBackEventArgs response, string collectionName, string collectionAnchor, OASISResult<T> result) where T : IHolonBase
        {
            if (response != null)
            {
                if (response.IsCallSuccessful && !response.IsError)
                    result.Result = response.Entry.EntryDataObject;
                else
                    ErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the LoadCollectionAsync method in the HoloOASIS Provider. Reason: { response.Message }");
            }
            else
                ErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the LoadCollectionAsync method in the HoloOASIS Provider. Reason: Unknown.");

            return result;
        }


        /// <summary>
        /// Handles any errors thrown by HoloNET or HoloOASIS. It fires the OnHoloOASISError error handler if there are any 
        /// subscriptions. The same applies to the OnStorageProviderError event implemented as part of the IOASISStorageProvider interface.
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="errorDetails"></param>
        /// <param name="holoNETEventArgs"></param>
        private void HandleError(string reason, Exception errorDetails, HoloNETErrorEventArgs holoNETEventArgs)
        {
            //OnStorageProviderError?.Invoke(this, new AvatarManagerErrorEventArgs { EndPoint = this.HoloNETClient.EndPoint, Reason = string.Concat(reason, holoNETEventArgs != null ? string.Concat(" - HoloNET Error: ", holoNETEventArgs.Reason, " - ", holoNETEventArgs.ErrorDetails.ToString()) : ""), ErrorDetails = errorDetails });
            OnStorageProviderError(HoloNETClient.EndPoint, string.Concat(reason, holoNETEventArgs != null ? string.Concat(" - HoloNET Error: ", holoNETEventArgs.Reason, " - ", holoNETEventArgs.ErrorDetails.ToString()) : ""), errorDetails);
            OnHoloOASISError?.Invoke(this, new HoloOASISErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = reason, ErrorDetails = errorDetails, HoloNETErrorDetails = holoNETEventArgs });
        }

        #endregion
    }
}
