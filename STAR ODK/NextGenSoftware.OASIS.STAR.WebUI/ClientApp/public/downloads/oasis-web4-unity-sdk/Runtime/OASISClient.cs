using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace NextGenSoftware.OASIS.API.Unity
{
    /// <summary>
    /// Main client for interacting with the OASIS Web4 API from Unity
    /// </summary>
    public class OASISClient
    {
        private readonly HttpClient _httpClient;
        private readonly OASISConfig _config;

        public AvatarAPI Avatar { get; }
        public DataAPI Data { get; }
        public SearchAPI Search { get; }
        public ProviderAPI Providers { get; }

        public OASISClient(OASISConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(config.BaseUrl),
                Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
            };

            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
            }

            // Initialize API modules
            Avatar = new AvatarAPI(this);
            Data = new DataAPI(this);
            Search = new SearchAPI(this);
            Providers = new ProviderAPI(this);
        }

        /// <summary>
        /// Check OASIS API health status
        /// </summary>
        public async Task<OASISResult<HealthCheckResponse>> GetHealthAsync()
        {
            return await GetAsync<HealthCheckResponse>("/health");
        }

        #region HTTP Methods

        internal async Task<OASISResult<T>> GetAsync<T>(string endpoint)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Get, endpoint);
        }

        internal async Task<OASISResult<T>> PostAsync<T>(string endpoint, object data = null)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Post, endpoint, data);
        }

        internal async Task<OASISResult<T>> PutAsync<T>(string endpoint, object data = null)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Put, endpoint, data);
        }

        internal async Task<OASISResult<T>> DeleteAsync<T>(string endpoint)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Delete, endpoint);
        }

        private async Task<OASISResult<T>> ExecuteRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            object data = null)
        {
            var result = new OASISResult<T>();
            int attempts = 0;

            while (attempts < _config.RetryAttempts)
            {
                try
                {
                    var request = new HttpRequestMessage(method, endpoint);

                    if (data != null)
                    {
                        var json = JsonConvert.SerializeObject(data);
                        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    }

                    var response = await _httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        result.Result = JsonConvert.DeserializeObject<T>(responseContent);
                        result.IsError = false;
                        result.Message = "Success";
                        return result;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                        result.ErrorDetails = responseContent;

                        // Don't retry on client errors (4xx)
                        if ((int)response.StatusCode < 500)
                        {
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.IsError = true;
                    result.Message = ex.Message;
                    result.Exception = ex;
                    
                    Debug.LogError($"[OASIS] Request failed (attempt {attempts + 1}/{_config.RetryAttempts}): {ex.Message}");
                }

                attempts++;
                if (attempts < _config.RetryAttempts)
                {
                    await Task.Delay(_config.RetryDelayMs * attempts); // Exponential backoff
                }
            }

            return result;
        }

        #endregion
    }

    #region Configuration

    /// <summary>
    /// Configuration for OASIS client
    /// </summary>
    [Serializable]
    public class OASISConfig
    {
        public string BaseUrl { get; set; } = "https://api.oasis.earth/api/v1";
        public string ApiKey { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public bool UseTestnet { get; set; } = false;
        public bool AutoFailover { get; set; } = true;
        public List<string> PreferredProviders { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result wrapper for all OASIS operations
    /// </summary>
    [Serializable]
    public class OASISResult<T>
    {
        public T Result { get; set; }
        public bool IsError { get; set; }
        public string Message { get; set; }
        public string ErrorDetails { get; set; }
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Health check response
    /// </summary>
    [Serializable]
    public class HealthCheckResponse
    {
        public string Status { get; set; }
        public string Version { get; set; }
        public long Timestamp { get; set; }
    }

    #endregion
}

