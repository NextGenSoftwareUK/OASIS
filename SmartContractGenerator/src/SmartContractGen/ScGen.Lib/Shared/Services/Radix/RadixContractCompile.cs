namespace ScGen.Lib.ImplContracts.Radix;

public sealed partial class RadixContractCompile
{
    private Result<CompileContractResponse> Validation(IFormFile file)
    {
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

    private (string? wasmPath, string? schemaPath) FindCompiledFiles(string baseDir)
    {
        string[] wasmFiles = Directory.GetFiles(baseDir, "*.wasm", SearchOption.AllDirectories);
        if (wasmFiles.Length == 0)
            return (null, null);

        string mainWasm = wasmFiles.FirstOrDefault(f => !Path.GetFileName(f).Contains("_with_schema"))
                          ?? wasmFiles.First();

        string? wasmDir = Path.GetDirectoryName(mainWasm);
        string wasmBase = Path.GetFileNameWithoutExtension(mainWasm);

        string[] possibleSchemaExts = [".rpd", ".schema", ".json"];
        foreach (string ext in possibleSchemaExts)
        {
            if (wasmDir != null)
            {
                string schemaPath = Path.Combine(wasmDir, wasmBase + ext);
                if (File.Exists(schemaPath))
                    return (mainWasm, schemaPath);
            }
        }

        if (wasmDir != null)
        {
            string[] foundSchemas = Directory.GetFiles(wasmDir, "*.rpd")
                .Concat(Directory.GetFiles(wasmDir, "*.schema"))
                .Concat(Directory.GetFiles(wasmDir, "*schema*.json"))
                .ToArray();

            if (foundSchemas.Length > 0)
                return (mainWasm, foundSchemas.First());
        }

        return (mainWasm, null);
    }

    private async Task<Result<CompileContractResponse>> CreateResponseAsync(string tempDir, CancellationToken token)
    {
        var (wasmPath, schemaPath) = FindCompiledFiles(tempDir);

        if (string.IsNullOrEmpty(wasmPath))
        {
            DirectoryExtensions.LogDirectoryContents(tempDir, logger);
            throw new FileNotFoundException(
                "No WASM files found in compilation output.\n" +
                "Expected WASM files in target directories.\n" +
                "Please check compilation logs for errors.");
        }

        byte[] wasmBytecode = await File.ReadAllBytesAsync(wasmPath, token);
        byte[] schemaData = [];
        string schemaFileName = "schema.rpd";

        if (!string.IsNullOrEmpty(schemaPath) && File.Exists(schemaPath))
        {
            schemaData = await File.ReadAllBytesAsync(schemaPath, token);
            schemaFileName = Path.GetFileName(schemaPath);
        }

        return Result<CompileContractResponse>.Success(new()
        {
            CompiledCode = wasmBytecode,
            CompiledCodeFileName = Path.GetFileName(wasmPath),
            Shema = schemaData,
            ShemaFileName = schemaFileName,
            ContentType = "application/wasm"
        });
    }
}