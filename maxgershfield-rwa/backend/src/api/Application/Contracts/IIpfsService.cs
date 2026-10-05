namespace Application.Contracts;

/// <summary>
/// Contract for IPFS-related operations such as uploading and retrieving files.
/// Abstracts interaction with the IPFS layer to ensure loose coupling and testability.
/// </summary>
public interface IIpfsService
{
    /// <summary>
    /// Uploads a file to the IPFS network and returns its content identifier (CID).
    /// </summary>
    /// <param name="request">The file upload request containing file content and metadata.</param>
    /// <param name="token">Cancellation token for aborting the operation if needed.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> wrapping <see cref="FileUploadResponse"/>, containing the file's CID and related info.
    /// </returns>
    Task<Result<FileUploadResponse>> UploadFileAsync(FileUploadRequest request, CancellationToken token = default);

    /// <summary>
    /// Retrieves a file from the IPFS network by its CID.
    /// </summary>
    /// <param name="cid">The unique content identifier of the file to retrieve.</param>
    /// <param name="token">Cancellation token for aborting the operation if needed.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> wrapping the raw file data as a byte array.
    /// </returns>
    Task<Result<byte[]>> GetFileAsync(string cid, CancellationToken token = default);
}