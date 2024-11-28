using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

public sealed partial class RadixOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    public bool IsVersionControlEnabled { get; set; }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IHolon> DeleteHolon(Guid id, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IHolon> DeleteHolon(string providerKey, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey, bool softDelete = true)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        throw new NotImplementedException();
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
    {
        throw new System.NotImplementedException();
    }

    public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
    {
        throw new System.NotImplementedException();
    }

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
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

    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

    public bool NativeCodeGenesis(ICelestialBody celestialBody)
    {
        throw new System.NotImplementedException();
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

    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        throw new NotImplementedException();
    }

    public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
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

    public OASISResult<ITransactionRespone> SendTransaction(IWalletTransactionRequest transaction)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionAsync(IWalletTransactionRequest transaction)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        throw new NotImplementedException();
    }
}