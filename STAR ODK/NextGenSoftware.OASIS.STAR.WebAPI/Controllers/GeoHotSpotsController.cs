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
    [ApiController]
    [Route("api/[controller]")]
    public class GeoHotSpotsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
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

        [HttpGet("{id}")]
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

        [HttpPost]
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

        [HttpPut("{id}")]
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

        [HttpDelete("{id}")]
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

        [HttpGet("nearby")]
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
