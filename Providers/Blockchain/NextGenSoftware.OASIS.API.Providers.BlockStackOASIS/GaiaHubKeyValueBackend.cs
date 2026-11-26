using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    /// <summary>Key-value records stored as files in one Gaia bucket. Each key is hex-encoded into a single file name, so any
    /// key maps to a valid Gaia path and listings decode back to the exact key.</summary>
    public sealed class GaiaHubKeyValueBackend : IKeyValueBackend
    {
        private const string Folder = "oasis-kv/";
        private readonly BlockStackClient _client;

        public GaiaHubKeyValueBackend(BlockStackClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            if (!client.IsConfigured) throw new ArgumentException("A Gaia client with a bucket key is required.", nameof(client));
        }

        private static string FileFor(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return Folder + Convert.ToHexString(Encoding.UTF8.GetBytes(key)).ToLowerInvariant();
        }

        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
            => _client.GetTextAsync(FileFor(key), cancellationToken);

        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => _client.PutTextAsync(FileFor(key), value ?? throw new ArgumentNullException(nameof(value)), "text/plain", cancellationToken);

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => await _client.DeleteFileAsync(FileFor(key), cancellationToken).ConfigureAwait(false);

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var names = await _client.ListFileNamesAsync(cancellationToken).ConfigureAwait(false);
            return names.Where(name => name.StartsWith(Folder, StringComparison.Ordinal))
                .Select(name => Encoding.UTF8.GetString(Convert.FromHexString(name[Folder.Length..])))
                .Where(key => key.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal))
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => _client.VerifyAsync(cancellationToken);
    }
}
