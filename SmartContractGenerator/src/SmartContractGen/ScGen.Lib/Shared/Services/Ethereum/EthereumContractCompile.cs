namespace ScGen.Lib.ImplContracts.Ethereum;

public sealed partial class EthereumContractCompile
{
    private Result<CompileContractResponse> Validation(IFormFile file)
    {
        if (!file.IsSolidityFile())
        {
            logger.ValidationFailed(nameof(CompileAsync),
                Messages.InvalidSolidityFile, httpContext.GetId().ToString());
            return Result<CompileContractResponse>.Failure(
                ResultPatternError.BadRequest(Messages.InvalidSolidityFile));
        }

        if (file.Length == 0)
        {
            logger.ValidationFailed(nameof(CompileAsync),
                Messages.EmptyFile, httpContext.GetId().ToString());
            return Result<CompileContractResponse>.Failure(ResultPatternError.BadRequest(Messages.EmptyFile));
        }

        if (file.Length > MaxFileSize)
        {
            logger.ValidationFailed(nameof(CompileAsync),
                Messages.FileTooLarge, httpContext.GetId().ToString());
            return Result<CompileContractResponse>.Failure(ResultPatternError.BadRequest(Messages.FileTooLarge));
        }

        return Result<CompileContractResponse>.Success();
    }

    private async Task<(string, string)> SaveSourceFileAsync(IFormFile sourceCodeFile, CancellationToken token)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        string sourceFilePath = Path.Combine(tempDir, sourceCodeFile.GetSoliditySafeFileName());

        await using FileStream fs = new(sourceFilePath, FileMode.CreateNew);
        await sourceCodeFile.CopyToAsync(fs, token);
        return (tempDir, sourceFilePath);
    }
    

    private async Task<Result<CompileContractResponse>> CreateResponseAsync(
        string outputDir, string contractName, CancellationToken token)
    {
        string? abiPath = Directory.EnumerateFiles(outputDir, "*.abi").FirstOrDefault();
        string? binPath = Directory.EnumerateFiles(outputDir, "*.bin").FirstOrDefault();

        if (abiPath is null)
            return Result<CompileContractResponse>.Failure(ResultPatternError.InternalServerError(Messages.NotFoundAbi));
        if (binPath is null)
            return Result<CompileContractResponse>.Failure(ResultPatternError.InternalServerError(Messages.NotFoundBin));

        Task<byte[]> abiTask = File.ReadAllBytesAsync(abiPath, token);
        Task<byte[]> binTask = File.ReadAllBytesAsync(binPath, token);

        byte[] abiBytes = await abiTask;
        byte[] binBytes = await binTask;


        return Result<CompileContractResponse>.Success(new()
        {
            Shema = abiBytes,
            ShemaFileName = $"{contractName}.abi",
            CompiledCode = binBytes,
            CompiledCodeFileName = $"{contractName}.bin",
            ContentType = "application/octet-stream"
        });
    }
}