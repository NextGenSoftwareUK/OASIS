using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Geo Hot Spots management endpoints for creating, updating, and managing STAR geo hot spots.
    /// Geo hot spots represent geographical locations of interest, events, or activities within the STAR universe.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GeoHotSpotsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all geo hot spots in the system.
        /// </summary>
        /// <returns>List of all geo hot spots available in the STAR system.</returns>
        /// <response code="200">Geo hot spots retrieved successfully</response>
        /// <response code="400">Error retrieving geo hot spots</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllGeoHotSpots()
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.LoadAllAsync(AvatarId, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<GeoHotSpot>>
                {
                    IsError = true,
                    Message = $"Error loading geo hot spots: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific geo hot spot by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to retrieve.</param>
        /// <returns>The requested geo hot spot details.</returns>
        /// <response code="200">Geo hot spot retrieved successfully</response>
        /// <response code="400">Error retrieving geo hot spot</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGeoHotSpot(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<GeoHotSpot>
                {
                    IsError = true,
                    Message = $"Error loading geo hot spot: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new geo hot spot for the authenticated avatar.
        /// </summary>
        /// <param name="hotSpot">The geo hot spot details to create.</param>
        /// <returns>The created geo hot spot with assigned ID and metadata.</returns>
        /// <response code="200">Geo hot spot created successfully</response>
        /// <response code="400">Error creating geo hot spot</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGeoHotSpot([FromBody] GeoHotSpot hotSpot)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.UpdateAsync(AvatarId, hotSpot);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<GeoHotSpot>
                {
                    IsError = true,
                    Message = $"Error creating geo hot spot: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates an existing geo hot spot by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to update.</param>
        /// <param name="hotSpot">The updated geo hot spot details.</param>
        /// <returns>The updated geo hot spot with modified data.</returns>
        /// <response code="200">Geo hot spot updated successfully</response>
        /// <response code="400">Error updating geo hot spot</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateGeoHotSpot(Guid id, [FromBody] GeoHotSpot hotSpot)
        {
            try
            {
                hotSpot.Id = id;
                var result = await _starAPI.GeoHotSpots.UpdateAsync(AvatarId, hotSpot);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<GeoHotSpot>
                {
                    IsError = true,
                    Message = $"Error updating geo hot spot: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deletes a geo hot spot by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Geo hot spot deleted successfully</response>
        /// <response code="400">Error deleting geo hot spot</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteGeoHotSpot(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting geo hot spot: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves geo hot spots within a specified radius of given coordinates.
        /// </summary>
        /// <param name="latitude">The latitude coordinate for the search center.</param>
        /// <param name="longitude">The longitude coordinate for the search center.</param>
        /// <param name="radiusKm">The search radius in kilometers (default: 10.0).</param>
        /// <returns>List of geo hot spots within the specified radius.</returns>
        /// <response code="200">Nearby geo hot spots retrieved successfully</response>
        /// <response code="400">Error retrieving nearby geo hot spots</response>
        [HttpGet("nearby")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNearbyGeoHotSpots([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 10.0)
        {
            try
            {
                throw new NotImplementedException("LoadAllNearAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<GeoHotSpot>>
                {
                    IsError = true,
                    Message = $"Error loading nearby geo hot spots: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
