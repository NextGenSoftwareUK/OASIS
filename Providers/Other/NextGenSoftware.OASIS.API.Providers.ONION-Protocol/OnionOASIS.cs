using System;
using System.Net;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ONION_Protocol
{
    /// <summary>
    /// OnionOASIS: OASIS storage reached only through Tor. The provider talks to the repository storage-service contract
    /// (see <see cref="StorageServiceKeyValueBackend"/>) published as a Tor onion service, here the OrbitDB storage service
    /// (<c>Providers/Other/NextGenSoftware.OASIS.API.Providers.OrbitDBOASIS/StorageService</c>). Every request goes through
    /// the configured Tor SOCKS5 proxy, which resolves the <c>.onion</c> address itself, so neither the client's nor the
    /// backend's network location is revealed. Records are compressed and encrypted with the caller's key before they leave
    /// the process, so the onion operator only ever stores ciphertext. Storage semantics are those of the shared
    /// <see cref="KeyValueStorageProviderBase"/>; the onion route is the only transport difference.
    /// </summary>
    public class OnionOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        /// <param name="onionServiceUrl">The onion service URL, e.g. <c>http://&lt;56 chars&gt;.onion</c>.</param>
        /// <param name="serviceToken">The storage service's bearer token.</param>
        /// <param name="encryptionKey">32-byte record encryption key.</param>
        /// <param name="torSocksHost">Host of the Tor client's SOCKS5 port.</param>
        /// <param name="torSocksPort">The Tor client's SOCKS5 port.</param>
        public OnionOASIS(string onionServiceUrl, string serviceToken, byte[] encryptionKey, string torSocksHost = "127.0.0.1", int torSocksPort = 9050)
            : base(CreateBackend(onionServiceUrl, serviceToken, encryptionKey, torSocksHost, torSocksPort))
        {
            ProviderName = "OnionOASIS";
            ProviderDescription = "Tor onion provider: encrypted records in an onion-hosted OASIS storage service, reached only through Tor SOCKS5.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.OnionOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static IKeyValueBackend CreateBackend(string onionServiceUrl, string serviceToken, byte[] encryptionKey, string torSocksHost, int torSocksPort)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Onion storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            if (!Uri.TryCreate(onionServiceUrl, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https")
                || !url.Host.EndsWith(".onion", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("An http(s) .onion service URL is required.", nameof(onionServiceUrl));
            if (string.IsNullOrWhiteSpace(torSocksHost))
                throw new ArgumentException("The Tor SOCKS5 host is required.", nameof(torSocksHost));
            if (torSocksPort is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(torSocksPort), "The Tor SOCKS5 port must be 1-65535.");

            // socks5:// sends the hostname to Tor unresolved, so the .onion name never reaches a DNS resolver.
            var tor = new SocketsHttpHandler { Proxy = new WebProxy($"socks5://{torSocksHost}:{torSocksPort}"), UseProxy = true };
            return new PrivateKeyValueBackend(
                new StorageServiceKeyValueBackend(onionServiceUrl, serviceToken, "onion storage service", "/orbitdb/", tor),
                encryptionKey, compress: true);
        }
    }
}
