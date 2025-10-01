using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Holon management endpoints for creating, updating, and managing STAR holons.
    /// Holons are the fundamental building blocks of the OASIS system - self-contained units that can contain other holons.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HolonsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all holons for the authenticated avatar.
        /// </summary>
        /// <returns>List of all holons associated with the current avatar.</returns>
        /// <response code="200">Holons retrieved successfully</response>
        /// <response code="400">Error retrieving holons</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllHolons()
        {
            try
            {
                var result = await _starAPI.Holons.LoadAllAsync(AvatarId, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetHolon(Guid id)
        {
            try
            {
                var result = await _starAPI.Holons.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARHolon>
                {
                    IsError = true,
                    Message = $"Error loading holon: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateHolon([FromBody] STARHolon holon)
        {
            try
            {
                var result = await _starAPI.Holons.UpdateAsync(AvatarId, holon);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARHolon>
                {
                    IsError = true,
                    Message = $"Error creating holon: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHolon(Guid id, [FromBody] STARHolon holon)
        {
            try
            {
                holon.Id = id;
                var result = await _starAPI.Holons.UpdateAsync(AvatarId, holon);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARHolon>
                {
                    IsError = true,
                    Message = $"Error updating holon: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHolon(Guid id)
        {
            try
            {
                var result = await _starAPI.Holons.DeleteAsync(AvatarId, id, 0);
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
        public async Task<IActionResult> GetHolonsByType(string type)
        {
            try
            {
                throw new NotImplementedException("LoadAllOfTypeAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-parent/{parentId}")]
        public async Task<IActionResult> GetHolonsByParent(Guid parentId)
        {
            try
            {
                throw new NotImplementedException("LoadAllForParentAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons for parent: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-metadata")]
        public async Task<IActionResult> GetHolonsByMetadata([FromQuery] string key, [FromQuery] string value)
        {
            try
            {
                throw new NotImplementedException("LoadAllByMetaDataAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons by metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
