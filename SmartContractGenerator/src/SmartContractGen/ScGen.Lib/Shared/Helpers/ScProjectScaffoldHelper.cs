namespace ScGen.Lib.Shared.Helpers;

public static class ScProjectScaffoldHelper
{
    public static void CreateEthereumProjectTemplate(string projectPath, ILogger logger)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(CreateEthereumProjectTemplate));

        if (!Directory.Exists(projectPath))
            Directory.CreateDirectory(projectPath);

        string contractsPath = Path.Combine(projectPath, KeyNames.Contracts);
        if (!File.Exists(Path.Combine(contractsPath)))
            File.Create(contractsPath);

        stopwatch.Stop();
        logger.OperationCompleted(nameof(CreateEthereumProjectTemplate), stopwatch.ElapsedMilliseconds);
    }

    public static void CreateSolanaProjectTemplate(string projectPath, ILogger logger, CancellationToken token = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(CreateSolanaProjectTemplate));

        try
        {
            if (Directory.Exists(projectPath)) return;

            string baseDir = Path.GetFullPath(Path.Combine(projectPath, ".."));

            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);

            const string anchor = "anchor";
            string arguments = new StringBuilder()
                .Append("init ")
                .Append(KeyNames.RustMainTemplate).ToString();

            ProcessExecutionResult result = ProcessExtensions.RunCommand(
                anchor, arguments, logger, baseDir, token, TimeSpan.FromSeconds(15));
            if (!result.IsSuccess)
                throw new Exception(result.GetErrorMessage());

            logger.LogInformation(result.StandardOutput);
        }
        catch (Exception ex)
        {
            logger.OperationFailedWithException(nameof(CreateSolanaProjectTemplate), ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            logger.OperationCompleted(nameof(CreateSolanaProjectTemplate), stopwatch.ElapsedMilliseconds);
        }
    }

    public static void CreateRadixProjectTemplate(string projectPath, ILogger logger)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(CreateRadixProjectTemplate));

        try
        {
            if (Directory.Exists(projectPath)) return;

            string baseDir = Path.GetFullPath(Path.Combine(projectPath, ".."));

            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);

            const string scrypto = "scrypto";
            string arguments = new StringBuilder()
                .Append("new-package ")
                .Append(KeyNames.ScryptoMainTemplate).ToString();

            ProcessExecutionResult result = ProcessExtensions.RunCommand(scrypto, arguments, logger, baseDir);
            if (!result.IsSuccess)
                throw new Exception(result.GetErrorMessage());
            
            logger.LogInformation(result.StandardOutput);
        }
        catch (Exception ex)
        {
            logger.OperationFailedWithException(nameof(CreateRadixProjectTemplate), ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            logger.OperationCompleted(nameof(CreateRadixProjectTemplate), stopwatch.ElapsedMilliseconds);
        }
    }
}