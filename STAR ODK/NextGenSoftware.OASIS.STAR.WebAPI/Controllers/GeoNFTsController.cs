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
    /// Geo NFTs management endpoints for creating, updating, and managing STAR geo NFTs.
    /// Geo NFTs represent location-based non-fungible tokens within the STAR universe.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GeoNFTsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all geo NFTs in the system.
        /// </summary>
        /// <returns>List of all geo NFTs available in the STAR system.</returns>
        /// <response code="200">Geo NFTs retrieved successfully</response>
        /// <response code="400">Error retrieving geo NFTs</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllGeoNFTs()
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadAllAsync(AvatarId, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error loading geo NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific geo NFT by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to retrieve.</param>
        /// <returns>The requested geo NFT details.</returns>
        /// <response code="200">Geo NFT retrieved successfully</response>
        /// <response code="400">Error retrieving geo NFT</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGeoNFT(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARGeoNFT>
                {
                    IsError = true,
                    Message = $"Error loading geo NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new geo NFT for the authenticated avatar.
        /// </summary>
        /// <param name="geoNFT">The geo NFT details to create.</param>
        /// <returns>The created geo NFT with assigned ID and metadata.</returns>
        /// <response code="200">Geo NFT created successfully</response>
        /// <response code="400">Error creating geo NFT</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGeoNFT([FromBody] STARGeoNFT geoNFT)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.UpdateAsync(AvatarId, geoNFT);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARGeoNFT>
                {
                    IsError = true,
                    Message = $"Error creating geo NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates an existing geo NFT by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to update.</param>
        /// <param name="geoNFT">The updated geo NFT details.</param>
        /// <returns>The updated geo NFT with modified data.</returns>
        /// <response code="200">Geo NFT updated successfully</response>
        /// <response code="400">Error updating geo NFT</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateGeoNFT(Guid id, [FromBody] STARGeoNFT geoNFT)
        {
            try
            {
                geoNFT.Id = id;
                var result = await _starAPI.GeoNFTs.UpdateAsync(AvatarId, geoNFT);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARGeoNFT>
                {
                    IsError = true,
                    Message = $"Error updating geo NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deletes a geo NFT by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Geo NFT deleted successfully</response>
        /// <response code="400">Error deleting geo NFT</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteGeoNFT(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting geo NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves geo NFTs within a specified radius of a geographic location.
        /// </summary>
        /// <param name="latitude">The latitude coordinate for the search center.</param>
        /// <param name="longitude">The longitude coordinate for the search center.</param>
        /// <param name="radiusKm">The search radius in kilometers (default: 10.0).</param>
        /// <returns>List of geo NFTs within the specified radius.</returns>
        /// <response code="200">Nearby geo NFTs retrieved successfully</response>
        /// <response code="400">Error retrieving nearby geo NFTs</response>
        [HttpGet("nearby")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNearbyGeoNFTs([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 10.0)
        {
            try
            {
                throw new NotImplementedException("LoadAllNearAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error loading nearby geo NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves geo NFTs associated with a specific avatar.
        /// </summary>
        /// <param name="avatarId">The unique identifier of the avatar.</param>
        /// <returns>List of geo NFTs associated with the specified avatar.</returns>
        /// <response code="200">Avatar geo NFTs retrieved successfully</response>
        /// <response code="400">Error retrieving avatar geo NFTs</response>
        [HttpGet("by-avatar/{avatarId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGeoNFTsByAvatar(Guid avatarId)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadAllAsync(avatarId, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error loading avatar geo NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches geo NFTs by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of geo NFTs matching the search query.</returns>
        /// <response code="200">Geo NFTs retrieved successfully</response>
        /// <response code="400">Error searching geo NFTs</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchGeoNFTs([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredGeoNFTs = result.Result?.Where(gnft => 
                    gnft.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    gnft.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<STARGeoNFT>>
                {
                    Result = filteredGeoNFTs,
                    IsError = false,
                    Message = "Geo NFTs retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error searching geo NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
