using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage
{
    /// <summary>
    /// The four operations a key-value or blob service must provide for <see cref="KeyValueStorageProviderBase"/> to store OASIS data on it.
    /// Implementations must throw on transport or service errors; a missing key is not an error.
    /// </summary>
    public interface IKeyValueBackend
    {
        /// <summary>Returns the value, or null when the key does not exist.</summary>
        Task<string> GetAsync(string key, CancellationToken cancellationToken = default);

        Task PutAsync(string key, string value, CancellationToken cancellationToken = default);

        /// <summary>Deleting a key that does not exist must succeed.</summary>
        Task DeleteAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>Returns every key starting with <paramref name="prefix"/>, following the service's pagination to the end.</summary>
        Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default);

        /// <summary>Makes an authenticated round trip to prove the credentials and store are usable.</summary>
        Task VerifyAsync(CancellationToken cancellationToken = default);
    }
}
