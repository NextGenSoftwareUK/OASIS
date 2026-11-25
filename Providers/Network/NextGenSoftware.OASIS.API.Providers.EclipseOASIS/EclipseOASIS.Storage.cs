// Generated: every storage member forwards to the inner provider (see class summary).
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EclipseOASIS
{
    public partial class EclipseOASIS
    {
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0) => _solana.LoadAllAvatarsAsync(version);
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => _solana.LoadAllAvatars(version);
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0) => _solana.LoadAvatarAsync(Id, version);
        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0) => _solana.LoadAvatar(Id, version);
        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0) => _solana.LoadAvatarByProviderKeyAsync(providerKey, version);
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => _solana.LoadAvatarByProviderKey(providerKey, version);
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0) => _solana.LoadAvatarByUsernameAsync(avatarUsername, version);
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => _solana.LoadAvatarByUsername(avatarUsername, version);
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0) => _solana.LoadAvatarByEmailAsync(avatarEmail, version);
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => _solana.LoadAvatarByEmail(avatarEmail, version);
        public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string verificationToken, int version = 0) => _solana.LoadAvatarByVerificationToken(verificationToken, version);
        public override Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string verificationToken, int version = 0) => _solana.LoadAvatarByVerificationTokenAsync(verificationToken, version);
        public override OASISResult<IAvatar> LoadAvatarByResetToken(string resetToken, int version = 0) => _solana.LoadAvatarByResetToken(resetToken, version);
        public override Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string resetToken, int version = 0) => _solana.LoadAvatarByResetTokenAsync(resetToken, version);
        public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string refreshToken, int version = 0) => _solana.LoadAvatarByRefreshToken(refreshToken, version);
        public override Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string refreshToken, int version = 0) => _solana.LoadAvatarByRefreshTokenAsync(refreshToken, version);
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) => _solana.LoadAvatarDetailAsync(id, version);
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => _solana.LoadAvatarDetail(id, version);
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0) => _solana.LoadAvatarDetailByEmailAsync(avatarEmail, version);
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => _solana.LoadAvatarDetailByEmail(avatarEmail, version);
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0) => _solana.LoadAvatarDetailByUsernameAsync(avatarUsername, version);
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => _solana.LoadAvatarDetailByUsername(avatarUsername, version);
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) => _solana.LoadAllAvatarDetailsAsync(version);
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => _solana.LoadAllAvatarDetails(version);
        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar) => _solana.SaveAvatarAsync(Avatar);
        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar) => _solana.SaveAvatar(Avatar);
        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar) => _solana.SaveAvatarDetailAsync(Avatar);
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar) => _solana.SaveAvatarDetail(Avatar);
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) => _solana.DeleteAvatarAsync(id, softDelete);
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => _solana.DeleteAvatar(id, softDelete);
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) => _solana.DeleteAvatarAsync(providerKey, softDelete);
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => _solana.DeleteAvatar(providerKey, softDelete);
        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true) => _solana.DeleteAvatarByEmailAsync(avatarEmail, softDelete);
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => _solana.DeleteAvatarByEmail(avatarEmail, softDelete);
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true) => _solana.DeleteAvatarByUsernameAsync(avatarUsername, softDelete);
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => _solana.DeleteAvatarByUsername(avatarUsername, softDelete);
        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => _solana.SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version);
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => _solana.Search(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version);
        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolonsForParent(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolonsForParent(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolonsByMetaData(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadHolonsByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => _solana.LoadAllHolons(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => _solana.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => _solana.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => _solana.SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider);
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => _solana.SaveHolons(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider);
        public override Task<NextGenSoftware.OASIS.API.Core.Holons.HolonPurgeSubject> GetPurgeSubjectAsync(Guid holonId, System.Threading.CancellationToken cancellationToken) => _solana.GetPurgeSubjectAsync(holonId, cancellationToken);
        public override Task<NextGenSoftware.OASIS.API.Core.Holons.HolonPurgeStoreOutcome> PurgeHolonAsync(Guid holonId, System.Threading.CancellationToken cancellationToken) => _solana.PurgeHolonAsync(holonId, cancellationToken);
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => _solana.DeleteHolonAsync(id);
        public override OASISResult<IHolon> DeleteHolon(Guid id) => _solana.DeleteHolon(id);
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) => _solana.DeleteHolonAsync(providerKey);
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => _solana.DeleteHolon(providerKey);
        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) => _solana.ImportAsync(holons);
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => _solana.Import(holons);
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) => _solana.ExportAllDataForAvatarByIdAsync(avatarId, version);
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => _solana.ExportAllDataForAvatarById(avatarId, version);
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) => _solana.ExportAllDataForAvatarByUsernameAsync(avatarUsername, version);
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => _solana.ExportAllDataForAvatarByUsername(avatarUsername, version);
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) => _solana.ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version);
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => _solana.ExportAllDataForAvatarByEmail(avatarEmailAddress, version);
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => _solana.ExportAllAsync(version);
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => _solana.ExportAll(version);
        // Activation state mirrors the inner provider's outcome.
        public override OASISResult<bool> ActivateProvider() => Mirror(_solana.ActivateProvider());
        public override async Task<OASISResult<bool>> ActivateProviderAsync() => Mirror(await _solana.ActivateProviderAsync());
        public override OASISResult<bool> DeActivateProvider() { var r = _solana.DeActivateProvider(); IsProviderActivated = false; return r; }
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { var r = await _solana.DeActivateProviderAsync(); IsProviderActivated = false; return r; }
        private OASISResult<bool> Mirror(OASISResult<bool> result) { IsProviderActivated = !result.IsError && result.Result; return result; }
    }
}
