using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Zomes management endpoints for creating, updating, and managing STAR zomes.
    /// Zomes represent Holochain applications and distributed computing modules within the STAR ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ZomesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all zomes in the system.
        /// </summary>
        /// <returns>List of all zomes available in the STAR system.</returns>
        /// <response code="200">Zomes retrieved successfully</response>
        /// <response code="400">Error retrieving zomes</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARZome>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARZome>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllZomes()
        {
            try
            {
                var result = await _starAPI.Zomes.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error loading zomes: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific zome by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the zome to retrieve.</param>
        /// <returns>The requested zome details.</returns>
        /// <response code="200">Zome retrieved successfully</response>
        /// <response code="400">Error retrieving zome</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetZome(Guid id)
        {
            try
            {
                var result = await _starAPI.Zomes.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error loading zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new zome for the authenticated avatar.
        /// </summary>
        /// <param name="zome">The zome details to create.</param>
        /// <returns>The created zome with assigned ID and metadata.</returns>
        /// <response code="200">Zome created successfully</response>
        /// <response code="400">Error creating zome</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateZome([FromBody] STARZome zome)
        {
            try
            {
                var result = await _starAPI.Zomes.UpdateAsync(AvatarId, zome);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error creating zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateZome(Guid id, [FromBody] STARZome zome)
        {
            try
            {
                zome.Id = id;
                var result = await _starAPI.Zomes.UpdateAsync(AvatarId, zome);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error updating zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteZome(Guid id)
        {
            try
            {
                var result = await _starAPI.Zomes.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-type/{type}")]
        public async Task<IActionResult> GetZomesByType(string type)
        {
            try
            {
                throw new NotImplementedException("LoadAllOfTypeAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error loading zomes of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("in-space/{spaceId}")]
        public async Task<IActionResult> GetZomesInSpace(Guid spaceId)
        {
            try
            {
                throw new NotImplementedException("LoadAllInSpaceAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error loading zomes in space: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
