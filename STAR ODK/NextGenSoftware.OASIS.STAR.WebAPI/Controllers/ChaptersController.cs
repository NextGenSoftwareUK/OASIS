using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Chapters management endpoints for creating, updating, and managing STAR chapters.
    /// Chapters represent story segments, quest lines, or narrative components within the STAR universe.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChaptersController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all chapters for the authenticated avatar.
        /// </summary>
        /// <returns>List of all chapters associated with the current avatar.</returns>
        /// <response code="200">Chapters retrieved successfully</response>
        /// <response code="400">Error retrieving chapters</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Chapter>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Chapter>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllChapters()
        {
            try
            {
                var result = await _starAPI.Chapters.LoadAllAsync(AvatarId, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Chapter>>
                {
                    IsError = true,
                    Message = $"Error loading chapters: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChapter(Guid id)
        {
            try
            {
                var result = await _starAPI.Chapters.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error loading chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateChapter([FromBody] Chapter chapter)
        {
            try
            {
                var result = await _starAPI.Chapters.UpdateAsync(AvatarId, chapter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error creating chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateChapter(Guid id, [FromBody] Chapter chapter)
        {
            try
            {
                chapter.Id = id;
                var result = await _starAPI.Chapters.UpdateAsync(AvatarId, chapter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error updating chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChapter(Guid id)
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
