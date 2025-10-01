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
        /// Retrieves all celestial spaces for the authenticated avatar.
        /// </summary>
        /// <returns>List of all celestial spaces associated with the current avatar.</returns>
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

        [HttpGet("{id}")]
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

        [HttpPost]
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

        [HttpPut("{id}")]
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

        [HttpDelete("{id}")]
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

        [HttpGet("by-type/{type}")]
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

        [HttpGet("in-space/{parentSpaceId}")]
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
    }
}
