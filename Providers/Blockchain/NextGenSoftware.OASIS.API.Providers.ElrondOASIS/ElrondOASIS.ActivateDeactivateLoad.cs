using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS
{
    public partial class ElrondOASIS
    {
        public ElrondOASIS(string rpcEndpoint = "https://api.elrond.com", string network = "mainnet", string chainId = "1", WalletManager walletManager = null)
            : this(new UnconfiguredElrondBackend(), rpcEndpoint, network, chainId, walletManager) { }

        /// <summary>
        /// Account-store storage: records are compressed and encrypted into Kubo envelopes, and the storage account's own
        /// MultiversX key-value store holds each key's (CID, SHA-256) commitment, written by signed SaveKeyValue transactions.
        /// </summary>
        /// <param name="proxyUrl">MultiversX proxy (gateway) API.</param>
        /// <param name="chainId">Chain id the proxy must serve ("1" mainnet, "D" devnet, "T" testnet, or a local chain).</param>
        /// <param name="secretKeyHex">32-byte Ed25519 secret key (hex) of the funded storage account.</param>
        /// <param name="kuboApi">Kubo HTTP API that holds the encrypted record envelopes.</param>
        /// <param name="encryptionKey">32-byte record encryption key.</param>
        public ElrondOASIS(string proxyUrl, string chainId, string secretKeyHex, string kuboApi, byte[] encryptionKey)
            : this(CreateStorageBackend(proxyUrl, chainId, secretKeyHex, kuboApi, encryptionKey), proxyUrl, "custom", chainId, null) { }

        /// <summary>erd1 address of the storage account for a secret key.</summary>
        public static string AddressForSecretKey(string secretKeyHex) => new MultiversXTransactionSigner(secretKeyHex).Address;

        private static NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend CreateStorageBackend(
            string proxyUrl, string chainId, string secretKeyHex, string kuboApi, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("MultiversX storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            var ledger = new MultiversXAccountKeyValueBackend(proxyUrl, chainId, secretKeyHex, kuboApi);
            return new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PrivateKeyValueBackend(ledger, encryptionKey, compress: true);
        }

        private sealed class UnconfiguredElrondBackend : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend
        {
            private static Exception Missing() => new InvalidOperationException(
                "MultiversX storage requires an explicit proxy, chain id, funded storage account key, Kubo endpoint and record encryption key.");
            public Task<string> GetAsync(string key, System.Threading.CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
            public Task PutAsync(string key, string value, System.Threading.CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task DeleteAsync(string key, System.Threading.CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, System.Threading.CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
            public Task VerifyAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromException(Missing());
        }

        private ElrondOASIS(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend backend, string rpcEndpoint, string network, string chainId, WalletManager walletManager)
            : base(backend)
        {
            _rpcEndpoint = rpcEndpoint;
            _network = network;
            _chainId = chainId;
            _walletManager = walletManager;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_rpcEndpoint);

            this.ProviderName = "ElrondOASIS";
            this.ProviderDescription = "Elrond Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ElrondOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }





































    }
}
