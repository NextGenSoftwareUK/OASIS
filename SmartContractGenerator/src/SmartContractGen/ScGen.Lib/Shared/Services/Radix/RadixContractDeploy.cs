namespace ScGen.Lib.ImplContracts.Radix;

public sealed partial class RadixContractDeploy
{
    private Result<DeployContractResponse> Validation(IFormFile? schema, IFormFile bytecodeFile)
    {
        if (schema == null || schema.Length == 0)
        {
            logger.ValidationFailed(nameof(DeployAsync),
                Messages.EmptyJson, httpContext.GetId().ToString());
            return Result<DeployContractResponse>.Failure(ResultPatternError.BadRequest(Messages.EmptyAbi));
        }

        if (!schema.IsRpdFile())
        {
            logger.ValidationFailed(nameof(DeployAsync),
                Messages.InvalidJsonFile, httpContext.GetId().ToString());
            return Result<DeployContractResponse>.Failure(ResultPatternError.BadRequest(Messages.InvalidAbiFile));
        }


        if (!bytecodeFile.IsWasmFile())
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


    private async Task<ProcessExecutionResult> DeployContractAsync(string wasmFilePath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(DeployContractAsync),
            httpContext.GetId().ToString(), httpContext.GetCorrelationId());

        try
        {
            if (!ProcessExtensions.EnsureDefaultResimAccountExists(logger).IsSuccess)
            {
                ProcessExecutionResult resCreateDefaultResimAccount = ProcessExtensions.CreateDefaultResimAccount(logger);
                if (!resCreateDefaultResimAccount.IsSuccess)
                    return resCreateDefaultResimAccount;
            }
            
            return await ProcessExtensions.DeployRadixContractAsync(wasmFilePath, logger);
            
        }
        catch (Exception ex)
        {
            logger.OperationFailed(nameof(DeployContractAsync), ex.Message,
                httpContext.GetId().ToString(), httpContext.GetCorrelationId());
            return ProcessExecutionResult.Failure(int.MinValue, string.Empty, ex.Message, TimeSpan.Zero);
        }
        finally
        {
            stopwatch.Stop();
            logger.OperationCompleted(nameof(DeployContractAsync),
                stopwatch.ElapsedMilliseconds, httpContext.GetCorrelationId());
        }
    }
    
    private static string? ExtractPackageAddress(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;
        const string marker = "Success! New Package: ";
        int idx = message.IndexOf(marker, StringComparison.Ordinal);
        if (idx == -1) return null;
        return message.Substring(idx + marker.Length).Trim();
    }
}