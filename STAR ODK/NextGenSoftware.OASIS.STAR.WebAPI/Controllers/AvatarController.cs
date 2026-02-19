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
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using System.Threading;

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

        // STARAPI implementation for inventory endpoints (Web4 doesn't have /api/avatar/inventory)
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());
        private static readonly SemaphoreSlim _bootLock = new(1, 1);

        private async Task EnsureStarApiBootedAsync()
        {
            if (_starAPI.IsOASISBooted)
                return;

            await _bootLock.WaitAsync();
            try
            {
                if (_starAPI.IsOASISBooted)
                    return;

                var boot = await _starAPI.BootOASISAsync("admin", "admin");
                if (boot.IsError)
                    throw new OASISException(boot.Message ?? "Failed to ignite WEB5 STAR API runtime.");

                // Set LoggedInAvatar to the authenticated avatar so InventoryItems property works
                if (Avatar != null)
                {
                    AvatarManager.LoggedInAvatar = Avatar;
                }
            }
            finally
            {
                _bootLock.Release();
            }
        }

        /// <summary>
        /// Forwards a request to WEB4 OASIS API and returns the response.
        /// </summary>
        private async Task<IActionResult> ForwardToWeb4Async(HttpMethod method, string endpoint, HttpContent? content = null)
        {
            if (string.IsNullOrWhiteSpace(_web4OasisApiBaseUrl))
            {
                return BadRequest(new OASISResult<string>
                {
                    IsError = true,
                    Message = "WEB4 OASIS API base URL is not configured. Please set 'Web4OasisApiBaseUrl' in appsettings.json or 'WEB4_OASIS_API_BASE_URL' environment variable."
                });
            }

            var candidates = new[] { _web4OasisApiBaseUrl }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            HttpResponseMessage? response = null;
            Exception? lastException = null;
            string? lastErrorUrl = null;

            // Forward Authorization header if present
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", authHeader.ToString());
            }

            foreach (var baseUrl in candidates)
            {
                try
                {
                    var request = new HttpRequestMessage(method, $"{baseUrl}{endpoint}");
                    if (content != null)
                        request.Content = content;

                    // Copy query string if present
                    if (Request.QueryString.HasValue)
                    {
                        var uriBuilder = new UriBuilder(request.RequestUri!);
                        uriBuilder.Query = Request.QueryString.Value?.ToString().TrimStart('?') ?? string.Empty;
                        request.RequestUri = uriBuilder.Uri;
                    }

                    response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        break;

                    lastErrorUrl = baseUrl;
                    var errorContent = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(errorContent))
                    {
                        return new ContentResult
                        {
                            StatusCode = (int)response.StatusCode,
                            ContentType = "application/json",
                            Content = errorContent
                        };
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    lastErrorUrl = baseUrl;
                }
            }

            if (response is null && lastException is not null)
            {
                return BadRequest(new OASISResult<string>
                {
                    IsError = true,
                    Message = $"Unable to reach WEB4 OASIS API at {lastErrorUrl ?? "any configured URL"}. Error: {lastException.Message}",
                    Exception = lastException
                });
            }

            if (response is null)
            {
                return BadRequest(new OASISResult<string>
                {
                    IsError = true,
                    Message = $"Unable to reach WEB4 OASIS API. Tried: {string.Join(", ", candidates)}"
                });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/json",
                Content = responseContent
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="AvatarController"/>.
        /// </summary>
        /// <param name="httpClient">HTTP client used to call WEB4 OASIS API endpoints.</param>
        public AvatarController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _web4OasisApiBaseUrl =
                configuration["Web4OasisApiBaseUrl"] ??
                configuration["OASIS:Web4OasisApiBaseUrl"] ??
                configuration["WEB4_OASIS_API_BASE_URL"] ??
                Environment.GetEnvironmentVariable("WEB4_OASIS_API_BASE_URL") ??
                string.Empty;
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
                
                if (string.IsNullOrWhiteSpace(_web4OasisApiBaseUrl))
                {
                    return BadRequest(new OASISResult<string>
                    {
                        IsError = true,
                        Message = "WEB4 OASIS API base URL is not configured. Please set 'Web4OasisApiBaseUrl' in appsettings.json or 'WEB4_OASIS_API_BASE_URL' environment variable."
                    });
                }

                var candidates = new[] { _web4OasisApiBaseUrl }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.TrimEnd('/'))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                HttpResponseMessage? response = null;
                Exception? lastException = null;
                string? lastErrorUrl = null;
                
                foreach (var baseUrl in candidates)
                {
                    try
                    {
                        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        response = await _httpClient.PostAsync($"{baseUrl}/api/avatar/authenticate", content);
                        if (response.IsSuccessStatusCode)
                            break;
                        
                        // Store the error response for better error reporting
                        lastErrorUrl = baseUrl;
                        var errorContent = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(errorContent))
                        {
                            // Forward the WEB4 error response
                            return new ContentResult
                            {
                                StatusCode = (int)response.StatusCode,
                                ContentType = "application/json",
                                Content = errorContent
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        lastErrorUrl = baseUrl;
                    }
                }

                if (response is null && lastException is not null)
                {
                    return BadRequest(new OASISResult<string>
                    {
                        IsError = true,
                        Message = $"Unable to reach WEB4 OASIS API at {lastErrorUrl ?? "any configured URL"}. Error: {lastException.Message}",
                        Exception = lastException
                    });
                }
                
                if (response is null)
                {
                    return BadRequest(new OASISResult<string>
                    {
                        IsError = true,
                        Message = $"Unable to reach WEB4 OASIS API authenticate endpoint. Tried: {string.Join(", ", candidates)}"
                    });
                }
                
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

                // Forward WEB4 error response
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
        /// Gets the current authenticated avatar. Delegates to WEB4 OASIS API.
        /// </summary>
        /// <returns>
        /// 200 OK with <see cref="IAvatar"/> when authenticated; 401 Unauthorized if no JWT present.
        /// </returns>
        [HttpGet("current")]
        [ProducesResponseType(typeof(OASISResult<IAvatar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentAvatar()
        {
            return await ForwardToWeb4Async(HttpMethod.Get, "/api/avatar/get-logged-in-avatar");
        }

        #region Avatar Inventory Management

        /// <summary>
        /// Gets all inventory items owned by the authenticated avatar.
        /// This is the avatar's actual inventory (items they own), not items they created.
        /// Inventory is shared across all games, apps, websites, and services.
        /// Delegates to WEB4 OASIS API.
        /// </summary>
        [HttpGet("inventory")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IInventoryItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetInventory()
        {
            // Implement directly in WEB5 using STARAPI (Web4 doesn't have this endpoint)
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return BadRequest(new OASISResult<IEnumerable<IInventoryItem>>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    });
                }

                await EnsureStarApiBootedAsync();
                var result = await _starAPI.InventoryItems.LoadAllForAvatarAsync(AvatarId, false, 0);
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IInventoryItem>>
                {
                    IsError = true,
                    Message = $"Error loading avatar inventory: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Adds an item to the avatar's inventory.
        /// Delegates to WEB4 OASIS API.
        /// </summary>
        [HttpPost("inventory")]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddItemToInventory([FromBody] JsonElement payload)
        {
            // Current implementation: Delegates to WEB4 OASIS API
            var json = payload.GetRawText();
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return await ForwardToWeb4Async(HttpMethod.Post, "/api/avatar/inventory", content);

            // TODO: Potential future WEB5 STARAPI implementation (commented out for now):
            //try
            //{
            //    if (AvatarId == Guid.Empty)
            //    {
            //        return BadRequest(new OASISResult<IInventoryItem>
            //        {
            //            IsError = true,
            //            Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
            //        });
            //    }

            //    var name = payload.TryGetProperty("Name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
            //        ? nameProp.GetString() ?? "Inventory Item"
            //        : "Inventory Item";
            //    var description = payload.TryGetProperty("Description", out var descProp) && descProp.ValueKind == JsonValueKind.String
            //        ? descProp.GetString() ?? string.Empty
            //        : string.Empty;

            //    await EnsureStarApiBootedAsync();
            //    var result = await _starAPI.InventoryItems.CreateAsync(AvatarId, name, description, null, null, null);
            //    if (result.IsError)
            //        return BadRequest(result);
            //    
            //    return Ok(result);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(new OASISResult<IInventoryItem>
            //    {
            //        IsError = true,
            //        Message = $"Error adding item to inventory: {ex.Message}",
            //        Exception = ex
            //    });
            //}
        }

        /// <summary>
        /// Removes an item from the avatar's inventory.
        /// Delegates to WEB4 OASIS API.
        /// </summary>
        [HttpDelete("inventory/{itemId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveItemFromInventory(Guid itemId)
        {
            // Current implementation: Delegates to WEB4 OASIS API
            return await ForwardToWeb4Async(HttpMethod.Delete, $"/api/avatar/inventory/{itemId}");

            // TODO: Potential future WEB5 STARAPI implementation (commented out for now):
            //try
            //{
            //    if (AvatarId == Guid.Empty)
            //    {
            //        return BadRequest(new OASISResult<bool>
            //        {
            //            IsError = true,
            //            Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
            //        });
            //    }

            //    await EnsureStarApiBootedAsync();
            //    var result = await _starAPI.InventoryItems.DeleteAsync(AvatarId, itemId, 0);
            //    if (result.IsError)
            //        return BadRequest(result);
            //    
            //    return Ok(result);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(new OASISResult<bool>
            //    {
            //        IsError = true,
            //        Message = $"Error removing item from inventory: {ex.Message}",
            //        Exception = ex
            //    });
            //}
        }

        /// <summary>
        /// Checks if the avatar has a specific item in their inventory.
        /// Delegates to WEB4 OASIS API.
        /// </summary>
        [HttpGet("inventory/{itemId}/has")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HasItem(Guid itemId)
        {
            // Current implementation: Delegates to WEB4 OASIS API
            return await ForwardToWeb4Async(HttpMethod.Get, $"/api/avatar/inventory/{itemId}/has");

            // TODO: Potential future WEB5 STARAPI implementation (commented out for now):
            //try
            //{
            //    if (AvatarId == Guid.Empty)
            //    {
            //        return BadRequest(new OASISResult<bool>
            //        {
            //            IsError = true,
            //            Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
            //        });
            //    }

            //    await EnsureStarApiBootedAsync();
            //    var inventoryResult = await _starAPI.InventoryItems.LoadAllForAvatarAsync(AvatarId, false, 0);
            //    if (inventoryResult.IsError)
            //        return BadRequest(inventoryResult);

            //    var hasItem = inventoryResult.Result?.Any(i => i.Id == itemId) ?? false;
            //    var result = new OASISResult<bool> { Result = hasItem, IsError = false, Message = hasItem ? "Avatar has the item." : "Avatar does not have the item." };
            //    return Ok(result);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(new OASISResult<bool>
            //    {
            //        IsError = true,
            //        Message = $"Error checking if avatar has item: {ex.Message}",
            //        Exception = ex
            //    });
            //}
        }

        /// <summary>
        /// Checks if the avatar has a specific item by name in their inventory.
        /// Delegates to WEB4 OASIS API.
        /// </summary>
        [HttpGet("inventory/has-by-name")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HasItemByName([FromQuery] string itemName)
        {
            // Current implementation: Delegates to WEB4 OASIS API
            return await ForwardToWeb4Async(HttpMethod.Get, $"/api/avatar/inventory/has-by-name?itemName={Uri.EscapeDataString(itemName)}");

            // TODO: Potential future WEB5 STARAPI implementation (commented out for now):
            //try
            //{
            //    if (AvatarId == Guid.Empty)
            //    {
            //        return BadRequest(new OASISResult<bool>
            //        {
            //            IsError = true,
            //            Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
            //        });
            //    }

            //    await EnsureStarApiBootedAsync();
            //    var inventoryResult = await _starAPI.InventoryItems.LoadAllForAvatarAsync(AvatarId, false, 0);
            //    if (inventoryResult.IsError)
            //        return BadRequest(inventoryResult);

            //    var hasItem = inventoryResult.Result?.Any(i =>
            //        i.Name?.Equals(itemName, StringComparison.OrdinalIgnoreCase) == true ||
            //        i.Description?.Contains(itemName, StringComparison.OrdinalIgnoreCase) == true) ?? false;
            //    var result = new OASISResult<bool> { Result = hasItem, IsError = false, Message = hasItem ? $"Avatar has item '{itemName}'." : $"Avatar does not have item '{itemName}'." };
            //    return Ok(result);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(new OASISResult<bool>
            //    {
            //        IsError = true,
            //        Message = $"Error checking if avatar has item by name: {ex.Message}",
            //        Exception = ex
            //    });
            //}
        }

        /// <summary>
        /// Searches the avatar's inventory by name or description.
        /// Delegates to WEB4 OASIS API.
        /// </summary>
        [HttpGet("inventory/search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IInventoryItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchInventory([FromQuery] string searchTerm)
        {
            // Current implementation: Delegates to WEB4 OASIS API
            return await ForwardToWeb4Async(HttpMethod.Get, $"/api/avatar/inventory/search?searchTerm={Uri.EscapeDataString(searchTerm)}");

            // TODO: Potential future WEB5 STARAPI implementation (commented out for now):
            //try
            //{
            //    if (AvatarId == Guid.Empty)
            //    {
            //        return BadRequest(new OASISResult<IEnumerable<IInventoryItem>>
            //        {
            //            IsError = true,
            //            Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
            //        });
            //    }

            //    await EnsureStarApiBootedAsync();
            //    var inventoryResult = await _starAPI.InventoryItems.LoadAllForAvatarAsync(AvatarId, false, 0);
            //    if (inventoryResult.IsError)
            //        return BadRequest(inventoryResult);

            //    var matchingItems = inventoryResult.Result?
            //        .Where(i => i.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
            //                    i.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
            //        .Cast<IInventoryItem>()
            //        .ToList() ?? new List<IInventoryItem>();
            //    var result = new OASISResult<IEnumerable<IInventoryItem>> { Result = matchingItems, IsError = false, Message = $"Found {matchingItems.Count} matching items." };
            //    return Ok(result);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(new OASISResult<IEnumerable<IInventoryItem>>
            //    {
            //        IsError = true,
            //        Message = $"Error searching inventory: {ex.Message}",
            //        Exception = ex
            //    });
            //}
        }

        /// <summary>
        /// Gets a specific item from the avatar's inventory by ID.
        /// Delegates to WEB4 OASIS API.
        /// </summary>
        [HttpGet("inventory/{itemId}")]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IInventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetInventoryItem(Guid itemId)
        {
            // Current implementation: Delegates to WEB4 OASIS API
            return await ForwardToWeb4Async(HttpMethod.Get, $"/api/avatar/inventory/{itemId}");

            // TODO: Potential future WEB5 STARAPI implementation (commented out for now):
            //try
            //{
            //    if (AvatarId == Guid.Empty)
            //    {
            //        return BadRequest(new OASISResult<IInventoryItem>
            //        {
            //            IsError = true,
            //            Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
            //        });
            //    }

            //    await EnsureStarApiBootedAsync();
            //    var result = await _starAPI.InventoryItems.LoadAsync(AvatarId, itemId, 0);
            //    if (result.IsError)
            //        return BadRequest(result);
            //    
            //    return Ok(result);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(new OASISResult<IInventoryItem>
            //    {
            //        IsError = true,
            //        Message = $"Error getting inventory item: {ex.Message}",
            //        Exception = ex
            //    });
            //}
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

