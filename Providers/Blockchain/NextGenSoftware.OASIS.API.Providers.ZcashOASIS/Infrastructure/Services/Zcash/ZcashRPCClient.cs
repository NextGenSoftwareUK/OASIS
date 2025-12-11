using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash
{
    public class ZcashRPCClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcUrl;
        private readonly string _rpcUser;
        private readonly string _rpcPassword;
        private int _requestId = 1;

        public ZcashRPCClient(string rpcUrl, string rpcUser, string rpcPassword)
        {
            _rpcUrl = rpcUrl;
            _rpcUser = rpcUser;
            _rpcPassword = rpcPassword;
            _httpClient = new HttpClient();
            
            // Set up basic authentication only if credentials are provided
            if (!string.IsNullOrWhiteSpace(_rpcUser) && !string.IsNullOrWhiteSpace(_rpcPassword))
            {
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_rpcUser}:{_rpcPassword}"));
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
            }
        }

        public async Task<OASISResult<bool>> TestConnectionAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var response = await SendRPCRequestAsync("getinfo", null);
                if (response != null)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Successfully connected to Zcash node";
                }
                else
                {
                    result.IsError = true;
                    result.Message = "Failed to connect to Zcash node";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error testing connection: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<string>> SendShieldedTransactionAsync(
            string fromAddress,
            string toAddress,
            decimal amount,
            string memo = null)
        {
            var result = new OASISResult<string>();
            try
            {
                var parameters = new
                {
                    fromaddress = fromAddress,
                    amounts = new[]
                    {
                        new
                        {
                            address = toAddress,
                            amount = amount.ToString("F8"),
                            memo = memo ?? ""
                        }
                    }
                };

                var response = await SendRPCRequestAsync("z_sendmany", parameters);
                if (response != null && response.ContainsKey("result"))
                {
                    var operationId = response["result"]?.ToString();
                    result.Result = operationId;
                    result.IsError = false;
                }
                else
                {
                    result.IsError = true;
                    result.Message = "Failed to send shielded transaction";
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

        public async Task<OASISResult<object>> GetTransactionAsync(string txId)
        {
            var result = new OASISResult<object>();
            try
            {
                var response = await SendRPCRequestAsync("gettransaction", new { txid = txId });
                if (response != null && response.ContainsKey("result"))
                {
                    result.Result = response["result"];
                    result.IsError = false;
                }
                else
                {
                    result.IsError = true;
                    result.Message = "Transaction not found";
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

        public async Task<OASISResult<string>> ExportViewingKeyAsync(string address)
        {
            var result = new OASISResult<string>();
            try
            {
                var response = await SendRPCRequestAsync("z_exportviewingkey", new { zaddr = address });
                if (response != null && response.ContainsKey("result"))
                {
                    result.Result = response["result"]?.ToString();
                    result.IsError = false;
                }
                else
                {
                    result.IsError = true;
                    result.Message = "Failed to export viewing key";
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

        public async Task<OASISResult<string>> GetNewAddressAsync(string addressType = "sapling")
        {
            var result = new OASISResult<string>();
            try
            {
                var response = await SendRPCRequestAsync("getnewaddress", new { type = addressType });
                if (response != null && response.ContainsKey("result"))
                {
                    result.Result = response["result"]?.ToString();
                    result.IsError = false;
                }
                else
                {
                    result.IsError = true;
                    result.Message = "Failed to get new address";
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

        public async Task<OASISResult<decimal>> GetBalanceAsync(string address = null)
        {
            var result = new OASISResult<decimal>();
            try
            {
                string method = address == null ? "getbalance" : "z_getbalance";
                object parameters = address == null ? null : new { address = address };

                var response = await SendRPCRequestAsync(method, parameters);
                if (response != null && response.ContainsKey("result"))
                {
                    var balanceStr = response["result"]?.ToString();
                    if (decimal.TryParse(balanceStr, out decimal balance))
                    {
                        result.Result = balance;
                        result.IsError = false;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = "Failed to parse balance";
                    }
                }
                else
                {
                    result.IsError = true;
                    result.Message = "Failed to get balance";
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

        private async Task<System.Collections.Generic.Dictionary<string, object>> SendRPCRequestAsync(string method, object parameters)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = method,
                @params = parameters,
                id = _requestId++
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_rpcUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"RPC request failed: {response.StatusCode} - {responseContent}");
            }

            var result = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(responseContent);
            return result;
        }
    }
}

