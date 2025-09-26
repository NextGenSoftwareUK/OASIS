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
    [ApiController]
    [Route("api/[controller]")]
    public class ZomesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllZomes()
        {
            try
            {
                var result = await _starAPI.Zomes.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error loading zomes: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetZome(Guid id)
        {
            try
            {
                var result = await _starAPI.Zomes.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error loading zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateZome([FromBody] STARZome zome)
        {
            try
            {
                var result = await _starAPI.Zomes.UpdateAsync(AvatarId, zome);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error creating zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateZome(Guid id, [FromBody] STARZome zome)
        {
            try
            {
                zome.Id = id;
                var result = await _starAPI.Zomes.UpdateAsync(AvatarId, zome);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error updating zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteZome(Guid id)
        {
            try
            {
                var result = await _starAPI.Zomes.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-type/{type}")]
        public async Task<IActionResult> GetZomesByType(string type)
        {
            try
            {
                throw new NotImplementedException("LoadAllOfTypeAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error loading zomes of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("in-space/{spaceId}")]
        public async Task<IActionResult> GetZomesInSpace(Guid spaceId)
        {
            try
            {
                throw new NotImplementedException("LoadAllInSpaceAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error loading zomes in space: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
