using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.FlowOASIS
{
    /// <summary>
    /// Flow OASIS provider. Flow blockchain for NFTs and gaming, accessed via the Flow REST API.
    /// Endpoint: https://rest-mainnet.onflow.org
    /// </summary>
    public class FlowOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
    {
        private static OASISResult<T> NoRecordStore<T>()
            => new OASISResult<T> { IsError = true, ErrorCount = 1, ErrorCode = "FLOWOASIS_NO_RECORD_STORE",
                Message = "FlowOASIS has no OASIS record store: it does not persist avatars or holons. The previous implementation echoed "
                    + "writes back and returned blank records for any id without touching the network." };

        private readonly HttpClient _http;
        private readonly string _rpcUrl;
        private bool _isActivated;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private StringContent Json(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        public FlowOASIS(string rpcUrl = "https://rest-mainnet.onflow.org")
        {
            _rpcUrl = rpcUrl?.TrimEnd('/') ?? "https://rest-mainnet.onflow.org";
            _http = new HttpClient { BaseAddress = new Uri(_rpcUrl + "/") };

            ProviderName = "FlowOASIS";
            ProviderDescription = "Flow provider — Flow blockchain for NFTs and gaming, accessed via the Flow REST API.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FlowOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_isActivated) { result.Result = true; result.Message = "FlowOASIS already activated"; return result; }
                var resp = await _http.GetAsync("v1/blocks?height=sealed");
                if (!resp.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"FlowOASIS RPC health check failed ({resp.StatusCode})");
                    return result;
                }
                _isActivated = true;
                result.Result = true;
                result.Message = "FlowOASIS activated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"FlowOASIS activation failed: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                _isActivated = false;
                _http.Dispose();
                result.Result = true;
                result.Message = "FlowOASIS deactivated";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"FlowOASIS deactivation failed: {ex.Message}", ex);
            }
            return result;
        }

        // ── Avatar CRUD ───────────────────────────────────────────────────────

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0) => Task.FromResult(NoRecordStore<IAvatar>());

        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar) => Task.FromResult(NoRecordStore<IAvatar>());

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) => Task.FromResult(NoRecordStore<bool>());

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0) => Task.FromResult(NoRecordStore<IEnumerable<IAvatar>>());

        // ── Holon CRUD ────────────────────────────────────────────────────────

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NoRecordStore<IHolon>());

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => Task.FromResult(NoRecordStore<IHolon>());

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NoRecordStore<IEnumerable<IHolon>>());

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => Task.FromResult(NoRecordStore<IEnumerable<IHolon>>());

        // ── Search ────────────────────────────────────────────────────────────

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => Task.FromResult(NoRecordStore<ISearchResults>());

        // ── Avatar Detail ─────────────────────────────────────────────────────

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) => Task.FromResult(NoRecordStore<IAvatarDetail>());

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail) => Task.FromResult(NoRecordStore<IAvatarDetail>());

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) => Task.FromResult(NoRecordStore<IEnumerable<IAvatarDetail>>());

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) => Task.FromResult(NoRecordStore<IEnumerable<IHolon>>());

        // ── IOASISNETProvider ─────────────────────────────────────────────────

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long lat, long lon, int radius)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            r.Result = new List<IAvatar>();
            return r;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long lat, long lon, int radius, HolonType holonType = HolonType.All)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            r.Result = new List<IHolon>();
            return r;
        }

        // ── IOASISBlockchainStorageProvider ───────────────────────────────────

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var r = new OASISResult<ITransactionResponse>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS SendTokenAsync not yet implemented");
            return r;
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request) => SendTokenAsync(request).Result;

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var r = new OASISResult<ITransactionResponse>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS MintTokenAsync not yet implemented");
            return r;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request) => MintTokenAsync(request).Result;

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var r = new OASISResult<ITransactionResponse>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS BurnTokenAsync not yet implemented");
            return r;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request) => BurnTokenAsync(request).Result;

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var r = new OASISResult<ITransactionResponse>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS LockTokenAsync not yet implemented");
            return r;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request) => LockTokenAsync(request).Result;

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var r = new OASISResult<ITransactionResponse>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS UnlockTokenAsync not yet implemented");
            return r;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request) => UnlockTokenAsync(request).Result;

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var r = new OASISResult<double>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS GetBalanceAsync not yet implemented");
            return r;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request) => GetBalanceAsync(request).Result;

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var r = new OASISResult<IList<IWalletTransaction>>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS GetTransactionsAsync not yet implemented");
            return r;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request) => GetTransactionsAsync(request).Result;

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var r = new OASISResult<IKeyPairAndWallet>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS GenerateKeyPairAsync not yet implemented");
            return r;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair() => GenerateKeyPairAsync().Result;

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var r = new OASISResult<(string, string, string)>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS CreateAccountAsync not yet implemented");
            return r;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var r = new OASISResult<(string, string)>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS RestoreAccountAsync not yet implemented");
            return r;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var r = new OASISResult<BridgeTransactionResponse>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS WithdrawAsync not yet implemented");
            return r;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var r = new OASISResult<BridgeTransactionResponse>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS DepositAsync not yet implemented");
            return r;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var r = new OASISResult<BridgeTransactionStatus>();
            OASISErrorHandling.HandleError(ref r, "FlowOASIS GetTransactionStatusAsync not yet implemented");
            return r;
        }

        // ── Sync wrappers ─────────────────────────────────────────────────────

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        public override OASISResult<IHolon> DeleteHolon(Guid id) => NoRecordStore<IHolon>();
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => NoRecordStore<IHolon>();
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ── Missing async stubs ───────────────────────────────────────────────

        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0) => Task.FromResult(NoRecordStore<IAvatar>());
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0) => Task.FromResult(NoRecordStore<IAvatar>());
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0) => Task.FromResult(NoRecordStore<IAvatar>());
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0) => Task.FromResult(NoRecordStore<IAvatarDetail>());
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0) => Task.FromResult(NoRecordStore<IAvatarDetail>());
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) => Task.FromResult(NoRecordStore<bool>());
        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true) => Task.FromResult(NoRecordStore<bool>());
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true) => Task.FromResult(NoRecordStore<bool>());
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => Task.FromResult(NoRecordStore<IHolon>());
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) => Task.FromResult(NoRecordStore<IHolon>());
        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NoRecordStore<IHolon>());
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NoRecordStore<IEnumerable<IHolon>>());
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NoRecordStore<IEnumerable<IHolon>>());
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NoRecordStore<IEnumerable<IHolon>>());
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NoRecordStore<IEnumerable<IHolon>>());
        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) => Task.FromResult(NoRecordStore<bool>());
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) => Task.FromResult(NoRecordStore<IEnumerable<IHolon>>());
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) => Task.FromResult(NoRecordStore<IEnumerable<IHolon>>());
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => Task.FromResult(NoRecordStore<IEnumerable<IHolon>>());
    }
}
