namespace SolanaBridge;

/// <summary>
/// Infrastructure-level service that handles interaction with the Solana blockchain.
/// This implementation acts as the technical bridge ("SolanaBridge") between your application and the Solana network,
/// providing methods to create and restore wallets, check balances, transfer funds, and verify transaction statuses.
/// 
/// This service follows the <see cref="ISolanaBridge"/> contract and is typically used in the infrastructure layer
/// of the application to encapsulate low-level Solana-specific logic, ensuring separation of concerns and 
/// clean architecture principles.
/// </summary>

public sealed class SolanaBridge(
    ILogger<SolanaBridge> logger,
    SolanaTechnicalAccountBridgeOptions options,
    IRpcClient rpcClient) : ISolanaBridge
{
    private const decimal Lamports = 1_000_000_000m;

    private ulong ConvertSolToLamports(decimal amountInSol)
        => (ulong)(amountInSol * Lamports);

    /// <summary>
    /// Retrieves the balance of a given Solana account in SOL.
    /// </summary>
    public async Task<Result<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetAccountBalanceAsync), date);
        try
        {
            RequestResult<ResponseValue<AccountInfo>> result = await rpcClient.GetAccountInfoAsync(accountAddress);

            if (result.WasSuccessful && result.Result.Value?.Lamports != null)
            {
                decimal balanceInSol = result.Result.Value.Lamports / Lamports;
                logger.OperationCompleted(nameof(GetAccountBalanceAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<decimal>.Success(balanceInSol);
            }

            logger.OperationCompleted(nameof(GetAccountBalanceAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<decimal>.Success();
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetAccountBalanceAsync), ex.Message);
            logger.OperationCompleted(nameof(GetAccountBalanceAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<decimal>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    /// <summary>
    /// Creates a new Solana account with a seed phrase and retrieves its details.
    /// </summary>
    public async Task<Result<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CreateAccountAsync), date);
        try
        {
            Mnemonic mnemonic = new(WordList.English, WordCount.Twelve);
            Wallet wallet = new(mnemonic);

            string seedPhrase = string.Join(" ", mnemonic.Words);
            string publicKey = wallet.Account.PublicKey;
            string privateKey = Convert.ToBase64String(wallet.Account.PrivateKey);

            logger.OperationCompleted(nameof(CreateAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return await Task.FromResult(Result<(string PublicKey, string PrivateKey, string SeedPhrase)>.Success(
                (publicKey,
                    privateKey,
                    seedPhrase)));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(CreateAccountAsync), ex.Message);
            logger.OperationCompleted(nameof(CreateAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return await Task.FromResult(Result<(string PublicKey, string PrivateKey, string SeedPhrase)>.Failure(
                ResultPatternError.InternalServerError(ex.Message)));
        }
    }

    /// <summary>
    /// Restores a Solana account using a seed phrase.
    /// </summary>
    public async Task<Result<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(RestoreAccountAsync), date);
        try
        {
            if (!SeedPhraseValidator.IsValidSeedPhrase(seedPhrase))
            {
                logger.OperationCompleted(nameof(RestoreAccountAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return await Task.FromResult(Result<(string PublicKey, string PrivateKey)>.Failure(
                    ResultPatternError.BadRequest(Messages.RestoreAccountIncorrectFormat)));
            }

            Mnemonic mnemonic = new(seedPhrase);
            Wallet wallet = new(mnemonic);

            string publicKey = wallet.Account.PublicKey;
            string privateKey = Convert.ToBase64String(wallet.Account.PrivateKey);

            return await Task.FromResult(
                Result<(string PublicKey, string PrivateKey)>.Success((publicKey, privateKey)));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(RestoreAccountAsync), ex.Message);
            logger.OperationCompleted(nameof(RestoreAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return await Task.FromResult(Result<(string PublicKey, string PrivateKey)>.Failure(
                ResultPatternError.InternalServerError(ex.Message)));
        }
    }

    /// <summary>
    /// Executes a withdrawal from a client account to the technical account.
    /// </summary>
    public async Task<Result<TransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress,
        string senderPrivateKey)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(WithdrawAsync), date);

        if (senderPrivateKey == options.PublicKey)
        {
            logger.OperationCompleted(nameof(WithdrawAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<TransactionResponse>.Failure(
                ResultPatternError.BadRequest(Messages.SelfTransaction));
        }

        ulong lamports = ConvertSolToLamports(amount);

        Account technicalAccount = new(
            Convert.FromBase64String(options.PrivateKey),
            new PublicKey(options.PublicKey)
        );
        Account clientAccount = new(
            Convert.FromBase64String(senderPrivateKey),
            new PublicKey(senderAccountAddress)
        );

        logger.OperationCompleted(nameof(WithdrawAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - date);
        return await ExecuteTransactionAsync(clientAccount, technicalAccount, lamports);
    }

    /// <summary>
    /// Executes a deposit from the technical account to a client account.
    /// </summary>
    public async Task<Result<TransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(DepositAsync), date);

        if (receiverAccountAddress == options.PublicKey)
        {
            logger.OperationCompleted(nameof(DepositAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<TransactionResponse>.Failure(
                ResultPatternError.BadRequest(Messages.SelfTransaction));
        }

        ulong lamports = ConvertSolToLamports(amount);

        Account technicalAccount = new(
            Convert.FromBase64String(options.PrivateKey),
            new PublicKey(options.PublicKey)
        );
        PublicKey receiverAccount = new(receiverAccountAddress);

        logger.OperationCompleted(nameof(DepositAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - date);
        return await ExecuteTransactionAsync(technicalAccount, receiverAccount, lamports);
    }


    /// <summary>
    /// Executes a transaction between two accounts.
    /// </summary>
    private async Task<Result<TransactionResponse>> ExecuteTransactionAsync(Account sender, PublicKey receiver,
        ulong lamports)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(ExecuteTransactionAsync), date);

        try
        {
            Result<decimal> balanceResult = await GetAccountBalanceAsync(sender.PublicKey);
            if (!balanceResult.IsSuccess)
            {
                logger.OperationCompleted(nameof(ExecuteTransactionAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<TransactionResponse>.Failure(balanceResult.Error);
            }

            decimal balanceSol = balanceResult.Value;
            if (lamports / Lamports >= balanceSol)
            {
                return Result<TransactionResponse>.Failure(
                    ResultPatternError.BadRequest(Messages.InsufficientFundsInTechAccount),
                    new(
                        string.Empty,
                        null,
                        false,
                        Messages.InvalidAmount,
                        BridgeTransactionStatus.InsufficientFunds));
            }

            RequestResult<ResponseValue<LatestBlockHash>> latestBlockHashResult =
                await rpcClient.GetLatestBlockHashAsync();
            if (!latestBlockHashResult.WasSuccessful || latestBlockHashResult.Result?.Value == null)
            {
                logger.OperationCompleted(nameof(ExecuteTransactionAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<TransactionResponse>.Failure(
                    ResultPatternError.InternalServerError(latestBlockHashResult.Reason));
            }

            string recentBlockHash = latestBlockHashResult.Result.Value.Blockhash;

            Transaction transaction = new()
            {
                RecentBlockHash = recentBlockHash,
                FeePayer = sender.PublicKey
            };

            transaction.Add(SystemProgram.Transfer(sender.PublicKey, receiver, lamports));

            transaction.Sign(sender);

            RequestResult<string> result = await rpcClient.SendTransactionAsync(transaction.Serialize());
            if (!result.WasSuccessful)
            {
                logger.OperationCompleted(nameof(ExecuteTransactionAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);

                return result.HttpStatusCode switch
                {
                    HttpStatusCode.InternalServerError => Result<TransactionResponse>.Failure(
                        ResultPatternError.InternalServerError(result.Reason)),
                    HttpStatusCode.BadRequest => Result<TransactionResponse>.Failure(
                        ResultPatternError.BadRequest(result.Reason)),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            logger.OperationCompleted(nameof(ExecuteTransactionAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<TransactionResponse>.Success(new TransactionResponse(
                result.Result,
                result.Result,
                true,
                null,
                BridgeTransactionStatus.Completed
            ));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(ExecuteTransactionAsync), ex.Message);
            logger.OperationCompleted(nameof(ExecuteTransactionAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<TransactionResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    /// <summary>
    /// Retrieves the status of a Solana transaction using its hash.
    /// </summary>
    public async Task<Result<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetTransactionStatusAsync), date);

        try
        {
            Commitment commitment = Commitment.Confirmed;
            RequestResult<TransactionMetaSlotInfo> transactionStatusResult =
                await rpcClient.GetTransactionAsync(transactionHash, commitment);

            if (transactionStatusResult == null)
            {
                logger.OperationCompleted(nameof(GetTransactionStatusAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<BridgeTransactionStatus>.Failure(
                    ResultPatternError.InternalServerError(Messages.TransactionFailed));
            }

            if (!transactionStatusResult.WasSuccessful || transactionStatusResult.Result == null)
            {
                if (transactionStatusResult.HttpStatusCode == HttpStatusCode.BadRequest)
                {
                    logger.OperationCompleted(nameof(GetTransactionStatusAsync), DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow - date);

                    return Result<BridgeTransactionStatus>.Failure(
                        ResultPatternError.InternalServerError(Messages.TransactionFailed));
                }
            }

            BridgeTransactionStatus status = transactionStatusResult.Result?.Meta?.Error == null
                ? BridgeTransactionStatus.Completed
                : BridgeTransactionStatus.Canceled;

            logger.OperationCompleted(nameof(GetTransactionStatusAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<BridgeTransactionStatus>.Success(status);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetTransactionStatusAsync), ex.Message);
            logger.OperationCompleted(nameof(GetTransactionStatusAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<BridgeTransactionStatus>.Failure(
                ResultPatternError.InternalServerError(ex.Message));
        }
    }
}