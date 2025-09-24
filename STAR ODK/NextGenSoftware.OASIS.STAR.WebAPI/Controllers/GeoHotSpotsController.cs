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
    public class IGeoHotSpotsController : ControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllIGeoHotSpots()
        {
            try
            {
                var hotSpots = await _starAPI.GeoHotSpots.LoadAllAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), null);
                return Ok(new OASISResult<IEnumerable<IGeoHotSpot>>
                {
                    IsError = false,
                    Message = "Geo hot spots loaded successfully",
                    Result = hotSpots
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IGeoHotSpot>>
                {
                    IsError = true,
                    Message = $"Error loading geo hot spots: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetIGeoHotSpot(Guid id)
        {
            try
            {
                var hotSpot = await _starAPI.GeoHotSpots.LoadAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IGeoHotSpot>
                {
                    IsError = true,
                    Message = $"Error loading geo hot spot: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateIGeoHotSpot([FromBody] IGeoHotSpot hotSpot)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.SaveAsync(hotSpot);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IGeoHotSpot>
                {
                    IsError = true,
                    Message = $"Error creating geo hot spot: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIGeoHotSpot(Guid id, [FromBody] IGeoHotSpot hotSpot)
        {
            try
            {
                hotSpot.Id = id;
                var result = await _starAPI.GeoHotSpots.SaveAsync(hotSpot);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IGeoHotSpot>
                {
                    IsError = true,
                    Message = $"Error updating geo hot spot: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIGeoHotSpot(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.DeleteAsync(id);
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
        public async Task<IActionResult> GetNearbyIGeoHotSpots([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 10.0)
        {
            try
            {
                var hotSpots = await _starAPI.GeoHotSpots.LoadAllNearAsync(latitude, longitude, radiusKm);
                return Ok(new OASISResult<IEnumerable<IGeoHotSpot>>
                {
                    IsError = false,
                    Message = "Nearby geo hot spots loaded successfully",
                    Result = hotSpots
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IGeoHotSpot>>
                {
                    IsError = true,
                    Message = $"Error loading nearby geo hot spots: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
