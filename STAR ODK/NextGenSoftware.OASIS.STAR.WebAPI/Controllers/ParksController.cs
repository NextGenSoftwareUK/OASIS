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
    public class IParksController : ControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllIParks()
        {
            try
            {
                var parks = await _starAPI.Parks.LoadAllAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), null);
                return Ok(new OASISResult<IEnumerable<IPark>>
                {
                    IsError = false,
                    Message = "IParks loaded successfully",
                    Result = parks
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IPark>>
                {
                    IsError = true,
                    Message = $"Error loading parks: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetIPark(Guid id)
        {
            try
            {
                var park = await _starAPI.Parks.LoadAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IPark>
                {
                    IsError = true,
                    Message = $"Error loading park: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateIPark([FromBody] IPark park)
        {
            try
            {
                var result = await _starAPI.Parks.SaveAsync(park);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IPark>
                {
                    IsError = true,
                    Message = $"Error creating park: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIPark(Guid id, [FromBody] IPark park)
        {
            try
            {
                park.Id = id;
                var result = await _starAPI.Parks.SaveAsync(park);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IPark>
                {
                    IsError = true,
                    Message = $"Error updating park: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIPark(Guid id)
        {
            try
            {
                var result = await _starAPI.Parks.DeleteAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting park: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyIParks([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 10.0)
        {
            try
            {
                var parks = await _starAPI.Parks.LoadAllNearAsync(latitude, longitude, radiusKm);
                return Ok(new OASISResult<IEnumerable<IPark>>
                {
                    IsError = false,
                    Message = "Nearby parks loaded successfully",
                    Result = parks
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IPark>>
                {
                    IsError = true,
                    Message = $"Error loading nearby parks: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-type/{type}")]
        public async Task<IActionResult> GetIParksByType(string type)
        {
            try
            {
                var parks = await _starAPI.Parks.LoadAllOfTypeAsync(type);
                return Ok(new OASISResult<IEnumerable<IPark>>
                {
                    IsError = false,
                    Message = $"IParks of type {type} loaded successfully",
                    Result = parks
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IPark>>
                {
                    IsError = true,
                    Message = $"Error loading parks of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
