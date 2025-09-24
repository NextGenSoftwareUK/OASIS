using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IQuestsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
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

        [HttpGet("{id}")]
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

        [HttpPost]
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

        [HttpPut("{id}")]
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

        [HttpDelete("{id}")]
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

        [HttpGet("by-avatar/{avatarId}")]
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
    }
}
