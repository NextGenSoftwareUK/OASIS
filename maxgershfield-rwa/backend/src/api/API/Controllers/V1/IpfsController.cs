
namespace API.Controllers.V1;

/// <summary>
/// Controller responsible for handling file operations via IPFS, including file upload, retrieval,
/// and on-the-fly image optimization for NFT logos. Exposes endpoints for interacting with IPFS service.
/// </summary>
[Route($"{ApiAddresses.Base}/files")]
[AllowAnonymous]
public sealed class IpfsController(IIpfsService service, IConfiguration config) : V1BaseController
{
    /// <summary>
    /// Uploads a file to the IPFS network.
    /// </summary>
    /// <param name="request">The file upload request containing the file and metadata.</param>
    /// <param name="token">Cancellation token to abort the upload if needed.</param>
    /// <returns>A response containing the uploaded file's IPFS CID or an error.</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAsync(
        [FromForm] FileUploadRequest request,
        CancellationToken token)
        => (await service.UploadFileAsync(request, token)).ToActionResult();

    /// <summary>
    /// Retrieves the original uncompressed file from IPFS by its CID.
    /// </summary>
    /// <param name="cid">The unique content identifier (CID) of the file in IPFS.</param>
    /// <param name="token">Cancellation token to cancel the operation if needed.</param>
    /// <returns>The raw file bytes with an appropriate content-disposition header.</returns>
    [HttpGet("full/{cid:required}")]
    public async Task<IActionResult> GetAsync(string cid, CancellationToken token)
    {
        Result<byte[]> result = await service.GetFileAsync(cid, token);

        if (!result.IsSuccess)
            return result.ToActionResult();

        return File(result.Value!, MediaTypeNames.Application.Octet, cid);
    }

    /// <summary>
    /// Returns an optimized WebP version of an NFT logo file stored in IPFS.
    /// Automatically compresses the image to reduce size while preserving visual quality.
    /// Includes browser cache headers for performance.
    /// </summary>
    /// <param name="fileId">The unique identifier (CID) of the image file in IPFS.</param>
    /// <param name="token">Cancellation token to cancel the operation if needed.</param>
    /// <returns>Compressed WebP image with cache headers or an error message.</returns>
    [HttpGet("nft-logo/{fileId:required}/optimized")]
    [ResponseCache(CacheProfileName = CacheProfileNames.OptimizedNftLogoCache)]
    public async Task<IActionResult> GetOptimizedNftLogoAsync(string fileId, CancellationToken token)
    {
        Result<byte[]> result = await service.GetFileAsync(fileId, token);

        if (!result.IsSuccess)
            return result.ToActionResult();

        const string quality = "imageOptimizer:quality";

        try
        {
            byte[] optimizedImage =
                await ImageOptimizer.OptimizeImageAsync(result.Value!, config.GetRequiredInt(quality));

            return File(optimizedImage, MediaTypeNames.Image.Webp);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}