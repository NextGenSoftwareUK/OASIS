using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Linq;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS
{
    /// <summary>
    /// Aztec provider. OASIS storage is committed to Aztec public state: each record is an encrypted envelope on a Kubo
    /// node, and the OasisKv Noir contract holds its SHA-256 under the record id, written by the storage account through
    /// the pinned aztec.js worker (see <see cref="AztecLedgerKeyValueBackend"/>). Avatar, avatar-detail and holon semantics
    /// come from <see cref="KeyValueStorageProviderBase"/>. Private notes, proofs and the bridge use the Aztec services.
    /// </summary>
    public partial class AztecOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider, IOASISNETProvider, IOASISSmartContractProvider
    {
        private readonly AztecAPIClient _apiClient;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;
        private readonly string _network;
        private readonly string _bridgeContractAddress;
        private readonly string _operatorAccountAlias;
        private readonly string _stablecoinContractAddress;

        private readonly IAztecService _aztecService;
        private readonly IAztecBridgeService _bridgeService;

        /// <param name="storageContract">Address of the deployed OasisKv contract that holds this provider's record commitments.</param>
        /// <param name="storageSecret">Storage account secret (Fr hex); the account must own the OasisKv contract.</param>
        /// <param name="storageSalt">Storage account deployment salt (Fr hex).</param>
        /// <param name="storageSigningKey">Storage account Schnorr signing key (Fq hex).</param>
        /// <param name="kuboApi">Kubo HTTP API that holds the encrypted record envelopes.</param>
        /// <param name="encryptionKey">32-byte record encryption key; envelopes are compressed then encrypted.</param>
        public AztecOASIS(string apiBaseUrl = null, string apiKey = null, string network = "sandbox",
            string bridgeContractAddress = "", string operatorAccountAlias = "oasis_operator",
            string stablecoinContractAddress = "", string storageContract = null, string storageSecret = null,
            string storageSalt = null, string storageSigningKey = null, string kuboApi = null, byte[] encryptionKey = null)
            : base(CreateBackend(apiBaseUrl ?? "http://localhost:8080", storageContract, storageSecret, storageSalt, storageSigningKey, kuboApi, encryptionKey))
        {
            ProviderName = nameof(AztecOASIS);
            ProviderDescription = "Aztec provider - public-state committed, encrypted record storage and private notes";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AztecOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            _apiBaseUrl = apiBaseUrl ?? "http://localhost:8080";
            _apiKey = apiKey ?? "";
            _network = network ?? "sandbox";
            _bridgeContractAddress = bridgeContractAddress ?? "";
            _operatorAccountAlias = string.IsNullOrWhiteSpace(operatorAccountAlias) ? "oasis_operator" : operatorAccountAlias;
            _stablecoinContractAddress = stablecoinContractAddress ?? "";

            _apiClient = new AztecAPIClient(_apiBaseUrl, _apiKey);
            _aztecService = new AztecService(_apiClient, _stablecoinContractAddress);
            _bridgeService = new AztecBridgeService(_apiClient, _bridgeContractAddress, _operatorAccountAlias);
        }

        private static IKeyValueBackend CreateBackend(string nodeUrl, string contract, string secret, string salt, string signingKey,
            string kuboApi, byte[] encryptionKey)
        {
            if (contract == null && secret == null && salt == null && signingKey == null && kuboApi == null && encryptionKey == null)
                return new UnconfiguredAztecStorageBackend();
            if (string.IsNullOrWhiteSpace(contract) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(salt)
                || string.IsNullOrWhiteSpace(signingKey) || string.IsNullOrWhiteSpace(kuboApi))
                throw new ArgumentException("Aztec storage requires the OasisKv contract, the storage account's secret, salt and signing key, and a Kubo endpoint.");
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Aztec storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            var ledger = new AztecLedgerKeyValueBackend(new AztecSdkBridge(), nodeUrl, contract, secret, salt, signingKey, kuboApi);
            return new PrivateKeyValueBackend(ledger, encryptionKey, compress: true);
        }

        private sealed class UnconfiguredAztecStorageBackend : IKeyValueBackend
        {
            private static Exception Missing() => new InvalidOperationException(
                "Aztec storage requires an explicit OasisKv contract, storage account, Kubo endpoint and record encryption key.");
            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
            public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
            public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Missing());
        }
    }
}
