using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Native.EndPoint;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryItemsController : ControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllInventoryItems()
        {
            try
            {
                var items = await _starAPI.InventoryItems.LoadAllAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), null);
                return Ok(new OASISResult<IEnumerable<InventoryItem>>
                {
                    IsError = false,
                    Message = "Inventory items loaded successfully",
                    Result = items
                });
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetInventoryItem(Guid id)
        {
            try
            {
                var item = await _starAPI.InventoryItems.LoadAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), id);
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

        [HttpPost]
        public async Task<IActionResult> CreateInventoryItem([FromBody] InventoryItem item)
        {
            try
            {
                var result = await _starAPI.InventoryItems.SaveAsync(item);
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
                var result = await _starAPI.InventoryItems.SaveAsync(item);
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
                var result = await _starAPI.InventoryItems.DeleteAsync(id);
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
                var items = await _starAPI.InventoryItems.LoadAllForAvatarAsync(avatarId);
                return Ok(new OASISResult<IEnumerable<InventoryItem>>
                {
                    IsError = false,
                    Message = "Avatar inventory items loaded successfully",
                    Result = items
                });
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
