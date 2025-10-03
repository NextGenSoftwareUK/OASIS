using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Chapters management endpoints for creating, updating, and managing STAR chapters.
    /// Chapters represent story segments, quest lines, or narrative components within the OASIS Omniverse. Chapters contain Missions which contain Quests which contain Sub-quests.
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

        /// <summary>
        /// Creates a new chapter with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing chapter details and source folder path.</param>
        /// <returns>Result of the chapter creation operation.</returns>
        /// <response code="200">Chapter created successfully</response>
        /// <response code="400">Error creating chapter</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateChapterWithOptions([FromBody] CreateChapterRequest request)
        {
            try
            {
                var result = await _starAPI.Chapters.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
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
        /// Loads a chapter by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to load.</param>
        /// <param name="version">The version of the chapter to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested chapter details.</returns>
        /// <response code="200">Chapter loaded successfully</response>
        /// <response code="400">Error loading chapter</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadChapter(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Chapters.LoadAsync(AvatarId, id, version, holonTypeEnum);
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
        /// Loads a chapter from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded chapter details.</returns>
        /// <response code="200">Chapter loaded successfully</response>
        /// <response code="400">Error loading chapter</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadChapterFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Chapters.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error loading chapter from path: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a chapter from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published chapter file.</param>
        /// <returns>The loaded chapter details.</returns>
        /// <response code="200">Chapter loaded successfully</response>
        /// <response code="400">Error loading chapter</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadChapterFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.Chapters.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error loading chapter from published file: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads all chapters for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of chapters.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all chapters for the avatar.</returns>
        /// <response code="200">Chapters loaded successfully</response>
        /// <response code="400">Error loading chapters</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Chapter>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Chapter>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllChaptersForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Chapters.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Chapter>>
                {
                    IsError = true,
                    Message = $"Error loading chapters for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a chapter to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the chapter publish operation.</returns>
        /// <response code="200">Chapter published successfully</response>
        /// <response code="400">Error publishing chapter</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishChapter(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Chapters.PublishAsync(
                    AvatarId, 
                    request.SourcePath, 
                    request.LaunchTarget, 
                    request.PublishPath, 
                    request.Edit, 
                    request.RegisterOnSTARNET, 
                    request.GenerateBinary, 
                    request.UploadToCloud
                );
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error publishing chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Downloads a chapter from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to download.</param>
        /// <param name="version">The version of the chapter to download.</param>
        /// <param name="downloadPath">Optional path where the chapter should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the chapter download operation.</returns>
        /// <response code="200">Chapter downloaded successfully</response>
        /// <response code="400">Error downloading chapter</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedChapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedChapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadChapter(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.Chapters.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<DownloadedChapter>
                {
                    IsError = true,
                    Message = $"Error downloading chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all versions of a specific chapter.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to get versions for.</param>
        /// <returns>List of all versions of the specified chapter.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Chapter>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Chapter>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetChapterVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.Chapters.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Chapter>>
                {
                    IsError = true,
                    Message = $"Error retrieving chapter versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a chapter.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested chapter version details.</returns>
        /// <response code="200">Chapter version loaded successfully</response>
        /// <response code="400">Error loading chapter version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadChapterVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.Chapters.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error loading chapter version: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Edits a chapter with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the chapter edit operation.</returns>
        /// <response code="200">Chapter edited successfully</response>
        /// <response code="400">Error editing chapter</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditChapter(Guid id, [FromBody] EditChapterRequest request)
        {
            try
            {
                var result = await _starAPI.Chapters.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error editing chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Unpublishes a chapter from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to unpublish.</param>
        /// <param name="version">The version of the chapter to unpublish.</param>
        /// <returns>Result of the chapter unpublish operation.</returns>
        /// <response code="200">Chapter unpublished successfully</response>
        /// <response code="400">Error unpublishing chapter</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishChapter(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Chapters.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error unpublishing chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Republishes a chapter to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to republish.</param>
        /// <param name="version">The version of the chapter to republish.</param>
        /// <returns>Result of the chapter republish operation.</returns>
        /// <response code="200">Chapter republished successfully</response>
        /// <response code="400">Error republishing chapter</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishChapter(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Chapters.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error republishing chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Activates a chapter.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to activate.</param>
        /// <param name="version">The version of the chapter to activate.</param>
        /// <returns>Result of the chapter activation operation.</returns>
        /// <response code="200">Chapter activated successfully</response>
        /// <response code="400">Error activating chapter</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateChapter(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Chapters.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error activating chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deactivates a chapter.
        /// </summary>
        /// <param name="id">The unique identifier of the chapter to deactivate.</param>
        /// <param name="version">The version of the chapter to deactivate.</param>
        /// <returns>Result of the chapter deactivation operation.</returns>
        /// <response code="200">Chapter deactivated successfully</response>
        /// <response code="400">Error deactivating chapter</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Chapter>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateChapter(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Chapters.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Chapter>
                {
                    IsError = true,
                    Message = $"Error deactivating chapter: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateChapterRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.Chapter;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<Chapter, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditChapterRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }


    public class DownloadedChapter
    {
        public Chapter Chapter { get; set; } = new Chapter();
        public string DownloadPath { get; set; } = "";
        public bool Success { get; set; } = false;
    }

}
