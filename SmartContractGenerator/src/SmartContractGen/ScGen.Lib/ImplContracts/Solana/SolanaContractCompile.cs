namespace ScGen.Lib.ImplContracts.Solana;

public sealed partial class SolanaContractCompile(
    ILogger<SolanaContractCompile> logger,
    IHttpContextAccessor httpContext) : ISolanaContractCompile
{
    private const long MaxFileSize = 100 * 1024 * 1024;

    public async Task<Result<CompileContractResponse>> CompileAsync(IFormFile sourceCodeFile, CancellationToken token = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(CompileAsync),
            httpContext.GetId().ToString(), httpContext.GetCorrelationId());

        Result<CompileContractResponse> validation = Validation(sourceCodeFile);
        if (!validation.IsSuccess) return validation;

        // Use a persistent build cache directory instead of random temp
        string persistentCacheDir = Path.Combine(Path.GetTempPath(), "anchor_build_cache");
        Directory.CreateDirectory(persistentCacheDir);
        
        string tempDir = Path.Combine(persistentCacheDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            string zipPath = Path.Combine(tempDir, "source.zip");
            await using (FileStream fs = new(zipPath, FileMode.Create, FileAccess.Write))
                await sourceCodeFile.CopyToAsync(fs, token);
            ZipFile.ExtractToDirectory(zipPath, tempDir);

            // Copy shared target cache if it exists (for faster incremental builds)
            string sharedTargetCache = Path.Combine(persistentCacheDir, "shared_target");
            string projectTarget = Path.Combine(tempDir, "target");
            
            if (Directory.Exists(sharedTargetCache))
            {
                try
                {
                    CopyDirectory(sharedTargetCache, projectTarget);
                    logger.LogInformation("Reusing cached build artifacts for faster compilation");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not copy cached target directory, building from scratch");
                }
            }

            ProcessExecutionResult result = await ProcessExtensions.RunAnchorAsync(tempDir, logger, token);
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

            // Cache the target directory for next build
            if (Directory.Exists(projectTarget))
            {
                try
                {
                    if (Directory.Exists(sharedTargetCache))
                        Directory.Delete(sharedTargetCache, true);
                    
                    CopyDirectory(projectTarget, sharedTargetCache);
                    logger.LogInformation("Cached build artifacts for future compilations");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not cache target directory");
                }
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
            // Clean up the temp build directory but keep the shared cache
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
            catch { /* Ignore cleanup errors */ }
            
            stopwatch.Stop();
            logger.OperationCompleted(nameof(CompileAsync),
                stopwatch.ElapsedMilliseconds, httpContext.GetCorrelationId());
        }
    }
    
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        
        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }
}