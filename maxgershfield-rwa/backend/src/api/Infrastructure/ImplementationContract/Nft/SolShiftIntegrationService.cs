namespace Infrastructure.ImplementationContract.Nft;

public sealed class SolShiftIntegrationService(
    ILogger<SolShiftIntegrationService> logger,
    IHttpClientFactory httpClientFactory) : ISolShiftIntegrationService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientNames.SolShiftClient);

    public async Task<Result<CreateTransactionResponse>> CreateTransactionAsync(CreateTransactionRequest request)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CreateTransactionAsync), date);

        string url = _httpClient.BaseAddress + "shift/create-transaction";
        try
        {
            Result<CreateTransactionResponse> response = await HttpClientHelper
                .PostAsync<CreateTransactionRequest, CreateTransactionResponse>(_httpClient, url, request);

            logger.OperationCompleted(nameof(CreateTransactionAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return response.IsSuccess
                ? Result<CreateTransactionResponse>.Success(response.Value)
                : Result<CreateTransactionResponse>.Failure(response.Error);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(CreateTransactionAsync), ex.Message);
            logger.OperationCompleted(nameof(CreateTransactionAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CreateTransactionResponse>.Failure(
                ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<SendTransactionResponse>> SendTransactionAsync(SendTransactionRequest request)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(SendTransactionAsync), date);
        string url = _httpClient.BaseAddress + "shift/send-transaction";

        try
        {
            Result<SendTransactionResponse> response = await HttpClientHelper
                .PostAsync<SendTransactionRequest, SendTransactionResponse>(_httpClient, url, request);

            logger.OperationCompleted(nameof(SendTransactionAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return response.IsSuccess
                ? Result<SendTransactionResponse>.Success(response.Value)
                : Result<SendTransactionResponse>.Failure(response.Error);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(SendTransactionAsync), ex.Message);
            logger.OperationCompleted(nameof(SendTransactionAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<SendTransactionResponse>.Failure(
                ResultPatternError.InternalServerError(ex.Message));
        }
    }
}