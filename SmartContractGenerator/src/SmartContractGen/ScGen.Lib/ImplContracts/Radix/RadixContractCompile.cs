namespace ScGen.Lib.ImplContracts.Radix;

public sealed partial class RadixContractCompile(
    ILogger<RadixContractCompile> logger,
    IHttpContextAccessor httpContext) : IRadixContractCompile
{
    private const long MaxFileSize = 100 * 1024 * 1024;

    public async Task<Result<CompileContractResponse>> CompileAsync(IFormFile sourceCodeFile, CancellationToken token = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(CompileAsync),
            httpContext.GetId().ToString(), httpContext.GetCorrelationId());

        Result<CompileContractResponse> validation = Validation(sourceCodeFile);
        if (!validation.IsSuccess) return validation;

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            string zipPath = Path.Combine(tempDir, "source.zip");
            await using (FileStream fs = new(zipPath, FileMode.Create, FileAccess.Write))
                await sourceCodeFile.CopyToAsync(fs, token);

            ZipFile.ExtractToDirectory(zipPath, tempDir);

            ProcessExecutionResult result = await ProcessExtensions.RunScryptoAsync(tempDir, logger, token);
            if (!result.IsSuccess)
            {
                return Result<CompileContractResponse>.Failure(
                    ResultPatternError.BadRequest(result.GetErrorMessage()));
            }

            return await CreateResponseAsync(tempDir, token);
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