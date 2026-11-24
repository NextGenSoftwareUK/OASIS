using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.StellarOASIS
{
    /// <summary>
    /// Stellar provider for OASIS storage. Records are compressed and encrypted into Kubo envelopes; the ledger is the
    /// authority: each write or delete is an Ed25519-signed ManageData transaction on the storage account, whose data
    /// entry commits the envelope's CID and SHA-256. Avatar, avatar-detail and holon semantics come from
    /// KeyValueStorageProviderBase.
    /// </summary>
    public class StellarOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        /// <summary>Stellar public network passphrase.</summary>
        public const string PublicNetworkPassphrase = "Public Global Stellar Network ; September 2015";

        /// <summary>Unconfigured instance: storage operations fail with an explicit configuration error.</summary>
        public StellarOASIS() : this(new UnconfiguredStellarBackend()) { }

        /// <param name="horizonUrl">Horizon endpoint of the network the storage account lives on.</param>
        /// <param name="networkPassphrase">The network passphrase signatures are bound to.</param>
        /// <param name="privateKeyHex">32-byte Ed25519 private key (hex) of the funded storage account.</param>
        /// <param name="kuboApi">Kubo HTTP API that holds the encrypted record envelopes.</param>
        /// <param name="encryptionKey">32-byte record encryption key; envelopes are compressed then encrypted.</param>
        public StellarOASIS(string horizonUrl, string networkPassphrase, string privateKeyHex, string kuboApi, byte[] encryptionKey)
            : this(CreateBackend(horizonUrl, networkPassphrase, privateKeyHex, kuboApi, encryptionKey)) { }

        private StellarOASIS(IKeyValueBackend backend) : base(backend)
        {
            ProviderName = "StellarOASIS";
            ProviderDescription = "Stellar provider - ledger-committed, encrypted record storage on account data entries";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.StellarOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }

        /// <summary>Stellar account ID (G...) of the configured storage account.</summary>
        public string StorageAccountId => (Backend as AccountBackend)?.AccountId;

        /// <summary>Stellar account ID (G...) for a 32-byte Ed25519 private key.</summary>
        public static string AccountIdForPrivateKey(string privateKeyHex) => new StellarEd25519Signer(privateKeyHex, PublicNetworkPassphrase).AccountId;

        private static IKeyValueBackend CreateBackend(string horizonUrl, string networkPassphrase, string privateKeyHex, string kuboApi, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Stellar storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            var ledger = new StellarLedgerKeyValueBackend(horizonUrl, networkPassphrase, privateKeyHex, kuboApi);
            return new AccountBackend(new PrivateKeyValueBackend(ledger, encryptionKey, compress: true), ledger.AccountId);
        }

        private sealed class AccountBackend(PrivateKeyValueBackend inner, string accountId) : IKeyValueBackend, IDisposable
        {
            public string AccountId { get; } = accountId;
            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => inner.GetAsync(key, cancellationToken);
            public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => inner.PutAsync(key, value, cancellationToken);
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => inner.DeleteAsync(key, cancellationToken);
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => inner.ListKeysAsync(prefix, cancellationToken);
            public Task VerifyAsync(CancellationToken cancellationToken = default) => inner.VerifyAsync(cancellationToken);
            public void Dispose() => inner.Dispose();
        }

        private sealed class UnconfiguredStellarBackend : IKeyValueBackend
        {
            private static Exception Missing() => new InvalidOperationException(
                "Stellar storage requires an explicit Horizon endpoint, network passphrase, Ed25519 storage key, Kubo endpoint and record encryption key.");
            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
            public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
            public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Missing());
        }

        // ─── IOASISNETProvider ────────────────────────────────────────────────────
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "StellarOASIS: Geolocation not supported."); return r; }
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "StellarOASIS: Geolocation not supported."); return r; }
    }
}
