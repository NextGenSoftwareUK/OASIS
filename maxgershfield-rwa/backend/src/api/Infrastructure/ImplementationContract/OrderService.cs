namespace Infrastructure.ImplementationContract;

/// <summary>
/// The <see cref="OrderService"/> class provides functionality for creating orders
/// involving cryptocurrency transactions across two networks (Radix and Solana). It
/// manages the creation of virtual accounts, validation of requests, and processing of
/// transactions, including exchange rates, account balances, and transaction statuses.
/// The class interacts with the database to store order details and utilizes bridges 
/// (RadixBridge, SolanaBridge) for transferring assets between networks. It ensures 
/// that the process complies with security and network requirements while maintaining 
/// robust logging and error handling to track the status of operations.
/// </summary>
public sealed class OrderService(
    DataContext dbContext,
    IHttpContextAccessor accessor,
    ISolanaBridge solanaBridge,
    IRadixBridge radixBridge,
    ILogger<OrderService> logger) : IOrderService
{
    private const string Sol = "SOL";
    private const string Xrd = "XRD";

    /// <summary>
    /// Creates a new order for a user, including the validation of the request, 
    /// creation of necessary virtual accounts, and initiating withdrawal and deposit 
    /// transactions across the networks. The method ensures proper exchange rate 
    /// application, transaction verification, and manages error handling at each step.
    /// </summary>
    /// <param name="request">The request object containing order details.</param>
    /// <param name="token">Cancellation token to support cancellation of the operation.</param>
    /// <returns>A <see cref="Result{CreateOrderResponse}"/> object indicating the success 
    /// or failure of the order creation process, along with appropriate error messages.</returns>
    public async Task<Result<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CreateOrderAsync), date);

        Guid userId = accessor.GetId();

        Result<CreateOrderResponse> validationRequest = await ValidateRequestAsync(userId, request, token);
        if (!validationRequest.IsSuccess)
        {
            logger.OperationCompleted(nameof(CreateOrderAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return validationRequest;
        }

        IBridge withdrawBridge = request.FromToken == Xrd ? radixBridge : solanaBridge;
        IBridge depositBridge = request.FromToken == Xrd ? solanaBridge : radixBridge;

        ExchangeRate exchangeRate = await dbContext.ExchangeRates.AsNoTrackingWithIdentityResolution()
            .Include(x => x.FromToken)
            .Include(x => x.ToToken)
            .Where(x => x.FromToken.Symbol == request.FromToken && x.ToToken.Symbol == request.ToToken)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(token) ?? new();

        decimal convertedAmount = exchangeRate.Rate * request.Amount;

        VirtualAccount? virtualAccount = await dbContext.VirtualAccounts
            .AsNoTrackingWithIdentityResolution()
            .Include(x => x.Network)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Network.Name == request.FromNetwork, token);


        if (request is { FromToken: Xrd, ToToken: Sol } or { FromToken: Sol, ToToken: Xrd })
        {
            Result<decimal> balance = await withdrawBridge.GetAccountBalanceAsync(virtualAccount!.Address, token);
            if (!balance.IsSuccess)
            {
                logger.OperationCompleted(nameof(CreateOrderAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<CreateOrderResponse>.Failure(balance.Error);
            }

            bool isTransactional = balance.Value > request.Amount;

            Order newOrder = new Order
            {
                UserId = userId,
                CreatedBy = accessor.GetId(),
                CreatedByIp = accessor.GetRemoteIpAddress(),
                FromToken = request.FromToken,
                ToToken = request.ToToken,
                FromNetwork = request.FromNetwork,
                ToNetwork = request.ToNetwork,
                DestinationAddress = request.DestinationAddress,
                ExchangeRateId = exchangeRate.Id,
                Amount = request.Amount,
                OrderStatus = OrderStatus.InsufficientFunds,
            };

            if (isTransactional)
            {
                Result<TransactionResponse> withdrawTrRs = default!;
                Result<TransactionResponse> depositTrRs = default!;
                Result<TransactionResponse> abortTrRs;
                try
                {
                    withdrawTrRs = await withdrawBridge.WithdrawAsync(request.Amount, virtualAccount.Address,
                        virtualAccount.PrivateKey);
                    if (!withdrawTrRs.IsSuccess)
                    {
                        logger.OperationCompleted(nameof(CreateOrderAsync), DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow - date);
                        return Result<CreateOrderResponse>.Failure(withdrawTrRs.Error);
                    }

                    depositTrRs = await depositBridge.DepositAsync(convertedAmount, request.DestinationAddress);
                    if (!depositTrRs.IsSuccess)
                    {
                        abortTrRs = await withdrawBridge.DepositAsync(request.Amount, virtualAccount.Address);
                        if (!abortTrRs.IsSuccess)
                        {
                            logger.OperationCompleted(nameof(CreateOrderAsync), DateTimeOffset.UtcNow,
                                DateTimeOffset.UtcNow - date);
                            return Result<CreateOrderResponse>.Failure(abortTrRs.Error);
                        }

                        logger.OperationCompleted(nameof(CreateOrderAsync), DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow - date);
                        return Result<CreateOrderResponse>.Failure(depositTrRs.Error);
                    }

                    Result<BridgeTransactionStatus> transactionStatus =
                        await depositBridge.GetTransactionStatusAsync(depositTrRs.Value?.TransactionId!, token);
                    if (!transactionStatus.IsSuccess)
                    {
                        logger.OperationCompleted(nameof(CreateOrderAsync), DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow - date);
                        return Result<CreateOrderResponse>.Failure(transactionStatus.Error);
                    }

                    OrderStatus orderStatus = transactionStatus.Value switch
                    {
                        BridgeTransactionStatus.Canceled => OrderStatus.Canceled,
                        BridgeTransactionStatus.Completed => OrderStatus.Completed,
                        BridgeTransactionStatus.Pending => OrderStatus.Pending,
                        BridgeTransactionStatus.Expired => OrderStatus.Expired,
                        BridgeTransactionStatus.InsufficientFunds => OrderStatus.InsufficientFunds,
                        BridgeTransactionStatus.SufficientFunds => OrderStatus.SufficientFunds,
                        BridgeTransactionStatus.InsufficientFundsForFee => OrderStatus.InsufficientFundsForFee,
                        _ => OrderStatus.NotFound
                    };

                    newOrder.OrderStatus = orderStatus;
                    newOrder.TransactionHash = depositTrRs.Value?.TransactionId;
                }
                catch (Exception e)
                {
                    logger.OperationException(nameof(CreateOrderAsync), e.Message);
                    if (withdrawTrRs.IsSuccess && (depositTrRs is null || !depositTrRs.IsSuccess))
                    {
                        abortTrRs = await withdrawBridge.DepositAsync(request.Amount, virtualAccount.Address);
                        if (!abortTrRs.IsSuccess)
                        {
                            logger.OperationCompleted(nameof(CreateOrderAsync), DateTimeOffset.UtcNow,
                                DateTimeOffset.UtcNow - date);
                            return Result<CreateOrderResponse>.Failure(abortTrRs.Error);
                        }
                    }

                    logger.OperationCompleted(nameof(CreateOrderAsync), DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow - date);
                    return Result<CreateOrderResponse>.Failure(ResultPatternError.InternalServerError(e.Message));
                }
            }

            using var transactionScope = TransactionExtensions.CreateTransactionScope();
            await dbContext.Orders.AddAsync(newOrder, token);
            logger.OperationCompleted(nameof(CreateOrderAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            int res = await dbContext.SaveChangesAsync(token);
            if (res != 0)
            {
                transactionScope.Complete();
                return Result<CreateOrderResponse>.Success(new CreateOrderResponse(newOrder.Id, "Order created"));
            }

            return Result<CreateOrderResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.CreateOrderFailed));
        }

        logger.OperationCompleted(nameof(CreateOrderAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - date);
        return Result<CreateOrderResponse>.Failure(ResultPatternError.BadRequest(Messages.CreateOrderUnsupported));
    }

    /// <summary>
    /// Validates the details of the incoming order request by checking the user ID, 
    /// network validity, token symbols, and destination address format. 
    /// Ensures the provided amount is greater than zero and that the networks and 
    /// tokens involved are available in the database.
    /// </summary>
    /// <param name="userId">The user ID for the account creating the order.</param>
    /// <param name="request">The order request to validate.</param>
    /// <param name="token">Cancellation token to support cancellation of the operation.</param>
    /// <returns>A <see cref="Result{CreateOrderResponse}"/> object indicating whether 
    /// the validation was successful, or containing error details.</returns>
    private async Task<Result<CreateOrderResponse>> ValidateRequestAsync(Guid? userId, CreateOrderRequest request,
        CancellationToken token)
    {
        try
        {
            if (!await dbContext.Users.AnyAsync(x => x.Id == userId, token))
                return Result<CreateOrderResponse>.Failure(
                    ResultPatternError.NotFound(Messages.UserNotFound));

            if (!await dbContext.Networks.AnyAsync(x => x.Name == request.FromNetwork, token))
                return Result<CreateOrderResponse>.Failure(
                    ResultPatternError.NotFound(Messages.NetworkNotFound));

            if (!await dbContext.Networks.AnyAsync(x => x.Name == request.ToNetwork, token))
                return Result<CreateOrderResponse>.Failure(
                    ResultPatternError.NotFound(Messages.NetworkNotFound));

            if (!await dbContext.NetworkTokens.AnyAsync(x => x.Symbol == request.FromToken, token))
                return Result<CreateOrderResponse>.Failure(
                    ResultPatternError.NotFound(Messages.NetworkTokenNotFound));

            if (!await dbContext.NetworkTokens.AnyAsync(x => x.Symbol == request.ToToken, token))
                return Result<CreateOrderResponse>.Failure(
                    ResultPatternError.NotFound(Messages.NetworkTokenNotFound));


            if (request.Amount <= 0)
                return Result<CreateOrderResponse>.Failure(
                    ResultPatternError.BadRequest(Messages.CreateOrderAmountFilter));

            if (request.FromToken == Xrd)
            {
                bool check = IsValidSolanaAddress(request.DestinationAddress);
                if (!check)
                    return Result<CreateOrderResponse>.Failure(
                        ResultPatternError.BadRequest(Messages.InvalidSolanaFormat));
            }
            else if (request.FromToken == Sol)
            {
                bool check = IsValidRadixAddress(request.DestinationAddress);
                if (!check)
                    return Result<CreateOrderResponse>.Failure(
                        ResultPatternError.BadRequest(Messages.InvalidAddressRadixFormat));
            }

            return Result<CreateOrderResponse>.Success();
        }
        catch (Exception e)
        {
            logger.OperationException(nameof(ValidateRequestAsync), e.Message);
            return Result<CreateOrderResponse>.Failure(ResultPatternError.InternalServerError(e.Message));
        }
    }

    /// <summary>
    /// Checks the balance of a user's virtual account, validates the order status, 
    /// and performs necessary actions based on the current state of the order. 
    /// This includes handling different order statuses (Completed, Pending, Expired, etc.), 
    /// ensuring sufficient funds for transactions, and processing funds transfers between networks.
    /// </summary>
    /// <param name="orderId">The unique identifier for the order.</param>
    /// <param name="token">Cancellation token for task cancellation (default is none).</param>
    /// <returns>A result containing the check balance response or an error message.</returns>
    public async Task<Result<CheckBalanceResponse>> CheckBalanceAsync(Guid orderId,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CheckBalanceAsync), date);

        using var transactionScope = TransactionExtensions.CreateTransactionScope();
        Order? order = await dbContext.Orders.FirstOrDefaultAsync(x => x.Id == orderId, token);
        if (order is null)
        {
            logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CheckBalanceResponse>.Failure(
                ResultPatternError.NotFound(Messages.OrderNotFound));
        }

        IBridge withdrawBridge = order.FromToken == Xrd ? radixBridge : solanaBridge;
        IBridge depositBridge = order.FromToken == Xrd ? solanaBridge : radixBridge;


        VirtualAccount? virtualAccount = await dbContext.VirtualAccounts
            .AsNoTracking()
            .Include(x => x.Network)
            .FirstOrDefaultAsync(x
                => x.UserId == order.UserId && x.Network.Name == order.FromNetwork, token);
        if (virtualAccount is null)
        {
            logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CheckBalanceResponse>.Failure(ResultPatternError.NotFound(Messages.VirtualAccountNotFound));
        }

        ExchangeRate exchangeRate =
            await dbContext.ExchangeRates.FirstOrDefaultAsync(x => x.Id == order.ExchangeRateId, token) ?? new();
        decimal convertedAmount = exchangeRate.Rate * order.Amount;

        Result<decimal> balance = await withdrawBridge.GetAccountBalanceAsync(virtualAccount.Address, token);
        if (!balance.IsSuccess)
        {
            logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CheckBalanceResponse>.Failure(balance.Error);
        }

        if (order.OrderStatus == OrderStatus.Completed)
        {
            logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CheckBalanceResponse>.Success(new(
                order.Id,
                order.FromNetwork,
                order.FromToken,
                balance.Value,
                0,
                order.OrderStatus.ToString(),
                Messages.OrderAlreadyCompleted,
                order.TransactionHash));
        }

        bool isExpired = (DateTimeOffset.UtcNow - order.CreatedAt).TotalMinutes > 10;
        if (isExpired)
        {
            int res = 1;
            if (order.OrderStatus != OrderStatus.Expired)
            {
                order.OrderStatus = OrderStatus.Expired;
                res = await dbContext.SaveChangesAsync(token);
                if (res > 0)
                {
                    transactionScope.Complete();
                }
            }

            logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return res != 0
                ? Result<CheckBalanceResponse>.Success(new(
                    order.Id,
                    order.FromNetwork,
                    order.FromToken,
                    balance.Value,
                    order.Amount,
                    OrderStatus.Expired.ToString(),
                    Messages.OrderCanceled,
                    order.TransactionHash))
                : Result<CheckBalanceResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.CheckBalanceFailed));
        }

        if (order.OrderStatus == OrderStatus.Pending)
        {
            Result<BridgeTransactionStatus> transactionStatus =
                await depositBridge.GetTransactionStatusAsync(order.TransactionHash!, token);
            if (!transactionStatus.IsSuccess)
            {
                logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<CheckBalanceResponse>.Failure(transactionStatus.Error);
            }

            OrderStatus orderStatus = transactionStatus.Value switch
            {
                BridgeTransactionStatus.Canceled => OrderStatus.Canceled,
                BridgeTransactionStatus.Completed => OrderStatus.Completed,
                BridgeTransactionStatus.Pending => OrderStatus.Pending,
                BridgeTransactionStatus.Expired => OrderStatus.Expired,
                BridgeTransactionStatus.InsufficientFunds => OrderStatus.InsufficientFunds,
                BridgeTransactionStatus.SufficientFunds => OrderStatus.SufficientFunds,
                BridgeTransactionStatus.InsufficientFundsForFee => OrderStatus.InsufficientFundsForFee,
                _ => OrderStatus.NotFound
            };

            order.OrderStatus = orderStatus;
            int updateResult = await dbContext.SaveChangesAsync(token);
            if (updateResult > 0)
            {
                transactionScope.Complete();
            }

            logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CheckBalanceResponse>.Success(new(
                order.Id,
                order.FromNetwork,
                order.FromToken,
                balance.Value,
                order.OrderStatus != OrderStatus.Completed ? order.Amount : 0,
                order.OrderStatus.ToString(),
                order.OrderStatus == OrderStatus.Completed ? Messages.OrderAlreadyCompleted : "",
                order.TransactionHash));
        }

        if (balance.Value <= order.Amount && order.OrderStatus != OrderStatus.Completed &&
            order.OrderStatus != OrderStatus.Canceled && order.OrderStatus != OrderStatus.Expired &&
            order.OrderStatus != OrderStatus.Pending)
        {
            logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CheckBalanceResponse>.Success(new(
                order.Id,
                order.FromNetwork,
                order.FromToken,
                balance.Value,
                order.Amount,
                order.OrderStatus.ToString(),
                Messages.OrderInsufficientFunds,
                order.TransactionHash));
        }

        if (balance.Value > order.Amount && order.OrderStatus != OrderStatus.Canceled &&
            order.OrderStatus != OrderStatus.Completed && order.OrderStatus != OrderStatus.Expired &&
            order.OrderStatus != OrderStatus.Pending)
        {
            Result<TransactionResponse> withdrawTrRs = default!;
            Result<TransactionResponse> depositTrRs = default!;
            Result<TransactionResponse> abortTrRs;
            try
            {
                withdrawTrRs = await withdrawBridge.WithdrawAsync(order.Amount, virtualAccount.Address,
                    virtualAccount.PrivateKey);
                if (!withdrawTrRs.IsSuccess)
                {
                    logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow - date);
                    return Result<CheckBalanceResponse>.Failure(withdrawTrRs.Error);
                }

                depositTrRs = await depositBridge.DepositAsync(convertedAmount, order.DestinationAddress);
                if (!depositTrRs.IsSuccess)
                {
                    abortTrRs = await withdrawBridge.DepositAsync(order.Amount, virtualAccount.Address);
                    if (!abortTrRs.IsSuccess)
                    {
                        logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow - date);
                        return Result<CheckBalanceResponse>.Failure(abortTrRs.Error);
                    }

                    logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow - date);
                    return Result<CheckBalanceResponse>.Failure(depositTrRs.Error);
                }

                Result<BridgeTransactionStatus> transactionStatus =
                    await depositBridge.GetTransactionStatusAsync(depositTrRs.Value?.TransactionId!, token);
                if (!transactionStatus.IsSuccess)
                {
                    logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow - date);
                    return Result<CheckBalanceResponse>.Failure(transactionStatus.Error);
                }

                OrderStatus orderStatus = transactionStatus.Value switch
                {
                    BridgeTransactionStatus.Canceled => OrderStatus.Canceled,
                    BridgeTransactionStatus.Completed => OrderStatus.Completed,
                    BridgeTransactionStatus.Pending => OrderStatus.Pending,
                    BridgeTransactionStatus.Expired => OrderStatus.Expired,
                    BridgeTransactionStatus.InsufficientFunds => OrderStatus.InsufficientFunds,
                    BridgeTransactionStatus.SufficientFunds => OrderStatus.SufficientFunds,
                    BridgeTransactionStatus.InsufficientFundsForFee => OrderStatus.InsufficientFundsForFee,
                    _ => OrderStatus.NotFound
                };
                order.OrderStatus = orderStatus;
                order.TransactionHash = depositTrRs.Value?.TransactionId;
            }
            catch (Exception e)
            {
                logger.OperationException(nameof(CheckBalanceAsync), e.Message);
                if (withdrawTrRs.IsSuccess && (depositTrRs is null || !depositTrRs.IsSuccess))
                {
                    abortTrRs = await withdrawBridge.DepositAsync(order.Amount, virtualAccount.Address);
                    if (!abortTrRs.IsSuccess)
                    {
                        logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow - date);
                        return Result<CheckBalanceResponse>.Failure(abortTrRs.Error);
                    }
                }

                logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<CheckBalanceResponse>.Failure(ResultPatternError.InternalServerError(e.Message));
            }

            int saveResult = await dbContext.SaveChangesAsync(token);
            if (saveResult > 0)
            {
                transactionScope.Complete();
            }

            Result<decimal> nowBalance = await withdrawBridge.GetAccountBalanceAsync(virtualAccount.Address, token);
            if (!nowBalance.IsSuccess)
            {
                logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<CheckBalanceResponse>.Failure(nowBalance.Error);
            }

            logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CheckBalanceResponse>.Success(new CheckBalanceResponse(
                order.Id,
                order.FromNetwork,
                order.FromToken,
                nowBalance.Value,
                order.OrderStatus != OrderStatus.Completed ? order.Amount : 0,
                order.OrderStatus.ToString(),
                order.OrderStatus == OrderStatus.Completed ? Messages.OrderAlreadyCompleted : "",
                order.TransactionHash));
        }

        logger.OperationCompleted(nameof(CheckBalanceAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - date);
        return Result<CheckBalanceResponse>.Success(new CheckBalanceResponse(
            order.Id,
            order.FromNetwork,
            order.FromToken,
            balance.Value,
            order.OrderStatus != OrderStatus.Completed ? order.Amount : 0,
            order.OrderStatus.ToString(),
            order.OrderStatus == OrderStatus.Completed ? Messages.OrderAlreadyCompleted : "",
            order.TransactionHash));
    }


    /// <summary>
    /// Validates if a given Solana address is correctly formatted. 
    /// A valid Solana address must be 44 characters long and only contain alphanumeric characters.
    /// </summary>
    /// <param name="address">The Solana address to validate.</param>
    /// <returns>Returns true if the address is valid, otherwise false.</returns>
    private bool IsValidSolanaAddress(string address)
        => address.Length == 44 && address.All(char.IsLetterOrDigit);

    /// <summary>
    /// Validates if a given Radix address starts with the correct prefix "account_tdx_". 
    /// This ensures that the address follows the expected format for Radix accounts.
    /// </summary>
    /// <param name="address">The Radix address to validate.</param>
    /// <returns>Returns true if the address starts with "account_tdx_", otherwise false.</returns>
    private bool IsValidRadixAddress(string address)
        => address.StartsWith("account_tdx_");
}