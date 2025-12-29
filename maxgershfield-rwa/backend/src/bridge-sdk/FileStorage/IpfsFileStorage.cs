using System.Buffers;
using System.Runtime.CompilerServices;
using Ipfs;
using Ipfs.Http;
using Microsoft.Extensions.Options;

namespace FileStorage;

/// <summary>
///     Provides an implementation of decentralized file storage using the IPFS protocol.
/// </summary>
public sealed class IpfsFileStorage(IpfsClient ipfsClient, IOptions<IpfsOptions> fileOptions) : IDecentralizedFileStorage
{
    /// <inheritdoc />
    public async Task<string> CreateAsync(Stream inputStream, string fileName, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        ArgumentOutOfRangeException.ThrowIfZero(inputStream.Length);

        IFileSystemNode result = await ipfsClient.FileSystem.AddAsync(
            inputStream,
            name: fileName,
            options: fileOptions.Value,
            cancel: token
        );

        return result.Id.ToString();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ReadOnlyMemory<byte>> GetAsync(string cid, int chunkSize = 8192, [EnumeratorCancellation] CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(cid);

        Stream stream = await ipfsClient.PostDownloadAsync("cat", token, cid);

        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            // Fast path, if the file fits in a single buffer, read it once and return immediately.
            if (stream.CanSeek && stream.Length <= chunkSize)
            {
                int bytesRead = await stream.ReadAsync(rentedBuffer.AsMemory(start: 0, chunkSize), token);

                yield return new ReadOnlyMemory<byte>(rentedBuffer, start: 0, bytesRead);
                yield break;
            }

            // Slow path, if the file doesn't fit in a single buffer, read it in chunks.
            while (!token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(rentedBuffer.AsMemory(start: 0, chunkSize), token);
                if (bytesRead is 0)
                    break;

                yield return new ReadOnlyMemory<byte>(rentedBuffer, start: 0, bytesRead);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
            await stream.DisposeAsync();
        }
    }
}
