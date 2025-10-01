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
        /// Retrieves all celestial bodies for the authenticated avatar.
        /// </summary>
        /// <returns>List of all celestial bodies associated with the current avatar.</returns>
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

        [HttpGet("{id}")]
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

        [HttpPost]
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

        [HttpPut("{id}")]
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

        [HttpDelete("{id}")]
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

        [HttpGet("by-type/{type}")]
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

        [HttpGet("in-space/{spaceId}")]
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
    }
}
