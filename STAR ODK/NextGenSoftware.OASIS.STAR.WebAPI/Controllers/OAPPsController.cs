using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using System.Collections.Generic;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// OAPPs (OASIS Applications) management endpoints for creating, updating, and managing STAR OAPPs.
    /// OAPPs represent applications and services that plug into the OASIS ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OAPPsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all OAPPs in the system.
        /// </summary>
        /// <returns>List of all OAPPs available in the STAR system.</returns>
        /// <response code="200">OAPPs retrieved successfully</response>
        /// <response code="400">Error retrieving OAPPs</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllOAPPs()
        {
            try
            {
                var result = await _starAPI.OAPPs.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<OAPP>>
                {
                    IsError = true,
                    Message = $"Error loading OAPPs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific OAPP by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to retrieve.</param>
        /// <returns>The requested OAPP details.</returns>
        /// <response code="200">OAPP retrieved successfully</response>
        /// <response code="400">Error retrieving OAPP</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetOAPP(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPs.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error loading OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new OAPP for the authenticated avatar.
        /// </summary>
        /// <param name="oapp">The OAPP details to create.</param>
        /// <returns>The created OAPP with assigned ID and metadata.</returns>
        /// <response code="200">OAPP created successfully</response>
        /// <response code="400">Error creating OAPP</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOAPP([FromBody] OAPP oapp)
        {
            try
            {
                var result = await _starAPI.OAPPs.UpdateAsync(AvatarId, oapp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error creating OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOAPP(Guid id, [FromBody] OAPP oapp)
        {
            try
            {
                oapp.Id = id;
                var result = await _starAPI.OAPPs.UpdateAsync(AvatarId, oapp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error updating OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOAPP(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPs.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/clone")]
        public async Task<IActionResult> CloneOAPP(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.OAPPs.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
