using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ICOSMICManagerBase : IOASISManager
    {
        Task<OASISResult<T>> SaveHolonAsync<T>(IHolon holon, Guid avatarId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.SaveHolonAsync", bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon, new();
        OASISResult<T> SaveHolon<T>(IHolon holon, Guid avatarId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.SaveHolon", bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon, new();
        Task<OASISResult<T>> LoadHolonAsync<T>(Guid holonId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadHolonAsync", bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new();
        OASISResult<T> LoadHolon<T>(Guid holonId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadHolon", bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new();
        Task<OASISResult<IEnumerable<T>>> LoadAllHolonsAsync<T>(ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsAsync", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new();
        OASISResult<IEnumerable<T>> LoadAllHolons<T>(ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsForAvatar", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new();
        Task<OASISResult<IEnumerable<T>>> SearchHolonsAsync<T>(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsAsync", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new();
        OASISResult<IEnumerable<T>> SearchHolons<T>(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsAsync", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new();
        Task<OASISResult<IEnumerable<T>>> LoadAllHolonsForAvatarAsync<T>(Guid avatarId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsForAvatarAsync", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new();
        OASISResult<IEnumerable<T>> LoadAllHolonsForAvatar<T>(Guid avatarId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsForAvatar", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new();
        Task<OASISResult<T>> DeleteHolonAsync<T>(Guid holonId, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.DeleteHolonAsync") where T : IHolon, new();
        OASISResult<T> DeleteHolon<T>(Guid holonId, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.DeleteHolon") where T : IHolon, new();
    }
}
