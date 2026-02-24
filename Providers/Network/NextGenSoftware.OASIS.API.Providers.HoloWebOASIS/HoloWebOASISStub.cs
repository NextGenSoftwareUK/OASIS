// Stub implementation so provider and test projects build. Full implementation is commented in HoloWebOASIS.cs.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.HoloWebOASIS
{
    public class HoloWebOASIS : OASISStorageProviderBase
    {
        public HoloWebOASIS()
        {
            ProviderName = "HoloWebOASIS";
            ProviderDescription = "HoloWeb Provider (stub)";
            ProviderType = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.HoloWebOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(NextGenSoftware.OASIS.API.Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        private static OASISResult<T> NotImpl<T>() => new OASISResult<T> { IsError = true, Message = "Not implemented" };
        private static OASISResult<IEnumerable<IAvatar>> NotImplAvatars() => new OASISResult<IEnumerable<IAvatar>> { IsError = true, Message = "Not implemented", Result = new List<IAvatar>() };
        private static OASISResult<IEnumerable<IAvatarDetail>> NotImplAvatarDetails() => new OASISResult<IEnumerable<IAvatarDetail>> { IsError = true, Message = "Not implemented", Result = new List<IAvatarDetail>() };
        private static OASISResult<IEnumerable<IHolon>> NotImplHolons() => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Not implemented", Result = new List<IHolon>() };

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0) => Task.FromResult(NotImplAvatars());
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => NotImplAvatars();
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0) => Task.FromResult(NotImpl<IAvatar>());
        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0) => NotImpl<IAvatar>();
        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0) => Task.FromResult(NotImpl<IAvatar>());
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => NotImpl<IAvatar>();
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0) => Task.FromResult(NotImpl<IAvatar>());
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => NotImpl<IAvatar>();
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0) => Task.FromResult(NotImpl<IAvatar>());
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => NotImpl<IAvatar>();
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) => Task.FromResult(NotImpl<IAvatarDetail>());
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => NotImpl<IAvatarDetail>();
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0) => Task.FromResult(NotImpl<IAvatarDetail>());
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => NotImpl<IAvatarDetail>();
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0) => Task.FromResult(NotImpl<IAvatarDetail>());
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => NotImpl<IAvatarDetail>();
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) => Task.FromResult(NotImplAvatarDetails());
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => NotImplAvatarDetails();
        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar) => Task.FromResult(NotImpl<IAvatar>());
        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar) => NotImpl<IAvatar>();
        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar) => Task.FromResult(NotImpl<IAvatarDetail>());
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar) => NotImpl<IAvatarDetail>();
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) => Task.FromResult(NotImpl<bool>());
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => NotImpl<bool>();
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) => Task.FromResult(NotImpl<bool>());
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => NotImpl<bool>();
        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true) => Task.FromResult(NotImpl<bool>());
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => NotImpl<bool>();
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true) => Task.FromResult(NotImpl<bool>());
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => NotImpl<bool>();
        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => Task.FromResult(NotImpl<ISearchResults>());
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => NotImpl<ISearchResults>();
        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotImpl<IHolon>());
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImpl<IHolon>();
        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotImpl<IHolon>());
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImpl<IHolon>();
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotImplHolons());
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplHolons();
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotImplHolons());
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplHolons();
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotImplHolons());
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplHolons();
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotImplHolons());
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplHolons();
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotImplHolons());
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplHolons();
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => Task.FromResult(NotImpl<IHolon>());
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotImpl<IHolon>();
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => Task.FromResult(NotImplHolons());
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotImplHolons();
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => Task.FromResult(NotImpl<IHolon>());
        public override OASISResult<IHolon> DeleteHolon(Guid id) => NotImpl<IHolon>();
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) => Task.FromResult(NotImpl<IHolon>());
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => NotImpl<IHolon>();
        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) => Task.FromResult(NotImpl<bool>());
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => NotImpl<bool>();
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) => Task.FromResult(NotImplHolons());
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => NotImplHolons();
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) => Task.FromResult(NotImplHolons());
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => NotImplHolons();
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) => Task.FromResult(NotImplHolons());
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => NotImplHolons();
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => Task.FromResult(NotImplHolons());
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => NotImplHolons();
    }
}
