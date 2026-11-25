using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Flow OASIS provider. Records are encrypted and stored in the OasisKv Cadence contract (SdkBridge/OasisKv.cdc):
    /// reads are scripts, writes are transactions signed by the account that holds the contract's Admin resource and are
    /// acknowledged only once sealed. The previous implementation echoed writes back and returned blank records.
    /// </summary>
    public class FlowOASIS : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValueStorageProviderBase, IOASISNETProvider, IOASISBlockchainStorageProvider
    {
        /// <summary>Unconfigured provider: every storage call fails explicitly until an access node, OasisKv contract,
        /// signer and record encryption key are supplied.</summary>
        public FlowOASIS(string rpcUrl = "https://rest-mainnet.onflow.org")
            : this(new UnconfiguredFlowBackend()) { }

        /// <param name="accessNodeRestUrl">Flow access node REST API, for example http://127.0.0.1:8888 for the emulator.</param>
        /// <param name="contractAddress">Account that holds the OasisKv contract.</param>
        /// <param name="signerAddress">Account that holds the OasisKv Admin resource (the deploying account).</param>
        /// <param name="signerPrivateKeyHex">Key of that account; it signs every write.</param>
        /// <param name="encryptionKey">32-byte record encryption key: contract storage is public.</param>
        public FlowOASIS(string accessNodeRestUrl, string contractAddress, string signerAddress, string signerPrivateKeyHex, byte[] encryptionKey,
            int signerKeyIndex = 0, string signatureAlgorithm = "ECDSA_P256")
            : this(new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PrivateKeyValueBackend(
                new FlowLedgerKeyValueBackend(new FlowSdkBridge(), accessNodeRestUrl, contractAddress, signerAddress, signerPrivateKeyHex,
                    signerKeyIndex, signatureAlgorithm),
                encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey)), compress: true)) { }

        private FlowOASIS(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend backend) : base(backend)
        {
            ProviderName = "FlowOASIS";
            ProviderDescription = "Flow provider — Flow blockchain for NFTs and gaming; records in the OasisKv Cadence contract.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FlowOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }

        private sealed class UnconfiguredFlowBackend : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend
        {
            private static Exception Missing() => new InvalidOperationException(
                "Flow storage requires an explicit access node, OasisKv contract, signer key and record encryption key.");
            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
            public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
            public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Missing());
        }

        // ── IOASISNETProvider ─────────────────────────────────────────────────

        /// <summary>Avatars whose stored Latitude/Longitude metadata lie within the radius (coordinates are micro-degrees).</summary>
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long lat, long lon, int radius)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            var all = LoadAllAvatars();
            if (all.IsError || all.Result == null) { OASISErrorHandling.HandleError(ref result, $"Error loading Flow avatars: {all.Message}", all.Exception); return result; }
            var nearby = new List<IAvatar>();
            foreach (var avatar in all.Result) if (Within(avatar?.MetaData, lat, lon, radius)) nearby.Add(avatar);
            result.Result = nearby;
            return result;
        }

        /// <summary>Holons whose stored Latitude/Longitude metadata lie within the radius (coordinates are micro-degrees).</summary>
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long lat, long lon, int radius, HolonType holonType = HolonType.All)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = LoadAllHolons(holonType);
            if (all.IsError || all.Result == null) { OASISErrorHandling.HandleError(ref result, $"Error loading Flow holons: {all.Message}", all.Exception); return result; }
            var nearby = new List<IHolon>();
            foreach (var holon in all.Result) if (Within(holon?.MetaData, lat, lon, radius)) nearby.Add(holon);
            result.Result = nearby;
            return result;
        }

        private static bool Within(IDictionary<string, object> metaData, long lat, long lon, int radius)
            => metaData != null
               && metaData.TryGetValue("Latitude", out var latValue) && metaData.TryGetValue("Longitude", out var lonValue)
               && double.TryParse(latValue?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var a)
               && double.TryParse(lonValue?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var b)
               && GeoHelper.CalculateDistance(lat / 1e6d, lon / 1e6d, a, b) <= radius;

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

    }
}
