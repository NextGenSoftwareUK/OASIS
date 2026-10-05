namespace FileStorage;

/// <summary>
///     Represents a decentralized file storage abstraction.
/// </summary>
public interface IDecentralizedFileStorage
{
    /// <summary>
    ///     Uploads a file stream to the decentralized storage.
    /// </summary>
    /// <param name="inputStream">
    ///     The input stream representing the file to store.
    /// </param>
    /// <param name="fileName">
    ///     The name of the file being uploaded.
    /// </param>
    /// <param name="token">
    ///     Optional cancellation token.
    /// </param>
    /// <returns>The unique hash of the stored file.</returns>
    Task<string> CreateAsync(Stream inputStream, string fileName, CancellationToken token = default);

    /// <summary>
    ///     Retrieves a file from decentralized storage by its hash in a chunked, memory-efficient manner.
    /// </summary>
    /// <param name="fileHash">
    ///     The hash of the file to retrieve.
    /// </param>
    /// <param name="chunkSize">
    ///     The size of each chunk to read in bytes. Default is 8192.
    /// </param>
    /// <param name="token">
    ///     Optional cancellation token.
    /// </param>
    /// <returns>An async enumerable of file content chunks.</returns>
    IAsyncEnumerable<ReadOnlyMemory<byte>> GetAsync(string fileHash, int chunkSize = 8192, CancellationToken token = default);
}
