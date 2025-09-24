using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChaptersController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllIChapters()
        {
            try
            {
                var result = await _starAPI.Chapters.LoadAllAsync(AvatarId, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IChapter>>
                {
                    IsError = true,
                    Message = $"Error loading chapters: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetIChapter(Guid id)
        {
            try
            {
                var result = await _starAPI.Chapters.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IChapter>
                {
                    IsError = true,
                    Message = $"Error loading chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateIChapter([FromBody] IChapter chapter)
        {
            try
            {
                var result = await _starAPI.Chapters.UpdateAsync(AvatarId, (Chapter)chapter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IChapter>
                {
                    IsError = true,
                    Message = $"Error creating chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIChapter(Guid id, [FromBody] IChapter chapter)
        {
            try
            {
                chapter.Id = id;
                var result = await _starAPI.Chapters.UpdateAsync(AvatarId, (Chapter)chapter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IChapter>
                {
                    IsError = true,
                    Message = $"Error updating chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIChapter(Guid id)
        {
            try
            {
                var result = await _starAPI.Chapters.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
