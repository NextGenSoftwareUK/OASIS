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
    public class IQuestsController : ControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllIQuests()
        {
            try
            {
                var quests = await _starAPI.Quests.LoadAllAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), null);
                return Ok(new OASISResult<IEnumerable<IQuest>>
                {
                    IsError = false,
                    Message = "IQuests loaded successfully",
                    Result = quests
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IQuest>>
                {
                    IsError = true,
                    Message = $"Error loading quests: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetIQuest(Guid id)
        {
            try
            {
                var quest = await _starAPI.Quests.LoadAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IQuest>
                {
                    IsError = true,
                    Message = $"Error loading quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateIQuest([FromBody] IQuest quest)
        {
            try
            {
                var result = await _starAPI.Quests.SaveAsync(quest);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIQuest(Guid id, [FromBody] IQuest quest)
        {
            try
            {
                quest.Id = id;
                var result = await _starAPI.Quests.SaveAsync(quest);
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIQuest(Guid id)
        {
            try
            {
                var result = await _starAPI.Quests.DeleteAsync(id);
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

        [HttpGet("by-avatar/{avatarId}")]
        public async Task<IActionResult> GetIQuestsByAvatar(Guid avatarId)
        {
            try
            {
                var quests = await _starAPI.Quests.LoadAllForAvatarAsync(avatarId);
                return Ok(new OASISResult<IEnumerable<IQuest>>
                {
                    IsError = false,
                    Message = "Avatar quests loaded successfully",
                    Result = quests
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IQuest>>
                {
                    IsError = true,
                    Message = $"Error loading avatar quests: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
