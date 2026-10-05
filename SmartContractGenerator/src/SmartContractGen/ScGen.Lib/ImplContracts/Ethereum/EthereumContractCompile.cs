namespace ScGen.Lib.ImplContracts.Ethereum;

public sealed partial class EthereumContractCompile(
    ILogger<EthereumContractCompile> logger,
    IHttpContextAccessor httpContext) : IEthereumContractCompile
{
    private const long MaxFileSize = 100 * 1024 * 1024;

    public async Task<Result<CompileContractResponse>> CompileAsync(IFormFile sourceCodeFile,
        CancellationToken token = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(CompileAsync),
            httpContext.GetId().ToString(), httpContext.GetCorrelationId());

        Result<CompileContractResponse> validation = Validation(sourceCodeFile);
        if (!validation.IsSuccess) return validation;

        (string tempDir, string sourceFilePath) = await SaveSourceFileAsync(sourceCodeFile, token);
        try
        {
            ProcessExecutionResult result = await ProcessExtensions
                .RunSolcAsync(sourceFilePath, tempDir, logger, token);
            if (!result.IsSuccess)
            {
                if (!string.IsNullOrWhiteSpace(result.StandardError))
                {
                    return Result<CompileContractResponse>.Failure(
                        ResultPatternError.BadRequest(result.StandardError));
                }

                return Result<CompileContractResponse>.Failure(
                    ResultPatternError.InternalServerError(result.GetErrorMessage()));
            }

            string contractName = Path.GetFileNameWithoutExtension(sourceFilePath);

            return await CreateResponseAsync(tempDir, contractName, token);
        }
        catch (Exception ex)
        {
            logger.OperationFailed(nameof(CompileAsync), ex.Message,
                httpContext.GetId().ToString(), httpContext.GetCorrelationId());
            return Result<CompileContractResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
        finally
        {
            tempDir.DeleteDirectorySafe();
            stopwatch.Stop();
            logger.OperationCompleted(nameof(CompileAsync),
                stopwatch.ElapsedMilliseconds, httpContext.GetCorrelationId());
        }
    }
}