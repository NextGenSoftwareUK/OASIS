using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;
using NATS.Net;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.NATSJetStreamOASIS
{
    /// <summary>
    /// Stores OASIS avatars and holons in a NATS JetStream key-value bucket (NATS.Net client).
    /// </summary>
    public class NATSJetStreamOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        public NATSJetStreamOASIS(string natsUrl = "nats://localhost:4222", string bucket = "oasis")
            : base(new NatsKvBackend(natsUrl, bucket))
        {
            ProviderName = "NATSJetStreamOASIS";
            ProviderDescription = "NATS JetStream provider: OASIS avatars and holons in a JetStream key-value bucket.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.NATSJetStreamOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            await ((NatsKvBackend)Backend).DisposeAsync();
            return await base.DeActivateProviderAsync();
        }
    }

    internal sealed class NatsKvBackend : IKeyValueBackend, IAsyncDisposable
    {
        private readonly string _url;
        private readonly string _bucket;
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private NatsConnection _connection;
        private INatsKVStore _store;

        public NatsKvBackend(string url, string bucket)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("A NATS server URL is required.", nameof(url));
            if (string.IsNullOrWhiteSpace(bucket)) throw new ArgumentException("A key-value bucket name is required.", nameof(bucket));
            _url = url;
            _bucket = bucket;
        }

        private async Task<INatsKVStore> StoreAsync(CancellationToken ct)
        {
            if (_store != null) return _store;
            await _connectLock.WaitAsync(ct);
            try
            {
                if (_store != null) return _store;
                _connection = new NatsConnection(new NatsOpts { Url = _url });
                await _connection.ConnectAsync();
                _store = await _connection.CreateKeyValueStoreContext().CreateOrUpdateStoreAsync(new NatsKVConfig(_bucket), ct);
                return _store;
            }
            finally { _connectLock.Release(); }
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var store = await StoreAsync(cancellationToken);
            try
            {
                var entry = await store.GetEntryAsync<string>(key, cancellationToken: cancellationToken);
                return entry.Value;
            }
            catch (NatsKVKeyNotFoundException) { return null; }
            catch (NatsKVKeyDeletedException) { return null; }
        }

        public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => await (await StoreAsync(cancellationToken)).PutAsync(key, value, cancellationToken: cancellationToken);

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => await (await StoreAsync(cancellationToken)).PurgeAsync(key, cancellationToken: cancellationToken);

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var store = await StoreAsync(cancellationToken);
            var keys = new List<string>();
            await foreach (var key in store.GetKeysAsync(cancellationToken: cancellationToken))
                if (key.StartsWith(prefix, StringComparison.Ordinal)) keys.Add(key);
            return keys;
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default) => await StoreAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            _store = null;
            if (_connection != null) { await _connection.DisposeAsync(); _connection = null; }
        }
    }
}
