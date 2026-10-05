using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS.Infrastructure.Services.Miden
{
    public class MidenAPIClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;

        public MidenAPIClient(string baseUrl, string apiKey = null)
        {
            _baseUrl = baseUrl?.TrimEnd('/');
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
            
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<OASISResult<T>> GetAsync<T>(string endpoint)
        {
            var result = new OASISResult<T>();
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    result.Result = JsonConvert.DeserializeObject<T>(content);
                    result.IsError = false;
                }
                else
                {
                    result.IsError = true;
                    result.Message = $"HTTP {response.StatusCode}: {content}";
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

        public async Task<OASISResult<T>> PostAsync<T>(string endpoint, object payload)
        {
            var result = new OASISResult<T>();
            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    result.Result = JsonConvert.DeserializeObject<T>(responseContent);
                    result.IsError = false;
                }
                else
                {
                    result.IsError = true;
                    result.Message = $"HTTP {response.StatusCode}: {responseContent}";
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
}

