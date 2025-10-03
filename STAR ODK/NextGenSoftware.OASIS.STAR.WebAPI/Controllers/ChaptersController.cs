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
        /// Retrieves all chapters in the system.
        /// </summary>
        /// <returns>List of all chapters available in the STAR system.</returns>
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

        /// <summary>
        /// Retrieves a specific chapter by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to retrieve.</param>
        /// <returns>The requested chapter details.</returns>
        /// <response code="200">Chapter retrieved successfully</response>
        /// <response code="400">Error retrieving chapter</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Creates a new chapter for the authenticated avatar.
        /// </summary>
        /// <param name="chapter">The chapter details to create.</param>
        /// <returns>The created chapter with assigned ID and metadata.</returns>
        /// <response code="200">Chapter created successfully</response>
        /// <response code="400">Error creating chapter</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Updates an existing chapter by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to update.</param>
        /// <param name="chapter">The updated chapter details.</param>
        /// <returns>The updated chapter with modified data.</returns>
        /// <response code="200">Chapter updated successfully</response>
        /// <response code="400">Error updating chapter</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Deletes a chapter by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Chapter deleted successfully</response>
        /// <response code="400">Error deleting chapter</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Searches chapters by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of chapters matching the search query.</returns>
        /// <response code="200">Chapters retrieved successfully</response>
        /// <response code="400">Error searching chapters</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Chapter>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Chapter>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchChapters([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.Chapters.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredChapters = result.Result?.Where(c => 
                    c.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    c.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<Chapter>>
                {
                    Result = filteredChapters,
                    IsError = false,
                    Message = "Chapters retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Chapter>>
                {
                    IsError = true,
                    Message = $"Error searching chapters: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
