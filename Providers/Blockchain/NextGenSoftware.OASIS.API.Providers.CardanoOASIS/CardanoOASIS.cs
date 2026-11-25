using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS
{
    public partial class CardanoOASIS : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValueStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcEndpoint;
        private readonly string _networkId;
        private readonly string _privateKey;
        private readonly string _contractAddress;
        private bool _isActivated;
        private WalletManager _walletManager;

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = new WalletManager(this, OASISDNA);
                return _walletManager;
            }
            set => _walletManager = value;
        }

        /// <summary>
        /// Initializes a new instance of the CardanoOASIS provider
        /// </summary>
        /// <param name="rpcEndpoint">Cardano RPC endpoint URL</param>
        /// <param name="networkId">Cardano network ID (mainnet, testnet, preprod)</param>
        /// <param name="privateKey">Private key for signing transactions</param>
        public CardanoOASIS(string rpcEndpoint = "https://cardano-mainnet.blockfrost.io/api/v0", string networkId = "mainnet", string privateKey = "")
            : this(new UnconfiguredCardanoBackend(), rpcEndpoint, networkId, privateKey) { }

        /// <summary>
        /// Ledger-committed storage: records are compressed and encrypted into Kubo envelopes and every write or delete is
        /// an Ed25519-signed transaction whose metadata (label 1967) commits the key and envelope.
        /// </summary>
        /// <param name="blockfrostUrl">Blockfrost-compatible API base URL.</param>
        /// <param name="projectId">Blockfrost project id, or null for an API without one.</param>
        /// <param name="networkId">"mainnet", or a test network name (enterprise addresses use network id 0).</param>
        /// <param name="signingKeyHex">32-byte Ed25519 payment signing key (hex) of the funded storage address.</param>
        /// <param name="kuboApi">Kubo HTTP API that holds the encrypted record envelopes.</param>
        /// <param name="encryptionKey">32-byte record encryption key.</param>
        public CardanoOASIS(string blockfrostUrl, string projectId, string networkId, string signingKeyHex, string kuboApi, byte[] encryptionKey)
            : this(CreateStorageBackend(blockfrostUrl, projectId, networkId, signingKeyHex, kuboApi, encryptionKey), blockfrostUrl, networkId, signingKeyHex) { }

        /// <summary>Enterprise address (bech32) of the storage key on the given network.</summary>
        public static string AddressForSigningKey(string signingKeyHex, string networkId) => new CardanoEd25519Signer(signingKeyHex, networkId == "mainnet").Address;

        private static NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend CreateStorageBackend(
            string blockfrostUrl, string projectId, string networkId, string signingKeyHex, string kuboApi, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Cardano storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            var ledger = new CardanoLedgerKeyValueBackend(blockfrostUrl, projectId, networkId == "mainnet", signingKeyHex, kuboApi);
            return new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PrivateKeyValueBackend(ledger, encryptionKey, compress: true);
        }

        private sealed class UnconfiguredCardanoBackend : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend
        {
            private static Exception Missing() => new InvalidOperationException(
                "Cardano storage requires an explicit Blockfrost-compatible API, signing key, Kubo endpoint and record encryption key.");
            public Task<string> GetAsync(string key, System.Threading.CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
            public Task PutAsync(string key, string value, System.Threading.CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task DeleteAsync(string key, System.Threading.CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, System.Threading.CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
            public Task VerifyAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromException(Missing());
        }

        private CardanoOASIS(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend backend, string rpcEndpoint, string networkId, string privateKey)
            : base(backend)
        {
            this.ProviderName = "CardanoOASIS";
            this.ProviderDescription = "Cardano Provider - Third-generation blockchain platform";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CardanoOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));

            _rpcEndpoint = rpcEndpoint ?? throw new ArgumentNullException(nameof(rpcEndpoint));
            _networkId = networkId ?? throw new ArgumentNullException(nameof(networkId));
            _privateKey = privateKey;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_rpcEndpoint)
            };
        }


    }
}
