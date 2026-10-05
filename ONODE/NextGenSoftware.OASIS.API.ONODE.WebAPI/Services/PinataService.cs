using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Service for uploading files to Pinata IPFS
    /// Used for uploading badge images and metadata for NFTs
    /// </summary>
    public class PinataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _pinataJwt;
        private readonly ILogger<PinataService> _logger;

        public PinataService(string pinataJwt, ILogger<PinataService> logger = null)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.pinata.cloud");
            _pinataJwt = pinataJwt;
            _logger = logger;
        }

        /// <summary>
        /// Upload an image file to Pinata IPFS
        /// </summary>
        /// <param name="imageBytes">Image file bytes</param>
        /// <param name="fileName">Filename (e.g., "badge.png")</param>
        /// <returns>IPFS URL</returns>
        public async Task<OASISResult<string>> UploadImageAsync(byte[] imageBytes, string fileName)
        {
            try
            {
                _logger?.LogInformation($"[PinataService] Uploading image: {fileName} ({imageBytes.Length} bytes)");

                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(imageBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                content.Add(fileContent, "file", fileName);

                var request = new HttpRequestMessage(HttpMethod.Post, "/pinning/pinFileToIPFS")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _pinataJwt);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError($"[PinataService] Upload failed: {responseBody}");
                    return new OASISResult<string>
                    {
                        IsError = true,
                        Message = $"Pinata upload failed: {responseBody}"
                    };
                }

                // Parse response to get IPFS hash
                var jsonDoc = JsonDocument.Parse(responseBody);
                if (jsonDoc.RootElement.TryGetProperty("IpfsHash", out var hashElement))
                {
                    var ipfsHash = hashElement.GetString();
                    var ipfsUrl = $"https://gateway.pinata.cloud/ipfs/{ipfsHash}";
                    
                    _logger?.LogInformation($"[PinataService] Upload successful: {ipfsUrl}");
                    
                    return new OASISResult<string>
                    {
                        Result = ipfsUrl,
                        Message = "Image uploaded to IPFS successfully",
                        IsSaved = true
                    };
                }

                return new OASISResult<string>
                {
                    IsError = true,
                    Message = "No IPFS hash in response"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[PinataService] Error uploading to Pinata");
                return new OASISResult<string>
                {
                    IsError = true,
                    Message = $"Error uploading to Pinata: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Upload JSON metadata to Pinata IPFS
        /// </summary>
        /// <param name="metadata">NFT metadata object</param>
        /// <param name="fileName">Filename (e.g., "metadata.json")</param>
        /// <returns>IPFS URL</returns>
        public async Task<OASISResult<string>> UploadMetadataAsync(object metadata, string fileName = "metadata.json")
        {
            try
            {
                _logger?.LogInformation($"[PinataService] Uploading metadata: {fileName}");

                var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                var jsonBytes = Encoding.UTF8.GetBytes(json);

                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(jsonBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                content.Add(fileContent, "file", fileName);

                var request = new HttpRequestMessage(HttpMethod.Post, "/pinning/pinFileToIPFS")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _pinataJwt);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError($"[PinataService] Metadata upload failed: {responseBody}");
                    return new OASISResult<string>
                    {
                        IsError = true,
                        Message = $"Pinata metadata upload failed: {responseBody}"
                    };
                }

                // Parse response to get IPFS hash
                var jsonDoc = JsonDocument.Parse(responseBody);
                if (jsonDoc.RootElement.TryGetProperty("IpfsHash", out var hashElement))
                {
                    var ipfsHash = hashElement.GetString();
                    var ipfsUrl = $"https://gateway.pinata.cloud/ipfs/{ipfsHash}";
                    
                    _logger?.LogInformation($"[PinataService] Metadata upload successful: {ipfsUrl}");
                    
                    return new OASISResult<string>
                    {
                        Result = ipfsUrl,
                        Message = "Metadata uploaded to IPFS successfully",
                        IsSaved = true
                    };
                }

                return new OASISResult<string>
                {
                    IsError = true,
                    Message = "No IPFS hash in response"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[PinataService] Error uploading metadata to Pinata");
                return new OASISResult<string>
                {
                    IsError = true,
                    Message = $"Error uploading metadata to Pinata: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Create NFT metadata and upload to IPFS
        /// </summary>
        public async Task<OASISResult<string>> CreateAndUploadNFTMetadataAsync(
            string name,
            string description,
            string imageUrl,
            object attributes = null)
        {
            try
            {
                var metadata = new
                {
                    name = name,
                    description = description,
                    image = imageUrl,
                    attributes = attributes ?? new object[] { }
                };

                return await UploadMetadataAsync(metadata, $"{name.Replace(" ", "_")}.json");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[PinataService] Error creating NFT metadata");
                return new OASISResult<string>
                {
                    IsError = true,
                    Message = $"Error creating NFT metadata: {ex.Message}"
                };
            }
        }
    }
}





