using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.AbiBinToJson;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.AbiJsonToBin;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetBlock;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetInfo;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRawAbi;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRequiredKeys;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetTableRows;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.Transaction;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient
{
    public class EosClient : IEosClient
    {
        private readonly Uri _eosHostNodeUri;
        private readonly HttpClient _httpClient;

        public EosClient(Uri eosHostNodeUri)
        {
            _eosHostNodeUri = eosHostNodeUri;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10),
                BaseAddress = _eosHostNodeUri
            };
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public async Task<GetNodeInfoResponseDto> GetNodeInfo()
        {
            return await SendRequest<GetNodeInfoResponseDto, object>(null, HttpMethod.Get,
                new Uri(_eosHostNodeUri + "v1/chain/get_info"));
        }

        public async Task<GetTableRowsResponseDto<T>> GetTableRows<T>(GetTableRowsRequestDto getTableRowsRequest)
        {
            return await SendRequest<GetTableRowsResponseDto<T>, GetTableRowsRequestDto>(getTableRowsRequest,
                HttpMethod.Post, new Uri(_eosHostNodeUri + "v1/chain/get_table_rows"));
        }

        public async Task<string[]> GetCurrencyBalance(GetCurrencyBalanceRequestDto getCurrencyBalanceRequestDto)
        {
            return await SendRequest<string[], GetCurrencyBalanceRequestDto>(getCurrencyBalanceRequestDto,
                HttpMethod.Post, new Uri(_eosHostNodeUri + "v1/chain/get_currency_balance"));
        }

        public async Task<GetAccountResponseDto> GetAccount(GetAccountDtoRequest getAccountDtoRequest)
        {
            return await SendRequest<GetAccountResponseDto, GetAccountDtoRequest>(getAccountDtoRequest,
                HttpMethod.Post, new Uri(_eosHostNodeUri + "v1/chain/get_account"));
        }

        public async Task<AbiJsonToBinResponseDto> AbiJsonToBin(AbiJsonToBinRequestDto abiJsonToBinRequestDto)
        {
            return await SendRequest<AbiJsonToBinResponseDto, AbiJsonToBinRequestDto>(abiJsonToBinRequestDto,
                HttpMethod.Post, new Uri(_eosHostNodeUri + "v1/chain/abi_json_to_bin"));
        }

        public async Task<string> AbiBinToJson(AbiBinToJsonRequestDto abiJsonToBinRequestDto)
        {
            return await SendRequest<string, AbiBinToJsonRequestDto>(abiJsonToBinRequestDto, HttpMethod.Post,
                new Uri(_eosHostNodeUri + "v1/chain/abi_bin_to_json"));
        }

        public async Task<string> SendTransaction(PerformTransactionRequestDto performTransactionRequestDto)
        {
            return await SendRequest<string, PerformTransactionRequestDto>(performTransactionRequestDto,
                HttpMethod.Post,
                new Uri(_eosHostNodeUri + "v1/chain/send_transaction"));
        }

        public async Task<string> PushTransaction(PerformTransactionRequestDto performTransactionRequestDto)
        {
            return await SendRequest<string, PerformTransactionRequestDto>(performTransactionRequestDto,
                HttpMethod.Post,
                new Uri(_eosHostNodeUri + "v1/chain/push_transactions"));
        }

        public async Task<GetRawAbiResponseDto> GetRawAbi(GetRawAbiRequestDto getRawAbiRequestDto)
        {
            return await SendRequest<GetRawAbiResponseDto, GetRawAbiRequestDto>(getRawAbiRequestDto, HttpMethod.Post,
                new Uri(_eosHostNodeUri + "v1/chain/get_raw_abi"));
        }

        public async Task<GetBlockResponseDto> GetBlock(GetBlockRequestDto getBlockRequestDto)
        {
            return await SendRequest<GetBlockResponseDto, GetBlockRequestDto>(getBlockRequestDto, HttpMethod.Post,
                new Uri(_eosHostNodeUri + "v1/chain/get_block"));
        }

        public async Task<GetBlockHeaderStateResponseDto> GetBlockHeaderState(GetBlockRequestDto getBlockRequestDto)
        {
            return await SendRequest<GetBlockHeaderStateResponseDto, GetBlockRequestDto>(getBlockRequestDto,
                HttpMethod.Post,
                new Uri(_eosHostNodeUri + "v1/chain/get_block_header_state"));
        }

        public async Task<string> GetRequiredKeys(GetRequiredKeysRequestDto getRequiredKeysRequestDto)
        {
            return await SendRequest<string, GetRequiredKeysRequestDto>(getRequiredKeysRequestDto, HttpMethod.Post,
                new Uri(_eosHostNodeUri + "v1/chain/get_required_keys"));
        }

        /// <summary>
        ///     Generic method for sending http requests
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="httpMethod">Http methods</param>
        /// <param name="uri">Host endpoint</param>
        /// <typeparam name="TResponse">Generic object as http-payload response</typeparam>
        /// <typeparam name="TRequest">Generic object as http-body request</typeparam>
        /// <returns>Received object from host</returns>
        /// <exception cref="ArgumentNullException">If some of input parameter is null</exception>
        private async Task<TResponse> SendRequest<TResponse, TRequest>(TRequest request, HttpMethod httpMethod, Uri uri)
        {
            // Validate input parameters
            if (httpMethod == null)
                throw new ArgumentNullException(nameof(httpMethod));
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            try
            {
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = httpMethod,
                    RequestUri = uri
                };

                if (request != null)
                {
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
                    httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(request));
                }

                // Send request into EOS-node endpoint
                var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage);
                if (!httpResponseMessage.IsSuccessStatusCode)
                    throw new HttpRequestException(
                        $"Provider: EOS. Incorrect response was received from endpoint! Endpoint: {uri.AbsoluteUri}.");

                var httpResponseBodyContent = await httpResponseMessage.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TResponse>(httpResponseBodyContent);
            }
            catch (Exception e)
            {
                LoggingManager.Log(
                    $"Provider: EOS. Error occured while performing the eos-request! Endpoint: {uri.AbsoluteUri}. Message: " +
                    e.Message, LogType.Error);
                throw;
            }
        }

        private void ReleaseUnmanagedResources()
        {
            try
            {
                _httpClient.CancelPendingRequests();
                _httpClient.Dispose();
            }
            catch 
            { 
            
            }
        }

        ~EosClient()
        {
            ReleaseUnmanagedResources();
        }
    }
}