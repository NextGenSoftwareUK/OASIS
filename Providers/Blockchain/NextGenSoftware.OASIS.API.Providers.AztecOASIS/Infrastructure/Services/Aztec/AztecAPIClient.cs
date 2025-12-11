using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec
{
    public class AztecAPIClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;

        public AztecAPIClient(string apiBaseUrl, string apiKey = null)
        {
            _apiBaseUrl = apiBaseUrl?.TrimEnd('/') ?? "http://localhost:8080";
            _apiKey = apiKey;
            _httpClient = new HttpClient();

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);
            }
        }

        public async Task<OASISResult<TResponse>> PostAsync<TResponse>(string endpoint, object payload)
        {
            var result = new OASISResult<TResponse>();
            try
            {
                var json = payload == null ? "{}" : JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}{endpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    result.IsError = true;
                    result.Message = $"Aztec API error: {response.StatusCode} - {responseContent}";
                    return result;
                }

                var responseObject = JsonConvert.DeserializeObject<TResponse>(responseContent);
                result.Result = responseObject;
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

        public async Task<OASISResult<TResponse>> GetAsync<TResponse>(string endpoint, IDictionary<string, string> queryParams = null)
        {
            var result = new OASISResult<TResponse>();
            try
            {
                var urlBuilder = new StringBuilder($"{_apiBaseUrl}{endpoint}");
                if (queryParams != null && queryParams.Count > 0)
                {
                    urlBuilder.Append("?");
                    foreach (var kv in queryParams)
                    {
                        urlBuilder.Append($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}&");
                    }
                    urlBuilder.Length--; // remove trailing &
                }

                var response = await _httpClient.GetAsync(urlBuilder.ToString());
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    result.IsError = true;
                    result.Message = $"Aztec API error: {response.StatusCode} - {responseContent}";
                    return result;
                }

                var responseObject = JsonConvert.DeserializeObject<TResponse>(responseContent);
                result.Result = responseObject;
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
}

