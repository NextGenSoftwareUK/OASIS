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
    /// Celestial Spaces management endpoints for creating, updating, and managing STAR celestial spaces.
    /// Celestial spaces represent regions, sectors, and areas within the STAR universe that contain celestial bodies.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CelestialSpacesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all celestial spaces in the system.
        /// </summary>
        /// <returns>List of all celestial spaces available in the STAR system.</returns>
        /// <response code="200">Celestial spaces retrieved successfully</response>
        /// <response code="400">Error retrieving celestial spaces</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllCelestialSpaces()
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error loading celestial spaces: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific celestial space by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to retrieve.</param>
        /// <returns>The requested celestial space details.</returns>
        /// <response code="200">Celestial space retrieved successfully</response>
        /// <response code="400">Error retrieving celestial space</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialSpace(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARCelestialSpace>
                {
                    IsError = true,
                    Message = $"Error loading celestial space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new celestial space for the authenticated avatar.
        /// </summary>
        /// <param name="celestialSpace">The celestial space details to create.</param>
        /// <returns>The created celestial space with assigned ID and metadata.</returns>
        /// <response code="200">Celestial space created successfully</response>
        /// <response code="400">Error creating celestial space</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCelestialSpace([FromBody] STARCelestialSpace celestialSpace)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.UpdateAsync(AvatarId, celestialSpace);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARCelestialSpace>
                {
                    IsError = true,
                    Message = $"Error creating celestial space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates an existing celestial space by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to update.</param>
        /// <param name="celestialSpace">The updated celestial space details.</param>
        /// <returns>The updated celestial space with modified data.</returns>
        /// <response code="200">Celestial space updated successfully</response>
        /// <response code="400">Error updating celestial space</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCelestialSpace(Guid id, [FromBody] STARCelestialSpace celestialSpace)
        {
            try
            {
                celestialSpace.Id = id;
                var result = await _starAPI.CelestialSpaces.UpdateAsync(AvatarId, celestialSpace);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARCelestialSpace>
                {
                    IsError = true,
                    Message = $"Error updating celestial space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deletes a celestial space by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Celestial space deleted successfully</response>
        /// <response code="400">Error deleting celestial space</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCelestialSpace(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting celestial space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves celestial spaces by a specific type.
        /// </summary>
        /// <param name="type">The celestial space type to filter by.</param>
        /// <returns>List of celestial spaces matching the specified type.</returns>
        /// <response code="200">Celestial spaces retrieved successfully</response>
        /// <response code="400">Error retrieving celestial spaces by type</response>
        [HttpGet("by-type/{type}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialSpacesByType(string type)
        {
            try
            {
                throw new NotImplementedException("LoadAllOfTypeAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error loading celestial spaces of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves celestial spaces within a specific parent celestial space.
        /// </summary>
        /// <param name="parentSpaceId">The unique identifier of the parent celestial space.</param>
        /// <returns>List of celestial spaces within the specified parent space.</returns>
        /// <response code="200">Celestial spaces retrieved successfully</response>
        /// <response code="400">Error retrieving celestial spaces in parent space</response>
        [HttpGet("in-space/{parentSpaceId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialSpacesInSpace(Guid parentSpaceId)
        {
            try
            {
                throw new NotImplementedException("LoadAllInSpaceAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error loading celestial spaces in parent space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches celestial spaces by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of celestial spaces matching the search query.</returns>
        /// <response code="200">Celestial spaces retrieved successfully</response>
        /// <response code="400">Error searching celestial spaces</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchCelestialSpaces([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredCelestialSpaces = result.Result?.Where(cs => 
                    cs.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    cs.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<STARCelestialSpace>>
                {
                    Result = filteredCelestialSpaces,
                    IsError = false,
                    Message = "Celestial spaces retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error searching celestial spaces: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
