using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.Shipox.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.Shipox;

/// <summary>
/// HTTP client for communicating with Shipox API
/// Handles authentication, request/response serialization, and error handling
/// </summary>
public class ShipoxApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ISecretVaultService _secretVault;
    private readonly Guid _merchantId;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;
    private ShipoxCredentials _credentials;
    private bool _initialized = false;

    public ShipoxApiClient(string baseUrl, ISecretVaultService secretVault, Guid merchantId)
    {
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
        _merchantId = merchantId;
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Set default headers
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Initializes the API client by retrieving credentials from Secret Vault
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        try
        {
            var credentialsResult = await _secretVault.GetShipoxCredentialsAsync(_merchantId);
            if (credentialsResult.IsError)
            {
                throw new Exception($"Failed to get Shipox credentials from vault: {credentialsResult.Message}");
            }

            _credentials = credentialsResult.Result;
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _credentials.ApiKey);
            _initialized = true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to initialize Shipox API client with credentials from vault: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sends a GET request to the Shipox API
    /// </summary>
    public async Task<OASISResult<T>> GetAsync<T>(string endpoint)
    {
        await InitializeAsync();
        var result = new OASISResult<T>();

        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                result.Result = data;
            }
            else
            {
                result.IsError = true;
                result.Message = $"Shipox API error: {response.StatusCode} - {content}";
            }
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Failed to call Shipox API: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    /// <summary>
    /// Sends a POST request to the Shipox API
    /// </summary>
    public async Task<OASISResult<T>> PostAsync<T>(string endpoint, object payload)
    {
        await InitializeAsync();
        var result = new OASISResult<T>();

        try
        {
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                result.Result = data;
            }
            else
            {
                result.IsError = true;
                result.Message = $"Shipox API error: {response.StatusCode} - {responseContent}";
            }
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Failed to call Shipox API: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    /// <summary>
    /// Sends a PUT request to the Shipox API
    /// </summary>
    public async Task<OASISResult<T>> PutAsync<T>(string endpoint, object payload)
    {
        await InitializeAsync();
        var result = new OASISResult<T>();

        try
        {
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                result.Result = data;
            }
            else
            {
                result.IsError = true;
                result.Message = $"Shipox API error: {response.StatusCode} - {responseContent}";
            }
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Failed to call Shipox API: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    /// <summary>
    /// Sends a DELETE request to the Shipox API
    /// </summary>
    public async Task<OASISResult<bool>> DeleteAsync(string endpoint)
    {
        await InitializeAsync();
        var result = new OASISResult<bool>();

        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                result.Result = true;
            }
            else
            {
                result.IsError = true;
                var content = await response.Content.ReadAsStringAsync();
                result.Message = $"Shipox API error: {response.StatusCode} - {content}";
            }
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Failed to call Shipox API: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

