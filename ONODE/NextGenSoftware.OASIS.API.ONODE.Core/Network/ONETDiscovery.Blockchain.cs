using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// Real Ethereum-compatible JSON-RPC eth_call against a configurable ONET registry smart contract, split
    /// into its own file for readability. Performs a genuine HTTP JSON-RPC request, computes the real
    /// Keccak-256 function selector from a human-readable signature (see Keccak256.cs), and decodes a real
    /// ABI-encoded dynamic array of addresses from the response - none of this is simulated.
    /// </summary>
    public partial class ONETDiscovery
    {
        /// <summary>JSON-RPC endpoint to send eth_call requests to. Empty by default - no real RPC endpoint is configured out of the box.</summary>
        public string BlockchainRpcEndpointUrl { get; set; } = string.Empty;

        /// <summary>
        /// Real eth_call JSON-RPC against ONETRegistryContractAddress/BlockchainRpcEndpointUrl, decoding the
        /// result as a Solidity address[] (a dynamic array of 20-byte addresses, ABI-encoded per the
        /// standard: offset word, length word, then one right-padded 32-byte word per address). Previously
        /// this was a stub that unconditionally returned a failed/empty ContractResult.
        /// </summary>
        private async Task<ContractResult> CallSmartContractFunctionAsync(BlockchainQuery query)
        {
            var result = new ContractResult();

            if (string.IsNullOrEmpty(query.ContractAddress))
            {
                result.ErrorMessage = "No ONET registry contract address configured.";
                return result;
            }

            if (string.IsNullOrEmpty(BlockchainRpcEndpointUrl))
            {
                result.ErrorMessage = "No blockchain RPC endpoint configured (set ONETDiscovery.BlockchainRpcEndpointUrl).";
                return result;
            }

            // Prefer an explicitly-supplied selector (Parameters["selector"]) for callers who already know
            // it, but otherwise compute the real Keccak-256 selector from FunctionName so callers only need
            // to supply a human-readable signature like "getRegisteredNodes()" - no separate Web3 tooling or
            // pre-computed hex required.
            var selector = query.Parameters.TryGetValue("selector", out var s) ? s?.ToString() : null;
            if (string.IsNullOrEmpty(selector))
            {
                if (string.IsNullOrEmpty(query.FunctionName))
                {
                    result.ErrorMessage = "No function selector or FunctionName configured.";
                    return result;
                }

                var signature = query.FunctionName.Contains('(') ? query.FunctionName : query.FunctionName + "()";
                selector = Keccak256.ComputeFunctionSelectorHex(signature);
            }

            try
            {
                var requestBody = "{\"jsonrpc\":\"2.0\",\"method\":\"eth_call\",\"params\":[{\"to\":\"" +
                    query.ContractAddress + "\",\"data\":\"" + selector + "\"},\"latest\"],\"id\":1}";

                using var cts = new System.Threading.CancellationTokenSource(query.Timeout);
                using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(BlockchainRpcEndpointUrl, content, cts.Token);
                var responseBody = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"RPC endpoint returned {(int)httpResponse.StatusCode}: {responseBody}";
                    return result;
                }

                using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var errorElement))
                {
                    result.ErrorMessage = $"RPC error: {errorElement.GetRawText()}";
                    return result;
                }

                if (!root.TryGetProperty("result", out var resultElement))
                {
                    result.ErrorMessage = "RPC response had no 'result' field.";
                    return result;
                }

                var hexResult = resultElement.GetString();
                var addresses = DecodeAddressArray(hexResult);

                result.Success = true;
                result.Data = addresses;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error calling ONET registry contract: {ex.Message}", ex);
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Decodes a Solidity dynamic address[] return value from eth_call hex output, per the standard ABI
        /// encoding: a 32-byte head containing the offset to the array data, then at that offset a 32-byte
        /// length word, then one 32-byte word per element (each holding a right-aligned 20-byte address).
        /// </summary>
        private static List<string> DecodeAddressArray(string? hex)
        {
            var addresses = new List<string>();
            if (string.IsNullOrEmpty(hex))
                return addresses;

            hex = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex.Substring(2) : hex;
            var bytes = Convert.FromHexString(hex);

            if (bytes.Length < 64)
                return addresses;

            // First word: byte offset (from the start of the return data) to the array's length word.
            var offset = (int)ReadUInt256AsLong(bytes, 0);
            if (offset < 0 || offset + 32 > bytes.Length)
                return addresses;

            var length = (int)ReadUInt256AsLong(bytes, offset);
            var elementsStart = offset + 32;

            for (int i = 0; i < length; i++)
            {
                var wordStart = elementsStart + (i * 32);
                if (wordStart + 32 > bytes.Length)
                    break;

                // An address occupies the low 20 bytes of its 32-byte word.
                var addressBytes = bytes.Skip(wordStart + 12).Take(20).ToArray();
                addresses.Add("0x" + Convert.ToHexString(addressBytes).ToLowerInvariant());
            }

            return addresses;
        }

        /// <summary>Reads a 32-byte big-endian word as a long, sufficient for realistic array offsets/lengths (not a full uint256).</summary>
        private static long ReadUInt256AsLong(byte[] bytes, int wordStart)
        {
            long value = 0;
            for (int i = wordStart + 24; i < wordStart + 32; i++) // last 8 bytes of the 32-byte word
                value = (value << 8) | bytes[i];
            return value;
        }

        /// <summary>Converts the raw registered addresses returned by the registry contract into NodeInfo entries. The on-chain registry only stores identity (address), not network reachability - so Address is the same on-chain address, not a host:port; a real deployment would need a companion off-chain or on-chain mapping from address to network endpoint.</summary>
        private List<NodeInfo> ParseNodeInfoFromBlockchainData(object data)
        {
            if (data is not List<string> addresses)
                return new List<NodeInfo>();

            return addresses.Select(address => new NodeInfo
            {
                Id = address,
                Address = address,
                IsActive = true,
                LastSeen = DateTime.UtcNow
            }).ToList();
        }
    }
}
