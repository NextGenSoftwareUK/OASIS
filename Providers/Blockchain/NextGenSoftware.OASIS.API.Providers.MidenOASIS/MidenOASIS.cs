using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using LockWeb3TokenRequest = NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.LockWeb3TokenRequest;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Infrastructure.Services.Miden;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Models;
using NextGenSoftware.OASIS.Common;
using System.Text.Json;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS
{
    /// <summary>
    /// Miden provider. OASIS storage is committed to a public Miden account: each record is an encrypted envelope on a
    /// Kubo node, and the account's <c>oasis::kv</c> storage map holds its SHA-256, written by account transactions through
    /// the pinned Miden SDK worker (see <see cref="MidenLedgerKeyValueBackend"/>). Avatar, avatar-detail and holon
    /// semantics come from <see cref="KeyValueStorageProviderBase"/>. Private notes, proofs and the bridge use the Miden services.
    /// </summary>
    public partial class MidenOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider, IOASISNETProvider, IOASISSmartContractProvider, IDisposable
    {
        private readonly MidenAPIClient _apiClient;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;
        private readonly string _network;

        private readonly StorageResources _storage;
        private readonly IMidenService _midenService;
        private readonly MidenBridgeService _bridgeService;

        /// <param name="rpcUrl">Miden node RPC endpoint used for storage (for example http://127.0.0.1:57291).</param>
        /// <param name="storageAccountId">Hex id of the public storage account that carries the oasis::kv component.</param>
        /// <param name="storageKeySeed">32-byte hex seed of the account's Falcon signing key.</param>
        /// <param name="kuboApi">Kubo HTTP API that holds the encrypted record envelopes.</param>
        /// <param name="encryptionKey">32-byte record encryption key; envelopes are compressed then encrypted.</param>
        public MidenOASIS(string apiBaseUrl = null, string apiKey = null, string network = "testnet", string rpcUrl = null,
            string storageAccountId = null, string storageKeySeed = null, string kuboApi = null, byte[] encryptionKey = null)
            : this(CreateBackend(rpcUrl, storageAccountId, storageKeySeed, kuboApi, encryptionKey), apiBaseUrl, apiKey, network) { }

        private MidenOASIS(StorageResources storage, string apiBaseUrl, string apiKey, string network)
            : base(storage.Backend)
        {
            _storage = storage;
            ProviderName = nameof(MidenOASIS);
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MidenOASIS);
            ProviderDescription = "Miden provider - account-storage committed, encrypted record storage and STARK-proven notes";
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            _apiBaseUrl = apiBaseUrl ?? Environment.GetEnvironmentVariable("MIDEN_API_URL") ?? "https://testnet.miden.xyz";
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("MIDEN_API_KEY");
            _network = network;

            _apiClient = new MidenAPIClient(_apiBaseUrl, _apiKey);
            _midenService = new MidenService(_apiClient);
            _bridgeService = new MidenBridgeService(_midenService);
        }

        private static StorageResources CreateBackend(string rpcUrl, string accountId, string keySeed, string kuboApi, byte[] encryptionKey)
        {
            rpcUrl ??= Environment.GetEnvironmentVariable("OASIS_MIDEN_RPC_URL");
            accountId ??= Environment.GetEnvironmentVariable("OASIS_MIDEN_STORAGE_ACCOUNT");
            keySeed ??= Environment.GetEnvironmentVariable("OASIS_MIDEN_STORAGE_KEY_SEED");
            kuboApi ??= Environment.GetEnvironmentVariable("OASIS_MIDEN_KUBO");
            var encodedKey = Environment.GetEnvironmentVariable("OASIS_MIDEN_STORAGE_KEY");
            if (encryptionKey == null && encodedKey != null)
                encryptionKey = Convert.FromBase64String(encodedKey);
            if (rpcUrl == null && accountId == null && keySeed == null && kuboApi == null && encryptionKey == null)
                return new StorageResources(new UnconfiguredMidenStorageBackend(), null, null);
            if (string.IsNullOrWhiteSpace(rpcUrl) || string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(keySeed) || string.IsNullOrWhiteSpace(kuboApi))
                throw new ArgumentException("Miden storage requires a node RPC endpoint, the storage account id and key seed, and a Kubo endpoint.");
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Miden storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            var sdk = new MidenSdkBridge();
            var ledger = new MidenLedgerKeyValueBackend(sdk, rpcUrl, accountId, keySeed, kuboApi);
            return new StorageResources(new PrivateKeyValueBackend(ledger, encryptionKey, compress: true), ledger, sdk);
        }

        internal int? SdkProcessId => _storage.Sdk?.WorkerProcessId;
        public void Dispose() => _storage.Dispose();

        private sealed class StorageResources : IDisposable
        {
            public IKeyValueBackend Backend { get; }
            public MidenSdkBridge Sdk { get; }
            private readonly MidenLedgerKeyValueBackend _ledger;
            private int _disposed;

            public StorageResources(IKeyValueBackend backend, MidenLedgerKeyValueBackend ledger, MidenSdkBridge sdk)
            {
                Backend = backend;
                _ledger = ledger;
                Sdk = sdk;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                try { if (Backend is IDisposable owner) owner.Dispose(); }
                finally { _ledger?.Dispose(); }
            }
        }

        private sealed class UnconfiguredMidenStorageBackend : IKeyValueBackend
        {
            private static Exception Missing() => new InvalidOperationException(
                "Miden storage requires an explicit node RPC endpoint, storage account, Kubo endpoint and record encryption key.");
            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
            public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
            public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Missing());
        }

    }
}
