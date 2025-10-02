using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// NFTs management endpoints for creating, updating, and managing STAR NFTs.
    /// NFTs represent non-fungible tokens and digital assets within the STAR universe.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NFTsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all NFTs in the system.
        /// </summary>
        /// <returns>List of all NFTs available in the STAR system.</returns>
        /// <response code="200">NFTs retrieved successfully</response>
        /// <response code="400">Error retrieving NFTs</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllNFTs()
        {
            try
            {
                var result = await _starAPI.NFTs.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARNFT>>
                {
                    IsError = true,
                    Message = $"Error loading NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific NFT by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to retrieve.</param>
        /// <returns>The requested NFT details.</returns>
        /// <response code="200">NFT retrieved successfully</response>
        /// <response code="400">Error retrieving NFT</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNFT(Guid id)
        {
            try
            {
                var result = await _starAPI.NFTs.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error loading NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new NFT for the authenticated avatar.
        /// </summary>
        /// <param name="nft">The NFT details to create.</param>
        /// <returns>The created NFT with assigned ID and metadata.</returns>
        /// <response code="200">NFT created successfully</response>
        /// <response code="400">Error creating NFT</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateNFT([FromBody] STARNFT nft)
        {
            try
            {
                var result = await _starAPI.NFTs.UpdateAsync(AvatarId, nft);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error creating NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNFT(Guid id, [FromBody] STARNFT nft)
        {
            try
            {
                nft.Id = id;
                var result = await _starAPI.NFTs.UpdateAsync(AvatarId, nft);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error updating NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNFT(Guid id)
        {
            try
            {
                var result = await _starAPI.NFTs.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/clone")]
        public async Task<IActionResult> CloneNFT(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.NFTs.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}


