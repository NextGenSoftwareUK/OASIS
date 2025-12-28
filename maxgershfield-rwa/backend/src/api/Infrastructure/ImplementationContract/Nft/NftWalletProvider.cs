namespace Infrastructure.ImplementationContract.Nft;

public sealed class NftWalletProvider(
    DataContext dbContext,
    ILogger<NftWalletProvider> logger,
    IHttpContextAccessor accessor) : INftWalletProvider
{
    public async ValueTask<Result<WalletKeyPair>> GetWalletAsync(string network, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetWalletAsync), date);

        try
        {
            ArgumentException.ThrowIfNullOrEmpty(network);

            var result = await (from u in dbContext.Users
                                join va in dbContext.VirtualAccounts on u.Id equals va.UserId
                                join n in dbContext.Networks on va.NetworkId equals n.Id
                                where u.Id == accessor.GetId() && n.Name == network
                                select new
                                {
                                    va.PrivateKey,
                                    va.PublicKey,
                                    va.SeedPhrase
                                }).FirstOrDefaultAsync(token);
            if (result is null)
            {
                logger.OperationCompleted(nameof(GetWalletAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return Result<WalletKeyPair>.Failure(ResultPatternError.NotFound(Messages.WalletNotFound));
            }

            logger.OperationCompleted(nameof(GetWalletAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<WalletKeyPair>.Success(new()
            {
                PrivateKey = Encoding.UTF8.GetBytes(result.PrivateKey),
                PublicKey = Encoding.UTF8.GetBytes(result.PublicKey),
                SeedPhrease = Encoding.UTF8.GetBytes(result.SeedPhrase)
            });
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetWalletAsync), ex.Message);
            logger.OperationCompleted(nameof(GetWalletAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<WalletKeyPair>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }
}