using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Celestial Bodies management endpoints for creating, updating, and managing STAR celestial bodies.
    /// Celestial bodies represent planets, stars, moons, and other astronomical objects in the STAR universe.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CelestialBodiesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all celestial bodies in the system.
        /// </summary>
        /// <returns>List of all celestial bodies available in the STAR system.</returns>
        /// <response code="200">Celestial bodies retrieved successfully</response>
        /// <response code="400">Error retrieving celestial bodies</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllCelestialBodies()
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialBody>>
                {
                    IsError = true,
                    Message = $"Error loading celestial bodies: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific celestial body by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to retrieve.</param>
        /// <returns>The requested celestial body details.</returns>
        /// <response code="200">Celestial body retrieved successfully</response>
        /// <response code="400">Error retrieving celestial body</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialBody(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARCelestialBody>
                {
                    IsError = true,
                    Message = $"Error loading celestial body: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new celestial body for the authenticated avatar.
        /// </summary>
        /// <param name="celestialBody">The celestial body details to create.</param>
        /// <returns>The created celestial body with assigned ID and metadata.</returns>
        /// <response code="200">Celestial body created successfully</response>
        /// <response code="400">Error creating celestial body</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCelestialBody([FromBody] STARCelestialBody celestialBody)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.UpdateAsync(AvatarId, celestialBody);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARCelestialBody>
                {
                    IsError = true,
                    Message = $"Error creating celestial body: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates an existing celestial body by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to update.</param>
        /// <param name="celestialBody">The updated celestial body details.</param>
        /// <returns>The updated celestial body with modified data.</returns>
        /// <response code="200">Celestial body updated successfully</response>
        /// <response code="400">Error updating celestial body</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCelestialBody(Guid id, [FromBody] STARCelestialBody celestialBody)
        {
            try
            {
                celestialBody.Id = id;
                var result = await _starAPI.CelestialBodies.UpdateAsync(AvatarId, celestialBody);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARCelestialBody>
                {
                    IsError = true,
                    Message = $"Error updating celestial body: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deletes a celestial body by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Celestial body deleted successfully</response>
        /// <response code="400">Error deleting celestial body</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCelestialBody(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting celestial body: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves celestial bodies by a specific type.
        /// </summary>
        /// <param name="type">The celestial body type to filter by.</param>
        /// <returns>List of celestial bodies matching the specified type.</returns>
        /// <response code="200">Celestial bodies retrieved successfully</response>
        /// <response code="400">Error retrieving celestial bodies by type</response>
        [HttpGet("by-type/{type}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialBodiesByType(string type)
        {
            try
            {
                throw new NotImplementedException("LoadAllOfTypeAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialBody>>
                {
                    IsError = true,
                    Message = $"Error loading celestial bodies of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves celestial bodies within a specific celestial space.
        /// </summary>
        /// <param name="spaceId">The unique identifier of the celestial space.</param>
        /// <returns>List of celestial bodies within the specified space.</returns>
        /// <response code="200">Celestial bodies retrieved successfully</response>
        /// <response code="400">Error retrieving celestial bodies in space</response>
        [HttpGet("in-space/{spaceId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialBodiesInSpace(Guid spaceId)
        {
            try
            {
                throw new NotImplementedException("LoadAllInSpaceAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialBody>>
                {
                    IsError = true,
                    Message = $"Error loading celestial bodies in space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches celestial bodies by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of celestial bodies matching the search query.</returns>
        /// <response code="200">Celestial bodies retrieved successfully</response>
        /// <response code="400">Error searching celestial bodies</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchCelestialBodies([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredCelestialBodies = result.Result?.Where(cb => 
                    cb.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    cb.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<STARCelestialBody>>
                {
                    Result = filteredCelestialBodies,
                    IsError = false,
                    Message = "Celestial bodies retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialBody>>
                {
                    IsError = true,
                    Message = $"Error searching celestial bodies: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
