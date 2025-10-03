using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Inventory Items management endpoints for creating, updating, and managing STAR inventory items.
    /// Inventory items represent collectible items, resources, and assets that avatars can own and trade.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryItemsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all inventory items in the system.
        /// </summary>
        /// <returns>List of all inventory items available in the STAR system.</returns>
        /// <response code="200">Inventory items retrieved successfully</response>
        /// <response code="400">Error retrieving inventory items</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<InventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<InventoryItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllInventoryItems()
        {
            try
            {
                var result = await _starAPI.InventoryItems.LoadAllAsync(AvatarId, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<InventoryItem>>
                {
                    IsError = true,
                    Message = $"Error loading inventory items: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific inventory item by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item to retrieve.</param>
        /// <returns>The requested inventory item details.</returns>
        /// <response code="200">Inventory item retrieved successfully</response>
        /// <response code="400">Error retrieving inventory item</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetInventoryItem(Guid id)
        {
            try
            {
                var result = await _starAPI.InventoryItems.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error loading inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new inventory item for the authenticated avatar.
        /// </summary>
        /// <param name="item">The inventory item details to create.</param>
        /// <returns>The created inventory item with assigned ID and metadata.</returns>
        /// <response code="200">Inventory item created successfully</response>
        /// <response code="400">Error creating inventory item</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateInventoryItem([FromBody] InventoryItem item)
        {
            try
            {
                var result = await _starAPI.InventoryItems.UpdateAsync(AvatarId, (InventoryItem)item);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error creating inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInventoryItem(Guid id, [FromBody] InventoryItem item)
        {
            try
            {
                item.Id = id;
                var result = await _starAPI.InventoryItems.UpdateAsync(AvatarId, (InventoryItem)item);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error updating inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventoryItem(Guid id)
        {
            try
            {
                var result = await _starAPI.InventoryItems.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-avatar/{avatarId}")]
        public async Task<IActionResult> GetInventoryItemsByAvatar(Guid avatarId)
        {
            try
            {
                var result = await _starAPI.InventoryItems.LoadAllAsync(avatarId, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<InventoryItem>>
                {
                    IsError = true,
                    Message = $"Error loading avatar inventory items: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
