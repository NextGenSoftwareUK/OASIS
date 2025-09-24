using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IHolonsController : ControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllIHolons()
        {
            try
            {
                var holons = await _starAPI.Holons.LoadAllAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), null);
                return Ok(new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = false,
                    Message = "IHolons loaded successfully",
                    Result = holons
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetIHolon(Guid id)
        {
            try
            {
                var holon = await _starAPI.Holons.LoadAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = $"Error loading holon: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateIHolon([FromBody] IHolon holon)
        {
            try
            {
                var result = await _starAPI.Holons.SaveAsync(holon);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = $"Error creating holon: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIHolon(Guid id, [FromBody] IHolon holon)
        {
            try
            {
                holon.Id = id;
                var result = await _starAPI.Holons.SaveAsync(holon);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = $"Error updating holon: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIHolon(Guid id)
        {
            try
            {
                var result = await _starAPI.Holons.DeleteAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting holon: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-type/{type}")]
        public async Task<IActionResult> GetIHolonsByType(string type)
        {
            try
            {
                var holons = await _starAPI.Holons.LoadAllOfTypeAsync(type);
                return Ok(new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = false,
                    Message = $"IHolons of type {type} loaded successfully",
                    Result = holons
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-parent/{parentId}")]
        public async Task<IActionResult> GetIHolonsByParent(Guid parentId)
        {
            try
            {
                var holons = await _starAPI.Holons.LoadAllForParentAsync(parentId);
                return Ok(new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = false,
                    Message = "IHolons for parent loaded successfully",
                    Result = holons
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons for parent: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-metadata")]
        public async Task<IActionResult> GetIHolonsByMetadata([FromQuery] string key, [FromQuery] string value)
        {
            try
            {
                var holons = await _starAPI.Holons.LoadAllByMetaDataAsync(key, value);
                return Ok(new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = false,
                    Message = "IHolons by metadata loaded successfully",
                    Result = holons
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons by metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
