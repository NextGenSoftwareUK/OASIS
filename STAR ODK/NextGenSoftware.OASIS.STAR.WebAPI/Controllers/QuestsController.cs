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
    /// Quest management endpoints for creating, updating, and managing STAR quests.
    /// Quests are interactive challenges and objectives that avatars can complete for rewards and progression.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QuestsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all quests in the system.
        /// </summary>
        /// <returns>List of all quests available in the STAR system.</returns>
        /// <response code="200">Quests retrieved successfully</response>
        /// <response code="400">Error retrieving quests</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllIQuests()
        {
            try
            {
                var result = await _starAPI.Quests.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error loading quests: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific quest by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to retrieve.</param>
        /// <returns>The requested quest details.</returns>
        /// <response code="200">Quest retrieved successfully</response>
        /// <response code="400">Error retrieving quest</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetIQuest(Guid id)
        {
            try
            {
                var result = await _starAPI.Quests.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Quest>
                {
                    IsError = true,
                    Message = $"Error loading quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new quest for the authenticated avatar.
        /// </summary>
        /// <param name="quest">The quest details to create.</param>
        /// <returns>The created quest with assigned ID and metadata.</returns>
        /// <response code="200">Quest created successfully</response>
        /// <response code="400">Error creating quest</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<IQuest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IQuest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateIQuest([FromBody] IQuest quest)
        {
            try
            {
                var result = await _starAPI.Quests.UpdateAsync(AvatarId, (Quest)quest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IQuest>
                {
                    IsError = true,
                    Message = $"Error creating quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates an existing quest by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to update.</param>
        /// <param name="quest">The updated quest details.</param>
        /// <returns>The updated quest with modified data.</returns>
        /// <response code="200">Quest updated successfully</response>
        /// <response code="400">Error updating quest</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<IQuest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IQuest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateIQuest(Guid id, [FromBody] IQuest quest)
        {
            try
            {
                quest.Id = id;
                var result = await _starAPI.Quests.UpdateAsync(AvatarId, (Quest)quest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IQuest>
                {
                    IsError = true,
                    Message = $"Error updating quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deletes a quest by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Quest deleted successfully</response>
        /// <response code="400">Error deleting quest</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteIQuest(Guid id)
        {
            try
            {
                var result = await _starAPI.Quests.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves all quests for a specific avatar.
        /// </summary>
        /// <param name="avatarId">The unique identifier of the avatar.</param>
        /// <returns>List of all quests associated with the specified avatar.</returns>
        /// <response code="200">Avatar quests retrieved successfully</response>
        /// <response code="400">Error retrieving avatar quests</response>
        [HttpGet("by-avatar/{avatarId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetIQuestsByAvatar(Guid avatarId)
        {
            try
            {
                var result = await _starAPI.Quests.LoadAllForAvatarAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error loading avatar quests: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Clones an existing quest with a new name.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to clone.</param>
        /// <param name="request">Clone request containing the new name for the cloned quest.</param>
        /// <returns>The newly created cloned quest.</returns>
        /// <response code="200">Quest cloned successfully</response>
        /// <response code="400">Error cloning quest</response>
        [HttpPost("{id}/clone")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CloneQuest(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.Quests.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves quests by a specific type.
        /// </summary>
        /// <param name="type">The quest type to filter by.</param>
        /// <returns>List of quests matching the specified type.</returns>
        /// <response code="200">Quests retrieved successfully</response>
        /// <response code="400">Error retrieving quests by type</response>
        [HttpGet("by-type/{type}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestsByType(string type)
        {
            try
            {
                var result = await _starAPI.Quests.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredQuests = result.Result?.Where(q => q.QuestType?.ToString() == type);
                return Ok(new OASISResult<IEnumerable<Quest>>
                {
                    Result = filteredQuests,
                    IsError = false,
                    Message = "Quests retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error retrieving quests by type: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves quests by status.
        /// </summary>
        /// <param name="status">The quest status to filter by.</param>
        /// <returns>List of quests matching the specified status.</returns>
        /// <response code="200">Quests retrieved successfully</response>
        /// <response code="400">Error retrieving quests by status</response>
        [HttpGet("by-status/{status}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestsByStatus(string status)
        {
            try
            {
                var result = await _starAPI.Quests.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredQuests = result.Result?.Where(q => q.Status?.ToString() == status);
                return Ok(new OASISResult<IEnumerable<Quest>>
                {
                    Result = filteredQuests,
                    IsError = false,
                    Message = "Quests retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error retrieving quests by status: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches quests by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of quests matching the search query.</returns>
        /// <response code="200">Quests retrieved successfully</response>
        /// <response code="400">Error searching quests</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchQuests([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.Quests.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredQuests = result.Result?.Where(q => 
                    q.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    q.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<Quest>>
                {
                    Result = filteredQuests,
                    IsError = false,
                    Message = "Quests retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error searching quests: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
