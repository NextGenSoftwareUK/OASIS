using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>AvatarAdminController endpoints — part of the Avatar API surface at api/avatar.</summary>
    [Route("api/avatar")]
    [ApiController]
    public class AvatarAdminController : OASISControllerBase
    {
        private AvatarManager AvatarManager => Program.AvatarManager;
        private readonly ILogger<AvatarAdminController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly object StarLogLock = new object();

        public AvatarAdminController(ILogger<AvatarAdminController> logger, IConfiguration configuration, IWebHostEnvironment env)
        {
            _logger = logger;
            _configuration = configuration;
            _env = env;
        }

        private void StarLog(string message, LogLevel level = LogLevel.Information)
        {
            var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] [STAR] {message}";
            _logger.Log(level, "[STAR] {Message}", message);
            try
            {
                var dir = string.IsNullOrEmpty(_env?.ContentRootPath) ? AppContext.BaseDirectory : _env.ContentRootPath;
                if (string.IsNullOrEmpty(dir)) dir = System.IO.Directory.GetCurrentDirectory() ?? ".";
                var path = System.IO.Path.Combine(dir, "star_api.log");
                lock (StarLogLock)
                    System.IO.File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { }
        }

        #region Session Management - OASIS SSO System 🚀

        /// <summary>
        /// Get all active sessions for a specific avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>List of active sessions</returns>
        [HttpGet("{avatarId}/sessions")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionManagement>> GetAvatarSessions(Guid avatarId)
        {
            try
            {
                var result = await AvatarManager.GetAvatarSessionsAsync(avatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionManagement>
                {
                    IsError = true,
                    Message = $"Error retrieving avatar sessions: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Logout avatar from specific sessions (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionIds">List of session IDs to logout</param>
        /// <returns>Success status</returns>
        [HttpPost("{avatarId}/sessions/logout")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<bool>> LogoutAvatarSessions(Guid avatarId, [FromBody] List<string> sessionIds)
        {
            try
            {
                var result = await AvatarManager.LogoutAvatarSessionsAsync(avatarId, sessionIds);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error logging out avatar sessions: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Logout avatar from all sessions (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{avatarId}/sessions/logout-all")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<bool>> LogoutAllAvatarSessions(Guid avatarId)
        {
            try
            {
                var result = await AvatarManager.LogoutAllAvatarSessionsAsync(avatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error logging out all avatar sessions: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Create a new session for an avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionData">Session information</param>
        /// <returns>Created session</returns>
        [HttpPost("{avatarId}/sessions")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>> CreateAvatarSession(Guid avatarId, [FromBody] NextGenSoftware.OASIS.API.Core.Objects.Avatar.CreateSessionRequest sessionData)
        {
            try
            {
                var result = await AvatarManager.CreateAvatarSessionAsync(avatarId, sessionData);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>
                {
                    IsError = true,
                    Message = $"Error creating avatar session: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Update an existing session (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="sessionData">Updated session information</param>
        /// <returns>Updated session</returns>
        [HttpPut("{avatarId}/sessions/{sessionId}")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>> UpdateAvatarSession(Guid avatarId, string sessionId, [FromBody] NextGenSoftware.OASIS.API.Core.Objects.Avatar.UpdateSessionRequest sessionData)
        {
            try
            {
                var result = await AvatarManager.UpdateAvatarSessionAsync(avatarId, sessionId, sessionData);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>
                {
                    IsError = true,
                    Message = $"Error updating avatar session: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Get session statistics for an avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Session statistics</returns>
        [HttpGet("{avatarId}/sessions/stats")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionStats>> GetAvatarSessionStats(Guid avatarId)
        {
            try
            {
                var result = await AvatarManager.GetAvatarSessionStatsAsync(avatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionStats>
                {
                    IsError = true,
                    Message = $"Error retrieving avatar session stats: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #region Avatar Inventory Management

        /// <summary>
        /// Gets all inventory items owned by the authenticated avatar.
        /// This is the avatar's actual inventory (items they own), not items they created.
        /// Inventory is shared across all games, apps, websites, and services.
        /// </summary>
        [HttpGet("inventory")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IEnumerable<IInventoryItem>>> GetAvatarInventory()
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.GetAvatarInventoryAsync(AvatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>>
                {
                    IsError = true,
                    Message = $"Error loading avatar inventory: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Adds an item to the avatar's inventory.
        /// Quantity (default 1): amount to add; if item with same name exists and Stack is true, this is added to existing Quantity.
        /// Stack (default true): if true and item exists by name, increment Quantity; if false and item exists, returns error "Item already exists".
        /// Accepts InventoryItem with Name, Description, optional Quantity, optional Stack, and optional MetaData.
        /// </summary>
        [HttpPost("inventory")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IInventoryItem>> AddItemToAvatarInventory([FromBody] InventoryItem inventoryItem)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                if (inventoryItem == null)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem>
                    {
                        IsError = true,
                        Message = "The request body is required. Please provide a valid Inventory Item object with Name, Description, and optional HolonSubType."
                    }, HttpStatusCode.BadRequest);
                }

                // Ensure HolonType is set if not provided
                if (inventoryItem.HolonType == HolonType.None)
                {
                    inventoryItem.HolonType = HolonType.InventoryItem;
                }

                var result = await AvatarManager.AddItemToAvatarInventoryAsync(AvatarId, inventoryItem);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem>
                {
                    IsError = true,
                    Message = $"Error adding item to inventory: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Decrements an item's quantity in the avatar's inventory. quantity must be 1 or greater. The item is removed only when its quantity reaches 0 after the decrement.
        /// </summary>
        [HttpDelete("inventory/{itemId}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> RemoveItemFromAvatarInventory(Guid itemId, [FromQuery] int quantity = 1)
        {
            var starLogEnabled = _configuration?.GetSection("Star")?["LoggingEnabled"]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

            if (starLogEnabled)
                StarLog($"RemoveItemFromAvatarInventory called: itemId={itemId} quantity={quantity} avatarId={AvatarId}");

            try
            {
                if (AvatarId == Guid.Empty)
                {
                    if (starLogEnabled)
                        StarLog("RemoveItemFromAvatarInventory rejected: AvatarId required", LogLevel.Warning);
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                if (quantity < 1)
                {
                    if (starLogEnabled)
                        StarLog($"RemoveItemFromAvatarInventory rejected: quantity must be >= 1 (got {quantity})", LogLevel.Warning);
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Quantity must be 1 or greater."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.RemoveItemFromAvatarInventoryAsync(AvatarId, itemId, quantity);

                if (starLogEnabled)
                    StarLog($"RemoveItemFromAvatarInventory result: itemId={itemId} quantity={quantity} success={!result.IsError} message={result.Message ?? "(ok)"}");

                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                if (starLogEnabled)
                {
                    StarLog($"RemoveItemFromAvatarInventory exception: itemId={itemId} quantity={quantity} error={ex.GetType().Name}: {ex.Message}", LogLevel.Error);
                    _logger.LogError(ex, "[STAR] RemoveItemFromAvatarInventory exception: itemId={ItemId} quantity={Quantity}", itemId, quantity);
                }
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error removing item from inventory: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Checks if the avatar has a specific item in their inventory.
        /// </summary>
        [HttpGet("inventory/{itemId}/has")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> AvatarHasItem(Guid itemId)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.AvatarHasItemAsync(AvatarId, itemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error checking if avatar has item: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Checks if the avatar has a specific item by name in their inventory.
        /// </summary>
        [HttpGet("inventory/has-by-name")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> AvatarHasItemByName([FromQuery] string itemName)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.AvatarHasItemByNameAsync(AvatarId, itemName);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error checking if avatar has item by name: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Searches the avatar's inventory by name or description.
        /// </summary>
        [HttpGet("inventory/search")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IEnumerable<IInventoryItem>>> SearchAvatarInventory([FromQuery] string searchTerm)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.SearchAvatarInventoryAsync(AvatarId, searchTerm);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>>
                {
                    IsError = true,
                    Message = $"Error searching inventory: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Gets a specific item from the avatar's inventory by ID.
        /// </summary>
        [HttpGet("inventory/{itemId}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IInventoryItem>> GetAvatarInventoryItem(Guid itemId)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.GetAvatarInventoryItemAsync(AvatarId, itemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem>
                {
                    IsError = true,
                    Message = $"Error getting inventory item: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Sends an item from the authenticated avatar's inventory to another avatar.
        /// Target is the recipient's username or avatar Id. Works for all items (STAR and local).
        /// </summary>
        [HttpPost("inventory/send-to-avatar")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> SendItemToAvatar([FromBody] SendItemRequest request)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "AvatarId is required. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }
                if (request == null || string.IsNullOrWhiteSpace(request.Target) || string.IsNullOrWhiteSpace(request.ItemName))
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Target and ItemName are required."
                    }, HttpStatusCode.BadRequest);
                }
                var quantity = request.Quantity < 1 ? 1 : request.Quantity;
                var result = await AvatarManager.SendItemToAvatarAsync(AvatarId, request.Target.Trim(), request.ItemName.Trim(), quantity, request.ItemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error sending item to avatar: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Sends an item from the authenticated avatar's inventory to a clan.
        /// Target is the clan name (or username when clan resolution is not yet implemented). Works for all items (STAR and local).
        /// </summary>
        [HttpPost("inventory/send-to-clan")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> SendItemToClan([FromBody] SendItemRequest request)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "AvatarId is required. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }
                if (request == null || string.IsNullOrWhiteSpace(request.Target) || string.IsNullOrWhiteSpace(request.ItemName))
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Target (clan name) and ItemName are required."
                    }, HttpStatusCode.BadRequest);
                }
                var quantity = request.Quantity < 1 ? 1 : request.Quantity;
                var result = await AvatarManager.SendItemToClanAsync(AvatarId, request.Target.Trim(), request.ItemName.Trim(), quantity, request.ItemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error sending item to clan: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        #endregion
    }
}