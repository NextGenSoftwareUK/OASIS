namespace ScGen.Lib.ImplContracts.Solana;

public sealed partial class  SolanaContractCompile
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

    private async Task<Result<CompileContractResponse>> CreateResponseAsync(string tempDir, CancellationToken token)
    {
        string deployDir = Path.Combine(tempDir, "target", "deploy");
        string idlDir = Path.Combine(tempDir, "target", "idl");

        string[] deployAllFiles = Directory.Exists(deployDir) ? Directory.GetFiles(deployDir) : [];
        string[] soFiles = Directory.GetFiles(deployDir, "*.so");
        string[] keypairFiles = Directory.GetFiles(deployDir, "*-keypair.json");

        if (!soFiles.Any())
        {
            throw new FileNotFoundException(
                $"No .so files found in target/deploy directory.\n" +
                $"Files in deployDir: {string.Join(", ", deployAllFiles)}\n");
        }

        if (!keypairFiles.Any())
        {
            throw new FileNotFoundException(
                $"No keypair files found in target/deploy directory.\n" +
                $"Files in deployDir: {string.Join(", ", deployAllFiles)}\n" +
                $"Expected: *-keypair.json files");
        }

        string soPath = soFiles.First();
        string keypairPath = keypairFiles.First();

        byte[] bytecode = await File.ReadAllBytesAsync(soPath, token);
        byte[] keypair = await File.ReadAllBytesAsync(keypairPath, token);

        // Package everything including IDL into a ZIP
        string outputZipPath = Path.Combine(Path.GetTempPath(), $"compiled-{Guid.NewGuid()}.zip");
        using (ZipArchive archive = ZipFile.Open(outputZipPath, ZipArchiveMode.Create))
        {
            // Add .so file
            archive.CreateEntryFromFile(soPath, Path.GetFileName(soPath));
            
            // Add keypair
            archive.CreateEntryFromFile(keypairPath, Path.GetFileName(keypairPath));
            
            // Add IDL files if they exist
            if (Directory.Exists(idlDir))
            {
                string[] idlFiles = Directory.GetFiles(idlDir, "*.json");
                foreach (string idlFile in idlFiles)
                {
                    string entryName = $"idl/{Path.GetFileName(idlFile)}";
                    archive.CreateEntryFromFile(idlFile, entryName);
                    logger.LogInformation($"✅ Included IDL file: {Path.GetFileName(idlFile)}");
                }
                
                if (idlFiles.Length == 0)
                {
                    logger.LogWarning("IDL directory exists but no .json files found");
                }
            }
            else
            {
                logger.LogWarning("No IDL directory found (target/idl). IDL will not be included in output.");
            }
        }

        byte[] zipBytes = await File.ReadAllBytesAsync(outputZipPath, token);
        File.Delete(outputZipPath);

        return Result<CompileContractResponse>.Success(
            new()
            {
                CompiledCode = zipBytes,
                CompiledCodeFileName = Path.GetFileNameWithoutExtension(soPath) + ".zip",
                Shema = keypair,
                ShemaFileName = Path.GetFileName(keypairPath),
                ContentType = "application/zip"
            });
    }
}