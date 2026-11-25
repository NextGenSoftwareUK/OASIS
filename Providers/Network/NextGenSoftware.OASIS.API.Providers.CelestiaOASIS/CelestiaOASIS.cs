using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.CelestiaOASIS
{
    /// <summary>
    /// OASIS provider for Celestia, a modular data availability (DA) layer.
    ///
    /// Storage: avatars, avatar details and holons are records of the shared key-value layout
    /// (<see cref="KeyValueStorageProviderBase"/>), stored append-only as blobs in one Celestia namespace through a
    /// celestia-node JSON-RPC endpoint (<see cref="CelestiaBlobKeyValueBackend"/>). Celestia blobs are public, so record
    /// keys and values are compressed and encrypted (<see cref="PrivateKeyValueBackend"/>) before submission; record counts,
    /// sizes and timing stay public. Writes cost the node account's fees on the network it is connected to.
    /// </summary>
    public class CelestiaOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IDisposable
    {
        private readonly CelestiaBlobKeyValueBackend _blobs;
        private readonly PrivateKeyValueBackend _private;

        /// <param name="nodeUrl">celestia-node JSON-RPC endpoint (bridge, full or light node with state access).</param>
        /// <param name="authToken">celestia-node auth token with write permission (blob submission).</param>
        /// <param name="namespaceIdHex">10-byte (20 hex characters) namespace ID reserved for this store.</param>
        /// <param name="startHeight">First height that can hold this store's records; the key index is rebuilt by scanning from here,
        /// so the node must retain blobs from this height onwards.</param>
        /// <param name="encryptionKey">32-byte record encryption key.</param>
        /// <param name="writerAddress">Bech32 account whose blobs are records; defaults to the node's submitting account.</param>
        public CelestiaOASIS(string nodeUrl, string authToken, string namespaceIdHex, ulong startHeight, byte[] encryptionKey, string? writerAddress = null)
            : this(CreateBlobBackend(nodeUrl, authToken, namespaceIdHex, startHeight, encryptionKey, writerAddress), encryptionKey) { }

        private CelestiaOASIS(CelestiaBlobKeyValueBackend blobs, byte[] encryptionKey) : this(blobs, new PrivateKeyValueBackend(blobs, encryptionKey, compress: true)) { }

        private CelestiaOASIS(CelestiaBlobKeyValueBackend blobs, PrivateKeyValueBackend backend) : base(backend)
        {
            _blobs = blobs;
            _private = backend;
            ProviderName = "CelestiaOASIS";
            ProviderDescription = "Celestia Modular DA Layer Provider - encrypted append-only key-value records in a blob namespace";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CelestiaOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }

        private static CelestiaBlobKeyValueBackend CreateBlobBackend(string nodeUrl, string authToken, string namespaceIdHex, ulong startHeight, byte[] encryptionKey, string? writerAddress)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl)) throw new ArgumentException("A celestia-node JSON-RPC URL is required.", nameof(nodeUrl));
            if (string.IsNullOrWhiteSpace(authToken)) throw new ArgumentException("A celestia-node auth token with write permission is required.", nameof(authToken));
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Celestia storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            var client = new HttpClient { BaseAddress = new Uri(nodeUrl), Timeout = TimeSpan.FromMinutes(2) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            try { return new CelestiaBlobKeyValueBackend(client, namespaceIdHex, startHeight, writerAddress, ownsClient: true); }
            catch { client.Dispose(); throw; }
        }

        public void Dispose()
        {
            _private.Dispose();
            _blobs.Dispose();
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "CelestiaOASIS: Geolocation not supported."); return r; }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "CelestiaOASIS: Geolocation not supported."); return r; }
    }
}
