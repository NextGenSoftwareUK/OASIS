using System.Globalization;
using TransactionBuilder = RadixEngineToolkit.TransactionBuilder;

namespace RadixBridge;

/// <summary>
/// Represents a bridge for interacting with the Radix network.
/// Provides functionality to manage accounts, check balances, 
/// and execute transactions (withdraw/deposit) with detailed logging.
/// </summary>
public sealed class RadixBridge : IRadixBridge
{
    private readonly RadixTechnicalAccountBridgeOptions _options;
    private readonly HttpClient _httpClient;
    private readonly string _network;
    private readonly Address _xrdAddress;
    private readonly ILogger<RadixBridge> _logger;

    /// <summary>
    /// Initializes a new instance of the RadixBridge class.
    /// Sets the network and XRD address based on the provided options.
    /// </summary>
    public RadixBridge(RadixTechnicalAccountBridgeOptions options, HttpClient httpClient, ILogger<RadixBridge> logger)
    {
        _logger = logger;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (options.NetworkId == (byte)NetworkType.Main)
        {
            _network = RadixBridgeHelper.MainNet;
            _xrdAddress = new Address(RadixBridgeHelper.MainNetXrdAddress);
        }
        else
        {
            _network = RadixBridgeHelper.StokeNet;
            _xrdAddress = new Address(RadixBridgeHelper.StokeNetXrdAddress);
        }
    }

    /// <summary>
    /// Retrieves the balance of an account.
    /// </summary>
    public async Task<Result<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        _logger.OperationStarted(nameof(GetAccountBalanceAsync), date);

        var data = new
        {
            network = _network,
            account_address = accountAddress,
            resource_address = _xrdAddress.AddressString()
        };

        Result<AccountFungibleResourceBalanceDto?> result =
            await HttpClientHelper.PostAsync<object, AccountFungibleResourceBalanceDto>(
                _httpClient,
                $"{_options.HostUri}/core/lts/state/account-fungible-resource-balance",
                data,
                token
            );

        _logger.OperationCompleted(nameof(GetAccountBalanceAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return result.Value != null
            ? Result<decimal>.Success(decimal.Parse(result.Value?.FungibleResourceBalance.Amount ?? "0"))
            : Result<decimal>.Success();
    }

    /// <summary>
    /// Creates a new Radix account with a randomly generated seed phrase.
    /// </summary>
    public Result<(PublicKey PublicKey, PrivateKey PrivateKey, string SeedPhrase)> CreateAccountAsync(
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        _logger.OperationStarted(nameof(CreateAccountAsync), date);
        token.ThrowIfCancellationRequested();

        Mnemonic mnemonic = new(Wordlist.English, WordCount.TwentyFour);
        PrivateKey privateKey = RadixBridgeHelper.GetPrivateKey(mnemonic);

        _logger.OperationCompleted(nameof(CreateAccountAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return Result<(PublicKey PublicKey, PrivateKey PrivateKey, string SeedPhrase)>
            .Success((privateKey.PublicKey(), privateKey, mnemonic.ToString()));
    }

    /// <summary>
    /// Restores an account using a seed phrase.
    /// </summary>
    public Result<(PublicKey PublicKey, PrivateKey PrivateKey)> RestoreAccountAsync(string seedPhrase,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        _logger.OperationStarted(nameof(RestoreAccountAsync), date);
        token.ThrowIfCancellationRequested();

        if (!SeedPhraseValidator.IsValidSeedPhrase(seedPhrase))
        {
            _logger.OperationCompleted(nameof(RestoreAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<(PublicKey PublicKey, PrivateKey PrivateKey)>
                .Failure(ResultPatternError.BadRequest(Messages.RestoreAccountIncorrectFormat));
        }

        Mnemonic mnemonic = new(seedPhrase);
        PrivateKey privateKey = RadixBridgeHelper.GetPrivateKey(mnemonic);

        _logger.OperationCompleted(nameof(RestoreAccountAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return Result<(PublicKey PublicKey, PrivateKey PrivateKey)>
            .Success((privateKey.PublicKey(), privateKey));
    }

    /// <summary>
    /// Retrieves the address of an account or identity based on the public key.
    /// </summary>
    public Result<string> GetAddressAsync(PublicKey publicKey, AddressType addressType, NetworkType networkType,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        _logger.OperationStarted(nameof(GetAddressAsync), date);
        token.ThrowIfCancellationRequested();

        byte network = (byte)networkType;

        _logger.OperationCompleted(nameof(GetAddressAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return addressType switch
        {
            AddressType.Account => Result<string>.Success(Address.VirtualAccountAddressFromPublicKey(publicKey, network)
                .AddressString()),
            AddressType.Identity => Result<string>.Success(Address
                .VirtualIdentityAddressFromPublicKey(publicKey, network).AddressString()),
            _ => Result<string>.Failure(ResultPatternError.BadRequest(Messages.RadixGetAddressInvalidType))
        };
    }

    /// <summary>
    /// Withdraws a specified amount from an account.
    /// </summary>
    public async Task<Result<TransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress,
        string senderPrivateKey)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        _logger.OperationStarted(nameof(WithdrawAsync), date);
        if (senderAccountAddress == _options.AccountAddress)
        {
            _logger.OperationCompleted(nameof(WithdrawAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<TransactionResponse>.Failure(
                ResultPatternError.BadRequest(Messages.SelfTransaction));
        }

        _logger.OperationCompleted(nameof(WithdrawAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return await ExecuteTransactionAsync(
            new(Encoders.Hex.DecodeData(senderPrivateKey), Curve.ED25519),
            _options.AccountAddress, amount);
    }

    /// <summary>
    /// Deposits a specified amount to an account.
    /// </summary>
    public async Task<Result<TransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        _logger.OperationStarted(nameof(DepositAsync), date);
        if (receiverAccountAddress == _options.AccountAddress)
        {
            _logger.OperationCompleted(nameof(DepositAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<TransactionResponse>.Failure(
                ResultPatternError.BadRequest(Messages.SelfTransaction));
        }

        _logger.OperationCompleted(nameof(DepositAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        return await ExecuteTransactionAsync(new(Encoders.Hex.DecodeData(_options.PrivateKey), Curve.ED25519),
            receiverAccountAddress, amount);
    }

    /// <summary>
    /// Executes a transaction (withdrawal or deposit) by building and submitting the transaction manifest.
    /// Detailed logging is provided at each step.
    /// </summary>
    private async Task<Result<TransactionResponse>> ExecuteTransactionAsync(PrivateKey sender, string receiver,
        decimal amount)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        _logger.OperationStarted(nameof(ExecuteTransactionAsync), date);
        const int fee = 10;
        try
        {
            Address senderAddress = Address.VirtualAccountAddressFromPublicKey(sender.PublicKey(),
                _options.NetworkId);
            Address receiverAddress = new(receiver);

            Result<decimal> balanceResult = await GetAccountBalanceAsync(senderAddress.AddressString());
            if (!balanceResult.IsSuccess)
            {
                _logger.OperationCompleted(nameof(ExecuteTransactionAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<TransactionResponse>.Failure(balanceResult.Error);
            }

            if (amount + fee >= balanceResult.Value)
            {
                _logger.OperationCompleted(nameof(ExecuteTransactionAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<TransactionResponse>.Failure(
                    ResultPatternError.BadRequest(Messages.InsufficientFunds),
                    new(
                        string.Empty,
                        null,
                        false,
                        Messages.InvalidAmount,
                        BridgeTransactionStatus.InsufficientFunds));
            }


            decimal roundedAmount = Math.Round(amount, 18, MidpointRounding.AwayFromZero);
            string formattedAmount =
                roundedAmount.ToString("F18", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');

            using TransactionManifest manifest = new ManifestBuilder()
                .AccountLockFeeAndWithdraw(senderAddress, new($"{fee}"), _xrdAddress, new(formattedAmount))
                .TakeFromWorktop(_xrdAddress, new(formattedAmount), new("xrdBucket"))
                .AccountTryDepositOrAbort(receiverAddress, new("xrdBucket"), null)
                .Build(_options.NetworkId);

            manifest.StaticallyValidate();

            ulong currentEpoch = (await _httpClient.GetConstructionMetadata(_options))?.CurrentEpoch ?? 0;

            using NotarizedTransaction transaction = new TransactionBuilder()
                .Header(new TransactionHeader(
                    networkId: _options.NetworkId,
                    startEpochInclusive: currentEpoch,
                    endEpochExclusive: currentEpoch + 50,
                    nonce: RadixBridgeHelper.RandomNonce(),
                    notaryPublicKey: sender.PublicKey(),
                    notaryIsSignatory: true,
                    tipPercentage: 0
                ))
                .Manifest(manifest)
                .Message(new Message.None())
                .NotarizeWithPrivateKey(sender);

            var data = new
            {
                network = _network,
                notarized_transaction_hex = Encoders.Hex.EncodeData(transaction.Compile()),
                force_recalculate = true
            };

            Result<TransactionSubmitResponse?> response =
                await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
                    _httpClient,
                    $"{_options.HostUri}/core/lts/transaction/submit",
                    data
                );

            _logger.OperationCompleted(nameof(ExecuteTransactionAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<TransactionResponse>.Success(new TransactionResponse(
                transaction.IntentHash().AsStr(),
                response.Value?.Duplicate.ToString(),
                response.Value != null,
                response.Value == null ? Messages.TransactionFailed : null,
                response.Value != null ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
            ));
        }
        catch (Exception e)
        {
            _logger.OperationException(nameof(ExecuteTransactionAsync), e.Message);
            _logger.OperationCompleted(nameof(ExecuteTransactionAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<TransactionResponse>.Failure(ResultPatternError.InternalServerError(e.Message));
        }
    }

    /// <summary>
    /// Retrieves the status of a submitted transaction.
    /// </summary>
    public async Task<Result<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        _logger.OperationStarted(nameof(GetTransactionStatusAsync), date);

        var data = new
        {
            network = _network,
            intent_hash = transactionHash
        };

        Result<TransactionStatusResponse?> response =
            await HttpClientHelper.PostAsync<object, TransactionStatusResponse>(
                _httpClient,
                $"{_options.HostUri}/core/transaction/status",
                data,
                token
            );

        if (response.Value is null)
        {
            _logger.OperationCompleted(nameof(GetTransactionStatusAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<BridgeTransactionStatus>.Failure(
                ResultPatternError.InternalServerError(Messages.RadixGetTransactionStatusFailed));
        }

        _logger.OperationCompleted(nameof(GetTransactionStatusAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - date);
        return response.Value.IntentStatus switch
        {
            RadixTransactionStatus.CommittedSuccess => Result<BridgeTransactionStatus>.Success(BridgeTransactionStatus
                .Completed),
            RadixTransactionStatus.CommittedFailure => Result<BridgeTransactionStatus>.Success(BridgeTransactionStatus
                .Canceled),
            RadixTransactionStatus.NotSeen => Result<BridgeTransactionStatus>.Success(BridgeTransactionStatus.NotFound),
            RadixTransactionStatus.InMemPool =>
                Result<BridgeTransactionStatus>.Success(),
            RadixTransactionStatus.PermanentRejection => Result<BridgeTransactionStatus>.Success(BridgeTransactionStatus
                .Canceled),
            RadixTransactionStatus.FateUncertainButLikelyRejection => Result<BridgeTransactionStatus>.Success(
                BridgeTransactionStatus.Canceled),
            RadixTransactionStatus.FateUncertain => Result<BridgeTransactionStatus>.Success(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #region Implementation for contract IBridge

    /// <summary>
    /// Creates a new account asynchronously using a randomly generated seed phrase.
    /// </summary>
    async Task<Result<(string PublicKey, string PrivateKey, string SeedPhrase)>> IBridge.CreateAccountAsync(
        CancellationToken token)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        _logger.OperationStarted(nameof(IBridge.CreateAccountAsync), date);

        try
        {
            token.ThrowIfCancellationRequested();
            Mnemonic mnemonic = new(Wordlist.English, WordCount.TwentyFour);
            using PrivateKey privateKey = RadixBridgeHelper.GetPrivateKey(mnemonic);
            string publicKey = Encoders.Hex.EncodeData(privateKey.PublicKeyBytes());

            _logger.OperationCompleted(nameof(IBridge.CreateAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<(string PublicKey, string PrivateKey, string SeedPhrase)>
                .Success((publicKey, privateKey.RawHex(), mnemonic.ToString()));
        }
        catch (Exception e)
        {
            _logger.OperationException(nameof(IBridge.CreateAccountAsync), e.Message);
            _logger.OperationCompleted(nameof(IBridge.CreateAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            await Task.CompletedTask;
            return Result<(string PublicKey, string PrivateKey, string SeedPhrase)>
                .Failure(ResultPatternError.InternalServerError(e.Message));
        }
    }

    /// <summary>
    /// Restores an account asynchronously using a provided seed phrase.
    /// </summary>
    async Task<Result<(string PublicKey, string PrivateKey)>> IBridge.RestoreAccountAsync(string seedPhrase,
        CancellationToken token)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        _logger.OperationStarted(nameof(IBridge.RestoreAccountAsync), date);
        try
        {
            token.ThrowIfCancellationRequested();

            if (!SeedPhraseValidator.IsValidSeedPhrase(seedPhrase))
            {
                _logger.OperationCompleted(nameof(IBridge.RestoreAccountAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<(string PublicKey, string PrivateKey)>
                    .Failure(ResultPatternError.BadRequest(Messages.RestoreAccountIncorrectFormat));
            }

            Mnemonic mnemonic = new(seedPhrase);
            PrivateKey privateKey = RadixBridgeHelper.GetPrivateKey(mnemonic);
            string publicKey = Encoders.Hex.EncodeData(privateKey.PublicKeyBytes());

            _logger.OperationCompleted(nameof(IBridge.RestoreAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<(string PublicKey, string PrivateKey)>
                .Success((publicKey, privateKey.RawHex()));
        }
        catch (Exception e)
        {
            _logger.OperationException(nameof(IBridge.RestoreAccountAsync), e.Message);
            _logger.OperationCompleted(nameof(IBridge.RestoreAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            await Task.CompletedTask;
            return Result<(string PublicKey, string PrivateKey)>
                .Failure(ResultPatternError.InternalServerError(e.Message));
        }
    }

    #endregion
}