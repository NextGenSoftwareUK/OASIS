namespace ScGen.Lib.ImplContracts.Ethereum;

public sealed partial class EthereumContractDeploy
{
    private const string Localhost = "127.0.0.1";
    private const int DefaultPort = 8545;


    private Result<DeployContractResponse> Validation(IFormFile? abiFile, IFormFile bytecodeFile)
    {
        BaseResult resultValidation = options.Value.IsValid();
        if (!resultValidation.IsSuccess)
        {
            logger.ValidationFailed(nameof(DeployAsync), resultValidation.Error.Message!, httpContext.GetId().ToString());
            return Result<DeployContractResponse>.Failure(resultValidation.Error);
        }

        if (abiFile == null)
        {
            logger.ValidationFailed(nameof(DeployAsync),
                Messages.MissingAbiFile, httpContext.GetId().ToString());
            return Result<DeployContractResponse>.Failure(ResultPatternError.BadRequest(Messages.MissingAbiFile));
        }

        if (!abiFile.IsAbiFile())
        {
            logger.ValidationFailed(nameof(DeployAsync),
                Messages.InvalidAbiFile, httpContext.GetId().ToString());
            return Result<DeployContractResponse>.Failure(ResultPatternError.BadRequest(Messages.InvalidAbiFile));
        }

        if (abiFile.Length == 0)
        {
            logger.ValidationFailed(nameof(DeployAsync),
                Messages.EmptyAbi, httpContext.GetId().ToString());
            return Result<DeployContractResponse>.Failure(ResultPatternError.BadRequest(Messages.EmptyAbi));
        }

        if (!bytecodeFile.IsEthereumBinFile())
        {
            logger.ValidationFailed(nameof(DeployAsync),
                Messages.InvalidBytecodeFile, httpContext.GetId().ToString());
            return Result<DeployContractResponse>.Failure(ResultPatternError.BadRequest(Messages.InvalidBytecodeFile));
        }

        if (bytecodeFile.Length == 0)
        {
            logger.ValidationFailed(nameof(DeployAsync),
                Messages.EmptyBytecode, httpContext.GetId().ToString());
            return Result<DeployContractResponse>.Failure(ResultPatternError.BadRequest(Messages.EmptyBytecode));
        }

        return Result<DeployContractResponse>.Success();
    }


    private async Task<bool> EnsureNodeRunningAsync(CancellationToken cancellationToken = default)
    {
        using (logger.BeginScopedOperation(nameof(EnsureNodeRunningAsync), httpContext.GetId().ToString(),
                   httpContext.GetCorrelationId(), PerformanceThreshold.Normal, true))
        {
            try
            {
                if (await CheckIfRunningTcpAsync(_options.RpcUrl.GetHost(), _options.RpcUrl.GetPort(),
                        cancellationToken))
                {
                    logger.OperationCompleted(nameof(EnsureNodeRunningAsync), 0, httpContext.GetCorrelationId());
                    return true;
                }

                bool started = StartNode();
                if (!started)
                {
                    logger.OperationFailed(nameof(EnsureNodeRunningAsync), Messages.FailedToStartNode,
                        httpContext.GetId().ToString(), httpContext.GetCorrelationId());
                    return false;
                }

                logger.OperationCompleted(nameof(EnsureNodeRunningAsync), 0, httpContext.GetCorrelationId());
                return true;
            }
            catch (Exception ex)
            {
                logger.OperationFailedWithException(nameof(EnsureNodeRunningAsync), ex.Message,
                    httpContext.GetCorrelationId());
                return false;
            }
        }
    }

    private async Task<Result<DeployContractResponse>> ExecuteContractDeploymentAsync(IFormFile abiFile, IFormFile bytecodeFile,
        CancellationToken token)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(ExecuteContractDeploymentAsync),
            httpContext.GetId().ToString(), httpContext.GetCorrelationId());

        string abi;
        string bytecode;

        using (StreamReader reader = new(abiFile.OpenReadStream()))
            abi = await reader.ReadToEndAsync(token);

        using (StreamReader reader = new(bytecodeFile.OpenReadStream()))
            bytecode = await reader.ReadToEndAsync(token);

        logger.LogInformation(Messages.StartDeploymentInEthereum);

        Nethereum.Web3.Accounts.Account account = new(_options.PrivateKey);
        Web3 web3 = ethereumFactory.CreateWeb3WithAccount(_options.RpcUrl, _options.PrivateKey);

        try
        {
            TransactionReceipt receipt = await web3.Eth.DeployContract
                .SendRequestAndWaitForReceiptAsync(
                    abi, bytecode, account.Address,
                    new Nethereum.Hex.HexTypes.HexBigInteger(_options.GasLimit)
                );

            bool success = receipt.Status.Value == 1;

            if (success)
                logger.LogInformation(Messages.ContractSuccessfullyDeployed);
            else
                logger.LogError(Messages.ContractFailedDeployed);

            return Result<DeployContractResponse>.Success(new()
            {
                ContractAddress = receipt.ContractAddress,
                TransactionHash = receipt.TransactionHash,
                Success = success
            });
        }
        catch (Exception ex)
        {
            logger.OperationFailedWithException(nameof(ExecuteContractDeploymentAsync), ex.Message,
                httpContext.GetCorrelationId());
            return Result<DeployContractResponse>.Failure(
                ResultPatternError.BadRequest(ex.Message));
        }
        finally
        {
            stopwatch.Stop();
            logger.OperationCompleted(nameof(ExecuteContractDeploymentAsync),
                stopwatch.ElapsedMilliseconds, httpContext.GetCorrelationId());
        }
    }


    public async Task<bool> CheckIfRunningTcpAsync(string host = Localhost,
        int port = DefaultPort, CancellationToken cancellationToken = default, int timeoutSeconds = 3)
    {
        using IDisposable scope = logger.BeginScopedOperation(
            nameof(CheckIfRunningTcpAsync), httpContext.GetId().ToString(),
            httpContext.GetCorrelationId(), PerformanceThreshold.Fast, true);

        if (string.IsNullOrWhiteSpace(host))
            return false;

        if (port <= 0 || port > 65535)
            return false;

        try
        {
            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using TcpClient client = new();

            await client.ConnectAsync(host, port, timeoutCts.Token);


            if (client.Connected)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }


    public bool StartNode()
    {
        using (logger.BeginScopedOperation(nameof(StartNode), httpContext.GetId().ToString(),
                   httpContext.GetCorrelationId(), PerformanceThreshold.Normal, true))
        {
            try
            {
                ProcessExecutionResult result = ProcessExtensions.RunGanache(logger, _options.RpcUrl.GetPort());
                if (!result.IsSuccess)
                {
                    logger.OperationFailed(nameof(StartNode), result.GetErrorMessage() + result.StandardError,
                        httpContext.GetId().ToString(), httpContext.GetCorrelationId());
                    return false;
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
                return true;
            }
            catch (Exception e)
            {
                logger.OperationFailedWithException(nameof(StartNode),
                    e.Message, httpContext.GetCorrelationId());
                return false;
            }
        }
    }
}