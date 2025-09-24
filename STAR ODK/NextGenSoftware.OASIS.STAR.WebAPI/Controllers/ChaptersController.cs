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
    public class IChaptersController : ControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllIChapters()
        {
            try
            {
                var chapters = await _starAPI.Chapters.LoadAllAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), null);
                return Ok(new OASISResult<IEnumerable<IChapter>>
                {
                    IsError = false,
                    Message = "IChapters loaded successfully",
                    Result = chapters
                });
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
                var chapter = await _starAPI.Chapters.LoadAsync(Guid.Parse("00000000-0000-0000-0000-000000000000"), id);
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
                var result = await _starAPI.Chapters.SaveAsync(chapter);
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
                var result = await _starAPI.Chapters.SaveAsync(chapter);
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
                var result = await _starAPI.Chapters.DeleteAsync(id);
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
