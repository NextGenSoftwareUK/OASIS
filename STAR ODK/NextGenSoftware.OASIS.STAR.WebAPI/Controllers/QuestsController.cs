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
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

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
        private QuestManager CreateQuestManager() => new QuestManager(AvatarId, new STARDNA());

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

                var filteredQuests = result.Result?.Where(q => q.QuestType.ToString() == type);
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

                var filteredQuests = result.Result?.Where(q => q.Status.ToString() == status);
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

        /// <summary>
        /// Creates a new quest with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing quest details and source folder path.</param>
        /// <returns>Result of the quest creation operation.</returns>
        /// <response code="200">Quest created successfully</response>
        /// <response code="400">Error creating quest</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateQuestWithOptions([FromBody] CreateQuestRequest request)
        {
            try
            {
                var result = await _starAPI.Quests.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Quest>
                {
                    IsError = true,
                    Message = $"Error creating quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a quest by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to load.</param>
        /// <param name="version">The version of the quest to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested quest details.</returns>
        /// <response code="200">Quest loaded successfully</response>
        /// <response code="400">Error loading quest</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadQuest(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Quests.LoadAsync(AvatarId, id, version, holonTypeEnum);
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
        /// Loads a quest from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded quest details.</returns>
        /// <response code="200">Quest loaded successfully</response>
        /// <response code="400">Error loading quest</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadQuestFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Quests.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Quest>
                {
                    IsError = true,
                    Message = $"Error loading quest from path: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a quest from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published quest file.</param>
        /// <returns>The loaded quest details.</returns>
        /// <response code="200">Quest loaded successfully</response>
        /// <response code="400">Error loading quest</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadQuestFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.Quests.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Quest>
                {
                    IsError = true,
                    Message = $"Error loading quest from published file: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads all quests for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of quests.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all quests for the avatar.</returns>
        /// <response code="200">Quests loaded successfully</response>
        /// <response code="400">Error loading quests</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllQuestsForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error loading quests for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a quest to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the quest publish operation.</returns>
        /// <response code="200">Quest published successfully</response>
        /// <response code="400">Error publishing quest</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishQuest(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Quests.PublishAsync(
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
                return BadRequest(new OASISResult<Quest>
                {
                    IsError = true,
                    Message = $"Error publishing quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Downloads a quest from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to download.</param>
        /// <param name="version">The version of the quest to download.</param>
        /// <param name="downloadPath">Optional path where the quest should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the quest download operation.</returns>
        /// <response code="200">Quest downloaded successfully</response>
        /// <response code="400">Error downloading quest</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedQuest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedQuest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadQuest(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.Quests.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<DownloadedQuest>
                {
                    IsError = true,
                    Message = $"Error downloading quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all versions of a specific quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to get versions for.</param>
        /// <returns>List of all versions of the specified quest.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.Quests.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error retrieving quest versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested quest version details.</returns>
        /// <response code="200">Quest version loaded successfully</response>
        /// <response code="400">Error loading quest version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadQuestVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.Quests.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Quest>
                {
                    IsError = true,
                    Message = $"Error loading quest version: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Edits a quest with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the quest edit operation.</returns>
        /// <response code="200">Quest edited successfully</response>
        /// <response code="400">Error editing quest</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditQuest(Guid id, [FromBody] EditQuestRequest request)
        {
            try
            {
                var result = await _starAPI.Quests.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Quest>
                {
                    IsError = true,
                    Message = $"Error editing quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Unpublishes a quest from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to unpublish.</param>
        /// <param name="version">The version of the quest to unpublish.</param>
        /// <returns>Result of the quest unpublish operation.</returns>
        /// <response code="200">Quest unpublished successfully</response>
        /// <response code="400">Error unpublishing quest</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Quest>
                {
                    IsError = true,
                    Message = $"Error unpublishing quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Republishes a quest to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to republish.</param>
        /// <param name="version">The version of the quest to republish.</param>
        /// <returns>Result of the quest republish operation.</returns>
        /// <response code="200">Quest republished successfully</response>
        /// <response code="400">Error republishing quest</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Quest>
                {
                    IsError = true,
                    Message = $"Error republishing quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Activates a quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to activate.</param>
        /// <param name="version">The version of the quest to activate.</param>
        /// <returns>Result of the quest activation operation.</returns>
        /// <response code="200">Quest activated successfully</response>
        /// <response code="400">Error activating quest</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Quest>
                {
                    IsError = true,
                    Message = $"Error activating quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deactivates a quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to deactivate.</param>
        /// <param name="version">The version of the quest to deactivate.</param>
        /// <returns>Result of the quest deactivation operation.</returns>
        /// <response code="200">Quest deactivated successfully</response>
        /// <response code="400">Error deactivating quest</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Quest>
                {
                    IsError = true,
                    Message = $"Error deactivating quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Starts a quest for the authenticated avatar.
        /// </summary>
        [HttpPost("{id}/start")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartQuest(Guid id, [FromBody] string startNotes = null)
        {
            try
            {
                var result = await CreateQuestManager().StartQuestAsync(AvatarId, id, startNotes);
                if (result.IsError)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error starting quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Completes an objective (sub-quest) for a quest.
        /// </summary>
        [HttpPost("{id}/objectives/{objectiveId}/complete")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteQuestObjective(Guid id, Guid objectiveId, [FromBody] CompleteQuestObjectiveRequest request = null)
        {
            try
            {
                var result = await CreateQuestManager().CompleteQuestObjectiveAsync(
                    AvatarId,
                    id,
                    objectiveId,
                    request?.GameSource,
                    request?.CompletionNotes);

                if (result.IsError)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error completing quest objective: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Completes a quest for the authenticated avatar.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to complete.</param>
        /// <param name="completionNotes">Optional completion notes.</param>
        /// <returns>Result of the quest completion operation.</returns>
        /// <response code="200">Quest completed successfully</response>
        /// <response code="400">Error completing quest</response>
        [HttpPost("{id}/complete")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteQuest(Guid id, [FromBody] string completionNotes = null)
        {
            try
            {
                var result = await CreateQuestManager().CompleteQuestAsync(AvatarId, id, completionNotes);
                if (result.IsError)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error completing quest: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets quest leaderboard for a specific quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest.</param>
        /// <param name="limit">Number of entries to return (default: 50).</param>
        /// <returns>Quest leaderboard entries.</returns>
        /// <response code="200">Leaderboard retrieved successfully</response>
        /// <response code="400">Error retrieving leaderboard</response>
        [HttpGet("{id}/leaderboard")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<QuestLeaderboard>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<QuestLeaderboard>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestLeaderboard(Guid id, [FromQuery] int limit = 50)
        {
            try
            {
                var managerResult = await CreateQuestManager().GetQuestLeaderboardAsync(id, limit);
                var result = new OASISResult<IEnumerable<QuestLeaderboard>>
                {
                    Result = managerResult.Result,
                    IsError = managerResult.IsError,
                    Message = managerResult.Message,
                    ErrorCode = managerResult.ErrorCode,
                    Exception = managerResult.Exception
                };

                if (result.IsError)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<QuestLeaderboard>>
                {
                    IsError = true,
                    Message = $"Error retrieving quest leaderboard: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets quest rewards for a specific quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest.</param>
        /// <returns>Quest rewards.</returns>
        /// <response code="200">Rewards retrieved successfully</response>
        /// <response code="400">Error retrieving rewards</response>
        [HttpGet("{id}/rewards")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<QuestReward>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<QuestReward>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestRewards(Guid id)
        {
            try
            {
                var managerResult = await CreateQuestManager().GetQuestRewardsAsync(id);
                var result = new OASISResult<IEnumerable<QuestReward>>
                {
                    Result = managerResult.Result,
                    IsError = managerResult.IsError,
                    Message = managerResult.Message,
                    ErrorCode = managerResult.ErrorCode,
                    Exception = managerResult.Exception
                };

                if (result.IsError)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<QuestReward>>
                {
                    IsError = true,
                    Message = $"Error retrieving quest rewards: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets quest statistics for the authenticated avatar.
        /// </summary>
        /// <returns>Quest statistics.</returns>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="400">Error retrieving statistics</response>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<string, object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Dictionary<string, object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestStats()
        {
            try
            {
                var result = await CreateQuestManager().GetQuestStatsAsync(AvatarId);
                if (result.IsError)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, object>>
                {
                    IsError = true,
                    Message = $"Error retrieving quest statistics: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateQuestRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.Quest;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<Quest, STARNETDNA>? CreateOptions { get; set; } = null;
    }

    public class EditQuestRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class CompleteQuestObjectiveRequest
    {
        public string GameSource { get; set; } = "";
        public string CompletionNotes { get; set; } = "";
    }
}
