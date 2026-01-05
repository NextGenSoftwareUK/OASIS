namespace ScGen.Lib.ImplContracts.Solana;

public sealed partial class SolanaContractDeploy
{
    private const string Localhost = "127.0.0.1";
    private const int DefaultPort = 8899;

    private Result<DeployContractResponse> Validation(IFormFile? keyPair, IFormFile bytecodeFile)
    {
        BaseResult resultValidation = options.Value.IsValid();
        if (!resultValidation.IsSuccess)
        {
            logger.ValidationFailed(nameof(DeployAsync), resultValidation.Error.Message!, httpContext.GetId().ToString());
            return Result<DeployContractResponse>.Failure(resultValidation.Error);
        }


        if (keyPair == null || keyPair.Length == 0)
        {
            logger.ValidationFailed(nameof(DeployAsync),
                Messages.EmptyJson, httpContext.GetId().ToString());
            return Result<DeployContractResponse>.Failure(ResultPatternError.BadRequest(Messages.EmptyAbi));
        }

        if (!keyPair.IsJsonFile())
        {
            logger.ValidationFailed(nameof(DeployAsync),
                Messages.InvalidJsonFile, httpContext.GetId().ToString());
            return Result<DeployContractResponse>.Failure(ResultPatternError.BadRequest(Messages.InvalidAbiFile));
        }


        if (!bytecodeFile.IsSolanaBinFile())
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

    private async Task<bool> EnsureNodeRunningAsync(CancellationToken cancellationToken = default)
    {
        using (logger.BeginScopedOperation(nameof(EnsureNodeRunningAsync), httpContext.GetId().ToString(),
                   httpContext.GetCorrelationId(), PerformanceThreshold.Normal, true))
        {
            try
            {
                if (await CheckIfRunningTcpAsync(_options.RpcUrl.GetHost(), _options.RpcUrl.GetPort(),
                        cancellationToken))
                {
                    logger.OperationCompleted(nameof(EnsureNodeRunningAsync), 0, httpContext.GetCorrelationId());
                    return true;
                }

                bool started = StartNode();
                if (!started)
                {
                    logger.OperationFailed(nameof(EnsureNodeRunningAsync), Messages.FailedToStartNode,
                        httpContext.GetId().ToString(), httpContext.GetCorrelationId());
                    return false;
                }

                logger.OperationCompleted(nameof(EnsureNodeRunningAsync), 0, httpContext.GetCorrelationId());
                return true;
            }
            catch (Exception ex)
            {
                logger.OperationFailedWithException(nameof(EnsureNodeRunningAsync), ex.Message,
                    httpContext.GetCorrelationId());
                return false;
            }
        }
    }


    public async Task<bool> CheckIfRunningTcpAsync(string host = Localhost,
        int port = DefaultPort, CancellationToken cancellationToken = default, int timeoutSeconds = 3)
    {
        using IDisposable scope = logger.BeginScopedOperation(
            nameof(CheckIfRunningTcpAsync), httpContext.GetId().ToString(),
            httpContext.GetCorrelationId(), PerformanceThreshold.Fast, true);

        if (string.IsNullOrWhiteSpace(host))
            return false;

        if (port <= 0 || port > 65535)
            return false;

        try
        {
            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using TcpClient client = new();

            await client.ConnectAsync(host, port, timeoutCts.Token);


            if (client.Connected)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool StartNode()
    {
        using (logger.BeginScopedOperation(nameof(StartNode), httpContext.GetId().ToString(),
                   httpContext.GetCorrelationId(), PerformanceThreshold.Normal, true))
        {
            try
            {
                ProcessExecutionResult result = ProcessExtensions.RunSolanaTestValidator(logger, _options.RpcUrl.GetPort());
                if (!result.IsSuccess)
                {
                    logger.OperationFailed(nameof(StartNode), result.GetErrorMessage() + result.StandardError,
                        httpContext.GetId().ToString(), httpContext.GetCorrelationId());
                    return false;
                }

                Thread.Sleep(TimeSpan.FromSeconds(6));
                return true;
            }
            catch (Exception e)
            {
                logger.OperationFailedWithException(nameof(StartNode),
                    e.Message, httpContext.GetCorrelationId());
                return false;
            }
        }
    }

    private async Task<Result<DeployContractResponse>> DeployProgramAsync(string programPath, string payerKeypairPath,
        CancellationToken token)
    {
        using IDisposable scope = logger.BeginScopedOperation(nameof(DeployProgramAsync),
            httpContext.GetId().ToString(), httpContext.GetCorrelationId(), PerformanceThreshold.Normal, true);
        try
        {
            if (!System.IO.File.Exists(payerKeypairPath))
            {
                string errorMessage = $"Payer keypair file not found: {payerKeypairPath}";
                logger.ValidationFailed(nameof(DeployProgramAsync), errorMessage, httpContext.GetId().ToString());
                return Result<DeployContractResponse>.Failure(ResultPatternError.InternalServerError(errorMessage));
            }

            string workingDirectory = Path.GetDirectoryName(programPath)!;
            string finalProgramKeypairPath = Path.Combine(workingDirectory, "program-keypair.json");

            // Generate program keypair using solana-keygen (this still works, it doesn't use HTTP)
            ProcessExecutionResult keygenResult = await ProcessExtensions.RunCommandAsync(
                "solana-keygen",
                $"new -o {finalProgramKeypairPath} --no-bip39-passphrase --force",
                logger,
                workingDirectory,
                token);

            if (!keygenResult.IsSuccess)
                return Result<DeployContractResponse>.Failure(
                    ResultPatternError.InternalServerError(keygenResult.StandardError));

            // Program ID will be extracted from Node.js script output after deployment
            logger.LogInformation("Using Node.js-based deployment (bypasses broken Solana CLI HTTP bug)");
            string programId = "unknown"; // Will be extracted from output

            // Get program size
            FileInfo programFileInfo = new FileInfo(programPath);
            logger.LogInformation($"Program size: {programFileInfo.Length} bytes");

            // Use our working Node.js deployment script (bypasses broken Solana CLI)
            // Use absolute path to avoid relative path issues
            string deployScriptPath = "/Volumes/Storage/QS_Asset_Rail/deploy-working-final.js";
            
            if (!System.IO.File.Exists(deployScriptPath))
            {
                logger.LogError($"Deploy script not found at: {deployScriptPath}");
                return Result<DeployContractResponse>.Failure(
                    ResultPatternError.InternalServerError($"Deploy script not found: {deployScriptPath}"));
            }

            logger.LogInformation($"Using Node.js deployment script: {deployScriptPath}");
            
            // Call our working Node.js script
            // Parameters: <program.so> <payer.json> <program-keypair.json> <rpc-url>
            string nodeArgs = $"\"{deployScriptPath}\" \"{programPath}\" \"{payerKeypairPath}\" \"{finalProgramKeypairPath}\" {_options.RpcUrl}";
            
            logger.LogInformation($"Node command: node {nodeArgs}");
            
            ProcessExecutionResult deployResult = await ProcessExtensions.RunCommandAsync(
                "node",
                nodeArgs,
                logger,
                workingDirectory,
                token);

            logger.LogInformation($"Deployment exit code: {deployResult.ExitCode}");
            logger.LogInformation($"Deployment stdout: {deployResult.StandardOutput}");
            if (!string.IsNullOrEmpty(deployResult.StandardError))
            {
                logger.LogWarning($"Deployment stderr: {deployResult.StandardError}");
            }

            if (!deployResult.IsSuccess)
                return Result<DeployContractResponse>.Failure(
                    ResultPatternError.InternalServerError($"Deployment failed: {deployResult.StandardError}"));

            // Extract Program ID from Node.js script output
            if (deployResult.StandardOutput.Contains("Program ID:"))
            {
                // Match either "   Program ID: xxxx" or "Program ID: xxxx"
                Match programIdMatch = Regex.Match(deployResult.StandardOutput, @"Program ID:\s+([a-zA-Z0-9]+)");
                if (programIdMatch is { Success: true, Groups.Count: > 1 })
                {
                    programId = programIdMatch.Groups[1].Value;
                    logger.LogInformation($"✅ Extracted Program ID from output: {programId}");
                }
            }
            
            logger.LogInformation($"Deployment succeeded! Program ID: {programId}");
            logger.LogInformation($"Full deployment output:\n{deployResult.StandardOutput}");
            
            // Extract signature from Node.js script output
            string signature = "n/a";
            if (deployResult.StandardOutput.Contains("Transaction confirmed:"))
            {
                Match match = Regex.Match(deployResult.StandardOutput, @"Transaction confirmed:\s+([a-zA-Z0-9]+)");
                if (match is { Success: true, Groups.Count: > 1 })
                {
                    signature = match.Groups[1].Value;
                    logger.LogInformation($"✅ Extracted signature from output: {signature}");
                }
            }
            else if (deployResult.StandardOutput.Contains("Signature:"))
            {
                Match match = Regex.Match(deployResult.StandardOutput, @"Signature:\s+([a-zA-Z0-9]+)");
                if (match is { Success: true, Groups.Count: > 1 })
                {
                    signature = match.Groups[1].Value;
                    logger.LogInformation($"✅ Extracted signature from output: {signature}");
                }
            }

            // Upload IDL if it exists - temporarily disabled due to RPC issues
            // await UploadIdlIfExists(workingDirectory, programId, token);

            DeployContractResponse response = new DeployContractResponse(
                ContractAddress: programId,
                Success: true,
                TransactionHash: signature);

            return Result<DeployContractResponse>.Success(response);
        }

        catch (Exception ex)
        {
            logger.OperationFailedWithException(nameof(DeployProgramAsync), ex.Message,
                httpContext.GetId().ToString(), httpContext.GetCorrelationId());
            return Result<DeployContractResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    private async Task UploadIdlIfExists(string workingDirectory, string programId, CancellationToken token)
    {
        using IDisposable scope = logger.BeginScopedOperation(nameof(UploadIdlIfExists),
            httpContext.GetId().ToString(), httpContext.GetCorrelationId(), PerformanceThreshold.Normal, true);
        try
        {
            // Look for IDL file in target/idl directory
            string idlDirectory = Path.Combine(workingDirectory, "target", "idl");
            if (!Directory.Exists(idlDirectory))
            {
                logger.LogInformation("No IDL directory found, skipping IDL upload");
                return;
            }

            string[] idlFiles = Directory.GetFiles(idlDirectory, "*.json");
            if (idlFiles.Length == 0)
            {
                logger.LogInformation("No IDL files found, skipping IDL upload");
                return;
            }

            string idlFilePath = idlFiles[0]; // Use the first IDL file found
            logger.LogInformation($"Found IDL file: {idlFilePath}, attempting upload for program {programId}");

            // Try to upload IDL using anchor idl init
            string idlArgs = $"idl init --filepath {idlFilePath} --provider.cluster devnet {programId}";
            ProcessExecutionResult idlResult = await ProcessExtensions.RunCommandAsync(
                "anchor",
                idlArgs,
                logger,
                workingDirectory,
                token);

            if (idlResult.IsSuccess)
            {
                logger.LogInformation($"IDL uploaded successfully for program {programId}");
            }
            else
            {
                // Log warning but don't fail deployment
                logger.LogWarning($"IDL upload failed (non-critical): {idlResult.StandardError}");
                logger.LogInformation("Program deployment succeeded, but IDL was not uploaded. Clients must use local IDL files.");
            }
        }
        catch (Exception ex)
        {
            // Log warning but don't fail deployment
            logger.LogWarning($"IDL upload failed with exception (non-critical): {ex.Message}");
        }
    }

    private string ExtractProgramIdFromKeypair(string keypairPath)
    {
        try
        {
            // Read the keypair JSON and convert to public key
            string keypairJson = System.IO.File.ReadAllText(keypairPath);
            byte[] secretKey = System.Text.Json.JsonSerializer.Deserialize<byte[]>(keypairJson) 
                ?? throw new InvalidOperationException("Failed to parse keypair");
            
            // Use the first 32 bytes as the public key (Ed25519 keypair format)
            byte[] publicKeyBytes = new byte[32];
            Array.Copy(secretKey, 32, publicKeyBytes, 0, 32);
            
            // Convert to base58
            string programId = Base58Encode(publicKeyBytes);
            logger.LogInformation($"Extracted program ID from keypair: {programId}");
            return programId;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to extract program ID from keypair: {ex.Message}");
            return "unknown";
        }
    }

    private static string Base58Encode(byte[] data)
    {
        const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        var encoded = new System.Text.StringBuilder();
        var intData = new System.Numerics.BigInteger(data.Reverse().Concat(new byte[] { 0 }).ToArray());
        
        while (intData > 0)
        {
            intData = System.Numerics.BigInteger.DivRem(intData, 58, out var remainder);
            encoded.Insert(0, alphabet[(int)remainder]);
        }
        
        // Add leading zeros
        foreach (var b in data)
        {
            if (b == 0)
                encoded.Insert(0, alphabet[0]);
            else
                break;
        }
        
        return encoded.ToString();
    }
}