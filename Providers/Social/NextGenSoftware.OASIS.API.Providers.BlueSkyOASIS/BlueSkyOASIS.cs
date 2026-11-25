using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS
{
    /// <summary>
    /// OASIS provider for the AT Protocol (BlueSky).
    ///
    /// Storage: avatars, avatar details and holons are records of the <c>one.oasis.record</c> collection in the
    /// logged-in account's PDS repository (putRecord/getRecord/deleteRecord/listRecords). Repository records are public,
    /// so keys and values are compressed and encrypted with an explicit 32-byte key before they reach the PDS.
    /// Publishing: <see cref="PublishPostAsync"/> creates <c>app.bsky.feed.post</c> records and
    /// <see cref="DeletePostAsync"/> removes them.
    /// Authentication: <c>com.atproto.server.createSession</c> with an app password (or account password); expired
    /// access tokens are renewed with <c>refreshSession</c>.
    /// </summary>
    public class BlueSkyOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IDisposable
    {
        public const string DefaultPds = "https://bsky.social";
        public const string PostCollection = "app.bsky.feed.post";
        private readonly AtProtoSessionClient? _client;
        private readonly IDisposable? _ownedBackend;

        /// <summary>
        /// Publishing-only instance (no storage configuration): storage operations fail explicitly.
        /// Credentials default to BLUESKY_IDENTIFIER / BLUESKY_APP_PASSWORD.
        /// </summary>
        public BlueSkyOASIS(string? identifier = null, string? appPassword = null, string pdsBase = DefaultPds)
            : this(PublishingClient(identifier ?? Environment.GetEnvironmentVariable("BLUESKY_IDENTIFIER"),
                appPassword ?? Environment.GetEnvironmentVariable("BLUESKY_APP_PASSWORD"), pdsBase), new UnconfiguredBackend(), null) { }

        /// <param name="pdsUrl">The PDS that hosts the account repository, e.g. https://bsky.social or a self-hosted PDS.</param>
        /// <param name="identifier">Account handle, DID or email.</param>
        /// <param name="password">App password (or account password).</param>
        /// <param name="encryptionKey">32-byte record encryption key; record keys and values are encrypted before storage.</param>
        public BlueSkyOASIS(Uri pdsUrl, string identifier, string password, byte[] encryptionKey)
            : this(new AtProtoSessionClient(pdsUrl, identifier, password), encryptionKey) { }

        private BlueSkyOASIS(AtProtoSessionClient client, byte[] encryptionKey)
            : this(client, CreateBackend(client, encryptionKey)) { }

        private BlueSkyOASIS(AtProtoSessionClient client, PrivateKeyValueBackend backend) : this(client, backend, backend) { }

        private BlueSkyOASIS(AtProtoSessionClient? client, IKeyValueBackend backend, IDisposable? ownedBackend) : base(backend)
        {
            _client = client;
            _ownedBackend = ownedBackend;
            ProviderName = "BlueSkyOASIS";
            ProviderDescription = "BlueSky / AT Protocol provider - encrypted records in the account's PDS repository";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BlueSkyOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static AtProtoSessionClient? PublishingClient(string? identifier, string? password, string pdsBase)
            => string.IsNullOrWhiteSpace(identifier) || string.IsNullOrEmpty(password)
                ? null : new AtProtoSessionClient(new Uri(pdsBase), identifier, password);

        private static PrivateKeyValueBackend CreateBackend(AtProtoSessionClient client, byte[] encryptionKey)
        {
            if (encryptionKey == null || encryptionKey.Length != 32)
                throw new ArgumentException("BlueSky storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
            return new PrivateKeyValueBackend(new AtProtoRepoKeyValueBackend(client), encryptionKey, compress: true);
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var result = await base.DeActivateProviderAsync();
            if (_client != null) await _client.ResetAsync();
            return result;
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        /// <summary>Creates an <c>app.bsky.feed.post</c> record and returns its AT-URI.</summary>
        public async Task<OASISResult<string>> PublishPostAsync(string text, CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<string>();
            try
            {
                var client = RequireClient();
                if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Post text is required.", nameof(text));
                var did = await client.GetDidAsync(cancellationToken);
                var created = await client.ProcedureAsync("com.atproto.repo.createRecord", new JsonObject
                {
                    ["repo"] = did,
                    ["collection"] = PostCollection,
                    ["record"] = new JsonObject
                    {
                        ["$type"] = PostCollection, ["text"] = text, ["createdAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    }
                }, cancellationToken);
                var uri = created.TryGetProperty("uri", out var value) ? value.GetString() : null;
                if (uri == null || !uri.StartsWith($"at://{did}/{PostCollection}/", StringComparison.Ordinal))
                    throw new InvalidDataException("The PDS did not acknowledge the post with its AT-URI.");
                result.Result = uri;
                result.Message = $"BlueSkyOASIS: post created at {uri}.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BlueSkyOASIS.PublishPostAsync: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>Deletes one of the account's own posts by AT-URI (at://did/app.bsky.feed.post/rkey).</summary>
        public async Task<OASISResult<bool>> DeletePostAsync(string atUri, CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<bool>();
            try
            {
                var client = RequireClient();
                var did = await client.GetDidAsync(cancellationToken);
                var prefix = $"at://{did}/{PostCollection}/";
                if (atUri == null || !atUri.StartsWith(prefix, StringComparison.Ordinal) || atUri.Length == prefix.Length)
                    throw new ArgumentException($"Expected an AT-URI of one of this account's posts ({prefix}<rkey>).", nameof(atUri));
                await client.ProcedureAsync("com.atproto.repo.deleteRecord", new JsonObject
                {
                    ["repo"] = did, ["collection"] = PostCollection, ["rkey"] = atUri[prefix.Length..]
                }, cancellationToken);
                result.Result = true;
                result.Message = $"BlueSkyOASIS: post {atUri} deleted.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"BlueSkyOASIS.DeletePostAsync: {ex.Message}", ex);
            }
            return result;
        }

        private AtProtoSessionClient RequireClient() => _client ?? throw new InvalidOperationException(
            "BlueSkyOASIS needs account credentials (identifier and app password) to publish.");

        // ─── IOASISNETProvider ────────────────────────────────────────────────────

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "BlueSkyOASIS: Geolocation is not supported.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "BlueSkyOASIS: Geolocation is not supported.");
            return result;
        }

        public void Dispose()
        {
            _ownedBackend?.Dispose();
            _client?.Dispose();
            GC.SuppressFinalize(this);
        }

        private sealed class UnconfiguredBackend : IKeyValueBackend
        {
            private static Exception Missing() => new InvalidOperationException(
                "BlueSky storage requires an explicit PDS URL, account credentials and a record encryption key.");
            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
            public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Missing());
            public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
            public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Missing());
        }
    }
}
