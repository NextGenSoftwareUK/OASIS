using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS
{
    /// <summary>
    /// Arweave storage provider. Avatars, avatar details and holons are stored through
    /// <see cref="KeyValueStorageProviderBase"/> on an append-only, owner-pinned key-value log
    /// (<see cref="ArweaveKeyValueBackend"/>): each write is a transaction signed by the configured JWK wallet and reads only
    /// consider that wallet's transactions, newest <c>OASIS-Seq</c> first. Because the chain is permanent and public, keys
    /// are HMAC-hidden and values compressed and encrypted (<see cref="PrivateKeyValueBackend"/>).
    /// The 32-byte record key is HKDF-SHA256 over the wallet JWK's private exponent <c>d</c> (no salt, info
    /// "OASIS Arweave record key v1"), so the same wallet always reads its own records and nobody else can.
    /// Non-storage helpers (UploadDataToArweaveAsync, UploadJsonToArweaveAsync, DownloadDataFromArweaveAsync) post or read
    /// plain public transactions for NFT metadata.
    /// </summary>
    public partial class ArweaveOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        internal const string RecordKeyInfo = "OASIS Arweave record key v1";
        private readonly ActivationBoundBackend _storage;
        private string _walletPath;
        private string _walletJson;
        private string _gatewayUrl;
        private OASISDNA _OASISDNA;
        private string _OASISDNAPath;
        private IArweaveService _arweaveService;
        private string _nodeExecutable;
        private string _sdkBridgePath;
        private bool _mineAfterPost;

        public ArweaveOASIS() : this(new ActivationBoundBackend())
        {
            OASISDNAManager.LoadDNA();
            _OASISDNA = OASISDNAManager.OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;
        }

        public ArweaveOASIS(string OASISDNAPath) : this(new ActivationBoundBackend())
        {
            _OASISDNAPath = OASISDNAPath;
            OASISDNAManager.LoadDNA(_OASISDNAPath);
            _OASISDNA = OASISDNAManager.OASISDNA;
        }

        public ArweaveOASIS(OASISDNA OASISDNA) : this(new ActivationBoundBackend())
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;
        }

        public ArweaveOASIS(OASISDNA OASISDNA, string OASISDNAPath) : this(new ActivationBoundBackend())
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAPath;
        }

        public ArweaveOASIS(string walletJson, string gatewayUrl, string sdkBridgePath, string nodeExecutable = "node", bool mineAfterPost = false)
            : this(new ActivationBoundBackend())
        {
            _walletJson = walletJson;
            _gatewayUrl = gatewayUrl;
            _sdkBridgePath = sdkBridgePath;
            _nodeExecutable = nodeExecutable;
            _mineAfterPost = mineAfterPost;
        }

        /// <summary>Binds storage to an already constructed native service (used by tests with a fake service).</summary>
        internal ArweaveOASIS(IArweaveService service, string walletJson) : this(new ActivationBoundBackend())
        {
            _walletJson = walletJson;
            Bind(service);
        }

        private ArweaveOASIS(ActivationBoundBackend storage) : base(storage)
        {
            _storage = storage;
            Init();
        }

        private void Bind(IArweaveService service)
        {
            _arweaveService = service;
            _storage.Bind(new PrivateKeyValueBackend(new ArweaveKeyValueBackend(service), DeriveRecordKey(_walletJson), compress: true));
        }

        /// <summary>HKDF-SHA256 over the JWK private exponent; see the class summary.</summary>
        internal static byte[] DeriveRecordKey(string walletJson)
        {
            if (string.IsNullOrWhiteSpace(walletJson))
                throw new InvalidOperationException("ArweaveOASIS is a writable storage provider and requires a JWK wallet.");
            using var wallet = System.Text.Json.JsonDocument.Parse(walletJson);
            if (!wallet.RootElement.TryGetProperty("d", out var d) || d.ValueKind != System.Text.Json.JsonValueKind.String || string.IsNullOrEmpty(d.GetString()))
                throw new InvalidOperationException("The Arweave JWK wallet has no private exponent 'd'.");
            var secret = Base64UrlDecode(d.GetString());
            try
            {
                return System.Security.Cryptography.HKDF.DeriveKey(System.Security.Cryptography.HashAlgorithmName.SHA256, secret, 32,
                    Array.Empty<byte>(), System.Text.Encoding.UTF8.GetBytes(RecordKeyInfo));
            }
            finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(secret); }
        }

        private static byte[] Base64UrlDecode(string value)
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            return Convert.FromBase64String(padded);
        }

        /// <summary>Storage configuration (wallet, gateway) is read at activation; before then every storage call fails explicitly.</summary>
        private sealed class ActivationBoundBackend : IKeyValueBackend
        {
            private IKeyValueBackend _inner;
            public void Bind(IKeyValueBackend inner) => _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            private IKeyValueBackend Inner => _inner ?? throw new InvalidOperationException("ArweaveOASIS storage is not activated.");
            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Inner.GetAsync(key, cancellationToken);
            public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Inner.PutAsync(key, value, cancellationToken);
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Inner.DeleteAsync(key, cancellationToken);
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Inner.ListKeysAsync(prefix, cancellationToken);
            public Task VerifyAsync(CancellationToken cancellationToken = default) => Inner.VerifyAsync(cancellationToken);
        }
    }
}
