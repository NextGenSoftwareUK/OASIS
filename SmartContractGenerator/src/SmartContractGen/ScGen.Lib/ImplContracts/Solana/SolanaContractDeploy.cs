namespace ScGen.Lib.ImplContracts.Solana;

public sealed partial class SolanaContractDeploy(
    ILogger<SolanaContractDeploy> logger,
    IHttpContextAccessor httpContext,
    IOptions<SolanaOptions> options) : ISolanaContractDeploy
{
    private readonly SolanaOptions _options = options.Value;

    public async Task<Result<DeployContractResponse>> DeployAsync(IFormFile? keyPair, IFormFile bytecodeFile,
        CancellationToken token = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(DeployAsync),
            httpContext.GetId().ToString(), httpContext.GetCorrelationId());

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            Result<DeployContractResponse> validation = Validation(keyPair, bytecodeFile);
            if (!validation.IsSuccess) return validation;

            // Only ensure local validator is running if UseLocalValidator is true
            if (_options.UseLocalValidator)
            {
                if (!await EnsureNodeRunningAsync(token))
                    return Result<DeployContractResponse>.Failure(
                        ResultPatternError.InternalServerError(Messages.NodeProcessNotRun));
            }
            else
            {
                logger.LogInformation($"Deploying to remote RPC: {_options.RpcUrl}");
            }

            string programPath = Path.Combine(tempDir, bytecodeFile.FileName);

            await using FileStream fs = new(programPath, FileMode.Create);
            await bytecodeFile.CopyToAsync(fs, token);

            // Save user-provided wallet keypair to temp file
            string walletKeypairPath;
            if (keyPair != null && keyPair.Length > 0)
            {
                walletKeypairPath = Path.Combine(tempDir, "wallet-keypair.json");
                await using FileStream walletFs = new(walletKeypairPath, FileMode.Create);
                await keyPair.CopyToAsync(walletFs, token);
                logger.LogInformation($"Using user-provided wallet keypair");
            }
            else
            {
                walletKeypairPath = _options.KeyPairPath;
                logger.LogInformation($"Using default wallet keypair from config: {walletKeypairPath}");
            }

            return await DeployProgramAsync(programPath, walletKeypairPath, token);
        }
        catch (Exception ex)
        {
            logger.OperationFailed(nameof(DeployAsync), ex.Message,
                httpContext.GetId().ToString(), httpContext.GetCorrelationId());
            return Result<DeployContractResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
        finally
        {
            stopwatch.Stop();
            logger.OperationCompleted(nameof(DeployAsync),
                stopwatch.ElapsedMilliseconds, httpContext.GetCorrelationId());
        }
    }
}