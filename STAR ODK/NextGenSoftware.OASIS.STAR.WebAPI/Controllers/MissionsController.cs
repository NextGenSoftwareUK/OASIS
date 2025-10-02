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
        /// Retrieves all missions in the system.
        /// </summary>
        /// <returns>List of all missions available in the STAR system.</returns>
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

        /// <summary>
        /// Updates an existing mission by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to update.</param>
        /// <param name="mission">The updated mission details.</param>
        /// <returns>The updated mission with modified data.</returns>
        /// <response code="200">Mission updated successfully</response>
        /// <response code="400">Error updating mission</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<IMission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMission>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Deletes a mission by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Mission deleted successfully</response>
        /// <response code="400">Error deleting mission</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Clones an existing mission with a new name.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to clone.</param>
        /// <param name="request">Clone request containing the new name for the cloned mission.</param>
        /// <returns>The newly created cloned mission.</returns>
        /// <response code="200">Mission cloned successfully</response>
        /// <response code="400">Error cloning mission</response>
        [HttpPost("{id}/clone")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Retrieves missions by a specific type.
        /// </summary>
        /// <param name="type">The mission type to filter by.</param>
        /// <returns>List of missions matching the specified type.</returns>
        /// <response code="200">Missions retrieved successfully</response>
        /// <response code="400">Error retrieving missions by type</response>
        [HttpGet("by-type/{type}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMissionsByType(string type)
        {
            try
            {
                var result = await _starAPI.Missions.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredMissions = result.Result?.Where(m => m.MissionType?.ToString() == type);
                return Ok(new OASISResult<IEnumerable<Mission>>
                {
                    Result = filteredMissions,
                    IsError = false,
                    Message = "Missions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Mission>>
                {
                    IsError = true,
                    Message = $"Error retrieving missions by type: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves missions by status.
        /// </summary>
        /// <param name="status">The mission status to filter by.</param>
        /// <returns>List of missions matching the specified status.</returns>
        /// <response code="200">Missions retrieved successfully</response>
        /// <response code="400">Error retrieving missions by status</response>
        [HttpGet("by-status/{status}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMissionsByStatus(string status)
        {
            try
            {
                var result = await _starAPI.Missions.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredMissions = result.Result?.Where(m => m.Status?.ToString() == status);
                return Ok(new OASISResult<IEnumerable<Mission>>
                {
                    Result = filteredMissions,
                    IsError = false,
                    Message = "Missions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Mission>>
                {
                    IsError = true,
                    Message = $"Error retrieving missions by status: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches missions by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of missions matching the search query.</returns>
        /// <response code="200">Missions retrieved successfully</response>
        /// <response code="400">Error searching missions</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchMissions([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.Missions.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredMissions = result.Result?.Where(m => 
                    m.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    m.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<Mission>>
                {
                    Result = filteredMissions,
                    IsError = false,
                    Message = "Missions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Mission>>
                {
                    IsError = true,
                    Message = $"Error searching missions: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
