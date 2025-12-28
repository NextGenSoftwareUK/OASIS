namespace Infrastructure.ImplementationContract;

public sealed class RwaTokenOwnershipTransferService(
    DataContext dbContext) : IRwaTokenOwnershipTransferService
{
    public async Task<Result<IEnumerable<GetRwaTokenOwnershipTransferResponse>>>
        GetByIdAsync(Guid id, CancellationToken token = default)
        => Result<IEnumerable<GetRwaTokenOwnershipTransferResponse>>.Success(await dbContext.RwaTokenOwnershipTransfers
            .AsNoTracking()
            .Include(x => x.BuyerWallet)
            .Include(x => x.SellerWallet)
            .Where(x => x.RwaTokenId == id && x.TransferStatus == RwaTokenOwnershipTransferStatus.Completed)
            .OrderBy(x => x.Id)
            .Select(x => x.ToRead()).ToListAsync(token));
}