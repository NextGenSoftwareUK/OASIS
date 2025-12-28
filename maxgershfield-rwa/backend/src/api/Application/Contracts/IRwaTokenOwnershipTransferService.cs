namespace Application.Contracts;

public interface IRwaTokenOwnershipTransferService
{
    Task<Result<IEnumerable<GetRwaTokenOwnershipTransferResponse>>> GetByIdAsync(Guid id, CancellationToken token = default);
}