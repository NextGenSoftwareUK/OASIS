using System.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public class BlockStackClient
    {
        private readonly HttpClient _httpClient;
        public string GaiaHubUrl { get; set; } = "https://hub.blockstack.org";
        public string AppDomain { get; set; } = "blockstack.example";
        public string StacksApiUrl { get; set; } = "https://api.stacks.co";
        
        public BlockStackClient()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "OASIS-BlockStack-Client/1.0");
        }
        
        /// <summary>Gaia hubs store per-address buckets and expose no listing of users, so this cannot be answered by the
        /// hub; it fails explicitly instead of returning invented directories.</summary>
        public Task<List<string>> ListUserDirectoriesAsync()
            => Task.FromException<List<string>>(new NotSupportedException(
                "Gaia hubs expose no user listing; look records up by their Gaia address instead."));

        /// <summary>Reads a Gaia file. Returns null only when the hub reports the file absent (404); any other failure throws.</summary>
        public async Task<Dictionary<string, object>> GetFileAsync(string filePath)
        {
            using var response = await _httpClient.GetAsync($"{GaiaHubUrl}/store/{filePath}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gaia hub read of {filePath} failed ({(int)response.StatusCode}): {content}");
            return JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidDataException($"Gaia hub returned an empty document for {filePath}.");
        }

        /// <summary>Writes a Gaia file; throws unless the hub acknowledges the write.</summary>
        public async Task PutFileAsync(string filePath, Dictionary<string, object> data)
        {
            var jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PutAsync($"{GaiaHubUrl}/store/{filePath}", content);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gaia hub write of {filePath} failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        }

        /// <summary>Deletes a Gaia file. Returns false when it was already absent (404); any other failure throws.</summary>
        public async Task<bool> DeleteFileAsync(string filePath)
        {
            using var response = await _httpClient.DeleteAsync($"{GaiaHubUrl}/store/{filePath}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gaia hub delete of {filePath} failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
            return true;
        }

        /// <summary>Lists Gaia files; throws on any hub failure rather than reporting an empty store.</summary>
        public async Task<List<Dictionary<string, object>>> ListFilesAsync(string directory = "")
        {
            using var response = await _httpClient.GetAsync($"{GaiaHubUrl}/list-files/{directory}");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gaia hub listing of {directory} failed ({(int)response.StatusCode}): {content}");
            return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<Dictionary<string, object>>();
        }
        
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
    
    // Supporting classes for BlockStack API responses
    public class StacksAddressesResponse
    {
        public List<StacksAddress> Results { get; set; } = new List<StacksAddress>();
    }
    
    public class StacksAddress
    {
        public string Address { get; set; }
        public string Type { get; set; }
        public string ScriptHash { get; set; }
    }
    
    public class GaiaHubInfo
    {
        public string ChallengeText { get; set; }
        public List<string> ReadUrlPrefix { get; set; } = new List<string>();
        public List<string> WriteUrlPrefix { get; set; } = new List<string>();
    }
}
