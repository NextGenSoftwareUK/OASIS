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

        /// <summary>
        /// Creates a new instance of <see cref="AvatarController"/>.
        /// </summary>
        /// <param name="httpClient">HTTP client used to call WEB4 OASIS API endpoints.</param>
        public AvatarController(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // TODO: Make this configurable - get from appsettings.json
                var response = await _httpClient.PostAsync("https://localhost:5002/api/avatar/authenticate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var authResult = JsonConvert.DeserializeObject<OASISResult<AuthenticateResponse>>(responseContent);
                    
                    if (!authResult.IsError && authResult.Result != null)
                    {
                        // Set the JWT token in the response headers for the client to use
                        Response.Headers["Authorization"] = $"Bearer {authResult.Result.JwtToken}";
                        
                        return Ok(authResult);
                    }
                    else
                    {
                        return BadRequest(authResult);
                    }
                }
                else
                {
                    return BadRequest(new OASISResult<string>
                    {
                        IsError = true,
                        Message = $"Authentication failed with status: {response.StatusCode}"
                    });
                }
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

