namespace Application.Contracts;

public interface ISolShiftIntegrationService
{
    Task<Result<CreateTransactionResponse>> CreateTransactionAsync(CreateTransactionRequest request);
    Task<Result<SendTransactionResponse>> SendTransactionAsync(SendTransactionRequest request);
}