using Ipfs;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;

namespace Infrastructure.ImplementationContract;

public sealed class NftPurchaseService(
    ILogger<NftPurchaseService> logger,
    DataContext dbContext,
    IHttpContextAccessor accessor,
    ISolShiftIntegrationService solShiftService) : INftPurchaseService
{
    public async Task<Result<string>> CreateAsync(CreateNftPurchaseRequest request)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CreateAsync), date);
        try
        {
            bool existingUser = await dbContext.Users.AnyAsync(x => x.Id == accessor.GetId());
            if (!existingUser)
                return Result<string>.Failure(ResultPatternError.NotFound(Messages.UserNotFound));

            RwaToken? existingRwa = await dbContext.RwaTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.RwaId);
            if (existingRwa is null)
                return Result<string>.Failure(ResultPatternError.NotFound(Messages.RwaTokenNotFound));

            if (existingRwa.VirtualAccountId is null)
                return Result<string>.Failure(ResultPatternError.BadRequest(Messages.NftAlreadyTransferred));

            VirtualAccount? seller = await dbContext.VirtualAccounts
                .AsNoTrackingWithIdentityResolution()
                .Include(x => x.Network)
                .FirstOrDefaultAsync(x => x.Id == existingRwa.VirtualAccountId);
            if (seller is null)
                return Result<string>.Failure(ResultPatternError.NotFound(Messages.VirtualAccountNotFound));

            WalletLinkedAccount? buyer = await dbContext.WalletLinkedAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PublicKey == request.BuyerPubKey);
            if (buyer is null)
                return Result<string>.Failure(
                    ResultPatternError.NotFound(Messages.CreateNftPurchaseBuyerAccountNotFound));

            if (seller.UserId == buyer.UserId)
                return Result<string>.Failure(
                    ResultPatternError.BadRequest(Messages.CannotPurchaseOwnNft));

            string base58SecretKey = seller.PrivateKey;

            if (seller.Network.Name == Networks.Solana)
            {
                Mnemonic mnemonic = new(seller.SeedPhrase);
                Wallet wallet = new(mnemonic);
                base58SecretKey = Base58.Encode(wallet.Account.PrivateKey);
            }

            const string tokenMint = "91AgzqSfXnCq6AJm5CPPHL3paB25difEJ1TfSnrFKrf";
            Result<CreateTransactionResponse> resultOfTransaction =
                await solShiftService.CreateTransactionAsync(new(
                    buyer.PublicKey,
                    seller.PublicKey,
                    base58SecretKey,
                    existingRwa.MintAccount,
                    existingRwa.Price,
                    tokenMint));
            if (!resultOfTransaction.IsSuccess)
                return Result<string>.Failure(resultOfTransaction.Error);


            string transactionHash = resultOfTransaction.Value.Data.Transaction;
            RwaTokenOwnershipTransfer newObj = new()
            {
                Price = existingRwa.Price,
                CreatedBy = accessor.GetId(),
                CreatedByIp = accessor.GetRemoteIpAddress(),
                BuyerWalletId = buyer.Id,
                SellerWalletId = seller.Id,
                TransactionDate = DateTimeOffset.UtcNow,
                TransactionHash = transactionHash,
                RwaTokenId = existingRwa.Id,
                TransferStatus = RwaTokenOwnershipTransferStatus.InProgress
            };

            await dbContext.RwaTokenOwnershipTransfers.AddAsync(newObj);
            logger.OperationCompleted(nameof(CreateAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync() != 0
                ? Result<string>.Success(transactionHash)
                : Result<string>.Failure(ResultPatternError.InternalServerError(Messages.CreateNftPurchaseFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(CreateAsync), ex.Message);
            logger.OperationCompleted(nameof(CreateAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<string>.Failure(
                ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<string>> SendAsync(SendNftPurchaseTrRequest request)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(SendAsync), date);
        try
        {
            RwaTokenOwnershipTransfer? existingRwaTokenOwner = await dbContext.RwaTokenOwnershipTransfers
                .FirstOrDefaultAsync(x => x.TransactionHash == request.TransactionHash);
            if (existingRwaTokenOwner is null)
                return Result<string>.Failure(
                    ResultPatternError.NotFound(Messages.RwaTokenOwnershipTransferNotFound));

            RwaToken? existingRwaToken =
                await dbContext.RwaTokens
                    .FirstOrDefaultAsync(x => x.Id == existingRwaTokenOwner.RwaTokenId);
            if (existingRwaToken is null)
                return Result<string>.Failure(ResultPatternError.NotFound(Messages.RwaTokenNotFound));

            Result<SendTransactionResponse> resultOfSendTransaction =
                await solShiftService.SendTransactionAsync(new(request.TransactionSignature));
            if (!resultOfSendTransaction.IsSuccess)
                return Result<string>.Failure(resultOfSendTransaction.Error);

            existingRwaToken.VirtualAccountId = null;
            existingRwaToken.WalletLinkedAccountId = existingRwaTokenOwner.BuyerWalletId;
            existingRwaTokenOwner.TransactionSignature = resultOfSendTransaction.Value.Data.TransactionId;
            existingRwaTokenOwner.TransferStatus = RwaTokenOwnershipTransferStatus.Completed;

            logger.OperationCompleted(nameof(SendAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync() != 0
                ? Result<string>.Success(resultOfSendTransaction.Value.Data.TransactionId)
                : Result<string>.Failure(ResultPatternError.InternalServerError(Messages.SendNftPurchaseFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(SendAsync), ex.Message);
            logger.OperationCompleted(nameof(SendAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<string>.Failure(
                ResultPatternError.InternalServerError(ex.Message));
        }
    }
}