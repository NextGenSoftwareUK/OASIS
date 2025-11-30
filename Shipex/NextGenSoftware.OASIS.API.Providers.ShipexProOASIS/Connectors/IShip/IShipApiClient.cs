using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;
using Microsoft.Extensions.Logging;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip
{
    /// <summary>
    /// Base HTTP client for communicating with iShip API.
    /// Handles authentication, error handling, retry logic, and request/response processing.
    /// </summary>
    public class IShipApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ISecretVaultService _secretVault;
        private readonly Guid? _merchantId;
        private readonly string _baseUrl;
        private readonly ILogger<IShipApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly int _maxRetries;
        private readonly int _retryDelayMs;
        private string _apiKey;

        public IShipApiClient(string baseUrl, ISecretVaultService secretVault, Guid? merchantId = null, ILogger<IShipApiClient> logger = null, int maxRetries = 3, int retryDelayMs = 1000)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
            _merchantId = merchantId;
            _logger = logger;
            _maxRetries = maxRetries;
            _retryDelayMs = retryDelayMs;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Initializes the API client by retrieving the API key from Secret Vault
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                var apiKeyResult = await _secretVault.GetIShipApiKeyAsync(_merchantId);
                if (apiKeyResult.IsError)
                {
                    throw new Exception($"Failed to get iShip API key from vault: {apiKeyResult.Message}");
                }

                _apiKey = apiKeyResult.Result;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize iShip API client with credentials from vault");
                throw;
            }
        }

        /// <summary>
        /// Executes an HTTP request with retry logic and error handling.
        /// </summary>
        protected async Task<OASISResult<T>> ExecuteRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            object request = null,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<T>();
            var url = endpoint.StartsWith("http") ? endpoint : $"{_baseUrl}/{endpoint.TrimStart('/')}";

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        var delay = _retryDelayMs * (int)Math.Pow(2, attempt - 1); // Exponential backoff
                        _logger?.LogInformation($"Retrying request to {url} (attempt {attempt + 1}/{_maxRetries + 1}) after {delay}ms");
                        await Task.Delay(delay, cancellationToken);
                    }

                    HttpRequestMessage requestMessage = new HttpRequestMessage(method, url);

                    if (request != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
                    {
                        var json = JsonSerializer.Serialize(request, _jsonOptions);
                        requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    }

                    _logger?.LogDebug($"Executing {method} request to {url}");

                    var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            if (typeof(T) == typeof(string))
                            {
                                result.Result = (T)(object)responseContent;
                            }
                            else if (string.IsNullOrWhiteSpace(responseContent))
                            {
                                result.Result = default(T);
                            }
                            else
                            {
                                result.Result = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                            }

                            result.IsError = false;
                            _logger?.LogDebug($"Request to {url} succeeded");
                            return result;
                        }
                        catch (JsonException ex)
                        {
                            _logger?.LogError(ex, $"Failed to deserialize response from {url}: {responseContent}");
                            OASISErrorHandling.HandleError(ref result, $"Failed to deserialize response: {ex.Message}");
                            return result;
                        }
                    }
                    else
                    {
                        // Check if we should retry based on status code
                        if (ShouldRetry(response.StatusCode) && attempt < _maxRetries)
                        {
                            _logger?.LogWarning($"Request to {url} failed with status {response.StatusCode}, will retry. Response: {responseContent}");
                            continue;
                        }

                        // Non-retryable error or max retries reached
                        var errorMessage = $"API request failed with status {response.StatusCode}: {responseContent}";
                        _logger?.LogError($"Request to {url} failed: {errorMessage}");
                        OASISErrorHandling.HandleError(ref result, errorMessage);
                        return result;
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    if (attempt < _maxRetries)
                    {
                        _logger?.LogWarning($"Request to {url} timed out, will retry");
                        continue;
                    }

                    _logger?.LogError(ex, $"Request to {url} timed out after {_maxRetries + 1} attempts");
                    OASISErrorHandling.HandleError(ref result, $"Request timed out: {ex.Message}");
                    return result;
                }
                catch (HttpRequestException ex)
                {
                    if (attempt < _maxRetries)
                    {
                        _logger?.LogWarning(ex, $"HTTP request to {url} failed, will retry");
                        continue;
                    }

                    _logger?.LogError(ex, $"HTTP request to {url} failed after {_maxRetries + 1} attempts");
                    OASISErrorHandling.HandleError(ref result, $"HTTP request failed: {ex.Message}");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Unexpected error executing request to {url}");
                    OASISErrorHandling.HandleError(ref result, $"Unexpected error: {ex.Message}");
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if a request should be retried based on HTTP status code.
        /// </summary>
        private bool ShouldRetry(System.Net.HttpStatusCode statusCode)
        {
            // Retry on server errors (5xx) and rate limiting (429)
            return (int)statusCode >= 500 || statusCode == System.Net.HttpStatusCode.TooManyRequests;
        }

        /// <summary>
        /// GET request helper.
        /// </summary>
        protected Task<OASISResult<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            return ExecuteRequestAsync<T>(HttpMethod.Get, endpoint, null, cancellationToken);
        }

        /// <summary>
        /// POST request helper.
        /// </summary>
        protected Task<OASISResult<T>> PostAsync<T>(string endpoint, object request, CancellationToken cancellationToken = default)
        {
            return ExecuteRequestAsync<T>(HttpMethod.Post, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// PUT request helper.
        /// </summary>
        protected Task<OASISResult<T>> PutAsync<T>(string endpoint, object request, CancellationToken cancellationToken = default)
        {
            return ExecuteRequestAsync<T>(HttpMethod.Put, endpoint, request, cancellationToken);
        }

        /// <summary>
        /// DELETE request helper.
        /// </summary>
        protected Task<OASISResult<T>> DeleteAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            return ExecuteRequestAsync<T>(HttpMethod.Delete, endpoint, null, cancellationToken);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}

