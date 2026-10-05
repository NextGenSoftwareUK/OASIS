using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;

/// <summary>
/// Helper for HTTP client operations with Radix API
/// </summary>
public static class HttpClientHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Posts data to a URL and deserializes the response
    /// </summary>
    public static async Task<OASISResult<TResponse?>> PostAsync<TRequest, TResponse>(
        HttpClient httpClient,
        string url,
        TRequest data,
        CancellationToken token = default)
    {
        var result = new OASISResult<TResponse?>();
        
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync(url, content, token);
            
            if (!response.IsSuccessStatusCode)
            {
                result.IsError = true;
                result.Message = $"HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync(token)}";
                return result;
            }
            
            var responseJson = await response.Content.ReadAsStringAsync(token);
            result.Result = JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions);
            result.IsError = false;
            
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = ex.Message;
            result.Exception = ex;
            return result;
        }
    }
}

