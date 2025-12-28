using System.Text.Json;
using Common.Contracts.Nft;

using Infrastructure.Extensions.Json;

namespace Infrastructure.ImplementationContract.Nft
{
    public sealed class NftMetadataSerializer(
        IDecentralizedFileStorage fileStorage,
        ILogger<NftMetadataSerializer> logger,
        IOptionsMonitor<IpfsOptions> options) : INftMetadataSerializer
    {
        public async Task<Result<string>> SerializeAsync(Common.DTOs.Nft nft, CancellationToken token = default)
        {
            DateTimeOffset date = DateTimeOffset.UtcNow;
            logger.OperationStarted(nameof(SerializeAsync), date);

            try
            {
                using MemoryStream memoryStream = new();
                await JsonSerializer.SerializeAsync(memoryStream, nft, NftMetadataSerializerContext.Default.Nft, token);

                memoryStream.Seek(offset: 0, SeekOrigin.Begin);

                string uniqueFileName = $"{nft.Name}_{Guid.NewGuid():N}_metadata.json";

                string cid = await fileStorage.CreateAsync(memoryStream, uniqueFileName, token);
                string fileUrl = options.CurrentValue.GatewayUrl + cid;

                logger.OperationCompleted(nameof(SerializeAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return Result<string>.Success(fileUrl);
            }
            catch (Exception ex)
            {
                logger.OperationException(nameof(SerializeAsync), ex.Message);
                logger.OperationCompleted(nameof(SerializeAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return Result<string>.Failure(ResultPatternError.InternalServerError(ex.Message));
            }
        }
    }
}
