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
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS
{
    /// <summary>
    /// Zcash provider. OASIS storage is ledger-committed: each record is an encrypted envelope on a Kubo node, and every
    /// write or delete is a transparent transaction from the configured storage address whose OP_RETURN commits the
    /// record key and the envelope's SHA-256 (see <see cref="ZcashLedgerKeyValueBackend"/>). Avatar, avatar-detail and
    /// holon semantics come from <see cref="KeyValueStorageProviderBase"/>. Shielded transfers and the bridge use the
    /// node's wallet RPC directly.
    /// </summary>
    public partial class ZcashOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider, IOASISNETProvider
    {
        private readonly IZcashService _zcashService;
        private readonly IZcashBridgeService _zcashBridgeService;
        private readonly ZcashRPCClient _rpcClient;
        private readonly string _network; // "mainnet", "testnet" or "regtest"
        private readonly string _burnAddress; // Shielded address to send tokens to when "burning"

        /// <param name="rpcUrl">zcashd JSON-RPC endpoint (defaults to ZCASH_RPC_URL).</param>
        /// <param name="storageAddress">Transparent address, held by the node wallet, that authors storage commitments.</param>
        /// <param name="kuboApi">Kubo HTTP API that holds the encrypted record envelopes.</param>
        /// <param name="encryptionKey">32-byte record encryption key; envelopes are compressed then encrypted.</param>
        public ZcashOASIS(string rpcUrl = null, string rpcUser = null, string rpcPassword = null, string network = "testnet",
            string burnAddress = null, string storageAddress = null, string kuboApi = null, byte[] encryptionKey = null)
            : this(new ZcashRPCClient(ResolveRpcUrl(rpcUrl), ResolveRpcUser(rpcUser), ResolveRpcPassword(rpcPassword)),
                ResolveRpcUrl(rpcUrl), ResolveRpcUser(rpcUser), ResolveRpcPassword(rpcPassword), network, burnAddress,
                storageAddress, kuboApi, encryptionKey) { }

        private ZcashOASIS(ZcashRPCClient rpcClient, string rpcUrl, string rpcUser, string rpcPassword, string network,
            string burnAddress, string storageAddress, string kuboApi, byte[] encryptionKey)
            : base(CreateBackend(rpcUrl, rpcUser, rpcPassword, storageAddress, kuboApi, encryptionKey))
        {
            this.ProviderName = nameof(ZcashOASIS);
            this.ProviderDescription = "Zcash provider - ledger-committed, encrypted record storage and shielded transactions";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ZcashOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            _network = network;
            _rpcClient = rpcClient;
            _zcashService = new ZcashService(_rpcClient);
            _zcashBridgeService = new ZcashBridgeService(_rpcClient);
            // Unspendable shielded burn address (all-zero payment address in Sapling format)
            _burnAddress = burnAddress ?? Environment.GetEnvironmentVariable("ZCASH_BURN_ADDRESS") ?? "zs1qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq";
        }

        /// <summary>Transparent address that authors this provider's storage commitments, when storage is configured.</summary>
        public string StorageAddress => (Backend as AddressedBackend)?.Address;

        private static string ResolveRpcUrl(string value) => value ?? Environment.GetEnvironmentVariable("ZCASH_RPC_URL") ?? "http://localhost:8232";
        private static string ResolveRpcUser(string value) => value ?? Environment.GetEnvironmentVariable("ZCASH_RPC_USER") ?? "user";
        private static string ResolveRpcPassword(string value) => value ?? Environment.GetEnvironmentVariable("ZCASH_RPC_PASSWORD") ?? "password";

        private static IKeyValueBackend CreateBackend(string rpcUrl, string rpcUser, string rpcPassword,
            string storageAddress, string kuboApi, byte[] encryptionKey)
        {
            if (storageAddress == null && kuboApi == null && encryptionKey == null)
                return new UnconfiguredZcashStorageBackend();
            if (string.IsNullOrWhiteSpace(storageAddress) || string.IsNullOrWhiteSpace(kuboApi))
                throw new ArgumentException("Zcash storage requires a transparent storage address and a Kubo endpoint.");
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Zcash storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            var ledger = new ZcashLedgerKeyValueBackend(rpcUrl, rpcUser, rpcPassword, storageAddress, kuboApi);
            return new AddressedBackend(new PrivateKeyValueBackend(ledger, encryptionKey, compress: true), storageAddress);
        }

        private sealed class AddressedBackend(PrivateKeyValueBackend inner, string address) : IKeyValueBackend, IDisposable
        {
            public string Address { get; } = address;
            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => inner.GetAsync(key, cancellationToken);
            public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => inner.PutAsync(key, value, cancellationToken);
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => inner.DeleteAsync(key, cancellationToken);
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => inner.ListKeysAsync(prefix, cancellationToken);
            public Task VerifyAsync(CancellationToken cancellationToken = default) => inner.VerifyAsync(cancellationToken);
            public void Dispose() => inner.Dispose();
        }

        private sealed class UnconfiguredZcashStorageBackend : IKeyValueBackend
        {
            private static Exception Missing() => new InvalidOperationException(
                "Zcash storage requires an explicit storage address, Kubo endpoint and record encryption key.");
            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
            public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
            public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Missing());
        }
    }
}
