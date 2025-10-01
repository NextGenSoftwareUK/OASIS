using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Mission management endpoints for creating, updating, and managing STAR missions.
    /// Missions are structured objectives that avatars can undertake within the STAR ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MissionsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all missions for the authenticated avatar.
        /// </summary>
        /// <returns>List of all missions associated with the current avatar.</returns>
        /// <response code="200">Missions retrieved successfully</response>
        /// <response code="400">Error retrieving missions</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllMissions()
        {
            try
            {
                var result = await _starAPI.Missions.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Mission>>
                {
                    IsError = true,
                    Message = $"Error loading missions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific mission by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to retrieve.</param>
        /// <returns>The requested mission details.</returns>
        /// <response code="200">Mission retrieved successfully</response>
        /// <response code="400">Error retrieving mission</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMission(Guid id)
        {
            try
            {
                var result = await _starAPI.Missions.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Mission>
                {
                    IsError = true,
                    Message = $"Error loading mission: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new mission for the authenticated avatar.
        /// </summary>
        /// <param name="mission">The mission details to create.</param>
        /// <returns>The created mission with assigned ID and metadata.</returns>
        /// <response code="200">Mission created successfully</response>
        /// <response code="400">Error creating mission</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<IMission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateMission([FromBody] IMission mission)
        {
            try
            {
                var result = await _starAPI.Missions.UpdateAsync(AvatarId, (Mission)mission);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IMission>
                {
                    IsError = true,
                    Message = $"Error creating mission: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMission(Guid id, [FromBody] IMission mission)
        {
            try
            {
                mission.Id = id;
                var result = await _starAPI.Missions.UpdateAsync(AvatarId, (Mission)mission);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IMission>
                {
                    IsError = true,
                    Message = $"Error updating mission: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMission(Guid id)
        {
            try
            {
                var result = await _starAPI.Missions.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting mission: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/clone")]
        public async Task<IActionResult> CloneMission(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.Missions.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning mission: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
