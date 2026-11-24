using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.NostrOASIS
{
    /// <summary>
    /// OASIS provider for the Nostr protocol.
    ///
    /// Storage: avatars, avatar details and holons are NIP-78 application-data events (kind 30078, one replaceable event
    /// per key) authored by the configured key. Event content is compressed and encrypted (relay events are public), every
    /// write is acknowledged by all configured relays, and reads verify each event's BIP-340 signature.
    /// Publishing: <see cref="PublishNoteAsync"/> posts kind-1 text notes. Public query source: see NostrOASIS.Cql.cs.
    /// Signing uses the constant-time libsecp256k1 port (NBitcoin.Secp256k1).
    /// </summary>
    public partial class NostrOASIS : KeyValueStorageProviderBase, IOASISStorageProvider
    {
        private readonly string[] _relayUrls;
        private readonly string? _nsecHex;

        private static readonly string[] DefaultRelays = { "wss://relay.damus.io", "wss://nos.lol", "wss://relay.nostr.band" };

        /// <summary>Relay/publishing instance without storage configuration; storage operations fail explicitly.</summary>
        public NostrOASIS(string[]? relayUrls = null, string? nsecHex = null)
            : this(Relays(relayUrls), nsecHex, new UnconfiguredNostrBackend()) { }

        /// <param name="relayUrls">Relays every storage write must be acknowledged by.</param>
        /// <param name="nsecHex">64-hex secp256k1 secret key that authors the storage events.</param>
        /// <param name="encryptionKey">32-byte record encryption key; event content is compressed then encrypted.</param>
        public NostrOASIS(string[] relayUrls, string nsecHex, byte[] encryptionKey)
            : this(Relays(relayUrls), nsecHex, CreateBackend(Relays(relayUrls), nsecHex, encryptionKey)) { }

        private NostrOASIS(string[] relayUrls, string? nsecHex, IKeyValueBackend backend) : base(backend)
        {
            _relayUrls = relayUrls;
            _nsecHex = nsecHex;
            ProviderName = "NostrOASIS";
            ProviderDescription = "Nostr provider - NIP-78 encrypted application-data storage over WebSocket relays";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.NostrOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        /// <summary>x-only public key (hex) that authors this provider's storage events.</summary>
        public string? StoragePublicKeyHex => _nsecHex == null ? null : new NostrEventSigner(_nsecHex).PublicKeyHex;

        private static string[] Relays(string[]? relayUrls) => relayUrls != null && relayUrls.Length > 0 ? relayUrls : DefaultRelays;

        private static IKeyValueBackend CreateBackend(string[] relays, string nsecHex, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("Nostr storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            return new PrivateKeyValueBackend(new NostrRelayKeyValueBackend(relays, nsecHex), encryptionKey, compress: true);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
            => publicEvents != null ? await ActivateQuerySourceAsync(CancellationToken.None) : await base.ActivateProviderAsync();

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        /// <summary>Publishes a kind-1 text note to every configured relay and requires each relay's OK.</summary>
        public async Task<OASISResult<string>> PublishNoteAsync(string text)
        {
            var result = new OASISResult<string>();
            if (string.IsNullOrEmpty(_nsecHex))
            {
                OASISErrorHandling.HandleError(ref result, "NostrOASIS: a secret key is required to publish notes.");
                return result;
            }
            try
            {
                var evt = new NostrEventSigner(_nsecHex).Sign(1, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), new JsonArray(), text ?? string.Empty);
                var id = evt["id"]!.GetValue<string>();
                var message = Encoding.UTF8.GetBytes(new JsonArray("EVENT", evt).ToJsonString());
                foreach (var relay in _relayUrls)
                {
                    using var socket = new ClientWebSocket();
                    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await socket.ConnectAsync(new Uri(relay), deadline.Token);
                    await socket.SendAsync(message, WebSocketMessageType.Text, true, deadline.Token);
                    var buffer = new byte[64 * 1024];
                    while (true)
                    {
                        var frame = await socket.ReceiveAsync(buffer, deadline.Token);
                        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(buffer, 0, frame.Count));
                        var root = doc.RootElement;
                        if (root[0].GetString() != "OK" || root[1].GetString() != id) continue;
                        if (!root[2].GetBoolean()) throw new InvalidOperationException($"{relay} rejected the note: {root[3].GetString()}");
                        break;
                    }
                }
                result.Result = id;
                result.Message = $"Nostr note published to {_relayUrls.Length} relay(s). EventId={id}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"NostrOASIS.PublishNoteAsync: {ex.Message}", ex);
            }
            return result;
        }

        private sealed class UnconfiguredNostrBackend : IKeyValueBackend
        {
            private static Exception Missing() => new InvalidOperationException(
                "Nostr storage requires explicit relays, a secret key and a record encryption key.");
            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
            public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
            public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Missing());
        }
    }
}
