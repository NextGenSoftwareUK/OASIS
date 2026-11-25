using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EclipseOASIS
{
    /// <summary>
    /// OASIS provider for Eclipse — a Solana VM (SVM) L2 settled on Ethereum with Celestia DA.
    /// Eclipse executes the Solana VM and exposes the Solana JSON-RPC API, so its storage is the
    /// Solana storage implementation (signed memo-program records written by the configured
    /// account) pointed at an Eclipse RPC endpoint. Every storage member delegates to the composed
    /// <see cref="SolanaOASIS"/> instance; there is no Eclipse-specific storage format.
    /// </summary>
    public class EclipseOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        public const string DefaultRpcUrl = "https://mainnetbeta-rpc.eclipse.xyz";
        private const string SignerRequired =
            "EclipseOASIS storage requires a signing account: construct it with (rpcUrl, privateKey, publicKey).";

        private readonly SolanaOASIS? _svmStorage;

        /// <summary>The SVM storage implementation every storage member delegates to; null when no signer is configured.</summary>
        public SolanaOASIS? SvmStorage => _svmStorage;

        public string RpcUrl { get; }

        /// <summary>Identifies the provider for an endpoint without a signer. Storage operations return an explicit error.</summary>
        public EclipseOASIS(string rpcUrl = DefaultRpcUrl) : this(rpcUrl, null, null) { }

        /// <summary>Eclipse storage against <paramref name="rpcUrl"/>, signed by the given base58 Solana-format keypair.</summary>
        public EclipseOASIS(string rpcUrl, string? privateKey, string? publicKey)
        {
            RpcUrl = new Uri(rpcUrl, UriKind.Absolute).ToString();
            ProviderName = "EclipseOASIS";
            ProviderDescription = "Eclipse SVM L2 Provider — Solana VM on Ethereum settlement with Celestia DA";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EclipseOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            if (string.IsNullOrWhiteSpace(privateKey) != string.IsNullOrWhiteSpace(publicKey))
                throw new ArgumentException("Supply both the private and the public key of the Eclipse signer, or neither.");
            if (!string.IsNullOrWhiteSpace(privateKey))
                _svmStorage = new SolanaOASIS(RpcUrl, privateKey, publicKey);
        }

        private static OASISResult<T> NoSigner<T>()
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, SignerRequired);
            return result;
        }

        private OASISResult<T> Svm<T>(Func<SolanaOASIS, OASISResult<T>> operation)
            => _svmStorage == null ? NoSigner<T>() : operation(_svmStorage);

        private Task<OASISResult<T>> SvmAsync<T>(Func<SolanaOASIS, Task<OASISResult<T>>> operation)
            => _svmStorage == null ? Task.FromResult(NoSigner<T>()) : operation(_svmStorage);

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            if (_svmStorage == null) return NoSigner<bool>();
            var result = _svmStorage.IsProviderActivated ? new OASISResult<bool>(true) : await _svmStorage.ActivateProviderAsync();
            IsProviderActivated = !result.IsError && result.Result;
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            if (_svmStorage == null) return NoSigner<bool>();
            var result = _svmStorage.IsProviderActivated ? new OASISResult<bool>(true) : _svmStorage.ActivateProvider();
            IsProviderActivated = !result.IsError && result.Result;
            return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var result = _svmStorage != null && _svmStorage.IsProviderActivated
                ? await _svmStorage.DeActivateProviderAsync() : new OASISResult<bool>(true);
            if (!result.IsError) IsProviderActivated = false;
            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            var result = _svmStorage != null && _svmStorage.IsProviderActivated
                ? _svmStorage.DeActivateProvider() : new OASISResult<bool>(true);
            if (!result.IsError) IsProviderActivated = false;
            return result;
        }

        // Avatars
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0) => SvmAsync(s => s.LoadAllAvatarsAsync(version));
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => Svm(s => s.LoadAllAvatars(version));
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0) => SvmAsync(s => s.LoadAvatarAsync(id, version));
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => Svm(s => s.LoadAvatar(id, version));
        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0) => SvmAsync(s => s.LoadAvatarByProviderKeyAsync(providerKey, version));
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => Svm(s => s.LoadAvatarByProviderKey(providerKey, version));
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0) => SvmAsync(s => s.LoadAvatarByUsernameAsync(avatarUsername, version));
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => Svm(s => s.LoadAvatarByUsername(avatarUsername, version));
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0) => SvmAsync(s => s.LoadAvatarByEmailAsync(avatarEmail, version));
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => Svm(s => s.LoadAvatarByEmail(avatarEmail, version));
        public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string verificationToken, int version = 0) => Svm(s => s.LoadAvatarByVerificationToken(verificationToken, version));
        public override Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string verificationToken, int version = 0) => SvmAsync(s => s.LoadAvatarByVerificationTokenAsync(verificationToken, version));
        public override OASISResult<IAvatar> LoadAvatarByResetToken(string resetToken, int version = 0) => Svm(s => s.LoadAvatarByResetToken(resetToken, version));
        public override Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string resetToken, int version = 0) => SvmAsync(s => s.LoadAvatarByResetTokenAsync(resetToken, version));
        public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string refreshToken, int version = 0) => Svm(s => s.LoadAvatarByRefreshToken(refreshToken, version));
        public override Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string refreshToken, int version = 0) => SvmAsync(s => s.LoadAvatarByRefreshTokenAsync(refreshToken, version));
        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar) => SvmAsync(s => s.SaveAvatarAsync(avatar));
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => Svm(s => s.SaveAvatar(avatar));
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) => SvmAsync(s => s.DeleteAvatarAsync(id, softDelete));
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => Svm(s => s.DeleteAvatar(id, softDelete));
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) => SvmAsync(s => s.DeleteAvatarAsync(providerKey, softDelete));
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => Svm(s => s.DeleteAvatar(providerKey, softDelete));
        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true) => SvmAsync(s => s.DeleteAvatarByEmailAsync(avatarEmail, softDelete));
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => Svm(s => s.DeleteAvatarByEmail(avatarEmail, softDelete));
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true) => SvmAsync(s => s.DeleteAvatarByUsernameAsync(avatarUsername, softDelete));
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => Svm(s => s.DeleteAvatarByUsername(avatarUsername, softDelete));

        // Avatar details
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) => SvmAsync(s => s.LoadAvatarDetailAsync(id, version));
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => Svm(s => s.LoadAvatarDetail(id, version));
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0) => SvmAsync(s => s.LoadAvatarDetailByEmailAsync(avatarEmail, version));
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => Svm(s => s.LoadAvatarDetailByEmail(avatarEmail, version));
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0) => SvmAsync(s => s.LoadAvatarDetailByUsernameAsync(avatarUsername, version));
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => Svm(s => s.LoadAvatarDetailByUsername(avatarUsername, version));
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) => SvmAsync(s => s.LoadAllAvatarDetailsAsync(version));
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => Svm(s => s.LoadAllAvatarDetails(version));
        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail) => SvmAsync(s => s.SaveAvatarDetailAsync(avatarDetail));
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => Svm(s => s.SaveAvatarDetail(avatarDetail));

        // Holons
        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => SvmAsync(s => s.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Svm(s => s.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => SvmAsync(s => s.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Svm(s => s.LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => SvmAsync(s => s.LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Svm(s => s.LoadHolonsForParent(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => SvmAsync(s => s.LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Svm(s => s.LoadHolonsForParent(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => SvmAsync(s => s.LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Svm(s => s.LoadHolonsByMetaData(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => SvmAsync(s => s.LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Svm(s => s.LoadHolonsByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => SvmAsync(s => s.LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Svm(s => s.LoadAllHolons(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version));
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SvmAsync(s => s.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider));
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => Svm(s => s.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider));
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SvmAsync(s => s.SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider));
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => Svm(s => s.SaveHolons(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider));
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => SvmAsync(s => s.DeleteHolonAsync(id));
        public override OASISResult<IHolon> DeleteHolon(Guid id) => Svm(s => s.DeleteHolon(id));
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) => SvmAsync(s => s.DeleteHolonAsync(providerKey));
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => Svm(s => s.DeleteHolon(providerKey));

        // Search, import and export
        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SvmAsync(s => s.SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version));
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => Svm(s => s.Search(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version));
        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) => SvmAsync(s => s.ImportAsync(holons));
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => Svm(s => s.Import(holons));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) => SvmAsync(s => s.ExportAllDataForAvatarByIdAsync(avatarId, version));
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => Svm(s => s.ExportAllDataForAvatarById(avatarId, version));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) => SvmAsync(s => s.ExportAllDataForAvatarByUsernameAsync(avatarUsername, version));
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => Svm(s => s.ExportAllDataForAvatarByUsername(avatarUsername, version));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) => SvmAsync(s => s.ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version));
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => Svm(s => s.ExportAllDataForAvatarByEmail(avatarEmailAddress, version));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => SvmAsync(s => s.ExportAllAsync(version));
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => Svm(s => s.ExportAll(version));

        // IOASISNETProvider
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters) => Svm(s => ((IOASISNETProvider)s).GetAvatarsNearMe(geoLat, geoLong, radiusInMeters));
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type) => Svm(s => ((IOASISNETProvider)s).GetHolonsNearMe(geoLat, geoLong, radiusInMeters, Type));
    }
}
