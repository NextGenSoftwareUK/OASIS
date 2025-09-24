using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ICelestialSpacesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllICelestialSpaces()
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ICelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error loading celestial spaces: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetICelestialSpace(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ICelestialSpace>
                {
                    IsError = true,
                    Message = $"Error loading celestial space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateICelestialSpace([FromBody] ICelestialSpace celestialSpace)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.UpdateAsync(AvatarId, (STARCelestialSpace)celestialSpace);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ICelestialSpace>
                {
                    IsError = true,
                    Message = $"Error creating celestial space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateICelestialSpace(Guid id, [FromBody] ICelestialSpace celestialSpace)
        {
            try
            {
                celestialSpace.Id = id;
                var result = await _starAPI.CelestialSpaces.UpdateAsync(AvatarId, (STARCelestialSpace)celestialSpace);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ICelestialSpace>
                {
                    IsError = true,
                    Message = $"Error updating celestial space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteICelestialSpace(Guid id)
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
        public async Task<IActionResult> GetICelestialSpacesByType(string type)
        {
            try
            {
                throw new NotImplementedException("LoadAllOfTypeAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ICelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error loading celestial spaces of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("in-space/{parentSpaceId}")]
        public async Task<IActionResult> GetICelestialSpacesInSpace(Guid parentSpaceId)
        {
            try
            {
                throw new NotImplementedException("LoadAllInSpaceAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ICelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error loading celestial spaces in parent space: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
