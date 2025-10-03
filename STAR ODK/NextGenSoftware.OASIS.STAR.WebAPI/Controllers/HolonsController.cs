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
        /// Retrieves all holons in the system.
        /// </summary>
        /// <returns>List of all holons available in the STAR system.</returns>
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

        /// <summary>
        /// Retrieves a specific holon by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to retrieve.</param>
        /// <returns>The requested holon details.</returns>
        /// <response code="200">Holon retrieved successfully</response>
        /// <response code="400">Error retrieving holon</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Creates a new holon for the authenticated avatar.
        /// </summary>
        /// <param name="holon">The holon details to create.</param>
        /// <returns>The created holon with assigned ID and metadata.</returns>
        /// <response code="200">Holon created successfully</response>
        /// <response code="400">Error creating holon</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Updates an existing holon by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to update.</param>
        /// <param name="holon">The updated holon details.</param>
        /// <returns>The updated holon with modified data.</returns>
        /// <response code="200">Holon updated successfully</response>
        /// <response code="400">Error updating holon</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Deletes a holon by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Holon deleted successfully</response>
        /// <response code="400">Error deleting holon</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Retrieves all holons of a specific type.
        /// </summary>
        /// <param name="type">The type of holons to retrieve.</param>
        /// <returns>List of holons matching the specified type.</returns>
        /// <response code="200">Holons retrieved successfully</response>
        /// <response code="400">Error retrieving holons by type</response>
        [HttpGet("by-type/{type}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Retrieves all holons that belong to a specific parent holon.
        /// </summary>
        /// <param name="parentId">The unique identifier of the parent holon.</param>
        /// <returns>List of child holons for the specified parent.</returns>
        /// <response code="200">Child holons retrieved successfully</response>
        /// <response code="400">Error retrieving child holons</response>
        [HttpGet("by-parent/{parentId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Retrieves all holons that match specific metadata criteria.
        /// </summary>
        /// <param name="key">The metadata key to search for.</param>
        /// <param name="value">The metadata value to match.</param>
        /// <returns>List of holons matching the specified metadata criteria.</returns>
        /// <response code="200">Holons retrieved successfully</response>
        /// <response code="400">Error retrieving holons by metadata</response>
        [HttpGet("by-metadata")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Searches holons by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of holons matching the search query.</returns>
        /// <response code="200">Holons retrieved successfully</response>
        /// <response code="400">Error searching holons</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchHolons([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.Holons.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredHolons = result.Result?.Where(h => 
                    h.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    h.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<STARHolon>>
                {
                    Result = filteredHolons,
                    IsError = false,
                    Message = "Holons retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error searching holons: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves holons by a specific status.
        /// </summary>
        /// <param name="status">The holon status to filter by.</param>
        /// <returns>List of holons matching the specified status.</returns>
        /// <response code="200">Holons retrieved successfully</response>
        /// <response code="400">Error retrieving holons by status</response>
        [HttpGet("by-status/{status}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHolonsByStatus(string status)
        {
            try
            {
                var result = await _starAPI.Holons.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredHolons = result.Result?.Where(h => h.Status?.ToString() == status);
                return Ok(new OASISResult<IEnumerable<STARHolon>>
                {
                    Result = filteredHolons,
                    IsError = false,
                    Message = "Holons retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error retrieving holons by status: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
