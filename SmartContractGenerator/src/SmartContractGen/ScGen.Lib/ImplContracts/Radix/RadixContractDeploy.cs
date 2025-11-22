namespace ScGen.Lib.ImplContracts.Radix;

public sealed partial class RadixContractDeploy(
    ILogger<RadixContractDeploy> logger,
    IHttpContextAccessor httpContext) : IRadixContractDeploy
{
    public async Task<Result<DeployContractResponse>> DeployAsync(IFormFile? schema, IFormFile bytecodeFile,
        CancellationToken token = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(DeployAsync),
            httpContext.GetId().ToString(), httpContext.GetCorrelationId());

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            Result<DeployContractResponse> validation = Validation(schema, bytecodeFile);
            if (!validation.IsSuccess) return validation;

            string programPath = Path.Combine(tempDir, bytecodeFile.FileName);
            await using (FileStream fs = new(programPath, FileMode.Create))
                await bytecodeFile.CopyToAsync(fs, token);

            if (schema is { Length: > 0 })
            {
                string schemaPath = Path.Combine(tempDir, schema.FileName);
                await using FileStream fs = new(schemaPath, FileMode.Create);
                await schema.CopyToAsync(fs, token);
            }

            ProcessExecutionResult result = await DeployContractAsync(programPath);
            if (!result.IsSuccess)
            {
                logger.OperationFailed(nameof(DeployAsync), result.GetErrorMessage(),
                    httpContext.GetId().ToString(), httpContext.GetCorrelationId());
                return Result<DeployContractResponse>.Failure(ResultPatternError.BadRequest(result.GetErrorMessage()));
            }


            return Result<DeployContractResponse>.Success(new()
            {
                ContractAddress = ExtractPackageAddress(result.StandardOutput.Trim()) ?? string.Empty,
                Success = true,
                TransactionHash = string.Empty
            });
        }
        catch (Exception ex)
        {
            logger.OperationFailed(nameof(DeployAsync), ex.Message,
                httpContext.GetId().ToString(), httpContext.GetCorrelationId());
            return Result<DeployContractResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
        finally
        {
            tempDir.DeleteDirectorySafe();
            stopwatch.Stop();
            logger.OperationCompleted(nameof(DeployAsync),
                stopwatch.ElapsedMilliseconds, httpContext.GetCorrelationId());
        }
    }
}