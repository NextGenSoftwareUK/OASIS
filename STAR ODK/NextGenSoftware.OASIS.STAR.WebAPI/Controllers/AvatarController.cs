using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Avatar endpoints for authenticating against the WEB4 OASIS API and accessing the current STAR session avatar.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AvatarController : STARControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _web4OasisApiBaseUrl;

        /// <summary>
        /// Creates a new instance of <see cref="AvatarController"/>.
        /// </summary>
        /// <param name="httpClient">HTTP client used to call WEB4 OASIS API endpoints.</param>
        public AvatarController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _web4OasisApiBaseUrl =
                configuration["Web4OasisApiBaseUrl"] ??
                configuration["WEB4_OASIS_API_BASE_URL"] ??
                "http://localhost:5003";
        }

        /// <summary>
        /// Authenticates with the WEB4 OASIS API and returns a JWT token for subsequent requests.
        /// </summary>
        /// <param name="model">Authentication request containing username and password.</param>
        /// <returns>
        /// 200 OK with <see cref="AuthenticateResponse"/> on success; 400 BadRequest on failure with details.
        /// </returns>
        [HttpPost("authenticate")]
        [ProducesResponseType(typeof(OASISResult<AuthenticateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest model)
        {
            try
            {
                // Call the WEB4 OASIS API authenticate endpoint
                var authenticateRequest = new
                {
                    username = model.Username,
                    password = model.Password
                };

                var json = JsonConvert.SerializeObject(authenticateRequest);
                var candidates = new[] { _web4OasisApiBaseUrl, "http://localhost:5003", "https://localhost:5002" }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.TrimEnd('/'))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                HttpResponseMessage? response = null;
                Exception? lastException = null;
                foreach (var baseUrl in candidates)
                {
                    try
                    {
                        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        response = await _httpClient.PostAsync($"{baseUrl}/api/avatar/authenticate", content);
                        if (response.IsSuccessStatusCode)
                            break;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                }

                if (response is null && lastException is not null)
                    throw lastException;
                if (response is null)
                    throw new InvalidOperationException("Unable to reach WEB4 OASIS API authenticate endpoint.");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var jwtToken = ExtractStringFromJson(responseContent, "jwtToken");
                    if (!string.IsNullOrWhiteSpace(jwtToken))
                        Response.Headers["Authorization"] = $"Bearer {jwtToken}";

                    return new ContentResult
                    {
                        StatusCode = StatusCodes.Status200OK,
                        ContentType = "application/json",
                        Content = responseContent
                    };
                }

                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    ContentType = "application/json",
                    Content = string.IsNullOrWhiteSpace(responseContent)
                        ? JsonConvert.SerializeObject(new OASISResult<string>
                        {
                            IsError = true,
                            Message = $"Authentication failed with status: {response.StatusCode}"
                        })
                        : responseContent
                };
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<string>
                {
                    IsError = true,
                    Message = $"Error during authentication: {ex.Message}"
                });
            }
        }

        private static string? ExtractStringFromJson(string json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                return ExtractStringRecursive(doc.RootElement, propertyName);
            }
            catch
            {
                return null;
            }
        }

        private static string? ExtractStringRecursive(System.Text.Json.JsonElement element, string propertyName)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                        property.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        return property.Value.GetString();
                    }

                    var nested = ExtractStringRecursive(property.Value, propertyName);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
            }
            else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested = ExtractStringRecursive(item, propertyName);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the current authenticated avatar from the STAR session context.
        /// </summary>
        /// <returns>
        /// 200 OK with <see cref="IAvatar"/> when authenticated; 401 Unauthorized if no JWT present.
        /// </returns>
        [HttpGet("current")]
        [ProducesResponseType(typeof(OASISResult<IAvatar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentAvatar()
        {
            if (Avatar == null)
            {
                return Unauthorized(new OASISResult<string>
                {
                    IsError = true,
                    Message = "No authenticated avatar found. Please authenticate first."
                });
            }

            return Ok(new OASISResult<IAvatar>
            {
                Result = Avatar,
                IsError = false,
                Message = "Avatar retrieved successfully"
            });
        }

        #region Avatar Inventory Management

        /// <summary>
        /// Gets all inventory items owned by the authenticated avatar
        /// This is the avatar's actual inventory (items they own), not items they created
        /// Inventory is shared across all games, apps, websites, and services
        /// </summary>
        [HttpGet("inventory")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IInventoryItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetInventory()
        {
            try
            {
                var result = await AvatarManager.Instance.GetAvatarInventoryAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IInventoryItem>>
                {
                    IsError = true,
                    Message = $"Error getting avatar inventory: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Adds an item to the avatar's inventory
        /// The item can be from the STARNET store (created by anyone) or a new item
        /// </summary>
        [HttpPost("inventory")]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddItemToInventory([FromBody] InventoryItem item)
        {
            try
            {
                var result = await AvatarManager.Instance.AddItemToAvatarInventoryAsync(AvatarId, item);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IInventoryItem>
                {
                    IsError = true,
                    Message = $"Error adding item to inventory: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Removes an item from the avatar's inventory
        /// </summary>
        [HttpDelete("inventory/{itemId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveItemFromInventory(Guid itemId)
        {
            try
            {
                var result = await AvatarManager.Instance.RemoveItemFromAvatarInventoryAsync(AvatarId, itemId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error removing item from inventory: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Checks if the avatar has a specific item in their inventory
        /// </summary>
        [HttpGet("inventory/{itemId}/has")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HasItem(Guid itemId)
        {
            try
            {
                var result = await AvatarManager.Instance.AvatarHasItemAsync(AvatarId, itemId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error checking for item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Checks if the avatar has a specific item by name in their inventory
        /// </summary>
        [HttpGet("inventory/has-by-name")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HasItemByName([FromQuery] string itemName)
        {
            try
            {
                var result = await AvatarManager.Instance.AvatarHasItemByNameAsync(AvatarId, itemName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error checking for item by name: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches the avatar's inventory by name or description
        /// </summary>
        [HttpGet("inventory/search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IInventoryItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchInventory([FromQuery] string searchTerm)
        {
            try
            {
                var result = await AvatarManager.Instance.SearchAvatarInventoryAsync(AvatarId, searchTerm);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IInventoryItem>>
                {
                    IsError = true,
                    Message = $"Error searching inventory: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets a specific item from the avatar's inventory by ID
        /// </summary>
        [HttpGet("inventory/{itemId}")]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetInventoryItem(Guid itemId)
        {
            try
            {
                var result = await AvatarManager.Instance.GetAvatarInventoryItemAsync(AvatarId, itemId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IInventoryItem>
                {
                    IsError = true,
                    Message = $"Error getting inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        #endregion
    }

    public class AuthenticateRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AuthenticateResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
        public bool IsBeamedIn { get; set; }
        public DateTime? LastBeamedIn { get; set; }
    }
}

