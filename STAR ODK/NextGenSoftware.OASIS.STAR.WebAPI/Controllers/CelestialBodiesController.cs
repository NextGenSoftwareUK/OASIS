using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CelestialBodiesController : ControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllCelestialBodies()
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadAllAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), null);
                if (result.IsError)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetICelestialBody(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), id);
                if (result.IsError)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateICelestialBody([FromBody] ICelestialBody celestialBody)
        {
            try
            {
                throw new NotImplementedException("SaveAsync method not yet implemented - use SaveHolonAsync instead");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ICelestialBody>
                {
                    IsError = true,
                    Message = $"Error creating celestial body: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateICelestialBody(Guid id, [FromBody] ICelestialBody celestialBody)
        {
            try
            {
                celestialBody.Id = id;
                throw new NotImplementedException("SaveAsync method not yet implemented - use SaveHolonAsync instead");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ICelestialBody>
                {
                    IsError = true,
                    Message = $"Error updating celestial body: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteICelestialBody(Guid id)
        {
            try
            {
                throw new NotImplementedException("DeleteAsync method not yet implemented");
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
                return BadRequest(new OASISResult<IEnumerable<ICelestialBody>>
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
                return BadRequest(new OASISResult<IEnumerable<ICelestialBody>>
                {
                    IsError = true,
                    Message = $"Error loading celestial bodies in space: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
