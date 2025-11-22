using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec
{
    /// <summary>
    /// Client for interacting with Aztec Testnet via RPC/API
    /// Uses real Aztec testnet endpoints - NO MOCKS
    /// </summary>
    public class AztecTestnetClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _nodeUrl;
        private readonly string _pxeUrl;

        public AztecTestnetClient(string nodeUrl = null, string pxeUrl = null)
        {
            // Aztec testnet full node
            _nodeUrl = nodeUrl ?? "https://aztec-testnet-fullnode.zkv.xyz";
            // PXE (Private eXecution Environment) for private state queries
            _pxeUrl = pxeUrl ?? "https://aztec-testnet-fullnode.zkv.xyz";
            
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5); // Aztec proofs can take time
        }

        /// <summary>
        /// Get account information from Aztec testnet
        /// </summary>
        public async Task<OASISResult<AztecAccountInfo>> GetAccountInfoAsync(string accountAddress)
        {
            var result = new OASISResult<AztecAccountInfo>();
            try
            {
                // Query PXE for account info
                var response = await _httpClient.GetAsync($"{_pxeUrl}/pxe/get_account_info?address={accountAddress}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    result.IsError = true;
                    result.Message = $"Aztec API error: {response.StatusCode} - {content}";
                    return result;
                }

                var accountInfo = JsonConvert.DeserializeObject<AztecAccountInfo>(content);
                result.Result = accountInfo;
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
        /// Get transaction status from Aztec testnet
        /// </summary>
        public async Task<OASISResult<AztecTransactionStatus>> GetTransactionStatusAsync(string txHash)
        {
            var result = new OASISResult<AztecTransactionStatus>();
            try
            {
                var response = await _httpClient.GetAsync($"{_nodeUrl}/tx/{txHash}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    result.IsError = true;
                    result.Message = $"Aztec API error: {response.StatusCode} - {content}";
                    return result;
                }

                var txStatus = JsonConvert.DeserializeObject<AztecTransactionStatus>(content);
                result.Result = txStatus;
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
        /// Submit a transaction to Aztec testnet
        /// </summary>
        public async Task<OASISResult<string>> SubmitTransactionAsync(object transactionPayload)
        {
            var result = new OASISResult<string>();
            try
            {
                var json = JsonConvert.SerializeObject(transactionPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_nodeUrl}/tx", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    result.IsError = true;
                    result.Message = $"Aztec API error: {response.StatusCode} - {responseContent}";
                    return result;
                }

                var txResponse = JsonConvert.DeserializeObject<AztecTxResponse>(responseContent);
                result.Result = txResponse?.TxHash;
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
        /// Get private notes for an account
        /// </summary>
        public async Task<OASISResult<AztecNote[]>> GetPrivateNotesAsync(string accountAddress)
        {
            var result = new OASISResult<AztecNote[]>();
            try
            {
                var response = await _httpClient.GetAsync($"{_pxeUrl}/pxe/get_notes?address={accountAddress}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    result.IsError = true;
                    result.Message = $"Aztec API error: {response.StatusCode} - {content}";
                    return result;
                }

                var notes = JsonConvert.DeserializeObject<AztecNote[]>(content);
                result.Result = notes ?? Array.Empty<AztecNote>();
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
    }

    // Data models for Aztec API responses
    public class AztecAccountInfo
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("publicKey")]
        public string PublicKey { get; set; }

        [JsonProperty("partialAddress")]
        public string PartialAddress { get; set; }
    }

    public class AztecTransactionStatus
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("txHash")]
        public string TxHash { get; set; }

        [JsonProperty("blockNumber")]
        public ulong? BlockNumber { get; set; }
    }

    public class AztecTxResponse
    {
        [JsonProperty("txHash")]
        public string TxHash { get; set; }
    }

    public class AztecNote
    {
        [JsonProperty("noteId")]
        public string NoteId { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("owner")]
        public string Owner { get; set; }
    }
}

