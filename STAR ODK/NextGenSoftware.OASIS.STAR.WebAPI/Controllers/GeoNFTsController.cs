using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeoNFTsController : ControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllGeoNFTs()
        {
            try
            {
                var geoNFTs = await _starAPI.GeoNFTs.LoadAllAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), null);
                return Ok(new OASISResult<IEnumerable<ISTARGeoNFT>>
                {
                    IsError = false,
                    Message = "Geo NFTs loaded successfully",
                    Result = geoNFTs
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ISTARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error loading geo NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGeoNFT(Guid id)
        {
            try
            {
                var geoNFT = await _starAPI.GeoNFTs.LoadAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ISTARGeoNFT>
                {
                    IsError = true,
                    Message = $"Error loading geo NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateGeoNFT([FromBody] ISTARGeoNFT geoNFT)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.SaveAsync(geoNFT);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ISTARGeoNFT>
                {
                    IsError = true,
                    Message = $"Error creating geo NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGeoNFT(Guid id, [FromBody] ISTARGeoNFT geoNFT)
        {
            try
            {
                geoNFT.Id = id;
                var result = await _starAPI.GeoNFTs.SaveAsync(geoNFT);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ISTARGeoNFT>
                {
                    IsError = true,
                    Message = $"Error updating geo NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGeoNFT(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.DeleteAsync(id);
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

        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyGeoNFTs([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 10.0)
        {
            try
            {
                var geoNFTs = await _starAPI.GeoNFTs.LoadAllNearAsync(latitude, longitude, radiusKm);
                return Ok(new OASISResult<IEnumerable<ISTARGeoNFT>>
                {
                    IsError = false,
                    Message = "Nearby geo NFTs loaded successfully",
                    Result = geoNFTs
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ISTARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error loading nearby geo NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-avatar/{avatarId}")]
        public async Task<IActionResult> GetGeoNFTsByAvatar(Guid avatarId)
        {
            try
            {
                var geoNFTs = await _starAPI.GeoNFTs.LoadAllForAvatarAsync(avatarId);
                return Ok(new OASISResult<IEnumerable<ISTARGeoNFT>>
                {
                    IsError = false,
                    Message = "Avatar geo NFTs loaded successfully",
                    Result = geoNFTs
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ISTARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error loading avatar geo NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
