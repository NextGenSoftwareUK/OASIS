using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec
{
    /// <summary>
    /// Service for executing Aztec CLI commands to submit real transactions
    /// Uses Aztec CLI (aztec-wallet) OR Node.js bridge service to interact with Aztec network - NO MOCKS
    /// </summary>
    public class AztecCLIService
    {
        private readonly string _nodeUrl;
        private readonly string _aztecCliPath;
        private readonly string _walletDataDir;
        private readonly string _nodeJsServiceUrl;
        private readonly HttpClient _httpClient;
        private readonly bool _useNodeJsService;

        public AztecCLIService(string nodeUrl = null, string aztecCliPath = null, string walletDataDir = null, string nodeJsServiceUrl = null)
        {
            _nodeUrl = nodeUrl ?? "https://aztec-testnet-fullnode.zkv.xyz";
            _aztecCliPath = aztecCliPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aztec", "bin", "aztec-wallet");
            _walletDataDir = walletDataDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aztec", "wallet");
            _nodeJsServiceUrl = nodeJsServiceUrl ?? Environment.GetEnvironmentVariable("AZTEC_BRIDGE_SERVICE_URL") ?? "http://localhost:3002";
            _httpClient = new HttpClient();
            _useNodeJsService = !string.IsNullOrEmpty(_nodeJsServiceUrl);
        }

        /// <summary>
        /// Submit a transaction using Aztec CLI or Node.js service
        /// </summary>
        public async Task<OASISResult<string>> SendTransactionAsync(
            string accountAlias,
            string contractAddress,
            string functionName,
            object[] functionArgs,
            string paymentMethod = "fpc-sponsored,fpc=contracts:sponsoredfpc")
        {
            var result = new OASISResult<string>();
            try
            {
                // Try Node.js service first (if available)
                if (_useNodeJsService)
                {
                    try
                    {
                        var nodeResult = await SendTransactionViaNodeJsAsync(accountAlias, contractAddress, functionName, functionArgs);
                        if (!nodeResult.IsError)
                        {
                            return nodeResult;
                        }
                        // Fall back to CLI if Node.js service fails
                    }
                    catch
                    {
                        // Fall back to CLI
                    }
                }
                // Build aztec-wallet send command
                var args = new StringBuilder();
                args.Append($"send {functionName}");
                args.Append($" --node-url {_nodeUrl}");
                args.Append($" --from accounts:{accountAlias}");
                args.Append($" --payment method={paymentMethod}");
                args.Append($" --contract-address {contractAddress}");

                // Add function arguments
                if (functionArgs != null && functionArgs.Length > 0)
                {
                    foreach (var arg in functionArgs)
                    {
                        args.Append($" --args {arg}");
                    }
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _aztecCliPath,
                    Arguments = args.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                };

                // Set environment variables
                processStartInfo.Environment["NODE_URL"] = _nodeUrl;
                processStartInfo.Environment["PATH"] = $"{Path.GetDirectoryName(_aztecCliPath)}:{Environment.GetEnvironmentVariable("PATH")}";

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    result.IsError = true;
                    result.Message = "Failed to start Aztec CLI process";
                    return result;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    result.IsError = true;
                    result.Message = $"Aztec CLI error: {error}";
                    return result;
                }

                // Parse transaction hash from output
                // Aztec CLI typically outputs: "Transaction sent: 0x..."
                var txHash = ExtractTransactionHash(output);
                if (string.IsNullOrEmpty(txHash))
                {
                    result.IsError = true;
                    result.Message = "Could not extract transaction hash from Aztec CLI output";
                    return result;
                }

                result.Result = txHash;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Deploy a contract using Aztec CLI
        /// </summary>
        public async Task<OASISResult<string>> DeployContractAsync(
            string accountAlias,
            string contractName,
            object[] constructorArgs,
            string paymentMethod = "fpc-sponsored,fpc=contracts:sponsoredfpc")
        {
            var result = new OASISResult<string>();
            try
            {
                var args = new StringBuilder();
                args.Append("deploy");
                args.Append($" --node-url {_nodeUrl}");
                args.Append($" --from accounts:{accountAlias}");
                args.Append($" --payment method={paymentMethod}");
                args.Append($" --alias {contractName.ToLower()}");

                if (constructorArgs != null && constructorArgs.Length > 0)
                {
                    foreach (var arg in constructorArgs)
                    {
                        args.Append($" --args {arg}");
                    }
                }

                args.Append(" --no-wait"); // Don't wait for confirmation

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _aztecCliPath,
                    Arguments = args.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                };

                processStartInfo.Environment["NODE_URL"] = _nodeUrl;
                processStartInfo.Environment["PATH"] = $"{Path.GetDirectoryName(_aztecCliPath)}:{Environment.GetEnvironmentVariable("PATH")}";

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    result.IsError = true;
                    result.Message = "Failed to start Aztec CLI process";
                    return result;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    result.IsError = true;
                    result.Message = $"Aztec CLI error: {error}";
                    return result;
                }

                // Parse contract address from output
                var contractAddress = ExtractContractAddress(output);
                if (string.IsNullOrEmpty(contractAddress))
                {
                    result.IsError = true;
                    result.Message = "Could not extract contract address from Aztec CLI output";
                    return result;
                }

                result.Result = contractAddress;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Get transaction receipt using Aztec CLI
        /// </summary>
        public async Task<OASISResult<AztecTransactionReceipt>> GetTransactionReceiptAsync(string txHash)
        {
            var result = new OASISResult<AztecTransactionReceipt>();
            try
            {
                // Use aztec CLI to get transaction info
                // Note: Aztec CLI doesn't have a direct get-receipt command,
                // so we'll query via the testnet client instead
                result.IsError = true;
                result.Message = "Use AztecTestnetClient.GetTransactionStatusAsync for transaction receipts";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }

        private string ExtractTransactionHash(string output)
        {
            // Look for patterns like "Transaction sent: 0x..." or "txHash: 0x..."
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("0x") && line.Length > 66) // Aztec tx hashes are 66 chars (0x + 64 hex)
                {
                    var parts = line.Split(new[] { "0x" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        var hashPart = "0x" + parts[1].Substring(0, Math.Min(64, parts[1].Length));
                        if (hashPart.Length == 66) // Valid hash length
                        {
                            return hashPart;
                        }
                    }
                }
            }
            return null;
        }

        private string ExtractContractAddress(string output)
        {
            // Look for contract address in output
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Contract deployed at") || line.Contains("Address:"))
                {
                    var parts = line.Split(new[] { "0x" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        var addrPart = "0x" + parts[1].Substring(0, Math.Min(64, parts[1].Length));
                        if (addrPart.Length == 66)
                        {
                            return addrPart;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Submit transaction via Node.js bridge service (uses Aztec.js SDK)
        /// </summary>
        private async Task<OASISResult<string>> SendTransactionViaNodeJsAsync(
            string accountAlias,
            string contractAddress,
            string functionName,
            object[] functionArgs)
        {
            var result = new OASISResult<string>();
            try
            {
                var payload = new
                {
                    accountAlias,
                    contractAddress,
                    functionName,
                    args = functionArgs ?? Array.Empty<object>()
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_nodeJsServiceUrl}/api/send-transaction", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    result.IsError = true;
                    result.Message = $"Node.js service error: {response.StatusCode} - {responseContent}";
                    return result;
                }

                var txResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                if (txResponse?.success == true && txResponse?.txHash != null)
                {
                    result.Result = txResponse.txHash.ToString();
                    result.IsError = false;
                }
                else
                {
                    result.IsError = true;
                    result.Message = "Node.js service returned unsuccessful response";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }
    }

    public class AztecTransactionReceipt
    {
        [JsonProperty("txHash")]
        public string TxHash { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("blockNumber")]
        public ulong? BlockNumber { get; set; }
    }
}

