namespace Infrastructure.ImplementationContract;

/// <summary>
/// Concrete implementation of <see cref="IIpfsService"/> for handling IPFS file operations.
/// Utilizes a decentralized file storage provider to upload and retrieve files,
/// and includes structured logging for observability.
/// </summary>
public sealed class IpfsService(
    ILogger<IpfsService> logger,
    IDecentralizedFileStorage fileStorage,
    IOptionsMonitor<IpfsOptions> options) : IIpfsService
{
    /// <summary>
    /// Uploads a file to the IPFS network.
    /// Performs validations, uploads the file via <see cref="IDecentralizedFileStorage"/>,
    /// and returns the resulting CID and accessible IPFS URL.
    /// </summary>
    /// <param name="request">The file upload request, containing file data and metadata.</param>
    /// <param name="token">Optional cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> wrapping <see cref="FileUploadResponse"/> with the file's CID and URL on success,
    /// or an appropriate error on failure.
    /// </returns>
    public async Task<Result<FileUploadResponse>> UploadFileAsync(FileUploadRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(UploadFileAsync), date);

        if (request.File.Length == 0)
            return Result<FileUploadResponse>.Failure(ResultPatternError.BadRequest(Messages.IpfsUploadEmptyFile));

        if (!Enum.IsDefined(typeof(FileType), request.Type))
            return Result<FileUploadResponse>.Failure(
                ResultPatternError.UnsupportedMediaType(Messages.IpfsInvalidTypeFile));

        try
        {
            await using Stream stream = request.File.OpenReadStream();
            string fileHash = await fileStorage.CreateAsync(stream, request.File.FileName, token);
            string fileUrl = options.CurrentValue.GatewayUrl + fileHash;

            logger.OperationCompleted(nameof(UploadFileAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<FileUploadResponse>.Success(
                new(Messages.IpfsSuccessMessageFile,
                    new(fileHash, fileUrl)));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(UploadFileAsync), ex.Message);
            logger.OperationCompleted(nameof(UploadFileAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<FileUploadResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    /// <summary>
    /// Retrieves a file from the IPFS network using its CID.
    /// Validates the CID format, fetches the file in chunks, and returns it as a byte array.
    /// </summary>
    /// <param name="cid">The unique content identifier of the file.</param>
    /// <param name="token">Optional cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> wrapping the file as a byte array, or an error if not found or invalid.
    /// </returns>
    public async Task<Result<byte[]>> GetFileAsync(string cid, CancellationToken token = default)
    {
        DateTimeOffset started = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetFileAsync), started);

        try
        {
            if (!IpfsValidator.IsValidCidV0(cid))
                return Result<byte[]>.Failure(ResultPatternError.BadRequest(Messages.IpfsInvalidFormatCid));

            IAsyncEnumerable<ReadOnlyMemory<byte>> fileChunks = fileStorage.GetAsync(cid, token: token);

            await using MemoryStream memoryStream = new();

            await foreach (ReadOnlyMemory<byte> chunk in fileChunks)
            {
                await memoryStream.WriteAsync(chunk, token);
            }

            if (memoryStream.Length == 0)
                return Result<byte[]>.Failure(ResultPatternError.NotFound(Messages.IpfsFileNotFound));

            byte[] resultBytes = memoryStream.ToArray();

            logger.OperationCompleted(nameof(GetFileAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - started);
            return Result<byte[]>.Success(resultBytes);
        }
        catch (FileNotFoundException fnfEx)
        {
            logger.OperationException(nameof(GetFileAsync), fnfEx.Message);
            return Result<byte[]>.Failure(ResultPatternError.NotFound(fnfEx.Message));
        }
        catch (IOException ioEx)
        {
            logger.OperationException(nameof(GetFileAsync), ioEx.Message);
            return Result<byte[]>.Failure(ResultPatternError.InternalServerError(ioEx.Message));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetFileAsync), ex.Message);
            return Result<byte[]>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }
}